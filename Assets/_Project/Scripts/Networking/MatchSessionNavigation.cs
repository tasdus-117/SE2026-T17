using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Networking
{
    /// <summary>Owns one asynchronous return operation, surviving destruction of the old session manager.</summary>
    public sealed class MatchSessionNavigation : MonoBehaviour
    {
        private static MatchSessionNavigation active;
        public static bool IsReturning => active != null;

        public static void ReturnToMainMenu()
        {
            if (IsReturning) return;
            var manager = NetworkManager.singleton;
            if (manager != null) manager.offlineScene = ""; // This operation, not Mirror's delayed scene load, owns navigation.
            var stopServer = NetworkServer.active && (NetworkClient.activeHost
                || manager != null && (manager.mode == NetworkManagerMode.Host || manager.mode == NetworkManagerMode.ServerOnly));
            var owner = new GameObject("Match session teardown");
            DontDestroyOnLoad(owner);
            active = owner.AddComponent<MatchSessionNavigation>();
            active.StartCoroutine(active.Return(manager, stopServer));
        }

        private IEnumerator Return(NetworkManager manager, bool stopServer)
        {
            // Disconnect callbacks run before Mirror finalizes connectState. Never re-enter them synchronously.
            yield return null;
            if (manager != null)
            {
                if (stopServer && NetworkClient.activeHost) manager.StopHost();
                else
                {
                    if (NetworkClient.active || NetworkClient.connection != null) manager.StopClient();
                    if (stopServer && NetworkServer.active) manager.StopServer();
                }
            }
            else
            {
                if (NetworkClient.active || NetworkClient.connection != null) NetworkClient.Disconnect();
                if (stopServer && NetworkServer.active) NetworkServer.Shutdown();
            }
            var deadline = Time.realtimeSinceStartup + 5f;
            while ((NetworkClient.active || NetworkClient.connection != null) && Time.realtimeSinceStartup < deadline) yield return null;
            if (NetworkClient.active || NetworkClient.connection != null || NetworkClient.spawned.Count > 0) NetworkClient.Shutdown();
            if (stopServer && NetworkServer.active) NetworkServer.Shutdown();
            while (NetworkManager.loadingSceneAsync != null && !NetworkManager.loadingSceneAsync.isDone) yield return null;
            if (manager is ArenaNetworkManager arena && arena != null) arena.ClearRoomClientState();
            if (manager != null) Destroy(manager.gameObject);
            yield return null; // Finish Unity destruction before resetting singleton references or creating the menu root.
            NetworkManager.ResetStatics();
            if (!NetworkServer.active) Transport.active = null;
            if (SceneManager.GetActiveScene().name != "MainMenu") yield return SceneManager.LoadSceneAsync("MainMenu");
            Destroy(gameObject);
        }

        private void OnDestroy() { if (active == this) active = null; }
    }
}
