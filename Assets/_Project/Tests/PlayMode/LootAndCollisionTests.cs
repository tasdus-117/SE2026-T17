#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.UI;
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
    public sealed class LootAndCollisionTests : NetworkGameplayFixture
    {
        private NetworkLootItem serverLoot;
        private uint lootId;
        private IEnumerator SpawnLoot(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/NetworkLootItem.prefab");
            Assert.That(prefab, Is.Not.Null);
            NetworkClient.RegisterPrefab(prefab);
            serverLoot = Track(Object.Instantiate(prefab, position, Quaternion.identity)).GetComponent<NetworkLootItem>();
            NetworkServer.Spawn(serverLoot.gameObject); lootId = serverLoot.netId;
            yield return Until(() => NetworkClient.spawned.ContainsKey(lootId));
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestPickup(uint.MaxValue);
            yield return Until(() => transport.PickupTemplate.HasValue);
            yield return Until(() => Vector3.Distance(owner.transform.position, ownerServer.transform.position) < 0.1f);
        }
        private void SendPickup(int sender, NetworkIdentity player, uint item)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                writer.WriteVarUInt(item); // Mirror's woven uint Command argument uses compressed varuint.
                var message = transport.PickupTemplate.Value;
                message.netId = player.netId; message.payload = writer.ToArraySegment();
                transport.SendFromVirtual(sender, message);
            }
        }
        private int Damage(NetworkIdentity identity) => identity.GetComponent<NetworkCombat>().CurrentDamage;

        [UnityTest]
        public IEnumerator Metrics_StartAtTen_AndUnarmedHitDealsExactlyOne()
        {
            yield return StartWorld();
            Assert.That(owner.CurrentHealth, Is.EqualTo(10)); Assert.That(owner.MaxHealth, Is.EqualTo(10));
            Assert.That(Damage(ownerServer), Is.EqualTo(1));
            AttackOwner(Vector3.forward); yield return Until(() => other.CurrentHealth == 9);
            Assert.That(hud.HealthView.HealthBar.maxValue, Is.EqualTo(10));
            Assert.That(hud.DamageText, Is.EqualTo("Damage: 1 HP (Unarmed)"));
        }
        [UnityTest]
        public IEnumerator FreshE_CollectsAuthoritatively_ReplicatesBuffAndDealsTwo()
        {
            yield return StartWorld();
            yield return SpawnLoot(ownerServer.transform.position + Vector3.left * 0.5f);
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
            Assert.That(NetworkClient.spawned.ContainsKey(lootId), Is.True, "No client-side consume prediction.");
            yield return Until(() => !NetworkClient.spawned.ContainsKey(lootId) && owner.GetComponent<NetworkCombat>().CurrentDamage == 2);
            Assert.That(NetworkServer.spawned.ContainsKey(lootId), Is.False);
            Assert.That(hud.DamageText, Is.EqualTo("Damage: 2 HP (Buffed)"));
            AttackOwner(Vector3.forward); yield return Until(() => other.CurrentHealth == 8);
            for (var i = 0; i < 35; i++) yield return null;
            Assert.That(Damage(ownerServer), Is.EqualTo(2), "Held E never stacks damage.");
        }
        [UnityTest]
        public IEnumerator RequestHasNoLocalConsumptionBeforeServerDelivery()
        {
            yield return StartWorld(); yield return SpawnLoot(ownerServer.transform.position);
            var proxy = NetworkClient.spawned[lootId].GetComponent<NetworkLootItem>();
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestPickup(lootId);
            Assert.That(proxy.IsCollected, Is.False); Assert.That(Damage(owner.netIdentity), Is.EqualTo(1));
            yield return Until(() => !NetworkClient.spawned.ContainsKey(lootId));
            Assert.That(Damage(ownerServer), Is.EqualTo(2));
        }
        [UnityTest]
        public IEnumerator OutOfRangeAndUnknownIds_AreRejectedByServer()
        {
            yield return StartWorld(); yield return SpawnLoot(ownerServer.transform.position + Vector3.right * 3);
            SendPickup(1, ownerServer, lootId); SendPickup(1, ownerServer, uint.MaxValue); SendPickup(1, ownerServer, 0);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(NetworkServer.spawned.ContainsKey(lootId), Is.True); Assert.That(serverLoot.IsCollected, Is.False);
            Assert.That(Damage(ownerServer), Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator PickupAtExactlyOnePointFiveMetres_IsAllowed()
        {
            yield return StartWorld(); yield return SpawnLoot(ownerServer.transform.position + Vector3.right * 1.5f);
            SendPickup(1, ownerServer, lootId); yield return Until(() => !NetworkServer.spawned.ContainsKey(lootId));
            Assert.That(Damage(ownerServer), Is.EqualTo(2));
        }
        private IEnumerator Race(bool virtualFirst)
        {
            yield return StartWorld(); yield return SpawnLoot((ownerServer.transform.position + otherServer.transform.position) * 0.5f);
            if (virtualFirst) { SendPickup(2, otherServer, lootId); SendPickup(1, ownerServer, lootId); }
            else { SendPickup(1, ownerServer, lootId); SendPickup(2, otherServer, lootId); }
            // Both serialized requests are queued before the same server transport pump.
            transport.Pump();
            yield return Until(() => !NetworkClient.spawned.ContainsKey(lootId)
                && owner.GetComponent<NetworkCombat>().CurrentDamage + other.GetComponent<NetworkCombat>().CurrentDamage == 3);
            Assert.That(Damage(ownerServer), Is.EqualTo(virtualFirst ? 1 : 2));
            Assert.That(Damage(otherServer), Is.EqualTo(virtualFirst ? 2 : 1));
            SendPickup(1, ownerServer, lootId); SendPickup(2, otherServer, lootId); transport.Pump();
            Assert.That(Damage(ownerServer) + Damage(otherServer), Is.EqualTo(3));
            Assert.That(NetworkServer.spawned.ContainsKey(lootId), Is.False);
        }
        [UnityTest] public IEnumerator Race_NativeFirst_WinsExactlyOnce() { yield return Race(false); }
        [UnityTest] public IEnumerator Race_VirtualFirst_WinsExactlyOnce() { yield return Race(true); }
        [UnityTest]
        public IEnumerator ForgedOwnerAndClientDirectCollection_CannotConsumeLoot()
        {
            yield return StartWorld(); yield return SpawnLoot(ownerServer.transform.position);
            Assert.That(NetworkClient.spawned[lootId].GetComponent<NetworkLootItem>().TryCollectServer(owner.GetComponent<NetworkPlayer>()), Is.False);
            LogAssert.Expect(LogType.Warning, new Regex("Command .*CmdRequestPickup.*without authority"));
            SendPickup(2, ownerServer, lootId);
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(serverLoot.IsCollected, Is.False); Assert.That(Damage(ownerServer), Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator AlreadyBuffedPlayer_CannotWasteAnotherLootItem()
        {
            yield return StartWorld(); yield return SpawnLoot(ownerServer.transform.position);
            SendPickup(1, ownerServer, lootId); yield return Until(() => Damage(ownerServer) == 2);
            yield return SpawnLoot(ownerServer.transform.position);
            SendPickup(1, ownerServer, lootId); for (var i = 0; i < 8; i++) yield return null;
            Assert.That(serverLoot.IsCollected, Is.False); Assert.That(NetworkServer.spawned.ContainsKey(lootId), Is.True);
        }
        [UnityTest]
        public IEnumerator DefeatedOwner_CannotPickupLoot()
        {
            yield return StartWorld(); yield return SpawnLoot(ownerServer.transform.position);
            ownerServer.GetComponent<NetworkHealth>().TakeDamage(10);
            SendPickup(1, ownerServer, lootId); for (var i = 0; i < 8; i++) yield return null;
            Assert.That(NetworkServer.spawned.ContainsKey(lootId), Is.True); Assert.That(Damage(ownerServer), Is.EqualTo(1));
        }
        private IEnumerator CollisionWorld()
        {
            yield return StartWorld();
            Assert.That(Physics.GetIgnoreLayerCollision(7, 7), Is.False);
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.layer = 6;
            floor.transform.position = new Vector3(1000, -0.5f, 1000); floor.transform.localScale = new Vector3(20, 1, 20);
            foreach (var actor in new[] { ownerServer, otherServer })
            {
                var controller = actor.GetComponent<CharacterController>(); controller.enabled = false;
                actor.transform.position = new Vector3(1000, controller.height * 0.5f - controller.center.y + 0.05f, actor == ownerServer ? 998.5f : 1001.5f);
                controller.enabled = true; actor.GetComponent<ServerPlayerMovement>().enabled = true;
            }
            Physics.SyncTransforms();
            owner.GetComponent<NetworkMovement>().SubmitInput(Vector2.zero, false);
            yield return Until(() => transport.MovementTemplate.HasValue);
        }
        private void MoveVirtual(Vector2 direction, uint sequence)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(new MovementInput { Sequence = sequence, MoveX = direction.x, MoveZ = direction.y });
                var message = transport.MovementTemplate.Value; message.netId = otherServer.netId; message.payload = writer.ToArraySegment();
                transport.SendFromVirtual(2, message, Channels.Unreliable);
            }
        }
        private float HorizontalGap => Vector2.Distance(new Vector2(ownerServer.transform.position.x, ownerServer.transform.position.z),
            new Vector2(otherServer.transform.position.x, otherServer.transform.position.z));
        [UnityTest]
        public IEnumerator OpposingMovement_StopsBodiesAtContact_OnServerAndReplicas()
        {
            yield return CollisionWorld();
            for (var i = 0; i < 110; i++)
            {
                owner.GetComponent<NetworkMovement>().SubmitInput(Vector2.up, false); MoveVirtual(Vector2.down, (uint)(100 + i));
                yield return new WaitForFixedUpdate();
                Assert.That(HorizontalGap, Is.GreaterThanOrEqualTo(0.99f), "Server capsule meshes may not penetrate.");
            }
            Assert.That(HorizontalGap, Is.LessThan(1.2f), "Both bodies must actually reach each other.");
            Assert.That(Vector3.Distance(owner.transform.position, other.transform.position), Is.GreaterThanOrEqualTo(0.95f));
            Assert.That(ownerServer.GetComponent<CharacterController>().isGrounded, Is.True, "Contact correction preserves grounded jump.");
            var beforeJump = ownerServer.transform.position.y;
            owner.GetComponent<NetworkMovement>().SubmitInput(Vector2.zero, true);
            for (var i = 0; i < 12; i++)
            { owner.GetComponent<NetworkMovement>().SubmitInput(Vector2.zero, false); yield return new WaitForFixedUpdate(); }
            Assert.That(ownerServer.transform.position.y, Is.GreaterThan(beforeJump + 0.2f));
        }
        [UnityTest]
        public IEnumerator CoincidentSpawn_SeparatesWithoutNaNOrLargeTeleport()
        {
            yield return CollisionWorld();
            foreach (var actor in new[] { ownerServer, otherServer })
            {
                var controller = actor.GetComponent<CharacterController>(); controller.enabled = false;
                actor.transform.position = new Vector3(1000, controller.height * 0.5f - controller.center.y + 0.05f, 1000); controller.enabled = true;
            }
            Physics.SyncTransforms();
            for (var i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.That(HorizontalGap, Is.GreaterThanOrEqualTo(0.99f));
            Assert.That(float.IsNaN(ownerServer.transform.position.x), Is.False);
            Assert.That(Vector3.Distance(ownerServer.transform.position, new Vector3(1000, ownerServer.transform.position.y, 1000)), Is.LessThan(2));
        }
        [UnityTest]
        public IEnumerator PushingAgainstWall_CannotPassThroughPeerOrEnvironment()
        {
            yield return CollisionWorld();
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.layer = 6;
            wall.transform.position = new Vector3(1000, 1, 1001.6f); wall.transform.localScale = new Vector3(8, 3, 0.2f);
            foreach (var actor in new[] { ownerServer, otherServer })
            {
                var controller = actor.GetComponent<CharacterController>(); controller.enabled = false;
                actor.transform.position = new Vector3(1000, controller.height * 0.5f - controller.center.y + 0.05f, actor == ownerServer ? 999.5f : 1001f);
                controller.enabled = true;
            }
            Physics.SyncTransforms();
            for (var i = 0; i < 80; i++)
            {
                owner.GetComponent<NetworkMovement>().SubmitInput(Vector2.up, false);
                MoveVirtual(Vector2.zero, (uint)(100 + i)); yield return new WaitForFixedUpdate();
                Assert.That(HorizontalGap, Is.GreaterThanOrEqualTo(0.99f));
                Assert.That(otherServer.transform.position.z, Is.LessThan(1001.65f));
            }
        }
    }
}
#endif
