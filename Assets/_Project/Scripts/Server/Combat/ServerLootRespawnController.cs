using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEngine;

namespace SmallArena.Server.Combat
{
    /// <summary>One authoritative weapon lease, retained after the map identity is consumed.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ArenaLootSpawner), typeof(ServerMatchLifecycleController))]
    public sealed class ServerLootRespawnController : MonoBehaviour
    {
        public const float Clearance = .9f;
        public const float WallMargin = 1f;
        public const float MinimumPlayerDistance = 2.5f;
        private ArenaNetworkManager manager;
        private ArenaLootSpawner spawner;
        private ServerMatchLifecycleController lifecycle;
        private NetworkLootItem mapItem;
        private NetworkHealth carrier;
        private NetworkConnectionToClient carrierConnection;
        private MatchSessionRuntime session;
        private LootRespawnTracker tracker;
        private readonly System.Random random = new System.Random();
        private bool requested;
        private double lastTick, nextPlacement, respawnNotBefore;
        public LootLifecycleState State => tracker?.State ?? LootLifecycleState.Cancelled;
        public double RemainingSeconds => tracker?.RemainingSeconds ?? 0;
        public string CarrierId => tracker?.CarrierId;
        public NetworkLootItem MapItem => mapItem;
        public IReadOnlyList<Vector3> LastCandidates { get; private set; } = Array.Empty<Vector3>();
        public IReadOnlyList<Vector3> LastPlayers { get; private set; } = Array.Empty<Vector3>();

