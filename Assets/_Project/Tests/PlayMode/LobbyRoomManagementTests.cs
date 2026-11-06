#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class LobbyRoomManagementTests : InputTestFixture
    {
        private ArenaNetworkManager manager;
        private ServerLobbyController server;
        private LobbyRoomView view;
        private M1LoopbackTransport transport;

        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            manager = (ArenaNetworkManager)NetworkManager.singleton;
            server = manager.GetComponent<ServerLobbyController>();
            view = Object.FindAnyObjectByType<LobbyRoomView>();
            manager.transport.enabled = false;
            transport = manager.gameObject.AddComponent<M1LoopbackTransport>();
            manager.transport = transport;
            Transport.active = transport;
        }

        [UnityTearDown]
        public IEnumerator CleanupRoom()
        {
            if (manager != null)
            {
                manager.offlineScene = manager.onlineScene = "";
                if (NetworkClient.activeHost && NetworkServer.active) manager.StopHost();
                else
                {
                    if (NetworkClient.active) manager.StopClient();
                    if (transport != null) transport.Pump();
                    if (NetworkServer.active) manager.StopServer();
                }
                Object.Destroy(manager.gameObject);
            }
            yield return null;
            NetworkManager.ResetStatics();
            Transport.active = null;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Lobby test cleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 15;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for lobby protocol/scene transition.");
        }
        private void OpenPanel() => Object.FindAnyObjectByType<MainMenuController>().OnMultiplayerClicked();
        private IEnumerator Host()
        {
            OpenPanel();
            view.HostButton.onClick.Invoke();
            yield return Until(() => manager.CurrentRoom.Members?.Length == 1);
        }
        private void Connect(int id) => transport.ConnectVirtual(id);
        private void ReadyRemote(int id, bool ready) => transport.SendFromVirtual(id, new LobbyReadyRequest { IsReady = ready });

        [UnityTest]
        public IEnumerator MultiplayerButton_OpensConnectionPanel_AndBackRestoresOfflineMenu()
        {
            var button = Object.FindObjectsByType<Button>().Single(item => item.name == "MultiplayerButton");
            Assert.That(button.interactable, Is.True);
            Assert.That(view.ConnectionVisible || view.RoomVisible, Is.False);
            button.onClick.Invoke();
            Assert.That(view.ConnectionVisible && !view.RoomVisible, Is.True);
            Assert.That(view.Address.text, Is.EqualTo("localhost"));
            Assert.That(view.HostButton.interactable && view.JoinButton.interactable, Is.True);
            Assert.That(Object.FindAnyObjectByType<NetworkManagerHUD>(), Is.Null);
            view.BackButton.onClick.Invoke();
            Assert.That(view.ConnectionVisible || view.RoomVisible || NetworkClient.active || NetworkServer.active, Is.False);
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HostButton_StaysInLobby_WithOneUnreadyMemberAndNoAvatar()
        {
            yield return Host();
            Assert.That(NetworkServer.active && NetworkClient.activeHost, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(manager.maxConnections, Is.EqualTo(4));
            Assert.That(view.RoomVisible, Is.True);
            Assert.That(manager.CurrentRoom.Members[0].IsHost, Is.True);
            Assert.That(manager.CurrentRoom.Members[0].IsReady, Is.False);
            Assert.That(view.StartButton.interactable, Is.False);
            Assert.That(NetworkServer.spawned, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Capacity_FourConnectionsJoin_FifthIsRejectedByServer()
        {
            yield return Host();
            Connect(2); Connect(3); Connect(4);
            yield return Until(() => server.Room.Count == 4 && manager.CurrentRoom.Members.Length == 4);
            Assert.That(NetworkServer.connections.Count, Is.EqualTo(4));
            LogAssert.Expect(LogType.Error, new Regex("Server full, client connectionId=5 .*will be kicked"));
            Connect(5);
            yield return null;
            yield return null;
            Assert.That(NetworkServer.connections.ContainsKey(5), Is.False);
            Assert.That(server.Room.Count, Is.EqualTo(4));
            Assert.That(server.Room.TryJoin(99, false), Is.False, "Room rules also reject a bypass of the transport limit.");
        }

        [UnityTest]
        public IEnumerator Disconnect_ReleasesRoomSlot_AndReplacementStartsUnready()
        {
            yield return Host();
            Connect(2); Connect(3); Connect(4);
            yield return Until(() => server.Room.Count == 4);
            ReadyRemote(2, true);
            yield return Until(() => server.Room.Members.Single(member => member.ConnectionId == 2).IsReady);
            transport.DisconnectVirtual(2);
            yield return Until(() => server.Room.Count == 3 && !NetworkServer.connections.ContainsKey(2));
            Connect(5);
            yield return Until(() => server.Room.Count == 4 && manager.CurrentRoom.Members.Any(member => member.ConnectionId == 5));
            Assert.That(server.Room.Members.Any(member => member.ConnectionId == 2), Is.False);
            Assert.That(server.Room.Members.Single(member => member.ConnectionId == 5).IsReady, Is.False);
            Assert.That(NetworkServer.spawned, Is.Empty);
        }

        [UnityTest]
        public IEnumerator HostCannotStart_UntilEveryMemberIsReady()
        {
            yield return Host();
            Connect(2);
            yield return Until(() => server.Room.Count == 2);
            view.ReadyButton.onClick.Invoke();
            yield return Until(() => server.Room.Members.Single(member => member.IsHost).IsReady);
            manager.RequestRoomStart();
            yield return null;
            yield return null;
            Assert.That(server.Room.Phase, Is.EqualTo(LobbyPhase.Waiting));
            Assert.That(view.StartButton.interactable, Is.False);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        [UnityTest]
        public IEnumerator OnlyReadyHost_CanLoadArena_AndPlayersSpawnAfterSceneLoad()
        {
            yield return Host();
            Connect(2);
            yield return Until(() => server.Room.Count == 2);
            ReadyRemote(2, true);
            view.ReadyButton.onClick.Invoke();
            yield return Until(() => server.Room.AllReady && view.StartButton.interactable);
            transport.SendFromVirtual(2, new LobbyStartRequest());
            yield return null;
            yield return null;
            Assert.That(server.Room.Phase, Is.EqualTo(LobbyPhase.Waiting), "A ready remote member cannot start the host's match.");
            view.StartButton.onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().name == "Arena_MVP" && NetworkClient.localPlayer != null);
            Assert.That(server.Room.Phase, Is.EqualTo(LobbyPhase.Playing));
            Assert.That(NetworkManager.startPositions.Count, Is.EqualTo(4));
            transport.SendFromVirtual(2, new ReadyMessage());
            transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => NetworkServer.spawned.Count == 2);
            Assert.That(NetworkServer.spawned.Values.Select(identity => identity.transform.position).Distinct().Count(), Is.EqualTo(2));
            Assert.That(NetworkClient.localPlayer.GetComponent<NetworkPlayer>().isLocalPlayer, Is.True);
            var offline = SceneManager.GetActiveScene().GetRootGameObjects().Single(root => root.name == "Player");
            Assert.That(offline.activeSelf, Is.False);
            Assert.That(server.Room.TryJoin(3, false), Is.False, "Joining a playing room is rejected.");
        }

        [UnityTest]
        public IEnumerator AddPlayerDuringLobby_CannotBypassTheServerSpawnGate()
        {
            yield return Host();
            Connect(2);
            yield return Until(() => server.Room.Count == 2);
            transport.SendFromVirtual(2, new ReadyMessage());
            transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return null;
            yield return null;
            Assert.That(NetworkServer.connections[2].identity, Is.Null);
            Assert.That(NetworkServer.spawned, Is.Empty);
        }

        [UnityTest]
        public IEnumerator LeavingLobby_ClearsMembersAndAllowsAnotherHostSession()
        {
            yield return Host();
            Connect(2);
            yield return Until(() => server.Room.Count == 2);
            view.LeaveButton.onClick.Invoke();
            yield return Until(() => !NetworkServer.active && !NetworkClient.active);
            Assert.That(server.Room.Count, Is.Zero);
            Assert.That(view.ConnectionVisible, Is.True);
            view.HostButton.onClick.Invoke();
            yield return Until(() => server.Room.Count == 1 && manager.CurrentRoom.Members?.Length == 1);
            Assert.That(server.Room.Members[0].IsReady, Is.False);
        }

        [UnityTest]
        public IEnumerator JoinButton_TrimsAddress_AndCancelRestoresMenu()
        {
            OpenPanel();
            var probe = manager.gameObject.AddComponent<LobbyUnavailableTransport>();
            manager.transport = probe;
            Transport.active = probe;
            view.Address.text = " 127.0.0.1 ";
            view.JoinButton.onClick.Invoke();
            Assert.That(probe.LastAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(NetworkClient.active && !view.HostButton.interactable && !view.JoinButton.interactable, Is.True);
            view.BackButton.onClick.Invoke();
            yield return null;
            Assert.That(NetworkClient.active || view.ConnectionVisible || view.RoomVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator FailedJoin_ShowsFriendlyStatus_AndReenablesConnectionButtons()
        {
            OpenPanel();
            var probe = manager.gameObject.AddComponent<LobbyUnavailableTransport>();
            manager.transport = probe;
            Transport.active = probe;
            view.JoinButton.onClick.Invoke();
            probe.FailConnection();
            yield return Until(() => view.HostButton.interactable && view.JoinButton.interactable);
            Assert.That(NetworkClient.active, Is.False);
            Assert.That(view.ConnectionVisible && view.HostButton.interactable && view.JoinButton.interactable, Is.True);
            Assert.That(manager.ConnectionStatus, Does.Contain("room may be full"));
        }

        [UnityTest]
        public IEnumerator ArenaReturn_CreatesFreshMenuManager_AndCanHostAgain()
        {
            yield return Host();
            view.ReadyButton.onClick.Invoke();
            yield return Until(() => view.StartButton.interactable);
            view.StartButton.onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().name == "Arena_MVP" && NetworkClient.localPlayer != null);
            var previous = manager;
            manager.LeaveRoom();
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && Object.FindAnyObjectByType<LobbyRoomView>() != null);
            yield return null;
            Assert.That(previous == null, Is.True, "Mirror destroys the stopped session root when leaving the arena.");
            manager = (ArenaNetworkManager)NetworkManager.singleton;
            server = manager.GetComponent<ServerLobbyController>();
            manager.transport.enabled = false;
            transport = manager.gameObject.AddComponent<M1LoopbackTransport>();
            manager.transport = transport;
            Transport.active = transport;
            view = Object.FindAnyObjectByType<LobbyRoomView>();
            OpenPanel();
            view.HostButton.onClick.Invoke();
            yield return Until(() => manager.CurrentRoom.Members?.Length == 1);
            Assert.That(Object.FindObjectsByType<ArenaNetworkManager>().Length, Is.EqualTo(1));
        }

        [Test]
        public void RoomRules_DropDuplicateReadyAndUnknownSender_AndLockOnceStarting()
        {
            var room = new LobbyRoom();
            room.Open();
            Assert.That(room.TryJoin(0, true) && room.TryJoin(2, false), Is.True);
            Assert.That(room.SetReady(99, true), Is.False);
            Assert.That(room.SetReady(0, true), Is.True);
            Assert.That(room.SetReady(0, true), Is.False);
            Assert.That(room.TryStart(0), Is.False);
            room.SetReady(2, true);
            Assert.That(room.TryStart(2), Is.False);
            Assert.That(room.TryStart(0), Is.True);
            Assert.That(room.TryStart(0) || room.SetReady(2, false) || room.TryJoin(3, false), Is.False);
            room.FinishLoading();
            Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Playing));
        }
    }

    /// <summary>No sockets: hold a pending connection until cancellation or an explicit transport failure.</summary>
    public sealed class LobbyUnavailableTransport : Transport
    {
        public string LastAddress { get; private set; }
        public void FailConnection() => OnClientDisconnected?.Invoke();
        public override bool Available() => true;
        public override bool ClientConnected() => false;
        public override void ClientConnect(string address) => LastAddress = address;
        public override void ClientDisconnect() => FailConnection();
        public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable) { }
        public override bool ServerActive() => false;
        public override void ServerStart() { }
        public override void ServerStop() { }
        public override void ServerSend(int id, ArraySegment<byte> segment, int channelId = Channels.Reliable) { }
        public override void ServerDisconnect(int id) { }
        public override string ServerGetClientAddress(int id) => "memory";
        public override Uri ServerUri() => new Uri("memory://lobby");
        public override int GetMaxPacketSize(int channelId = Channels.Reliable) => 65535;
        public override void Shutdown() { }
    }
}
#endif
