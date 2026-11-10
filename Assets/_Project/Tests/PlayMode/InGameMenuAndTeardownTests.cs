#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Mirror;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Core;
using SmallArena.Input;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class InGameMenuAndTeardownTests : InputTestFixture
    {
        private Keyboard keyboard;
        private ArenaNetworkManager manager;
        private M1LoopbackTransport transport;
        private GameObject independentServer;
        private InGameMenuController menu;
        private InGameMenuView view;
        private PlayerInputHandler input;
        private float captureDelta;
        private CursorLockMode cursorLock;
        private bool cursorVisible;

        public override void Setup()
        {
            base.Setup();
            captureDelta = Time.captureDeltaTime;
            cursorLock = Cursor.lockState;
            cursorVisible = Cursor.visible;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            yield return Until(() => !MatchSessionNavigation.IsReturning);
            var current = NetworkManager.singleton;
            if (current != null)
            {
                current.offlineScene = current.onlineScene = "";
                if (NetworkClient.activeHost && NetworkServer.active) current.StopHost();
                else if (NetworkClient.active || NetworkClient.connection != null) current.StopClient();
            }
            if (transport != null) transport.Pump();
            if (NetworkServer.active) NetworkServer.Shutdown();
            NetworkClient.Shutdown();
            if (current != null) Object.Destroy(current.gameObject);
            if (independentServer != null) Object.Destroy(independentServer);
            yield return null;
            NetworkManager.ResetStatics();
            Transport.active = null;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Match menu cleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            Time.captureDeltaTime = captureDelta;
            Cursor.lockState = cursorLock;
            Cursor.visible = cursorVisible;
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 20;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for scene/network teardown.");
        }

        private void FindMenu()
        {
            menu = Object.FindAnyObjectByType<InGameMenuController>();
            Assert.That(menu, Is.Not.Null, "Arena must contain its independent, wired menu prefab.");
            view = menu.GetComponent<InGameMenuView>();
            input = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<PlayerInputHandler>()
                : GameObject.Find("Player").GetComponent<PlayerInputHandler>();
        }
        private IEnumerator OfflineArena()
        {
            yield return SceneManager.LoadSceneAsync("Arena_MVP");
            yield return null;
            FindMenu();
        }
        private void Escape()
        {
            Press(keyboard.escapeKey);
            InputSystem.Update(); // UnityTest fixture Press/Release queue events until the next input update.
            Release(keyboard.escapeKey);
            InputSystem.Update();
        }
        private static void AssertMenuCursor()
        {
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
        }

        [UnityTest]
        public IEnumerator Escape_TogglesMenuCursorAndLocalInput_WithoutPausingServerTime()
        {
            yield return OfflineArena();
            Assert.That(view.PauseVisible || view.ResultVisible, Is.False);
            var timeScale = Time.timeScale;
            Escape();
            Assert.That(view.PauseVisible && input.IsMenuInputBlocked, Is.True);
            AssertMenuCursor();
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            Escape();
            Assert.That(view.PauseVisible || input.IsMenuInputBlocked, Is.False);
            // A -nographics player has no native cursor to lock (the getter remains None).
            // Interactive Test Runner runs must still verify the physical cursor lock.
            if (!Application.isBatchMode) Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(Cursor.visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Pause_ClearsGameplayIntent_ResumeDoesNotReplayHeldButtons()
        {
            yield return OfflineArena();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space, Key.J, Key.E));
            InputSystem.Update();
            Escape();
            Assert.That(input.MoveInput, Is.EqualTo(Vector2.zero));
            Assert.That(input.ConsumeJumpPressed() || input.ConsumePunchPressed() || input.ConsumeInteractPressed(), Is.False);
            view.ResumeButton.onClick.Invoke();
            InputSystem.Update();
            Assert.That(input.MoveInput.y, Is.EqualTo(1));
            Assert.That(input.ConsumeJumpPressed() || input.ConsumePunchPressed() || input.ConsumeInteractPressed(), Is.False);
            Release(keyboard.spaceKey); InputSystem.Update();
            Press(keyboard.spaceKey); InputSystem.Update();
            Assert.That(input.ConsumeJumpPressed(), Is.True, "A new press after resume is accepted.");
        }

        [UnityTest]
        public IEnumerator ResultHook_ShowsVictoryAndDefeat_WithoutChangingMatchRules()
        {
            yield return OfflineArena();
            var match = Object.FindAnyObjectByType<GameManager>();
            var state = match.CurrentState;
            menu.ShowMatchResult(true);
            Assert.That(view.ResultVisible && input.IsMenuInputBlocked, Is.True);
            Assert.That(view.ResultHeader, Is.EqualTo("VICTORY"));
            AssertMenuCursor();
            Escape(); menu.Resume();
            Assert.That(view.ResultVisible && !view.PauseVisible, Is.True);
            menu.ShowMatchResult(false);
            Assert.That(view.ResultHeader, Is.EqualTo("DEFEAT"));
            Assert.That(match.CurrentState, Is.EqualTo(state));
        }

        [UnityTest]
        public IEnumerator DisableAndReenable_DoesNotDuplicateEscapeListeners()
        {
            yield return OfflineArena();
            Escape();
            menu.enabled = false;
            Assert.That(input.IsMenuInputBlocked, Is.False);
            menu.enabled = true;
            Assert.That(input.IsMenuInputBlocked, Is.True);
            Escape();
            Assert.That(view.PauseVisible || input.IsMenuInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator OfflineLeave_UsesSharedNavigation_AndLoadsFreshMenu()
        {
            yield return OfflineArena();
            Escape();
            view.LeaveButton.onClick.Invoke();
            yield return Returned();
            AssertFreshMenu();
        }

        [UnityTest]
        public IEnumerator ResultReturn_RepeatedRequests_LoadMainMenuExactlyOnce()
        {
            yield return OfflineArena();
            var loads = 0;
            void Loaded(Scene scene, LoadSceneMode mode) { if (scene.name == "MainMenu") loads++; }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                menu.ShowMatchResult(false);
                view.ResultReturnButton.onClick.Invoke();
                menu.LeaveMatch();
                SceneNavigation.LoadMainMenu();
                yield return Returned();
                Assert.That(loads, Is.EqualTo(1));
                AssertFreshMenu();
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
        }

        private IEnumerator HostArena(bool guest)
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            manager = (ArenaNetworkManager)NetworkManager.singleton;
            manager.transport.enabled = false;
            transport = manager.gameObject.AddComponent<M1LoopbackTransport>();
            manager.transport = Transport.active = transport;
            Object.FindAnyObjectByType<MainMenuController>().OnMultiplayerClicked();
            var lobby = Object.FindAnyObjectByType<LobbyRoomView>();
            lobby.HostButton.onClick.Invoke();
            yield return Until(() => manager.CurrentRoom.Members?.Length == 1);
            {
                transport.ConnectVirtual(2);
                yield return Until(() => manager.CurrentRoom.Members.Length == 2);
                transport.SendFromVirtual(2, new LobbyReadyRequest { IsReady = true });
            }
            lobby.ReadyButton.onClick.Invoke();
            yield return Until(() => lobby.StartButton.interactable);
            lobby.StartButton.onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().name == "Arena_MVP" && NetworkClient.localPlayer != null);
            if (guest)
            {
                transport.SendFromVirtual(2, new ReadyMessage());
                transport.SendFromVirtual(2, new AddPlayerMessage());
                yield return Until(() => NetworkServer.spawned.Values.Count(identity => identity.GetComponent<NetworkPlayer>() != null) == 2);
            }
            if (!guest)
            {
                transport.DisconnectVirtual(2);
                yield return Until(() => NetworkServer.connections.Count == 1);
            }
            FindMenu();
        }

        [UnityTest]
        public IEnumerator HostPause_OnlyGatesLocalOwner_AndServerKeepsRunning()
        {
            yield return HostArena(true);
            var guest = NetworkServer.connections[2].identity.GetComponent<PlayerInputHandler>();
            Escape();
            Assert.That(input.IsMenuInputBlocked, Is.True);
            Assert.That(guest.IsMenuInputBlocked, Is.False);
            Assert.That(NetworkServer.active && NetworkClient.activeHost && transport.ServerActive(), Is.True);
            Assert.That(NetworkServer.connections.Count, Is.EqualTo(2));
            view.ResumeButton.onClick.Invoke();
            Assert.That(input.IsMenuInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator HostLeave_CleansListenersIdentitiesAndRoom_ThenCanHostAgain()
        {
            yield return HostArena(true);
            var identities = NetworkServer.spawned.Values.ToArray();
            var server = manager.GetComponent<ServerLobbyController>();
            var room = server.Room;
            var old = manager;
            menu.LeaveMatch();
            yield return Returned();
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False);
            Assert.That(NetworkServer.connections, Is.Empty);
            Assert.That(NetworkServer.spawned, Is.Empty);
            Assert.That(identities.All(identity => identity == null), Is.True);
            Assert.That(old == null, Is.True);
            Assert.That(room.Count, Is.Zero);
            AssertFreshMenu();
            manager = (ArenaNetworkManager)NetworkManager.singleton;
            manager.transport.enabled = false;
            transport = manager.gameObject.AddComponent<M1LoopbackTransport>();
            manager.transport = Transport.active = transport;
            manager.HostRoom();
            yield return Until(() => manager.CurrentRoom.Members?.Length == 1);
            Assert.That(NetworkServer.active && NetworkClient.activeHost, Is.True);
        }

        // Mirror has one static NetworkClient. This creates a real native client proxy and a
        // separately owned in-memory server, not a shared host identity or fabricated ownership flags.
        private IEnumerator ClientArena()
        {
            independentServer = new GameObject("Independent menu test server");
            Object.DontDestroyOnLoad(independentServer);
            transport = independentServer.AddComponent<M1LoopbackTransport>();
            Transport.active = transport;
            NetworkServer.Listen(4);
            NetworkServer.OnConnectedEvent = connection => connection.isAuthenticated = true;
            NetworkServer.OnDisconnectedEvent = NetworkServer.DestroyPlayerForConnection;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            NetworkServer.RegisterHandler<AddPlayerMessage>((connection, message) =>
            {
                var actor = Object.Instantiate(prefab, new Vector3(-6, 1, -6), Quaternion.identity);
                Object.DontDestroyOnLoad(actor);
                actor.GetComponent<NetworkPlayer>().SetConnectionId(connection.connectionId);
                NetworkServer.AddPlayerForConnection(connection, actor);
            });
            yield return SceneManager.LoadSceneAsync("Arena_MVP");
            var root = new GameObject("Native client manager");
            root.SetActive(false);
            manager = root.AddComponent<ArenaNetworkManager>();
            manager.transport = transport;
            manager.playerPrefab = prefab;
            manager.autoCreatePlayer = true;
            manager.offlineScene = "Assets/_Project/Scenes/MainMenu.unity";
            manager.onlineScene = "";
            manager.headlessStartMode = HeadlessStartOptions.DoNothing;
            root.SetActive(true);
            manager.StartClient();
            yield return Until(() => NetworkClient.localPlayer != null);
            Assert.That(NetworkClient.activeHost, Is.False);
            Assert.That(manager.mode, Is.EqualTo(NetworkManagerMode.ClientOnly));
            Assert.That(NetworkClient.localPlayer, Is.Not.SameAs(NetworkServer.connections[1].identity));
            FindMenu();
        }

        [UnityTest]
        public IEnumerator ClientLeave_CleansProxyAndPlayer_WithoutStoppingIndependentServer()
        {
            yield return ClientArena();
            var proxy = NetworkClient.localPlayer;
            var actor = NetworkServer.connections[1].identity;
            menu.LeaveMatch();
            yield return Returned();
            Assert.That(NetworkClient.active, Is.False);
            Assert.That(proxy == null && actor == null, Is.True);
            Assert.That(NetworkServer.active && transport.ServerActive(), Is.True);
            Assert.That(NetworkServer.connections, Is.Empty);
            Assert.That(NetworkServer.spawned, Is.Empty);
            AssertFreshMenu();
        }

        [UnityTest]
        public IEnumerator ServerDisconnect_WithoutLeaveClick_ReturnsClientToMainMenu()
        {
            yield return ClientArena();
            transport.ServerDisconnect(1);
            yield return Returned();
            Assert.That(NetworkClient.active, Is.False);
            Assert.That(NetworkServer.connections, Is.Empty);
            AssertFreshMenu();
        }

        private static IEnumerator Returned()
        {
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && !MatchSessionNavigation.IsReturning);
            yield return null;
        }
        private static void AssertFreshMenu()
        {
            Assert.That(NetworkClient.localPlayer, Is.Null);
            Assert.That(NetworkClient.connection, Is.Null);
            Assert.That(NetworkClient.spawned, Is.Empty);
            AssertMenuCursor();
            var fresh = NetworkManager.singleton as ArenaNetworkManager;
            Assert.That(fresh, Is.Not.Null);
            Assert.That(fresh.CurrentRoom.Phase, Is.EqualTo(LobbyPhase.Offline));
            Assert.That(fresh.GetComponent<ServerLobbyController>().Room.Count, Is.Zero);
            Assert.That(Object.FindObjectsByType<ArenaNetworkManager>().Length, Is.EqualTo(1));
        }
    }
}
#endif
