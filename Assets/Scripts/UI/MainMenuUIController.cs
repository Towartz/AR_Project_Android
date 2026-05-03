using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ARtiGraf.UI
{
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private RectTransform titleContainer;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private RectTransform footerContainer;

        [Header("Title Settings")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private float titleAnimationDuration = 0.8f;

        [Header("Button Settings")]
        [SerializeField] private Button mulaiBelajarButton;
        [SerializeField] private Button petunjukButton;
        [SerializeField] private Button quizButton;
        [SerializeField] private Button tentangButton;
        [SerializeField] private Button keluarButton;

        [Header("Visual Settings")]
        [SerializeField] private Color primaryColor = new Color(0.098f, 0.463f, 0.824f);
        [SerializeField] private Color secondaryColor = new Color(0.051f, 0.106f, 0.243f);
        [SerializeField] private Color accentColor = new Color(0.956f, 0.968f, 0.988f);
        [SerializeField] private float buttonSpacing = 20f;
        [SerializeField] private float buttonHeight = 80f;

        [Header("Animation Settings")]
        [SerializeField] private float buttonHoverScale = 1.05f;
        [SerializeField] private float buttonPressScale = 0.95f;
        [SerializeField] private float transitionDuration = 0.3f;

        private Vector3[] _originalButtonScales;
        private Color _originalTitleColor;
        private bool _isLoadingScene;

        private void Start()
        {
            InitializeButtons();
            StartTitleAnimation();
        }

        private void InitializeButtons()
        {
            _originalButtonScales = new Vector3[5];

            Button[] buttons = { mulaiBelajarButton, petunjukButton, quizButton, tentangButton, keluarButton };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    ApplyButtonSize(buttons[i]);
                    _originalButtonScales[i] = buttons[i].transform.localScale;
                    AddButtonListeners(buttons[i], i);
                }
            }

            if (titleText != null)
            {
                _originalTitleColor = titleText.color;
            }
        }

        private void AddButtonListeners(Button button, int index)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            }

            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener((data) => OnButtonHover(button));
            trigger.triggers.Add(hoverEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => OnButtonExit(button, index));
            trigger.triggers.Add(exitEntry);

            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((data) => OnButtonPress(button));
            trigger.triggers.Add(downEntry);

            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((data) => OnButtonRelease(button, index));
            trigger.triggers.Add(upEntry);
        }

        private void OnButtonHover(Button button)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleButton(button.transform, buttonHoverScale));
            PlayHoverSound();
        }

        private void OnButtonExit(Button button, int index)
        {
            if (index < _originalButtonScales.Length)
            {
                StartCoroutine(ScaleButton(button.transform, _originalButtonScales[index].x));
            }
        }

        private void OnButtonPress(Button button)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleButton(button.transform, buttonPressScale));
        }

        private void OnButtonRelease(Button button, int index)
        {
            if (index < _originalButtonScales.Length)
            {
                StartCoroutine(ScaleButton(button.transform, _originalButtonScales[index].x));
            }
        }

        private IEnumerator ScaleButton(Transform buttonTransform, float targetScale)
        {
            Vector3 startScale = buttonTransform.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                float smoothT = SmoothTween(t);
                buttonTransform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
                yield return null;
            }

            buttonTransform.localScale = endScale;
        }

        private float SmoothTween(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private void StartTitleAnimation()
        {
            if (titleContainer != null)
            {
                StartCoroutine(AnimateTitleIn());
            }
        }

        private IEnumerator AnimateTitleIn()
        {
            if (titleContainer == null) yield break;

            Vector3 startPos = titleContainer.localPosition;
            Vector3 targetPos = startPos;
            float distance = 50f;

            titleContainer.localPosition = startPos + Vector3.up * distance;
            titleContainer.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < titleAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / titleAnimationDuration;
                float smoothT = SmoothTween(t);

                titleContainer.localPosition = Vector3.Lerp(startPos + Vector3.up * distance, startPos, smoothT);
                titleContainer.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, smoothT);

                if (titleText != null)
                {
                    Color c = _originalTitleColor;
                    c.a = smoothT;
                    titleText.color = c;
                }

                yield return null;
            }

            titleContainer.localPosition = targetPos;
            titleContainer.localScale = Vector3.one;
            if (titleText != null) titleText.color = _originalTitleColor;
        }

        private void PlayHoverSound()
        {
            // Audio feedback can be added here
        }

        #region Button Actions

        public void OnMulaiBelajarClicked()
        {
            StartCoroutine(LoadSceneWithTransition("MaterialSelectScene"));
        }

        public void OnPetunjukClicked()
        {
            StartCoroutine(LoadSceneWithTransition("GuideScene"));
        }

        public void OnQuizClicked()
        {
            StartCoroutine(LoadSceneWithTransition("QuizHuntScene"));
        }

        public void OnTentangClicked()
        {
            StartCoroutine(LoadSceneWithTransition("AboutScene"));
        }

        public void OnKeluarClicked()
        {
            StartCoroutine(QuitWithAnimation());
        }

        private IEnumerator LoadSceneWithTransition(string sceneName)
        {
            if (_isLoadingScene || string.IsNullOrEmpty(sceneName))
                yield break;

            _isLoadingScene = true;
            SetMenuButtonsInteractable(false);
            yield return FadeOut();

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = true;
                while (!loadOperation.isDone)
                    yield return null;
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private IEnumerator FadeOut()
        {
            // Create fade overlay
            Canvas fadeCanvas = CreateFadeCanvas();
            Image fadeImage = fadeCanvas.GetComponentInChildren<Image>();

            float elapsed = 0f;
            Color startColor = new Color(0, 0, 0, 0);
            Color endColor = new Color(0, 0, 0, 1);

            float duration = Mathf.Clamp(transitionDuration, 0.01f, 0.12f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                fadeImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            Destroy(fadeCanvas.gameObject);
        }

        private Canvas CreateFadeCanvas()
        {
            GameObject fadeObj = new GameObject("FadeCanvas");
            Canvas canvas = fadeObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = fadeObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            fadeObj.AddComponent<GraphicRaycaster>();

            GameObject fadeImageObj = new GameObject("FadeImage");
            fadeImageObj.transform.SetParent(fadeObj.transform);
            Image img = fadeImageObj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = true;

            RectTransform rect = fadeImageObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return canvas;
        }

        private void SetMenuButtonsInteractable(bool interactable)
        {
            Button[] buttons = { mulaiBelajarButton, petunjukButton, quizButton, tentangButton, keluarButton };
            foreach (Button button in buttons)
            {
                if (button != null)
                    button.interactable = interactable;
            }
        }

        private IEnumerator QuitWithAnimation()
        {
            yield return FadeOut();
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        #endregion

        #region Layout Helpers

        public void RefreshButtonLayout()
        {
            if (buttonContainer == null) return;

            float totalHeight = 0f;
            Button[] buttons = { mulaiBelajarButton, petunjukButton, quizButton, tentangButton, keluarButton };

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    ApplyButtonSize(button);
                    RectTransform rect = button.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        totalHeight += rect.sizeDelta.y + buttonSpacing;
                    }
                }
            }

            Vector2 containerSize = buttonContainer.sizeDelta;
            containerSize.y = totalHeight;
            buttonContainer.sizeDelta = containerSize;
        }

        private void ApplyButtonSize(Button button)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            Vector2 size = rect.sizeDelta;
            size.y = buttonHeight;
            rect.sizeDelta = size;
        }

        #endregion
    }
}
