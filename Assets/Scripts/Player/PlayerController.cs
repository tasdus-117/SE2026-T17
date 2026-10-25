using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallArena.Player
{
    /// <summary>
    /// Player controller with client-side prediction and server-side validation
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float rotationSpeed = 10f;
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;
        
        // Components
        private CharacterController characterController;
        private PlayerInput playerInput;
        
        // Input
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpInput;
        private bool sprintInput;
        
        // Movement state
        private Vector3 velocity;
        private bool isGrounded;
        
        // Network state
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<PlayerMovementState> movementState = new NetworkVariable<PlayerMovementState>();
        
        // Anti-cheat
        private float lastServerValidationTime;
        private const float SERVER_VALIDATION_INTERVAL = 0.5f;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheck = groundCheckObj.transform;
                groundCheck.SetParent(transform);
                groundCheck.localPosition = new Vector3(0, -1f, 0);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsOwner)
            {
                // Enable input for local player
                if (playerInput != null)
                {
                    playerInput.enabled = true;
                }
                
                // Setup camera
                SetupCamera();
            }
            else
            {
                // Disable input for remote players
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                }
            }
            
            // Subscribe to network variable changes for interpolation
            if (!IsOwner)
            {
                networkPosition.OnValueChanged += OnNetworkPositionChanged;
                networkRotation.OnValueChanged += OnNetworkRotationChanged;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (!IsOwner)
            {
                networkPosition.OnValueChanged -= OnNetworkPositionChanged;
                networkRotation.OnValueChanged -= OnNetworkRotationChanged;
            }
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            HandleMovement();
            HandleRotation();
            
            // Periodically validate position with server
            if (IsClient && Time.time - lastServerValidationTime > SERVER_VALIDATION_INTERVAL)
            {
                ValidatePositionServerRpc(transform.position, transform.rotation);
                lastServerValidationTime = Time.time;
            }
        }
        
        private void HandleMovement()
        {
            // Ground check
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            
            // Calculate movement
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            float currentSpeed = sprintInput ? sprintSpeed : moveSpeed;
            
            characterController.Move(move * currentSpeed * Time.deltaTime);
            
            // Jump
            if (jumpInput && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                jumpInput = false;
            }
            
            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
            
            // Send movement state to server
            if (move.magnitude > 0.1f || !isGrounded)
            {
                SendMovementStateServerRpc(transform.position, transform.rotation, velocity, isGrounded);
            }
        }
        
        private void HandleRotation()
        {
            if (lookInput.magnitude > 0.1f)
            {
                float rotation = lookInput.x * rotationSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up * rotation);
            }
        }
        
        /// <summary>
        /// Send movement state to server for validation
        /// </summary>
        [ServerRpc]
        private void SendMovementStateServerRpc(Vector3 position, Quaternion rotation, Vector3 vel, bool grounded)
        {
            // Validate movement on server
            if (ValidateMovement(position))
            {
                networkPosition.Value = position;
                networkRotation.Value = rotation;
                
                movementState.Value = new PlayerMovementState
                {
                    Position = position,
                    Rotation = rotation,
                    Velocity = vel,
                    IsGrounded = grounded,
                    Timestamp = Time.time
                };
            }
            else
            {
                // Movement is invalid, force correction
                Debug.LogWarning($"Invalid movement detected for client {OwnerClientId}");
                CorrectPositionClientRpc(networkPosition.Value, networkRotation.Value);
            }
        }
        
        /// <summary>
        /// Validate position periodically
        /// </summary>
        [ServerRpc]
        private void ValidatePositionServerRpc(Vector3 position, Quaternion rotation)
        {
            float distance = Vector3.Distance(position, networkPosition.Value);
            
            if (distance > Network.GameNetworkManager.Instance.Config.maxPositionDrift)
            {
                Debug.LogWarning($"Position drift detected for client {OwnerClientId}: {distance}");
                CorrectPositionClientRpc(networkPosition.Value, networkRotation.Value);
            }
        }
        
        /// <summary>
        /// Server corrects client position if validation fails
        /// </summary>
        [ClientRpc]
        private void CorrectPositionClientRpc(Vector3 correctPosition, Quaternion correctRotation)
        {
            if (!IsOwner) return;
            
            transform.position = correctPosition;
            transform.rotation = correctRotation;
            velocity = Vector3.zero;
            
            Debug.Log("Position corrected by server");
        }
        
        private bool ValidateMovement(Vector3 newPosition)
        {
            if (networkPosition.Value == Vector3.zero)
            {
                return true; // First position update
            }
            
            float distance = Vector3.Distance(networkPosition.Value, newPosition);
            float maxDistance = Network.GameNetworkManager.Instance.Config.maxMovementSpeed * Time.deltaTime * 2f;
            
            return distance <= maxDistance;
        }
        
        private void OnNetworkPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (!IsOwner)
            {
                // Interpolate position for remote players
                StartCoroutine(InterpolatePosition(newValue));
            }
        }
        
        private void OnNetworkRotationChanged(Quaternion previousValue, Quaternion newValue)
        {
            if (!IsOwner)
            {
                // Interpolate rotation for remote players
                StartCoroutine(InterpolateRotation(newValue));
            }
        }
        
        private System.Collections.IEnumerator InterpolatePosition(Vector3 targetPosition)
        {
            float elapsed = 0f;
            float duration = 0.1f;
            Vector3 startPosition = transform.position;
            
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = targetPosition;
        }
        
        private System.Collections.IEnumerator InterpolateRotation(Quaternion targetRotation)
        {
            float elapsed = 0f;
            float duration = 0.1f;
            Quaternion startRotation = transform.rotation;
            
            while (elapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.rotation = targetRotation;
        }
        
        private void SetupCamera()
        {
            // Setup first-person camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(transform);
                mainCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
                mainCamera.transform.localRotation = Quaternion.identity;
            }
        }
        
        #region Input Callbacks
        
        public void OnMove(InputValue value)
        {
            if (!IsOwner) return;
            moveInput = value.Get<Vector2>();
        }
        
        public void OnLook(InputValue value)
        {
            if (!IsOwner) return;
            lookInput = value.Get<Vector2>();
        }
        
        public void OnJump(InputValue value)
        {
            if (!IsOwner) return;
            jumpInput = value.isPressed;
        }
        
        public void OnSprint(InputValue value)
        {
            if (!IsOwner) return;
            sprintInput = value.isPressed;
        }
        
        #endregion
        
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }
    }
    
    /// <summary>
    /// Network-serializable movement state
    /// </summary>
    public struct PlayerMovementState : INetworkSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public bool IsGrounded;
        public float Timestamp;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref IsGrounded);
            serializer.SerializeValue(ref Timestamp);
        }
    }
}
