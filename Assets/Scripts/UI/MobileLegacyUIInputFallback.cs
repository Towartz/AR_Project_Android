using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace ARtiGraf.UI
{
    /// <summary>
    /// Android builds have been more reliable with the legacy UI module in this project.
    /// This keeps scene-authored EventSystems intact, but switches their input backend on device.
    /// </summary>
    public static class MobileLegacyUIInputFallback
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            EnsureBridge();
            Apply();
            SanitizeRaycastTargets();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBridge();
            Apply();
            SanitizeRaycastTargets();
        }

        static void EnsureBridge()
        {
#if UNITY_ANDROID
            if (Object.FindFirstObjectByType<MobileButtonInputBridge>() != null)
                return;

            GameObject bridge = new GameObject("MobileButtonInputBridge");
            Object.DontDestroyOnLoad(bridge);
            bridge.AddComponent<MobileButtonInputBridge>();
#endif
        }

        static void Apply()
        {
#if UNITY_ANDROID && ENABLE_LEGACY_INPUT_MANAGER
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem == null)
                    continue;

                StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacyModule == null)
                    legacyModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();

                // Keep scene-authored InputSystemUIInputModule alive. Standalone is only a backup,
                // while MobileButtonInputBridge below manually invokes buttons if Unity input fails.
                legacyModule.enabled = true;
                legacyModule.forceModuleActive = true;
            }
#endif
        }

        static void SanitizeRaycastTargets()
        {
#if UNITY_ANDROID
            Graphic[] graphics = Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                    continue;

                if (ShouldReceiveRaycast(graphic))
                    continue;

                graphic.raycastTarget = false;
            }
#endif
        }

        static bool ShouldReceiveRaycast(Graphic graphic)
        {
            GameObject go = graphic.gameObject;

            // Direct component checks
            if (go.GetComponent<Selectable>() != null)
                return true;

            if (go.GetComponentInParent<Selectable>() != null)
                return true;

            // Check if any child has a Selectable (e.g. Image background of a Button)
            if (go.GetComponentInChildren<Selectable>(true) != null)
                return true;

            if (go.GetComponent<IPointerClickHandler>() != null ||
                go.GetComponent<IPointerDownHandler>() != null ||
                go.GetComponent<IPointerUpHandler>() != null)
                return true;

            // Walk up the hierarchy
            Transform current = go.transform;
            while (current != null)
            {
                if (current.GetComponent<IPointerClickHandler>() != null ||
                    current.GetComponent<IPointerDownHandler>() != null ||
                    current.GetComponent<IPointerUpHandler>() != null)
                    return true;

                current = current.parent;
            }

            // Walk down the hierarchy for pointer handlers on children
            IPointerClickHandler[] clickHandlers = go.GetComponentsInChildren<IPointerClickHandler>(true);
            if (clickHandlers != null && clickHandlers.Length > 0)
                return true;

            return false;
        }
    }

    sealed class MobileButtonInputBridge : MonoBehaviour
    {
        const float MaxTapDistancePixels = 90f;
        const float MaxTapSeconds = 0.75f;
        const float ClickCooldownSeconds = 0.18f;

        Vector2 touchStartPosition;
        Vector2 inputSystemStartPosition;
        Vector2 mouseStartPosition;
        float touchStartTime;
        float inputSystemStartTime;
        float mouseStartTime;
        float lastClickTime = -10f;
        bool trackingLegacyTouch;
        bool trackingInputSystemTouch;
        bool trackingMouseTouch;
        int trackingFingerId = -1;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>(32);

        void Update()
        {
#if UNITY_ANDROID
            bool handledLegacy = HandleLegacyTouches();
            if (!handledLegacy)
                HandleInputSystemTouch();

            HandleMouseFallback();
#endif
        }

        bool HandleLegacyTouches()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount <= 0)
            {
                trackingLegacyTouch = false;
                trackingFingerId = -1;
                return false;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    trackingLegacyTouch = true;
                    trackingFingerId = touch.fingerId;
                    touchStartPosition = touch.position;
                    touchStartTime = Time.unscaledTime;
                    return true;
                }

                if (!trackingLegacyTouch || touch.fingerId != trackingFingerId)
                    continue;

                if (touch.phase == TouchPhase.Canceled)
                {
                    trackingLegacyTouch = false;
                    trackingFingerId = -1;
                    return true;
                }

                if (touch.phase == TouchPhase.Ended)
                {
                    TryClick(touch.position, touchStartPosition, touchStartTime);
                    trackingLegacyTouch = false;
                    trackingFingerId = -1;
                    return true;
                }
            }

            return true;
#else
            return false;
