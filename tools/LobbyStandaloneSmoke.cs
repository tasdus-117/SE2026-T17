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
    public sealed class LobbyStandaloneSmoke : MonoBehaviour
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
            public uint localPlayer;
            public string[] errors;
        }
        [Serializable] private sealed class TeardownResult
        {
            public string role;
            public string scenario;
            public bool success;
            public bool hostSurvivedClientLeave;
            public string scene;
            public bool networkStopped;
            public bool freshManager;
            public bool noIdentities;
            public bool menuCursor;
            public string[] errors;
        }
        private readonly List<string> errors = new List<string>();
        private string role;
        private string directory;
        private bool opened;
        private ArenaNetworkManager manager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Argument("--lobby-smoke") == null) return;
            var root = new GameObject("Lobby standalone verification");
            DontDestroyOnLoad(root);
            root.AddComponent<LobbyStandaloneSmoke>();
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
            while ((SceneManager.GetActiveScene().name != "Arena_MVP" || NetworkClient.localPlayer == null || NetworkClient.spawned.Count != 2)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Finish(opened && manager.CurrentRoom.Phase == LobbyPhase.Playing && NetworkClient.spawned.Count == 2 && NetworkClient.localPlayer != null && errors.Count == 0);
            if (Argument("--menu-teardown") != null) yield return Teardown();
            yield return new WaitForSecondsRealtime(2);
            Application.Quit(errors.Count == 0 ? 0 : 1);
        }
        private IEnumerator Teardown()
        {
            var scenario = Argument("--menu-teardown");
            var deadline = Time.realtimeSinceStartup + 45;
            var other = role == "host" ? "client" : "host";
            while (!File.Exists(Path.Combine(directory, other + ".json")) && Time.realtimeSinceStartup < deadline) yield return null;
            var menu = FindAnyObjectByType<InGameMenuController>();
            var oldManager = manager;
            var survived = false;
            menu.ShowMatchResult(role == "host");
            if (role == scenario) menu.GetComponent<InGameMenuView>().ResultReturnButton.onClick.Invoke();
            else if (role == "host")
            {
                while (!File.Exists(Path.Combine(directory, "client.menu.json")) && Time.realtimeSinceStartup < deadline) yield return null;
                survived = NetworkServer.active && NetworkClient.activeHost && NetworkServer.connections.Count == 1 && NetworkServer.spawned.Count == 1;
                menu.LeaveMatch();
            }
            // In the host-leave case the client does not click Leave: transport disconnect must navigate it.
            while ((SceneManager.GetActiveScene().name != "MainMenu" || MatchSessionNavigation.IsReturning) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            var fresh = NetworkManager.singleton as ArenaNetworkManager;
            var stopped = !NetworkServer.active && !NetworkClient.active && NetworkClient.connection == null && NetworkClient.localPlayer == null;
            var clean = NetworkServer.spawned.Count == 0 && NetworkClient.spawned.Count == 0 && FindObjectsByType<NetworkIdentity>().Length == 0;
            var freshRoom = oldManager == null && fresh != null && fresh.RoomEnabled && fresh.CurrentRoom.Phase == LobbyPhase.Offline && FindObjectsByType<ArenaNetworkManager>().Length == 1;
            var cursor = Cursor.lockState == CursorLockMode.None && Cursor.visible;
            var success = SceneManager.GetActiveScene().name == "MainMenu" && stopped && clean && freshRoom && cursor && errors.Count == 0 && (role != "host" || scenario != "client" || survived);
            File.WriteAllText(Path.Combine(directory, role + ".menu.json"), JsonUtility.ToJson(new TeardownResult
            {
                role = role, scenario = scenario, success = success, hostSurvivedClientLeave = survived,
                scene = SceneManager.GetActiveScene().name, networkStopped = stopped, freshManager = freshRoom,
                noIdentities = clean, menuCursor = cursor, errors = errors.ToArray()
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
                visiblePlayers = NetworkClient.spawned.Count, localPlayer = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0,
                errors = errors.ToArray()
            }, true));
            if (!success) Application.Quit(1);
        }
        private void OnDestroy() => Application.logMessageReceived -= Capture;
    }
}
#endif
