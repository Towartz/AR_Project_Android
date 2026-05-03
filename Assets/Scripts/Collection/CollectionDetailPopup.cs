using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.Collection
{
    /// <summary>
    /// Popup detail item saat anak tap item di CollectionScene.
    /// Tampilkan: nama, deskripsi, fun fact, tombol TTS eja ulang.
    /// </summary>
    public class CollectionDetailPopup : MonoBehaviour
    {
        [SerializeField] GameObject popupPanel;
        [SerializeField] Image thumbnailImage;
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;
        [SerializeField] Text descriptionText;
        [SerializeField] Text funFactText;
        [SerializeField] GameObject starBadge;
        [SerializeField] Button ttsButton;
        [SerializeField] Text ttsButtonLabel;

        MaterialContentData currentContent;

        void Start()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            ApplyResponsiveLayout();
        }

        public void Show(MaterialContentData content)
        {
            if (content == null) return;
            EnsureReferences();
            currentContent = content;

            if (thumbnailImage != null)
            {
                bool hasImage = RuntimeSpriteCache.ApplyContentSprite(thumbnailImage, content, Color.white);
                thumbnailImage.gameObject.SetActive(hasImage);
            }

            if (titleText != null)       titleText.text       = content.Title;
            if (subtitleText != null)    subtitleText.text    = content.Subtitle;
            if (descriptionText != null) descriptionText.text = content.Description;
            if (funFactText != null)
            {
                funFactText.text = !string.IsNullOrWhiteSpace(content.FunFact)
                    ? content.FunFact : string.Empty;
                funFactText.gameObject.SetActive(!string.IsNullOrWhiteSpace(content.FunFact));
            }

            bool hasStar = AppSession.HasStarForContent(content.Id);
            if (starBadge != null) starBadge.SetActive(hasStar);
            if (ttsButtonLabel != null) ttsButtonLabel.text = "Dengar: " + content.Title;

            ApplyResponsiveLayout();
            if (popupPanel != null) popupPanel.SetActive(true);
        }

        public void OnTTSButtonPressed()
        {
            if (currentContent == null) return;
            TTSController tts = EnsureTTSController();
            tts?.SpellThenSpeak(currentContent.SpellWord, currentContent.Title, currentContent.NameAudioClip);
        }

        public void OnClosePressed()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            currentContent = null;
        }

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyResponsiveLayout();
        }

        void ApplyResponsiveLayout()
        {
            EnsureReferences();
            RectTransform panelRect = popupPanel != null ? popupPanel.GetComponent<RectTransform>() : null;
            bool portrait = Screen.height >= Screen.width;

            if (panelRect != null)
            {
                SetAnchors(panelRect,
                    portrait ? new Vector2(0.06f, 0.14f) : new Vector2(0.20f, 0.08f),
                    portrait ? new Vector2(0.94f, 0.84f) : new Vector2(0.80f, 0.92f));
            }

            SetAnchors(GetRect(thumbnailImage),
                portrait ? new Vector2(0.27f, 0.55f) : new Vector2(0.07f, 0.18f),
                portrait ? new Vector2(0.73f, 0.91f) : new Vector2(0.38f, 0.88f));
            SetAnchors(GetRect(titleText),
                portrait ? new Vector2(0.08f, 0.45f) : new Vector2(0.42f, 0.72f),
                portrait ? new Vector2(0.92f, 0.54f) : new Vector2(0.92f, 0.88f));
            SetAnchors(GetRect(subtitleText),
                portrait ? new Vector2(0.08f, 0.38f) : new Vector2(0.42f, 0.62f),
                portrait ? new Vector2(0.92f, 0.45f) : new Vector2(0.92f, 0.72f));
            SetAnchors(GetRect(descriptionText),
                portrait ? new Vector2(0.10f, 0.21f) : new Vector2(0.42f, 0.30f),
                portrait ? new Vector2(0.90f, 0.37f) : new Vector2(0.92f, 0.60f));
            SetAnchors(GetRect(funFactText),
                portrait ? new Vector2(0.10f, 0.12f) : new Vector2(0.42f, 0.18f),
                portrait ? new Vector2(0.90f, 0.21f) : new Vector2(0.92f, 0.30f));
            SetAnchors(GetRect(ttsButton),
                portrait ? new Vector2(0.08f, 0.035f) : new Vector2(0.40f, 0.045f),
                portrait ? new Vector2(0.47f, 0.11f) : new Vector2(0.64f, 0.15f));

            Button closeButton = popupPanel != null ? popupPanel.transform.Find("CloseButton")?.GetComponent<Button>() : null;
            SetAnchors(GetRect(closeButton),
                portrait ? new Vector2(0.53f, 0.035f) : new Vector2(0.68f, 0.045f),
                portrait ? new Vector2(0.92f, 0.11f) : new Vector2(0.92f, 0.15f));

            ConfigureText(titleText, portrait ? 42 : 44, 24, TextAnchor.MiddleCenter);
            ConfigureText(subtitleText, portrait ? 24 : 27, 16, TextAnchor.MiddleCenter);
            ConfigureText(descriptionText, portrait ? 24 : 27, 16, TextAnchor.MiddleCenter);
            ConfigureText(funFactText, portrait ? 22 : 25, 15, TextAnchor.MiddleCenter);
            ConfigureText(ttsButtonLabel, portrait ? 22 : 26, 15, TextAnchor.MiddleCenter);
        }

        void EnsureReferences()
        {
            if (popupPanel == null)
                popupPanel = transform.Find("PopupPanel")?.gameObject;
            Transform root = popupPanel != null ? popupPanel.transform : transform;
            if (thumbnailImage == null)
                thumbnailImage = FindChildImage(root, "Thumbnail");
            if (titleText == null)
                titleText = FindChildText(root, "Title");
            if (subtitleText == null)
                subtitleText = FindChildText(root, "Subtitle");
            if (descriptionText == null)
                descriptionText = FindChildText(root, "Description");
            if (funFactText == null)
                funFactText = FindChildText(root, "FunFact");
            if (ttsButton == null)
                ttsButton = FindChildButton(root, "TTSButton");
            if (ttsButtonLabel == null && ttsButton != null)
                ttsButtonLabel = ttsButton.GetComponentInChildren<Text>(true);
        }

        static TTSController EnsureTTSController()
        {
            TTSController tts = TTSController.Instance ?? FindAnyObjectByType<TTSController>();
            if (tts != null) return tts;

            GameObject ttsObject = new GameObject("BuhenAR_RuntimeTTS");
            DontDestroyOnLoad(ttsObject);
            ttsObject.AddComponent<AudioSource>();
            return ttsObject.AddComponent<TTSController>();
        }

        static RectTransform GetRect(Graphic graphic) => graphic != null ? graphic.rectTransform : null;
        static RectTransform GetRect(Selectable selectable) => selectable != null ? selectable.GetComponent<RectTransform>() : null;

        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ConfigureText(Text text, int maxSize, int minSize, TextAnchor anchor)
        {
            BuhenARTextStyle.Configure(text, maxSize, minSize, anchor, VerticalWrapMode.Truncate, 1.08f);
        }

        static Image FindChildImage(Transform root, string childName)
        {
            if (root == null) return null;
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i].GetComponent<Image>();
            }
            return null;
        }

        static Text FindChildText(Transform root, string childName)
        {
            if (root == null) return null;
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i].GetComponent<Text>();
            }
            return null;
        }

        static Button FindChildButton(Transform root, string childName)
        {
            if (root == null) return null;
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i].GetComponent<Button>();
            }
            return null;
        }
    }
}
