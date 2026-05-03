using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Lightweight tutorial animation for the pre-camera page.
    /// Uses procedural UI motion instead of GIF/video to keep APK size and memory stable.
    /// </summary>
    public class TutorialMotionController : MonoBehaviour
    {
        [Header("Animated Parts")]
        public RectTransform card;
        public RectTransform scanFrame;
        public RectTransform arrow;
        public RectTransform arObject;
        public Image scanFrameImage;

        [Header("Motion")]
        [SerializeField] float speed = 1f;
        [SerializeField] float cardFloat = 10f;
        [SerializeField] float arrowTravel = 34f;
        [SerializeField] float pulseScale = 0.08f;

        Vector2 cardBasePosition;
        Vector2 arrowBasePosition;
        Vector3 frameBaseScale;
        Vector3 arBaseScale;
        Color frameBaseColor;

        void Awake()
        {
            if (card != null)
                cardBasePosition = card.anchoredPosition;

            if (arrow != null)
                arrowBasePosition = arrow.anchoredPosition;

            if (scanFrame != null)
                frameBaseScale = scanFrame.localScale;

            if (arObject != null)
                arBaseScale = arObject.localScale;

            if (scanFrameImage != null)
                frameBaseColor = scanFrameImage.color;
        }

        void Update()
        {
            float time = Time.unscaledTime * speed;
            float wave = Mathf.Sin(time * 2.1f);
            float pulse = 1f + (Mathf.Sin(time * 3.4f) * 0.5f + 0.5f) * pulseScale;

            if (card != null)
                card.anchoredPosition = cardBasePosition + new Vector2(0f, wave * cardFloat);

            if (arrow != null)
                arrow.anchoredPosition = arrowBasePosition + new Vector2(Mathf.Sin(time * 2.8f) * arrowTravel, 0f);

            if (scanFrame != null)
                scanFrame.localScale = frameBaseScale * pulse;

            if (arObject != null)
            {
                arObject.localScale = arBaseScale * (1f + (pulse - 1f) * 0.75f);
                arObject.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.6f) * 5f);
            }

            if (scanFrameImage != null)
            {
                Color color = frameBaseColor;
                color.a = Mathf.Lerp(0.65f, 1f, Mathf.Sin(time * 3.2f) * 0.5f + 0.5f);
                scanFrameImage.color = color;
            }
        }
    }
}
