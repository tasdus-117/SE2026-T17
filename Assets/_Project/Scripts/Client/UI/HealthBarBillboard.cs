using UnityEngine;
namespace SmallArena.Client.UI
{
    public sealed class HealthBarBillboard : MonoBehaviour
    {
        private void LateUpdate() { if (Camera.main != null) transform.rotation = Camera.main.transform.rotation; }
    }
}
