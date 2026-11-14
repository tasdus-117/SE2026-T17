using System;
using System.Linq;
using NUnit.Framework;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
#if !LOBBY_PORTABLE
using Mirror;
#endif

namespace SmallArena.Tests.PlayMode
{
    public sealed class LobbyMemberSearchTests
    {
        private static LobbyRoom Fixture()
        {
            var room = new LobbyRoom();
            room.Open();
            foreach (var id in new[] { 9, 0, 7, 2 })
            {
                Assert.That(room.TryJoin(id, id == 0), Is.True);
                if (id != 0) room.SetReady(id, true);
            }
            return room;
        }

        // RED seam: baseline has only Members; compile symbol explicitly selects old behavior.
        // It proves a missing ordering/page behavior, not an existing search wire request.
        private static LobbyMember[] Query(LobbyRoom room, int requester = 0, bool? ready = null,
            bool? host = null, int page = 0, int pageSize = 4)
        {
#if LOBBY_BASELINE
            return room.Members;
#else
            return room.SearchMembers(requester, ready, host, page, pageSize);
#endif
        }

        [Test]
        public void C01_OrderBeforePaging_TiesCrossPages()
        {
            var room = Fixture();
            Assert.That(Query(room, pageSize: 2).Select(m => m.ConnectionId), Is.EqualTo(new[] { 2, 7 }));
            Assert.That(Query(room, page: 1, pageSize: 2).Select(m => m.ConnectionId), Is.EqualTo(new[] { 9, 0 }));
        }

#if !LOBBY_BASELINE
        [Test]
        public void C02_DefaultsAndLegacyOrder_AreDistinct()
        {
            var room = Fixture();
            Assert.That(Query(room).Select(m => m.ConnectionId), Is.EqualTo(new[] { 2, 7, 9, 0 }));
            Assert.That(room.Members.Select(m => m.ConnectionId), Is.EqualTo(new[] { 0, 2, 7, 9 }));
        }

        [TestCase(true, new[] { 2, 7, 9 })]
        [TestCase(false, new[] { 0 })]
        public void C03_ReadyFilter(bool ready, int[] ids) =>
            Assert.That(Query(Fixture(), ready: ready).Select(m => m.ConnectionId), Is.EqualTo(ids));

        [Test]
        public void C04_HostAndCombinedFilters()
        {
            var room = Fixture();
            Assert.That(Query(room, host: true).Select(m => m.ConnectionId), Is.EqualTo(new[] { 0 }));
            Assert.That(Query(room, host: false).Select(m => m.ConnectionId), Is.EqualTo(new[] { 2, 7, 9 }));
            Assert.That(Query(room, ready: true, host: true), Is.Empty);
            Assert.That(Query(room, ready: false, host: true).Select(m => m.ConnectionId), Is.EqualTo(new[] { 0 }));
            Assert.That(Query(room, ready: false, host: true, pageSize: 1).Select(m => m.ConnectionId), Is.EqualTo(new[] { 0 }));
            Assert.That(Query(room, ready: true, page: 1, pageSize: 2).Select(m => m.ConnectionId), Is.EqualTo(new[] { 9 }));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void C05_SizeOutsideBounds_Throws(int size) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => Query(Fixture(), pageSize: size));

        [TestCase(1, new[] { 2 })]
        [TestCase(2, new[] { 2, 7 })]
        [TestCase(3, new[] { 2, 7, 9 })]
        [TestCase(4, new[] { 2, 7, 9, 0 })]
        public void C06_SizeWithinBounds(int size, int[] ids) =>
            Assert.That(Query(Fixture(), pageSize: size).Select(m => m.ConnectionId), Is.EqualTo(ids));

        [Test]
        public void C07_PageBoundsAndOverflow()
        {
            var room = Fixture();
            Assert.Throws<ArgumentOutOfRangeException>(() => Query(room, page: -1));
            Assert.That(Query(room, page: 0, pageSize: 2).Select(m => m.ConnectionId), Is.EqualTo(new[] { 2, 7 }));
            Assert.That(Query(room, page: 1, pageSize: 2).Select(m => m.ConnectionId), Is.EqualTo(new[] { 9, 0 }));
            Assert.That(Query(room, page: 2, pageSize: 2), Is.Empty);
            Assert.That(Query(room, page: 2, pageSize: 1).Select(m => m.ConnectionId), Is.EqualTo(new[] { 9 }));
            Assert.That(Query(room, page: 3, pageSize: 1).Select(m => m.ConnectionId), Is.EqualTo(new[] { 0 }));
            Assert.That(Query(room, page: 4, pageSize: 1), Is.Empty);
            Assert.That(Query(room, page: int.MaxValue), Is.Empty);
        }

