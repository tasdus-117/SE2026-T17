using UnityEngine;

namespace SmallArena.Client.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private LobbyRoomController lobbyController = null;
        private void Awake() => SceneNavigation.ShowMenuCursor();

        private void OnEnable() => SceneNavigation.ShowMenuCursor();

        public void OnSinglePlayerClicked() => SceneNavigation.LoadSinglePlayer();

        public void OnMultiplayerClicked()
        {
            if (lobbyController != null) lobbyController.ShowConnectionPanel();
        }

        public void OnTutorialClicked() => Debug.Log("Tutorial Coming Soon");

        public void OnExitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
