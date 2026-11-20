using System;
using Mirror;
using SmallArena.Domain;
using SmallArena.Networking;
using TMPro;
using UnityEngine;

namespace SmallArena.Client.UI
{
    /// <summary>Projects synchronized deadlines; never changes match or participant state.</summary>
    public sealed class MatchLifecycleHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI clockText = null;
        [SerializeField] private TextMeshProUGUI phaseText = null;
        [SerializeField] private TextMeshProUGUI respawnText = null;
        private NetworkMatchController state;
        public string ClockText => clockText.text;
        public string PhaseText => phaseText.text;
        public string RespawnText => respawnText.text;
        private void Update()
        {
            if (state == null && NetworkClient.active)
                foreach (var identity in NetworkClient.spawned.Values)
                    if (identity != null && identity.TryGetComponent<NetworkMatchController>(out var found)) { state = found; break; }
            var health = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<NetworkHealth>() : null;
            var visible = state != null && health != null && health.IsLifecycleBound;
            clockText.gameObject.SetActive(visible); phaseText.gameObject.SetActive(visible); respawnText.gameObject.SetActive(visible);
            if (!visible) return;
            var seconds = (int)Math.Ceiling(state.DisplayClock);
            clockText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            phaseText.text = state.Phase == MatchLifecyclePhase.Countdown ? $"STARTING IN {Math.Ceiling(state.CountdownRemaining)}..."
                : state.Phase == MatchLifecyclePhase.WaitingForPlayers ? "WAITING FOR PLAYERS..."
                : state.Phase == MatchLifecyclePhase.Ended ? "MATCH ENDED" : "";
            respawnText.text = state.Phase != MatchLifecyclePhase.InProgress ? ""
                : health.LifeState == ParticipantLifeState.Respawning ? $"Respawning in {health.RespawnRemainingSeconds:0.0}s..."
                : health.LifeState == ParticipantLifeState.Eliminated ? "ELIMINATED" : "";
        }
    }
}
