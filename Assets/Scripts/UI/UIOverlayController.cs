using ARtiGraf.Core;
using ARtiGraf.Data;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    public class UIOverlayController : MonoBehaviour
    {
        [Header("Status & Context")]
        [SerializeField] Text statusText;
        [SerializeField] Text contextText;

        [Header("Content Panel")]
        [SerializeField] Text  titleText;
        [SerializeField] Text  subtitleText;
        [SerializeField] Text  descriptionText;
        [SerializeField] Text  categoryText;
        [SerializeField] Text  objectTypeText;
        [SerializeField] Text  focusText;

        [Header("Panel GameObjects")]
        [SerializeField] GameObject infoPanel;
        [SerializeField] GameObject scanGuide;
        [SerializeField] GameObject helpPanel;
        [SerializeField] Canvas     overlayCanvas;

        [Header("Editor / Preview")]
        [SerializeField] Image    previewBackdropImage;
        [SerializeField] RawImage editorCameraFeedImage;
        [SerializeField] bool     editorCameraFeedMirrorHorizontally;
        [SerializeField] bool     editorCameraFeedMirrorVertically = true;
        [SerializeField] int      editorCameraFeedRotationOffset;

        [Header("Panel Animation")]
        [SerializeField] float panelSlideDistance = 80f;
        [SerializeField] float panelAnimDuration  = 0.22f;

        [Header("Responsive AR Scanner Layout")]
        [SerializeField] RectTransform topBarRect;
        [SerializeField] RectTransform statusPanelRect;
        [SerializeField] RectTransform scanGuideRect;
        [SerializeField] RectTransform infoPanelRect;
        [SerializeField] RectTransform infoToggleButtonRect;
        [SerializeField] RectTransform helpPanelRect;
        [SerializeField] RectTransform backButtonRect;
        [SerializeField] RectTransform helpButtonRect;
        [SerializeField] RectTransform musicButtonRect;
        [SerializeField] RectTransform flashButtonRect;
        [SerializeField] Text          topTitleText;
        [SerializeField] Text          scanGuideText;
        [SerializeField] Text          helpText;

        bool previewModeActive;
        bool editorCameraFeedActive;
        bool contentVisible;
        Coroutine panelAnimCoroutine;
        int lastLayoutWidth = -1;
        int lastLayoutHeight = -1;

        void Awake()
        {
            EnsurePreviewBackdrop();
            EnsureScannerLayoutReferences();
            ApplyResponsiveScannerLayout(force: true);
            SetInfoButtonVisible(false);
            if (infoPanel != null) infoPanel.SetActive(false);
        }

        void Update()
        {
            ApplyResponsiveScannerLayout();
        }

        // ── Status ────────────────────────────────────────────────────────────
        public void SetTrackingStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        public void SetSessionContext(string message)
        {
            if (contextText != null) contextText.text = message;
        }

        // ── Content show / hide ───────────────────────────────────────────────
        public void ShowContent(MaterialContentData content)
        {
            if (content == null) { HideContent(); return; }

            if (titleText       != null) titleText.text       = content.Title;
            if (subtitleText    != null) subtitleText.text    = LimitText(content.Subtitle, 56);
            if (descriptionText != null) descriptionText.text = LimitText(content.Description, 145);
            if (categoryText    != null)
                categoryText.text = "Materi: " + AppSession.GetCategoryLabel(content.Category);
            if (focusText       != null) focusText.text       = LimitText(BuildFocusText(content), 76);
            if (objectTypeText  != null) objectTypeText.text  = LimitText(BuildObjectText(content), 44);
            if (contextText     != null)
            {
                string scanLabel = AppSession.TotalScansThisRun > 0
                    ? "  •  Scan " + AppSession.TotalScansThisRun : string.Empty;
                contextText.text = "Mode " + AppSession.GetSelectedCategoryLabel() + scanLabel;
            }

            contentVisible = true;
            SetInfoButtonVisible(true);
            SetInfoPanelVisible(false);
            if (scanGuide != null) scanGuide.SetActive(false);
        }

        public void HideContent()
        {
            previewModeActive = false;
            contentVisible = false;
            UpdatePreviewBackdropVisibility();
            SetInfoButtonVisible(false);
            SetInfoPanelVisible(false);
            if (focusText  != null) focusText.text  = string.Empty;
            if (scanGuide  != null) scanGuide.SetActive(true);
        }

        // ── Animated panel toggle ─────────────────────────────────────────────
        void SetInfoPanelVisible(bool visible)
        {
            if (infoPanel == null) return;
            if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);

            ApplyResponsiveScannerLayout(force: true);
            if (!infoPanel.activeSelf && visible) infoPanel.SetActive(true);
            panelAnimCoroutine = StartCoroutine(AnimatePanel(infoPanel, visible));
        }

        IEnumerator AnimatePanel(GameObject panel, bool show)
        {
            RectTransform rt = panel.GetComponent<RectTransform>();
            if (rt == null) { panel.SetActive(show); yield break; }

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();

            float startAlpha  = cg.alpha;
            float targetAlpha = show ? 1f : 0f;
            Vector2 startPos  = rt.anchoredPosition;
            float offsetY     = show ? -panelSlideDistance : 0f;
            float targetOffY  = show ? 0f : -panelSlideDistance;
            rt.anchoredPosition = new Vector2(startPos.x, startPos.y + offsetY);

            float elapsed = 0f;
            while (elapsed < panelAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / panelAnimDuration);
                float smooth = t * t * (3f - 2f * t);
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, smooth);
                rt.anchoredPosition = new Vector2(startPos.x,
                    Mathf.Lerp(startPos.y + offsetY, startPos.y + targetOffY, smooth));
                yield return null;
            }

            cg.alpha = targetAlpha;
            rt.anchoredPosition = new Vector2(startPos.x, startPos.y + targetOffY);
            if (!show) panel.SetActive(false);
        }

        // ── Preview mode ──────────────────────────────────────────────────────
        public void SetPreviewModeActive(bool isActive)
        {
            previewModeActive = isActive;
            UpdatePreviewBackdropVisibility();
        }

        public void SetEditorCameraFeedActive(bool isActive)
        {
            editorCameraFeedActive = isActive;
            UpdatePreviewBackdropVisibility();
        }

        public void SetEditorCameraFeedTexture(Texture texture, bool isActive, bool verticallyMirrored, int rotationAngle)
        {
            EnsureEditorCameraFeed();
            if (editorCameraFeedImage == null) return;

            bool flipH = editorCameraFeedMirrorHorizontally;
            bool flipV = verticallyMirrored || editorCameraFeedMirrorVertically;
            editorCameraFeedImage.texture = texture;
            editorCameraFeedImage.uvRect  = new Rect(flipH ? 1f : 0f, flipV ? 1f : 0f,
                                                      flipH ? -1f : 1f, flipV ? -1f : 1f);
            editorCameraFeedImage.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, -(rotationAngle + editorCameraFeedRotationOffset));
            editorCameraFeedImage.gameObject.SetActive(isActive && texture != null);
        }

        void UpdatePreviewBackdropVisibility()
        {
            EnsurePreviewBackdrop();
            EnsureEditorCameraFeed();
            if (editorCameraFeedImage != null && !editorCameraFeedActive)
                editorCameraFeedImage.gameObject.SetActive(false);
            if (previewBackdropImage != null)
                previewBackdropImage.gameObject.SetActive(previewModeActive && !editorCameraFeedActive);
        }

        // ── Panel toggles ─────────────────────────────────────────────────────
        public void ToggleInfoPanel()
        {
            if (infoPanel == null || !contentVisible) return;
            bool next = !infoPanel.activeSelf;
            SetInfoPanelVisible(next);
        }

        void SetInfoButtonVisible(bool visible)
        {
            if (infoToggleButtonRect != null)
            {
                infoToggleButtonRect.gameObject.SetActive(visible);
            }
        }

        public void ToggleHelpPanel()
        {
            if (helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);
        }

        // ── Responsive scanner layout ────────────────────────────────────────
        void ApplyResponsiveScannerLayout(bool force = false)
        {
            Canvas canvas = ResolveOverlayCanvas();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            int width = Mathf.RoundToInt(canvasRect != null && canvasRect.rect.width > 1f
                ? canvasRect.rect.width
                : Screen.width);
            int height = Mathf.RoundToInt(canvasRect != null && canvasRect.rect.height > 1f
                ? canvasRect.rect.height
                : Screen.height);

            if (!force && width == lastLayoutWidth && height == lastLayoutHeight)
                return;

            lastLayoutWidth = width;
            lastLayoutHeight = height;

            bool landscape = width > height;
            if (landscape)
                ApplyLandscapeScannerLayout();
            else
                ApplyPortraitScannerLayout();
        }

        void ApplyPortraitScannerLayout()
        {
            EnsureScannerLayoutReferences();
            Vector2 size = ResolveCanvasSize();
            bool compact = Mathf.Min(size.x, size.y) < 760f;

            SetAnchors(topBarRect, new Vector2(0f, compact ? 0.875f : 0.885f), new Vector2(1f, 1f));
            SetAnchors(statusPanelRect, new Vector2(compact ? 0.04f : 0.08f, compact ? 0.785f : 0.795f), new Vector2(compact ? 0.96f : 0.92f, compact ? 0.858f : 0.865f));
            SetAnchors(scanGuideRect, new Vector2(compact ? 0.055f : 0.1f, 0.405f), new Vector2(compact ? 0.945f : 0.9f, 0.565f));
            SetAnchors(infoPanelRect, new Vector2(compact ? 0.03f : 0.04f, 0.018f), new Vector2(compact ? 0.97f : 0.96f, 0.485f));
            SetAnchors(infoToggleButtonRect, new Vector2(compact ? 0.68f : 0.74f, 0.50f), new Vector2(compact ? 0.96f : 0.96f, 0.58f));
            SetAnchors(helpPanelRect, new Vector2(compact ? 0.055f : 0.08f, 0.2f), new Vector2(compact ? 0.945f : 0.92f, 0.78f));

            SetAnchors(backButtonRect,  new Vector2(0.02f, 0.07f), new Vector2(0.22f, 0.43f));
            SetAnchors(flashButtonRect, new Vector2(0.25f, 0.07f), new Vector2(0.47f, 0.43f));
            SetAnchors(musicButtonRect, new Vector2(0.53f, 0.07f), new Vector2(0.75f, 0.43f));
            SetAnchors(helpButtonRect,  new Vector2(0.78f, 0.07f), new Vector2(0.98f, 0.43f));
            SetTextRect(topTitleText, new Vector2(0.24f, 0.62f), new Vector2(0.76f, 0.98f));
            SetTextRect(contextText, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.60f));

            ConfigureText(topTitleText, compact ? 25 : 28, 20, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureText(contextText, 15, 13, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureText(statusText, 25, 21, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureText(scanGuideText, compact ? 26 : 28, 21, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureInfoPanelText(portrait: true);
            ConfigureText(helpText, 28, 20, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureButtonLabel(backButtonRect, compact ? 18 : 19, 17);
            ConfigureButtonLabel(flashButtonRect, compact ? 17 : 18, 16);
            ConfigureButtonLabel(musicButtonRect, compact ? 17 : 18, 16);
            ConfigureButtonLabel(helpButtonRect, compact ? 17 : 18, 16);
        }

        void ApplyLandscapeScannerLayout()
        {
            EnsureScannerLayoutReferences();

            SetAnchors(topBarRect, new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            SetAnchors(statusPanelRect, new Vector2(0.17f, 0.745f), new Vector2(0.83f, 0.85f));
            SetAnchors(scanGuideRect, new Vector2(0.18f, 0.38f), new Vector2(0.58f, 0.55f));
            SetAnchors(infoPanelRect, new Vector2(0.62f, 0.08f), new Vector2(0.98f, 0.82f));
            SetAnchors(infoToggleButtonRect, new Vector2(0.83f, 0.015f), new Vector2(0.98f, 0.09f));
            SetAnchors(helpPanelRect, new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.8f));
            SetCorner(backButtonRect, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(184f, 68f));
            SetCorner(helpButtonRect, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(184f, 68f));
            SetCorner(musicButtonRect, new Vector2(1f, 0.5f), new Vector2(-222f, 0f), new Vector2(160f, 68f));
            SetCorner(flashButtonRect, new Vector2(1f, 0.5f), new Vector2(-400f, 0f), new Vector2(160f, 68f));
            SetTextRect(topTitleText, new Vector2(0.34f, 0.46f), new Vector2(0.66f, 0.92f));
            SetTextRect(contextText, new Vector2(0.32f, 0.16f), new Vector2(0.68f, 0.42f));

            ConfigureText(topTitleText, 31, 22, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureText(contextText, 18, 14, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureText(statusText, 21, 16, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureText(scanGuideText, 23, 17, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
            ConfigureInfoPanelText(portrait: false);
            ConfigureText(helpText, 25, 18, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
        }

        void ConfigureInfoPanelText(bool portrait)
        {
            float side = portrait ? 28f : 20f;
            SetChildAnchors(categoryText, new Vector2(side, portrait ? -56f : -46f), new Vector2(-side, -12f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            SetChildAnchors(titleText, new Vector2(side, portrait ? -122f : -102f), new Vector2(-side, portrait ? -58f : -48f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            SetChildAnchors(subtitleText, new Vector2(side, portrait ? -182f : -146f), new Vector2(-side, portrait ? -126f : -106f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            SetChildAnchors(descriptionText, new Vector2(side, portrait ? -470f : -395f), new Vector2(-side, portrait ? -194f : -152f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            SetChildAnchors(focusText, new Vector2(side, portrait ? 74f : 62f), new Vector2(-side, portrait ? 122f : 102f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
            SetChildAnchors(objectTypeText, new Vector2(side, portrait ? 22f : 18f), new Vector2(-side, portrait ? 68f : 56f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));

            ConfigureText(categoryText, portrait ? 25 : 22, 21, TextAnchor.UpperLeft, VerticalWrapMode.Truncate);
            ConfigureText(titleText, portrait ? 40 : 35, 30, TextAnchor.UpperLeft, VerticalWrapMode.Truncate);
            ConfigureText(subtitleText, portrait ? 27 : 24, 22, TextAnchor.UpperLeft, VerticalWrapMode.Truncate);
            ConfigureText(descriptionText, portrait ? 27 : 24, 22, TextAnchor.UpperLeft, VerticalWrapMode.Truncate);
            ConfigureText(focusText, portrait ? 23 : 21, 20, TextAnchor.LowerLeft, VerticalWrapMode.Truncate);
            ConfigureText(objectTypeText, portrait ? 23 : 21, 20, TextAnchor.LowerLeft, VerticalWrapMode.Truncate);
        }

        void EnsureScannerLayoutReferences()
        {
            if (topBarRect == null) topBarRect = FindRect("TopBar");
            if (statusPanelRect == null) statusPanelRect = FindRect("StatusPanel");
            if (scanGuideRect == null && scanGuide != null) scanGuideRect = scanGuide.GetComponent<RectTransform>();
            if (scanGuideRect == null) scanGuideRect = FindRect("ScanGuideFrame");
            if (infoPanelRect == null && infoPanel != null) infoPanelRect = infoPanel.GetComponent<RectTransform>();
            if (helpPanelRect == null && helpPanel != null) helpPanelRect = helpPanel.GetComponent<RectTransform>();
            if (infoToggleButtonRect == null) infoToggleButtonRect = FindRect("InfoToggleButton");
            if (backButtonRect == null) backButtonRect = FindRect("BackButton");
            if (helpButtonRect == null) helpButtonRect = FindRect("HelpButton");
            if (musicButtonRect == null) musicButtonRect = FindRect("MusicToggleButton");
            if (flashButtonRect == null) flashButtonRect = FindRect("FlashToggleButton");
            if (topTitleText == null) topTitleText = FindText("TopTitle");
            if (scanGuideText == null) scanGuideText = FindText("ScanGuide");
            if (helpText == null) helpText = FindChildText(helpPanel, "HelpText");
        }

        RectTransform FindRect(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            return found != null ? found.GetComponent<RectTransform>() : null;
        }

        Text FindText(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            return found != null ? found.GetComponent<Text>() : null;
        }

        static Text FindChildText(GameObject root, string childName)
        {
            if (root == null) return null;
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == childName)
                    return child.GetComponent<Text>();
            }
            return null;
        }

        static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetCorner(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        static void SetChildAnchors(Text text, Vector2 offsetMin, Vector2 offsetMax,
                                    Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            if (text == null) return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static void SetTextRect(Text text, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (text == null) return;
            SetAnchors(text.rectTransform, anchorMin, anchorMax);
        }

        static void ConfigureButtonLabel(RectTransform buttonRect, int maxSize, int minSize)
        {
            if (buttonRect == null) return;
            Text label = buttonRect.GetComponentInChildren<Text>(true);
            ConfigureText(label, maxSize, minSize, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate);
        }

        static void ConfigureText(Text text, int maxSize, int minSize, TextAnchor alignment, VerticalWrapMode verticalOverflow)
        {
            BuhenARTextStyle.Configure(text, maxSize, minSize, alignment, verticalOverflow, 1.02f);
        }

        // ── Text builders ─────────────────────────────────────────────────────
        static string BuildObjectText(MaterialContentData content)
        {
            if (content == null || string.IsNullOrWhiteSpace(content.ObjectType)) return string.Empty;
            return "Objek visual: " + content.ObjectType;
        }

        static string BuildFocusText(MaterialContentData content)
        {
            if (content == null) return string.Empty;
            string form  = content.FontTypeFocus;
            string color = content.ColorFocus;
            if (!string.IsNullOrWhiteSpace(form) && !string.IsNullOrWhiteSpace(color))
                return "Ciri utama: " + form + "  •  " + color;
            if (!string.IsNullOrWhiteSpace(form))  return "Ciri utama: " + form;
            if (!string.IsNullOrWhiteSpace(color)) return "Ciri utama: " + color;
            return string.Empty;
        }

        static string LimitText(string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string clean = value.Trim();
            if (clean.Length <= maxChars) return clean;

            int cut = Mathf.Clamp(maxChars - 1, 24, clean.Length - 1);
            for (int i = cut; i > Mathf.Max(0, cut - 32); i--)
            {
                if (char.IsWhiteSpace(clean[i]) || clean[i] == ',' || clean[i] == '.')
                {
                    cut = i;
                    break;
                }
            }

            return clean.Substring(0, cut).TrimEnd(' ', ',', '.') + "...";
        }

        // ── Ensure backdrop / feed ────────────────────────────────────────────
        void EnsurePreviewBackdrop()
        {
            if (previewBackdropImage != null) return;
            Canvas canvas = ResolveOverlayCanvas();
            if (canvas == null) return;
            var go = new GameObject("PreviewBackdrop", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            previewBackdropImage = go.GetComponent<Image>();
            previewBackdropImage.color = new Color(0.03f, 0.07f, 0.12f, 0.8f);
            previewBackdropImage.raycastTarget = false;
            previewBackdropImage.gameObject.SetActive(false);
        }

        void EnsureEditorCameraFeed()
        {
            if (editorCameraFeedImage != null) return;
            Canvas canvas = ResolveOverlayCanvas();
            if (canvas == null) return;
            var go = new GameObject("EditorCameraFeed", typeof(RectTransform), typeof(RawImage));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            editorCameraFeedImage = go.GetComponent<RawImage>();
            editorCameraFeedImage.color = Color.white;
            editorCameraFeedImage.raycastTarget = false;
            editorCameraFeedImage.gameObject.SetActive(false);
        }

        Canvas ResolveOverlayCanvas()
        {
            if (overlayCanvas != null) return overlayCanvas;
            overlayCanvas = GetComponent<Canvas>();
            if (overlayCanvas != null) return overlayCanvas;
            var named = GameObject.Find("Canvas_Overlay");
            if (named != null) overlayCanvas = named.GetComponent<Canvas>();
            if (overlayCanvas != null) return overlayCanvas;
            overlayCanvas = FindAnyObjectByType<Canvas>();
            return overlayCanvas;
        }

        Vector2 ResolveCanvasSize()
        {
            Canvas canvas = ResolveOverlayCanvas();
            RectTransform rect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (rect != null && rect.rect.width > 1f && rect.rect.height > 1f)
                return new Vector2(rect.rect.width, rect.rect.height);
            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }
    }
}
