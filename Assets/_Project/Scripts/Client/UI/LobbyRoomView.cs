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
            membersText.text = $"ROOM  {members.Length} / 4\n\n" + string.Join("\n", members.Select(member =>
                $"Player {member.ConnectionId + 1}{(member.IsHost ? " (Host)" : "")}{(member.ConnectionId == snapshot.LocalConnectionId ? " (You)" : "")} — {(member.IsReady ? "Ready" : "Not ready")}"));
            var local = members.FirstOrDefault(member => member.ConnectionId == snapshot.LocalConnectionId);
            readyLabel.text = local.IsReady ? "Not Ready" : "Ready";
            readyButton.interactable = snapshot.Phase == LobbyPhase.Waiting;
            startButton.gameObject.SetActive(local.IsHost);
            startButton.interactable = snapshot.Phase == LobbyPhase.Waiting && members.Length > 0 && members.All(member => member.IsReady);
            leaveButton.interactable = snapshot.Phase == LobbyPhase.Waiting;
            if (snapshot.Phase == LobbyPhase.Loading) membersText.text += "\n\nLoading arena...";
        }
    }
}
