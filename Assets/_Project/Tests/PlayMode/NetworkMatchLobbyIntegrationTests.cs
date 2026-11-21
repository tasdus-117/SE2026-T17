#if UNITY_EDITOR
using System.Collections;
using Mirror;
using NUnit.Framework;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkMatchLobbyIntegrationTests : LobbyTestFixture
    {
        private IEnumerator StartReadyRoom()
        {
            yield return Host(); Connect(2); yield return Until(() => server.Room.Count == 2);
            ReadyRemote(2, true); view.ReadyButton.onClick.Invoke(); yield return Until(() => view.StartButton.interactable);
            manager.RequestRoomStart(); yield return Until(() => NetworkClient.localPlayer != null);
        }
        [UnityTest] public IEnumerator LobbySessionAndSlotsSurviveSceneLoad_AndWaitForEveryExpectedSpawn()
        {
            yield return StartReadyRoom();
            var runtime = manager.GetComponent<ServerMatchLifecycleController>();
            Assert.That(runtime, Is.Not.Null); Assert.That(runtime.Session.SessionId, Is.EqualTo(server.SessionId));
            Assert.That(runtime.Session.Phase, Is.EqualTo(MatchLifecyclePhase.WaitingForPlayers));
            Assert.That(NetworkClient.localPlayer.GetComponent<NetworkHealth>().CanAct, Is.False);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => runtime.Session.Phase == MatchLifecyclePhase.Countdown);
            var member = runtime.Session.ByConnection(2);
            var player = NetworkServer.connections[2].identity.GetComponent<NetworkHealth>();
            Assert.That(player.ParticipantId, Is.EqualTo(member.ParticipantId)); Assert.That(player.SlotIndex, Is.EqualTo(member.SlotIndex));
            Assert.That(player.LifeId, Is.EqualTo(1)); Assert.That(player.CurrentHealth, Is.EqualTo(10));
            yield return Until(() => runtime.Session.Phase == MatchLifecyclePhase.InProgress);
            Assert.That(player.CanAct, Is.True); Assert.That(runtime.Session.ClockRemaining(NetworkTime.localTime), Is.InRange(179, 180));
        }
        [UnityTest] public IEnumerator LobbyDisconnectBeforeGuestSpawnReleasesBindingAndAllowsCountdown()
        {
            yield return StartReadyRoom(); var runtime = manager.GetComponent<ServerMatchLifecycleController>();
            var connection = NetworkServer.connections[2]; var participant = runtime.Session.ByConnection(2);
            transport.DisconnectVirtual(2); yield return Until(() => runtime.Session.Phase == MatchLifecyclePhase.Countdown);
            Assert.That(runtime.Session.ByConnection(2), Is.Null); Assert.That(participant.LifeState, Is.EqualTo(ParticipantLifeState.Disconnected));
            Assert.That(runtime.Session.ActiveBindingCount, Is.EqualTo(1));
            manager.OnServerAddPlayer(connection); Assert.That(connection.identity, Is.Null);
            yield return Until(() => runtime.Session.Phase == MatchLifecyclePhase.InProgress);
            Assert.That(NetworkClient.localPlayer.GetComponent<NetworkHealth>().CanAct, Is.True);
        }
    }
}
#endif
