#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    public sealed class LobbyStartBoundaryTests : LobbyTestFixture
    {
        private IEnumerator ReadyHost()
        {
            yield return Host(); Connect(2);
            yield return Until(() => server.Room.Count == 2);
            ReadyRemote(2, true); view.ReadyButton.onClick.Invoke();
            yield return Until(() => server.Room.AllReady && view.StartButton.interactable);
        }

        private void Install(LobbyStartOperation operation)
            => typeof(ServerLobbyController).GetField("startOperation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(server, operation);
        private void Publish()
            => typeof(ServerLobbyController).GetMethod("Publish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(server, null);

        [UnityTest]
        public IEnumerator T02_RealMirrorRetry_CountsActualProductionBoundaryAndNativeLoad()
        {
            yield return ReadyHost();
            int boundary = 0, loads = 0, terminals = 0;
            var room = server.Room;
            Install(new LobbyStartOperation(room, () => { boundary++; manager.LoadRoomArena(); },
                () => { if (room.Phase == LobbyPhase.Playing) terminals++; Publish(); },
                error => Assert.Fail(error.Message)));
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => { if (scene.name == "Arena_MVP") loads++; };
            SceneManager.sceneLoaded += loaded;
            try
            {
                manager.RequestRoomStart(); manager.RequestRoomStart();
                yield return Until(() => room.Phase == LobbyPhase.Playing && NetworkClient.localPlayer != null);
                manager.RequestRoomStart(); transport.Pump(); yield return null; yield return null;
                Assert.That(boundary, Is.EqualTo(1), "B: production boundary calls across retries");
                Assert.That(loads, Is.EqualTo(1), "L: Unity native sceneLoaded side effects");
                Assert.That(terminals, Is.EqualTo(1), "S: actual room terminal transitions");
            }
            finally { SceneManager.sceneLoaded -= loaded; }
        }

        [UnityTest]
        public IEnumerator T05_RealMissingScene_RetryAndLateCompleteCannotReopenFailedOperation()
        {
            yield return ReadyHost();
            typeof(ArenaNetworkManager).GetField("arenaScenePath", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, "Assets/BT3_missing/Arena_MVP.unity");
            int boundary = 0, loads = 0, failures = 0, terminals = 0;
            bool retryRejected = false, lateRejected = false;
            var room = server.Room;
            LobbyStartOperation operation = null;
            operation = new LobbyStartOperation(room, () => { boundary++; manager.LoadRoomArena(); },
                () => { if (room.Phase.ToString() == "Failed" || room.Phase == LobbyPhase.Playing) terminals++; Publish(); },
                error =>
                {
                    failures++;
                    Assert.That(room.Phase.ToString(), Is.EqualTo("Failed"), "Terminal before teardown");
                    Assert.That(error.Message, Does.Contain("unavailable"), "Real Application.CanStreamedLevelBeLoaded preflight signal");
                    retryRejected = !operation.TryStart(0);
                    lateRejected = !operation.Complete();
                    Assert.That(manager.ServerCanSpawnPlayer(), Is.False);
                    manager.AbortRoomStart(error.Message);
                });
            Install(operation);
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => { if (scene.name == "Arena_MVP") loads++; };
            SceneManager.sceneLoaded += loaded;
            try
            {
                manager.RequestRoomStart(); manager.RequestRoomStart();
                yield return Until(() => !NetworkServer.active && !MatchSessionNavigation.IsReturning);
                Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Offline), "Final session teardown state");
                Assert.That(retryRejected && lateRejected, Is.True);
                Assert.That(boundary, Is.EqualTo(1)); Assert.That(loads, Is.Zero);
                Assert.That(failures, Is.EqualTo(1)); Assert.That(terminals, Is.EqualTo(1));
            }
            finally { SceneManager.sceneLoaded -= loaded; }
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void T13_AuthenticatedCount_RealConnectCallbackBoundary(int count)
        {
            // Direct integration at Mirror's authenticated callback contract, not a socket test.
            // OnServerAuthenticated sets the incoming connection true before OnServerConnect.
            manager.StartServer();
            NetworkConnectionToClient incoming = null;
            for (var id = 1; id <= count; id++)
            {
                incoming = new NetworkConnectionToClient(id) { isAuthenticated = true };
                NetworkServer.connections.Add(id, incoming);
            }
            Assert.That(NetworkServer.connections.Values.Count(c => c.isAuthenticated), Is.EqualTo(count));
            int admitted = 0;
            manager.ServerConnected += _ => admitted++;
            manager.OnServerConnect(incoming);
            Assert.That(admitted, Is.EqualTo(count <= 4 ? 1 : 0), "Authenticated count 4 must be admitted; 5 rejected.");
            Assert.That(incoming.isAuthenticated, Is.EqualTo(count <= 4));
            Assert.That(server.Room.Count, Is.LessThanOrEqualTo(4));
        }

        [Test]
        public void T14_ExistingWrongSceneIsRejectedBeforeMirrorLoad()
        {
            manager.StartServer();
            Assert.That(Application.CanStreamedLevelBeLoaded("Assets/_Project/Scenes/MainMenu.unity"), Is.True);
            typeof(ArenaNetworkManager).GetField("arenaScenePath", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, "Assets/_Project/Scenes/MainMenu.unity");
            var previous = NetworkManager.loadingSceneAsync;
            var error = Assert.Throws<InvalidOperationException>(() => manager.LoadRoomArena());
            Assert.That(error.Message, Does.Contain("must target Arena_MVP"));
            Assert.That(NetworkManager.loadingSceneAsync, Is.SameAs(previous));
            Assert.That(NetworkServer.isLoadingScene, Is.False);
        }
    }
}
#endif
