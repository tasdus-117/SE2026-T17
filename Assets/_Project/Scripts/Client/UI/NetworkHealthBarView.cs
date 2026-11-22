using System;
using SmallArena.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Client.UI
{
    public sealed class NetworkHealthBarView : MonoBehaviour
    {
        [SerializeField] private Slider healthBar = null;
        [SerializeField] private TextMeshProUGUI healthText = null;
        private IHealthSource source;
        private Action<int, int> listener;
        private uint generation;
        public Slider HealthBar => healthBar;
        public string HealthText => healthText.text;
        public void Bind(IHealthSource health)
        {
            Unsubscribe();
            source = health;
            if (source == null)
            {
                healthText.text = "";
                healthBar.SetValueWithoutNotify(0);
                return;
            }
            if (!isActiveAndEnabled) return;
            var current = generation;
            listener = (hp, max) => { if (current == generation && isActiveAndEnabled) Changed(hp, max); };
            source.OnHealthChanged += listener;
            Changed(source.CurrentHealth, source.MaxHealth);
        }
        private void Unsubscribe()
        {
            generation++;
            if (source != null && listener != null) source.OnHealthChanged -= listener;
            listener = null;
        }
        private void OnEnable() => Bind(source);
        private void OnDisable() => Unsubscribe();
        private void Changed(int current, int max)
        {
            healthBar.maxValue = max;
            healthBar.SetValueWithoutNotify(current);
            healthText.text = $"HP: {current}/{max}";
        }
        private void OnDestroy() => Unsubscribe();
    }
}
