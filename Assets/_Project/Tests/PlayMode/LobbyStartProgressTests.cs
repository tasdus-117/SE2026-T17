#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using SmallArena.Networking;
using SmallArena.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class LobbyStartProgressTests : LobbyTestFixture
    {
        private static bool OverlayVisible()
        {
            var overlay = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.GetType().FullName == "SmallArena.Client.UI.ArenaLoadingOverlay");
            return overlay != null && (bool)overlay.GetType().GetProperty("IsVisible").GetValue(overlay);
        }

        private IEnumerator ReadyHost(int guests = 1)
        {
            yield return Host();
            for (int id = 2; id < 2 + guests; id++) Connect(id);
            yield return Until(() => server.Room.Count == guests + 1);
            for (int id = 2; id < 2 + guests; id++) ReadyRemote(id, true);
            view.ReadyButton.onClick.Invoke();
            yield return Until(() => server.Room.AllReady && view.StartButton.interactable);
        }

        [UnityTest]
        public IEnumerator T08_HostRealScene_OverlaySurvivesRoomPanelThenCleansAtLocalReady()
        {
            yield return ReadyHost();
            Assert.That(OverlayVisible(), Is.False);
            var panel = GameObject.Find("RoomPanel");
            Assert.That(panel, Is.Not.Null);
            int actualArenaLoads = 0; bool survived = false;
            Action<Scene, LoadSceneMode> loaded = (scene, mode) =>
            {
                if (scene.name != "Arena_MVP") return;
                actualArenaLoads++;
                survived = panel == null && OverlayVisible();
            };
            SceneManager.sceneLoaded += loaded;
            try
            {
                manager.RequestRoomStart(); manager.RequestRoomStart();
                yield return Until(() => NetworkClient.localPlayer != null && server.Room.Phase == LobbyPhase.Playing);
                Assert.That(survived, Is.True, "Overlay must remain visible after RoomPanel destruction, before Mirror local-ready callback.");
                Assert.That(actualArenaLoads, Is.EqualTo(1), "Native host scene load side effect");
                Assert.That(OverlayVisible(), Is.False, "Cleanup at this client's scene-ready");
                manager.RequestRoomStart();
                transport.Pump(); yield return null;
                Assert.That(actualArenaLoads, Is.EqualTo(1));
            }
            finally { SceneManager.sceneLoaded -= loaded; }
        }

        [UnityTest]
        public IEnumerator T09_ClientOnlyHook_ServerCallbackDoesNotCompleteGuestOverlay()
        {
            // Local lifecycle characterization only: no separate native guest process.
            Assert.That(NetworkServer.active, Is.False);
            manager.OnClientChangeScene("Arena_MVP", SceneOperation.Normal, false);
            Assert.That(OverlayVisible(), Is.True, "Client scene-start must show local overlay.");
            Object.Destroy(GameObject.Find("RoomPanel")); yield return null;
            Assert.That(OverlayVisible(), Is.True);
            manager.OnServerSceneChanged("Arena_MVP");
            Assert.That(OverlayVisible(), Is.True, "Server complete is not guest-ready.");
            manager.OnStopClient(); yield return null;
            Assert.That(OverlayVisible(), Is.False);
        }

        [UnityTest]
        public IEnumerator T10_T14_RealPreflightFailure_ReturnsReadableNoticeWithoutLoading()
        {
            yield return ReadyHost();
            var path = typeof(ArenaNetworkManager).GetField("arenaScenePath", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(path, Is.Not.Null, "The actual load boundary must validate its configured scene before Mirror's log/return path.");
            path.SetValue(manager, "Assets/_Project/Scenes/BT3_missing_scene.unity");
            int arenaLoads = 0;
            Action<Scene, LoadSceneMode> loaded = (scene, mode) => { if (scene.name == "Arena_MVP") arenaLoads++; };
            SceneManager.sceneLoaded += loaded;
            try
            {
                manager.RequestRoomStart(); manager.RequestRoomStart();
                yield return Until(() => !NetworkServer.active && Object.FindAnyObjectByType<LobbyRoomView>() != null);
                yield return null;
                var returnedView = Object.FindAnyObjectByType<LobbyRoomView>();
                Assert.That(returnedView.ErrorVisible, Is.True, "Failure notice must survive destruction of session manager.");
                Assert.That(returnedView.ErrorMessage, Does.Contain("Unable to start match"));
                Assert.That(returnedView.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
                    .Any(text => text.isActiveAndEnabled && text.text.Contains("Unable to start match")), Is.True);
                Assert.That(OverlayVisible(), Is.False); Assert.That(arenaLoads, Is.Zero);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            }
            finally { SceneManager.sceneLoaded -= loaded; }
        }

        [UnityTest]
        public IEnumerator T11_GuestDisconnect_DoesNotFailServerLoad()
        {
            yield return ReadyHost(2);
            manager.RequestRoomStart();
            transport.DisconnectVirtual(2);
            yield return Until(() => server.Room.Phase == LobbyPhase.Playing && NetworkClient.localPlayer != null);
            Assert.That(NetworkServer.active, Is.True);
            Assert.That(server.Room.Members.Any(member => member.ConnectionId == 3), Is.True);
            Assert.That(OverlayVisible(), Is.False);
        }

        [Test]
        public void T14_InactiveBoundarySignalsFailureInsteadOfSilentReturn()
        {
            var root = new GameObject("BT3 inactive manager");
            try
            {
                var inactive = root.AddComponent<ArenaNetworkManager>();
                Assert.Throws<InvalidOperationException>(() => inactive.LoadRoomArena());
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
