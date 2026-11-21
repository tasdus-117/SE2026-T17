using System;
using System.Text;
using Mirror;
using TMPro;
using UnityEngine;

namespace SmallArena.Client.UI
{
    public sealed class KillFeedHudView : MonoBehaviour
    {
        [SerializeField] private MatchLifecycleHudView lifecycleHud = null;
        [SerializeField] private TextMeshProUGUI feedText = null;
        public string FeedText => feedText.text;
        private void Update()
        {
            var state = lifecycleHud.State;
            var text = new StringBuilder();
            if (state != null) foreach (var entry in state.Feed)
            {
                var remaining = 4 - (NetworkTime.time - entry.ServerTime);
                if (remaining <= 0) continue;
                var alpha = (int)(Math.Min(1, remaining) * 255);
                text.Append("<alpha=#").Append(alpha.ToString("X2")).Append('>');
                if (string.IsNullOrEmpty(entry.AttackerId)) text.Append($"Player {entry.VictimSlot + 1} fell into the void");
                else text.Append($"Player {entry.AttackerSlot + 1} eliminated Player {entry.VictimSlot + 1}");
                text.Append("\n");
            }
            feedText.text = text.ToString();
        }
    }
}
