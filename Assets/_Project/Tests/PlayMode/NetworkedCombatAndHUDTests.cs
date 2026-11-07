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
    public sealed class NetworkedCombatAndHUDTests : InputTestFixture
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<GameObject> suspended = new List<GameObject>();
        private ArenaNetworkManager manager;
        private M1LoopbackTransport transport;
        private NetworkIdentity ownerServer, otherServer;
        private NetworkHealth owner, other;
        private NetworkHealthHUD hud;
        private InGameMenuController menu;
        private Mouse mouse;
        private float captureDelta;

        public override void Setup()
        {
            base.Setup();
            captureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
        }
        private GameObject Track(GameObject root) { created.Add(root); return root; }
        private T Prefab<T>(string name) where T : Component => Track(Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/" + name + ".prefab"))).GetComponent<T>();
        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for actual Mirror Command/SyncVar delivery.");
        }
        private IEnumerator StartWorld()
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
        private void Arrange(bool virtualAttacks)
        {
            ownerServer.transform.position = new Vector3(1000, 1, virtualAttacks ? 1001 : 1000);
            otherServer.transform.position = new Vector3(1000, 1, virtualAttacks ? 1000 : 1001);
            Physics.SyncTransforms();
        }
        private void AttackVirtual(Vector3 direction, NetworkIdentity target = null)
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
        private void AttackOwner(Vector3 direction) => NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestAttack(direction);
        private IEnumerator Click()
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

        [UnityTest]
        public IEnumerator NativeClientProxy_CannotApplyHealthChanges_EvenAlongsideActiveTestServer()
        {
            yield return StartWorld();
            Assert.That(owner.TakeDamage(99), Is.False);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().TakeDamage(-1), Is.False);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().TakeDamage(0), Is.False);
            yield return null;
            Assert.That(owner.CurrentHealth, Is.EqualTo(100));
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator LeftClick_UsesCommand_CooldownDropsRapidFreshClicks()
        {
            yield return StartWorld();
            Set(mouse.position, new Vector2(Screen.width / 2, Screen.height / 2)); InputSystem.Update();
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            yield return Click();
            yield return Until(() => other.CurrentHealth == 90);
            yield return Click(); yield return Click();
            Assert.That(otherServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(90));
            yield return new WaitForSecondsRealtime(0.55f);
            yield return Click();
            yield return Until(() => other.CurrentHealth == 80);
            Assert.That(owner.CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator HeldMouse_DoesNotAutoAttackAfterCooldown()
        {
            yield return StartWorld();
            Set(mouse.position, new Vector2(Screen.width / 2, Screen.height / 2)); InputSystem.Update();
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            Press(mouse.leftButton); InputSystem.Update();
            yield return Until(() => other.CurrentHealth == 90);
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(other.CurrentHealth, Is.EqualTo(90));
        }
        [UnityTest]
        public IEnumerator RemoteAttack_ReplicatesToVictimsNativeProxyAndLocalHUD()
        {
            yield return StartWorld();
            Arrange(true); AttackVirtual(Vector3.forward);
            yield return Until(() => owner.CurrentHealth == 90);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(90));
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(hud.HealthView.HealthBar.value, Is.EqualTo(90));
            Assert.That(hud.HealthView.HealthText, Is.EqualTo("HP: 90/100"));
            Assert.That(other.CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator InvalidAimAndOutOfRangeRequests_CannotDealDamage()
        {
            yield return StartWorld();
            AttackOwner(new Vector3(float.NaN, 0, 1)); AttackOwner(Vector3.forward * 100); AttackOwner(Vector3.back);
            otherServer.transform.position += Vector3.forward * 3; Physics.SyncTransforms();
            AttackOwner(Vector3.forward);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator EnvironmentWall_BlocksHitEvenWithinSphere()
        {
            yield return StartWorld();
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.layer = 6;
            wall.transform.position = ownerServer.transform.position + new Vector3(0, 1, 0.5f);
            wall.transform.localScale = new Vector3(3, 3, 0.1f); Physics.SyncTransforms();
            AttackOwner(Vector3.forward);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator DuplicateColliders_OnlyTakeOneHit_AndSelfIsExcluded()
        {
            yield return StartWorld();
            var collider = otherServer.gameObject.AddComponent<SphereCollider>(); collider.center = Vector3.up; collider.radius = 0.4f;
            Physics.SyncTransforms(); AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 90);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator ForgedCommandForOtherOwner_IsRejectedByMirror()
        {
            yield return StartWorld();
            LogAssert.Expect(LogType.Warning, new Regex("Command .*CmdPerformAttack.*without authority"));
            AttackVirtual(Vector3.forward, ownerServer);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(owner.CurrentHealth, Is.EqualTo(100)); Assert.That(other.CurrentHealth, Is.EqualTo(100));
        }
        [UnityTest]
        public IEnumerator ServerDeath_ClampsOnce_ReplicatesDefeatAndWinner_AndStopsFurtherDamage()
        {
            yield return StartWorld();
            Arrange(true);
            var health = ownerServer.GetComponent<NetworkHealth>(); var deaths = 0;
            health.OnDeath += () => deaths++;
            Assert.That(health.TakeDamage(90), Is.True); AttackVirtual(Vector3.forward);
            yield return Until(() => owner.Result == PlayerMatchResult.Defeat && other.Result == PlayerMatchResult.Victory);
            Assert.That(owner.CurrentHealth, Is.Zero); Assert.That(deaths, Is.EqualTo(1));
            Assert.That(health.TakeDamage(int.MaxValue), Is.False);
            Assert.That(otherServer.GetComponent<NetworkHealth>().TakeDamage(1), Is.False);
            Assert.That(menu.GetComponent<InGameMenuView>().ResultHeader, Is.EqualTo("DEFEAT"));
            Assert.That(menu.GetComponent<InGameMenuView>().ResultVisible, Is.True);
            Assert.That(owner.GetComponent<PlayerInputHandler>().IsMenuInputBlocked, Is.True);
        }
        [UnityTest]
        public IEnumerator NativeWinner_GetsVictoryModal_FromReplicatedResultOnly()
        {
            yield return StartWorld();
            otherServer.GetComponent<NetworkHealth>().TakeDamage(90); AttackOwner(Vector3.forward);
            yield return Until(() => owner.Result == PlayerMatchResult.Victory);
            Assert.That(menu.GetComponent<InGameMenuView>().ResultHeader, Is.EqualTo("VICTORY"));
            Assert.That(menu.GetComponent<InGameMenuView>().ResultVisible, Is.True);
        }
        [UnityTest]
        public IEnumerator HUDClick_RaycastBlocksAttack_AndDoesNotQueueForLater()
        {
            yield return StartWorld();
            Track(new GameObject("Combat UI events", typeof(EventSystem), typeof(InputSystemUIInputModule))).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            menu.Resume(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var panel = hud.HealthView.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center));
            Set(mouse.position, point); InputSystem.Update();
            yield return null;
            var hitResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hitResults);
            Assert.That(hitResults, Is.Not.Empty, $"HUD must receive a real pointer raycast at {point}; panel={panel.rect}, screen={Screen.width}x{Screen.height}.");
            yield return Click();
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(100));
            Set(mouse.position, new Vector2(Screen.width / 2, Screen.height / 2)); InputSystem.Update();
            yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(100), "A blocked UI press cannot become a delayed attack.");
            yield return Click();
            yield return Until(() => other.CurrentHealth == 90);
        }
        [UnityTest]
        public IEnumerator PendingMember_DelaysCombatUntilItsSpawnOrDisconnect()
        {
            yield return StartWorld();
            transport.ConnectVirtual(3);
            yield return Until(() => NetworkServer.connections.ContainsKey(3));
            AttackOwner(Vector3.forward);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(100));
            transport.DisconnectVirtual(3);
            yield return Until(() => !NetworkServer.connections.ContainsKey(3));
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 90);
        }
        [UnityTest]
        public IEnumerator RemoteHealthBar_ShowsReplicatedHealth_WithoutLocalHUDRebinding()
        {
            yield return StartWorld();
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 90);
            var bar = other.GetComponentInChildren<NetworkHealthBarView>();
            Assert.That(bar, Is.Not.Null); Assert.That(bar.HealthText, Is.EqualTo("HP: 90/100"));
            Assert.That(hud.HealthView.HealthText, Is.EqualTo("HP: 100/100"));
            Assert.That(owner.GetComponentInChildren<NetworkHealthBarView>(), Is.Null);
            bar.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.55f);
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 80);
            bar.gameObject.SetActive(true);
            Assert.That(bar.HealthText, Is.EqualTo("HP: 80/100"));
        }
    }
}
#endif
