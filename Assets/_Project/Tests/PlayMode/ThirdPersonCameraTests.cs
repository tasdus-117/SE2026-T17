using System.Collections;
using System.Linq;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    // AC-005 / TC-007 / TC-008: follow, fixed heading and arena visibility.
    public sealed class ThirdPersonCameraTests
    {
        private static readonly Vector3 Offset = new Vector3(0f, 4.5f, -6f);
        private GameObject cameraObject;
        private GameObject target;
        private ThirdPersonCameraController follow;
        private float previousCaptureDeltaTime;
        private Scene arena;

        [SetUp]
        public void SetUp()
        {
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            target = new GameObject("Camera target under test");
            cameraObject = new GameObject("Follow camera under test");
            cameraObject.transform.position = Offset;
            follow = cameraObject.AddComponent<ThirdPersonCameraController>();
            follow.SetTarget(target.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
            if (arena.IsValid() && arena.isLoaded) yield return SceneManager.UnloadSceneAsync(arena);
            Time.captureDeltaTime = previousCaptureDeltaTime;
        }

        [UnityTest]
        public IEnumerator MovingTarget_SmoothlyTracksAllAxes_WithoutSnapping()
        {
            target.transform.position = new Vector3(4f, 1.2f, 5f);
            var desired = target.transform.position + Offset;
            var initialPosition = cameraObject.transform.position;
            var previousError = Vector3.Distance(initialPosition, desired);
            yield return null;
            Assert.That(Vector3.Distance(cameraObject.transform.position, initialPosition), Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(cameraObject.transform.position, desired), Is.GreaterThan(0.1f), "Follow should damp, not snap.");
            for (var frame = 0; frame < 60; frame++)
            {
                yield return null;
                var error = Vector3.Distance(cameraObject.transform.position, desired);
                Assert.That(error, Is.LessThanOrEqualTo(previousError + 0.00001f), "Stationary-target damping must converge without overshoot.");
                previousError = error;
            }
            Assert.That(previousError, Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator Tracking_AlwaysRestoresThirtyDegreePitch_AndZeroYawAndRoll()
        {
            for (var frame = 0; frame < 20; frame++)
            {
                target.transform.position += new Vector3(0.05f, 0f, 0.1f);
                target.transform.rotation = Quaternion.Euler(0f, frame * 17f, 0f);
                cameraObject.transform.rotation = Quaternion.Euler(12f, 75f, 15f);
                yield return null;
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(cameraObject.transform.eulerAngles.y, 0f)), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(cameraObject.transform.rotation, Quaternion.Euler(30f, 0f, 0f)), Is.LessThan(0.01f));
            }
        }

        [UnityTest]
        public IEnumerator NullOrDestroyedTarget_DoesNotMoveCamera_OrThrow()
        {
            follow.SetTarget(null);
            var position = cameraObject.transform.position;
            yield return null;
            Assert.That(cameraObject.transform.position, Is.EqualTo(position));
            follow.SetTarget(target.transform);
            Object.DestroyImmediate(target);
            yield return null;
            Assert.That(cameraObject.transform.position, Is.EqualTo(position));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ArenaCamera_IsWired_AndKeepsCapsuleVisibleAtEdgesAndJumpHeight()
        {
            yield return SceneManager.LoadSceneAsync("Arena_MVP", LoadSceneMode.Additive);
            arena = SceneManager.GetSceneByName("Arena_MVP");
            var roots = arena.GetRootGameObjects();
            var player = roots.Single(item => item.name == "Player");
            var sceneCamera = roots.Single(item => item.name == "Main Camera").GetComponent<Camera>();
            var sceneFollow = sceneCamera.GetComponent<ThirdPersonCameraController>();
            var occlusion = sceneCamera.GetComponent<CameraOcclusionController>();
            Assert.That(sceneFollow.Target, Is.SameAs(player.transform));
            Assert.That(sceneFollow.enabled && occlusion.enabled, Is.True);
            Assert.That(Vector3.Distance(sceneCamera.transform.position, player.transform.position + Offset), Is.LessThan(0.01f));
            player.GetComponent<PlayerController>().enabled = false;
            var character = player.GetComponent<CharacterController>();

            foreach (var height in new[] { 1f, 2.2f })
            foreach (var x in new[] { -7.3f, 0f, 7.3f })
            foreach (var z in new[] { -7.3f, 0f, 7.3f })
            {
                character.enabled = false;
                player.transform.position = new Vector3(x, height, z);
                character.enabled = true;
                Physics.SyncTransforms();
                for (var frame = 0; frame < 60; frame++) yield return null;
                AssertCapsuleVisible(sceneCamera, character.bounds);
            }

            // Exercise a moving target at 5 m/s with a jump-height vertical arc.
            for (var frame = 0; frame < 180; frame++)
            {
                var height = 1f + 1.2f * Mathf.Max(0f, Mathf.Sin(frame * Mathf.PI / 60f));
                character.Move(new Vector3(0f, height - player.transform.position.y, -5f / 60f));
                yield return null;
                AssertCapsuleVisible(sceneCamera, character.bounds);
            }

            var walls = roots.SelectMany(item => item.GetComponentsInChildren<Renderer>()).Where(item => item.name.StartsWith("Wall")).ToArray();
            Assert.That(walls.Any(item => item.shadowCastingMode == ShadowCastingMode.ShadowsOnly), Is.True, "Foreground wall must not obscure the south-edge capsule.");
            foreach (var wall in walls) Assert.That(wall.GetComponent<Collider>().enabled, Is.True);
            occlusion.enabled = false;
            foreach (var wall in walls) Assert.That(wall.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        }

        private static void AssertCapsuleVisible(Camera camera, Bounds bounds)
        {
            foreach (var sample in new[] { bounds.center, bounds.center + Vector3.up * bounds.extents.y * 0.95f,
                bounds.center - Vector3.up * bounds.extents.y * 0.95f,
                bounds.center + Vector3.right * bounds.extents.x * 0.95f,
                bounds.center - Vector3.right * bounds.extents.x * 0.95f })
            {
                var viewport = camera.WorldToViewportPoint(sample);
                Assert.That(viewport.z, Is.GreaterThan(camera.nearClipPlane));
                Assert.That(viewport.x, Is.InRange(0f, 1f));
                Assert.That(viewport.y, Is.InRange(0f, 1f));
                foreach (var hit in Physics.RaycastAll(sample, camera.transform.position - sample,
                    Vector3.Distance(sample, camera.transform.position), 1 << LayerMask.NameToLayer("Environment"), QueryTriggerInteraction.Ignore))
                {
                    var renderer = hit.collider.GetComponent<Renderer>();
                    if (renderer != null) Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly), hit.collider.name);
                }
            }
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y, 0f)), Is.LessThan(0.001f));
        }
    }
}
