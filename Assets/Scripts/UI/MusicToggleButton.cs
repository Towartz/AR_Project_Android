using ARtiGraf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    public class MusicToggleButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Text label;
        [SerializeField] string unmutedLabel = "Musik ON";
        [SerializeField] string mutedLabel = "Musik OFF";

        void Awake()
        {
            ResolveReferences();
            if (button != null)
                button.onClick.AddListener(BackgroundMusicController.ToggleGlobalMute);
        }

        void OnEnable()
        {
            BackgroundMusicController.StateChanged += RefreshLabel;
            RefreshLabel();
        }

        void OnDisable()
        {
            BackgroundMusicController.StateChanged -= RefreshLabel;
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(BackgroundMusicController.ToggleGlobalMute);
        }

        public void RefreshLabel()
        {
            if (label != null)
                label.text = BackgroundMusicController.IsMuted ? mutedLabel : unmutedLabel;
        }

        void ResolveReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (label == null)
                label = GetComponentInChildren<Text>(true);
        }
    }
}
