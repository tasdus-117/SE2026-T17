using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmallArena.Client.UI
{
    /// <summary>Presentation only: panels do not change match rules or network state.</summary>
    public sealed class InGameMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel = null;
        [SerializeField] private GameObject resultPanel = null;
        [SerializeField] private TextMeshProUGUI resultHeader = null;
        [SerializeField] private Button resumeButton = null;
        [SerializeField] private Button leaveButton = null;
        [SerializeField] private Button resultReturnButton = null;
        public bool PauseVisible => pausePanel.activeSelf;
        public bool ResultVisible => resultPanel.activeSelf;
        public string ResultHeader => resultHeader.text;
        public Button ResumeButton => resumeButton;
        public Button LeaveButton => leaveButton;
        public Button ResultReturnButton => resultReturnButton;

        public void ShowPause(bool show) => pausePanel.SetActive(show);
        public void ShowResult(bool winner)
        {
            pausePanel.SetActive(false);
            resultHeader.text = winner ? "VICTORY" : "DEFEAT";
            resultPanel.SetActive(true);
        }
        public void SetLeaving()
        {
            resumeButton.interactable = leaveButton.interactable = resultReturnButton.interactable = false;
        }
    }
}
