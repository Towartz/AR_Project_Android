using UnityEngine;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Switches between portrait and landscape UI roots based on the current screen aspect.
    /// </summary>
    public class ResponsiveLayoutController : MonoBehaviour
    {
        [SerializeField] GameObject portraitRoot;
        [SerializeField] GameObject landscapeRoot;

        int lastScreenWidth = -1;
        int lastScreenHeight = -1;

        public void Configure(GameObject portrait, GameObject landscape)
        {
            portraitRoot = portrait;
            landscapeRoot = landscape;
        }

        void Start()
        {
            Apply(force: true);
        }

        void Update()
        {
            Apply();
        }

        void Apply(bool force = false)
        {
            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);

            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
                return;

            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;

            bool useLandscape = screenWidth > screenHeight;
            if (portraitRoot != null)
                portraitRoot.SetActive(!useLandscape || landscapeRoot == null);
            if (landscapeRoot != null)
                landscapeRoot.SetActive(useLandscape || portraitRoot == null);
        }
    }
}
