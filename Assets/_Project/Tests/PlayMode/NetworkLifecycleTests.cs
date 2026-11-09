#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.Presentation;
using SmallArena.Input;
using SmallArena.Networking;
using SmallArena.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkLifecycleTests : InputTestFixture
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private ArenaNetworkManager manager;
        private Scene arena;
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        }

        public override void TearDown()
        {
            try
            {
                if (manager != null)
                {
                    manager.offlineScene = "";
                    if (NetworkClient.active && NetworkServer.active) manager.StopHost();
                    else if (NetworkServer.active) manager.StopServer();
                    else if (NetworkClient.active) manager.StopClient();
                }
                foreach (var item in created) if (item != null) Object.DestroyImmediate(item);
                created.Clear();
                NetworkManager.ResetStatics();
                Transport.active = null;
            }
            finally { base.TearDown(); }
        }

        [UnityTearDown]
        public IEnumerator UnloadArena()
        {
            if (!arena.IsValid() || !arena.isLoaded) yield break;
            if (manager != null)
            {
                manager.offlineScene = "";
                if (NetworkClient.active && NetworkServer.active) manager.StopHost();
                else if (NetworkServer.active) manager.StopServer();
                else if (NetworkClient.active) manager.StopClient();
            }
            if (SceneManager.sceneCount == 1)
                SceneManager.SetActiveScene(SceneManager.CreateScene("Lifecycle test cleanup"));
            yield return SceneManager.UnloadSceneAsync(arena);
        }

        private GameObject Track(GameObject item) { created.Add(item); return item; }

        private void CreateManager(bool markers = true)
        {
            var owner = Track(new GameObject("Lifecycle manager under test"));
            owner.SetActive(false);
            var transport = owner.AddComponent<LifecycleTestTransport>();
            manager = owner.AddComponent<ArenaNetworkManager>();
            manager.transport = transport;
            manager.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            manager.offlineScene = "";
            manager.onlineScene = "";
            manager.playerSpawnMethod = PlayerSpawnMethod.RoundRobin;
            manager.maxConnections = 4;
            manager.autoCreatePlayer = true;
            manager.headlessStartMode = HeadlessStartOptions.DoNothing;
            owner.SetActive(true);
            if (!markers) return;
            foreach (var position in new[] { new Vector3(-6, 1, -6), new Vector3(6, 1, -6), new Vector3(-6, 1, 6), new Vector3(6, 1, 6) })
            {
                var marker = Track(new GameObject("Test start"));
                marker.transform.position = position;
                marker.AddComponent<NetworkStartPosition>();
            }
        }

        private NetworkConnectionToClient AddRemote(int id)
        {
            var connection = new NetworkConnectionToClient(id) { isAuthenticated = true };
            Assert.That(NetworkServer.AddConnection(connection), Is.True);
            manager.OnServerConnect(connection);
            manager.OnServerAddPlayer(connection);
            if (connection.identity != null) Track(connection.identity.gameObject);
            return connection;
        }

        private static IEnumerator WaitForLocalPlayer()
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (NetworkClient.localPlayer == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(NetworkClient.localPlayer, Is.Not.Null, "Mirror host handshake must spawn the owning player.");
        }

        [UnityTest]
        public IEnumerator HostOwnership_UsesMirrorFlags_AndBindsCameraAndInput()
        {
            CreateManager();
            var camera = Track(new GameObject("Local camera")).AddComponent<ThirdPersonCameraController>();
            var starts = 0;
            var stops = 0;
            Action<NetworkPlayer> onStart = _ => starts++;
            Action<NetworkPlayer> onStop = _ => stops++;
            NetworkPlayer.OnLocalPlayerSpawned += onStart;
            NetworkPlayer.OnLocalPlayerDespawned += onStop;
            try
            {
                manager.StartHost();
                yield return WaitForLocalPlayer();
                var local = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
                Track(local.gameObject);
                Assert.That(local.isLocalPlayer && local.IsLocalControllable, Is.True);
                Assert.That(local.ConnectionId, Is.EqualTo(NetworkServer.localConnection.connectionId));
                Assert.That(starts, Is.EqualTo(1));
                Assert.That(camera.Target, Is.SameAs(local.transform));
                Assert.That(local.GetComponent<PlayerInputHandler>().IsInputReady, Is.True);
                var remote = AddRemote(17).identity.GetComponent<NetworkPlayer>();
                Assert.That(remote.isLocalPlayer || remote.IsLocalControllable, Is.False);
                Assert.That(remote.GetComponent<PlayerInputHandler>().IsInputReady, Is.False);
                camera.enabled = false;
                camera.SetTarget(null);
                camera.enabled = true;
                Assert.That(camera.Target, Is.SameAs(local.transform), "Late camera enable must recover the local target.");
                manager.StopHost();
                yield return null;
                Assert.That(stops, Is.EqualTo(1));
                Assert.That(camera.Target, Is.Null);
            }
            finally
            {
                NetworkPlayer.OnLocalPlayerSpawned -= onStart;
                NetworkPlayer.OnLocalPlayerDespawned -= onStop;
            }
        }

        [UnityTest]
        public IEnumerator FourPlayers_GetDistinctServerSpawns_AndFreedSlotIsReused()
        {
            CreateManager();
            manager.StartServer();
            var connections = Enumerable.Range(1, 4).Select(AddRemote).ToArray();
            Assert.That(connections.Select(item => item.identity.transform.position).Distinct().Count(), Is.EqualTo(4));
            foreach (var connection in connections)
            {
                Assert.That(connection.identity.GetComponent<NetworkPlayer>().ConnectionId, Is.EqualTo(connection.connectionId));
                Assert.That(NetworkServer.spawned.ContainsKey(connection.identity.netId), Is.True);
                Assert.That(NetworkManager.startPositions.Any(start => start.position == connection.identity.transform.position), Is.True);
            }
            var oldIdentity = connections[1].identity;
            var oldId = oldIdentity.netId;
            var freedPosition = oldIdentity.transform.position;
            manager.OnServerDisconnect(connections[1]);
            Assert.That(connections[1].identity, Is.Null);
            Assert.That(connections[1].owned, Is.Empty);
            Assert.That(NetworkServer.spawned.ContainsKey(oldId), Is.False);
            Assert.DoesNotThrow(() => manager.OnServerDisconnect(connections[1]));
            var replacement = AddRemote(9);
            Assert.That(replacement.identity.transform.position, Is.EqualTo(freedPosition));
            yield return null;
            Assert.That(oldIdentity == null, Is.True, "Disconnected player must be destroyed, not just unregistered.");
        }

        [Test]
        public void FullSpawnSet_RejectsExtraPlayer_AndDuplicateAddDoesNotLeak()
        {
            CreateManager();
            manager.StartServer();
            var first = AddRemote(1);
            AddRemote(2); AddRemote(3); AddRemote(4);
            manager.OnServerAddPlayer(first);
            LogAssert.Expect(LogType.Warning, "[TV1] No free server spawn for connection 5.");
            var extra = AddRemote(5);
            Assert.That(extra.identity, Is.Null);
            Assert.That(NetworkServer.spawned.Count, Is.EqualTo(4));
        }

        [Test]
        public void InactiveServer_CannotCreatePlayer_AndEmptyDisconnectIsSafe()
        {
            CreateManager();
            var connection = new NetworkConnectionToClient(7);
            manager.OnServerAddPlayer(connection);
            Assert.That(connection.identity, Is.Null);
            Assert.DoesNotThrow(() => manager.OnServerDisconnect(connection));
            Assert.DoesNotThrow(() => manager.OnServerDisconnect(null));
        }

        [Test]
        public void MissingMarkers_DoesNotSpawnAtClientOrPrefabCoordinates()
        {
            CreateManager(false);
            manager.StartServer();
            LogAssert.Expect(LogType.Warning, "[TV1] No free server spawn for connection 7.");
            Assert.That(AddRemote(7).identity, Is.Null);
            Assert.That(NetworkServer.spawned, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RemotePrefab_DoesNotReadKeyboard_Move_OrConsumeCombatIntent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            var remote = Track(Object.Instantiate(prefab, new Vector3(1000, 1, 1000), Quaternion.Euler(0, 45, 0)));
            var input = remote.GetComponent<PlayerInputHandler>();
            var camera = Track(new GameObject("Guarded camera")).AddComponent<ThirdPersonCameraController>();
            camera.SetTarget(remote.transform);
            var spyOwner = Track(new GameObject("Remote combat spy"));
            spyOwner.SetActive(false);
            spyOwner.AddComponent<NetworkIdentity>();
            var spy = spyOwner.AddComponent<LifecycleIntentSpy>();
            spyOwner.AddComponent<CombatController>();
            spyOwner.SetActive(true);
            var position = remote.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space, Key.J, Key.E));
            InputSystem.Update();
            yield return null;
            yield return null;
            Assert.That(input.IsInputReady, Is.False);
            Assert.That(input.MoveInput, Is.EqualTo(Vector2.zero));
            Assert.That(input.ConsumeJumpPressed() || input.ConsumePunchPressed() || input.ConsumeInteractPressed(), Is.False);
            Assert.That(remote.transform.position, Is.EqualTo(position));
            Assert.That(Mathf.DeltaAngle(remote.transform.eulerAngles.y, 45f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(spy.Consumed, Is.Zero, "Remote combat must return before polling its input source.");
            Assert.That(camera.Target, Is.Null);
        }

        [UnityTest]
        public IEnumerator ArenaMarkers_RegisterFourStarts_WhileOfflinePlayerRemainsIdentityFree()
        {
            yield return SceneManager.LoadSceneAsync("Arena_MVP", LoadSceneMode.Additive);
            arena = SceneManager.GetSceneByName("Arena_MVP");
            var roots = arena.GetRootGameObjects();
            var markers = roots.SelectMany(item => item.GetComponentsInChildren<NetworkStartPosition>()).ToArray();
            Assert.That(markers.Length, Is.EqualTo(4));
            Assert.That(markers.Select(item => item.transform.position).Distinct().Count(), Is.EqualTo(4));
            foreach (var marker in markers) Assert.That(NetworkManager.startPositions, Does.Contain(marker.transform));
            var player = roots.Single(item => item.name == "Player");
            Assert.That(player.GetComponent<NetworkIdentity>(), Is.Null);
            Assert.That(player.GetComponent<PlayerInputHandler>().IsInputReady, Is.True);
            var camera = roots.Single(item => item.name == "Main Camera").GetComponent<ThirdPersonCameraController>();
            Assert.That(camera.Target, Is.SameAs(player.transform));
        }

        [UnityTest]
        public IEnumerator NetworkSceneLoad_DisablesOfflineActors_AndCameraFollowsSpawnedOwner()
        {
            CreateManager(false);
            manager.onlineScene = "Assets/_Project/Scenes/Arena_MVP.unity";
            manager.StartHost();
            yield return WaitForLocalPlayer();
            arena = SceneManager.GetSceneByName("Arena_MVP");
            Assert.That(arena.isLoaded, Is.True);
            var roots = arena.GetRootGameObjects();
            foreach (var name in new[] { "Player", "Bot", "WeaponPickup", "_GameManager", "MatchCanvas" })
                Assert.That(roots.Single(item => item.name == name).activeSelf, Is.False, name);
            Assert.That(NetworkManager.startPositions.Count, Is.EqualTo(4));
            var camera = roots.Single(item => item.name == "Main Camera").GetComponent<ThirdPersonCameraController>();
            Assert.That(camera.Target, Is.SameAs(NetworkClient.localPlayer.transform));
            Assert.That(NetworkClient.localPlayer.GetComponent<PlayerInputHandler>().IsInputReady, Is.True);
            Track(NetworkClient.localPlayer.gameObject);
            manager.StopHost();
            yield return null;
        }
    }

    // Host traffic uses Mirror's real LocalConnection queues. Remote sends need no socket for these server tests.
    public sealed class LifecycleTestTransport : Transport
    {
        private bool serverRunning;
        public override bool Available() => true;
        public override bool ClientConnected() => false;
        public override void ClientConnect(string address) { }
        public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable) { }
        public override void ClientDisconnect() { }
        public override Uri ServerUri() => new Uri("memory://localhost");
        public override bool ServerActive() => serverRunning;
        public override void ServerStart() => serverRunning = true;
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable) { }
        public override void ServerDisconnect(int connectionId) { }
        public override string ServerGetClientAddress(int connectionId) => "localhost";
        public override void ServerStop() => serverRunning = false;
        public override int GetMaxPacketSize(int channelId = Channels.Reliable) => 65535;
        public override void Shutdown() => serverRunning = false;
    }

    public sealed class LifecycleIntentSpy : MonoBehaviour, SmallArena.Protocol.ICombatInputSource
    {
        public int Consumed { get; private set; }
        public bool IsInputReady => true;
        public bool ConsumePunchPressed() { Consumed++; return true; }
        public bool ConsumeInteractPressed() { Consumed++; return true; }
    }
}
#endif
