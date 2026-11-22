using NUnit.Framework;
using SmallArena.Protocol;

namespace SmallArena.Tests.PlayMode
{
    public sealed class CombatAimTests
    {
        [TestCase(3f, 4f, 0.6f, 0.8f)]
        [TestCase(-3f, 4f, -0.6f, 0.8f)]
        [TestCase(0f, -2f, 0f, -1f)]
        [TestCase(float.MaxValue, float.MaxValue, 0.70710678f, 0.70710678f)]
        [TestCase(float.Epsilon, 0f, 1f, 0f)]
        public void HorizontalIntent_NormalizesWithoutOverflowOrUnderflow(float x, float z, float expectedX, float expectedZ)
        {
            Assert.That(CombatAim.TryNormalizeHorizontal(x, z, out var dx, out var dz), Is.True);
            Assert.That(dx, Is.EqualTo(expectedX).Within(0.000001f));
            Assert.That(dz, Is.EqualTo(expectedZ).Within(0.000001f));
        }

        [TestCase(0f, 0f)]
        [TestCase(float.NaN, 1f)]
        [TestCase(1f, float.NaN)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(1f, float.NegativeInfinity)]
        public void InvalidHorizontalIntent_HasNoDirection(float x, float z)
        {
            Assert.That(CombatAim.TryNormalizeHorizontal(x, z, out var dx, out var dz), Is.False);
            Assert.That(dx, Is.Zero);
            Assert.That(dz, Is.Zero);
        }

        [TestCase(0f, 0f, 1f, true)]
        [TestCase(0.1f, 0f, 1f, true)]
        [TestCase(0f, 0.1f, 1f, true)]
        [TestCase(0f, 0f, 0f, false)]
        [TestCase(0f, 0f, -1f, false)]
        [TestCase(1f, 0f, 0f, false)]
        [TestCase(0f, 0f, 10f, false)]
        [TestCase(float.MaxValue, 0f, 1f, false)]
        [TestCase(0f, float.NaN, 1f, false)]
        [TestCase(0f, 0f, float.PositiveInfinity, false)]
        public void LegacyMelee_PreservesExistingWireTolerance(float x, float y, float z, bool expected)
        {
            Assert.That(CombatAim.IsLegacyMeleeDirection(x, y, z), Is.EqualTo(expected));
        }
    }
}
