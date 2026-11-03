using UnityEngine;

namespace SmallArena.Client.Presentation
{
    /// <summary>World-relative, fixed-heading presentation follow after character movement.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 4.5f, -6f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float pitchAngle = 30f;

        private Vector3 currentVelocity;

        public Transform Target => target;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            currentVelocity = Vector3.zero;
        }

        private void Awake() => ApplyFixedRotation();

        private void OnEnable() => currentVelocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;

            var desiredPosition = target.position + followOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
                ref currentVelocity, smoothTime);
            ApplyFixedRotation();
        }

        private void ApplyFixedRotation() => transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
}
