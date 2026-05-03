using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.UI;
using UnityEngine;

namespace ARtiGraf.AR
{
    public class MaterialContentController : MonoBehaviour
    {
        const string DefaultLibraryResourceName = "ARtiGrafContentLibrary";

        [SerializeField] MaterialContentLibrary library;
        [SerializeField] UIOverlayController overlayController;
        [SerializeField] bool allowDemoContentInRuntime;

        public MaterialContentLibrary Library => library;
        public bool AllowDemoContentInRuntime => allowDemoContentInRuntime;

        void Awake()
        {
            EnsureLibrary();
        }

        public bool EnsureLibrary()
        {
            if (library != null)
            {
                return true;
            }

            library = Resources.Load<MaterialContentLibrary>(DefaultLibraryResourceName);
            return library != null;
        }

        public bool TryGetContent(string referenceImageName, out MaterialContentData content)
        {
            EnsureLibrary();
            if (library == null || string.IsNullOrWhiteSpace(referenceImageName))
            {
                content = null;
                return false;
            }

            if (!library.TryGetByReferenceImage(referenceImageName, out content))
            {
                return false;
            }

            if (!IsRuntimeContentAllowed(content) || !MatchesSelectedCategory(content))
            {
                content = null;
                return false;
            }

            return true;
        }

        public bool TryGetContentForScanPayload(string scanPayload, out MaterialContentData content)
        {
            EnsureLibrary();
            if (library == null || string.IsNullOrWhiteSpace(scanPayload))
            {
                content = null;
                return false;
            }

            if (!library.TryGetByScanPayload(scanPayload, out content))
            {
                return false;
            }

            if (!IsRuntimeContentAllowed(content) || !MatchesSelectedCategory(content))
            {
                content = null;
                return false;
            }

            return true;
        }

        public bool IsRuntimeContentAllowed(MaterialContentData content)
        {
            return content != null &&
                   content.HasPrefab &&
                   (allowDemoContentInRuntime || !content.IsDemoContent);
        }

        public int GetRuntimeContentCount(LearningCategory? category)
        {
            EnsureLibrary();
            if (library == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData content = library.Items[i];
                if (!IsRuntimeContentAllowed(content))
                {
                    continue;
                }

                if (category.HasValue && content.Category != category.Value)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        bool MatchesSelectedCategory(MaterialContentData content)
        {
            return !AppSession.SelectedCategory.HasValue ||
                   (content != null && content.Category == AppSession.SelectedCategory.Value);
        }

        string BuildSessionContext()
        {
            string categoryLabel = AppSession.GetSelectedCategoryLabel();
            string modeLabel = AppSession.SelectedCategory.HasValue
                ? "Mode " + categoryLabel + " aktif"
                : "Mode scan semua materi";
            int discoveredCount = AppSession.GetDiscoveredContentCount(AppSession.SelectedCategory);
            int totalCount = GetRuntimeContentCount(AppSession.SelectedCategory);
            string discoveredLabel = totalCount > 0
                ? " • Ditemukan " + Mathf.Clamp(discoveredCount, 0, totalCount) + "/" + totalCount
                : string.Empty;
            string scanLabel = AppSession.TotalScansThisRun > 0
                ? " • Scan " + AppSession.TotalScansThisRun
                : string.Empty;
            return modeLabel + discoveredLabel + scanLabel;
        }

        void RefreshOverlayContext()
        {
            if (overlayController != null)
            {
                overlayController.SetSessionContext(BuildSessionContext());
            }
        }

        public void ShowContent(MaterialContentData content)
        {
            AppSession.RecordViewedContent(content);

            if (overlayController != null)
            {
                overlayController.ShowContent(content);
            }

            RefreshOverlayContext();
        }

        public void PreviewContent(MaterialContentData content)
        {
            if (overlayController != null)
            {
                overlayController.ShowContent(content);
            }

            RefreshOverlayContext();
        }

        public void HideContent()
        {
            if (overlayController != null)
            {
                overlayController.HideContent();
            }

            RefreshOverlayContext();
        }

        public void SetStatus(string message)
        {
            if (overlayController != null)
            {
                overlayController.SetTrackingStatus(message);
            }
        }

        public void SetPreviewModeActive(bool isActive)
        {
            if (overlayController != null)
            {
                overlayController.SetPreviewModeActive(isActive);
            }
        }

        public void SetEditorCameraFeedActive(bool isActive)
        {
            if (overlayController != null)
            {
                overlayController.SetEditorCameraFeedActive(isActive);
            }
        }

        public void SetEditorCameraFeedTexture(Texture texture, bool isActive, bool verticallyMirrored, int rotationAngle)
        {
            if (overlayController != null)
            {
                overlayController.SetEditorCameraFeedTexture(texture, isActive, verticallyMirrored, rotationAngle);
            }
        }

        public void InitializeOverlayContext()
        {
            RefreshOverlayContext();
        }
    }
}
