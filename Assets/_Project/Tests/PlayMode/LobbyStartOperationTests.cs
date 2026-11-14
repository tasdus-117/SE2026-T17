#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using SmallArena.Networking;
using SmallArena.Server.Sessions;

namespace SmallArena.Tests.PlayMode
{
    // Reflection keeps the pre-patch tests compilable. Absence of the planned behavior
    // is an assertion failure, not a compiler/discovery failure.
    public sealed class LobbyStartOperationTests
    {
        private static object Operation(LobbyRoom room, Action load, Action changed, Action<Exception> failed)
        {
            var type = typeof(ServerLobbyController).Assembly.GetType("SmallArena.Server.Sessions.LobbyStartOperation");
            Assert.That(type, Is.Not.Null, "Start operation must expose guarded completion/failure at the production coordinator boundary.");
            return Activator.CreateInstance(type, room, load, changed, failed);
        }
        private static bool Call(object operation, string name, params object[] args)
            => (bool)operation.GetType().GetMethod(name).Invoke(operation, args);

        private static LobbyRoom ReadyRoom(int count = 2)
        {
            var room = new LobbyRoom(); room.Open();
            for (var id = 0; id < count; id++) { room.TryJoin(id, id == 0); room.SetReady(id, true); }
            return room;
        }

        [TestCase("host", "solo_ready", "single")]
        [TestCase("host", "two_partial", "burst")]
        [TestCase("host", "two_ready", "after_result")]
        [TestCase("guest", "two_partial", "single")]
        [TestCase("guest", "two_ready", "burst")]
        [TestCase("guest", "four_ready", "after_result")]
        [TestCase("unknown", "solo_ready", "burst")]
        [TestCase("unknown", "two_partial", "after_result")]
        [TestCase("unknown", "two_ready", "single")]
        [TestCase("host", "four_ready", "single")]
        [TestCase("unknown", "four_ready", "burst")]
        [TestCase("host", "solo_ready", "after_result")]
        public void T01_Pairwise_AuthorityRosterAndDelivery(string requester, string roster, string delivery)
        {
            var room = ReadyRoom(roster == "solo_ready" ? 1 : roster == "four_ready" ? 4 : 2);
            if (roster == "two_partial") room.SetReady(1, false);
            int boundary = 0, fakeLoads = 0, success = 0;
            var operation = Operation(room, () => { boundary++; fakeLoads++; },
                () => { if (room.Phase == LobbyPhase.Playing) success++; }, _ => Assert.Fail("Unexpected failure"));
            var sender = requester == "host" ? 0 : requester == "guest" ? 1 : 999;
            var accepted = requester == "host" && (roster == "two_ready" || roster == "four_ready");
            Assert.That(Call(operation, "TryStart", sender), Is.EqualTo(accepted));
            if (delivery == "burst") Assert.That(Call(operation, "TryStart", sender), Is.False);
            Call(operation, "Complete");
            if (delivery == "after_result") Assert.That(Call(operation, "TryStart", sender), Is.False);
            Assert.That(boundary, Is.EqualTo(accepted ? 1 : 0), "B: attempts including rejected retries");
            Assert.That(fakeLoads, Is.EqualTo(accepted ? 1 : 0), "L here is a fake dependency, not native loading");
            Assert.That(success, Is.EqualTo(accepted ? 1 : 0));
            Assert.That(room.Phase, Is.EqualTo(accepted ? LobbyPhase.Playing : LobbyPhase.Waiting));
        }

        [Test]
        public void T02_T03_T04_RetryWhileLoadingAndAfterTerminal_CountsBoundaryAndCompletion()
        {
            var room = ReadyRoom(); int boundary = 0, fakeLoads = 0, terminal = 0;
            var operation = Operation(room, () => { boundary++; fakeLoads++; },
                () => { if (room.Phase == LobbyPhase.Playing) terminal++; }, _ => Assert.Fail());
            Assert.That(Call(operation, "TryStart", 0), Is.True);
            Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Loading));
            Assert.That(Call(operation, "TryStart", 0), Is.False);
            Assert.That(Call(operation, "TryStart", 0), Is.False);
            Assert.That(boundary, Is.EqualTo(1));
            Assert.That(Call(operation, "Complete"), Is.True);
            Assert.That(Call(operation, "Complete"), Is.False);
            Assert.That(Call(operation, "TryStart", 0), Is.False);
            Assert.That(boundary, Is.EqualTo(1)); Assert.That(fakeLoads, Is.EqualTo(1));
            Assert.That(terminal, Is.EqualTo(1));
        }

        [Test]
        public void T05_FailureRetryAndLateCallback_OneAttemptZeroLoadsOneFailure()
        {
            var room = ReadyRoom(); int boundary = 0, failed = 0, terminal = 0;
            var operation = Operation(room, () => { boundary++; throw new InvalidOperationException("startup boundary failure"); },
                () => { if (room.Phase.ToString() == "Failed" || room.Phase == LobbyPhase.Playing) terminal++; },
                error => { Assert.That(error.Message, Is.EqualTo("startup boundary failure")); failed++; });
            Assert.That(Call(operation, "TryStart", 0), Is.True);
            Assert.That(room.Phase.ToString(), Is.EqualTo("Failed"));
            Assert.That(Call(operation, "TryStart", 0), Is.False);
            Assert.That(Call(operation, "Complete"), Is.False);
            // This unit test observes attempts/terminal only. Native zero-load evidence
            // comes from T05_RealMissingScene's SceneManager.sceneLoaded observer.
            Assert.That(boundary, Is.EqualTo(1));
            Assert.That(failed, Is.EqualTo(1)); Assert.That(terminal, Is.EqualTo(1));
            room.Close(); Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Offline));
        }

        [Test]
        public void T12_NewSessionCanStart_StaleCompletionWhileWaitingIsIgnored()
        {
            var room = ReadyRoom(); int attempts = 0;
            var old = Operation(room, () => attempts++, () => { }, _ => Assert.Fail());
            Call(old, "TryStart", 0); Call(old, "Complete"); room.Close(); room.Open();
            Assert.That(Call(old, "Complete"), Is.False);
            room.TryJoin(0, true); room.TryJoin(1, false); room.SetReady(0, true); room.SetReady(1, true);
            var next = Operation(room, () => attempts++, () => { }, _ => Assert.Fail());
            Assert.That(Call(next, "TryStart", 0), Is.True); Call(next, "Complete");
            Assert.That(attempts, Is.EqualTo(2)); Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Playing));
        }
    }
}
#endif
