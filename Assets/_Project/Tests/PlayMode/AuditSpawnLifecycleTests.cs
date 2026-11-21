#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Mirror;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class AuditSpawnLifecycleTests : LobbyTestFixture
    {
        private ServerMatchLifecycleController lifecycle;
        private IEnumerator Waiting(bool third = false, double grace = 3)
        {
            yield return Host();
            Assert.That(manager.disconnectInactiveConnections, Is.False);
            lifecycle = manager.GetComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: 1,
                countdownDurationSeconds: .1, matchDurationSeconds: 30, respawnDelaySeconds: .1,
                spawnProtectionSeconds: .01, spawnGraceSeconds: grace),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"));
            Connect(2); if (third) Connect(3);
            yield return Until(() => server.Room.Count == (third ? 3 : 2));
            ReadyRemote(2, true); if (third) ReadyRemote(3, true);
            view.ReadyButton.onClick.Invoke(); yield return Until(() => view.StartButton.interactable);
            manager.RequestRoomStart(); yield return Until(() => NetworkClient.localPlayer != null);
            Assert.That(lifecycle.Session.Phase, Is.EqualTo(MatchLifecyclePhase.WaitingForPlayers));
        }
        private void Spawn(int id)
        { transport.SendFromVirtual(id, new ReadyMessage()); transport.SendFromVirtual(id, new AddPlayerMessage()); }
        private static void Place(NetworkIdentity identity, Vector3 point)
        { identity.GetComponent<NetworkTransformReliable>().ServerTeleport(point, Quaternion.identity); Physics.SyncTransforms(); }
        private IEnumerator Hit(int id)
        {
            var player = NetworkClient.localPlayer;
            Place(player, new Vector3(-3, 1.05f, -3));
            Place(NetworkServer.connections[id].identity, new Vector3(-3, 1.05f, -1.8f));
            player.GetComponent<NetworkPlayer>().RequestAttack(Vector3.forward);
            yield return null; yield return null;
        }
        [UnityTest] public IEnumerator F1_DelayedAddPlayerAndEarlyFall_PreserveHpResultsAndHudUntilBarrierRelease()
        {
            yield return Waiting(); var session = lifecycle.Session;
            var host = NetworkClient.localPlayer.GetComponent<NetworkHealth>();
            Place(host.netIdentity, new Vector3(-3, -6, -3));
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(host.CurrentHealth, Is.EqualTo(10)); Assert.That(host.Result, Is.EqualTo(PlayerMatchResult.Playing));
            Assert.That(session.Participants.Sum(p => p.Deaths), Is.Zero); Assert.That(session.WinnerParticipantId, Is.Empty);
            Assert.That(host.CanAct, Is.False); Assert.That(NetworkServer.connections[2].identity, Is.Null);
            Assert.That(Object.FindAnyObjectByType<MatchResultHudView>().IsVisible, Is.False);
            Assert.That(lifecycle.ReplicatedState.Phase, Is.EqualTo(MatchLifecyclePhase.WaitingForPlayers));
            Spawn(2);
            yield return Until(() => host.LifeId == 2 && host.CurrentHealth == 10);
            Assert.That(session.ByConnection(0).Deaths, Is.EqualTo(1));
            Assert.That(session.Participants.Sum(p => p.Kills), Is.Zero);
            Assert.That(session.Phase, Is.EqualTo(MatchLifecyclePhase.InProgress));
            Assert.That(host.Result, Is.EqualTo(PlayerMatchResult.Playing));
            Assert.That(NetworkServer.connections[2].identity.GetComponent<NetworkHealth>().Result, Is.EqualTo(PlayerMatchResult.Playing));
            Assert.That(lifecycle.ReplicatedState.Scores.Single(p => p.ParticipantId == host.ParticipantId).Deaths, Is.EqualTo(1));
            yield return null; Assert.That(Object.FindAnyObjectByType<MatchResultHudView>().IsVisible, Is.False);
        }
        [UnityTest] public IEnumerator F2_LegitimateDelayedSpawnWithinGrace_ReleasesOneSharedGameplayBarrier()
        {
            yield return Waiting(); yield return new WaitForSeconds(.15f);
            Assert.That(lifecycle.Session.Phase, Is.EqualTo(MatchLifecyclePhase.WaitingForPlayers));
            Spawn(2); yield return Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.InProgress);
            Assert.That(NetworkServer.connections[0].identity.transform.position, Is.Not.EqualTo(NetworkServer.connections[2].identity.transform.position));
            yield return Hit(2);
            Assert.That(NetworkServer.connections[2].identity.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(9));
        }
        [UnityTest] public IEnumerator F2_StalledSpawnTimesOut_TwoSpawnedPlayersContinueAndCanAttack()
        {
            yield return Waiting(third: true, grace: 1.5); var session = lifecycle.Session; var stalled = NetworkServer.connections[2];
            Spawn(3); yield return Until(() => !NetworkServer.connections.ContainsKey(2) && session.Phase == MatchLifecyclePhase.InProgress);
            Assert.That(server.Room.Count, Is.EqualTo(2)); Assert.That(manager.CurrentRoom.Members.Length, Is.EqualTo(2));
            Assert.That(session.ByConnection(2), Is.Null); Assert.That(session.ByParticipant(session.SessionId + "/1").LifeState, Is.EqualTo(ParticipantLifeState.Disconnected));
            Assert.That(session.Participants.Sum(p => p.Deaths + p.Kills), Is.Zero);
            manager.OnServerAddPlayer(stalled); Assert.That(stalled.identity, Is.Null);
            yield return Hit(3); Assert.That(NetworkServer.connections[3].identity.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(9));
            Assert.That(session.WinnerParticipantId, Is.Empty);
        }
        [UnityTest] public IEnumerator F2_StalledSpawnWithOnlyOneSurvivor_AbortsToRetainedLobbyWithoutScores()
        {
            yield return Waiting(grace: .5); var session = lifecycle.Session; var host = manager;
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && server.Room.Phase == LobbyPhase.Waiting);
            Assert.That(NetworkManager.singleton, Is.SameAs(host)); Assert.That(NetworkServer.active, Is.True);
            Assert.That(server.Room.Count, Is.EqualTo(1)); Assert.That(server.Room.Members[0].IsReady, Is.False);
            Assert.That(session.Match.State, Is.EqualTo(OnlineMatchState.Aborted)); Assert.That(session.WinnerParticipantId, Is.Empty);
            Assert.That(session.Participants.Sum(p => p.Kills + p.Deaths + p.DamageDealt), Is.Zero);
            Assert.That(session.ActiveBindingCount, Is.Zero); Assert.That(NetworkServer.spawned.Count, Is.Zero);
            Connect(2); yield return Until(() => server.Room.Count == 2);
            Assert.That(server.Room.Members.Single(p => p.ConnectionId == 2).SlotIndex, Is.EqualTo(1));
            Assert.That(server.Room.Members.All(p => !p.IsReady), Is.True);
        }
        [UnityTest] public IEnumerator F2_MidLoadDisconnect_AbortsInsufficientRosterAndRejectsLateSpawn()
        {
            yield return Waiting(); var session = lifecycle.Session; var old = NetworkServer.connections[2];
            transport.DisconnectVirtual(2);
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && server.Room.Phase == LobbyPhase.Waiting);
            Assert.That(session.Match.State, Is.EqualTo(OnlineMatchState.Aborted)); Assert.That(session.WinnerParticipantId, Is.Empty);
            manager.OnServerAddPlayer(old); Assert.That(old.identity, Is.Null); Assert.That(NetworkServer.spawned.Count, Is.Zero);
            Assert.That(server.Room.Members.Length, Is.EqualTo(1)); Assert.That(server.Room.Members[0].IsReady, Is.False);
        }
        [UnityTest] public IEnumerator F2_RejectedNonRosterConnectionCannotBlockActiveParticipantsCombat()
        {
            yield return Waiting(); Spawn(2); yield return Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.InProgress);
            Connect(3); yield return Until(() => transport.RejectionTo(3) != null);
            Assert.That(lifecycle.Session.ByConnection(3), Is.Null);
            yield return Hit(2);
            Assert.That(NetworkServer.connections[2].identity.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(9));
            Assert.That(lifecycle.Session.ActiveBindingCount, Is.EqualTo(2));
        }
    }

    public sealed class AuditFallReplicaTests : NetworkGameplayFixture
    {
        [UnityTest] public IEnumerator F1_EarlyFallAndDelayedSpawn_ReplicateProtectedHpAndNormalRespawnToNativeClient()
        {
            yield return StartWorld(spawnSecond: false);
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.layer = 6;
            floor.transform.position = new Vector3(1000, -.5f, 1000); floor.transform.localScale = new Vector3(20, 1, 20);
            var markers = NetworkManager.startPositions.Where(t => t.name == "Combat start").ToArray();
            markers[0].position = new Vector3(997, 1, 997); markers[1].position = new Vector3(1003, 1, 1003);
            Physics.SyncTransforms();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab");
            NetworkClient.RegisterPrefab(prefab);
            var runtime = manager.gameObject.AddComponent<ServerMatchLifecycleController>();
            runtime.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, countdownDurationSeconds: .1,
                respawnDelaySeconds: .3, spawnProtectionSeconds: .01), prefab, floor.GetComponent<BoxCollider>());
            runtime.BeginSession("fall-replica", new[] { new LobbyMember { ConnectionId = 1, SlotIndex = 0 }, new LobbyMember { ConnectionId = 2, SlotIndex = 1 } });
            var authoritative = ownerServer.GetComponent<NetworkHealth>();
            ownerServer.GetComponent<NetworkTransformReliable>().ServerTeleport(new Vector3(1000, -6, 1000), Quaternion.identity);
            yield return Until(() => owner.IsLifecycleBound && NetworkClient.spawned.Values.Any(i => i.GetComponent<NetworkMatchController>() != null));
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            var proxy = NetworkClient.spawned.Values.Select(i => i.GetComponent<NetworkMatchController>()).First(i => i != null);
            Assert.That(owner, Is.Not.SameAs(authoritative)); Assert.That(proxy, Is.Not.SameAs(runtime.ReplicatedState));
            Assert.That(authoritative.CurrentHealth, Is.EqualTo(10)); Assert.That(owner.CurrentHealth, Is.EqualTo(10));
            Assert.That(owner.Result, Is.EqualTo(PlayerMatchResult.Playing)); Assert.That(proxy.Phase, Is.EqualTo(MatchLifecyclePhase.WaitingForPlayers));
            Assert.That(hud.GetComponent<MatchResultHudView>().IsVisible, Is.False);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => owner.CurrentHealth == 0);
            yield return Until(() => owner.LifeId == 2 && owner.CurrentHealth == 10 && proxy.Scores.Any(s => s.Deaths == 1));
            Assert.That(authoritative.LifeId, Is.EqualTo(owner.LifeId)); Assert.That(owner.Result, Is.EqualTo(PlayerMatchResult.Playing));
            Assert.That(proxy.WinnerParticipantId, Is.Empty); Assert.That(proxy.Scores.Sum(s => s.Kills), Is.Zero);
            Assert.That(hud.GetComponent<MatchResultHudView>().IsVisible, Is.False);
        }
    }
}
#endif
