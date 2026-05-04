using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Keeps full-page artwork proportional and swaps portrait/landscape sprites.
    /// UI hotspots can be parented to the same frame so their anchors stay aligned
    /// with the artwork instead of the physical screen.
    /// </summary>
    public class ResponsiveArtworkFrame : MonoBehaviour
    {
        [SerializeField] Image artworkImage;
        [SerializeField] AspectRatioFitter aspectFitter;
        [SerializeField] Sprite portraitSprite;
        [SerializeField] Sprite landscapeSprite;

        int lastScreenWidth = -1;
        int lastScreenHeight = -1;

        public void Configure(Image image, AspectRatioFitter fitter, Sprite portrait, Sprite landscape)
        {
            artworkImage = image;
            aspectFitter = fitter;
            portraitSprite = portrait;
            landscapeSprite = landscape != null ? landscape : portrait;
            Apply(force: true);
        }

        void Awake()
        {
            if (artworkImage == null)
                artworkImage = GetComponentInChildren<Image>(true);
            if (aspectFitter == null)
                aspectFitter = GetComponent<AspectRatioFitter>();

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
            Sprite selected = useLandscape && landscapeSprite != null ? landscapeSprite : portraitSprite;
            if (selected == null)
                return;

            if (artworkImage != null)
            {
                artworkImage.sprite = selected;
                artworkImage.preserveAspect = true;
                artworkImage.raycastTarget = false;
            }

            if (aspectFitter != null && selected.rect.height > 0f)
                aspectFitter.aspectRatio = selected.rect.width / selected.rect.height;
        }
    }
}
