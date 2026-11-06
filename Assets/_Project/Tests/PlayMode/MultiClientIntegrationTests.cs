#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.Presentation;
using SmallArena.Input;
using SmallArena.Networking;
using SmallArena.Protocol;
using SmallArena.Server.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    /// <summary>One native NetworkClient plus a virtual second connection. No host-shared transforms or worker processes.</summary>
    public sealed class MultiClientIntegrationTests : InputTestFixture
    {
        private static readonly MethodInfo Command = typeof(NetworkMovement).GetMethod("CmdSubmitMovement", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly List<GameObject> created = new List<GameObject>();
        private ArenaNetworkManager manager;
        private M1LoopbackTransport transport;
        private NetworkConnectionToClient connectionA;
        private NetworkConnectionToClient connectionB;
        private Keyboard keyboard;
        private float captureDelta;
        private uint sequence;

        public override void Setup()
        {
            base.Setup();
            captureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = Time.fixedDeltaTime;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            sequence = 100;
        }

        public override void TearDown()
        {
            try
            {
                if (manager != null)
                {
                    manager.offlineScene = manager.onlineScene = "";
                    if (NetworkClient.active) manager.StopClient();
                    transport.Pump();
                    if (NetworkServer.active) manager.StopServer();
                }
                NetworkClient.Shutdown();
                foreach (var item in created) if (item != null) Object.DestroyImmediate(item);
                created.Clear();
                NetworkManager.ResetStatics();
                Transport.active = null;
                // Test Runner still fails unexpected errors/exceptions; normal Mirror lifecycle logs are allowed.
            }
            finally { Time.captureDeltaTime = captureDelta; base.TearDown(); }
        }

        private GameObject Track(GameObject item) { created.Add(item); return item; }
        private static IEnumerator Until(Func<bool> predicate)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Timed out waiting for native Mirror loopback delivery.");
        }

        private IEnumerator StartWorld()
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(1000, -0.5f, 1000);
            floor.transform.localScale = new Vector3(40, 1, 40);
            floor.layer = 6;
            foreach (var offset in new[] { new Vector3(-6, 1, -6), new Vector3(6, 1, -6), new Vector3(-6, 1, 6), new Vector3(6, 1, 6) })
            {
                var marker = Track(new GameObject("M1 native start"));
                marker.transform.position = new Vector3(1000, 0, 1000) + offset;
                marker.AddComponent<NetworkStartPosition>();
            }
            var owner = Track(new GameObject("M1 native ArenaNetworkManager"));
            owner.SetActive(false);
            transport = owner.AddComponent<M1LoopbackTransport>();
            manager = owner.AddComponent<ArenaNetworkManager>();
            manager.transport = transport;
            manager.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            manager.offlineScene = manager.onlineScene = "";
            manager.playerSpawnMethod = PlayerSpawnMethod.RoundRobin;
            manager.autoCreatePlayer = true;
            manager.maxConnections = 4;
            manager.headlessStartMode = HeadlessStartOptions.DoNothing;
            owner.SetActive(true);
            Physics.SyncTransforms();
            manager.StartServer();
            // StartClient, not StartHost: exercise real spawn/deserialize with separate client replicas.
            manager.StartClient();
            yield return Until(() => NetworkClient.localPlayer != null);
            connectionA = NetworkServer.connections[M1LoopbackTransport.NativeClientId];
            transport.ConnectVirtual(2);
            transport.SendFromVirtual(2, new ReadyMessage());
            transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => NetworkServer.connections.TryGetValue(2, out connectionB) && connectionB.identity != null
                && NetworkClient.spawned.Count == 2 && transport.VisibleTo(2).Count == 2);
            foreach (var identity in NetworkServer.spawned.Values) Track(identity.gameObject);
            foreach (var identity in NetworkClient.spawned.Values) Track(identity.gameObject);
            NetworkClient.localPlayer.GetComponent<PlayerController>().enabled = false;
            // Capture the woven Command's actual hash/index rather than inventing protocol identifiers.
            NetworkClient.localPlayer.GetComponent<NetworkMovement>().SubmitInput(Vector2.zero, false);
            yield return Until(() => transport.MovementTemplate.HasValue);
            for (var frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
            Assert.That(NetworkClient.activeHost, Is.False);
        }

        private uint SendA(Vector2 move)
        {
            var sent = sequence++;
            Command.Invoke(NetworkClient.localPlayer.GetComponent<NetworkMovement>(), new object[]
            { new MovementInput { Sequence = sent, MoveX = move.x, MoveZ = move.y } });
            return sent;
        }
        private uint SendVirtual(int sender, NetworkIdentity target, Vector2 move)
        {
            var sent = sequence++;
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(new MovementInput { Sequence = sent, MoveX = move.x, MoveZ = move.y });
                var message = transport.MovementTemplate.Value;
                message.netId = target.netId;
                message.payload = writer.ToArraySegment();
                transport.SendFromVirtual(sender, message, Channels.Unreliable);
            }
            return sent;
        }
        private static float Planar(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        private static ServerPlayerMovement Simulation(NetworkIdentity identity) => identity.GetComponent<ServerPlayerMovement>();
        private static void AssertNoOrphans()
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                Assert.That(connection.identity, Is.Not.Null);
                Assert.That(connection.owned.Count, Is.EqualTo(1));
                foreach (var identity in connection.owned)
                {
                    Assert.That(identity != null, Is.True);
                    Assert.That(identity.connectionToClient, Is.SameAs(connection));
                    Assert.That(NetworkServer.spawned[identity.netId], Is.SameAs(identity));
                }
            }
            Assert.That(NetworkServer.spawned.Count, Is.EqualTo(NetworkServer.connections.Count));
            // Do not require an empty log stream: Mirror logs successful connect/spawn/disconnect activity.
        }

        [UnityTest]
        public IEnumerator TwoClients_SpawnDistinctOwnedPlayers_AtRegisteredMarkers()
        {
            yield return StartWorld();
            Assert.That(NetworkServer.connections.Count, Is.EqualTo(2));
            Assert.That(connectionA.identity.netId, Is.Not.EqualTo(connectionB.identity.netId));
            Assert.That(Planar(connectionA.identity.transform.position, connectionB.identity.transform.position), Is.GreaterThan(1));
            foreach (var connection in new[] { connectionA, connectionB })
            {
                var server = connection.identity;
                var proxy = NetworkClient.spawned[server.netId];
                Assert.That(NetworkManager.startPositions.Any(marker => Planar(marker.position, server.transform.position) < 0.01f), Is.True);
                Assert.That(proxy, Is.Not.SameAs(server));
                Assert.That(proxy.isClient && !proxy.isServer, Is.True);
                Assert.That(proxy.isLocalPlayer && proxy.isOwned, Is.EqualTo(connection == connectionA));
                Assert.That(proxy.GetComponent<CharacterController>().enabled, Is.False);
                Assert.That(server.GetComponent<CharacterController>().enabled, Is.True);
            }
            AssertNoOrphans();
        }

        [UnityTest]
        public IEnumerator InputFromA_CannotControlB_EvenWithForgedCommandTarget()
        {
            yield return StartWorld();
            var a = connectionA.identity;
            var b = connectionB.identity;
            var beforeA = a.transform.position;
            var beforeB = b.transform.position;
            NetworkClient.localPlayer.GetComponent<PlayerController>().enabled = true;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            yield return Until(() => a.transform.position.z > beforeA.z + 0.5f);
            var proxyB = NetworkClient.spawned[b.netId];
            Assert.That(proxyB.GetComponent<PlayerInputHandler>().MoveInput, Is.EqualTo(Vector2.zero));
            Assert.That(Simulation(proxyB).SimulationTick, Is.Zero);
            Assert.That(Planar(beforeB, b.transform.position), Is.LessThan(0.001f));
            LogAssert.Expect(LogType.Warning, new Regex("Command .*CmdSubmitMovement.*without authority"));
            SendVirtual(M1LoopbackTransport.NativeClientId, b, Vector2.right);
            transport.Pump();
            yield return null;
            Assert.That(Simulation(b).LastAcceptedSequence, Is.Zero);
            Assert.That(Planar(beforeB, b.transform.position), Is.LessThan(0.001f));
            Assert.That(Planar(beforeB, proxyB.transform.position), Is.LessThan(0.01f));
            AssertNoOrphans();
        }

        [UnityTest]
        public IEnumerator ServerMovement_ReplicatesBothPlayers_ToSeparateClientProxies()
        {
            yield return StartWorld();
            var a = connectionA.identity;
            var b = connectionB.identity;
            var starts = new[] { a.transform.position, b.transform.position };
            var proxyA = NetworkClient.spawned[a.netId];
            var proxyB = NetworkClient.spawned[b.netId];
            var intermediate = 0;
            for (var frame = 0; frame < 30; frame++)
            {
                SendA(Vector2.up); SendVirtual(2, b, Vector2.left);
                yield return new WaitForFixedUpdate();
                if (Planar(proxyB.transform.position, starts[1]) > 0.02f) intermediate++;
            }
            SendA(Vector2.zero); SendVirtual(2, b, Vector2.zero);
            yield return Until(() => Vector3.Distance(proxyA.transform.position, a.transform.position) < 0.03f
                && Vector3.Distance(proxyB.transform.position, b.transform.position) < 0.03f);
            Assert.That(Planar(starts[0], a.transform.position), Is.GreaterThan(1));
            Assert.That(Planar(starts[1], b.transform.position), Is.GreaterThan(1));
            Assert.That(intermediate, Is.GreaterThan(3), "Native replicas must receive intermediate snapshots.");
            Assert.That(proxyA.GetComponent<NetworkTransformReliable>().clientSnapshots.Count, Is.GreaterThan(0));
            Assert.That(proxyB.GetComponent<NetworkTransformReliable>().clientSnapshots.Count, Is.GreaterThan(0));
            Assert.That(transport.StateMessagesTo(2, a.netId), Is.GreaterThan(0));
            AssertNoOrphans();
        }

        [UnityTest]
        public IEnumerator VirtualDisconnect_UnspawnsB_AndNativeClientContinuesMoving()
        {
            yield return StartWorld();
            var b = connectionB.identity;
            var proxy = NetworkClient.spawned[b.netId];
            var id = b.netId;
            transport.DisconnectVirtual(2);
            yield return Until(() => !NetworkServer.spawned.ContainsKey(id) && !NetworkClient.spawned.ContainsKey(id));
            yield return null;
            Assert.That(b == null && proxy == null, Is.True);
            Assert.That(connectionB.identity, Is.Null);
            Assert.That(connectionB.owned, Is.Empty);
            var a = connectionA.identity;
            var before = a.transform.position;
            SendA(Vector2.up);
            yield return Until(() => a.transform.position.z > before.z + 0.3f);
            SendA(Vector2.zero);
            Assert.That(NetworkClient.localPlayer, Is.Not.Null);
            AssertNoOrphans();
        }

        [UnityTest]
        public IEnumerator RepeatedDisconnect_ReleasesMarker_AndReplacementGetsFreshIdentity()
        {
            yield return StartWorld();
            var id = connectionB.identity.netId;
            var position = connectionB.identity.transform.position;
            transport.DisconnectVirtual(2); transport.DisconnectVirtual(2);
            yield return Until(() => !NetworkClient.spawned.ContainsKey(id));
            Assert.DoesNotThrow(() => manager.OnServerDisconnect(connectionB));
            transport.ConnectVirtual(3);
            transport.SendFromVirtual(3, new ReadyMessage()); transport.SendFromVirtual(3, new AddPlayerMessage());
            yield return Until(() => NetworkServer.connections.TryGetValue(3, out var replacement) && replacement.identity != null
                && NetworkClient.spawned.ContainsKey(replacement.identity.netId));
            var newcomer = NetworkServer.connections[3].identity;
            Track(newcomer.gameObject); Track(NetworkClient.spawned[newcomer.netId].gameObject);
            Assert.That(newcomer.netId, Is.Not.EqualTo(id));
            Assert.That(Planar(newcomer.transform.position, position), Is.LessThan(0.001f));
            Assert.That(transport.VisibleTo(3), Does.Contain(connectionA.identity.netId));
            Assert.That(NetworkClient.spawned.ContainsKey(id), Is.False);
            AssertNoOrphans();
        }

        [UnityTest]
        public IEnumerator HackedClientVector_IsClamped_WithoutMovingOtherAvatar()
        {
            yield return StartWorld();
            var a = connectionA.identity;
            var b = connectionB.identity;
            var beforeB = b.transform.position;
            var beforeA = a.transform.position;
            for (var frame = 0; frame < 20; frame++)
            {
                var position = a.transform.position;
                var tick = Simulation(a).SimulationTick;
                var sent = SendA(new Vector2(3000, 4000));
                yield return Until(() => Simulation(a).LastAcceptedSequence == sent);
                yield return new WaitForFixedUpdate();
                var budget = 5f * Time.fixedDeltaTime * (Simulation(a).SimulationTick - tick);
                Assert.That(Simulation(a).AcceptedMove.magnitude, Is.EqualTo(1).Within(0.000001f));
                Assert.That(Planar(a.transform.position, position), Is.LessThanOrEqualTo(budget + 0.0002f));
            }
            Assert.That(Planar(beforeA, a.transform.position), Is.GreaterThan(0.5f));
            Assert.That(Planar(beforeB, b.transform.position), Is.LessThan(0.001f));
            AssertNoOrphans();
        }

        [UnityTest]
        public IEnumerator NativeClientDisconnect_PublishesOneDespawn_AndCleansBothSides()
        {
            var starts = 0; var stops = 0;
            Action<NetworkPlayer> onStart = _ => starts++;
            Action<NetworkPlayer> onStop = _ => stops++;
            NetworkPlayer.OnLocalPlayerSpawned += onStart;
            NetworkPlayer.OnLocalPlayerDespawned += onStop;
            try
            {
                yield return StartWorld();
                Assert.That(starts, Is.EqualTo(1));
                var identity = connectionA.identity;
                var id = identity.netId;
                manager.StopClient();
                yield return Until(() => !NetworkClient.active && !NetworkServer.connections.ContainsKey(M1LoopbackTransport.NativeClientId)
                    && !transport.VisibleTo(2).Contains(id));
                yield return null;
                Assert.That(stops, Is.EqualTo(1));
                Assert.That(NetworkClient.spawned, Is.Empty);
                Assert.That(NetworkServer.spawned.ContainsKey(id), Is.False);
                Assert.That(identity == null, Is.True);
                Assert.That(connectionA.identity, Is.Null);
                Assert.That(connectionA.owned, Is.Empty);
                Assert.That(transport.VisibleTo(2), Has.No.Member(id));
                Assert.That(NetworkServer.spawned.Count, Is.EqualTo(1));
                AssertNoOrphans();
            }
            finally
            {
                NetworkPlayer.OnLocalPlayerSpawned -= onStart;
                NetworkPlayer.OnLocalPlayerDespawned -= onStop;
            }
        }
    }

    /// <summary>Queued native Transport callbacks with real Mirror batches; no sockets or production test hooks.</summary>
    public sealed class M1LoopbackTransport : Transport
    {
        public const int NativeClientId = 1;
        private readonly Queue<Action> pending = new Queue<Action>();
        private readonly Dictionary<int, HashSet<uint>> visible = new Dictionary<int, HashSet<uint>>();
        private readonly Dictionary<int, Dictionary<uint, int>> states = new Dictionary<int, Dictionary<uint, int>>();
        private bool server;
        private bool client;
        public CommandMessage? MovementTemplate { get; private set; }
        public HashSet<uint> VisibleTo(int id) => visible.TryGetValue(id, out var set) ? set : new HashSet<uint>();
        public int StateMessagesTo(int id, uint netId) => states.TryGetValue(id, out var counts) && counts.TryGetValue(netId, out var count) ? count : 0;
        public override bool Available() => true;
        public override bool ClientConnected() => client;
        public override bool ServerActive() => server;
        public override Uri ServerUri() => new Uri("memory://m1");
        public override int GetMaxPacketSize(int channelId = Channels.Reliable) => 65535;
        public override string ServerGetClientAddress(int connectionId) => "memory-" + connectionId;
        public override void ServerStart() => server = true;
        public override void ServerStop() { server = false; pending.Clear(); }
        public override void Shutdown() { server = client = false; pending.Clear(); }
        public override void ClientConnect(string address)
        {
            pending.Enqueue(() =>
            {
                client = true;
                OnServerConnectedWithAddress.Invoke(NativeClientId, "memory-native");
                OnClientConnected.Invoke();
            });
        }
        public void ConnectVirtual(int id)
        {
            visible[id] = new HashSet<uint>(); states[id] = new Dictionary<uint, int>();
            pending.Enqueue(() => OnServerConnectedWithAddress.Invoke(id, "memory-virtual"));
        }
        public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable)
        {
            ReadMessages(segment, (id, reader) =>
            {
                if (id != NetworkMessages.GetId<CommandMessage>()) return;
                var command = reader.Read<CommandMessage>();
                command.payload = default; // Keep metadata only, never the pooled payload.
                MovementTemplate = command;
            });
            var copy = segment.ToArray();
            pending.Enqueue(() => OnServerDataReceived.Invoke(NativeClientId, new ArraySegment<byte>(copy), channelId));
        }
        public void SendFromVirtual<T>(int id, T message, int channelId = Channels.Reliable) where T : struct, NetworkMessage
        {
            using (var writer = NetworkWriterPool.Get())
            using (var batch = NetworkWriterPool.Get())
            {
                NetworkMessages.Pack(message, writer);
                var batcher = new Batcher(GetMaxPacketSize(channelId));
                batcher.AddMessage(writer.ToArraySegment(), NetworkTime.localTime);
                while (batcher.GetBatch(batch))
                {
                    var copy = batch.ToArray();
                    pending.Enqueue(() => OnServerDataReceived.Invoke(id, new ArraySegment<byte>(copy), channelId));
                }
            }
        }
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable)
        {
            var copy = segment.ToArray();
            if (connectionId == NativeClientId)
                pending.Enqueue(() => { if (client) OnClientDataReceived.Invoke(new ArraySegment<byte>(copy), channelId); });
            else if (visible.ContainsKey(connectionId)) ReadMessages(new ArraySegment<byte>(copy), (id, reader) =>
            {
                if (id == NetworkMessages.GetId<SpawnMessage>()) visible[connectionId].Add(reader.Read<SpawnMessage>().netId);
                else if (id == NetworkMessages.GetId<ObjectDestroyMessage>()) visible[connectionId].Remove(reader.Read<ObjectDestroyMessage>().netId);
                else if (id == NetworkMessages.GetId<ObjectHideMessage>()) visible[connectionId].Remove(reader.Read<ObjectHideMessage>().netId);
                else if (id == NetworkMessages.GetId<EntityStateMessage>())
                {
                    var netId = reader.Read<EntityStateMessage>().netId;
                    var counts = states[connectionId];
                    counts[netId] = counts.TryGetValue(netId, out var count) ? count + 1 : 1;
                }
            });
        }
        private static void ReadMessages(ArraySegment<byte> packet, Action<ushort, NetworkReader> inspect)
        {
            var unbatcher = new Unbatcher();
            Assert.That(unbatcher.AddBatch(packet), Is.True);
            while (unbatcher.GetNextMessage(out var message, out _))
                using (var reader = NetworkReaderPool.Get(message))
                {
                    Assert.That(NetworkMessages.UnpackId(reader, out var id), Is.True);
                    inspect(id, reader);
                }
        }
        public void DisconnectVirtual(int id) => pending.Enqueue(() => OnServerDisconnected.Invoke(id));
        public override void ServerDisconnect(int connectionId)
        {
            if (connectionId == NativeClientId) ClientDisconnect(); else DisconnectVirtual(connectionId);
        }
        public override void ClientDisconnect()
        {
            if (!client) return;
            client = false;
            pending.Enqueue(() => { OnServerDisconnected?.Invoke(NativeClientId); OnClientDisconnected?.Invoke(); });
        }
        public void Pump()
        {
            for (var count = pending.Count; count > 0 && pending.Count > 0; count--) pending.Dequeue().Invoke();
        }
        public override void ClientEarlyUpdate() => Pump();
        public override void ServerEarlyUpdate() => Pump();
    }
}
#endif
