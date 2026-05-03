using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
using InputSystemTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputSystemMouse = UnityEngine.InputSystem.Mouse;
#endif

namespace ARtiGraf.AR
{
    public class ARTouchInteractionController : MonoBehaviour
    {
        public struct TouchSample
        {
            public int FingerId;
            public Vector2 Position;
            public Vector2 DeltaPosition;
            public TouchPhase Phase;
        }

        [Header("Settings")]
        public bool enableTouchInteraction = true;
        public bool enableDesktopPreviewControls = true;
        public float touchGestureDeadzonePixels = 1.6f;
        public float tapToRefocusMaxDuration = 0.25f;
        public float tapToRefocusMaxMovementPixels = 12f;

        [Header("Desktop Settings")]
        public float desktopScrollScaleDelta = 20f;

        public event Action<Vector2> OnPanDelta;
        public event Action<float> OnScaleDelta;
        public event Action<Vector2> OnRotateDelta;
        public event Action<Vector2> OnTap;

        readonly List<TouchSample> touchBuffer = new List<TouchSample>(4);
        readonly List<TouchSample> interactionTouchBuffer = new List<TouchSample>(4);
        readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(8);

        float previousPinchDistance = -1f;
        Vector2 previousTwoFingerMidpoint = new Vector2(float.NaN, float.NaN);
        Vector2 singleTouchStartPosition;
        float singleTouchStartTime = -1f;
        bool singleTouchMoved;
        bool enhancedTouchEnabled;
        Vector3 previousMousePosition;

        void Awake()
        {
            EnableTouchBackendIfNeeded();
        }

        void OnDestroy()
        {
            DisableTouchBackendIfNeeded();
        }

        void Update()
        {
            UpdateTouchInteraction();
            UpdateDesktopInteraction();
        }

        void UpdateTouchInteraction()
        {
            RefreshTouchBuffer();
            if (!enableTouchInteraction || !Application.isMobilePlatform || touchBuffer.Count == 0)
            {
                ResetTouchInteractionState();
                return;
            }

            FilterInteractionTouches();
            if (interactionTouchBuffer.Count == 0)
            {
                previousPinchDistance = -1f;
                previousTwoFingerMidpoint = new Vector2(float.NaN, float.NaN);
                return;
            }

            if (interactionTouchBuffer.Count >= 2)
            {
                singleTouchStartTime = -1f;
                singleTouchMoved = false;
                TouchSample firstTouch = interactionTouchBuffer[0];
                TouchSample secondTouch = interactionTouchBuffer[1];
                float currentDistance = Vector2.Distance(firstTouch.Position, secondTouch.Position);
                Vector2 currentMidpoint = (firstTouch.Position + secondTouch.Position) * 0.5f;

                if (IsValidGesturePoint(previousTwoFingerMidpoint))
                {
                    Vector2 midpointDelta = currentMidpoint - previousTwoFingerMidpoint;
                    if (midpointDelta.sqrMagnitude > touchGestureDeadzonePixels * touchGestureDeadzonePixels)
                    {
                        OnPanDelta?.Invoke(midpointDelta);
                    }
                }

                if (previousPinchDistance > 0f)
                {
                    float pinchDelta = currentDistance - previousPinchDistance;
                    if (Mathf.Abs(pinchDelta) > 0.5f)
                    {
                        OnScaleDelta?.Invoke(pinchDelta);
                    }
                }

                previousPinchDistance = currentDistance;
                previousTwoFingerMidpoint = currentMidpoint;
                return;
            }

            previousPinchDistance = -1f;
            previousTwoFingerMidpoint = new Vector2(float.NaN, float.NaN);
            TouchSample touch = interactionTouchBuffer[0];
            switch (touch.Phase)
            {
                case TouchPhase.Began:
                    singleTouchStartPosition = touch.Position;
                    singleTouchStartTime = Time.unscaledTime;
                    singleTouchMoved = false;
                    return;
                case TouchPhase.Moved:
                    if ((touch.Position - singleTouchStartPosition).sqrMagnitude >
                        tapToRefocusMaxMovementPixels * tapToRefocusMaxMovementPixels)
                    {
                        singleTouchMoved = true;
                    }

                    if (touch.DeltaPosition.sqrMagnitude < touchGestureDeadzonePixels * touchGestureDeadzonePixels)
                    {
                        return;
                    }

                    OnRotateDelta?.Invoke(touch.DeltaPosition);
                    return;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (IsTapGesture(touch))
                    {
                        OnTap?.Invoke(touch.Position);
                    }

                    singleTouchStartTime = -1f;
                    singleTouchMoved = false;
                    return;
            }

            if (touch.Phase == TouchPhase.Stationary &&
                (touch.Position - singleTouchStartPosition).sqrMagnitude >
                tapToRefocusMaxMovementPixels * tapToRefocusMaxMovementPixels)
            {
                singleTouchMoved = true;
            }
        }

        void UpdateDesktopInteraction()
        {
            if (!enableDesktopPreviewControls || Application.isMobilePlatform)
            {
                return;
            }

            if (IsMouseOverBlockingUi())
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemMouse mouse = InputSystemMouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 pointerPosition = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame ||
                mouse.rightButton.wasPressedThisFrame ||
                mouse.middleButton.wasPressedThisFrame)
            {
                previousMousePosition = pointerPosition;
            }

