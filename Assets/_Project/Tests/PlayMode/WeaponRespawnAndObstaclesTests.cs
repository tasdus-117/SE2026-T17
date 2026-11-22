#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Mirror.RemoteCalls;
using NUnit.Framework;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Combat;
using SmallArena.Server.Sessions;
using SmallArena.Server.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    public sealed class WeaponRespawnAndObstaclesTests : LobbyTestFixture
    {
        private ServerMatchLifecycleController lifecycle;
        private ServerLootRespawnController loot;
        private NetworkHealth host, guest;
        private IEnumerator Begin(double delay = 30, double duration = 90)
        {
            yield return Host();
            lifecycle = manager.GetComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: 1,
                countdownDurationSeconds: .2, matchDurationSeconds: duration, respawnDelaySeconds: .08,
                spawnProtectionSeconds: .01, weaponRespawnDelaySeconds: delay),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"));
            Connect(2); yield return Until(() => server.Room.Count == 2); ReadyRemote(2, true); view.ReadyButton.onClick.Invoke();
            yield return Until(() => view.StartButton.interactable); manager.RequestRoomStart();
            yield return Until(() => NetworkClient.localPlayer != null);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            loot = manager.GetComponent<ServerLootRespawnController>(); Assert.That(loot, Is.Not.Null);
            yield return Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.InProgress && loot.MapItem != null);
            host = NetworkClient.localPlayer.GetComponent<NetworkHealth>(); guest = NetworkServer.connections[2].identity.GetComponent<NetworkHealth>();
            Assert.That(lifecycle.ArenaFloor, Is.Not.Null); Assert.That(lifecycle.Session.Configuration.MaxHealth, Is.EqualTo(10));
        }
        private static void Place(NetworkHealth player, Vector3 position)
        {
            var body = player.GetComponent<CharacterController>(); body.enabled = false;
            player.GetComponent<ServerPlayerMovement>().ClearRuntimeInput();
            player.GetComponent<ArenaNetworkTransform>().ServerTeleport(position, Quaternion.identity);
            body.enabled = true; Physics.SyncTransforms();
        }
        private IEnumerator Collect()
        {
            yield return Until(() => host.CanAct && loot.MapItem != null);
            var item = loot.MapItem; var id = item.netId;
            Place(host, item.transform.position + Vector3.up * .74f);
            host.GetComponent<NetworkPlayer>().RequestPickup(id);
            yield return Until(() => loot.State == LootLifecycleState.CarriedByPlayer && !NetworkServer.spawned.ContainsKey(id));
            Assert.That(loot.CarrierId, Is.EqualTo(host.ParticipantId));
            Assert.That(host.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(2));
        }
        private void GuestAttack()
        {
            ushort hash = 0; bool found = false;
            for (var n = 0; n <= ushort.MaxValue; n++)
                if (RemoteProcedureCalls.GetDelegate((ushort)n)?.Method.Name.Contains("CmdPerformAttack") == true)
                { hash = (ushort)n; found = true; break; }
            Assert.That(found, Is.True);
            using (var writer = NetworkWriterPool.Get())
            {
                writer.WriteVector3(Vector3.forward); writer.WriteVarLong(guest.LifeId);
                transport.SendFromVirtual(2, new CommandMessage { netId = guest.netId,
                    componentIndex = guest.GetComponent<NetworkPlayer>().ComponentIndex, functionHash = hash, payload = writer.ToArraySegment() });
            }
        }
        private void AssertPlacement()
        {
            var p = loot.MapItem.transform.position; var floor = lifecycle.ArenaFloor; var bounds = floor.bounds;
            Assert.That(p.x, Is.GreaterThan(bounds.min.x + 1).And.LessThan(bounds.max.x - 1));
            Assert.That(p.z, Is.GreaterThan(bounds.min.z + 1).And.LessThan(bounds.max.z - 1));
            Assert.That(Physics.Raycast(p + Vector3.up * 3, Vector3.down, out var hit, 5, 1 << 6), Is.True);
            Assert.That(hit.collider, Is.SameAs(floor)); Assert.That(p.y - hit.point.y, Is.EqualTo(.3f).Within(.001f));
            Assert.That(Physics.CheckBox(new Vector3(p.x, hit.point.y + .94f, p.z), Vector3.one * .8f,
                Quaternion.identity, (1 << 6) | (1 << 7), QueryTriggerInteraction.Ignore), Is.False);
            Func<Vector3, double> score = point => loot.LastPlayers.Count == 0 ? 0 : loot.LastPlayers.Min(player =>
                (double)(point.x - player.x) * (point.x - player.x) + (double)(point.z - player.z) * (point.z - player.z));
            Assert.That(score(p), Is.EqualTo(loot.LastCandidates.Max(score)).Within(.001));
            foreach (var player in loot.LastPlayers) Assert.That(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(player.x, player.z)), Is.GreaterThanOrEqualTo(2.5f));
        }
        [UnityTest] public IEnumerator CarrierKilledByOpponent_WaitsRealThirtySeconds_ThenReplicatesFairLoot()
        {
            yield return Begin(); AssertPlacement(); yield return Collect();
            Place(guest, host.transform.position + Vector3.back * 1.1f);
            Assert.That(host.TakeDamage(9), Is.True); var beforeDeath = NetworkTime.localTime; GuestAttack();
            yield return Until(() => loot.State == LootLifecycleState.RespawningAfterDeath);
            Assert.That(loot.RemainingSeconds, Is.GreaterThan(29)); Assert.That(host.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(1));
            yield return Until(() => host.LifeId == 2 && host.CanAct);
            Assert.That(host.CurrentHealth, Is.EqualTo(10)); Assert.That(host.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(1));
            while (NetworkTime.localTime - beforeDeath < 29.9) { Assert.That(loot.MapItem == null, Is.True); yield return null; }
            yield return Until(() => loot.MapItem != null);
            Assert.That(NetworkTime.localTime - beforeDeath, Is.GreaterThanOrEqualTo(30)); AssertPlacement();
            var id = loot.MapItem.netId; yield return Until(() => transport.VisibleTo(2).Contains(id));
        }
        [UnityTest] public IEnumerator TwentyActualRespawns_OnRealArena_StayClearAndMaximinFair()
        {
            yield return Begin(.03, 120); var ids = new HashSet<uint>(); var positions = new HashSet<Vector3>();
            for (var round = 0; round < 20; round++)
            {
                AssertPlacement(); ids.Add(loot.MapItem.netId); positions.Add(loot.MapItem.transform.position);
                yield return Collect();
                yield return Until(() => lifecycle.Session.ByParticipant(host.ParticipantId).Character.CanReceiveDamage(NetworkTime.localTime));
                Assert.That(host.TakeDamage(10), Is.True); yield return Until(() => loot.MapItem != null);
                Assert.That(loot.State, Is.EqualTo(LootLifecycleState.AvailableOnMap));
            }
            AssertPlacement(); Assert.That(ids.Count, Is.EqualTo(20)); Assert.That(positions.Count, Is.GreaterThan(10));
        }
        [UnityTest] public IEnumerator NonCarrierDisconnectDoesNotStartTimer_AndHostTeardownCancelsLease()
        {
            yield return Begin(.3); var mapId = loot.MapItem.netId;
            transport.DisconnectVirtual(2); yield return Until(() => !NetworkServer.connections.ContainsKey(2));
            Assert.That(loot.State, Is.EqualTo(LootLifecycleState.AvailableOnMap)); Assert.That(loot.MapItem.netId, Is.EqualTo(mapId));
            yield return Collect(); // Host carrier leaves: the session ends and no respawn is allowed.
            manager.StopHost(); yield return null;
            Assert.That(loot.State, Is.EqualTo(LootLifecycleState.Cancelled)); Assert.That(loot.MapItem == null, Is.True);
        }
        [UnityTest] public IEnumerator GuestCarrierDisconnectStartsRespawnWhileHostContinues()
        {
            yield return Begin(.3); var item = loot.MapItem;
            Place(guest, item.transform.position + Vector3.up * .74f);
            Assert.That(item.TryCollectServer(guest.GetComponent<NetworkPlayer>()), Is.True);
            Assert.That(loot.CarrierId, Is.EqualTo(guest.ParticipantId));
            transport.DisconnectVirtual(2);
            yield return Until(() => loot.State == LootLifecycleState.RespawningAfterDeath);
            Assert.That(loot.RemainingSeconds, Is.GreaterThan(0)); Assert.That(host.CanAct, Is.True);
            yield return Until(() => loot.MapItem != null); AssertPlacement();
            Assert.That(loot.CarrierId, Is.Null); Assert.That(host.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator MatchEndCancelsPendingRespawn_AndDestroysNoExtraLoot()
        {
            yield return Begin(30, 1.5); yield return Collect(); Assert.That(host.TakeDamage(10), Is.True);
            yield return Until(() => loot.State == LootLifecycleState.RespawningAfterDeath);
            yield return Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.Ended);
            yield return null; Assert.That(loot.State, Is.EqualTo(LootLifecycleState.Cancelled));
            Assert.That(loot.MapItem == null, Is.True); Assert.That(NetworkServer.spawned.Values.Any(i => i.GetComponent<NetworkLootItem>() != null), Is.False);
        }
        [UnityTest] public IEnumerator BlockedArenaRetainsReadyTimer_AndRetriesAfterClearanceReturns()
        {
            yield return Begin(.03, 20); yield return Collect(); Assert.That(host.TakeDamage(10), Is.True);
            var bounds = lifecycle.ArenaFloor.bounds;
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.layer = 6;
            blocker.transform.position = new Vector3(bounds.center.x, bounds.max.y + 1.5f, bounds.center.z);
            blocker.transform.localScale = new Vector3(bounds.size.x, 3, bounds.size.z); Physics.SyncTransforms();
            var start = NetworkTime.localTime; yield return Until(() => NetworkTime.localTime >= start + 1);
            Assert.That(loot.MapItem == null, Is.True); Assert.That(loot.State, Is.EqualTo(LootLifecycleState.RespawningAfterDeath));
            Assert.That(loot.RemainingSeconds, Is.Zero); Assert.That(loot.LastCandidates.Count, Is.Zero);
            UnityEngine.Object.Destroy(blocker); yield return null;
            yield return Until(() => loot.MapItem != null); AssertPlacement();
        }
    }
}
#endif
