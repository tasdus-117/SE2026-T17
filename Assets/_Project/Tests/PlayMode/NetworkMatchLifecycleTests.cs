#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Protocol;
using SmallArena.Server.Sessions;
using SmallArena.Server.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkMatchLifecycleTests : NetworkGameplayFixture
    {
        private ServerMatchLifecycleController lifecycle;
        private NetworkHealth ServerOwner => ownerServer.GetComponent<NetworkHealth>();
        private NetworkHealth ServerOther => otherServer.GetComponent<NetworkHealth>();
        private IEnumerator StartSession(int lives = 0, double duration = 8, double countdown = .5)
        {
            yield return StartWorld();
            NetworkClient.localPlayer.GetComponent<NetworkMovement>().SubmitInput(Vector2.zero, false);
            yield return Until(() => transport.MovementTemplate.HasValue);
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.layer = 6;
            floor.transform.position = new Vector3(1000, -.5f, 1000); floor.transform.localScale = new Vector3(20, 1, 20);
            var markers = NetworkManager.startPositions.Where(t => t.name == "Combat start").ToArray();
            markers[0].position = new Vector3(997, 1, 997); markers[1].position = new Vector3(1003, 1, 1003);
            Physics.SyncTransforms();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab");
            Assert.That(prefab, Is.Not.Null); NetworkClient.RegisterPrefab(prefab);
            lifecycle = manager.gameObject.AddComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: 1,
                matchDurationSeconds: duration, respawnDelaySeconds: .3, minimumSafeSpawnDistanceMetres: 1,
                spawnProtectionSeconds: .01, countdownDurationSeconds: countdown, maximumLives: lives), prefab, floor.GetComponent<BoxCollider>());
            lifecycle.BeginSession("native-session", new[] { new LobbyMember { ConnectionId = 1, SlotIndex = 0 }, new LobbyMember { ConnectionId = 2, SlotIndex = 1 } });
            yield return Until(() => owner.IsLifecycleBound && other.IsLifecycleBound && owner.MatchPhase == MatchLifecyclePhase.Countdown);
        }
        private IEnumerator Live() => Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.InProgress && owner.CanAct && other.CanAct);
        private void Move(uint sequence, long life, Vector2? direction = null)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                var intent = direction ?? Vector2.right;
                writer.Write(new MovementInput { Sequence = sequence, MoveX = intent.x, MoveZ = intent.y, LifeId = life });
                var message = transport.MovementTemplate.Value;
                message.netId = otherServer.netId; message.payload = writer.ToArraySegment(); transport.SendFromVirtual(2, message, Channels.Unreliable);
            }
        }
        [UnityTest] public IEnumerator CountdownRejectsAttack_ThenLiveAcceptsAttack()
        {
            yield return StartSession(); Assert.That(ServerOwner.CanAct, Is.False);
            AttackOwner(Vector3.forward); yield return null; yield return null;
            Assert.That(ServerOther.CurrentHealth, Is.EqualTo(10)); yield return Live();
            AttackOwner(Vector3.forward); yield return Until(() => other.CurrentHealth == 9);
        }
        [UnityTest] public IEnumerator CountdownRejectsPickupCommand_AndLiveAcceptsSameItem()
        {
            yield return StartSession(countdown: 1);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/NetworkLootItem.prefab");
            NetworkClient.RegisterPrefab(prefab);
            var item = Track(Object.Instantiate(prefab, ownerServer.transform.position, Quaternion.identity)); NetworkServer.Spawn(item);
            var id = item.GetComponent<NetworkIdentity>().netId;
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestPickup(id);
            yield return null; yield return null;
            Assert.That(item != null && !item.GetComponent<NetworkLootItem>().IsCollected, Is.True);
            Assert.That(ServerOwner.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(1));
            yield return Live(); NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestPickup(id);
            yield return Until(() => !NetworkServer.spawned.ContainsKey(id));
            Assert.That(ServerOwner.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator CountdownFreezesPhysics_AndLiveMovementReplicates()
        {
            yield return StartSession(countdown: 1);
            var movement = otherServer.GetComponent<ServerPlayerMovement>(); movement.enabled = true;
            var frozen = otherServer.transform.position;
            Move(1, ServerOther.LifeId); yield return new WaitForSeconds(.15f);
            Assert.That(otherServer.transform.position, Is.EqualTo(frozen));
            yield return Live();
            var before = otherServer.transform.position; Move(2, ServerOther.LifeId);
            yield return Until(() => otherServer.transform.position.x > before.x + .1f);
            yield return Until(() => other.transform.position.x > before.x + .05f);
            Assert.That(ownerServer.transform.position.x, Is.EqualTo(1000).Within(.01));
        }
        [UnityTest] public IEnumerator ClockReplicatesToSeparateNativeClientAndEndsFrozen()
        {
            yield return StartSession(duration: 1.2); yield return Live();
            yield return Until(() => NetworkClient.spawned.Values.Any(i => i.GetComponent<NetworkMatchController>() != null));
            var proxy = NetworkClient.spawned.Values.Select(i => i.GetComponent<NetworkMatchController>()).First(s => s != null);
            Assert.That(proxy, Is.Not.SameAs(lifecycle.ReplicatedState));
            yield return Until(() => proxy.Phase == MatchLifecyclePhase.InProgress && proxy.MatchClock < 1);
            Assert.That(proxy.MatchClock, Is.EqualTo(lifecycle.Session.ClockRemaining(NetworkTime.localTime)).Within(.25));
            yield return Until(() => lifecycle.Session.ClockRemaining(NetworkTime.localTime) < .15);
            Assert.That(ServerOwner.TakeDamage(10), Is.True); // Respawn deadline falls after the match deadline.
            yield return Until(() => proxy.Phase == MatchLifecyclePhase.Ended && owner.Result == PlayerMatchResult.Draw);
            yield return null;
            Assert.That(hud.GetComponent<MatchLifecycleHudView>().RespawnText, Is.Empty);
            Assert.That(ServerOwner.CurrentHealth, Is.Zero, "No resurrection after the match deadline.");
            Assert.That(ServerOwner.CanAct, Is.False); Assert.That(ServerOwner.TakeDamage(1), Is.False);
            Assert.That(menu.GetComponent<InGameMenuView>().ResultHeader, Is.EqualTo("MATCH ENDED"));
        }
        [UnityTest] public IEnumerator LethalHitRespawnsWithFullHealthNewLifeAndResetBuff()
        {
            yield return StartSession(); yield return Live();
            var lootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/NetworkLootItem.prefab");
            NetworkClient.RegisterPrefab(lootPrefab);
            var loot = Track(Object.Instantiate(lootPrefab, otherServer.transform.position, Quaternion.identity)); NetworkServer.Spawn(loot);
            Assert.That(loot.GetComponent<NetworkLootItem>().TryCollectServer(otherServer.GetComponent<NetworkPlayer>()), Is.True);
            Assert.That(ServerOther.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(2));
            ServerOther.TakeDamage(9); AttackOwner(Vector3.forward);
            yield return Until(() => other.LifeState == ParticipantLifeState.Respawning);
            Assert.That(other.Result, Is.EqualTo(PlayerMatchResult.Playing)); Assert.That(ServerOther.CanAct, Is.False);
            Assert.That(otherServer.GetComponent<CharacterController>().enabled, Is.False);
            Assert.That(ServerOther.GetComponent<NetworkCombat>().CurrentDamage, Is.EqualTo(1));
            AttackVirtual(Vector3.forward); Assert.That(ServerOwner.CurrentHealth, Is.EqualTo(10));
            yield return Until(() => other.LifeId == 2 && other.CanAct);
            Assert.That(other.CurrentHealth, Is.EqualTo(10)); Assert.That(otherServer.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(Vector3.Distance(otherServer.transform.position, new Vector3(1003, 1.04f, 1003)), Is.LessThan(.1f));
        }
        [UnityTest] public IEnumerator OldLifeCommandCannotAttackAfterRespawn()
        {
            yield return StartSession(); yield return Live(); var previous = ServerOther.LifeId;
            ServerOther.TakeDamage(10); yield return Until(() => ServerOther.LifeId > previous);
            Arrange(true); yield return new WaitForSeconds(.03f);
            using (var writer = NetworkWriterPool.Get())
            {
                writer.WriteVector3(Vector3.forward); writer.WriteVarLong(previous);
                var cmd = transport.AttackTemplate.Value; cmd.netId = otherServer.netId; cmd.payload = writer.ToArraySegment(); transport.SendFromVirtual(2, cmd);
            }
            yield return null; yield return null; Assert.That(ServerOwner.CurrentHealth, Is.EqualTo(10));
            AttackVirtual(Vector3.forward); yield return Until(() => owner.CurrentHealth == 9);
        }
        [UnityTest] public IEnumerator FiniteLifeEliminationBroadcastsVictoryAndFreezesWinner()
        {
            yield return StartSession(lives: 1); yield return Live(); ServerOther.TakeDamage(10);
            yield return Until(() => owner.Result == PlayerMatchResult.Victory && other.Result == PlayerMatchResult.Defeat);
            Assert.That(lifecycle.Session.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); Assert.That(ServerOwner.CanAct, Is.False);
            Assert.That(ServerOther.RemainingLives, Is.Zero);
        }
        [UnityTest] public IEnumerator DisconnectClearsBindingWithoutStoppingRemainingClock()
        {
            yield return StartSession(); yield return Live(); var session = lifecycle.Session;
            var connection = NetworkServer.connections[2]; var id = otherServer.netId; transport.DisconnectVirtual(2);
            yield return Until(() => !NetworkClient.spawned.ContainsKey(id));
            Assert.That(session.ByConnection(2), Is.Null); Assert.That(session.ByParticipant("native-session/1").LifeState, Is.EqualTo(ParticipantLifeState.Disconnected));
            var before = session.ClockRemaining(NetworkTime.localTime); yield return new WaitForSeconds(.1f);
            Assert.That(session.ClockRemaining(NetworkTime.localTime), Is.LessThan(before)); Assert.That(ServerOwner.CanAct, Is.True);
            manager.OnServerDisconnect(connection); Assert.That(session.ActiveBindingCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator LifecycleHudShowsClockPhaseAndRespawnCountdown()
        {
            yield return StartSession(countdown: 1); var view = hud.GetComponent<MatchLifecycleHudView>();
            yield return Until(() => view.PhaseText.StartsWith("STARTING IN")); Assert.That(view.ClockText, Is.EqualTo("00:08"));
            yield return Live(); ServerOwner.TakeDamage(10);
            yield return Until(() => view.RespawnText.StartsWith("Respawning in")); Assert.That(menu.IsPaused, Is.False);
            yield return Until(() => owner.LifeId == 2 && view.RespawnText == "");
        }
        [UnityTest] public IEnumerator StopServerClearsSessionBindingsAndStateIdentity()
        {
            yield return StartSession(); var session = lifecycle.Session; manager.StopClient(); transport.Pump(); manager.StopServer();
            yield return null; Assert.That(lifecycle.Session, Is.Null); Assert.That(session.ActiveBindingCount, Is.Zero);
            Assert.That(NetworkServer.spawned.Count, Is.Zero);
        }
        [UnityTest] public IEnumerator FiveMovingDeaths_RespawnGroundedInsideFloor_WithMatchingClientPoses()
        {
            yield return StartSession(duration: 30); yield return Live();
            var simulation = otherServer.GetComponent<ServerPlayerMovement>(); simulation.enabled = true;
            var body = otherServer.GetComponent<CharacterController>();
            uint sequence = 0;
            for (var cycle = 0; cycle < 5; cycle++)
            {
                yield return Until(() => lifecycle.Session.ByConnection(2).Character.CanReceiveDamage(NetworkTime.localTime));
                var oldLife = ServerOther.LifeId; Move(++sequence, oldLife); yield return new WaitForFixedUpdate();
                Assert.That(ServerOther.TakeDamage(10), Is.True);
                Assert.That(body.enabled, Is.False); Assert.That(simulation.VerticalVelocity, Is.Zero);
                Assert.That(otherServer.GetComponent<NetworkTransformReliable>().enabled, Is.True);
                // An out-of-bounds corpse supplies old interpolation snapshots before the discontinuity.
                otherServer.transform.position = new Vector3(1015, -6, 1015); Physics.SyncTransforms();
                Move(++sequence, oldLife); yield return null; yield return null;
                yield return Until(() => ServerOther.LifeId == oldLife + 1 && body.isGrounded
                    && other.LifeId == ServerOther.LifeId && Vector3.Distance(other.transform.position, otherServer.transform.position) < .05f);
                Assert.That(body.enabled, Is.True);
                Assert.That(otherServer.transform.position.x, Is.InRange(990.5f, 1009.5f));
                Assert.That(otherServer.transform.position.z, Is.InRange(990.5f, 1009.5f));
                Assert.That(otherServer.transform.position.y, Is.InRange(.85f, 1.1f));
                Assert.That(other.GetComponent<CharacterController>().enabled, Is.False, "Clients never simulate collision, including their own avatar.");
                Assert.That(Vector3.Distance(owner.transform.position, ownerServer.transform.position), Is.LessThan(.05f));
                Move(++sequence, oldLife); yield return null; yield return null;
                Assert.That(simulation.AcceptedMove, Is.EqualTo(Vector2.zero), "Old-life packets cannot reactivate movement.");
            }
        }
        [UnityTest] public IEnumerator RespawnThenForwardMovement_IsBlockedByEnvironmentWall()
        {
            yield return StartSession(duration: 20); yield return Live();
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.layer = 6;
            wall.transform.position = new Vector3(1003, 1.5f, 1005); wall.transform.localScale = new Vector3(6, 3, 1);
            Physics.SyncTransforms();
            var simulation = otherServer.GetComponent<ServerPlayerMovement>(); simulation.enabled = true;
            Assert.That(ServerOther.TakeDamage(10), Is.True);
            yield return Until(() => other.LifeId == 2 && otherServer.GetComponent<CharacterController>().isGrounded);
            var start = otherServer.transform.position;
            for (uint step = 1; step <= 80; step++) { Move(step, 2, Vector2.up); yield return new WaitForFixedUpdate(); }
            yield return Until(() => Vector3.Distance(other.transform.position, otherServer.transform.position) < .05f);
            Assert.That(otherServer.transform.position.z, Is.GreaterThan(start.z + .2f).And.LessThanOrEqualTo(1004.05f));
            Assert.That(otherServer.transform.position.y, Is.InRange(.85f, 1.1f));
            Assert.That(otherServer.GetComponent<CharacterController>().isGrounded, Is.True);
            Assert.That(ServerOther.LifeId, Is.EqualTo(2), "No second void death after hitting the wall.");
        }
        [UnityTest] public IEnumerator UnsafeMarkersWait_AndHighMarkerSnapsOnlyToDesignatedArenaFloor()
        {
            yield return StartSession(duration: 20); yield return Live();
            var markers = NetworkManager.startPositions.Where(t => t.name == "Combat start").ToArray();
            markers[1].position = new Vector3(1012, 1, 1012);
            var outside = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); outside.layer = 6;
            outside.transform.position = new Vector3(1012, -.5f, 1012); outside.transform.localScale = new Vector3(4, 1, 4);
            var cover = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); cover.layer = 6;
            cover.transform.position = new Vector3(997, 1, 997); cover.transform.localScale = Vector3.one * 2;
            Physics.SyncTransforms(); Assert.That(ServerOther.TakeDamage(10), Is.True);
            var deadAt = NetworkTime.localTime;
            yield return Until(() => NetworkTime.localTime >= deadAt + 1);
            Assert.That(ServerOther.LifeId, Is.EqualTo(1)); Assert.That(ServerOther.LifeState, Is.EqualTo(ParticipantLifeState.Respawning));
            Assert.That(otherServer.GetComponent<CharacterController>().enabled, Is.False);
            markers[1].position = new Vector3(1003, 500, 1003); Physics.SyncTransforms();
            yield return Until(() => other.LifeId == 2 && Vector3.Distance(other.transform.position, otherServer.transform.position) < .05f);
            Assert.That(otherServer.transform.position.y, Is.InRange(1, 1.1f));
            Assert.That(otherServer.transform.position.x, Is.EqualTo(1003).Within(.01f));
            Assert.That(otherServer.transform.position.z, Is.EqualTo(1003).Within(.01f));
        }
        [UnityTest] public IEnumerator ReliableTeleport_PreservesDecodedPoseAcrossQueuedStateAndTeleportOrder()
        {
            yield return StartSession(); yield return Live();
            var type = ownerServer.GetComponent<NetworkTransformReliable>().GetType();
            var a = Track(new GameObject("Isolated authoritative serializer")); a.SetActive(false); a.AddComponent<NetworkIdentity>();
            var b = Track(new GameObject("Isolated observing decoder")); b.SetActive(false); b.AddComponent<NetworkIdentity>();
            var sender = (NetworkTransformReliable)a.AddComponent(type); sender.target = a.transform;
            var receiver = (NetworkTransformReliable)b.AddComponent(type); receiver.target = b.transform;
            a.SetActive(true); b.SetActive(true);
            receiver.syncDirection = SyncDirection.ServerToClient;
            typeof(NetworkIdentity).GetProperty("isClient").SetValue(b.GetComponent<NetworkIdentity>(), true);
            var teleport = type.GetMethod("OnTeleport", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Vector3), typeof(Quaternion) }, null);
            var origin = new Vector3(1000, 1, 1000); var destination = new Vector3(997, 1, 997);
            a.transform.position = origin;
            CopySnapshot(sender, receiver, true); yield return null;
            teleport.Invoke(sender, new object[] { destination, Quaternion.identity });
            // A queued state can be decoded while the receiver still has its pre-teleport baseline.
            CopySnapshot(sender, receiver, false); yield return null;
            teleport.Invoke(receiver, new object[] { destination, Quaternion.identity });
            a.transform.position = destination + Vector3.right * .2f;
            CopySnapshot(sender, receiver, false);
            var decoded = receiver.clientSnapshots.Values.Last().position;
            Assert.That(Vector3.Distance(decoded, a.transform.position), Is.LessThan(.002f), "Teleport must not change the wire delta origin.");
        }
        private static void CopySnapshot(NetworkTransformReliable sender, NetworkTransformReliable receiver, bool initial)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                sender.OnSerialize(writer, initial);
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment())) receiver.OnDeserialize(reader, initial);
            }
        }
    }
}
#endif
