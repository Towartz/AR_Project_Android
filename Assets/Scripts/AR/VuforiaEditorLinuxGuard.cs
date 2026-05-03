using UnityEngine;
using Vuforia;

namespace ARtiGraf.AR
{
    [DefaultExecutionOrder(-32000)]
    public class VuforiaEditorLinuxGuard : MonoBehaviour
    {
        [SerializeField] VuforiaBehaviour vuforiaBehaviour;
        [SerializeField] MonoBehaviour initializationErrorHandler;

        void Awake()
        {
            if (!ShouldDisableVuforiaForEditorPreview())
            {
                return;
            }

            if (vuforiaBehaviour == null)
            {
                vuforiaBehaviour = GetComponent<VuforiaBehaviour>();
            }

            if (initializationErrorHandler == null)
            {
                initializationErrorHandler = GetComponent<DefaultInitializationErrorHandler>();
            }

            if (initializationErrorHandler != null)
            {
                initializationErrorHandler.enabled = false;
            }

            if (vuforiaBehaviour != null)
            {
                vuforiaBehaviour.enabled = false;
            }

            Debug.Log("[ARtiGraf] Vuforia editor runtime dimatikan di Linux. Scene akan memakai preview mode tanpa kamera.");
        }

        static bool ShouldDisableVuforiaForEditorPreview()
        {
#if UNITY_EDITOR
            return Application.platform == RuntimePlatform.LinuxEditor;
#else
            return false;
#endif
        }
    }
}
