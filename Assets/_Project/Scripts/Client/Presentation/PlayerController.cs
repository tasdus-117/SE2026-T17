using Mirror;
using SmallArena.Input;
using SmallArena.Domain;
using SmallArena.Protocol;
using UnityEngine;

namespace SmallArena.Client
{
    /// <summary>Local character locomotion from input intent; no combat or match rules.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler))]
    public sealed class PlayerController : MonoBehaviour, IMovementReadiness, IMatchEndParticipant
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20.0f;
        [SerializeField] private float groundedGravity = -2.0f;

        private float verticalVelocity;
        private NetworkIdentity netIdentity;

        public bool IsMovementReady => isActiveAndEnabled && characterController != null
            && characterController.enabled && inputHandler != null && inputHandler.IsInputReady;

        public void EndMatch() => enabled = false;

        private void Reset() => BindComponents();

        private void Awake() => BindComponents();

        private void OnEnable() => verticalVelocity = 0f;

        private void BindComponents()
        {
            netIdentity = GetComponent<NetworkIdentity>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            if (netIdentity != null && !netIdentity.isLocalPlayer) return;
            // Heading never follows movement or camera rotation.
            transform.rotation = Quaternion.identity;
            var moveInput = Vector2.ClampMagnitude(inputHandler.MoveInput, 1f);
            var moveDir = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;

            // Consume even in the air: an ignored press must not jump on landing.
            var jumpPressed = inputHandler.ConsumeJumpPressed();
            if (!characterController.enabled) return;

            var grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = groundedGravity;
            if (grounded && jumpPressed)
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(gravity));

            verticalVelocity += gravity * Time.deltaTime;
            var motion = (moveDir + Vector3.up * verticalVelocity) * Time.deltaTime;
            var collisions = characterController.Move(motion);
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }
    }
}
