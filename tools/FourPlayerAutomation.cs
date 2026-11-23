#if SMALL_ARENA_FOUR_PLAYER_HARNESS
using System;
using System.IO;
using System.Linq;
using Mirror;
using SmallArena.Client.UI;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Networking.Verification;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Client.Verification
{
    // Files are the local controller protocol, not a replacement for Mirror Commands or replication.
    public sealed class FourPlayerAutomation : MonoBehaviour
    {
        [Serializable] private sealed class Command { public int sequence = 0; public string action = ""; public int slot = 0; public float moveX = 0, moveZ = 0; public float seconds = 0; }
        [Serializable] private sealed class Actor
        {
            public int slot, hp, damage; public long life; public uint netId; public float x, y, z;
            public string lifeState, result; public bool canAct;
        }
        [Serializable] private sealed class Score
        { public string participantId, lifeState; public int slot, rank, kills, deaths; public long damage, life; public bool connected; }
        [Serializable] private sealed class Member { public int ConnectionId, SlotIndex; public bool IsReady, IsHost; }
        [Serializable] private sealed class Snapshot
        {
            public string run, commit, tooling, unity, scene, room, phase, session, winner, fault;
            public int process, localSlot = -1, sequence, command, avatars, identities, lootCount, feedCount, serverConnections;
            public bool connected, server, moveActive;
            public double now, clock, lastDeathAt;
            public Member[] members;
            public Actor[] actors; public Score[] scores;
            public Vector3 lootPosition; public uint lootId;
        }
        private string directory, id, run;
        private ArenaNetworkManager manager;
        private int lastCommand, sample;
        private double nextSample, started, maxRuntime, moveUntil;
        private Vector2 move;
        private string fault;
        private bool entered;
        private static string Arg(string flag)
        {
            var args = Environment.GetCommandLineArgs(); var i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Arg("--four-player-harness") == null) return;
            var root = new GameObject("Four player opt-in verification"); DontDestroyOnLoad(root); root.AddComponent<FourPlayerAutomation>();
        }
        private void Awake()
        {
            id = Arg("--four-player-harness"); directory = Arg("--four-results"); run = Arg("--four-run");
            if (!int.TryParse(id, out var index) || index < 0 || index > 3 || string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(run))
                throw new ArgumentException("Invalid four-player harness identity/results flags.");
            maxRuntime = double.Parse(Arg("--four-max-runtime") ?? "900", System.Globalization.CultureInfo.InvariantCulture);
            if (maxRuntime < 30 || maxRuntime > 3600) throw new ArgumentOutOfRangeException(nameof(maxRuntime));
            Directory.CreateDirectory(directory); started = Time.realtimeSinceStartupAsDouble;
            Application.runInBackground = true; Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0;
            Application.logMessageReceived += Capture;
            Event("STARTED");
        }
        private void Capture(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) fault = message + "\n" + stack; }
        private void Event(string name) => File.AppendAllText(Path.Combine(directory, "events-" + id + ".jsonl"),
            "{\"event\":\"" + name + "\",\"sequence\":" + lastCommand + "}\n");
        private void Update()
        {
            try
            {
                if (Time.realtimeSinceStartupAsDouble - started > maxRuntime) { fault = "Infrastructure: worker deadline exceeded"; Publish(); Application.Quit(1); return; }
                if (manager == null) manager = NetworkManager.singleton as ArenaNetworkManager;
                if (!entered && SceneManager.GetActiveScene().name == "MainMenu" && manager != null)
                {
                    var view = FindAnyObjectByType<LobbyRoomView>(); var menu = FindAnyObjectByType<MainMenuController>();
                    if (view == null || menu == null) return;
                    menu.OnMultiplayerClicked();
                    if (!(manager.transport is PortTransport port)) throw new InvalidOperationException("KCP PortTransport required.");
                    port.Port = ushort.Parse(Arg("--four-port") ?? "7777");
                    if (id == "0") view.HostButton.onClick.Invoke();
                    else { view.Address.text = Arg("--four-address") ?? "127.0.0.1"; view.JoinButton.onClick.Invoke(); }
                    entered = true; Event("MENU_ENTRY");
                }
                if (NetworkClient.localPlayer != null)
                {
                    // Explicit instrumented input replaces device sampling only in this build/CLI session.
                    var controller = NetworkClient.localPlayer.GetComponent<PlayerController>(); if (controller != null) controller.enabled = false;
                    var movement = NetworkClient.localPlayer.GetComponent<NetworkMovement>();
                    if (moveUntil > 0)
                    {
                        movement.SubmitInput(Time.realtimeSinceStartupAsDouble < moveUntil ? move : Vector2.zero, false);
                        if (Time.realtimeSinceStartupAsDouble >= moveUntil) moveUntil = 0;
                    }
                }
                // Immutable sequence files avoid File.Replace sharing/rename races across runtimes.
                var path = Path.Combine(directory, "command-" + id + "-" + (lastCommand + 1) + ".json");
                if (File.Exists(path))
                {
                    Command command;
                    using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(file)) command = JsonUtility.FromJson<Command>(reader.ReadToEnd());
                    if (command.sequence > lastCommand) { lastCommand = command.sequence; Execute(command); Event(command.action.ToUpperInvariant()); }
                }
                if (Time.realtimeSinceStartupAsDouble >= nextSample) { nextSample = Time.realtimeSinceStartupAsDouble + .2; Publish(); }
            }
            catch (Exception error)
            {
                fault = (error is IOException ? "Infrastructure: " : "Product: ") + error;
                // Never recursively try to write the same failed artifact from an exception path.
                Console.Error.WriteLine("[FOUR_PLAYER_FAILURE] " + fault); Application.Quit(1);
            }
        }
        private void Execute(Command command)
        {
            var local = NetworkClient.localPlayer;
            switch (command.action)
            {
                case "ready": FindAnyObjectByType<LobbyRoomView>().ReadyButton.onClick.Invoke(); break;
                case "start": FindAnyObjectByType<LobbyRoomView>().StartButton.onClick.Invoke(); break;
                case "arrange": FourPlayerHarnessBridge.Arrange(0, command.slot, false); break;
                case "loot": FourPlayerHarnessBridge.Arrange(0, command.slot, true); break;
                case "pickup":
                    var item = NetworkClient.spawned.Values.Select(i => i.GetComponent<NetworkLootItem>()).First(l => l != null);
                    local.GetComponent<NetworkPlayer>().RequestPickup(item.netId); break;
                case "attack": local.GetComponent<NetworkPlayer>().RequestAttack(Vector3.forward); break;
                case "move": move = new Vector2(command.moveX, command.moveZ); moveUntil = Time.realtimeSinceStartupAsDouble + command.seconds; break;
                case "leave":
                    var menu = FindAnyObjectByType<InGameMenuController>();
                    if (menu != null) menu.LeaveMatch(); else manager.LeaveRoom();
                    break;
                case "shutdown": Publish(); Event("CLEAN_SHUTDOWN"); Application.Quit(0); break;
                default: throw new InvalidOperationException("Unknown harness command " + command.action);
            }
        }
        private void Publish()
        {
            var state = NetworkClient.spawned.Values.Where(i => i != null).Select(i => i.GetComponent<NetworkMatchController>()).FirstOrDefault(s => s != null);
            var identities = NetworkClient.spawned.Values.Where(i => i != null).ToArray();
            var loot = identities.Select(i => i.GetComponent<NetworkLootItem>()).Where(l => l != null).ToArray();
            var members = manager != null ? manager.CurrentRoom.Members : null;
            var localConnection = manager != null ? manager.CurrentRoom.LocalConnectionId : -1;
            var localSlot = members == null ? -1 : members.Where(m => m.ConnectionId == localConnection).Select(m => m.SlotIndex).DefaultIfEmpty(-1).First();
            var snapshot = new Snapshot { run = run, commit = FourPlayerBuildIdentity.Commit, tooling = FourPlayerBuildIdentity.Tooling,
                unity = Application.unityVersion, process = System.Diagnostics.Process.GetCurrentProcess().Id,
                scene = SceneManager.GetActiveScene().name, room = manager != null ? manager.CurrentRoom.Phase.ToString() : "Offline",
                phase = state != null ? state.Phase.ToString() : "None", session = state != null ? state.SessionId : "",
                winner = state != null ? state.WinnerParticipantId : "", fault = fault ?? "", sequence = ++sample, command = lastCommand,
                connected = NetworkClient.isConnected, server = NetworkServer.active,
                members = members == null ? Array.Empty<Member>() : members.Select(m => new Member { ConnectionId = m.ConnectionId,
                    SlotIndex = m.SlotIndex, IsReady = m.IsReady, IsHost = m.IsHost }).ToArray(),
                localSlot = localSlot, now = NetworkTime.time,
                clock = state != null ? state.DisplayClock : 0, moveActive = moveUntil > 0,
                lastDeathAt = state != null && state.Feed.Count > 0 ? state.Feed[state.Feed.Count - 1].ServerTime : 0,
                identities = identities.Length, lootCount = loot.Length, lootId = loot.Length > 0 ? loot[0].netId : 0,
                lootPosition = loot.Length > 0 ? loot[0].transform.position : Vector3.zero,
                serverConnections = NetworkServer.active ? NetworkServer.connections.Count : 0,
                feedCount = state != null ? state.Feed.Count : 0,
                scores = state == null ? Array.Empty<Score>() : state.Scores.Select(s => new Score { participantId = s.ParticipantId,
                    slot = s.SlotIndex, rank = s.Rank, kills = s.Kills, deaths = s.Deaths, damage = s.DamageDealt,
                    life = s.LifeId, lifeState = s.LifeState.ToString(), connected = s.IsConnected }).ToArray(),
                actors = identities.Select(i => i.GetComponent<NetworkHealth>()).Where(h => h != null).OrderBy(h => h.SlotIndex)
                    .Select(h => new Actor { slot = h.SlotIndex, hp = h.CurrentHealth, damage = h.GetComponent<NetworkCombat>().CurrentDamage,
                        life = h.LifeId, netId = h.netId, x = h.transform.position.x, y = h.transform.position.y, z = h.transform.position.z,
                        lifeState = h.LifeState.ToString(), result = h.Result.ToString(), canAct = h.CanAct }).ToArray() };
            snapshot.avatars = snapshot.actors.Length;
            var segment = (snapshot.sequence - 1) / 250;
            File.AppendAllText(Path.Combine(directory, "stream-" + id + "-" + segment + ".jsonl"), JsonUtility.ToJson(snapshot) + "\n");
        }
        private void OnDestroy() { Application.logMessageReceived -= Capture; }
    }
}
#endif
