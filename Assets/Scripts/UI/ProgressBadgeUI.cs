using ARtiGraf.Core;
using ARtiGraf.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Menampilkan badge progress konten yang sudah ditemukan di scene manapun.
    /// Hubungkan ke MaterialContentController untuk mendapat total konten yang benar.
    /// </summary>
    public class ProgressBadgeUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] Text    badgeText;
        [SerializeField] Slider  progressSlider;
        [SerializeField] Image   badgeIcon;
        [SerializeField] Color   completedColor = new Color(0.13f, 0.77f, 0.37f);
        [SerializeField] Color   normalColor    = new Color(0.36f, 0.64f, 1.00f);

        [Header("Config")]
        [SerializeField] AR.MaterialContentController contentController;
        [SerializeField] bool refreshEveryFrame = false;

        int cachedDiscovered = -1;
        int cachedTotal      = -1;

        void OnEnable()  => Refresh();
        void LateUpdate() { if (refreshEveryFrame) Refresh(); }

        public void Refresh()
        {
            int discovered = AppSession.GetDiscoveredContentCount(AppSession.SelectedCategory);
            int total      = contentController != null
                ? contentController.GetRuntimeContentCount(AppSession.SelectedCategory) : 0;

            if (discovered == cachedDiscovered && total == cachedTotal) return;
            cachedDiscovered = discovered;
            cachedTotal      = total;

            bool complete = total > 0 && discovered >= total;

            if (badgeText != null)
            {
                badgeText.text = total > 0
                    ? discovered + " / " + total + " ditemukan"
                    : discovered + " ditemukan";
            }

            if (progressSlider != null)
            {
                progressSlider.value = total > 0 ? (float)discovered / total : 0f;
            }

            if (badgeIcon != null)
            {
                badgeIcon.color = complete ? completedColor : normalColor;
            }
        }
    }
}
