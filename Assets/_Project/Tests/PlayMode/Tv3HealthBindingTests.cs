using System;
using System.Reflection;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace SmallArena.Tests.PlayMode
{
    public sealed class Tv3HealthBindingTests
    {
        private GameObject owner;
        private NetworkHealthBarView view;
        private sealed class Source : IHealthSource
        {
            public int CurrentHealth { get; set; } = 7;
            public int MaxHealth => 10;
            public event Action<int, int> OnHealthChanged;
            public Action<int, int> Capture() => OnHealthChanged;
        }
        [SetUp] public void Setup()
        {
            owner = new GameObject("health binding");
            owner.SetActive(false);
            view = owner.AddComponent<NetworkHealthBarView>();
            var slider = new GameObject("slider", typeof(RectTransform), typeof(Slider));
            slider.transform.SetParent(owner.transform);
            var label = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(owner.transform);
            typeof(NetworkHealthBarView).GetField("healthBar", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, slider.GetComponent<Slider>());
            typeof(NetworkHealthBarView).GetField("healthText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, label.GetComponent<TextMeshProUGUI>());
            owner.SetActive(true);
        }
        [TearDown] public void Teardown() => Object.DestroyImmediate(owner);
        [Test] public void Rebind_RejectsAlreadyCapturedOldCallback()
        {
            var old = new Source();
            view.Bind(old);
            var delayed = old.Capture();
            view.Bind(new Source { CurrentHealth = 3 });
            delayed(9, 10);
            Assert.That(view.HealthText, Is.EqualTo("HP: 3/10"));
        }
        [Test] public void Disable_RejectsLateCallbackAndEnableRefreshes()
        {
            var source = new Source();
            view.Bind(source);
            var delayed = source.Capture();
            view.enabled = false;
            delayed(9, 10);
            Assert.That(view.HealthText, Is.EqualTo("HP: 7/10"));
            source.CurrentHealth = 4;
            view.enabled = true;
            Assert.That(view.HealthText, Is.EqualTo("HP: 4/10"));
        }
        [Test] public void Unbind_ClearsOldHealth()
        {
            view.Bind(new Source());
            view.Bind(null);
            Assert.That(view.HealthText, Is.Empty);
        }
    }
}
