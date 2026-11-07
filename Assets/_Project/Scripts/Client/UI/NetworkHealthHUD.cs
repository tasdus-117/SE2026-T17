using Mirror;
using SmallArena.Input;
using SmallArena.Networking;
using UnityEngine;

namespace SmallArena.Client.UI
{
    public sealed class NetworkHealthHUD : MonoBehaviour
    {
        [SerializeField] private GameObject panel = null;
        [SerializeField] private NetworkHealthBarView healthView = null;
        [SerializeField] private InGameMenuController matchMenu = null;
        [SerializeField] private GameObject controls = null;
        private NetworkHealth source;
        public NetworkHealthBarView HealthView => healthView;
        public bool IsVisible => panel.activeSelf;
        private void Awake()
        {
            if (matchMenu == null) matchMenu = FindAnyObjectByType<InGameMenuController>();
        }
        private void OnEnable()
        {
            NetworkPlayer.OnLocalPlayerSpawned += Spawned;
            NetworkPlayer.OnLocalPlayerDespawned += Despawned;
            Bind(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<NetworkHealth>() : null);
        }
        private void Spawned(NetworkPlayer player) { if (player.isLocalPlayer) Bind(player.GetComponent<NetworkHealth>()); }
        private void Despawned(NetworkPlayer player) { if (source != null && source.gameObject == player.gameObject) Bind(null); }
        private void Bind(NetworkHealth health)
        {
            if (source != null) source.OnResultChanged -= ResultChanged;
            source = health;
            panel.SetActive(source != null);
            if (controls != null) controls.SetActive(source != null);
            healthView.Bind(source);
            if (source == null) return;
            source.OnResultChanged += ResultChanged;
            ResultChanged(source.Result);
        }
        private void ResultChanged(PlayerMatchResult result)
        {
            if (result == PlayerMatchResult.Playing || source == null) return;
            var input = source.GetComponent<PlayerInputHandler>();
            if (input != null) input.SetMenuInputBlocked(true);
            if (matchMenu != null) matchMenu.ShowMatchResult(result == PlayerMatchResult.Victory);
        }
        private void OnDisable()
        {
            NetworkPlayer.OnLocalPlayerSpawned -= Spawned;
            NetworkPlayer.OnLocalPlayerDespawned -= Despawned;
            Bind(null);
        }
    }
}
