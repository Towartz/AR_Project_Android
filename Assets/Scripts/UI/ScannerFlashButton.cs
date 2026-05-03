using ARtiGraf.AR;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    public class ScannerFlashButton : MonoBehaviour
    {
        [SerializeField] ARImageTrackingController scanner;
        [SerializeField] Button button;
        [SerializeField] Text label;
        [SerializeField] string flashOffLabel = "Flash";
        [SerializeField] string flashOnLabel = "Flash ON";

        void Awake()
        {
            ResolveReferences();
            if (button != null)
            {
                button.onClick.AddListener(ToggleFlash);
            }
        }

        void OnEnable()
        {
            RefreshLabel();
        }

        void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ToggleFlash);
            }
        }

        void ToggleFlash()
        {
            ResolveReferences();
            if (scanner != null)
            {
                scanner.ToggleFlash();
            }

            RefreshLabel();
        }

        void RefreshLabel()
        {
            ResolveReferences();
            if (label != null)
            {
                label.text = scanner != null && scanner.IsFlashEnabled ? flashOnLabel : flashOffLabel;
            }
        }

        void ResolveReferences()
        {
            if (scanner == null)
            {
                scanner = FindAnyObjectByType<ARImageTrackingController>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }
        }
    }
}