        [Test]
        public void C08_UnknownAndRemovedMember_DeniedBeforeValidation()
        {
            var room = Fixture();
            Assert.Throws<UnauthorizedAccessException>(() => Query(room, requester: 99, page: -1, pageSize: 0));
            room.Remove(2);
            Assert.Throws<UnauthorizedAccessException>(() => Query(room, requester: 2));
            Assert.That(Query(room, requester: 7).Select(m => m.ConnectionId), Is.EqualTo(new[] { 7, 9, 0 }));
        }

        [Test]
        public void C09_OfflineLoadingPlaying_Denied()
        {
            Assert.Throws<UnauthorizedAccessException>(() => Query(new LobbyRoom()));
            var room = Fixture();
            room.SetReady(0, true);
            Assert.That(room.TryStart(0), Is.True);
            Assert.Throws<UnauthorizedAccessException>(() => Query(room));
            room.FinishLoading();
            Assert.Throws<UnauthorizedAccessException>(() => Query(room));
            room.Close();
            Assert.Throws<UnauthorizedAccessException>(() => Query(room));
        }

        [Test]
        public void C10_QueryFailure_PreservesOriginalSnapshotAndReadyStart()
        {
            var room = Fixture();
            Assert.Throws<ArgumentOutOfRangeException>(() => Query(room, pageSize: 5));
            // Exact original construction used by ServerLobbyController.Publish.
            var snapshot = new LobbySnapshot { Phase = room.Phase, LocalConnectionId = 0, Members = room.Members };
            Assert.That(snapshot.Phase, Is.EqualTo(LobbyPhase.Waiting));
            Assert.That(snapshot.LocalConnectionId, Is.Zero);
            Assert.That(snapshot.Members.Select(m => m.ConnectionId), Is.EqualTo(new[] { 0, 2, 7, 9 }));
            Assert.That(snapshot.Members.Select(m => m.IsReady), Is.EqualTo(new[] { false, true, true, true }));
            Assert.That(snapshot.Members.Select(m => m.IsHost), Is.EqualTo(new[] { true, false, false, false }));
            var ready = new LobbyReadyRequest { IsReady = true };
            Assert.That(room.SetReady(0, ready.IsReady), Is.True);
            Assert.That(room.SetReady(0, ready.IsReady), Is.False);
            Assert.That(room.TryStart(0), Is.True);
            Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Loading));
        }

        [Test]
        public void C11_ReadyGuestCannotStart_StateUnchanged()
        {
            var room = Fixture();
            room.SetReady(0, true);
            Assert.That(room.AllReady, Is.True);
            Assert.That(room.TryStart(2), Is.False, "A valid ready guest must not start the host's match.");
            Assert.That(room.Phase, Is.EqualTo(LobbyPhase.Waiting));
            Assert.That(room.TryStart(0), Is.True);
        }

        [Test]
        public void C12_ResultCopyAndChangedDataset()
        {
            var room = Fixture();
            var first = Query(room);
            first[0].ConnectionId = 500;
            Assert.That(Query(room).Select(m => m.ConnectionId), Is.EqualTo(new[] { 2, 7, 9, 0 }));
            room.SetReady(0, true);
            Assert.That(Query(room).Select(m => m.ConnectionId), Is.EqualTo(new[] { 0, 2, 7, 9 }));
        }

        [Test]
        public void C13_InsertionOrderProbe_100Seeds()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var random = new Random(seed);
                var ids = new[] { 0, 2, 7, 9 }.OrderBy(_ => random.Next()).ToArray();
                var room = new LobbyRoom(); room.Open();
                foreach (var id in ids) { room.TryJoin(id, id == 0); if (id != 0) room.SetReady(id, true); }
                Assert.That(Query(room).Select(m => m.ConnectionId), Is.EqualTo(new[] { 2, 7, 9, 0 }), "seed=" + seed);
            }
        }
#if !LOBBY_PORTABLE
        // Frozen decoder from a702d17 LobbyMessages field order and Mirror 96 WriteArray.
        // Deliberately does not use reader.Read<LobbySnapshot>() or ReadArray<LobbyMember>().
        [Test]
        public void C14_CurrentWriter_FrozenLegacyDecoder_AfterQueryFailure()
        {
            var room = Fixture();
            Assert.Throws<ArgumentOutOfRangeException>(() => Query(room, pageSize: 0));
            var legacy = new LobbySnapshot { Phase = room.Phase, LocalConnectionId = 0, Members = room.Members };
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(legacy);
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                {
                    Assert.That(reader.ReadByte(), Is.EqualTo((byte)LobbyPhase.Waiting));
                    Assert.That(reader.ReadVarInt(), Is.Zero);
                    Assert.That(Compression.DecompressVarUInt(reader), Is.EqualTo(5UL));
                    foreach (var id in new[] { 0, 2, 7, 9 })
                    {
                        Assert.That(reader.ReadVarInt(), Is.EqualTo(id));
                        Assert.That(reader.ReadBool(), Is.EqualTo(id != 0));
                        Assert.That(reader.ReadBool(), Is.EqualTo(id == 0));
                    }
                    Assert.That(reader.Remaining, Is.Zero);
                }
            }
        }
#endif
#endif
    }
}
