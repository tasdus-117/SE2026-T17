using System.Linq;
using Mirror;
using SmallArena.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Client.UI
{
    [RequireComponent(typeof(LobbyRoomView))]
    public sealed class LobbyRoomController : MonoBehaviour
    {
        [SerializeField] private ArenaNetworkManager manager = null;
        private LobbyRoomView view;
        private bool visible;
        private bool wasConnecting;

        private void OnEnable()
        {
            view = GetComponent<LobbyRoomView>();
            if (NetworkManager.singleton is ArenaNetworkManager existing && existing != null
                && (existing.gameObject.scene.name == "DontDestroyOnLoad" || existing.gameObject.scene == gameObject.scene)) manager = existing;
            ReleaseIdleManager();
            view.HostButton.onClick.AddListener(Host);
            view.JoinButton.onClick.AddListener(Join);
            view.BackButton.onClick.AddListener(Back);
            view.ReadyButton.onClick.AddListener(Ready);
            view.StartButton.onClick.AddListener(StartMatch);
            view.LeaveButton.onClick.AddListener(Leave);
            if (manager != null) manager.RoomChanged += Refresh;
        }
        private void OnDisable()
        {
            view.HostButton.onClick.RemoveListener(Host);
            view.JoinButton.onClick.RemoveListener(Join);
            view.BackButton.onClick.RemoveListener(Back);
            view.ReadyButton.onClick.RemoveListener(Ready);
            view.StartButton.onClick.RemoveListener(StartMatch);
            view.LeaveButton.onClick.RemoveListener(Leave);
            if (manager != null) manager.RoomChanged -= Refresh;
        }
        private void ReleaseIdleManager()
        {
            // Idle roots belong to the menu scene. Only active sessions persist across arena loads.
            if (manager == null || NetworkClient.active || NetworkServer.active || NetworkClient.connection != null) return;
            manager.dontDestroyOnLoad = false;
            if (manager.gameObject.scene != gameObject.scene) SceneManager.MoveGameObjectToScene(manager.gameObject, gameObject.scene);
        }
        private void Update()
        {
            ReleaseIdleManager();
            // Mirror finishes its connection-state change after the disconnect callback returns.
            // Refresh once when that transition completes, not every frame.
            if (visible && manager != null && wasConnecting != (NetworkClient.active && manager.CurrentRoom.Phase == LobbyPhase.Offline)) Refresh();
        }
        public void ShowConnectionPanel()
        {
            visible = true;
            SceneNavigation.ShowMenuCursor();
            Refresh();
        }
        private void Host() { if (manager != null) manager.HostRoom(); Refresh(); }
        private void Join() { if (manager != null) manager.JoinRoom(view.Address.text); Refresh(); }
        private void Back()
        {
            visible = false;
            if (manager != null) manager.LeaveRoom();
            view.ShowMenu();
        }
        private void Leave() { if (manager != null) manager.LeaveRoom(); Refresh(); }
        private void Ready()
        {
            if (manager == null) return;
            var local = (manager.CurrentRoom.Members ?? new LobbyMember[0]).FirstOrDefault(member => member.ConnectionId == manager.CurrentRoom.LocalConnectionId);
            manager.RequestReady(!local.IsReady);
        }
        private void StartMatch() { if (manager != null) manager.RequestRoomStart(); }
        private void Refresh()
        {
            if (!visible || manager == null) return;
            ReleaseIdleManager();
            SceneNavigation.ShowMenuCursor();
            wasConnecting = NetworkClient.active && manager.CurrentRoom.Phase == LobbyPhase.Offline;
            view.Render(manager.CurrentRoom, wasConnecting, manager.ConnectionStatus);
        }
    }
}
