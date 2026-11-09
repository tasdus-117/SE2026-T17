using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    [TestFixture]
    public sealed class GameplayConfigurationTests
    {
        [Test]
        public void Defaults_MatchApprovedRulesV01_WithExplicitUnits()
        {
            var rules = new GameplayConfiguration();
            Assert.That(rules.Revision, Is.EqualTo("rules-v0.1"));
            Assert.That(rules.MaxHealth, Is.EqualTo(100));
            Assert.That(rules.ShotDamage, Is.EqualTo(25));
            Assert.That(rules.ShotCooldownSeconds, Is.EqualTo(0.5));
            Assert.That(rules.ShotRangeMetres, Is.EqualTo(30));
            Assert.That(rules.MaxHorizontalSpeedMetresPerSecond, Is.EqualTo(5));
            Assert.That(rules.ValidInputTimeoutSeconds, Is.EqualTo(0.25));
            Assert.That(rules.MatchDurationSeconds, Is.EqualTo(180));
            Assert.That(rules.RespawnDelaySeconds, Is.EqualTo(3));
            Assert.That(rules.MinimumSafeSpawnDistanceMetres, Is.EqualTo(5));
            Assert.That(rules.SpawnRetryIntervalSeconds, Is.EqualTo(0.5));
            Assert.That(rules.SpawnProtectionSeconds, Is.EqualTo(1));
            Assert.That(rules.ReconnectGraceSeconds, Is.EqualTo(15));
            Assert.That(GameplayConfiguration.LethalOpponentHitScore, Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("rules-v0.2")]
        [TestCase("RULES-v0.1")]
        public void UnknownRevision_IsRejected(string revision) =>
            Assert.Throws<ArgumentException>(() => new GameplayConfiguration(revision: revision));

        // Exercise every public numeric parameter independently, including all nonfinite values.
        private static IEnumerable<TestCaseData> InvalidNumericValues()
        {
            foreach (var parameter in typeof(GameplayConfiguration).GetConstructors()[0].GetParameters())
            {
                if (parameter.ParameterType == typeof(int))
                {
                    foreach (var value in new[] { int.MinValue, -1, 0 })
                        yield return new TestCaseData(parameter.Name, value).SetName("Reject_" + parameter.Name + "_" + value);
                }
                if (parameter.ParameterType != typeof(double)) continue;
                foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                    yield return new TestCaseData(parameter.Name, value).SetName("Reject_" + parameter.Name + "_" + value);
                if (parameter.Name != "maxHorizontalSpeedMetresPerSecond" && parameter.Name != "minimumSafeSpawnDistanceMetres")
                    yield return new TestCaseData(parameter.Name, 0.0).SetName("Reject_" + parameter.Name + "_Zero");
            }
        }

        [TestCaseSource(nameof(InvalidNumericValues))]
        public void InvalidNumericConfig_IsRejectedBeforeUse(string name, object value)
        {
            var constructor = typeof(GameplayConfiguration).GetConstructors()[0];
            var parameters = constructor.GetParameters();
            var arguments = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                arguments[i] = parameters[i].Name == name ? value : parameters[i].DefaultValue;
            var error = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(arguments));
            Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(((ArgumentOutOfRangeException)error.InnerException).ParamName, Is.EqualTo(name));
        }

        [Test]
        public void ZeroSpeedAndSafeDistance_AreAllowed_WithoutInventedTuningCaps()
        {
            var zero = new GameplayConfiguration(maxHorizontalSpeedMetresPerSecond: 0, minimumSafeSpawnDistanceMetres: 0);
            Assert.That(zero.MaxHorizontalSpeedMetresPerSecond, Is.Zero);
            Assert.That(zero.MinimumSafeSpawnDistanceMetres, Is.Zero);
            var large = new GameplayConfiguration(maxHealth: int.MaxValue, shotDamage: int.MaxValue,
                shotRangeMetres: double.MaxValue, matchDurationSeconds: double.Epsilon);
            Assert.That(large.MaxHealth, Is.EqualTo(int.MaxValue));
            Assert.That(large.ShotRangeMetres, Is.EqualTo(double.MaxValue));
            Assert.That(large.MatchDurationSeconds, Is.EqualTo(double.Epsilon));
        }

        [Test]
        public void MatchRetainsOneImmutableSnapshot_WhenNewMatchesUseDifferentTuning()
        {
            var rules = new GameplayConfiguration();
            var first = new OnlineMatch(rules);
            var second = new OnlineMatch(new GameplayConfiguration(maxHealth: 120));
            Assert.That(first.Configuration, Is.SameAs(rules));
            Assert.That(first.Configuration.MaxHealth, Is.EqualTo(100));
            Assert.That(second.Configuration.MaxHealth, Is.EqualTo(120));
            Assert.That(typeof(GameplayConfiguration).IsSealed, Is.True);
            foreach (var property in typeof(GameplayConfiguration).GetProperties())
                Assert.That(property.GetSetMethod(true), Is.Null, property.Name);
            Assert.That(typeof(OnlineMatch).GetProperty(nameof(OnlineMatch.Configuration)).GetSetMethod(true), Is.Null);
            Assert.Throws<ArgumentNullException>(() => new OnlineMatch(null));
        }
    }
}
