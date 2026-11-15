using Mirror;
using SmallArena.Networking;
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

        private void Start()
        {
            if (NetworkServer.active || NetworkClient.active) return;
            // Mirror's scene postprocessor disables scene identities until a server spawns
            // them. Single Player has no server: enable its local geometry before match Start.
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (root.GetComponent<ArenaObstacles>() != null) root.SetActive(true);
        }
    }
}
