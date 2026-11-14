using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SmallArena.Core;
using SmallArena.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    // FR-002 / AC-002: structural portions of TC-001 and TC-004 only.
    public sealed class ArenaLayoutTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Arena_MVP.unity";
        private const float Tolerance = 0.002f;
        private Scene arena;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            arena = SceneManager.GetSceneByPath(ScenePath);
            yield return null; // Start() commits the real offline lifecycle after scene activation.
            Assert.That(arena.IsValid() && arena.isLoaded, Is.True);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator UnloadArena()
        {
            if (arena.IsValid() && arena.isLoaded)
            {
                var cleanup = SceneManager.CreateScene("ArenaLayoutTests Cleanup");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(arena);
            }
        }

        [Test]
        public void Scene_LoadsWithNoMissingComponents()
        {
            Assert.That(arena.path, Is.EqualTo(ScenePath));
            foreach (var item in Objects())
            {
                Assert.That(item.GetComponents<Component>().All(component => component != null), Is.True, item.name);
            }
        }

        [Test]
        public void SceneCoordinator_IsWired_AndStartsReadyOfflineMatch()
        {
            var manager = Entity("_GameManager").GetComponent<GameManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.True);
            Assert.That(manager.ValidateMatchReadiness().IsReady, Is.True);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(manager.IsPlaying, Is.True);
        }

        [TestCase("Player", "Player")]
        [TestCase("Bot", "Bot")]
        [TestCase("WeaponPickup", "Pickup")]
        public void Entity_CountIsExactlyOne_AndUsesNamedLayer(string name, string layer)
        {
            var objects = Objects();
            Assert.That(objects.Count(item => item.name == name), Is.EqualTo(1));
            Assert.That(objects.Count(item => item.layer == Layer(layer)), Is.EqualTo(1));
            Assert.That(Entity(name).layer, Is.EqualTo(Layer(layer)));
        }

        [Test]
        public void Floor_IsSolid16By16_WithSurfaceAtZero()
        {
            var floor = Entity("Floor").GetComponent<BoxCollider>();
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.enabled && !floor.isTrigger, Is.True);
            Assert.That(floor.gameObject.layer, Is.EqualTo(Layer("Environment")));
            Near(floor.bounds.min.x, -8);
            Near(floor.bounds.max.x, 8);
            Near(floor.bounds.min.z, -8);
            Near(floor.bounds.max.z, 8);
            Near(floor.bounds.max.y, 0);
        }

        [TestCase("WallNorth")]
        [TestCase("WallSouth")]
        [TestCase("WallEast")]
        [TestCase("WallWest")]
        public void Wall_IsSolidEnvironment_AtLeastThreeMetresHigh_OutsidePlayableArea(string name)
        {
            var wall = Entity(name).GetComponent<BoxCollider>();
            Assert.That(wall.enabled && !wall.isTrigger, Is.True);
            Assert.That(wall.gameObject.layer, Is.EqualTo(Layer("Environment")));
            Assert.That(wall.bounds.size.y, Is.GreaterThanOrEqualTo(3));
            Near(wall.bounds.min.y, 0);
            switch (name)
            {
                case "WallNorth": Near(wall.bounds.min.z, 8); AssertSpans(wall.bounds.min.x, wall.bounds.max.x); break;
                case "WallSouth": Near(wall.bounds.max.z, -8); AssertSpans(wall.bounds.min.x, wall.bounds.max.x); break;
                case "WallEast": Near(wall.bounds.min.x, 8); AssertSpans(wall.bounds.min.z, wall.bounds.max.z); break;
                case "WallWest": Near(wall.bounds.max.x, -8); AssertSpans(wall.bounds.min.z, wall.bounds.max.z); break;
            }
        }

        [Test]
        public void Environment_HasFloorFourWallsAndTwoCoverBlocks_WithOverlappingWallCorners()
        {
            var geometry = Entity("ArenaGeometry");
            var boxes = geometry.GetComponentsInChildren<BoxCollider>();
            Assert.That(boxes.Length, Is.EqualTo(7));
            Assert.That(geometry.GetComponentsInChildren<Transform>().All(item => item.gameObject.layer == Layer("Environment")), Is.True);
            Assert.That(geometry.GetComponentsInChildren<Rigidbody>(), Is.Empty);
            foreach (var northSouth in new[] { "WallNorth", "WallSouth" })
            {
                foreach (var eastWest in new[] { "WallEast", "WallWest" })
                {
                    Assert.That(BoundsOf(northSouth).Intersects(BoundsOf(eastWest)), Is.True, northSouth + " / " + eastWest);
                }
            }
        }

        [TestCase("CoverNorthWest", -4f, 2.5f)]
        [TestCase("CoverSouthEast", 4f, -2.5f)]
        public void Cover_IsSolidAndBlocksSight_WithoutClosingTheCentralLane(string name, float x, float z)
        {
            var cover = Entity(name);
            var collider = cover.GetComponent<BoxCollider>();
            Assert.That(collider.enabled && !collider.isTrigger, Is.True);
            Assert.That(cover.GetComponent<MeshRenderer>().enabled, Is.True);
            Near(collider.bounds.min.y, BoundsOf("Floor").max.y);

            var environment = LayerMask.GetMask("Environment");
            Assert.That(Physics.Linecast(new Vector3(x, 1, z - 2), new Vector3(x, 1, z + 2),
                out var hit, environment, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider, Is.SameAs(collider));
            Assert.That(Physics.CheckBox(new Vector3(0, 1, 0), new Vector3(1, 0.9f, 7),
                Quaternion.identity, environment, QueryTriggerInteraction.Ignore), Is.False);
        }

        [Test]
        public void Player_HasOnlyCharacterControllerCollision_WithRequestedTuning()
        {
            var player = Entity("Player");
            var controller = player.GetComponent<CharacterController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.enabled && !controller.isTrigger, Is.True);
            Assert.That(player.GetComponentsInChildren<Rigidbody>(), Is.Empty);
            Assert.That(player.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
            Near(controller.height, 2);
            Near(controller.radius, 0.5f);
            Near(controller.slopeLimit, 45);
            Near(controller.stepOffset, 0.3f);
            Near(controller.skinWidth, 0.08f);
            Assert.That(controller.minMoveDistance, Is.EqualTo(0.001f).Within(0.00001f));
        }

        [Test]
        public void Bot_HasSolidStaticCapsuleCollider()
        {
            var bot = Entity("Bot");
            var collider = bot.GetComponent<CapsuleCollider>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.enabled && !collider.isTrigger, Is.True);
            Assert.That(bot.GetComponentsInChildren<Rigidbody>(), Is.Empty);
            Near(collider.height, 2);
            Near(collider.radius, 0.5f);
        }

        [Test]
        public void Pickup_HasTriggerAndKinematicBody_WithoutGravity()
        {
            var pickup = Entity("WeaponPickup");
            var colliders = pickup.GetComponentsInChildren<Collider>();
            var body = pickup.GetComponent<Rigidbody>();
            Assert.That(colliders.Length, Is.EqualTo(1));
            Assert.That(colliders[0].enabled && colliders[0].isTrigger, Is.True);
            Assert.That(body, Is.Not.Null);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(body.useGravity, Is.False);
        }

        [TestCase("Player")]
        [TestCase("Bot")]
        [TestCase("WeaponPickup")]
        public void Entity_ContainsOnlyExpectedComponents(string name)
        {
            var allowed = new[] { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(CharacterController), typeof(CapsuleCollider), typeof(BoxCollider), typeof(Rigidbody) }.ToList();
            if (name == "Player")
            {
                allowed.Add(typeof(SmallArena.Input.PlayerInputHandler));
                allowed.Add(typeof(SmallArena.Client.PlayerController));
                allowed.Add(typeof(SmallArena.Player.CombatController));
            }
            if (name == "Bot")
            {
                allowed.Add(typeof(SmallArena.Server.Combat.Health));
                allowed.Add(typeof(SmallArena.Client.UI.OfflineHealthPresenter));
                allowed.AddRange(new[] { typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Slider), typeof(TMPro.TextMeshProUGUI),
                    typeof(SmallArena.Client.UI.NetworkHealthBarView), typeof(SmallArena.Client.UI.HealthBarBillboard) });
            }
            if (name == "WeaponPickup")
            {
                allowed.Add(typeof(SmallArena.Pickups.WeaponPickup));
                allowed.Add(typeof(SmallArena.Client.Presentation.PickupVisual));
            }
            foreach (var component in Entity(name).GetComponentsInChildren<Component>(true))
            {
                Assert.That(component, Is.Not.Null);
                Assert.That(allowed.Contains(component.GetType()), Is.True, name + ": " + component.GetType().Name);
            }
        }

        [TestCase("Player", 0, 1, -4)]
        [TestCase("Bot", 0, 1, 4)]
        [TestCase("WeaponPickup", -3, 0.3f, 0)]
        public void Spawn_IsInsideBounds_AndFlushWithSupportingFloor(string name, float x, float y, float z)
        {
            var entity = Entity(name);
            Assert.That(Vector3.Distance(entity.transform.position, new Vector3(x, y, z)), Is.LessThan(Tolerance));
            var bounds = BoundsOf(name);
            Assert.That(bounds.min.x, Is.GreaterThan(-8));
            Assert.That(bounds.max.x, Is.LessThan(8));
            Assert.That(bounds.min.z, Is.GreaterThan(-8));
            Assert.That(bounds.max.z, Is.LessThan(8));
            Near(bounds.min.y, BoundsOf("Floor").max.y);
            Assert.That(Physics.Raycast(new Vector3(bounds.center.x, bounds.min.y + 0.1f, bounds.center.z),
                Vector3.down, out var hit, 0.2f, LayerMask.GetMask("Environment"), QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider.gameObject.name, Is.EqualTo("Floor"));
        }

        [Test]
        public void Spawns_DoNotOverlapEachOtherOrArenaGeometry()
        {
            var names = new[] { "Player", "Bot", "WeaponPickup" };
            var barriers = Entity("ArenaGeometry").GetComponentsInChildren<BoxCollider>()
                .Where(collider => collider.gameObject.name != "Floor").ToArray();
            for (var first = 0; first < names.Length; first++)
            {
                for (var second = first + 1; second < names.Length; second++)
                    Assert.That(BoundsOf(names[first]).Intersects(BoundsOf(names[second])), Is.False);
                foreach (var barrier in barriers)
                    Assert.That(BoundsOf(names[first]).Intersects(barrier.bounds), Is.False, barrier.name);
            }

            var playerBounds = BoundsOf("Player");
            var playerOffset = playerBounds.center - Entity("Player").transform.position;
            foreach (var marker in Objects().Where(item => item.GetComponent<Mirror.NetworkStartPosition>() != null))
            {
                var spawnBounds = new Bounds(marker.transform.position + playerOffset, playerBounds.size);
                foreach (var barrier in barriers)
                    Assert.That(spawnBounds.Intersects(barrier.bounds), Is.False, marker.name + " / " + barrier.name);
            }
        }

        [Test]
        public void EntityMaterials_AreDistinctUrpLit()
        {
            var materials = new[] { "Player", "Bot", "WeaponPickup" }.Select(name => Entity(name).GetComponent<MeshRenderer>().sharedMaterial).ToArray();
            Assert.That(materials.Distinct().Count(), Is.EqualTo(3));
            foreach (var material in materials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            }
        }

        [Test]
        public void Scene_HasCameraAndDirectionalLight()
        {
            Assert.That(Objects().SelectMany(item => item.GetComponents<Camera>()).Count(), Is.EqualTo(1));
            Assert.That(Objects().SelectMany(item => item.GetComponents<Light>()).Count(light => light.type == LightType.Directional), Is.EqualTo(1));
        }

        private GameObject[] Objects() => arena.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(item => item.gameObject).ToArray();
        private GameObject Entity(string name) => Objects().Single(item => item.name == name);
        private Bounds BoundsOf(string name) => Entity(name).GetComponent<Collider>().bounds;
        private static int Layer(string name)
        {
            var layer = LayerMask.NameToLayer(name);
            Assert.That(layer, Is.GreaterThan(0), name);
            return layer;
        }
        private static void Near(float actual, float expected) => Assert.That(actual, Is.EqualTo(expected).Within(Tolerance));
        private static void AssertSpans(float minimum, float maximum)
        {
            Assert.That(minimum, Is.LessThanOrEqualTo(-8));
            Assert.That(maximum, Is.GreaterThanOrEqualTo(8));
        }
    }
}
