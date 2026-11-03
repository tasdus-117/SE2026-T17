#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class PlayerControllerTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject player;
        private GameObject floor;
        private CharacterController character;
        private float previousCaptureDeltaTime;

        public override void Setup()
        {
            base.Setup();
            previousCaptureDeltaTime = Time.captureDeltaTime;
            // Headless runs can render faster than CharacterController.minMoveDistance
            // supports. Use a stable game timestep for physical trajectory assertions.
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(1000f, -0.5f, 1000f);
            floor.transform.localScale = new Vector3(100f, 1f, 100f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            player = OfflinePlayerFixture.Instantiate(prefab, new Vector3(1000f, 1f, 1000f), Quaternion.identity);
            character = player.GetComponent<CharacterController>();
            Assert.That(player.GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerInputHandler>().enabled, Is.True);
            Physics.SyncTransforms();
        }

        public override void TearDown()
        {
            try
            {
                if (player != null) Object.DestroyImmediate(player);
                if (floor != null) Object.DestroyImmediate(floor);
            }
            finally
            {
                Time.captureDeltaTime = previousCaptureDeltaTime;
                base.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator WasdAndDiagonal_MoveAtFiveMetresPerSecond_WithFixedWorldHeading()
        {
            yield return WaitForGround();
            var keys = new[] { new[] { Key.W }, new[] { Key.S }, new[] { Key.A }, new[] { Key.D }, new[] { Key.W, Key.D } };
            var directions = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, new Vector3(1f, 0f, 1f).normalized };
            for (var index = 0; index < keys.Length; index++)
            {
                SetKeys(keys[index]);
                player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                var start = player.transform.position;
                var elapsed = 0f;
                for (var frame = 0; frame < 12; frame++)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                var displacement = player.transform.position - start;
                displacement.y = 0f;
                Assert.That(Vector3.Distance(displacement, directions[index] * (5f * elapsed)), Is.LessThan(0.025f));
                Assert.That(displacement.magnitude / elapsed, Is.LessThanOrEqualTo(5.05f));
                Assert.That(Vector3.Distance(player.transform.forward, Vector3.forward), Is.LessThan(0.0001f));
            }
            SetKeys();
        }

        [UnityTest]
        public IEnumerator GroundedSpace_JumpsAboutOnePointTwoMetres_AndReturnsToGround()
        {
            yield return WaitForGround();
            var startY = player.transform.position.y;
            var peak = startY;
            SetKeys(Key.Space);
            yield return null;
            SetKeys();
            Assert.That(character.isGrounded, Is.False);
            var elapsed = 0f;
            while (!character.isGrounded && elapsed < 3f)
            {
                peak = Mathf.Max(peak, player.transform.position.y);
                yield return null;
                elapsed += Time.deltaTime;
            }
            Assert.That(peak - startY, Is.InRange(1.0f, 1.3f));
            Assert.That(character.isGrounded, Is.True);
            Assert.That(player.transform.position.y, Is.EqualTo(startY).Within(0.03f));
        }

        [UnityTest]
        public IEnumerator AirborneSpace_IsDiscarded_AndDoesNotJumpWhenLanding()
        {
            yield return WaitForGround();
            character.enabled = false;
            player.transform.position += Vector3.up * 2f;
            character.enabled = true;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(character.isGrounded, Is.False);
            var startY = player.transform.position.y;
            SetKeys(Key.Space);
            yield return null;
            SetKeys();
            Assert.That(player.transform.position.y, Is.LessThan(startY), "Space must not reverse an airborne fall.");
            yield return WaitForGround();
            var landedY = player.transform.position.y;
            for (var frame = 0; frame < 12; frame++)
            {
                yield return null;
                Assert.That(character.isGrounded, Is.True, "An airborne press must not be buffered until landing.");
                Assert.That(player.transform.position.y, Is.EqualTo(landedY).Within(0.03f));
            }
        }

        [UnityTest]
        public IEnumerator ArenaScene_PlayerInheritsEnabledComponentsAndReferences()
        {
            var operation = SceneManager.LoadSceneAsync("Arena_MVP", LoadSceneMode.Additive);
            yield return operation;
            var scene = SceneManager.GetSceneByName("Arena_MVP");
            try
            {
                GameObject arenaPlayer = null;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == "Player") arenaPlayer = root;
                Assert.That(arenaPlayer, Is.Not.Null);
                var input = arenaPlayer.GetComponent<PlayerInputHandler>();
                var controller = arenaPlayer.GetComponent<PlayerController>();
                Assert.That(input, Is.Not.Null);
                Assert.That(controller, Is.Not.Null);
                Assert.That(input.enabled && controller.enabled, Is.True);
                var serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("characterController").objectReferenceValue, Is.SameAs(arenaPlayer.GetComponent<CharacterController>()));
                Assert.That(serialized.FindProperty("inputHandler").objectReferenceValue, Is.SameAs(input));
                Assert.That(new SerializedObject(input).FindProperty("inputActions").objectReferenceValue, Is.Not.Null);
            }
            finally { SceneManager.UnloadSceneAsync(scene); }
        }

        private IEnumerator WaitForGround()
        {
            var elapsed = 0f;
            do
            {
                yield return null;
                elapsed += Time.deltaTime;
            } while (!character.isGrounded && elapsed < 3f);
            Assert.That(character.isGrounded, Is.True, "Player did not settle on the test floor.");
        }

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
#endif
