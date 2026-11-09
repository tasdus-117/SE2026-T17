using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SmallArena.Client.Presentation
{
    /// <summary>Keep the followed capsule visible through foreground arena walls; physics stays intact.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThirdPersonCameraController))]
    public sealed class CameraOcclusionController : MonoBehaviour
    {
        // The scene supplies the Environment layer; actors never become occluders.
        [SerializeField] private LayerMask occlusionLayers = 0;

        private ThirdPersonCameraController follow;
        private readonly Dictionary<Renderer, ShadowCastingMode> hidden = new Dictionary<Renderer, ShadowCastingMode>();
        private readonly HashSet<Renderer> obstructing = new HashSet<Renderer>();
        private readonly List<Renderer> restored = new List<Renderer>();

        private void Awake() => follow = GetComponent<ThirdPersonCameraController>();

        private void LateUpdate()
        {
            obstructing.Clear();
            var target = follow.Target;
            if (target != null)
            {
                var collider = target.GetComponent<Collider>();
                // Network replicas disable CharacterController; disabled collider bounds are empty at the origin.
                var renderer = target.GetComponentInChildren<Renderer>();
                var bounds = collider != null && collider.enabled ? collider.bounds
                    : renderer != null ? renderer.bounds : new Bounds(target.position, Vector3.zero);
                CheckLine(bounds.center);
                CheckLine(bounds.center + Vector3.up * bounds.extents.y * 0.9f);
                CheckLine(bounds.center - Vector3.up * bounds.extents.y * 0.9f);
                CheckLine(bounds.center + Vector3.right * bounds.extents.x * 0.9f);
                CheckLine(bounds.center - Vector3.right * bounds.extents.x * 0.9f);
            }

            restored.Clear();
            foreach (var item in hidden)
            {
                if (obstructing.Contains(item.Key)) continue;
                if (item.Key != null) item.Key.shadowCastingMode = item.Value;
                restored.Add(item.Key);
            }
            foreach (var renderer in restored) hidden.Remove(renderer);
        }

        private void CheckLine(Vector3 point)
        {
            var direction = transform.position - point;
            // Cast from the actor so a camera inside a wall still detects that wall.
            foreach (var hit in Physics.RaycastAll(point, direction.normalized, direction.magnitude,
                occlusionLayers, QueryTriggerInteraction.Ignore))
            {
                var renderer = hit.collider.GetComponent<Renderer>();
                if (renderer == null) continue;
                obstructing.Add(renderer);
                if (!hidden.ContainsKey(renderer)) hidden.Add(renderer, renderer.shadowCastingMode);
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        private void OnDisable()
        {
            foreach (var item in hidden)
                if (item.Key != null) item.Key.shadowCastingMode = item.Value;
            hidden.Clear();
        }
    }
}
