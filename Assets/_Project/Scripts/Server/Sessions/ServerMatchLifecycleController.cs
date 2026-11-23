using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Simulation;
using UnityEngine;

namespace SmallArena.Server.Sessions
{
    /// <summary>Owns runtime bindings and geometry; the pure domain owns time, life and phase rules.</summary>
    [RequireComponent(typeof(ArenaNetworkManager)), DisallowMultipleComponent]
    public sealed class ServerMatchLifecycleController : MonoBehaviour
    {
        [SerializeField] private GameObject matchStatePrefab = null;
        [SerializeField, Min(1)] private float matchDurationSeconds = 180;
        [SerializeField, Min(0)] private float countdownSeconds = 3;
        [SerializeField, Min(.01f)] private float respawnSeconds = 3;
        [SerializeField, Min(0)] private int maximumLives = 0;
        [SerializeField, Min(.01f)] private float spawnGraceSeconds = 10;
        [SerializeField, Min(0)] private int targetKills = 5;
        [SerializeField, Min(.01f)] private float postMatchSeconds = 10;
        private ArenaNetworkManager manager;
        private GameplayConfiguration configuration;
        private NetworkMatchController replicated;
        private BoxCollider arenaFloor;
        private double postMatchDeadline;
        private double lastServerTime;
        // Unity's unscaled clock samples fixed time inside physics callbacks. Keep one
        // monotonic timeline across fixed-step, frame, Command and teardown callbacks.
        private double ServerNow => lastServerTime = Math.Max(lastServerTime, NetworkTime.localTime);
        private readonly Dictionary<int, NetworkConnectionToClient> connections = new Dictionary<int, NetworkConnectionToClient>();
        private readonly Dictionary<int, NetworkHealth> actors = new Dictionary<int, NetworkHealth>();
        private readonly Dictionary<int, long> publishedLives = new Dictionary<int, long>();
        private readonly Dictionary<int, double> nextSpawnAttempt = new Dictionary<int, double>();
        public MatchSessionRuntime Session { get; private set; }
        public NetworkMatchController ReplicatedState => replicated;
        public BoxCollider ArenaFloor => arenaFloor;
        public void Configure(GameplayConfiguration value, GameObject statePrefab, BoxCollider floor = null)
        {
            if (Session != null) throw new InvalidOperationException("Configure before starting a session.");
            configuration = value ?? throw new ArgumentNullException(nameof(value)); matchStatePrefab = statePrefab;
            arenaFloor = floor;
        }
        private void OnEnable()
        {
            manager = GetComponent<ArenaNetworkManager>();
            manager.ServerPlayerSpawned += Spawned;
            manager.ServerDisconnected += Disconnected;
            manager.ServerStopped += Close;
            manager.ServerSceneLoaded += SceneLoaded;
            manager.ServerCanBindParticipant = CanBind;
        }
        private bool CanBind(NetworkConnectionToClient connection) => Session == null ||
            connections.TryGetValue(connection.connectionId, out var expected)
            && ReferenceEquals(expected, connection) && Session.CanBindSpawn(connection.connectionId, ServerNow);
        public void BeginSession(string id, LobbyMember[] members)
        {
            if (!NetworkServer.active || Session != null) throw new InvalidOperationException("One active server session required.");
            var config = configuration ?? new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: 1,
                shotRangeMetres: 1.8, matchDurationSeconds: matchDurationSeconds, respawnDelaySeconds: respawnSeconds,
                minimumSafeSpawnDistanceMetres: 1.5, countdownDurationSeconds: countdownSeconds, maximumLives: maximumLives,
                targetKills: targetKills, postMatchDurationSeconds: postMatchSeconds, spawnGraceSeconds: spawnGraceSeconds);
            Session = new MatchSessionRuntime(id, config);
            Session.Eliminated += Eliminated;
            foreach (var member in members)
            {
                if (!NetworkServer.connections.TryGetValue(member.ConnectionId, out var conn)
                    || !Session.AddParticipant(id + "/" + member.SlotIndex, member.ConnectionId, member.SlotIndex, ServerNow))
                    throw new InvalidOperationException("Roster must contain unique connected participants.");
                connections.Add(member.ConnectionId, conn);
            }
            Session.LockRoster(ServerNow);
            foreach (var conn in connections.Values.ToArray()) if (conn.identity != null) Spawned(conn, conn.identity.gameObject);
        }
        private void SceneLoaded(string scene)
        {
            if (Session != null && (scene == "Arena_MVP" || scene.EndsWith("Arena_MVP.unity")))
            {
                foreach (var arena in FindObjectsByType<ArenaObstacles>())
                    if (arena.isServer) { arenaFloor = arena.FloorCollider; break; }
                EnsureState();
            }
        }
        private void EnsureState()
        {
            if (replicated != null || matchStatePrefab == null) return;
            var instance = Instantiate(matchStatePrefab);
            NetworkServer.Spawn(instance);
            replicated = instance.GetComponent<NetworkMatchController>();
        }
        private void Spawned(NetworkConnectionToClient conn, GameObject player)
        {
            if (Session == null || !connections.TryGetValue(conn.connectionId, out var expected) || expected != conn
                || !Session.BindSpawn(conn.connectionId, ServerNow)) return;
            var p = Session.ByConnection(conn.connectionId);
            var health = player.GetComponent<NetworkHealth>();
            actors.Add(conn.connectionId, health);
            health.BindRuntime(() => Session != null && Session.CanAct(p, ServerNow),
                (amount, attacker) => Damage(p, amount, attacker), () => BeginAttack(p));
            EnsureState(); Publish();
        }
        private bool BeginAttack(MatchParticipantRuntime participant)
        {
            // Sample the monotonic server clock once for both eligibility and commit.
            var now = ServerNow;
            if (Session == null || !Session.TryAttack(participant.ParticipantId, participant.LifeId, now)) return false;
            Publish();
            return true;
        }
        private bool Damage(MatchParticipantRuntime participant, int amount, NetworkHealth attacker)
        {
            if (Session == null || !Session.TryDamage(participant.ParticipantId, participant.LifeId, amount, ServerNow,
                attacker?.ParticipantId, attacker?.LifeId ?? 0)) return false;
            Publish(); return true;
        }
        private void Eliminated(MatchElimination elimination)
        { if (replicated != null) replicated.PublishElimination(elimination, Session); }
        private void Update()
        {
            if (!NetworkServer.active || Session == null) return;
            var now = ServerNow;
            Session.Tick(now);
            foreach (var p in Session.Participants)
            {
                if (!p.IsConnected && connections.TryGetValue(p.ConnectionId, out var conn))
                { connections.Remove(p.ConnectionId); conn.Disconnect(); }
                if (p.LifeState != ParticipantLifeState.Respawning || p.RespawnRemainingSeconds(now) > 0
                    || Session.Phase != MatchLifecyclePhase.InProgress
                    || nextSpawnAttempt.TryGetValue(p.ConnectionId, out var retryAt) && now < retryAt
                    || !actors.TryGetValue(p.ConnectionId, out var actor) || actor == null) continue;
                nextSpawnAttempt[p.ConnectionId] = now + Session.Configuration.SpawnRetryIntervalSeconds;
                if (TrySpawnPosition(actor, p.SlotIndex, out var position) && Session.Match.IsGameplayActive(now))
                {
                    var controller = actor.GetComponent<CharacterController>();
                    controller.enabled = false;
                    actor.GetComponent<ServerPlayerMovement>().ClearRuntimeInput();
                    actor.transform.SetPositionAndRotation(position, Quaternion.identity);
                    Physics.SyncTransforms();
                    actor.GetComponent<NetworkTransformReliable>().ServerTeleport(position, Quaternion.identity);
                    // Keep the participant dead throughout the discontinuous pose commit. Only then
                    // publish the new life and allow the next authoritative physics step.
                    if (Session.TryRespawn(p.ParticipantId, p.LifeId, now, true))
                    { controller.enabled = true; Physics.SyncTransforms(); }
                }
            }
            Publish();
            if (Session != null && Session.Phase == MatchLifecyclePhase.Ended
                && (Session.Match.State == OnlineMatchState.Aborted || now >= postMatchDeadline)
                && manager.RoomEnabled) GetComponent<ServerLobbyController>()?.ReturnToLobby();
        }
        private bool TrySpawnPosition(NetworkHealth actor, int slot, out Vector3 position)
        {
            var cc = actor.GetComponent<CharacterController>();
            if (arenaFloor == null || !arenaFloor.enabled) { position = default; return false; }
            Physics.SyncTransforms();
            var bounds = arenaFloor.bounds;
            var scale = actor.transform.lossyScale;
            var radius = cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var halfHeight = Mathf.Max(radius, cc.height * Mathf.Abs(scale.y) / 2);
            var centerOffset = Vector3.Scale(cc.center, scale);
            var markers = NetworkManager.startPositions;
            for (var n = 0; n < markers.Count; n++)
            {
                var marker = markers[(slot + n) % markers.Count];
                if (marker == null) continue;
                var x = marker.position.x + centerOffset.x; var z = marker.position.z + centerOffset.z;
                // Validate the entire footprint, not just a center ray hitting any Environment object.
                if (x - radius <= bounds.min.x || x + radius >= bounds.max.x
                    || z - radius <= bounds.min.z || z + radius >= bounds.max.z) continue;
                var origin = new Vector3(x, bounds.max.y + 2, z);
                if (!Physics.Raycast(origin, Vector3.down, out var floor, bounds.size.y + 3, 1 << 6, QueryTriggerInteraction.Ignore)
                    || floor.collider != arenaFloor || floor.normal.y < .7f) continue;
                position = new Vector3(x, floor.point.y + halfHeight + .04f, z) - centerOffset;
                var candidate = position;
                if (actors.Values.Any(a => a != null && a != actor && a.CanAct
                    && Vector3.Distance(candidate, a.transform.position) < Session.Configuration.MinimumSafeSpawnDistanceMetres)) continue;
                var center = position + centerOffset;
                var segment = halfHeight - radius;
                if (Physics.OverlapCapsule(center + Vector3.up * segment, center - Vector3.up * segment, radius,
                    (1 << 6) | (1 << 7), QueryTriggerInteraction.Ignore).Any(c => c.gameObject != actor.gameObject)) continue;
                return true;
            }
            position = default; return false;
        }
        private void Publish()
        {
            if (Session == null) return;
            if (Session.Phase == MatchLifecyclePhase.Ended && postMatchDeadline == 0)
                postMatchDeadline = ServerNow + Session.Configuration.PostMatchDurationSeconds;
            foreach (var p in Session.Participants)
            {
                if (!actors.TryGetValue(p.ConnectionId, out var actor) || actor == null) continue;
                var outcome = Session.Phase != MatchLifecyclePhase.Ended ? PlayerMatchResult.Playing
                    : Session.WinnerParticipantId == "" ? PlayerMatchResult.Draw
                    : Session.WinnerParticipantId == p.ParticipantId ? PlayerMatchResult.Victory : PlayerMatchResult.Defeat;
                actor.PublishRuntime(p, Session.Configuration, Session.Phase, outcome);
                var lifeChanged = !publishedLives.TryGetValue(p.ConnectionId, out var life) || life != p.LifeId;
                if (lifeChanged || p.LifeState != ParticipantLifeState.Alive) actor.GetComponent<NetworkCombat>().ResetForLife();
                publishedLives[p.ConnectionId] = p.LifeId;
                actor.GetComponent<CharacterController>().enabled = p.IsConnected && p.LifeState == ParticipantLifeState.Alive;
                if (!actor.CanAct) actor.GetComponent<ServerPlayerMovement>().ClearRuntimeInput();
            }
            if (replicated != null) replicated.Publish(Session, ServerNow, postMatchDeadline);
        }
        private void Disconnected(NetworkConnectionToClient connection)
        {
            if (Session == null || !connections.TryGetValue(connection.connectionId, out var expected) || expected != connection) return;
            Session.Disconnect(connection.connectionId, ServerNow);
            connections.Remove(connection.connectionId); actors.Remove(connection.connectionId); publishedLives.Remove(connection.connectionId);
            Publish();
        }
        private void Close()
        {
            if (Session != null) { Session.Eliminated -= Eliminated; Session.Teardown(ServerNow); }
            Session = null; connections.Clear(); actors.Clear(); publishedLives.Clear(); nextSpawnAttempt.Clear();
            postMatchDeadline = 0;
            arenaFloor = null;
            if (replicated != null && NetworkServer.active) NetworkServer.Destroy(replicated.gameObject);
            replicated = null;
        }
        public void ResetForLobby() { if (NetworkServer.active) Close(); }
        private void OnDisable()
        {
            if (manager != null)
            {
                manager.ServerPlayerSpawned -= Spawned; manager.ServerDisconnected -= Disconnected;
                manager.ServerStopped -= Close; manager.ServerSceneLoaded -= SceneLoaded;
                manager.ServerCanBindParticipant = null;
            }
            Close();
        }
    }
}