#endif
        }

        void HandleInputSystemTouch()
        {
#if ENABLE_INPUT_SYSTEM
            Touchscreen screen = Touchscreen.current;
            if (screen == null)
                return;

            if (screen.primaryTouch.press.wasPressedThisFrame)
            {
                trackingInputSystemTouch = true;
                inputSystemStartPosition = screen.primaryTouch.position.ReadValue();
                inputSystemStartTime = Time.unscaledTime;
                return;
            }

            if (screen.primaryTouch.press.wasReleasedThisFrame && trackingInputSystemTouch)
            {
                Vector2 endPosition = screen.primaryTouch.position.ReadValue();
                TryClick(endPosition, inputSystemStartPosition, inputSystemStartTime);
                trackingInputSystemTouch = false;
            }
#endif
        }

        void HandleMouseFallback()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                trackingMouseTouch = true;
                mouseStartPosition = Input.mousePosition;
                mouseStartTime = Time.unscaledTime;
                return;
            }

            if (trackingMouseTouch && Input.GetMouseButtonUp(0))
            {
                TryClick(Input.mousePosition, mouseStartPosition, mouseStartTime);
                trackingMouseTouch = false;
            }
#endif
        }

        void TryClick(Vector2 endPosition, Vector2 startPosition, float startTime)
        {
            if (Time.unscaledTime - lastClickTime < ClickCooldownSeconds)
                return;

            if ((endPosition - startPosition).sqrMagnitude > MaxTapDistancePixels * MaxTapDistancePixels)
                return;

            if (Time.unscaledTime - startTime > MaxTapSeconds)
                return;

            if (TryInvokeRaycastTarget(endPosition))
                return;

            Button button = FindTopButton(endPosition);
            if (button == null)
                return;

            lastClickTime = Time.unscaledTime;
            Debug.Log("BuhenAR manual UI click: " + button.name);
            button.onClick.Invoke();
        }

        bool TryInvokeRaycastTarget(Vector2 screenPosition)
        {
            raycastResults.Clear();

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (systems.Length > 0)
                    eventSystem = systems[0];
            }

            if (eventSystem == null)
                return false;

            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left,
                clickCount = 1,
                clickTime = Time.unscaledTime
            };

            GraphicRaycaster[] raycasters = Object.FindObjectsByType<GraphicRaycaster>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < raycasters.Length; i++)
            {
                GraphicRaycaster raycaster = raycasters[i];
                if (raycaster == null || !raycaster.isActiveAndEnabled)
                    continue;

                raycaster.Raycast(pointerData, raycastResults);
            }

            if (raycastResults.Count == 0)
                return false;

            raycastResults.Sort(CompareRaycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject target = raycastResults[i].gameObject;
                if (target == null || !target.activeInHierarchy)
                    continue;

                Button button = target.GetComponentInParent<Button>();
                if (IsClickable(button))
                {
                    lastClickTime = Time.unscaledTime;
                    Debug.Log("BuhenAR manual raycast button click: " + button.name);
                    button.onClick.Invoke();
                    return true;
                }

                GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
                if (clickHandler == null || clickHandler.GetComponentInParent<Button>() != null)
                    continue;

                lastClickTime = Time.unscaledTime;
                pointerData.pointerPressRaycast = raycastResults[i];
                pointerData.pointerCurrentRaycast = raycastResults[i];
                pointerData.pointerPress = clickHandler;
                pointerData.rawPointerPress = target;

                Debug.Log("BuhenAR manual raycast UI click: " + clickHandler.name);
                ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerClickHandler);
                return true;
            }

            return false;
        }

        static int CompareRaycastResults(RaycastResult a, RaycastResult b)
        {
            int cmp = b.sortingLayer.CompareTo(a.sortingLayer);
            if (cmp != 0) return cmp;

            cmp = b.sortingOrder.CompareTo(a.sortingOrder);
            if (cmp != 0) return cmp;

            cmp = b.depth.CompareTo(a.depth);
            if (cmp != 0) return cmp;

            return a.distance.CompareTo(b.distance);
        }

        static Button FindTopButton(Vector2 screenPosition)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Button bestButton = null;
            long bestScore = long.MinValue;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (!IsClickable(button))
                    continue;

                RectTransform rect = button.transform as RectTransform;
                if (rect == null)
                    continue;

                Canvas canvas = button.GetComponentInParent<Canvas>();
                Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;

                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
                    continue;

                long score = GetRenderScore(button.transform, canvas);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestButton = button;
                }
            }

            return bestButton;
        }

        static bool IsClickable(Button button)
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable || !button.IsInteractable())
                return false;

            Transform current = button.transform;
            while (current != null)
            {
                CanvasGroup group = current.GetComponent<CanvasGroup>();
                if (group != null && (!group.interactable || group.alpha <= 0.01f))
                    return false;

                current = current.parent;
            }

            return true;
        }

        static long GetRenderScore(Transform target, Canvas canvas)
        {
            int sortingLayer = canvas != null ? SortingLayer.GetLayerValueFromID(canvas.sortingLayerID) : 0;
            int sortingOrder = canvas != null ? canvas.sortingOrder : 0;

            long score = ((long)sortingLayer + 32768L) * 1000000000000L;
            score += ((long)sortingOrder + 32768L) * 10000000L;

            Transform current = target;
            long multiplier = 1L;
            while (current != null && multiplier < 10000000L)
            {
                score += current.GetSiblingIndex() * multiplier;
                multiplier *= 100L;
                current = current.parent;
            }

            return score;
        }
    }
}