        private void OnEnable()
        {
            manager = GetComponent<ArenaNetworkManager>(); spawner = GetComponent<ArenaLootSpawner>();
            lifecycle = GetComponent<ServerMatchLifecycleController>();
            spawner.ServerSpawnOverride = Request;
            manager.ServerDisconnected += Disconnected; manager.ServerStopped += Close;
        }
        private bool Request()
        {
            if (lifecycle.Session == null) return false; // Legacy direct Bootstrap keeps its original map loot.
            requested = true; return true;
        }
        private void Update()
        {
            if (!NetworkServer.active || !requested) return;
            var current = lifecycle.Session;
            if (current == null || current.Phase == MatchLifecyclePhase.Ended) { Cancel(); return; }
            var now = NetworkTime.localTime;
            if (current.Phase == MatchLifecyclePhase.InProgress && !current.Match.IsGameplayActive(now)) { Cancel(); return; }
            if (session != current)
            {
                Close(); requested = true; session = current;
                tracker = new LootRespawnTracker(current.Configuration.WeaponRespawnDelaySeconds); lastTick = now;
            }
            tracker.Tick(Math.Max(0, now - lastTick)); lastTick = now;
            if (State == LootLifecycleState.CarriedByPlayer) return;
            if (current.Phase != MatchLifecyclePhase.InProgress && current.Phase != MatchLifecyclePhase.Countdown) return;
            if (State == LootLifecycleState.RespawningAfterDeath && (!tracker.IsReady || now < respawnNotBefore || current.Phase != MatchLifecyclePhase.InProgress)) return;
            if (State == LootLifecycleState.Cancelled || mapItem != null || now < nextPlacement) return;
            nextPlacement = now + .5;
            if (!TrySelectPosition(random.Next(), out var position)) return;
            var item = Instantiate(spawner.LootPrefab, position, Quaternion.identity);
            NetworkServer.Spawn(item); mapItem = item.GetComponent<NetworkLootItem>();
            mapItem.SetServerClaim(Claim);
            if (tracker.IsReady) tracker.ConfirmSpawn();
        }
        private bool Claim(NetworkPlayer player)
        {
            var health = player.GetComponent<NetworkHealth>();
            if (session == null || lifecycle.Session != session || session.Phase != MatchLifecyclePhase.InProgress
                || health == null || !health.CanAct || !health.IsLifecycleBound || health.SessionId != session.SessionId
                || session.ByParticipant(health.ParticipantId)?.LifeState != ParticipantLifeState.Alive
                || !tracker.TryPickUp(health.ParticipantId)) return false;
            carrier = health; carrierConnection = player.connectionToClient;
            carrier.OnDeath += CarrierDied; return true;
        }
        private void CarrierDied() => LoseCarrier();
        private void Disconnected(NetworkConnectionToClient connection)
        { if (ReferenceEquals(carrierConnection, connection)) LoseCarrier(); }
        private void LoseCarrier()
        {
            if (tracker == null || State != LootLifecycleState.CarriedByPlayer) return;
            if (session == null || session.Phase == MatchLifecyclePhase.Ended) { Cancel(); return; }
            tracker.CarrierLost(tracker.CarrierId);
            // Start at the loss timestamp, not at the previous Update (which could shorten 30 s).
            lastTick = NetworkTime.localTime;
            respawnNotBefore = lastTick + tracker.DelaySeconds;
            ReleaseCarrier();
        }
        private void ReleaseCarrier()
        {
            if (carrier != null)
            {
                carrier.OnDeath -= CarrierDied;
                var combat = carrier.GetComponent<NetworkCombat>(); if (combat != null && combat.isServer) combat.ResetForLife();
            }
            carrier = null; carrierConnection = null;
        }
        private void Cancel()
        {
            tracker?.Cancel(); ReleaseCarrier();
            if (mapItem != null && NetworkServer.active) NetworkServer.Destroy(mapItem.gameObject);
            mapItem = null;
        }
        private void Close()
        {
            Cancel(); tracker = null; session = null; requested = false; nextPlacement = respawnNotBefore = 0;
            LastCandidates = Array.Empty<Vector3>();
            LastPlayers = Array.Empty<Vector3>();
        }
        [Server] public bool TrySelectPosition(int seed, out Vector3 position)
        {
            position = default;
            var floor = lifecycle.ArenaFloor;
            if (!NetworkServer.active || lifecycle.Session == null || floor == null || !floor.enabled) return false;
            Physics.SyncTransforms(); var bounds = floor.bounds;
            if (bounds.size.x <= WallMargin * 2 || bounds.size.z <= WallMargin * 2) return false;
            var players = NetworkServer.spawned.Values.Where(i => i != null).Select(i => i.GetComponent<NetworkHealth>())
                .Where(h => h != null && h.IsLifecycleBound && h.SessionId == lifecycle.Session.SessionId
                    && h.LifeState == ParticipantLifeState.Alive && !h.IsDead)
                .Select(h => new LootPoint(h.transform.position.x, h.transform.position.z)).ToArray();
            LastPlayers = players.Select(p => new Vector3((float)p.X, 0, (float)p.Z)).ToArray();
            var candidates = new List<Vector3>(); var rng = new System.Random(seed);
            for (var row = 0; row < 4; row++) for (var col = 0; col < 6; col++)
            {
                var x = bounds.min.x + WallMargin + (float)((col + .15 + .7 * rng.NextDouble()) / 6) * (bounds.size.x - 2 * WallMargin);
                var z = bounds.min.z + WallMargin + (float)((row + .15 + .7 * rng.NextDouble()) / 4) * (bounds.size.z - 2 * WallMargin);
                var ray = new Vector3(x, bounds.max.y + 10, z);
                if (!Physics.Raycast(ray, Vector3.down, out var hit, bounds.size.y + 11, 1 << 6, QueryTriggerInteraction.Ignore)
                    || hit.collider != floor || hit.normal.y < .9f) continue;
                var clearanceCenter = new Vector3(x, hit.point.y + Clearance + .04f, z);
                if (Physics.CheckBox(clearanceCenter, Vector3.one * Clearance, Quaternion.identity, (1 << 6) | (1 << 7), QueryTriggerInteraction.Ignore)) continue;
                if (players.Any(p => p.DistanceSquared(new LootPoint(x, z)) < MinimumPlayerDistance * MinimumPlayerDistance)) continue;
                candidates.Add(new Vector3(x, hit.point.y + .3f, z));
            }
            LastCandidates = candidates.ToArray();
            var index = FairLootSpawnSelector.SelectIndex(candidates.Select(p => new LootPoint(p.x, p.z)).ToArray(), players,
                candidates.Count == 0 ? 0 : rng.Next(candidates.Count));
            if (index < 0) return false;
            position = candidates[index]; return true;
        }
        private void OnDisable()
        {
            if (spawner != null) spawner.ServerSpawnOverride = null;
            if (manager != null) { manager.ServerDisconnected -= Disconnected; manager.ServerStopped -= Close; }
            Close();
        }
    }
}
