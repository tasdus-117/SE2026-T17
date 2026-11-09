using Mirror;
using UnityEngine;

namespace SmallArena.Core
{
    /// <summary>Keep the offline sample actors/HUD out of sessions entered through Bootstrap.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class OfflineMatchScope : MonoBehaviour
    {
        [SerializeField] private GameObject[] offlineRoots = new GameObject[0];

        private void Awake()
        {
            if (!NetworkServer.active && !NetworkClient.active) return;
            foreach (var root in offlineRoots)
                if (root != null) root.SetActive(false);
        }
    }
}
