using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SmallArena.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    // Input portions of AC-006/007/008/012 and TC-002/003/006/010.
    // Actual movement, grounding, attack cooldown and collection are later responsibilities.
    public sealed class PlayerInputHandlerTests : InputTestFixture
    {
        private Keyboard keyboard;
        private InputActionAsset source;
        private GameObject owner;
        private PlayerInputHandler handler;
        private string assetJson;

        public override void Setup()
        {
            base.Setup();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
            assetJson = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Input/SmallArena.inputactions"));
            ReplaceSource(assetJson);
        }

        public override void TearDown()
        {
            try
            {
                if (owner != null) Object.DestroyImmediate(owner);
                if (source != null) Object.DestroyImmediate(source);
            }
            finally { base.TearDown(); }
        }

        [TestCase(Key.W, 0, 1)]
        [TestCase(Key.A, -1, 0)]
        [TestCase(Key.S, 0, -1)]
        [TestCase(Key.D, 1, 0)]
        public void CardinalMovement_ProducesExpectedUnitVector(Key key, float x, float y)
        {
            SetKeys(key);
            AssertMove(x, y);
        }

        [TestCase(Key.W, Key.S)]
        [TestCase(Key.A, Key.D)]
        public void OpposingKeys_CancelPreviouslyActiveMovement(Key first, Key opposite)
        {
            SetKeys(first);
            Assert.That(handler.MoveInput.magnitude, Is.EqualTo(1).Within(0.0001f));
            SetKeys(first, opposite);
            AssertMove(0, 0);
            SetKeys(opposite);
            Assert.That(handler.MoveInput.magnitude, Is.EqualTo(1).Within(0.0001f));
        }

        [TestCase(Key.W, Key.D, 1, 1)]
        [TestCase(Key.W, Key.A, -1, 1)]
        [TestCase(Key.S, Key.D, 1, -1)]
        [TestCase(Key.S, Key.A, -1, -1)]
        public void DiagonalMovement_IsNormalized(Key first, Key second, float x, float y)
        {
            SetKeys(first, second);
            AssertMove(x / Mathf.Sqrt(2), y / Mathf.Sqrt(2));
            Assert.That(handler.MoveInput.magnitude, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void AllFourDirections_CancelToZero()
        {
            SetKeys(Key.W, Key.A, Key.S, Key.D);
            AssertMove(0, 0);
        }

        [Test]
        public void ReleasingMovement_ClearsIntent()
        {
            SetKeys(Key.W);
            SetKeys();
            AssertMove(0, 0);
        }

        [Test]
        public void Adapter_ClampsEvenWhenCompositeIsNotNormalized()
        {
            ReplaceSource(assetJson.Replace("2DVector(mode=0)", "2DVector(mode=1)"));
            SetKeys(Key.W, Key.D);
            AssertMove(1 / Mathf.Sqrt(2), 1 / Mathf.Sqrt(2));
            Assert.That(handler.MoveInput.magnitude, Is.LessThanOrEqualTo(1f));
        }

        [TestCase(Key.Space, "Jump")]
        [TestCase(Key.J, "Punch")]
        [TestCase(Key.E, "Interact")]
        public void FreshPress_IsConsumedExactlyOnce(Key key, string action)
        {
            Assert.That(Consume(action), Is.False);
            SetKeys(key);
            Assert.That(Consume(action), Is.True);
            Assert.That(Consume(action), Is.False);
            Assert.That(Consume(action), Is.False);
        }

        [TestCase(Key.Space, "Jump")]
        [TestCase(Key.J, "Punch")]
        [TestCase(Key.E, "Interact")]
        public void HeldButton_DoesNotRepeatAcrossUpdatesOrKeyboardStateEvents(Key key, string action)
        {
            SetKeys(key);
            Assert.That(Consume(action), Is.True);
            for (var update = 0; update < 5; update++)
            {
                InputSystem.Update();
                Assert.That(Consume(action), Is.False);
                SetKeys(key, update % 2 == 0 ? Key.W : Key.A);
                Assert.That(Consume(action), Is.False);
            }
        }

        [TestCase(Key.Space, "Jump")]
        [TestCase(Key.J, "Punch")]
        [TestCase(Key.E, "Interact")]
        public void ReleaseAndRepress_ProducesOneNewIntent(Key key, string action)
        {
            SetKeys(key);
            Assert.That(Consume(action), Is.True);
            SetKeys();
            Assert.That(Consume(action), Is.False);
            SetKeys(key);
            Assert.That(Consume(action), Is.True);
            Assert.That(Consume(action), Is.False);
        }

        [Test]
        public void ButtonIntents_AreIndependent()
        {
            SetKeys(Key.Space, Key.J, Key.E);
            Assert.That(handler.ConsumeJumpPressed(), Is.True);
            Assert.That(handler.ConsumeJumpPressed(), Is.False);
            Assert.That(handler.ConsumePunchPressed(), Is.True);
            Assert.That(handler.ConsumePunchPressed(), Is.False);
            Assert.That(handler.ConsumeInteractPressed(), Is.True);
            Assert.That(handler.ConsumeInteractPressed(), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Disabling_ClearsMovementPendingButtonsAndEnabledActions(bool disableObject)
        {
            SetKeys(Key.W, Key.Space, Key.J, Key.E);
            AssertMove(0, 1);
            Assert.That(EnabledGameplayActions(), Is.EqualTo(4));

            if (disableObject) owner.SetActive(false);
            else handler.enabled = false;

            AssertMove(0, 0);
            AssertNoButtons();
            Assert.That(EnabledGameplayActions(), Is.EqualTo(0));
        }

        [Test]
        public void DisabledComponent_ReceivesNoNewInput()
        {
            handler.enabled = false;
            SetKeys(Key.W, Key.Space, Key.J, Key.E);
            AssertMove(0, 0);
            AssertNoButtons();
        }

        [Test]
        public void ReenableWhileHeld_RestoresMovement_ButDoesNotReplayButtons()
        {
            SetKeys(Key.W, Key.Space, Key.J, Key.E);
            handler.enabled = false;
            handler.enabled = true;
            InputSystem.Update();
            AssertMove(0, 1);
            AssertNoButtons();

            SetKeys(Key.W, Key.A, Key.Space, Key.J, Key.E);
            AssertNoButtons();
            SetKeys();
            SetKeys(Key.Space, Key.J, Key.E);
            Assert.That(handler.ConsumeJumpPressed(), Is.True);
            Assert.That(handler.ConsumePunchPressed(), Is.True);
            Assert.That(handler.ConsumeInteractPressed(), Is.True);
        }

        [Test]
        public void RepeatedEnableDisable_KeepsExactlyFourOwnedActions()
        {
            for (var cycle = 0; cycle < 3; cycle++)
            {
                SetKeys();
                handler.enabled = false;
                Assert.That(EnabledGameplayActions(), Is.EqualTo(0));
                handler.enabled = true;
                Assert.That(EnabledGameplayActions(), Is.EqualTo(4));
                SetKeys(Key.J);
                Assert.That(handler.ConsumePunchPressed(), Is.True);
                Assert.That(handler.ConsumePunchPressed(), Is.False);
            }
        }

        [Test]
        public void SharedSourceAsset_IsNotEnabledOrMutated()
        {
            SetKeys(Key.W, Key.J);
            AssertMove(0, 1);
            Assert.That(source.FindActionMap("Gameplay").enabled, Is.False);
            Assert.That(source.actionMaps.SelectMany(map => map.actions).All(action => !action.enabled), Is.True);
        }

        [Test]
        public void DisablingOneAdapter_DoesNotDisableAnotherOwnersActions()
        {
            var otherOwner = new GameObject("Independent input adapter");
            otherOwner.SetActive(false);
            var other = otherOwner.AddComponent<PlayerInputHandler>();
            other.Configure(source);
            otherOwner.SetActive(true);
            try
            {
                SetKeys(Key.Space);
                Assert.That(handler.ConsumeJumpPressed(), Is.True);
                Assert.That(other.ConsumeJumpPressed(), Is.True);
                handler.enabled = false;
                SetKeys();
                SetKeys(Key.W, Key.J);
                AssertMove(0, 0);
                AssertNoButtons();
                Assert.That(other.MoveInput, Is.EqualTo(Vector2.up));
                Assert.That(other.ConsumePunchPressed(), Is.True);
                Assert.That(EnabledGameplayActions(), Is.EqualTo(4));
            }
            finally { Object.DestroyImmediate(otherOwner); }
        }

        [Test]
        public void RemovingKeyboard_ClearsMovement()
        {
            SetKeys(Key.W);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.Update();
            AssertMove(0, 0);
        }

        [Test]
        public void ConfigureAfterEnabling_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => handler.Configure(source));
            handler.enabled = false;
            Assert.Throws<InvalidOperationException>(() => handler.Configure(source));
        }

        [Test]
        public void MissingSource_DisablesWithDiagnostic()
        {
            Object.DestroyImmediate(owner);
            owner = new GameObject("Unconfigured input adapter");
            owner.SetActive(false);
            handler = owner.AddComponent<PlayerInputHandler>();
            LogAssert.Expect(LogType.Error, "PlayerInputHandler requires an InputActionAsset before enabling.");
            owner.SetActive(true);
            Assert.That(handler.enabled, Is.False);
            Assert.That(EnabledGameplayActions(), Is.EqualTo(0));
        }

        [Test]
        public void MissingGameplayMap_DisablesWithoutPartiallyEnabledActions()
        {
            ReplaceSource("{\"name\":\"Invalid\",\"maps\":[],\"controlSchemes\":[]}", false);
            LogAssert.Expect(LogType.Error, "PlayerInputHandler requires Gameplay/Move (Vector2 Value), Jump, Punch and Interact (Button).");
            owner.SetActive(true);
            Assert.That(handler.enabled, Is.False);
            Assert.That(EnabledGameplayActions(), Is.EqualTo(0));
        }

        private void ReplaceSource(string json, bool activate = true)
        {
            if (owner != null) Object.DestroyImmediate(owner);
            if (source != null) Object.DestroyImmediate(source);
            source = InputActionAsset.FromJson(json);
            owner = new GameObject("Input adapter under test");
            owner.SetActive(false);
            handler = owner.AddComponent<PlayerInputHandler>();
            handler.Configure(source);
            if (activate) owner.SetActive(true);
        }

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private bool Consume(string action)
        {
            switch (action)
            {
                case "Jump": return handler.ConsumeJumpPressed();
                case "Punch": return handler.ConsumePunchPressed();
                case "Interact": return handler.ConsumeInteractPressed();
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private void AssertMove(float x, float y) => Assert.That(Vector2.Distance(handler.MoveInput, new Vector2(x, y)), Is.LessThan(0.0001f));
        private void AssertNoButtons()
        {
            Assert.That(handler.ConsumeJumpPressed(), Is.False);
            Assert.That(handler.ConsumePunchPressed(), Is.False);
            Assert.That(handler.ConsumeInteractPressed(), Is.False);
        }
        private static int EnabledGameplayActions() => InputSystem.ListEnabledActions().Count(action => action.actionMap.name == "Gameplay");
    }
}
