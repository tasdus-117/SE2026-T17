using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Client.UI
{
    /// <summary>Scene transitions only; match rules and initialization belong to the loaded scene.</summary>
    public static class SceneNavigation
    {
        public static void ShowMenuCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void LoadSinglePlayer() => SceneManager.LoadScene("Arena_MVP", LoadSceneMode.Single);

        public static void LoadMainMenu() => SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);

        public static void ReloadActiveScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }
}
