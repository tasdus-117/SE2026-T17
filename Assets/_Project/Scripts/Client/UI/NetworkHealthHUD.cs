using Mirror;
using SmallArena.Input;
using SmallArena.Networking;
using UnityEngine;
using TMPro;
using SmallArena.Domain;

namespace SmallArena.Client.UI
{
    public sealed class NetworkHealthHUD : MonoBehaviour
    {
        [SerializeField] private GameObject panel = null;
        [SerializeField] private NetworkHealthBarView healthView = null;
        [SerializeField] private InGameMenuController matchMenu = null;
        [SerializeField] private GameObject controls = null;
        [SerializeField] private TextMeshProUGUI damageText = null;
        [SerializeField] private TextMeshProUGUI presentationText = null;
        private readonly PlayerPresentationBinding presentation = new PlayerPresentationBinding();
        public PlayerPresentationSnapshot Presentation => presentation.State;
        public string PresentationText => presentationText != null ? presentationText.text : "";
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
            presentation.Changed += PresentationChanged;
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
            presentation.Bind(source != null ? source.GetComponent<IPlayerPresentationSource>() : null);
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
        private void PresentationChanged(PlayerPresentationSnapshot value)
        {
            if (presentationText == null) return;
            presentationText.text = value == null || value.Phase != MatchLifecyclePhase.InProgress ? ""
                : value.Rejection != "" ? $"Shot rejected: {value.Rejection}"
                : value.ShotOutcome == ShotOutcome.Blocked ? "Shot blocked by cover"
                : value.ShotOutcome == ShotOutcome.Hit ? "Hit"
                : value.ShotOutcome == ShotOutcome.Missed ? "Missed" : "";
        }
        private void ResultChanged(PlayerMatchResult result)
        {
            if (result == PlayerMatchResult.Playing || source == null) return;
            var input = source.GetComponent<PlayerInputHandler>();
            if (input != null) input.SetMenuInputBlocked(true);
            if (source.IsLifecycleBound) return; // Competitive result UI observes the authoritative standings.
            if (matchMenu != null)
            {
                if (result == PlayerMatchResult.Draw) matchMenu.ShowMatchEnded();
                else matchMenu.ShowMatchResult(result == PlayerMatchResult.Victory);
            }
        }
        private void OnDisable()
        {
            NetworkPlayer.OnLocalPlayerSpawned -= Spawned;
            NetworkPlayer.OnLocalPlayerDespawned -= Despawned;
            Bind(null);
            presentation.Changed -= PresentationChanged;
        }
    }
}
