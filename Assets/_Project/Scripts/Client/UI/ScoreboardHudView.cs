using System.Collections.Generic;
using System.Text;
using Mirror;
using SmallArena.Domain;
using SmallArena.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallArena.Client.UI
{
    public sealed class ScoreboardHudView : MonoBehaviour
    {
        [SerializeField] private MatchLifecycleHudView lifecycleHud = null;
        [SerializeField] private GameObject panel = null;
        [SerializeField] private TextMeshProUGUI rowsText = null;
        private InGameMenuController menu;
        public bool IsVisible => panel.activeSelf;
        public string RowsText => rowsText.text;
        private void Awake() => menu = FindAnyObjectByType<InGameMenuController>();
        private void Update()
        {
            var state = lifecycleHud.State;
            var show = state != null && NetworkClient.localPlayer != null && state.Phase != MatchLifecyclePhase.Ended
                && Keyboard.current != null && Keyboard.current.tabKey.isPressed && (menu == null || !menu.IsPaused);
            panel.SetActive(show);
            if (show) rowsText.text = FormatRows(state.Scores);
        }
        public static string FormatRows(IReadOnlyList<MatchScoreEntry> rows)
        {
            var text = new StringBuilder("<mspace=15>RK  PLAYER    K   D   DAMAGE  STATUS\n");
            foreach (var row in rows)
                text.AppendFormat("{0,-4}Player {1,-3}{2,-4}{3,-4}{4,-8}{5}\n", row.Rank, row.SlotIndex + 1,
                    row.Kills, row.Deaths, row.DamageDealt, row.IsConnected ? row.LifeState.ToString() : "Disconnected");
            return text.Append("</mspace>").ToString();
        }
    }
}
