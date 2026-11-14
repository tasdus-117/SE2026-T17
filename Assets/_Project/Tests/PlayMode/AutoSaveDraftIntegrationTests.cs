#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SmallArena.Domain;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    public sealed class AutoSaveDraftIntegrationTests : LobbyTestFixture
    {
        [UnityTest]
        public IEnumerator TC15_LobbyJoinAndReady_AutomaticallySave_WithoutRestoringLiveAuthority()
        {
            yield return Host();
            var session = server.Drafts;
            yield return Until(() => session.LastSave.IsCompleted);
            var read = session.ReadAsync(); yield return Until(() => read.IsCompleted);
            var first = read.GetAwaiter().GetResult();
            Assert.That(first.Members.Count, Is.EqualTo(1));
            Connect(2); yield return Until(() => server.Room.Count == 2);
            view.ReadyButton.onClick.Invoke(); yield return Until(() => server.Room.Members.Single(m => m.IsHost).IsReady);
            yield return Until(() => session.LastSave.IsCompleted);
            session.LastSave.GetAwaiter().GetResult();
            read = session.ReadAsync(); yield return Until(() => read.IsCompleted);
            var latest = read.GetAwaiter().GetResult();
            Assert.That(latest.IsValid(), Is.True);
            Assert.That(latest.Revision, Is.GreaterThan(first.Revision));
            Assert.That(latest.Members.Count, Is.EqualTo(2));
            Assert.That(latest.Members.Single(m => m.IsHost).IsReady, Is.True);
            Assert.That(latest.Members.Single(m => !m.IsHost).IsReady, Is.False);
            view.LeaveButton.onClick.Invoke(); yield return Until(() => !Mirror.NetworkServer.active && !Mirror.NetworkClient.active);
            // Disconnect may itself publish a final roster change. Observe that valid newer archive,
            // rather than pinning an implementation-specific number of Publish callbacks.
            yield return Until(() => session.LastSave.IsCompleted);
            read = session.ReadAsync(); yield return Until(() => read.IsCompleted);
            var archived = read.GetAwaiter().GetResult();
            Assert.That(archived.Revision, Is.GreaterThanOrEqualTo(latest.Revision));
            view.HostButton.onClick.Invoke(); yield return Until(() => server.Room.Count == 1);
            Assert.That(server.Drafts.RoomId, Is.Not.EqualTo(session.RoomId));
            Assert.That(server.Room.Members[0].IsReady, Is.False, "Archival drafts must not grant live Ready/host/connection authority.");
            read = session.ReadAsync(); yield return Until(() => read.IsCompleted);
            Assert.That(read.GetAwaiter().GetResult().Revision, Is.EqualTo(archived.Revision));
        }
    }
}
#endif
