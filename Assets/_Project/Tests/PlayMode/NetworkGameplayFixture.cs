#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.UI;
using SmallArena.Input;
using SmallArena.Networking;
using SmallArena.Server.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public abstract class NetworkGameplayFixture : InputTestFixture
    {
        protected readonly List<GameObject> created = new List<GameObject>();
        protected readonly List<GameObject> suspended = new List<GameObject>();
        protected ArenaNetworkManager manager;
        protected M1LoopbackTransport transport;
        protected NetworkIdentity ownerServer, otherServer;
        protected NetworkHealth owner, other;
        protected NetworkHealthHUD hud;
        protected InGameMenuController menu;
        protected Mouse mouse;
        protected Keyboard keyboard;
        protected float captureDelta;

        public override void Setup()
        {
            base.Setup();
            captureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }
        protected GameObject Track(GameObject root) { created.Add(root); return root; }
        protected T Prefab<T>(string name) where T : Component => Track(Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/" + name + ".prefab"))).GetComponent<T>();
        protected static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for actual Mirror Command/SyncVar delivery.");
        }
        protected IEnumerator StartWorld()
        {
            // The authoring project may open Arena_MVP before Test Runner enters PlayMode.
            // Its offline input/EventSystem must not share this fixture's virtual devices.
            foreach (var sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
                if (sceneRoot.activeSelf && (sceneRoot.name == "Player" || sceneRoot.name == "_GameManager"
                    || sceneRoot.GetComponent<Canvas>() != null || sceneRoot.GetComponent<EventSystem>() != null))
                { suspended.Add(sceneRoot); sceneRoot.SetActive(false); }
            foreach (var z in new[] { 0f, 1f })
            {
                var marker = Track(new GameObject("Combat start"));
                marker.transform.position = new Vector3(1000, 1, 1000 + z);
                marker.AddComponent<NetworkStartPosition>();
            }
            var root = Track(new GameObject("Combat test manager")); root.SetActive(false);
            transport = root.AddComponent<M1LoopbackTransport>();
            manager = root.AddComponent<ArenaNetworkManager>(); manager.transport = transport;
            manager.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            manager.offlineScene = manager.onlineScene = "";
            manager.playerSpawnMethod = PlayerSpawnMethod.RoundRobin;
            manager.headlessStartMode = HeadlessStartOptions.DoNothing;
            root.SetActive(true); manager.StartServer(); manager.StartClient();
            yield return Until(() => NetworkClient.localPlayer != null);
            transport.ConnectVirtual(2); transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => NetworkClient.spawned.Count == 2);
            ownerServer = NetworkServer.connections[1].identity; otherServer = NetworkServer.connections[2].identity;
            foreach (var identity in NetworkServer.spawned.Values)
            {
                Track(identity.gameObject);
                identity.GetComponent<ServerPlayerMovement>().enabled = false;
            }
            foreach (var identity in NetworkClient.spawned.Values) Track(identity.gameObject);
            owner = NetworkClient.localPlayer.GetComponent<NetworkHealth>();
            other = NetworkClient.spawned[otherServer.netId].GetComponent<NetworkHealth>();
            NetworkClient.localPlayer.GetComponent<PlayerController>().enabled = false;
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = false;
            Arrange(false);
            // Capture the woven method/index/hash without accepting an attack or spending cooldown.
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestAttack(Vector3.zero);
            yield return Until(() => transport.AttackTemplate.HasValue);
            menu = Prefab<InGameMenuController>("InGameMenu");
            hud = Prefab<NetworkHealthHUD>("NetworkHUD");
            yield return null;
            Assert.That(NetworkClient.activeHost, Is.False);
            Assert.That(owner, Is.Not.SameAs(ownerServer.GetComponent<NetworkHealth>()));
        }
        protected void Arrange(bool virtualAttacks)
        {
            ownerServer.transform.position = new Vector3(1000, 1, virtualAttacks ? 1001 : 1000);
            otherServer.transform.position = new Vector3(1000, 1, virtualAttacks ? 1000 : 1001);
            Physics.SyncTransforms();
        }
        protected void AttackVirtual(Vector3 direction, NetworkIdentity target = null)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                writer.WriteVector3(direction);
                var command = transport.AttackTemplate.Value;
                command.netId = target != null ? target.netId : otherServer.netId;
                command.payload = writer.ToArraySegment();
                transport.SendFromVirtual(2, command);
            }
        }
        protected void AttackOwner(Vector3 direction) => NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestAttack(direction);
        protected IEnumerator Click()
        {
            Press(mouse.leftButton); InputSystem.Update(); yield return null;
            Release(mouse.leftButton); InputSystem.Update(); yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (manager != null) { manager.offlineScene = ""; if (NetworkClient.active) manager.StopClient(); transport.Pump(); if (NetworkServer.active) manager.StopServer(); }
            NetworkClient.Shutdown();
            foreach (var root in created) if (root != null) Object.Destroy(root);
            created.Clear();
            yield return null;
            NetworkManager.ResetStatics(); Transport.active = null;
            foreach (var root in suspended) if (root != null) root.SetActive(true);
            suspended.Clear();
            Time.captureDeltaTime = captureDelta;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

    }
}
#endif
