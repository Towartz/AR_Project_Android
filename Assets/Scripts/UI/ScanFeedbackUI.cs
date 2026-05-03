using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using ARtiGraf.Data;

namespace ARtiGraf.UI
{
    public class ScanFeedbackUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] float displayDuration = 2.5f;
        [SerializeField] CanvasGroup canvasGroup;

        Coroutine fadeRoutine;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        public void ShowFeedback(MaterialContentData content, string message)
        {
            if (panel == null) return;

            if (titleText != null)
                titleText.text = content != null ? content.Title : "Unknown";

            if (statusText != null)
                statusText.text = message;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(ShowAndFadeOut());
        }

        IEnumerator ShowAndFadeOut()
        {
            panel.SetActive(true);

            if (canvasGroup != null)
            {
                float t = 0;
                while (t < 0.2f)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = t / 0.2f;
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(displayDuration);

            if (canvasGroup != null)
            {
                float t = 0;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = 1f - (t / 0.5f);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            panel.SetActive(false);
        }
    }
}