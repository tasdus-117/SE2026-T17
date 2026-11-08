using Mirror;
using SmallArena.Networking;
using SmallArena.Server.Simulation;
using UnityEngine;
using SmallArena.Client;
using SmallArena.Client.Presentation;

namespace SmallArena.Tests.PlayMode
{
    internal static class OfflinePlayerFixture
    {
        // Match Arena_MVP's explicit removed-component overrides when testing offline mechanics.
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var player = Object.Instantiate(prefab, position, rotation);
            player.SetActive(false);
            Object.DestroyImmediate(player.GetComponent<ServerPlayerSeparation>());
            Object.DestroyImmediate(player.GetComponent<OpponentHealthPresenter>());
            Object.DestroyImmediate(player.GetComponent<NetworkCombatInput>());
            Object.DestroyImmediate(player.GetComponent<NetworkCombat>());
            Object.DestroyImmediate(player.GetComponent<NetworkHealth>());
            Object.DestroyImmediate(player.GetComponent<ServerPlayerMovement>());
            Object.DestroyImmediate(player.GetComponent<NetworkMovement>());
            Object.DestroyImmediate(player.GetComponent<NetworkTransformReliable>());
            Object.DestroyImmediate(player.GetComponent<NetworkPlayer>());
            Object.DestroyImmediate(player.GetComponent<NetworkIdentity>());
            player.SetActive(true);
            return player;
        }
    }
}
