using UnityEngine;
namespace SmallArena.Client.Presentation
{
    /// <summary>Animate a render-only child; collection collider and authoritative anchor never move.</summary>
    public sealed class PickupVisual : MonoBehaviour
    {
        private Renderer source;
        private MeshRenderer visual;
        private bool previousForceOff;
        private void Awake()
        {
            var mesh = GetComponent<MeshFilter>(); source = GetComponent<Renderer>();
            if (mesh == null || source == null || Application.isBatchMode) return;
            previousForceOff = source.forceRenderingOff;
            var child = new GameObject("Pickup visual", typeof(MeshFilter), typeof(MeshRenderer));
            child.layer = 0; child.transform.SetParent(transform, false); // Render helper is not another Pickup entity.
            child.GetComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            visual = child.GetComponent<MeshRenderer>(); visual.sharedMaterials = source.sharedMaterials;
            source.forceRenderingOff = true;
        }
        private void LateUpdate()
        {
            if (visual == null || source == null) return;
            visual.enabled = source.enabled;
            visual.transform.localPosition = Vector3.up * (0.1f + 0.08f * Mathf.Sin(Time.time * 2f));
            visual.transform.localRotation = Quaternion.Euler(0, Time.time * 45f, 0);
        }
        private void OnDestroy() { if (visual != null) Destroy(visual.gameObject); if (source != null) source.forceRenderingOff = previousForceOff; }
    }
}
