using SmallArena.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Client.UI
{
    public sealed class NetworkHealthBarView : MonoBehaviour
    {
        [SerializeField] private Slider healthBar = null;
        [SerializeField] private TextMeshProUGUI healthText = null;
        private NetworkHealth source;
        public Slider HealthBar => healthBar;
        public string HealthText => healthText.text;
        public void Bind(NetworkHealth health)
        {
            if (source != null) source.OnHealthChanged -= Changed;
            source = health;
            if (source == null || !isActiveAndEnabled) return;
            source.OnHealthChanged += Changed;
            Changed(source.CurrentHealth, source.MaxHealth);
        }
        private void OnEnable() => Bind(source);
        private void OnDisable() { if (source != null) source.OnHealthChanged -= Changed; }
        private void Changed(int current, int max)
        {
            healthBar.maxValue = max;
            healthBar.SetValueWithoutNotify(current);
            healthText.text = $"HP: {current}/{max}";
        }
        private void OnDestroy() => Bind(null);
    }
}
