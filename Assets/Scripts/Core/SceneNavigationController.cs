using System.Collections;
using System.Collections.Generic;
using ARtiGraf.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using InputSystemKeyboard = UnityEngine.InputSystem.Keyboard;
#endif

namespace ARtiGraf.Core
{
    /// <summary>
    /// Navigasi scene terpusat dengan fade transition dan history stack untuk tombol Back.
    /// </summary>
    public class SceneNavigationController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] string mainMenuSceneName      = "MainMenuScene";
        [SerializeField] string guideSceneName         = "GuideScene";
        [SerializeField] string materialSelectSceneName = "MaterialSelectScene";
        [SerializeField] string arScanSceneName        = "ARScanScene";
        [SerializeField] string quizSceneName          = "QuizScene";
        [SerializeField] string aboutSceneName         = "AboutScene";
        [SerializeField] string resultSceneName        = "ResultScene";
        [SerializeField] string collectionSceneName    = "CollectionScene";
        [SerializeField] string quizHuntSceneName      = "QuizHuntScene";

        [Header("Transition")]
        [SerializeField] float fadeDuration   = 0.14f;
        [SerializeField] float fadeInDuration = 0.10f;
        [SerializeField] Color transitionColor = new Color32(9, 19, 42, 255);
        [SerializeField] bool  enableFade     = true;

        // ── Scene history ─────────────────────────────────────────────────────
        static readonly Stack<string> SceneHistory = new Stack<string>();
        static bool TransitionInProgress;
        static float TransitionStartedAt;
        static readonly Color SafeTransitionColor = new Color32(9, 19, 42, 255);

        // ── Android back button ───────────────────────────────────────────────
        void Update()
        {
            if (IsBackButtonPressed()) HandleBackButton();
        }

        static bool IsBackButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return InputSystemKeyboard.current != null &&
                   InputSystemKeyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        void HandleBackButton()
        {
            if (SceneHistory.Count > 0)
                GoBack();
            else
                QuitApplication();
        }

        public void GoBack()
        {
            if (SceneHistory.Count == 0) return;
            string prev = SceneHistory.Pop();
            BeginSceneTransition(prev, pushCurrent: false);
        }

        // ── Public navigation methods ─────────────────────────────────────────
        public void LoadScene(string sceneName) => BeginSceneTransition(sceneName, pushCurrent: true);

        public void OpenMainMenu()       => BeginSceneTransition(mainMenuSceneName, pushCurrent: false); // root — clear history
        public void OpenGuide()          => LoadScene(guideSceneName);
        public void OpenMaterialSelect() => LoadScene(materialSelectSceneName);
        public void OpenQuiz()           => LoadScene(quizHuntSceneName);
        public void OpenAbout()          => LoadScene(aboutSceneName);
        public void OpenResult()         => LoadScene(resultSceneName);
        public void RestartQuiz()        => LoadScene(quizHuntSceneName);
        public void OpenCollection()     => LoadScene(collectionSceneName);
        public void OpenQuizHunt()       => LoadScene(quizHuntSceneName);
        public void OpenQuizHuntFruits()
        {
            AppSession.SelectCategory(LearningCategory.Typography);
            LoadScene(quizHuntSceneName);
        }
        public void OpenQuizHuntAnimals()
        {
            AppSession.SelectCategory(LearningCategory.Color);
            LoadScene(quizHuntSceneName);
        }

        public void OpenARForTypography()
        {
            AppSession.SelectCategory(LearningCategory.Typography);
            LoadScene(arScanSceneName);
        }

        public void OpenARForColor()
        {
            AppSession.SelectCategory(LearningCategory.Color);
            LoadScene(arScanSceneName);
        }

        public void OpenARForAllContent()
        {
            AppSession.ClearCategory();
            LoadScene(arScanSceneName);
        }

        public void OpenSelectedCategoryAR()
        {
            if (!AppSession.SelectedCategory.HasValue) { OpenMaterialSelect(); return; }
            if (AppSession.SelectedCategory.Value == LearningCategory.Typography)
                OpenARForTypography();
            else
                OpenARForColor();
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void BeginSceneTransition(string sceneName, bool pushCurrent)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            if (TransitionInProgress)
            {
                bool staleTransition = Time.realtimeSinceStartup - TransitionStartedAt > 18f;
                if (!staleTransition)
                    return;

                Debug.LogWarning("Scene transition was stale. Resetting transition lock before loading " + sceneName + ".");
                TransitionInProgress = false;
            }

            var hostObject = new GameObject("SceneTransitionHost");
            DontDestroyOnLoad(hostObject);

            SceneTransitionHost host = hostObject.AddComponent<SceneTransitionHost>();
            host.Begin(sceneName, pushCurrent, mainMenuSceneName, enableFade,
                       Mathf.Clamp(fadeDuration, 0.01f, 0.14f),
                       Mathf.Clamp(fadeInDuration, 0.01f, 0.10f),
                       ResolveTransitionColor(transitionColor));
        }

        static Color ResolveTransitionColor(Color color)
        {
            // Scene lama masih menyimpan warna cream/putih; pakai navy supaya kalau load lambat
            // tidak terlihat seperti aplikasi stuck di layar putih.
            if (color.r > 0.86f && color.g > 0.82f && color.b > 0.72f)
                return SafeTransitionColor;
            return color;
        }

        class SceneTransitionHost : MonoBehaviour
        {
            public void Begin(string sceneName, bool pushCurrent, string mainMenuSceneName,
                              bool enableFade, float fadeOutDuration, float fadeInDuration, Color fadeColor)
            {
                StartCoroutine(Run(sceneName, pushCurrent, mainMenuSceneName, enableFade,
                                   fadeOutDuration, fadeInDuration, fadeColor));
            }

            IEnumerator Run(string sceneName, bool pushCurrent, string mainMenuSceneName,
                            bool enableFade, float fadeOutDuration, float fadeInDuration, Color fadeColor)
            {
                TransitionInProgress = true;
                TransitionStartedAt = Time.realtimeSinceStartup;

                Canvas fadeCanvas = null;
                Image fadeImage = null;
                try
                {
                    if (enableFade)
                    {
                        fadeCanvas = CreateFadeCanvas(out fadeImage, fadeColor);
                        yield return FadeImage(fadeImage, 0f, 1f, fadeOutDuration);
                    }

                    if (pushCurrent)
                        SceneHistory.Push(SceneManager.GetActiveScene().name);
                    else if (sceneName == mainMenuSceneName)
                        SceneHistory.Clear();

                    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    if (loadOperation != null)
                    {
                        loadOperation.allowSceneActivation = true;
                        float loadStart = Time.realtimeSinceStartup;
                        while (!loadOperation.isDone)
                        {
                            if (Time.realtimeSinceStartup - loadStart > 12f)
                                break;
                            yield return null;
                        }
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneName);
                        yield return null;
                    }

                    if (enableFade && fadeImage != null)
                        yield return FadeImage(fadeImage, 1f, 0f, fadeInDuration);
                }
                finally
                {
                    if (fadeCanvas != null)
                        Destroy(fadeCanvas.gameObject);

                    TransitionInProgress = false;
                    Destroy(gameObject);
                }
            }

            static IEnumerator FadeImage(Image image, float from, float to, float duration)
            {
                if (image == null)
                    yield break;

                float elapsed = 0f;
                Color color = image.color;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    t = t * t * (3f - (2f * t));
                    color.a = Mathf.Lerp(from, to, t);
                    image.color = color;
                    yield return null;
                }

                color.a = to;
                image.color = color;
            }

            static Canvas CreateFadeCanvas(out Image fadeImage, Color fadeColor)
            {
                var canvasObject = new GameObject("FadeCanvas");
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObject.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObject);

                var imageObject = new GameObject("FadeImage");
                imageObject.transform.SetParent(canvasObject.transform, false);
                fadeImage = imageObject.AddComponent<Image>();
                fadeColor.a = 0f;
                fadeImage.color = fadeColor;
                fadeImage.raycastTarget = true;

                var rect = imageObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                return canvas;
            }
        }
    }
}
