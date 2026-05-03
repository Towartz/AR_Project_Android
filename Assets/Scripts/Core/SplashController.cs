using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using InputSystemKeyboard = UnityEngine.InputSystem.Keyboard;
using InputSystemMouse = UnityEngine.InputSystem.Mouse;
using InputSystemTouchscreen = UnityEngine.InputSystem.Touchscreen;
#endif

namespace ARtiGraf.Core
{
    /// <summary>
    /// Custom splash screen dengan artwork responsive portrait/landscape dan skip touch.
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] float  delaySeconds   = 2.5f;
        [SerializeField] string nextSceneName  = "MainMenuScene";
        [SerializeField] bool   allowSkipTouch = true;

        [Header("UI")]
        [SerializeField] CanvasGroup logoGroup;
        [SerializeField] Text        versionLabel;
        [SerializeField] float       fadeInDuration  = 0.6f;
        [SerializeField] float       fadeOutDuration = 0.4f;

        [Header("Responsive Artwork")]
        [SerializeField] RectTransform artworkRoot;
        [SerializeField] Image         portraitArtwork;
        [SerializeField] Image         landscapeArtwork;
        [SerializeField] float         artworkPulseScale = 0.008f;

        [Header("Custom Animation")]
        [SerializeField] RectTransform titleRoot;
        [SerializeField] RectTransform taglineRoot;
        [SerializeField] RectTransform loadingHintRoot;
        [SerializeField] Image         topGlowImage;
        [SerializeField] Text          titleLabel;
        [SerializeField] Text          taglineLabel;
        [SerializeField] Text          loadingHintLabel;
        [SerializeField] float         titlePopScale = 1.08f;
        [SerializeField] float         pulseScale = 0.035f;
        [SerializeField] float         loadingDotInterval = 0.28f;

        bool skipped;
        bool splashVisible;
        float dotTimer;
        int dotCount;
        CanvasGroup rootGroup;
        CanvasGroup titleGroup;
        CanvasGroup taglineGroup;
        CanvasGroup loadingGroup;
        CanvasGroup glowGroup;
        CanvasGroup artworkGroup;
        Vector3 titleBaseScale = Vector3.one;
        Vector3 glowBaseScale = Vector3.one;
        Vector3 artworkBaseScale = Vector3.one;
        Vector2 taglineBasePosition;
        Vector2 loadingBasePosition;
        Color glowBaseColor = Color.white;
        int lastScreenWidth = -1;
        int lastScreenHeight = -1;

        public void ConfigureArtwork(RectTransform root, Image portrait, Image landscape)
        {
            artworkRoot = root;
            portraitArtwork = portrait;
            landscapeArtwork = landscape;
        }

        void Start()
        {
            CacheUi();

            if (titleLabel != null)
                titleLabel.text = "BuhenAR";

            if (taglineLabel != null)
                taglineLabel.text = "Belajar buah dan hewan dengan AR";

            if (loadingHintLabel != null)
                loadingHintLabel.text = "Memuat";

            if (versionLabel != null)
                versionLabel.text = "v" + Application.version;

            SelectResponsiveArtwork(force: true);
            PrepareIntroState();
            StartCoroutine(SplashRoutine());
        }

        void Update()
        {
            AnimateIdle();
            AnimateLoadingDots();
            SelectResponsiveArtwork();

            if (!allowSkipTouch || skipped) return;
            if (IsSkipRequested())
            {
                skipped = true;
                StopAllCoroutines();
                StartCoroutine(FadeOutAndLoad());
            }
        }

        static bool IsSkipRequested()
        {
#if ENABLE_INPUT_SYSTEM
            if (InputSystemKeyboard.current != null &&
                InputSystemKeyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (InputSystemMouse.current != null &&
                (InputSystemMouse.current.leftButton.wasPressedThisFrame ||
                 InputSystemMouse.current.rightButton.wasPressedThisFrame ||
                 InputSystemMouse.current.middleButton.wasPressedThisFrame))
            {
                return true;
            }

            return InputSystemTouchscreen.current != null &&
                   InputSystemTouchscreen.current.primaryTouch.press.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.anyKeyDown ||
                   (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began);
#else
            return false;
#endif
        }

        IEnumerator SplashRoutine()
        {
            yield return AnimateIntro();

            float held = 0f;
            while (held < delaySeconds && !skipped)
            {
                held += Time.deltaTime;
                yield return null;
            }

            yield return FadeOutAndLoad();
        }

        IEnumerator FadeOutAndLoad()
        {
            splashVisible = false;
            CanvasGroup fadeTarget = rootGroup != null ? rootGroup : logoGroup;
            yield return FadeGroup(fadeTarget, fadeTarget != null ? fadeTarget.alpha : 1f, 0f,
                Mathf.Clamp(fadeOutDuration, 0.01f, 0.18f));

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = true;
                while (!loadOperation.isDone)
                    yield return null;
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        void CacheUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                rootGroup = EnsureGroup(canvas.gameObject);
            }

            if (titleRoot == null)
                titleRoot = FindRectTransform("Title");
            if (taglineRoot == null)
                taglineRoot = FindRectTransform("Tagline");
            if (loadingHintRoot == null)
                loadingHintRoot = FindRectTransform("LoadingHint");
            if (topGlowImage == null)
                topGlowImage = FindNamedComponent<Image>("TopGlow");
            if (portraitArtwork == null)
                portraitArtwork = FindNamedComponent<Image>("SplashPortrait");
            if (landscapeArtwork == null)
                landscapeArtwork = FindNamedComponent<Image>("SplashLandscape");
            if (artworkRoot == null)
            {
                RectTransform foundArtworkRoot = FindRectTransform("SplashArtworkRoot");
                if (foundArtworkRoot != null)
                    artworkRoot = foundArtworkRoot;
                else if (portraitArtwork != null && portraitArtwork.transform.parent != null)
                    artworkRoot = portraitArtwork.transform.parent.GetComponent<RectTransform>();
            }
            if (titleLabel == null && titleRoot != null)
                titleLabel = titleRoot.GetComponent<Text>();
            if (taglineLabel == null && taglineRoot != null)
                taglineLabel = taglineRoot.GetComponent<Text>();
            if (loadingHintLabel == null && loadingHintRoot != null)
                loadingHintLabel = loadingHintRoot.GetComponent<Text>();

            titleGroup = titleRoot != null ? EnsureGroup(titleRoot.gameObject) : logoGroup;
            taglineGroup = taglineRoot != null ? EnsureGroup(taglineRoot.gameObject) : null;
            loadingGroup = loadingHintRoot != null ? EnsureGroup(loadingHintRoot.gameObject) : null;
            glowGroup = topGlowImage != null ? EnsureGroup(topGlowImage.gameObject) : null;
            artworkGroup = artworkRoot != null ? EnsureGroup(artworkRoot.gameObject) : null;

            if (titleRoot != null)
                titleBaseScale = titleRoot.localScale;
            if (taglineRoot != null)
                taglineBasePosition = taglineRoot.anchoredPosition;
            if (loadingHintRoot != null)
                loadingBasePosition = loadingHintRoot.anchoredPosition;
            if (topGlowImage != null)
            {
                glowBaseScale = topGlowImage.rectTransform.localScale;
                glowBaseColor = topGlowImage.color;
            }
            if (artworkRoot != null)
                artworkBaseScale = artworkRoot.localScale;
        }

        void PrepareIntroState()
        {
            if (rootGroup != null)
                rootGroup.alpha = 1f;
            if (logoGroup != null)
                logoGroup.alpha = 0f;
            if (titleGroup != null)
                titleGroup.alpha = 0f;
            if (taglineGroup != null)
                taglineGroup.alpha = 0f;
            if (loadingGroup != null)
                loadingGroup.alpha = 0f;
            if (glowGroup != null)
                glowGroup.alpha = 0f;
            if (artworkGroup != null)
                artworkGroup.alpha = 0f;

            if (titleRoot != null)
                titleRoot.localScale = titleBaseScale * 0.84f;
            if (taglineRoot != null)
                taglineRoot.anchoredPosition = taglineBasePosition + new Vector2(0f, -24f);
            if (loadingHintRoot != null)
                loadingHintRoot.anchoredPosition = loadingBasePosition + new Vector2(0f, -14f);
        }

        IEnumerator AnimateIntro()
        {
            splashVisible = true;
            float duration = Mathf.Max(0.05f, fadeInDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Smooth(t);

                if (logoGroup != null)
                    logoGroup.alpha = ease;
                if (titleGroup != null)
                    titleGroup.alpha = ease;
                if (glowGroup != null)
                    glowGroup.alpha = Smooth(Mathf.Clamp01(t * 1.25f));
                if (artworkGroup != null)
                    artworkGroup.alpha = ease;
                if (taglineGroup != null)
                    taglineGroup.alpha = Smooth(Mathf.Clamp01((t - 0.24f) / 0.58f));
                if (loadingGroup != null)
                    loadingGroup.alpha = Smooth(Mathf.Clamp01((t - 0.52f) / 0.36f));

                if (titleRoot != null)
                {
                    float scale = Mathf.LerpUnclamped(0.84f, titlePopScale, EaseOutBack(t));
                    scale = Mathf.Lerp(scale, 1f, Smooth(Mathf.Clamp01((t - 0.72f) / 0.28f)));
                    titleRoot.localScale = titleBaseScale * scale;
                }

                if (taglineRoot != null)
                {
                    float tagT = Smooth(Mathf.Clamp01((t - 0.22f) / 0.58f));
                    taglineRoot.anchoredPosition = Vector2.Lerp(taglineBasePosition + new Vector2(0f, -24f), taglineBasePosition, tagT);
                }

                if (loadingHintRoot != null)
                {
                    float loadT = Smooth(Mathf.Clamp01((t - 0.5f) / 0.4f));
                    loadingHintRoot.anchoredPosition = Vector2.Lerp(loadingBasePosition + new Vector2(0f, -14f), loadingBasePosition, loadT);
                }

                yield return null;
            }

            if (titleRoot != null)
                titleRoot.localScale = titleBaseScale;
            if (taglineRoot != null)
                taglineRoot.anchoredPosition = taglineBasePosition;
            if (loadingHintRoot != null)
                loadingHintRoot.anchoredPosition = loadingBasePosition;
        }

        void AnimateIdle()
        {
            if (!splashVisible)
                return;

            float time = Time.unscaledTime;
            float pulse = (Mathf.Sin(time * 3.2f) + 1f) * 0.5f;

            if (topGlowImage != null)
            {
                float scale = 1f + pulse * pulseScale;
                topGlowImage.rectTransform.localScale = glowBaseScale * scale;
                Color color = glowBaseColor;
                color.a = Mathf.Lerp(0.06f, 0.14f, pulse);
                topGlowImage.color = color;
            }

            if (artworkRoot != null)
            {
                float scale = 1f + pulse * artworkPulseScale;
                artworkRoot.localScale = artworkBaseScale * scale;
            }

            if (titleRoot != null)
            {
                float bob = Mathf.Sin(time * 2.1f) * 3f;
                titleRoot.anchoredPosition = new Vector2(titleRoot.anchoredPosition.x, bob);
            }
        }

        void SelectResponsiveArtwork(bool force = false)
        {
            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);

            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
                return;

            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;

            bool useLandscape = screenWidth > screenHeight;
            if (portraitArtwork != null)
                portraitArtwork.gameObject.SetActive(!useLandscape || landscapeArtwork == null);
            if (landscapeArtwork != null)
                landscapeArtwork.gameObject.SetActive(useLandscape || portraitArtwork == null);
        }

        void AnimateLoadingDots()
        {
            if (loadingHintLabel == null || !splashVisible)
                return;

            dotTimer += Time.deltaTime;
            if (dotTimer < loadingDotInterval)
                return;

            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            loadingHintLabel.text = "Memuat" + new string('.', dotCount);
        }

        IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        static CanvasGroup EnsureGroup(GameObject target)
        {
            if (target == null)
                return null;

            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        static RectTransform FindRectTransform(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            return found != null ? found.GetComponent<RectTransform>() : null;
        }

        static T FindNamedComponent<T>(string objectName) where T : Component
        {
            GameObject found = GameObject.Find(objectName);
            return found != null ? found.GetComponent<T>() : null;
        }

        static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - (2f * value));
        }

        static float EaseOutBack(float value)
        {
            value = Mathf.Clamp01(value) - 1f;
            const float overshoot = 1.70158f;
            return 1f + (overshoot + 1f) * value * value * value + overshoot * value * value;
        }
    }
}
