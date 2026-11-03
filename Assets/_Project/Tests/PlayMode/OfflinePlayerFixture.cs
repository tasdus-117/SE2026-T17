using Mirror;
using SmallArena.Networking;
using UnityEngine;

namespace SmallArena.Tests.PlayMode
{
    internal static class OfflinePlayerFixture
    {
        // Match Arena_MVP's explicit removed-component overrides when testing offline mechanics.
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var player = Object.Instantiate(prefab, position, rotation);
            player.SetActive(false);
            Object.DestroyImmediate(player.GetComponent<NetworkPlayer>());
            Object.DestroyImmediate(player.GetComponent<NetworkIdentity>());
            player.SetActive(true);
            return player;
        }
    }
}
