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
    public sealed class NetworkedCombatAndHUDTests : NetworkGameplayFixture
    {
        [UnityTest]
        public IEnumerator NativeClientProxy_CannotApplyHealthChanges_EvenAlongsideActiveTestServer()
        {
            yield return StartWorld();
            Assert.That(owner.TakeDamage(99), Is.False);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().TakeDamage(-1), Is.False);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().TakeDamage(0), Is.False);
            yield return null;
            Assert.That(owner.CurrentHealth, Is.EqualTo(10));
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator LeftClick_UsesCommand_CooldownDropsRapidFreshClicks()
        {
            yield return StartWorld();
            Set(mouse.position, new Vector2(Screen.width / 2, Screen.height / 2)); InputSystem.Update();
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            yield return Click();
            yield return Until(() => other.CurrentHealth == 9);
            yield return Click(); yield return Click();
            Assert.That(otherServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(9));
            yield return new WaitForSecondsRealtime(1.05f);
            yield return Click();
            yield return Until(() => other.CurrentHealth == 8);
            Assert.That(owner.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator HeldMouse_DoesNotAutoAttackAfterCooldown()
        {
            yield return StartWorld();
            Set(mouse.position, new Vector2(Screen.width / 2, Screen.height / 2)); InputSystem.Update();
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            Press(mouse.leftButton); InputSystem.Update();
            yield return Until(() => other.CurrentHealth == 9);
            yield return new WaitForSecondsRealtime(1.15f);
            Assert.That(other.CurrentHealth, Is.EqualTo(9));
        }
        [UnityTest]
        public IEnumerator RemoteAttack_ReplicatesToVictimsNativeProxyAndLocalHUD()
        {
            yield return StartWorld();
            Arrange(true); AttackVirtual(Vector3.forward);
            yield return Until(() => owner.CurrentHealth == 9);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(9));
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(hud.HealthView.HealthBar.value, Is.EqualTo(9));
            Assert.That(hud.HealthView.HealthText, Is.EqualTo("HP: 9/10"));
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator InvalidAimAndOutOfRangeRequests_CannotDealDamage()
        {
            yield return StartWorld();
            AttackOwner(new Vector3(float.NaN, 0, 1)); AttackOwner(Vector3.forward * 10); AttackOwner(Vector3.back);
            otherServer.transform.position += Vector3.forward * 3; Physics.SyncTransforms();
            AttackOwner(Vector3.forward);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
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
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator DuplicateColliders_OnlyTakeOneHit_AndSelfIsExcluded()
        {
            yield return StartWorld();
            var collider = otherServer.gameObject.AddComponent<SphereCollider>(); collider.center = Vector3.up; collider.radius = 0.4f;
            Physics.SyncTransforms(); AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 9);
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator ForgedCommandForOtherOwner_IsRejectedByMirror()
        {
            yield return StartWorld();
            LogAssert.Expect(LogType.Warning, new Regex("Command .*CmdPerformAttack.*without authority"));
            AttackVirtual(Vector3.forward, ownerServer);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(owner.CurrentHealth, Is.EqualTo(10)); Assert.That(other.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator ServerDeath_ClampsOnce_ReplicatesDefeatAndWinner_AndStopsFurtherDamage()
        {
            yield return StartWorld();
            Arrange(true);
            var health = ownerServer.GetComponent<NetworkHealth>(); var deaths = 0;
            health.OnDeath += () => deaths++;
            Assert.That(health.TakeDamage(9), Is.True); AttackVirtual(Vector3.forward);
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
            otherServer.GetComponent<NetworkHealth>().TakeDamage(9); AttackOwner(Vector3.forward);
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
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
            Set(mouse.position, new Vector2(Screen.width / 2, Screen.height / 2)); InputSystem.Update();
            yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(10), "A blocked UI press cannot become a delayed attack.");
            yield return Click();
            yield return Until(() => other.CurrentHealth == 9);
        }
        [UnityTest]
        public IEnumerator PendingMember_DelaysCombatUntilItsSpawnOrDisconnect()
        {
            yield return StartWorld();
            transport.ConnectVirtual(3);
            yield return Until(() => NetworkServer.connections.ContainsKey(3));
            AttackOwner(Vector3.forward);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
            transport.DisconnectVirtual(3);
            yield return Until(() => !NetworkServer.connections.ContainsKey(3));
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 9);
        }
        [UnityTest]
        public IEnumerator RemoteHealthBar_ShowsReplicatedHealth_WithoutLocalHUDRebinding()
        {
            yield return StartWorld();
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 9);
            var bar = other.GetComponentInChildren<NetworkHealthBarView>();
            Assert.That(bar, Is.Not.Null); Assert.That(bar.HealthText, Is.EqualTo("HP: 9/10"));
            Assert.That(hud.HealthView.HealthText, Is.EqualTo("HP: 10/10"));
            Assert.That(owner.GetComponentInChildren<NetworkHealthBarView>(), Is.Null);
            bar.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(1.05f);
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 8);
            bar.gameObject.SetActive(true);
            Assert.That(bar.HealthText, Is.EqualTo("HP: 8/10"));
        }
    }
}
#endif
