using UnityEngine;

namespace SmallArena.Client.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Awake() => SceneNavigation.ShowMenuCursor();

        private void OnEnable() => SceneNavigation.ShowMenuCursor();

        public void OnSinglePlayerClicked() => SceneNavigation.LoadSinglePlayer();

        public void OnMultiplayerClicked() => Debug.Log("[TV1] Multiplayer will connect via Bootstrap scene.");

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
