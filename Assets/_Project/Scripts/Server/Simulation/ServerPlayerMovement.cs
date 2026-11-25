using Mirror;
using SmallArena.Networking;
using SmallArena.Protocol;
using SmallArena.Server.Validation;
using UnityEngine;

namespace SmallArena.Server.Simulation
{
    /// <summary>Authoritative fixed-step character simulation. Commands never execute physics.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(NetworkMovement))]
    public sealed class ServerPlayerMovement : NetworkBehaviour
    {
        public const float MoveSpeed = 5f;
        public const float JumpHeight = 1.2f;
        public const float Gravity = -20f;
        public const float GroundedVelocity = -2f;
        public const double InputTimeout = 0.25d;

        private CharacterController character;
        private NetworkMovement bridge;
        private NetworkHealth health;
        private Vector2 move;
        private float verticalVelocity;
        private bool pendingJump;
        private bool hasInput;
        private bool hasSequence;
        private uint lastSequence;
        private uint lastJumpSequence;
        private double lastInputTime;

        public uint SimulationTick { get; private set; }
        public uint LastAcceptedSequence => lastSequence;
        public Vector2 AcceptedMove => move;
        public float VerticalVelocity => verticalVelocity;

        private void Awake()
        {
            character = GetComponent<CharacterController>();
            bridge = GetComponent<NetworkMovement>();
            health = GetComponent<NetworkHealth>();
        }

        public override void OnStartServer()
        {
            move = Vector2.zero;
            verticalVelocity = 0f;
            ClearIntent();
            hasSequence = false;
            lastSequence = lastJumpSequence = SimulationTick = 0;
            character.enabled = true;
            bridge.ServerInputReceived += ReceiveInput;
        }

        public override void OnStartClient()
        {
            // Replica transforms (including the owner) are written only by NetworkTransform.
            if (!isServer) character.enabled = false;
        }

        public override void OnStopServer()
        {
            bridge.ServerInputReceived -= ReceiveInput;
            ClearIntent();
        }

        private void OnDisable() => ClearIntent();

        private void ClearIntent()
        {
            move = Vector2.zero;
            verticalVelocity = 0f;
            pendingJump = hasInput = false;
            lastInputTime = 0d;
            // Temporary revocation must not reopen either replay watermark.
        }
        public void ClearRuntimeInput()
        {
            if (!isServer) return;
            ClearIntent();
            verticalVelocity = 0;
        }

        private void ReceiveInput(MovementInput input)
        {
            if (!isServer || !isActiveAndEnabled) return;
            if (!character.enabled || health != null && !health.CanAct) { ClearIntent(); return; }
            if (hasSequence && !MovementInputValidator.IsNewer(input.Sequence, lastSequence)) return;
            if (health != null && health.IsLifecycleBound && input.LifeId != health.LifeId) return;
            if (!MovementInputValidator.TryClamp(input.MoveX, input.MoveZ, out var accepted)) return;
            move = accepted;
            lastSequence = input.Sequence;
            lastInputTime = NetworkTime.localTime;
            hasInput = true;
            hasSequence = true;
            if (MovementInputValidator.IsNewer(input.JumpSequence, lastJumpSequence))
            {
                lastJumpSequence = input.JumpSequence;
                // Consume airborne edges now, never defer them until landing.
                if (character.enabled && character.isGrounded) pendingJump = true;
            }
        }

        private void FixedUpdate()
        {
            Simulate(NetworkTime.localTime);
        }

        private void Simulate(double serverTime)
        {
            if (!isServer || !NetworkServer.active || !isActiveAndEnabled) return;
            if (!character.enabled) { ClearIntent(); return; }
            if (health != null && health.IsLifecycleBound && !health.CanAct) { ClearRuntimeInput(); return; }
            if (health != null && !health.CanAct) ClearIntent();
            var fresh = hasInput && serverTime - lastInputTime <= (health?.InputTimeoutSeconds ?? InputTimeout);
            var direction = fresh ? move : Vector2.zero;
            var jump = fresh && pendingJump;
            pendingJump = false;
            var grounded = character.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = GroundedVelocity;
            if (grounded && jump) verticalVelocity = Mathf.Sqrt(2f * JumpHeight * Mathf.Abs(Gravity));
            // Only the trusted server fixed delta advances position, once per physics tick.
            verticalVelocity += Gravity * Time.fixedDeltaTime;
            transform.rotation = Quaternion.identity;
            var speed = (float)(health?.MovementSpeed ?? MoveSpeed);
            var velocity = new Vector3(direction.x * speed, verticalVelocity, direction.y * speed);
            var collisions = character.Move(velocity * Time.fixedDeltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            SimulationTick = unchecked(SimulationTick + 1);
        }
    }
}
