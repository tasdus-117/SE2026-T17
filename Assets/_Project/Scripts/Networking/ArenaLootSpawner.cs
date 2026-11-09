using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Networking
{
    [RequireComponent(typeof(ArenaNetworkManager))]
    public sealed class ArenaLootSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject lootPrefab = null;
        private ArenaNetworkManager manager;
        private bool spawnedThisRound;
        public static readonly Vector3 SpawnPosition = new Vector3(-3, 0.3f, 0);
        private void OnEnable()
        {
            manager = GetComponent<ArenaNetworkManager>();
            manager.ServerStarted += Started;
            manager.ServerSceneLoaded += Loaded;
            manager.ServerStopped += Stopped;
        }
        private void OnDisable()
        {
            if (manager == null) return;
            manager.ServerStarted -= Started; manager.ServerSceneLoaded -= Loaded; manager.ServerStopped -= Stopped;
        }
        private void Started() => Loaded(SceneManager.GetActiveScene().name);
        private void Stopped() => spawnedThisRound = false;
        private void Loaded(string scene)
        {
            if (!NetworkServer.active || spawnedThisRound || lootPrefab == null
                || !(scene == "Arena_MVP" || scene.EndsWith("/Arena_MVP.unity"))) return;
            spawnedThisRound = true;
            var item = Instantiate(lootPrefab, SpawnPosition, Quaternion.identity);
            NetworkServer.Spawn(item);
        }
    }
}
