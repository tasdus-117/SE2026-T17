#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.Presentation;
using SmallArena.Input;
using SmallArena.Networking;
using SmallArena.Protocol;
using SmallArena.Server.Simulation;
using SmallArena.Server.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkMovementTests : InputTestFixture
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private readonly List<NetworkIdentity> replicas = new List<NetworkIdentity>();
        private static readonly MethodInfo Command = typeof(NetworkMovement).GetMethod("CmdSubmitMovement", BindingFlags.Instance | BindingFlags.NonPublic);
        private ArenaNetworkManager manager;
        private GameObject player;
        private NetworkMovement bridge;
        private ServerPlayerMovement simulation;
        private Keyboard keyboard;
        private float previousCaptureDelta;
        private uint sequence;

        public override void Setup()
        {
            base.Setup();
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = Time.fixedDeltaTime;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            sequence = 100;
        }

        public override void TearDown()
        {
            try
            {
                foreach (var replica in replicas)
                {
                    if (replica == null) continue;
                    SetFlag(replica, "isLocalPlayer", false);
                    SetFlag(replica, "isOwned", false);
                    SetFlag(replica, "isClient", false);
                }
                replicas.Clear();
                if (manager != null)
                {
                    manager.offlineScene = "";
                    if (NetworkClient.active && NetworkServer.active) manager.StopHost();
                    else if (NetworkServer.active) manager.StopServer();
                }
                foreach (var item in created) if (item != null) Object.DestroyImmediate(item);
                created.Clear();
                NetworkManager.ResetStatics();
                Transport.active = null;
            }
            finally
            {
                Time.captureDeltaTime = previousCaptureDelta;
                base.TearDown();
            }
        }

        private GameObject Track(GameObject item) { created.Add(item); return item; }

        private IEnumerator StartHost(bool automaticInput = false)
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(1000, -0.5f, 1000);
            floor.transform.localScale = new Vector3(100, 1, 100);
            floor.layer = 6;
            var marker = Track(new GameObject("Movement start"));
            marker.transform.position = new Vector3(1000, 1, 1000);
            marker.AddComponent<NetworkStartPosition>();
            var owner = Track(new GameObject("Movement test manager"));
            owner.SetActive(false);
            var transport = owner.AddComponent<LifecycleTestTransport>();
            manager = owner.AddComponent<ArenaNetworkManager>();
            manager.transport = transport;
            manager.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            manager.offlineScene = manager.onlineScene = "";
            manager.autoCreatePlayer = true;
            manager.playerSpawnMethod = PlayerSpawnMethod.RoundRobin;
            manager.headlessStartMode = HeadlessStartOptions.DoNothing;
            owner.SetActive(true);
            Physics.SyncTransforms();
            manager.StartHost();
            var deadline = Time.realtimeSinceStartup + 15f;
            while (NetworkClient.localPlayer == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(NetworkClient.localPlayer, Is.Not.Null);
            player = Track(NetworkClient.localPlayer.gameObject);
            bridge = player.GetComponent<NetworkMovement>();
            simulation = player.GetComponent<ServerPlayerMovement>();
            player.GetComponent<PlayerController>().enabled = automaticInput;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        // Invoke the woven Command wrapper, not the server receiver: this exercises real Mirror serialization,
        // LocalConnection delivery and requiresAuthority checks with deliberately hostile client payloads.
        private uint Send(Vector2 move, uint jump = 0)
        {
            var sent = sequence++;
            Command.Invoke(bridge, new object[] { new MovementInput { Sequence = sent, MoveX = move.x, MoveZ = move.y, JumpSequence = jump } });
            return sent;
        }

        private static IEnumerator Until(Func<bool> predicate)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Timed out waiting for server command processing.");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        [TestCase(3f, 4f)]
        [TestCase(-6f, -8f)]
        [TestCase(float.MaxValue, float.MaxValue)]
        public void OversizedFiniteVectors_ClampToUnitMagnitude(float x, float z)
        {
            Assert.That(MovementInputValidator.TryClamp(x, z, out var move), Is.True);
            Assert.That(move.magnitude, Is.EqualTo(1f).Within(0.000001f));
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(float.NegativeInfinity, 0f)]
        [TestCase(0f, float.NaN)]
        [TestCase(0f, float.PositiveInfinity)]
        public void NonFiniteVectors_AreRejected(float x, float z)
        {
            Assert.That(MovementInputValidator.TryClamp(x, z, out var move), Is.False);
            Assert.That(move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AnalogInputBelowOne_PreservesMagnitude()
        {
            Assert.That(MovementInputValidator.TryClamp(0.3f, 0.4f, out var move), Is.True);
            Assert.That(move, Is.EqualTo(new Vector2(0.3f, 0.4f)));
        }

        [TestCase(0u, uint.MaxValue, true)]
        [TestCase(10u, 10u, false)]
        [TestCase(9u, 10u, false)]
        [TestCase(11u, 10u, true)]
        public void SequenceOrdering_AllowsWrapButRejectsReplay(uint candidate, uint previous, bool expected)
            => Assert.That(MovementInputValidator.IsNewer(candidate, previous), Is.EqualTo(expected));

        [UnityTest]
        public IEnumerator OversizedCommand_CannotExceedFiveMetersPerSecond()
        {
            yield return StartHost();
            var sent = Send(new Vector2(30, 40));
            yield return Until(() => simulation.LastAcceptedSequence == sent);
            Assert.That(simulation.AcceptedMove.magnitude, Is.EqualTo(1f).Within(0.000001f));
            for (var frame = 0; frame < 20; frame++)
            {
                var position = player.transform.position;
                var tick = simulation.SimulationTick;
                Send(new Vector2(30, 40));
                yield return new WaitForFixedUpdate();
                var budget = ServerPlayerMovement.MoveSpeed * Time.fixedDeltaTime * (simulation.SimulationTick - tick);
                Assert.That(HorizontalDistance(position, player.transform.position), Is.LessThanOrEqualTo(budget + 0.0002f));
            }
        }

        [UnityTest]
        public IEnumerator LocalWasd_CommandPipeline_MovesOnlyByServerFixedSteps()
        {
            yield return StartHost(true);
            // Client-side offline speed tuning is not part of the network payload or server speed budget.
            var serialized = new SerializedObject(player.GetComponent<PlayerController>());
            serialized.FindProperty("moveSpeed").floatValue = 999f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            yield return Until(() => simulation.AcceptedMove == Vector2.up);
            var position = player.transform.position;
            var tick = simulation.SimulationTick;
            for (var frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
            var expected = 5f * Time.fixedDeltaTime * (simulation.SimulationTick - tick);
            Assert.That(player.transform.position.z - position.z, Is.EqualTo(expected).Within(0.003f));
            Assert.That(player.transform.position.x, Is.EqualTo(position.x).Within(0.0002f));
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.identity), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator CommandFlood_DoesNotPurchaseAdditionalSimulationTime()
        {
            yield return StartHost();
            var position = player.transform.position;
            var tick = simulation.SimulationTick;
            uint last = 0;
            for (var request = 0; request < 200; request++) last = Send(new Vector2(999, 0));
            yield return Until(() => simulation.LastAcceptedSequence == last);
            var budget = 5f * Time.fixedDeltaTime * (simulation.SimulationTick - tick);
            Assert.That(HorizontalDistance(position, player.transform.position), Is.LessThanOrEqualTo(budget + 0.0002f));
        }

        [UnityTest]
        public IEnumerator InvalidOutOfOrderAndDuplicateCommands_DoNotReplaceAcceptedIntent()
        {
            yield return StartHost();
            var first = Send(Vector2.right * 0.25f);
            yield return Until(() => simulation.LastAcceptedSequence == first);
            var observed = new List<uint>();
            Action<MovementInput> record = input => { if (input.Sequence >= first - 1) observed.Add(simulation.LastAcceptedSequence); };
            bridge.ServerInputReceived += record;
            try
            {
                Command.Invoke(bridge, new object[] { new MovementInput { Sequence = first + 1, MoveX = float.NaN } });
                Command.Invoke(bridge, new object[] { new MovementInput { Sequence = first, MoveX = 1 } });
                Command.Invoke(bridge, new object[] { new MovementInput { Sequence = first - 1, MoveX = 1 } });
                Command.Invoke(bridge, new object[] { new MovementInput { Sequence = first + 2 } });
                yield return Until(() => observed.Count == 4);
                Assert.That(observed, Is.EqualTo(new[] { first, first, first, first + 2 }));
                Assert.That(simulation.AcceptedMove, Is.EqualTo(Vector2.zero));
            }
            finally { bridge.ServerInputReceived -= record; }
        }

        [UnityTest]
        public IEnumerator MissingHeartbeat_StopsHorizontalMovement()
        {
            yield return StartHost();
            var sent = Send(Vector2.up);
            yield return Until(() => simulation.LastAcceptedSequence == sent);
            yield return new WaitForSecondsRealtime((float)ServerPlayerMovement.InputTimeout + 0.1f);
            var position = player.transform.position;
            for (var frame = 0; frame < 10; frame++) yield return new WaitForFixedUpdate();
            Assert.That(HorizontalDistance(position, player.transform.position), Is.LessThan(0.0002f));
        }

        [UnityTest]
        public IEnumerator JumpCounter_ProducesGroundedJump_AndDropsAirborneOrRepeatedEdges()
        {
            yield return StartHost();
            var character = player.GetComponent<CharacterController>();
            Assert.That(character.isGrounded, Is.True);
            var initialHeight = player.transform.position.y;
            var sent = Send(Vector2.zero, 1);
            yield return Until(() => simulation.LastAcceptedSequence == sent && simulation.VerticalVelocity > 0);
            var peak = player.transform.position.y;
            Assert.That(character.isGrounded, Is.False);
            // Fresh edge 2 arrives in the air. Repeating it in heartbeats must not jump again on landing.
            for (var frame = 0; frame < 60; frame++)
            {
                Send(Vector2.zero, 2);
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.transform.position.y);
            }
            Assert.That(peak - initialHeight, Is.InRange(1.05f, 1.25f));
            Assert.That(character.isGrounded, Is.True);
            // A landing can settle one skin width above the initial flush spawn pose.
            Assert.That(player.transform.position.y, Is.EqualTo(initialHeight).Within(character.skinWidth + 0.005f));
            Assert.That(simulation.VerticalVelocity, Is.LessThan(0));
        }

        private static void SetFlag(NetworkIdentity identity, string name, bool value)
            => typeof(NetworkIdentity).GetProperty(name).SetValue(identity, value);

        private GameObject CreateReplica(bool owner)
        {
            var replica = Track(Object.Instantiate(manager.playerPrefab, new Vector3(2000, 3, 2000), Quaternion.Euler(0, 45, 0)));
            replica.GetComponent<PlayerController>().enabled = false;
            var identity = replica.GetComponent<NetworkIdentity>();
            replicas.Add(identity);
            SetFlag(identity, "isClient", true);
            SetFlag(identity, "isOwned", owner);
            SetFlag(identity, "isLocalPlayer", owner);
            replica.GetComponent<ServerPlayerMovement>().OnStartClient();
            return replica;
        }

        [UnityTest]
        public IEnumerator RemoteReplica_IgnoresLocalKeys_AndRunsNoCharacterPhysics()
        {
            yield return StartHost();
            var replica = CreateReplica(false);
            replica.GetComponent<PlayerController>().enabled = true;
            var position = replica.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
            InputSystem.Update();
            for (var frame = 0; frame < 5; frame++) yield return new WaitForFixedUpdate();
            Assert.That(replica.GetComponent<PlayerInputHandler>().MoveInput, Is.EqualTo(Vector2.zero));
            Assert.That(replica.GetComponent<CharacterController>().enabled, Is.False);
            Assert.That(replica.GetComponent<ServerPlayerMovement>().SimulationTick, Is.Zero);
            Assert.That(replica.transform.position, Is.EqualTo(position));
            Assert.That(Mathf.DeltaAngle(replica.transform.eulerAngles.y, 45), Is.EqualTo(0).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ReliableSnapshots_ReachOwnerAndObservers_WithInterpolation()
        {
            yield return StartHost();
            var authoritative = player.GetComponent<NetworkTransformReliable>();
            Assert.That(authoritative.syncDirection, Is.EqualTo(SyncDirection.ServerToClient));
            Assert.That(authoritative.syncMode, Is.EqualTo(SyncMode.Observers));
            Assert.That(authoritative.syncPosition && authoritative.syncRotation && authoritative.interpolatePosition, Is.True);
            foreach (var owner in new[] { false, true })
            {
                // Standalone replica flags isolate Mirror's deserialize/interpolate path within the host test process.
                var replica = CreateReplica(owner);
                var transformSync = replica.GetComponent<NetworkTransformReliable>();
                Assert.That(transformSync.authority, Is.False, "Even the owning client cannot upload official transforms.");
                using (var writer = NetworkWriterPool.Get())
                {
                    authoritative.OnSerialize(writer, true);
                    using (var reader = NetworkReaderPool.Get(writer.ToArraySegment())) transformSync.OnDeserialize(reader, true);
                }
                Assert.That(transformSync.clientSnapshots.Count, Is.GreaterThan(0));
                var received = transformSync.clientSnapshots.Values[transformSync.clientSnapshots.Count - 1];
                Assert.That(received.position, Is.EqualTo(player.transform.position));
                var now = NetworkTime.time;
                var from = received.position - Vector3.right * 2;
                transformSync.clientSnapshots.Clear();
                transformSync.clientSnapshots.Add(now - 1, new TransformSnapshot(now - 1, now - 1, from, Quaternion.identity, Vector3.one));
                transformSync.clientSnapshots.Add(now + 1, new TransformSnapshot(now + 1, now + 1, received.position, Quaternion.identity, Vector3.one));
                typeof(NetworkTransformReliable).GetMethod("UpdateClient", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(transformSync, null);
                Assert.That(replica.transform.position.x, Is.GreaterThan(from.x + 0.1f).And.LessThan(received.position.x - 0.1f));
                Assert.That(replica.GetComponent<CharacterController>().enabled, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ServerToClientTransform_RejectsForgedClientPositionUpload()
        {
            yield return StartHost();
            var sync = player.GetComponent<NetworkTransformReliable>();
            var position = player.transform.position;
            using (var writer = NetworkWriterPool.Get())
            {
                writer.WriteVector3(position + Vector3.right * 1000);
                writer.WriteQuaternion(Quaternion.Euler(0, 90, 0));
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment())) sync.OnDeserialize(reader, true);
            }
            Assert.That(player.transform.position, Is.EqualTo(position));
            Assert.That(player.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(sync.serverSnapshots, Is.Empty);
        }

        [UnityTest]
        public IEnumerator OwnerCameraOcclusion_UsesVisualBounds_WhenReplicaColliderIsDisabled()
        {
            yield return StartHost();
            var replica = CreateReplica(true);
            var camera = Track(new GameObject("Replica camera")).AddComponent<ThirdPersonCameraController>();
            camera.SetTarget(replica.transform);
            camera.transform.position = replica.transform.position + new Vector3(0, 4.5f, -6);
            var occlusion = camera.gameObject.AddComponent<CameraOcclusionController>();
            var settings = new SerializedObject(occlusion);
            settings.FindProperty("occlusionLayers").intValue = 1 << 6;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.layer = 6;
            wall.transform.position = replica.transform.position + new Vector3(0, 1.5f, -3);
            wall.transform.localScale = new Vector3(8, 4, 0.5f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(replica.GetComponent<CharacterController>().enabled, Is.False);
            Assert.That(wall.GetComponent<Renderer>().shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));
            Assert.That(wall.GetComponent<Collider>().enabled, Is.True);
        }
    }
}
#endif
