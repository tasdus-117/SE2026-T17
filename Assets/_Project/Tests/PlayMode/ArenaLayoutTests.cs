using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SmallArena.Core;
using SmallArena.Domain;
using SmallArena.Networking;
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

        [Test]
        public void OpenArena_GeneratesSolidMixedObstaclesInsideTheFloor()
        {
            var floor = BoundsOf("Floor");
            Assert.That(Entity("ArenaGeometry").GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
            var obstacles = ObstacleColliders();
            Assert.That(obstacles.Length, Is.InRange(3, 6));
            Assert.That(obstacles.Select(item => item.GetType()).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            for (var index = 0; index < obstacles.Length; index++)
            {
                var obstacle = obstacles[index];
                var bounds = obstacle.bounds;
                Assert.That(obstacle.enabled && !obstacle.isTrigger, Is.True);
                Assert.That(obstacle.gameObject.layer, Is.EqualTo(Layer("Environment")));
                Assert.That(obstacle.GetComponent<MeshRenderer>().enabled, Is.True);
                Near(bounds.min.y, floor.max.y);
                Assert.That(bounds.min.x, Is.GreaterThan(floor.min.x));
                Assert.That(bounds.max.x, Is.LessThan(floor.max.x));
                Assert.That(bounds.min.z, Is.GreaterThan(floor.min.z));
                Assert.That(bounds.max.z, Is.LessThan(floor.max.z));
                for (var other = index + 1; other < obstacles.Length; other++)
                    Assert.That(bounds.Intersects(obstacles[other].bounds), Is.False);
            }
        }

        [UnityTest]
        public IEnumerator OfflineFall_EndsMatchInDefeatAndOffersReplay()
        {
            var player = Entity("Player");
            player.transform.position = new Vector3(9f, ArenaObstacles.FallHeight - 0.1f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(Entity("_GameManager").GetComponent<GameManager>().CurrentState, Is.EqualTo(MatchState.Defeat));
            Assert.That(player.GetComponent<SmallArena.Server.Combat.Health>().CurrentHealth, Is.Zero);
            Assert.That(Entity("VictoryText").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("DEFEAT!"));
            Assert.That(Entity("PlayAgainButton").activeInHierarchy, Is.True);
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
            var barriers = ObstacleColliders();
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
        private Collider[] ObstacleColliders() => Entity("ArenaObstacles").GetComponentsInChildren<Collider>();
    }
}
