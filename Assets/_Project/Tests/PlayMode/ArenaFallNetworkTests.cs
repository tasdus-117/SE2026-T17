#if UNITY_EDITOR
using System.Collections;
using Mirror;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Networking;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    public sealed class ArenaFallNetworkTests : NetworkGameplayFixture
    {
        [UnityTest]
        public IEnumerator ServerFall_ReplicatesZeroHealthDefeatAndWinner_ExactlyOnce()
        {
            yield return StartWorld();
            var server = ownerServer.GetComponent<NetworkHealth>();
            server.TakeDamage(3);
            yield return Until(() => owner.CurrentHealth == 7);
            var deaths = 0;
            server.OnDeath += () => deaths++;
            ownerServer.transform.position = new Vector3(1000, ArenaObstacles.FallHeight - 1, 1000);
            yield return Until(() => owner.CurrentHealth == 0 && owner.Result == PlayerMatchResult.Defeat);
            Assert.That(server.CurrentHealth, Is.Zero);
            Assert.That(otherServer.GetComponent<NetworkHealth>().Result, Is.EqualTo(PlayerMatchResult.Victory));
            Assert.That(menu.GetComponent<InGameMenuView>().ResultVisible, Is.True);
            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(deaths, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ClientReplicaBelowBoundary_CannotApplyDamage()
        {
            yield return StartWorld();
            owner.transform.position = new Vector3(1000, ArenaObstacles.FallHeight - 1, 1000);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(owner.CurrentHealth, Is.EqualTo(10));
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(10));
            Assert.That(owner.Result, Is.EqualTo(PlayerMatchResult.Playing));
        }
    }
}
#endif
