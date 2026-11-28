#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Mirror;
using NUnit.Framework;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkRoomCodeJoinTests
    {
        private ArenaNetworkManager manager;
        private ServerLobbyController server;
        private sealed class Observer : NetworkConnectionToClient
        {
            public int LobbySnapshots { get; private set; }
            public Observer(int id) : base(id) { isAuthenticated = true; }
            protected override void SendToTransport(ArraySegment<byte> data, int channelId = Channels.Reliable)
            {
                var batches = new Unbatcher();
                Assert.That(batches.AddBatch(data), Is.True);
                while (batches.GetNextMessage(out var message, out _))
                    using (var reader = NetworkReaderPool.Get(message))
                        if (NetworkMessages.UnpackId(reader, out var id) && id == NetworkMessages.GetId<LobbySnapshot>()) LobbySnapshots++;
            }
            public override void Disconnect() { }
        }
        private static ushort FreePort()
        {
            using (var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
                return (ushort)((IPEndPoint)socket.Client.LocalEndPoint).Port;
        }
        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for UDP discovery / actual Mirror lobby delivery.");
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            manager = (ArenaNetworkManager)NetworkManager.singleton;
            server = manager.GetComponent<ServerLobbyController>();
            manager.DiscoveryPort = FreePort();
            ((PortTransport)manager.transport).Port = FreePort();
            // Server-only admission tests stay in the lobby, just as ConfigureRoom does for HostRoom.
            manager.onlineScene = "";
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (manager != null)
            {
                manager.offlineScene = manager.onlineScene = "";
                if (NetworkClient.activeHost && NetworkServer.active) manager.StopHost();
                else { if (NetworkClient.active) manager.StopClient(); if (NetworkServer.active) manager.StopServer(); }
                Object.Destroy(manager.gameObject);
            }
            yield return null;
            NetworkServer.Shutdown(); NetworkClient.Shutdown(); NetworkManager.ResetStatics(); Transport.active = null;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Room directory test cleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator HostedRoomPublishesOneOfFourAndRemovesOnStop()
        {
            manager.StartHostedRoom("Test Room");
            yield return Until(() => manager.CurrentRoom.Members?.Length == 1);
            Assert.That(RoomCodeGenerator.IsValid(manager.CurrentRoomCode), Is.True);
            var code = manager.CurrentRoomCode;
            using (var browser = new LanRoomDirectoryService(manager.DiscoveryPort))
            {
                browser.Refresh();
                yield return Until(() => browser.GetPublicRooms().Any(r => r.RoomCode == code));
                var room = browser.GetPublicRooms().Single(r => r.RoomCode == code);
                Assert.That(room.CurrentPlayers, Is.EqualTo(1)); Assert.That(room.MaxPlayers, Is.EqualTo(4));
                Assert.That(room.IsPublic, Is.True); Assert.That(room.Phase, Is.EqualTo(RoomDirectoryPhase.Lobby));
                // Test unpublish, not navigation: StopServer otherwise starts a new MainMenu load.
                manager.offlineScene = "";
                manager.StopHost();
                yield return Until(() => browser.GetPublicRooms().Count == 0);
                Assert.That(manager.CurrentRoomCode, Is.Empty);
            }
        }
        [UnityTest] public IEnumerator CodeOnlyJoinUsesDiscoveredKcpEndpointAndRealLobbyMessages()
        {
            // Mirror has one static native client: a server-side observer represents the other member.
            manager.StartServer();
            var observer = new Observer(999999); NetworkServer.connections.Add(observer.connectionId, observer);
            manager.OnServerConnect(observer);
            RoomAdmissionTestMessages.Send(manager.transport, observer.connectionId);
            var code = manager.CurrentRoomCode;
            using (var browser = new LanRoomDirectoryService(manager.DiscoveryPort))
            {
                browser.Refresh();
                yield return Until(() => browser.ResolveRoomCode(code).Success);
                Assert.That(browser.ResolveRoomCode(code).Listing.CurrentPlayers, Is.EqualTo(1));
                // The code API supplies address/port, not the test caller.
                manager.networkAddress = "invalid.example";
                Assert.That(manager.TryJoinRoomByCode(" " + code.ToLowerInvariant() + " ", out var failure), Is.True);
                Assert.That(failure, Is.EqualTo(RoomJoinFailureReason.None));
                yield return Until(() => manager.CurrentRoom.Members?.Length == 2);
                Assert.That(server.Room.Count, Is.EqualTo(2));
                Assert.That(NetworkClient.isConnected, Is.True);
                Assert.That(manager.CurrentRoomCode, Is.EqualTo(code));
                Assert.That(manager.networkAddress, Is.Not.EqualTo("invalid.example"));
                Assert.That(server.Room.Members.All(m => !m.IsReady), Is.True);
            }
        }
        [Test] public void InvalidAndMissingCodesDoNotStartClient()
        {
            Assert.That(manager.TryJoinRoomByCode("O01234", out var invalid), Is.False);
            Assert.That(invalid, Is.EqualTo(RoomJoinFailureReason.InvalidCodeFormat));
            Assert.That(manager.TryJoinRoomByCode("ZZZ999", out var missing), Is.False);
            Assert.That(missing, Is.EqualTo(RoomJoinFailureReason.RoomNotFound));
            Assert.That(NetworkClient.active, Is.False);
        }
        [UnityTest] public IEnumerator FourIndependentUdpListenersReceiveAndReleaseOnePort()
        {
            var port = FreePort();
            using (var a = new LanRoomDirectoryService(port))
            using (var b = new LanRoomDirectoryService(port))
            using (var c = new LanRoomDirectoryService(port))
            using (var d = new LanRoomDirectoryService(port))
            {
                a.PublishRoom(new RoomListingInfo("ABC234", "LAN", "Host", 1),
                    new RoomResolvedEndpoint("127.0.0.1", 7777, "ABC234", Guid.NewGuid().ToString("N")));
                yield return Until(() => b.ResolveRoomCode("ABC234").Success && c.ResolveRoomCode("ABC234").Success && d.ResolveRoomCode("ABC234").Success);
            }
            using (var exclusive = new UdpClient(AddressFamily.InterNetwork))
            {
                exclusive.ExclusiveAddressUse = true;
                Assert.DoesNotThrow(() => exclusive.Client.Bind(new IPEndPoint(IPAddress.Any, port)));
            }
        }
        [Test] public void LaunchArgumentsPreserveRolesAndValidateDiscoverySettings()
        {
            var options = ArenaLaunchOptions.Read(new[] { "--arena-client", "--arena-address", "127.0.0.1", "--arena-port", "7788", "--arena-discovery-port", "47778", "--arena-room-code", "abc234" });
            Assert.That(options.Role, Is.EqualTo(ArenaNetworkRole.Client)); Assert.That(options.Port, Is.EqualTo(7788));
            Assert.That(options.Address, Is.EqualTo("127.0.0.1")); Assert.That(options.DiscoveryPort, Is.EqualTo(47778));
            Assert.That(options.RoomCode, Is.EqualTo("ABC234"));
            Assert.Throws<ArgumentException>(() => ArenaLaunchOptions.Read(new[] { "--arena-discovery-port", "0" }));
            Assert.Throws<ArgumentException>(() => ArenaLaunchOptions.Read(new[] { "--arena-room-code", "OOOOOO" }));
        }
        [UnityTest] public IEnumerator MalformedDatagramDoesNotInterruptDiscovery()
        {
            var port = FreePort();
            using (var browser = new LanRoomDirectoryService(port))
            using (var raw = new UdpClient())
            {
                // ARD1 publish with an invalid seven-bit string length.
                var malformed = new byte[] { 0x41, 0x52, 0x44, 0x31, 2, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };
                raw.Send(malformed, malformed.Length, new IPEndPoint(IPAddress.Loopback, port));
                yield return Until(() => { browser.Tick(); return browser.IsAvailable; });
                using (var publisher = new LanRoomDirectoryService(port))
                {
                    publisher.PublishRoom(new RoomListingInfo("ABC234", "LAN", "Host", 1),
                        new RoomResolvedEndpoint("127.0.0.1", 7777, "ABC234", Guid.NewGuid().ToString("N")));
                    yield return Until(() => browser.ResolveRoomCode("ABC234").Success);
                    Assert.That(browser.IsAvailable && publisher.IsAvailable, Is.True);
                }
            }
        }
        [UnityTest] public IEnumerator MissingCodeResolutionIsBoundedAndCancelSuppressesOldCallback()
        {
            RoomJoinFailureReason? result = null;
            manager.JoinRoomByCode("ZZZ999", value => result = value);
            yield return Until(() => result.HasValue);
            Assert.That(result, Is.EqualTo(RoomJoinFailureReason.RoomNotFound));
            Assert.That(NetworkClient.active, Is.False);
            result = null;
            manager.JoinRoomByCode("ZZZ999", value => result = value);
            manager.CancelRoomCodeJoin();
            yield return new WaitForSecondsRealtime(2.1f);
            Assert.That(result, Is.Null); Assert.That(NetworkClient.active, Is.False);
        }
        [UnityTest] public IEnumerator WrongRoomInstanceIsRejectedBeforeRosterAdmission()
        {
            manager.StartServer(); var peer = new Observer(999999);
            Assert.That(manager.RoomEnabled, Is.True);
            NetworkServer.connections.Add(peer.connectionId, peer); manager.OnServerConnect(peer);
            Assert.That(server.Room.Count, Is.EqualTo(0));
            var admitted = new Observer(888888);
            NetworkServer.connections.Add(admitted.connectionId, admitted); manager.OnServerConnect(admitted);
            Assert.That(NetworkServer.active, Is.True);
            Assert.That(NetworkServer.isLoadingScene, Is.False, "Admission fixture must not queue packets behind a scene-loading barrier.");
            Assert.That(admitted.isAuthenticated, Is.True);
            RoomAdmissionTestMessages.Send(manager.transport, admitted.connectionId);
            yield return Until(() => admitted.LobbySnapshots > 0);
            Assert.That(peer.LobbySnapshots, Is.EqualTo(0), "A transport-authenticated but pending peer must not receive the roster.");
            RoomAdmissionTestMessages.Send(manager.transport, peer.connectionId, manager.CurrentRoomCode, Guid.NewGuid().ToString("N"));
            yield return Until(() => !peer.isAuthenticated);
            Assert.That(peer.isAuthenticated, Is.False);
            Assert.That(server.Room.Count, Is.EqualTo(1), "Rejected peer must not change the admitted roster.");
            yield return null;
        }
    }
    internal static class RoomAdmissionTestMessages
    {
        internal static void Send(Transport transport, int id, string code = null, string instance = null)
        {
            using (var writer = NetworkWriterPool.Get())
            using (var packet = NetworkWriterPool.Get())
            {
                NetworkMessages.Pack(new LobbyAdmissionRequest { Version = LobbyAdmissionRequest.CurrentVersion,
                    RoomCode = code, RoomInstanceId = instance }, writer);
                var batch = new Batcher(transport.GetMaxPacketSize());
                batch.AddMessage(writer.ToArraySegment(), NetworkTime.localTime);
                while (batch.GetBatch(packet)) transport.OnServerDataReceived.Invoke(id, packet.ToArraySegment(), Channels.Reliable);
            }
        }
    }
}
#endif
