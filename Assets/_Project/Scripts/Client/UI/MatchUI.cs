using SmallArena.Domain;
using TMPro;
using UnityEngine;

namespace SmallArena.Client.UI
{
    /// <summary>Event-driven view of the match. Serialized components implement read-only Domain contracts.</summary>
    [DisallowMultipleComponent]
    public sealed class MatchUI : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour gameManager = null;
        [SerializeField] private MonoBehaviour botHealth = null;
        [SerializeField] private MonoBehaviour playerCombat = null;
        [SerializeField] private TextMeshProUGUI botHpText = null;
        [SerializeField] private TextMeshProUGUI damageText = null;
        [SerializeField] private GameObject victoryBanner = null;
        [SerializeField] private TextMeshProUGUI victoryText = null;

        private IMatchStateSource matchSource;
        private IHealthSource healthSource;
        private IDamageSource damageSource;

        public bool IsReady => isActiveAndEnabled && gameManager != null && botHealth != null && playerCombat != null
            && gameManager is IMatchStateSource
            && botHealth is IHealthSource && playerCombat is IDamageSource
            && botHpText != null && damageText != null && victoryBanner != null && victoryText != null;

        public bool IsBoundTo(MonoBehaviour manager, MonoBehaviour health, MonoBehaviour combat) =>
            gameManager == manager && botHealth == health && playerCombat == combat;

        private void OnEnable()
        {
            matchSource = gameManager as IMatchStateSource;
            healthSource = botHealth as IHealthSource;
            damageSource = playerCombat as IDamageSource;
            if (matchSource != null) matchSource.OnMatchStateChanged += HandleStateChanged;
            if (healthSource != null) healthSource.OnHealthChanged += HandleHealthChanged;
            if (damageSource != null) damageSource.OnDamageChanged += HandleDamageChanged;
            if (healthSource != null) HandleHealthChanged(healthSource.CurrentHealth, healthSource.MaxHealth);
            if (damageSource != null) HandleDamageChanged(damageSource.CurrentDamage);
            HandleStateChanged(matchSource?.CurrentState ?? MatchState.Initializing);
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (matchSource != null) matchSource.OnMatchStateChanged -= HandleStateChanged;
            if (healthSource != null) healthSource.OnHealthChanged -= HandleHealthChanged;
            if (damageSource != null) damageSource.OnDamageChanged -= HandleDamageChanged;
            matchSource = null;
            healthSource = null;
            damageSource = null;
        }

        private void HandleHealthChanged(int current, int maximum)
        {
            if (botHpText != null) botHpText.text = $"Bot HP: {current} / {maximum}";
        }

        private void HandleDamageChanged(int damage)
        {
            if (damageText != null) damageText.text = $"Damage: {damage} HP ({(damage > 1 ? "Buffed" : "Unarmed")})";
        }

        private void HandleStateChanged(MatchState state)
        {
            if (victoryText != null) victoryText.text = "VICTORY!";
            if (victoryBanner != null) victoryBanner.SetActive(state == MatchState.Victory);
        }
    }
}
