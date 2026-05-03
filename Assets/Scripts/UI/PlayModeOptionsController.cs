using ARtiGraf.Core;
using UnityEngine;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Overlay pilihan mode setelah user membaca petunjuk di MaterialSelectScene.
    /// Main Menu tetap sederhana; fitur lanjutan dibuka dari sini.
    /// </summary>
    public class PlayModeOptionsController : MonoBehaviour
    {
        [SerializeField] GameObject optionsPanel;
        [SerializeField] SceneNavigationController navigator;
        [SerializeField] bool hideOnAwake = true;

        void Awake()
        {
            EnsureReferences();
            if (hideOnAwake && optionsPanel != null)
                optionsPanel.SetActive(false);
        }

        void EnsureReferences()
        {
            if (navigator == null)
                navigator = GetComponent<SceneNavigationController>();

            if (navigator == null)
                navigator = FindAnyObjectByType<SceneNavigationController>(FindObjectsInactive.Include);
        }

        public void ShowOptions()
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(true);
        }

        public void HideOptions()
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
        }

        public void OpenAR()
        {
            EnsureReferences();
            navigator?.OpenARForAllContent();
        }

        public void OpenQuiz()
        {
            EnsureReferences();
            navigator?.OpenQuizHunt();
        }

        public void OpenCollection()
        {
            EnsureReferences();
            navigator?.OpenCollection();
        }

        public void OpenQuizHunt()
        {
            EnsureReferences();
            navigator?.OpenQuizHunt();
        }
    }
}
