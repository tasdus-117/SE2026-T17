using System;
using SmallArena.Domain;
using SmallArena.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallArena.Input
{
    /// <summary>Keyboard actions to intent only. Consumers own match, movement and combat rules.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputHandler : MonoBehaviour, ICombatInputSource, IMatchEndParticipant
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionAsset ownedActions;
        private InputActionMap gameplay;
        private InputAction move;
        private InputAction jump;
        private InputAction punch;
        private InputAction interact;
        private bool jumpPressed;
        private bool punchPressed;
        private bool interactPressed;

        public Vector2 MoveInput { get; private set; }
        public bool IsInputReady => isActiveAndEnabled && ownedActions != null;

        public void EndMatch() => enabled = false;

        public bool ConsumeJumpPressed() => Consume(ref jumpPressed);
        public bool ConsumePunchPressed() => Consume(ref punchPressed);
        public bool ConsumeInteractPressed() => Consume(ref interactPressed);

        /// <summary>Optional composition seam; call while inactive, before the first enable.</summary>
        public void Configure(InputActionAsset actions)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            if (isActiveAndEnabled || ownedActions != null)
                throw new InvalidOperationException("Configure input before the component's first enable.");
            inputActions = actions;
        }

        private void OnEnable()
        {
            ClearInput();
            if (!EnsureActions())
            {
                enabled = false;
                return;
            }

            move.performed += OnMove;
            move.canceled += OnMove;
            jump.performed += OnJump;
            punch.performed += OnPunch;
            interact.performed += OnInteract;
            gameplay.Enable();
        }

        private void OnDisable()
        {
            if (move != null) { move.performed -= OnMove; move.canceled -= OnMove; }
            if (jump != null) jump.performed -= OnJump;
            if (punch != null) punch.performed -= OnPunch;
            if (interact != null) interact.performed -= OnInteract;
            gameplay?.Disable();
            ClearInput();
        }

        private void OnDestroy()
        {
            if (ownedActions != null)
            {
                ownedActions.Disable();
                Destroy(ownedActions);
            }
        }

        private bool EnsureActions()
        {
            if (ownedActions != null) return true;
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputHandler requires an InputActionAsset before enabling.", this);
                return false;
            }

            // Each adapter owns its action state; shared project assets stay untouched.
            ownedActions = Instantiate(inputActions);
            gameplay = ownedActions.FindActionMap("Gameplay");
            move = gameplay?.FindAction("Move");
            jump = gameplay?.FindAction("Jump");
            punch = gameplay?.FindAction("Punch");
            interact = gameplay?.FindAction("Interact");
            if (move == null || move.type != InputActionType.Value || move.expectedControlType != "Vector2"
                || jump == null || jump.type != InputActionType.Button
                || punch == null || punch.type != InputActionType.Button
                || interact == null || interact.type != InputActionType.Button)
            {
                Debug.LogError("PlayerInputHandler requires Gameplay/Move (Vector2 Value), Jump, Punch and Interact (Button).", this);
                Destroy(ownedActions);
                ownedActions = null;
                return false;
            }

            // A held button at enable is not a fresh press. Movement still performs
            // its normal Value-action initial-state check on the next input update.
            jump.wantsInitialStateCheck = false;
            punch.wantsInitialStateCheck = false;
            interact.wantsInitialStateCheck = false;
            return true;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton()) jumpPressed = true;
        }

        private void OnPunch(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton()) punchPressed = true;
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton()) interactPressed = true;
        }

        private void ClearInput()
        {
            MoveInput = Vector2.zero;
            jumpPressed = false;
            punchPressed = false;
            interactPressed = false;
        }

        private static bool Consume(ref bool pressed)
        {
            var result = pressed;
            pressed = false;
            return result;
        }
    }
}
