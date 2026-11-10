#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.UI;
using SmallArena.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class LobbyGovernanceAndScreenTests : LobbyTestFixture
    {
        [UnityTest]
        public IEnumerator Slots_NativeHostAndRemoteSnapshot_ShowTwoUnreadyMembers()
        {
            yield return Host();
            Connect(2);
            yield return Until(() => manager.CurrentRoom.Members.Length == 2 && transport.SnapshotTo(2).Members?.Length == 2);
            var remote = Object.Instantiate(view, view.transform.parent);
            try
            {
                remote.Render(transport.SnapshotTo(2), false, "");
                for (var i = 0; i < 4; i++)
                {
                    Assert.That(view.SlotStatus(i), Is.EqualTo(i < 2 ? "Not Ready" : "Waiting..."));
                    Assert.That(remote.SlotStatus(i), Is.EqualTo(view.SlotStatus(i)));
                }
                Assert.That(view.SlotName(0), Does.Contain("Host"));
                Assert.That(remote.SlotName(1), Does.Contain("You"));
                Assert.That(remote.StartButton.gameObject.activeSelf, Is.False);
            }
            finally { Object.Destroy(remote.gameObject); }
        }

        [UnityTest]
        public IEnumerator StartGuard_OneReadyHostBlocked_ThenAllReady_ThenUnreadyLocksImmediately()
        {
            yield return Host();
            view.ReadyButton.onClick.Invoke();
            yield return Until(() => server.Room.Members[0].IsReady);
            Assert.That(view.StartButton.interactable || server.Room.CanStart, Is.False);
            manager.RequestRoomStart();
            yield return null; yield return null;
            Assert.That(server.Room.Phase, Is.EqualTo(LobbyPhase.Waiting));
            Connect(2);
            yield return Until(() => server.Room.Count == 2);
            ReadyRemote(2, true);
            yield return Until(() => view.StartButton.interactable);
            Assert.That(manager.CurrentRoom.CanStart, Is.True);
            transport.SendFromVirtual(2, new LobbyStartRequest());
            yield return null; yield return null;
            Assert.That(server.Room.Phase, Is.EqualTo(LobbyPhase.Waiting), "Ready non-host cannot start.");
            ReadyRemote(2, false);
            yield return Until(() => !view.StartButton.interactable);
            manager.RequestRoomStart();
            yield return null; yield return null;
            Assert.That(server.Room.Phase, Is.EqualTo(LobbyPhase.Waiting));
            Assert.That(view.SlotStatus(1), Is.EqualTo("Not Ready"));
        }

        [UnityTest]
        public IEnumerator Disconnect_ReusesVacatedSlot_WithoutMovingOthersOrKeepingReady()
        {
            yield return Host(); Connect(2); Connect(3);
            yield return Until(() => server.Room.Count == 3);
            ReadyRemote(2, true);
            yield return Until(() => view.SlotStatus(1) == "READY");
            transport.DisconnectVirtual(2);
            yield return Until(() => view.SlotStatus(1) == "Waiting...");
            Assert.That(server.Room.Members.Single(m => m.ConnectionId == 3).SlotIndex, Is.EqualTo(2));
            Connect(4);
            yield return Until(() => view.SlotStatus(1) == "Not Ready");
            Assert.That(server.Room.Members.Single(m => m.ConnectionId == 4).SlotIndex, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FifthConnection_ReceivesSpecificReason_AndFourMembersStayIntact()
        {
            yield return Host(); Connect(2); Connect(3); Connect(4);
            yield return Until(() => server.Room.Count == 4);
            Connect(5);
            yield return Until(() => transport.RejectionTo(5) != null);
            Assert.That(transport.RejectionTo(5), Is.EqualTo(LobbyLimits.FullReason));
            Assert.That(server.Room.Count, Is.EqualTo(4));
            yield return Until(() => !NetworkServer.connections.ContainsKey(5));
            Assert.That(NetworkClient.activeHost && NetworkServer.active, Is.True);
            Assert.That(NetworkServer.connections.Count, Is.EqualTo(4));
            ReadyRemote(2, true);
            yield return Until(() => view.SlotStatus(1) == "READY");
        }

        [UnityTest]
        public IEnumerator FullRoom_NativeJoiningClientShowsModal_PreservesReasonAfterDisconnect_AndCanRetry()
        {
            OpenPanel(); manager.StartServer();
            Connect(2); Connect(3); Connect(4); Connect(5);
            yield return Until(() => server.Room.Count == 4);
            // Native client joins this shared-process server; JoinRoom intentionally forbids a second UI session.
            manager.StartClient();
            yield return Until(() => view.ErrorVisible && !NetworkClient.active);
            Assert.That(view.ConnectionVisible, Is.True);
            Assert.That(view.ErrorMessage, Is.EqualTo("Cannot join: Room is full (4/4)"));
            Assert.That(manager.ConnectionStatus, Is.EqualTo(view.ErrorMessage));
            Assert.That(view.JoinButton.interactable, Is.True);
            view.DismissErrorButton.onClick.Invoke();
            Assert.That(view.ErrorVisible, Is.False);
            transport.DisconnectVirtual(5);
            yield return Until(() => server.Room.Count == 3);
            // The fixture shares server/client statics; start the native client directly after freeing a slot.
            manager.StartClient();
            yield return Until(() => manager.CurrentRoom.Members?.Length == 4);
            Assert.That(view.RoomVisible, Is.True);
            Assert.That(manager.CurrentRoom.Members.Single(m => m.ConnectionId == 1).IsReady, Is.False);
        }

        [UnityTest]
        public IEnumerator F11_FreshPressToggles_HoldDoesNotRepeat_AndCursorIsPreserved()
        {
            var display = Object.FindAnyObjectByType<ScreenDisplayController>();
            if (display == null) display = new GameObject("Display test").AddComponent<ScreenDisplayController>();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var before = display.LastRequestedFullscreen;
            var cursor = Cursor.visible; var cursorLock = Cursor.lockState; var calls = 0;
            System.Action<bool> requested = _ => calls++;
            display.FullscreenRequested += requested;
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11));
                yield return null;
                Assert.That(display.LastRequestedFullscreen, Is.EqualTo(!before));
                yield return null; yield return null;
                Assert.That(calls, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11)); yield return null;
                Assert.That(calls, Is.EqualTo(2));
                Assert.That(display.LastRequestedFullscreen, Is.EqualTo(before));
                Assert.That(Cursor.visible, Is.EqualTo(cursor));
                Assert.That(Cursor.lockState, Is.EqualTo(cursorLock));
            }
            finally { display.FullscreenRequested -= requested; }
        }

        [UnityTest]
        public IEnumerator DisplayController_PersistsAcrossMenuBootstrapArena_WithoutDuplicates()
        {
            var display = Object.FindAnyObjectByType<ScreenDisplayController>();
            if (display == null) display = new GameObject("Persistent display test").AddComponent<ScreenDisplayController>();
            // Stop idle menu networking root before Bootstrap creates its own.
            Object.Destroy(manager.gameObject); yield return null; NetworkManager.ResetStatics();
            yield return SceneManager.LoadSceneAsync("Bootstrap"); yield return null;
            Assert.That(Object.FindAnyObjectByType<ScreenDisplayController>(), Is.SameAs(display));
            manager = (ArenaNetworkManager)NetworkManager.singleton;
            Object.Destroy(manager.gameObject); yield return null; NetworkManager.ResetStatics();
            yield return SceneManager.LoadSceneAsync("Arena_MVP"); yield return null;
            Assert.That(Object.FindObjectsByType<ScreenDisplayController>().Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<ScreenDisplayController>(), Is.SameAs(display));
        }
    }
}
#endif
