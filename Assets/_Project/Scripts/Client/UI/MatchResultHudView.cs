using System;
using System.Linq;
using Mirror;
using SmallArena.Domain;
using SmallArena.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Client.UI
{
    public sealed class MatchResultHudView : MonoBehaviour
    {
        [SerializeField] private MatchLifecycleHudView lifecycleHud = null;
        [SerializeField] private GameObject panel = null;
        [SerializeField] private TextMeshProUGUI headerText = null;
        [SerializeField] private TextMeshProUGUI standingsText = null;
        [SerializeField] private TextMeshProUGUI countdownText = null;
        [SerializeField] private Button returnButton = null;
        [SerializeField] private Button leaveButton = null;
        private InGameMenuController menu;
        public bool IsVisible => panel.activeSelf;
        public string HeaderText => headerText.text;
        public string StandingsText => standingsText.text;
        public string CountdownText => countdownText.text;
        public Button ReturnButton => returnButton;
        private void Awake() => menu = FindAnyObjectByType<InGameMenuController>();
        private void OnEnable() { returnButton.onClick.AddListener(ReturnToLobby); leaveButton.onClick.AddListener(Leave); }
        private void OnDisable()
        {
            returnButton.onClick.RemoveListener(ReturnToLobby); leaveButton.onClick.RemoveListener(Leave);
            if (menu != null) menu.SetCompetitiveResultVisible(false);
        }
        private static void ReturnToLobby() { if (NetworkManager.singleton is ArenaNetworkManager manager) manager.RequestReturnToLobby(); }
        private static void Leave() => SceneNavigation.LoadMainMenu();
        private void Update()
        {
            var state = lifecycleHud.State;
            var local = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<NetworkHealth>() : null;
            var visible = state != null && state.Phase == MatchLifecyclePhase.Ended && local != null && local.SessionId == state.SessionId;
            if (panel.activeSelf != visible)
                panel.SetActive(visible);
            if (menu != null && menu.CompetitiveResultVisible != visible) menu.SetCompetitiveResultVisible(visible);
            if (!visible) return;
            var row = state.Scores.FirstOrDefault(s => s.ParticipantId == local.ParticipantId);
            headerText.text = state.WinnerParticipantId == local.ParticipantId ? "VICTORY" : $"DEFEAT — Rank #{row.Rank}";
            standingsText.text = ScoreboardHudView.FormatRows(state.Scores);
            countdownText.text = $"Returning to lobby in {Math.Ceiling(state.PostMatchRemaining)}s...";
            var manager = NetworkManager.singleton as ArenaNetworkManager;
            returnButton.gameObject.SetActive(manager != null && (manager.CurrentRoom.Members ?? Array.Empty<LobbyMember>())
                .Any(m => m.ConnectionId == manager.CurrentRoom.LocalConnectionId && m.IsHost));
        }
    }
}
