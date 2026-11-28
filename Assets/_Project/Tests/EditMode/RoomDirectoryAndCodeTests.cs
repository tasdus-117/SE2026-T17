using System;
using System.Linq;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class RoomDirectoryAndCodeTests
    {
        private double time;
        private RoomDirectory directory;
        private const string Id = "11111111111111111111111111111111";
        [SetUp] public void SetUp() { time = 0; directory = new RoomDirectory(() => time); }
        private void Publish(string code = "ABC234", int players = 1, bool visible = true,
            RoomDirectoryPhase phase = RoomDirectoryPhase.Lobby, long revision = 1, string id = Id)
            => directory.PublishRoom(new RoomListingInfo(code, "Room", "Host", players, phase, visible),
                new RoomResolvedEndpoint("127.0.0.1", 7777, code, id), revision);

        [Test] public void GeneratedCodesUseExactlySixUnambiguousSymbols()
        {
            for (var i = 0; i < 1024; i++)
            {
                var code = RoomCodeGenerator.Generate();
                Assert.That(code.Length, Is.EqualTo(6));
                Assert.That(code.All(c => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(c)), Is.True);
            }
        }
        [TestCase(null)] [TestCase("")] [TestCase("ABC23")] [TestCase("ABC2345")]
        [TestCase("OBC234")] [TestCase("0BC234")] [TestCase("IBC234")] [TestCase("1BC234")]
        [TestCase("abc234")] [TestCase("ABC 34")] [TestCase("ＡBC234")]
        public void InvalidCodesRejected(string code) => Assert.That(RoomCodeGenerator.IsValid(code), Is.False);
        [Test] public void NormalizeDoesNotTruncateOrRemoveInternalWhitespace()
        {
            Assert.That(RoomCodeGenerator.Normalize("  abc234\t"), Is.EqualTo("ABC234"));
            Assert.That(RoomCodeGenerator.Normalize(null), Is.Empty);
            Assert.That(RoomCodeGenerator.Normalize("ab c234"), Is.EqualTo("AB C234"));
        }
        [Test] public void CollisionPredicateIsBoundedAndRetried()
        {
            var calls = 0;
            var code = RoomCodeGenerator.Generate(_ => ++calls < 3);
            Assert.That(calls, Is.EqualTo(3)); Assert.That(RoomCodeGenerator.IsValid(code), Is.True);
            calls = 0;
            Assert.Throws<InvalidOperationException>(() => RoomCodeGenerator.Generate(_ => { calls++; return true; }, 4));
            Assert.That(calls, Is.EqualTo(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => RoomCodeGenerator.Generate(null, 0));
        }
        [Test] public void PublicDefaultAndPrivateCodeResolution()
        {
            Publish(); Publish("DEF567", visible: false, id: "22222222222222222222222222222222");
            Assert.That(directory.GetPublicRooms().Single().RoomCode, Is.EqualTo("ABC234"));
            Assert.That(directory.GetPublicRooms()[0].IsPublic, Is.True);
            Assert.That(directory.ResolveRoomCode("def567").Success, Is.True);
        }
        [Test] public void TtlUsesReceiptClockAndHeartbeatRenewsIt()
        {
            Publish(); time = 5.9; Publish(revision: 2); time = 6;
            Assert.That(directory.ResolveRoomCode("ABC234").Success, Is.True);
            time = 11.9;
            Assert.That(directory.GetPublicRooms(), Is.Empty);
            Assert.That(directory.ResolveRoomCode("ABC234").FailureReason, Is.EqualTo(RoomJoinFailureReason.RoomNotFound));
        }
        [Test] public void OldRevisionDoesNotRollbackOrExtendLease()
        {
            Publish(players: 3, revision: 2); time = 5; Publish(players: 1);
            Assert.That(directory.ResolveRoomCode("ABC234").Listing.CurrentPlayers, Is.EqualTo(3));
            time = 6; Assert.That(directory.GetPublicRooms(), Is.Empty);
        }
        [Test] public void RemovalIsInstanceScopedAndRejectsDelayedHeartbeat()
        {
            Publish(); Assert.That(directory.RemoveRoom(Id, 2), Is.True);
            Publish(); Assert.That(directory.GetPublicRooms(), Is.Empty);
            Publish(id: "22222222222222222222222222222222");
            directory.RemoveRoom(Id, 3);
            Assert.That(directory.GetPublicRooms().Count, Is.EqualTo(1));
        }
        [Test] public void ConflictingRoomCodeFailsClosedUntilOneClaimReleased()
        {
            Publish(); const string second = "22222222222222222222222222222222";
            Publish(id: second);
            Assert.That(directory.ResolveRoomCode("ABC234").FailureReason, Is.EqualTo(RoomJoinFailureReason.DirectoryUnavailable));
            Assert.That(directory.GetPublicRooms(), Is.Empty);
            Assert.That(directory.ClaimLost("ABC234", second), Is.True);
            Assert.That(directory.ClaimLost("ABC234", Id), Is.False);
            directory.RemoveRoom(second, 2);
            Assert.That(directory.ResolveRoomCode("ABC234").Success, Is.True);
        }
        [Test] public void RemovalBeforeFirstHeartbeatStillPreventsReplay()
        {
            directory.RemoveRoom(Id, 2); Publish();
            Assert.That(directory.GetPublicRooms(), Is.Empty);
        }
        [Test] public void ReceiptClockMustBeMonotonicAndFinite()
        {
            time = 2; Publish(); time = 1;
            Assert.Throws<InvalidOperationException>(() => directory.GetPublicRooms());
            time = double.NaN;
            Assert.Throws<InvalidOperationException>(() => directory.GetPublicRooms());
        }
        [TestCase("!!!", RoomJoinFailureReason.InvalidCodeFormat)]
        [TestCase("ZZZ999", RoomJoinFailureReason.RoomNotFound)]
        [TestCase(" abc234 ", RoomJoinFailureReason.None)]
        public void ResolveNormalizesAndReportsReason(string code, RoomJoinFailureReason reason)
        {
            Publish(); var result = directory.ResolveRoomCode(code);
            Assert.That(result.FailureReason, Is.EqualTo(reason));
            Assert.That(result.Endpoint == null, Is.EqualTo(reason != RoomJoinFailureReason.None));
        }
        [Test] public void PhasePrecedesCapacityAndFreshJoinIsRejected()
        {
            Publish(players: 4);
            Assert.That(directory.ResolveRoomCode("ABC234").FailureReason, Is.EqualTo(RoomJoinFailureReason.RoomFull));
            Publish(players: 4, phase: RoomDirectoryPhase.InMatch, revision: 2);
            Assert.That(directory.ResolveRoomCode("ABC234").FailureReason, Is.EqualTo(RoomJoinFailureReason.MatchAlreadyInProgress));
        }
        [Test] public void RejectsMalformedListingEndpointAndResolution()
        {
            Assert.Throws<ArgumentException>(() => new RoomListingInfo("ABC234", "\n", "Host", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomListingInfo("ABC234", "Room", "Host", 5));
            Assert.Throws<ArgumentException>(() => new RoomResolvedEndpoint("host", 0, "ABC234", Id));
            Assert.Throws<ArgumentException>(() => new RoomJoinResolution(RoomJoinFailureReason.None));
        }
    }
}