            if (mouse.leftButton.isPressed ||
                mouse.rightButton.isPressed ||
                mouse.middleButton.isPressed)
            {
                Vector2 mouseDelta = pointerPosition - (Vector2)previousMousePosition;
                previousMousePosition = pointerPosition;

                if (mouseDelta.sqrMagnitude > 0.01f)
                {
                    if (mouse.leftButton.isPressed)
                    {
                        OnRotateDelta?.Invoke(mouseDelta);
                    }
                    else
                    {
                        OnPanDelta?.Invoke(mouseDelta);
                    }
                }
            }

            float scrollDelta = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                OnScaleDelta?.Invoke(scrollDelta * desktopScrollScaleDelta);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                Vector3 currentMousePosition = Input.mousePosition;
                Vector2 mouseDelta = currentMousePosition - previousMousePosition;
                previousMousePosition = currentMousePosition;

                if (mouseDelta.sqrMagnitude > 0.01f)
                {
                    if (Input.GetMouseButton(0))
                    {
                        OnRotateDelta?.Invoke(mouseDelta);
                    }
                    else
                    {
                        OnPanDelta?.Invoke(mouseDelta);
                    }
                }
            }

            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                OnScaleDelta?.Invoke(scrollDelta * desktopScrollScaleDelta);
            }
#endif
        }

        void RefreshTouchBuffer()
        {
            touchBuffer.Clear();

#if ENABLE_INPUT_SYSTEM
            if (enhancedTouchEnabled)
            {
                var activeTouches = InputSystemTouch.activeTouches;
                for (int i = 0; i < activeTouches.Count; i++)
                {
                    InputSystemTouch touch = activeTouches[i];
                    touchBuffer.Add(new TouchSample
                    {
                        FingerId = touch.touchId,
                        Position = touch.screenPosition,
                        DeltaPosition = touch.delta,
                        Phase = ConvertTouchPhase(touch.phase)
                    });
                }
            }
#endif

            if (touchBuffer.Count > 0)
            {
                return;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            for (int i = 0; i < Input.touchCount; i++)
            {
                UnityEngine.Touch touch = Input.GetTouch(i);
                touchBuffer.Add(new TouchSample
                {
                    FingerId = touch.fingerId,
                    Position = touch.position,
                    DeltaPosition = touch.deltaPosition,
                    Phase = touch.phase
                });
            }
#endif
        }

        void FilterInteractionTouches()
        {
            interactionTouchBuffer.Clear();
            for (int i = 0; i < touchBuffer.Count; i++)
            {
                TouchSample touch = touchBuffer[i];
                if (!IsTouchOverBlockingUi(touch.Position))
                {
                    interactionTouchBuffer.Add(touch);
                }
            }
        }

        bool IsTouchOverBlockingUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                GameObject hitObject = uiRaycastResults[i].gameObject;
                if (hitObject == null) continue;

                if (hitObject.GetComponentInParent<Selectable>() != null ||
                    hitObject.GetComponentInParent<ScrollRect>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsMouseOverBlockingUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            return InputSystemMouse.current != null &&
                   IsTouchOverBlockingUi(InputSystemMouse.current.position.ReadValue());
#elif ENABLE_LEGACY_INPUT_MANAGER
            return IsTouchOverBlockingUi(Input.mousePosition);
#else
            return false;
#endif
        }

        bool IsTapGesture(TouchSample touch)
        {
            if (singleTouchMoved || singleTouchStartTime < 0f) return false;
            if (Time.unscaledTime - singleTouchStartTime > tapToRefocusMaxDuration) return false;
            return (touch.Position - singleTouchStartPosition).sqrMagnitude <= tapToRefocusMaxMovementPixels * tapToRefocusMaxMovementPixels;
        }

        static bool IsValidGesturePoint(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsNaN(point.y);
        }

        void ResetTouchInteractionState()
        {
            previousPinchDistance = -1f;
            previousTwoFingerMidpoint = new Vector2(float.NaN, float.NaN);
            singleTouchStartPosition = Vector2.zero;
            singleTouchStartTime = -1f;
            singleTouchMoved = false;
        }

        void EnableTouchBackendIfNeeded()
        {
#if ENABLE_INPUT_SYSTEM
            if (!Application.isMobilePlatform || enhancedTouchEnabled) return;
            EnhancedTouchSupport.Enable();
            enhancedTouchEnabled = true;
#endif
        }

        void DisableTouchBackendIfNeeded()
        {
#if ENABLE_INPUT_SYSTEM
            if (!enhancedTouchEnabled) return;
            EnhancedTouchSupport.Disable();
            enhancedTouchEnabled = false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static TouchPhase ConvertTouchPhase(UnityEngine.InputSystem.TouchPhase phase)
        {
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began: return TouchPhase.Began;
                case UnityEngine.InputSystem.TouchPhase.Moved: return TouchPhase.Moved;
                case UnityEngine.InputSystem.TouchPhase.Stationary: return TouchPhase.Stationary;
                case UnityEngine.InputSystem.TouchPhase.Ended: return TouchPhase.Ended;
                case UnityEngine.InputSystem.TouchPhase.Canceled: return TouchPhase.Canceled;
                default: return TouchPhase.Canceled;
            }
        }
#endif
    }
}
