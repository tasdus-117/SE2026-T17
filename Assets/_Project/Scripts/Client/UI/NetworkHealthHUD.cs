using Mirror;
using SmallArena.Input;
using SmallArena.Networking;
using UnityEngine;
using TMPro;

namespace SmallArena.Client.UI
{
    public sealed class NetworkHealthHUD : MonoBehaviour
    {
        [SerializeField] private GameObject panel = null;
        [SerializeField] private NetworkHealthBarView healthView = null;
        [SerializeField] private InGameMenuController matchMenu = null;
        [SerializeField] private GameObject controls = null;
        [SerializeField] private TextMeshProUGUI damageText = null;
        private NetworkHealth source;
        private NetworkCombat combat;
        public string DamageText => damageText != null ? damageText.text : "";
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
            if (combat != null) combat.OnDamageChanged -= DamageChanged;
            source = health;
            combat = source != null ? source.GetComponent<NetworkCombat>() : null;
            panel.SetActive(source != null);
            if (controls != null) controls.SetActive(source != null);
            healthView.Bind(source);
            if (source == null) return;
            source.OnResultChanged += ResultChanged;
            if (combat != null) { combat.OnDamageChanged += DamageChanged; DamageChanged(combat.CurrentDamage); }
            ResultChanged(source.Result);
        }
        private void DamageChanged(int damage)
        {
            if (damageText != null) damageText.text = $"Damage: {damage} HP ({(damage > 1 ? "Buffed" : "Unarmed")})";
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
