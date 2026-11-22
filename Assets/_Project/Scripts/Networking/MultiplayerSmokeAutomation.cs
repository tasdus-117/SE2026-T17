using System;
using System.Linq;
using System.Collections;
using Mirror;
using SmallArena.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Networking
{
    // No object, transport changes or handlers exist unless an explicit auto-role is supplied.
    [DefaultExecutionOrder(-1000)]
    public sealed class MultiplayerSmokeAutomation : MonoBehaviour
    {
        public struct CombatAck : NetworkMessage { public uint Actor; public uint Victim; }
        public struct CombatComplete : NetworkMessage { }
        public struct Stage : NetworkMessage { public Vector3 Origin; public uint Actor; public uint Victim; }
        private string[] arguments;
        private ArenaNetworkManager manager;
        private bool host, readySent, startSent, connected, spawned, countdown, playing, staged, moved, attacked, verified, acknowledged, finishing, failed;
        private int expected, port, maximum;
        private string address, action, peerParticipant;
        private uint peerId, victimId;
        private int disconnected = -1;
        private Vector3 origin;
        private double began, stageTime, initialClock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("--auto-host") && !args.Contains("--auto-join")) return;
            var instance = new GameObject("Explicit CLI multiplayer automation");
            DontDestroyOnLoad(instance);
            instance.AddComponent<MultiplayerSmokeAutomation>();
        }
        private string Value(string name, string fallback = null)
        {
            var index = Array.IndexOf(arguments, name);
            if (index < 0) return fallback;
            if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("Missing value for " + name);
            return arguments[index + 1];
        }
        private void Awake()
        {
            began = Time.realtimeSinceStartupAsDouble;
            arguments = Environment.GetCommandLineArgs();
            Application.logMessageReceived += Capture;
            try
            {
                host = arguments.Contains("--auto-host");
                address = Value("--auto-join");
                if (host == (address != null)) throw new ArgumentException("Supply exactly one auto-role.");
                expected = int.Parse(Value("--expected-players", "2"));
                port = int.Parse(Value("--port", "7777"));
                maximum = int.Parse(Value("--max-runtime-seconds", "90"));
                action = Value("--smoke-action", "none");
                if (expected < 2 || expected > 4 || port < 1 || port > 65535 || maximum < 5 || maximum > 300
                    || action != "none" && action != "hold" && action != "combat-duel"
                    || action == "combat-duel" && expected != 2) throw new ArgumentException("Invalid CLI automation options.");
                Application.runInBackground = true;
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
                Emit("GAME_ENTRY", "scene=" + SceneManager.GetActiveScene().name);
            }
            catch (Exception error) { Fail(error.Message); }
        }
        private void Capture(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            failed = true;
            if (!finishing) Fail(type + ": " + message);
        }
        private void Emit(string name, string detail = "")
        {
            var line = "[SMOKE_EVENT] " + name + " role=" + (host ? "host" : "client") + " " + detail;
            Debug.Log(line);
            Console.WriteLine(line);
        }
        private void Fail(string reason)
        {
            if (finishing) return;
            failed = true; finishing = true;
            Emit("FAILURE", reason.Replace('\n', ' ').Replace('\r', ' '));
            if (manager != null) manager.LeaveRoom();
            Application.Quit(1);
        }
        private void Update()
        {
            if (Time.realtimeSinceStartupAsDouble - began > maximum) { Fail("max-runtime deadline"); return; }
            if (finishing) return;
            try { Tick(); } catch (Exception error) { Fail(error.ToString()); }
        }
        private void Tick()
        {
            if (manager == null)
            {
                manager = NetworkManager.singleton as ArenaNetworkManager;
                if (manager == null) return;
                if (!manager.RoomEnabled || !(manager.transport is PortTransport transport) || manager.transport.GetType().FullName != "kcp2k.KcpTransport")
                    throw new InvalidOperationException("CLI automation requires the production MainMenu KCP room root.");
                transport.Port = (ushort)port;
                manager.ServerDisconnected += OnPeerDisconnected;
                if (host)
                {
                    manager.HostRoom();
                    NetworkServer.RegisterHandler<CombatAck>(OnCombatAck);
                    if (!NetworkServer.active) throw new InvalidOperationException("Host did not start.");
                    Emit("HOST_STARTED", "transport=KCP port=" + port);
                }
                else manager.JoinRoom(address);
                NetworkClient.RegisterHandler<Stage>(message => { origin = message.Origin; peerId = message.Actor; victimId = message.Victim; staged = true; stageTime = Time.realtimeSinceStartupAsDouble; });
                NetworkClient.RegisterHandler<CombatComplete>(_ => { if (!verified) Fail("Premature combat completion"); else StartCoroutine(Shutdown()); });
            }
            if (!connected && NetworkClient.isConnected) { connected = true; Emit("CLIENT_CONNECTED", "transport=KCP"); }
            if (connected && !NetworkClient.isConnected) throw new InvalidOperationException("Unexpected local disconnect.");
            var room = manager.CurrentRoom;
            if (room.Members != null && room.Members.Any(member => member.ConnectionId == room.LocalConnectionId))
            {
                if (!readySent && arguments.Contains("--auto-ready")) { readySent = true; manager.RequestReady(true); }
                if (readySent && room.Members.First(member => member.ConnectionId == room.LocalConnectionId).IsReady && !readyMarked)
                { readyMarked = true; Emit("LOBBY_READY", "members=" + room.Members.Length); }
                if (host && !startSent && arguments.Contains("--auto-start") && room.Members.Length == expected && room.CanStart)
                { startSent = true; manager.RequestRoomStart(); }
            }
            var players = NetworkClient.spawned.Values.Where(identity => identity != null && identity.GetComponent<NetworkPlayer>() != null).ToArray();
            var match = FindAnyObjectByType<NetworkMatchController>();
            if (!spawned && NetworkClient.localPlayer != null && players.Length == expected)
            { spawned = true; Emit("AVATARS_SPAWNED", "count=" + players.Length); }
            if (match == null) return;
            if (!countdown && match.Phase == MatchLifecyclePhase.Countdown) { countdown = true; Emit("MATCH_COUNTDOWN"); }
            if (!playing && spawned && match.Phase == MatchLifecyclePhase.InProgress)
            { playing = true; initialClock = match.MatchClock; Emit("MATCH_IN_PROGRESS", "scene=" + SceneManager.GetActiveScene().name); }
            if (!playing) return;
            if (host && disconnected >= 0)
            {
                var row = match.Scores.FirstOrDefault(score => score.ParticipantId == peerParticipant);
                if (!NetworkServer.connections.ContainsKey(disconnected) && !NetworkServer.spawned.ContainsKey(peerId)
                    && !NetworkClient.spawned.ContainsKey(peerId) && !row.IsConnected && row.LifeState == ParticipantLifeState.Disconnected
                    && players.Length == expected - 1)
                {
                    Emit("PEER_DISCONNECTED", "orphan=0 remaining=" + players.Length);
                    if (match.Phase != MatchLifecyclePhase.InProgress || match.MatchClock >= initialClock
                        || NetworkClient.localPlayer == null || !NetworkClient.localPlayer.GetComponent<NetworkHealth>().CanAct)
                        throw new InvalidOperationException("Remaining host match stopped progressing.");
                    Emit("SERVER_RESILIENT", "clock=ticking control=allowed");
                    if (action == "hold" || acknowledged) StartCoroutine(Shutdown());
                }
                return;
            }
            if (host && action == "hold" && peerId == 0)
            {
                var peer = players.First(identity => !identity.isLocalPlayer);
                peerId = peer.netId; peerParticipant = peer.GetComponent<NetworkHealth>().ParticipantId;
            }
            if (action != "combat-duel") return;
            if (host && !staged) StageDuel();
            if (!host && staged && NetworkClient.localPlayer != null)
            {
                var local = NetworkClient.localPlayer;
                if (local.netId != peerId) throw new InvalidOperationException("Staged actor is not local owner.");
                if (HorizontalDistance(local.transform.position, origin) < .06f && Mathf.Abs(local.transform.position.y - origin.y) < .2f && !movementBegan) movementBegan = true;
                if (movementBegan && !moved)
                {
                    local.GetComponent<NetworkMovement>().SubmitInput(Vector2.up, false);
                    if (HorizontalDistance(local.transform.position, origin) > .08f) { moved = true; Emit("MOVEMENT_VERIFIED", "replicated=1"); }
                }
                else local.GetComponent<NetworkMovement>().SubmitInput(Vector2.zero, false);
                if (moved && !attacked && Time.realtimeSinceStartupAsDouble - stageTime > 1 && local.GetComponent<NetworkHealth>().CanAct)
                { attacked = true; local.GetComponent<NetworkPlayer>().RequestAttack(Vector3.forward); }
            }
            if (host && staged && !moved && NetworkServer.spawned.TryGetValue(peerId, out var actor) && HorizontalDistance(actor.transform.position, origin) > .08f)
            { moved = true; Emit("MOVEMENT_VERIFIED", "authoritative=1"); }
            if (!verified && CombatVerified(match))
            {
                verified = true; Emit("COMBAT_VERIFIED", "hp=9 damage=1");
                if (!host) NetworkClient.Send(new CombatAck { Actor = peerId, Victim = victimId });
            }
        }
        private bool readyMarked, movementBegan;
        private static float HorizontalDistance(Vector3 first, Vector3 second) => new Vector2(first.x - second.x, first.z - second.z).magnitude;
        private bool CombatVerified(NetworkMatchController match)
        {
            var registry = host ? NetworkServer.spawned : NetworkClient.spawned;
            return staged && registry.TryGetValue(victimId, out var victim) && victim.GetComponent<NetworkHealth>().CurrentHealth == 9
                && registry.TryGetValue(peerId, out var actor) && actor.GetComponent<NetworkHealth>().CurrentHealth == 10
                && match.Scores.Any(score => score.ParticipantId == actor.GetComponent<NetworkHealth>().ParticipantId && score.DamageDealt == 1);
        }
        private void StageDuel()
        {
            var actors = NetworkServer.spawned.Values.Where(identity => identity != null && identity.GetComponent<NetworkPlayer>() != null).ToArray();
            if (actors.Length != 2 || actors.Any(identity => !identity.GetComponent<NetworkHealth>().CanAct)) return;
            var floor = FindAnyObjectByType<ArenaObstacles>().FloorCollider;
            for (var x = -5; x <= 5; x += 2)
                for (var z = -5; z <= 5; z += 2)
                {
                    var start = new Vector3(x, floor.bounds.max.y + 1.05f, z);
                    var end = start + Vector3.forward * 1.2f;
                    if (!floor.bounds.Contains(new Vector3(x, floor.bounds.center.y, z))
                        || Physics.CheckCapsule(start + Vector3.down * .45f, start + Vector3.up * .45f, .55f, 1 << 6)
                        || Physics.CheckCapsule(end + Vector3.down * .45f, end + Vector3.up * .45f, .55f, 1 << 6)) continue;
                    var peer = actors.First(identity => !identity.isLocalPlayer);
                    var victim = actors.First(identity => identity.isLocalPlayer);
                    peerId = peer.netId; victimId = victim.netId; origin = start;
                    peerParticipant = peer.GetComponent<NetworkHealth>().ParticipantId;
                    peer.GetComponent<ArenaNetworkTransform>().ServerTeleport(start, Quaternion.identity);
                    victim.GetComponent<ArenaNetworkTransform>().ServerTeleport(end, Quaternion.identity);
                    staged = true;
                    peer.connectionToClient.Send(new Stage { Origin = start, Actor = peerId, Victim = victimId });
                    return;
                }
            throw new InvalidOperationException("No obstacle-free duel staging pair.");
        }
        private void OnCombatAck(NetworkConnectionToClient connection, CombatAck message)
        {
            var match = FindAnyObjectByType<NetworkMatchController>();
            if (connection.identity == null || connection.identity.netId != peerId || message.Actor != peerId
                || message.Victim != victimId || match == null || !CombatVerified(match)) { Fail("Invalid combat acknowledgement"); return; }
            if (!verified) { verified = true; Emit("COMBAT_VERIFIED", "hp=9 damage=1 authoritative=1"); }
            acknowledged = true;
            connection.Send(new CombatComplete());
        }
        private void OnPeerDisconnected(NetworkConnectionToClient connection)
        { if (connection.connectionId != 0 && peerId != 0) disconnected = connection.connectionId; }
        private IEnumerator Shutdown()
        {
            if (finishing) yield break;
            finishing = true;
            manager.LeaveRoom();
            yield return null;
            if (NetworkServer.active || NetworkClient.active || NetworkClient.localPlayer != null) failed = true;
            if (!failed) Emit("CLEAN_SHUTDOWN", "network=inactive");
            Application.Quit(failed ? 1 : 0);
        }
        private void OnDestroy()
        {
            Application.logMessageReceived -= Capture;
            if (manager != null) manager.ServerDisconnected -= OnPeerDisconnected;
        }
    }
}
