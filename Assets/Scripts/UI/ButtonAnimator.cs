using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Animasi skala (hover/press) untuk Button tanpa memblokir Button.onClick.
    /// EventTrigger menggantikan IPointerClickHandler dari Button sehingga onClick
    /// tidak terpanggil di Android. Class ini implement interface pointer secara
    /// langsung agar onClick Button tetap berjalan normal.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonAnimator : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField] float hoverScale = 1.05f;
        [SerializeField] float pressScale = 0.95f;
        [SerializeField] float duration   = 0.12f;

        Vector3    _originalScale;
        Coroutine  _scaleCoroutine;

        void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData) => AnimateTo(hoverScale);
        public void OnPointerExit(PointerEventData eventData)  => AnimateTo(1f);
        public void OnPointerDown(PointerEventData eventData)  => AnimateTo(pressScale);
        public void OnPointerUp(PointerEventData eventData)    => AnimateTo(1f);

        void AnimateTo(float targetMultiplier)
        {
            if (_scaleCoroutine != null)
                StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleRoutine(_originalScale * targetMultiplier));
        }

        IEnumerator ScaleRoutine(Vector3 target)
        {
            Vector3 start   = transform.localScale;
            float   elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = t * t * (3f - 2f * t);
                transform.localScale = Vector3.Lerp(start, target, s);
                yield return null;
            }

            transform.localScale = target;
        }
    }
}
