using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace SmallArena.Tests.PlayMode
{
    /// <summary>Read-only checks of imported project settings; no scene behavior is added.</summary>
    [TestFixture]
    public sealed class ArenaConfigurationTests
    {
        private static readonly string[] ArenaLayers = { "Environment", "Player", "Bot", "Pickup" };

        [TestCase("Environment")]
        [TestCase("Player")]
        [TestCase("Bot")]
        [TestCase("Pickup")]
        public void NamedLayer_ResolvesToPositiveIndex(string name)
        {
            var index = LayerMask.NameToLayer(name);

            Assert.That(index, Is.InRange(1, 31), name);
            Assert.That(LayerMask.LayerToName(index), Is.EqualTo(name));
            Assert.That(LayerMask.GetMask(name), Is.Not.EqualTo(0));
        }

        [Test]
        public void ArenaLayers_HaveDistinctIndices()
        {
            var indices = new HashSet<int>();
            foreach (var name in ArenaLayers)
            {
                Assert.That(indices.Add(ResolveLayer(name)), Is.True, name);
            }
        }

        [TestCase("Default", "Default", true)]
        [TestCase("Default", "Environment", true)]
        [TestCase("Default", "Player", true)]
        [TestCase("Default", "Bot", true)]
        [TestCase("Default", "Pickup", false)]
        [TestCase("Environment", "Environment", true)]
        [TestCase("Environment", "Player", true)]
        [TestCase("Environment", "Bot", true)]
        [TestCase("Environment", "Pickup", false)]
        [TestCase("Player", "Player", false)]
        [TestCase("Player", "Bot", true)]
        [TestCase("Player", "Pickup", true)]
        [TestCase("Bot", "Bot", false)]
        [TestCase("Bot", "Pickup", false)]
        [TestCase("Pickup", "Pickup", false)]
        public void CollisionPair_MatchesPolicyInBothDirections(string first, string second, bool enabled)
        {
            var firstLayer = ResolveLayer(first);
            var secondLayer = ResolveLayer(second);

            Assert.That(Physics.GetIgnoreLayerCollision(firstLayer, secondLayer), Is.EqualTo(!enabled));
            Assert.That(Physics.GetIgnoreLayerCollision(secondLayer, firstLayer), Is.EqualTo(!enabled));
        }

        [Test]
        public void ArenaLayers_IgnoreUnrelatedBuiltInAndUnusedLayers()
        {
            var participants = new HashSet<int> { ResolveLayer("Default") };
            foreach (var name in ArenaLayers)
            {
                participants.Add(ResolveLayer(name));
            }

            foreach (var name in ArenaLayers)
            {
                var layer = ResolveLayer(name);
                for (var other = 0; other < 32; other++)
                {
                    if (!participants.Contains(other))
                    {
                        Assert.That(Physics.GetIgnoreLayerCollision(layer, other), Is.True, name + " / " + other);
                    }
                }
            }
        }

        [Test]
        public void DefaultPipeline_IsAssignedUrp()
        {
            AssertUrp(GraphicsSettings.defaultRenderPipeline);
        }

        [Test]
        public void EveryQualityLevel_HasExplicitUrpOverride()
        {
            for (var quality = 0; quality < QualitySettings.names.Length; quality++)
            {
                AssertUrp(QualitySettings.GetRenderPipelineAssetAt(quality));
            }
        }

        [Test]
        public void ActivePipeline_IsUrp_AndMatchesSelectedSettings()
        {
            var selected = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.defaultRenderPipeline;

            AssertUrp(GraphicsSettings.currentRenderPipeline);
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(selected));
        }

        [Test]
        public void ActiveColorSpace_IsLinear()
        {
            Assert.That(QualitySettings.activeColorSpace, Is.EqualTo(ColorSpace.Linear));
        }

        private static int ResolveLayer(string name)
        {
            var index = LayerMask.NameToLayer(name);
            Assert.That(index, Is.InRange(0, 31), "Missing layer: " + name);
            return index;
        }

        private static void AssertUrp(RenderPipelineAsset pipeline)
        {
            Assert.That(pipeline, Is.Not.Null);
            // Avoid adding a URP implementation assembly dependency to the test asmdef.
            Assert.That(pipeline.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"));
        }
    }
}
