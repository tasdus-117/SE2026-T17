using NUnit.Framework;
using SmallArena.Client;
using UnityEngine;
namespace SmallArena.Tests.PlayMode
{
    public sealed class Tv3IntentTests
    {
        [Test]
        public void Aim_UsesMuzzlePlaneAndWorldXZ()
        {
            Assert.That(MouseAim.TryDirection(new Ray(new Vector3(3, 10, 4), Vector3.down),
                Vector3.zero, out var aim), Is.True);
            Assert.That(Vector3.Distance(aim, new Vector3(.6f, 0, .8f)), Is.LessThan(.00001f));
        }
        [Test]
        public void Aim_RejectsParallelBehindZeroAndNonfinite()
        {
            foreach (var ray in new[] { new Ray(Vector3.up, Vector3.right),
                new Ray(Vector3.up, Vector3.up), new Ray(Vector3.up, Vector3.down),
                new Ray(new Vector3(float.NaN, 1, 0), Vector3.down) })
            {
                Assert.That(MouseAim.TryDirection(ray, Vector3.zero, out var aim), Is.False);
                Assert.That(aim, Is.EqualTo(Vector3.zero));
            }
            Assert.That(MouseAim.TryDirection(new Ray(Vector3.up, Vector3.down),
                new Vector3(float.PositiveInfinity, 0, 0), out _), Is.False);
        }
        [Test]
        public void HeldFire_RequiresReleaseAndNeverCatchesUpAfterLag()
        {
            var fire = new HeldFireCadence();
            Assert.That(fire.TryRequest(true, true, 0, .5), Is.False);
            fire.TryRequest(true, false, 0, .5);
            Assert.That(fire.TryRequest(true, true, 0, .5), Is.True);
            Assert.That(fire.TryRequest(true, true, .49, .5), Is.False);
            Assert.That(fire.TryRequest(true, true, 10, .5), Is.True);
            Assert.That(fire.TryRequest(true, true, 10, .5), Is.False);
            Assert.That(fire.TryRequest(true, true, 10.5, .5), Is.True);
        }
        [Test]
        public void HeldFire_BlockResetAndClockRollbackRequireFreshRelease()
        {
            var fire = new HeldFireCadence();
            fire.TryRequest(true, false, 0, 1);
            Assert.That(fire.TryRequest(true, true, 0, 1), Is.True);
            fire.TryRequest(false, false, 1, 1);
            Assert.That(fire.TryRequest(true, true, 2, 1), Is.False);
            fire.TryRequest(true, false, 2, 1);
            Assert.That(fire.TryRequest(true, true, 2, 1), Is.True);
            fire.Reset();
            Assert.That(fire.TryRequest(true, true, 3, 1), Is.False);
            fire.TryRequest(true, false, 3, 1);
            Assert.That(fire.TryRequest(true, true, 1, 1), Is.False);
        }
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(0)]
        [TestCase(-1)]
        public void HeldFire_InvalidCadenceFailsClosed(double interval)
        {
            var fire = new HeldFireCadence();
            fire.TryRequest(true, false, 0, interval);
            Assert.That(fire.TryRequest(true, true, 1, interval), Is.False);
        }
    }
}
