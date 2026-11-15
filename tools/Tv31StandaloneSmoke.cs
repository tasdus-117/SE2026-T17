// Copied only into the isolated validation project's Client assembly for the instrumented smoke build.
#if SMALL_ARENA_M2_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using SmallArena.Client.UI;
using SmallArena.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallArena.Client.Verification
{
    public sealed class Tv31StandaloneSmoke : MonoBehaviour
    {
        [Serializable] private sealed class Result
        {
            public string role;
            public string unity;
            public string scene;
            public bool success;
            public bool openedFromMenu;
            public bool noDebugHud;
            public int roomMembers;
            public int visiblePlayers;
            public int visibleLoot;
            public uint localPlayer;
            public string[] errors;
            public string layout;
            public bool collidersMatch;
        }
        [Serializable] private sealed class LayoutReport { public ArenaObstacle[] obstacles; }
        [Serializable] private sealed class FallReport
        {
            public string role;
            public string faller;
            public bool success;
            public int victimHealth;
            public string victimResult;
            public string localResult;
            public string resultHeader;
            public int serverVictimHealth;
            public string[] errors;
        }
        private bool CollidersMatch()
        {
            var layout = FindAnyObjectByType<ArenaObstacles>();
            if (layout == null || layout.Layout.Count < 3 || layout.Layout.Count > 6) return false;
            Physics.SyncTransforms();
            var colliders = layout.GetComponentsInChildren<Collider>();
            if (colliders.Length != layout.Layout.Count) return false;
            for (var i = 0; i < colliders.Length; i++)
            {
                var expected = layout.Layout[i];
                var actual = colliders[i];
                var shape = actual is BoxCollider ? PrimitiveType.Cube : actual is SphereCollider ? PrimitiveType.Sphere : PrimitiveType.Capsule;
                if (shape != expected.Shape || !actual.enabled || actual.isTrigger
                    || Vector3.Distance(actual.bounds.center, expected.Position) > 0.002f
                    || Vector3.Distance(actual.bounds.size, expected.Size) > 0.002f) return false;
            }
            return true;
        }
        private readonly List<string> errors = new List<string>();
        private string role;
        private string directory;
        private bool opened;
        private ArenaNetworkManager manager;
        private static int VisiblePlayers => NetworkClient.spawned.Values.Count(identity => identity != null && identity.GetComponent<NetworkPlayer>() != null);
        private static int VisibleLoot => NetworkClient.spawned.Values.Count(identity => identity != null && identity.GetComponent<NetworkLootItem>() != null);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Argument("--lobby-smoke") == null) return;
            var root = new GameObject("Lobby standalone verification");
            DontDestroyOnLoad(root);
            root.AddComponent<Tv31StandaloneSmoke>();
        }
        private static string Argument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++) if (arguments[index] == name) return arguments[index + 1];
            return null;
        }
        private void Awake()
        {
            role = Argument("--lobby-smoke");
            directory = Argument("--lobby-results");
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Application.logMessageReceived += Capture;
        }
        private void Capture(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
        }
        private IEnumerator Start()
        {
            var deadline = Time.realtimeSinceStartup + 60;
            while (FindAnyObjectByType<LobbyRoomView>() == null && Time.realtimeSinceStartup < deadline) yield return null;
            var view = FindAnyObjectByType<LobbyRoomView>();
            manager = NetworkManager.singleton as ArenaNetworkManager;
            if (view == null || manager == null) { Finish(false); yield break; }
            var button = FindObjectsByType<Button>().Single(item => item.name == "MultiplayerButton");
            button.onClick.Invoke();
            opened = view.ConnectionVisible;
            if (manager.transport is PortTransport port && ushort.TryParse(Argument("--lobby-port"), out var selected)) port.Port = selected;
            if (role == "host") view.HostButton.onClick.Invoke();
            else { view.Address.text = "127.0.0.1"; view.JoinButton.onClick.Invoke(); }
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, role + ".started"), "Menu button invoked");
            while (manager.CurrentRoom.Members?.Length != 2 && Time.realtimeSinceStartup < deadline) yield return null;
            if (manager.CurrentRoom.Members?.Length != 2) { Finish(false); yield break; }
            view.ReadyButton.onClick.Invoke();
            if (role == "host")
            {
                while (!view.StartButton.interactable && Time.realtimeSinceStartup < deadline) yield return null;
                if (!view.StartButton.interactable) { Finish(false); yield break; }
                view.StartButton.onClick.Invoke();
            }
            while ((SceneManager.GetActiveScene().name != "Arena_MVP" || NetworkClient.localPlayer == null || VisiblePlayers != 2 || VisibleLoot != 1)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Finish(opened && manager.CurrentRoom.Phase == LobbyPhase.Playing && VisiblePlayers == 2 && VisibleLoot == 1 && NetworkClient.localPlayer != null && errors.Count == 0);
            yield return VerifyFall();
            yield return new WaitForSecondsRealtime(2);
            Application.Quit(errors.Count == 0 ? 0 : 1);
        }
        private IEnumerator VerifyFall()
        {
            var faller = Argument("--arena-faller") ?? "client";
            var deadline = Time.realtimeSinceStartup + 30;
            var other = role == "host" ? "client" : "host";
            while (!File.Exists(Path.Combine(directory, other + ".json")) && Time.realtimeSinceStartup < deadline) yield return null;
            uint victimId = 0;
            if (role == "host")
            {
                var victim = NetworkServer.spawned.Values.Single(identity => identity.GetComponent<NetworkPlayer>() != null
                    && (faller == "host" ? identity.isLocalPlayer : !identity.isLocalPlayer));
                victimId = victim.netId;
                victim.GetComponent<NetworkHealth>().TakeDamage(3);
                var controller = victim.GetComponent<CharacterController>();
                controller.enabled = false;
                victim.transform.position = new Vector3(9, 1, 0);
                controller.enabled = true;
                Physics.SyncTransforms();
                File.WriteAllText(Path.Combine(directory, "victim.id"), victimId.ToString());
            }
            while (!File.Exists(Path.Combine(directory, "victim.id")) && Time.realtimeSinceStartup < deadline) yield return null;
            victimId = uint.Parse(File.ReadAllText(Path.Combine(directory, "victim.id")));
            var replica = NetworkClient.spawned[victimId].GetComponent<NetworkHealth>();
            var local = NetworkClient.localPlayer.GetComponent<NetworkHealth>();
            var view = FindAnyObjectByType<InGameMenuView>();
            var expected = role == faller ? PlayerMatchResult.Defeat : PlayerMatchResult.Victory;
            while ((replica.CurrentHealth != 0 || local.Result != expected || !view.ResultVisible) && Time.realtimeSinceStartup < deadline) yield return null;
            var authoritative = role == "host" ? NetworkServer.spawned[victimId].GetComponent<NetworkHealth>().CurrentHealth : -1;
            var success = replica.CurrentHealth == 0 && replica.Result == PlayerMatchResult.Defeat
                && local.Result == expected && view.ResultVisible && view.ResultHeader == (role == faller ? "DEFEAT" : "VICTORY")
                && (role != "host" || authoritative == 0) && errors.Count == 0;
            File.WriteAllText(Path.Combine(directory, role + ".fall.json"), JsonUtility.ToJson(new FallReport
            {
                role = role, faller = faller, success = success, victimHealth = replica.CurrentHealth,
                victimResult = replica.Result.ToString(), localResult = local.Result.ToString(), resultHeader = view.ResultHeader,
                serverVictimHealth = authoritative, errors = errors.ToArray()
            }, true));
            if (!success) Application.Quit(1);
        }
        private void Finish(bool success)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, role + ".json"), JsonUtility.ToJson(new Result
            {
                role = role, unity = Application.unityVersion, scene = SceneManager.GetActiveScene().name,
                success = success, openedFromMenu = opened, noDebugHud = FindAnyObjectByType<NetworkManagerHUD>() == null,
                roomMembers = manager != null ? manager.CurrentRoom.Members?.Length ?? 0 : 0,
                visiblePlayers = VisiblePlayers, visibleLoot = VisibleLoot, localPlayer = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0,
                errors = errors.ToArray(),
                layout = JsonUtility.ToJson(new LayoutReport { obstacles = FindAnyObjectByType<ArenaObstacles>().Layout.ToArray() }),
                collidersMatch = CollidersMatch()
            }, true));
            if (!success) Application.Quit(1);
        }
        private void OnDestroy() => Application.logMessageReceived -= Capture;
    }
}
#endif
