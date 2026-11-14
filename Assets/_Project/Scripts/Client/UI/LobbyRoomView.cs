using System.Linq;
using SmallArena.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Client.UI
{
    /// <summary>Serialized presentation only. Membership and readiness come from server snapshots.</summary>
    public sealed class LobbyRoomView : MonoBehaviour
    {
        [SerializeField] private GameObject menuCard = null;
        [SerializeField] private GameObject connectionPanel = null;
        [SerializeField] private GameObject roomPanel = null;
        [SerializeField] private TMP_InputField address = null;
        [SerializeField] private Button hostButton = null;
        [SerializeField] private Button joinButton = null;
        [SerializeField] private Button backButton = null;
        [SerializeField] private Button readyButton = null;
        [SerializeField] private Button startButton = null;
        [SerializeField] private Button leaveButton = null;
        [SerializeField] private TextMeshProUGUI statusText = null;
        [SerializeField] private TextMeshProUGUI membersText = null;
        [SerializeField] private TextMeshProUGUI readyLabel = null;
        [SerializeField] private TextMeshProUGUI[] slotNames = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] slotStatuses = new TextMeshProUGUI[4];
        [SerializeField] private GameObject errorPanel = null;
        [SerializeField] private TextMeshProUGUI errorText = null;
        [SerializeField] private Button dismissErrorButton = null;
        public Button DismissErrorButton => dismissErrorButton;
        public bool ErrorVisible => errorPanel != null && errorPanel.activeSelf;
        public string ErrorMessage => errorText != null ? errorText.text : "";
        public string SlotName(int index) => slotNames[index].text;
        public string SlotStatus(int index) => slotStatuses[index].text;
        public void ShowError(string reason)
        {
            if (errorText != null) errorText.text = "Cannot join: " + reason;
            if (errorPanel != null) errorPanel.SetActive(true);
        }
        public void DismissError() { if (errorPanel != null) errorPanel.SetActive(false); }
        public void ShowSessionError(string message)
        {
            if (errorText != null) errorText.text = message;
            if (errorPanel != null) errorPanel.SetActive(true);
        }
        public TMP_InputField Address => address;
        public Button HostButton => hostButton;
        public Button JoinButton => joinButton;
        public Button BackButton => backButton;
        public Button ReadyButton => readyButton;
        public Button StartButton => startButton;
        public Button LeaveButton => leaveButton;
        public bool ConnectionVisible => connectionPanel.activeSelf;
        public bool RoomVisible => roomPanel.activeSelf;

        public void ShowMenu()
        {
            DismissError();
            menuCard.SetActive(true);
            connectionPanel.SetActive(false);
            roomPanel.SetActive(false);
        }
        public void Render(LobbySnapshot snapshot, bool connecting, string status)
        {
            var inRoom = snapshot.Phase != LobbyPhase.Offline;
            menuCard.SetActive(false);
            connectionPanel.SetActive(!inRoom);
            roomPanel.SetActive(inRoom);
            hostButton.interactable = joinButton.interactable = !connecting;
            address.interactable = !connecting;
            statusText.text = status;
            var members = snapshot.Members ?? new LobbyMember[0];
            membersText.text = $"ROOM  {members.Length} / 4";
            for (var index = 0; index < LobbyLimits.Capacity; index++)
            {
                var occupied = members.Any(member => member.SlotIndex == index);
                var member = members.FirstOrDefault(item => item.SlotIndex == index);
                if (index < slotNames.Length && slotNames[index] != null)
                    slotNames[index].text = occupied
                        ? $"{index + 1}. Player {member.ConnectionId + 1}{(member.IsHost ? " (Host)" : "")}{(member.ConnectionId == snapshot.LocalConnectionId ? " (You)" : "")}"
                        : $"{index + 1}. Empty slot";
                if (index < slotStatuses.Length && slotStatuses[index] != null)
                {
                    slotStatuses[index].text = !occupied ? "Waiting..." : member.IsReady ? "READY" : "Not Ready";
                    slotStatuses[index].color = !occupied ? Color.gray : member.IsReady
                        ? new Color(0.3f, 0.9f, 0.45f) : new Color(1f, 0.6f, 0.25f);
                }
            }
            var local = members.FirstOrDefault(member => member.ConnectionId == snapshot.LocalConnectionId);
            readyLabel.text = local.IsReady ? "Not Ready" : "Ready";
            readyButton.interactable = snapshot.Phase == LobbyPhase.Waiting;
            startButton.gameObject.SetActive(local.IsHost);
            startButton.interactable = local.IsHost && snapshot.CanStart;
            leaveButton.interactable = snapshot.Phase == LobbyPhase.Waiting;
            if (snapshot.Phase == LobbyPhase.Loading) membersText.text += "\n\nLoading arena...";
        }
    }
}
