using System.Collections.Generic;
using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.Collection
{
    /// <summary>
    /// Singleton. Mengelola koleksi item yang sudah ditemukan anak lewat scan AR.
    /// Data persisten di PlayerPrefs.
    /// CollectionScene: tampilkan grid semua item, item belum ditemukan ditampilkan
    /// sebagai siluet abu-abu dengan tanda tanya.
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        static CollectionManager instance;
        public static CollectionManager Instance => instance;

        [Header("Library")]
        [SerializeField] MaterialContentLibrary library;
        const string DefaultLibraryResource = "ARtiGrafContentLibrary";

        [Header("Collection UI - hanya aktif di CollectionScene")]
        [SerializeField] Transform collectionGrid;
        [SerializeField] GameObject collectionItemPrefab;
        [SerializeField] Text totalDiscoveredText;

        Coroutine buildRoutine;
        LearningCategory? activeFilter;

        void Awake()
        {
            bool hasSceneUi = HasCollectionSceneUi();
            if (instance != null && instance != this)
            {
                bool existingHasSceneUi = instance.HasCollectionSceneUi();
                if (hasSceneUi && !existingHasSceneUi)
                {
                    Destroy(instance.gameObject);
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }

            instance = this;
            if (!hasSceneUi)
            {
                DontDestroyOnLoad(gameObject);
            }
            EnsureLibrary();
        }

        void Start()
        {
            ApplyResponsiveLayout();
            if (collectionGrid != null && collectionItemPrefab != null)
            {
                RequestBuildCollectionUI(null);
            }
            else
            {
                RefreshCounterText(null);
            }
        }

        void OnDestroy()
        {
            if (buildRoutine != null)
            {
                StopCoroutine(buildRoutine);
                buildRoutine = null;
            }

            if (instance == this)
                instance = null;
        }

        bool HasCollectionSceneUi()
        {
            return collectionGrid != null || collectionItemPrefab != null || totalDiscoveredText != null;
        }

        System.Collections.IEnumerator BuildCollectionUINextFrame(LearningCategory? filter)
        {
            if (totalDiscoveredText != null)
                totalDiscoveredText.text = "Memuat koleksi...";

            yield return null;

            EnsureLibrary();
            ApplyResponsiveLayout();

            if (collectionGrid == null || collectionItemPrefab == null || library == null)
            {
                RefreshCounterText(filter);
                buildRoutine = null;
                yield break;
            }

            ClearGrid();

            int visibleIndex = 0;
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData item = library.Items[i];
                if (item == null || item.IsDemoContent) continue;
                if (filter.HasValue && item.Category != filter.Value) continue;

                bool discovered = IsDiscovered(item.Id);
                bool hasStar = AppSession.HasStarForContent(item.Id);

                try
                {
                    GameObject go = Instantiate(collectionItemPrefab, collectionGrid);
                    go.name = "CollectionItem_" + (string.IsNullOrWhiteSpace(item.Id) ? item.Title : item.Id);
                    go.SetActive(true);

                    CollectionItemUI ui = go.GetComponent<CollectionItemUI>();
                    if (ui != null)
                    {
                        ui.Setup(item, discovered, hasStar);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Failed to build collection item for " + item.name + ": " + ex.Message);
                }

                visibleIndex++;
                if (visibleIndex % 4 == 0)
                {
                    RefreshCounterText(filter);
                    yield return null;
                }
            }

            RefreshCounterText(filter);
            buildRoutine = null;
        }

        void EnsureLibrary()
        {
            if (library == null)
                library = Resources.Load<MaterialContentLibrary>(DefaultLibraryResource);
        }

        // ── Catat item ditemukan ──────────────────────────────────────────────
        public void RecordDiscovered(MaterialContentData content)
        {
            AppSession.RecordDiscoveredContent(content);
        }

        public bool IsDiscovered(string contentId)
        {
            return AppSession.IsContentDiscovered(contentId);
        }

        public int GetDiscoveredCount(LearningCategory? category = null)
        {
            EnsureLibrary();
            if (library == null) return 0;
            int count = 0;
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData item = library.Items[i];
                if (item == null || item.IsDemoContent) continue;
                if (category.HasValue && item.Category != category.Value) continue;
                if (IsDiscovered(item.Id)) count++;
            }
            return count;
        }

        public int GetTotalCount(LearningCategory? category = null)
        {
            EnsureLibrary();
            if (library == null) return 0;
            int count = 0;
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData item = library.Items[i];
                if (item == null || item.IsDemoContent) continue;
                if (category.HasValue && item.Category != category.Value) continue;
                count++;
            }
            return count;
        }

        public List<MaterialContentData> GetDiscoveredItems(LearningCategory? category = null)
        {
            EnsureLibrary();
            var result = new List<MaterialContentData>();
            if (library == null) return result;
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData item = library.Items[i];
                if (item == null || item.IsDemoContent) continue;
                if (category.HasValue && item.Category != category.Value) continue;
                if (IsDiscovered(item.Id)) result.Add(item);
            }
            return result;
        }

        // ── Build UI grid koleksi ─────────────────────────────────────────────
        public void BuildCollectionUI(LearningCategory? filter = null)
        {
            RequestBuildCollectionUI(filter);
        }

        void RequestBuildCollectionUI(LearningCategory? filter)
        {
            activeFilter = filter;
            if (!isActiveAndEnabled)
                return;

            if (buildRoutine != null)
            {
                StopCoroutine(buildRoutine);
                buildRoutine = null;
            }

            buildRoutine = StartCoroutine(BuildCollectionUINextFrame(activeFilter));
        }

        void ClearGrid()
        {
            if (collectionGrid == null) return;

            var children = new List<GameObject>();
            foreach (Transform child in collectionGrid)
            {
                if (child != null && child.gameObject != collectionItemPrefab)
                    children.Add(child.gameObject);
            }

            for (int i = 0; i < children.Count; i++)
                Destroy(children[i]);
        }

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyResponsiveLayout();
        }

        void ApplyResponsiveLayout()
        {
            if (collectionGrid == null) return;

            Vector2 size = ResolveCanvasSize();
            bool portrait = size.y >= size.x;
            float width = Mathf.Max(320f, size.x);
            float height = Mathf.Max(480f, size.y);
            bool compact = Mathf.Min(width, height) < 760f;

            float scrollMinX = portrait ? 0.19f : 0.126f;
            float scrollMaxX = portrait ? 0.81f : 0.873f;
            float scrollMinY = portrait ? 0.13f : 0.128f;
            float scrollMaxY = portrait ? 0.78f : 0.742f;
            int columns = portrait ? 4 : 7;
            float usableWidth = width * (scrollMaxX - scrollMinX);
            float spacingX = portrait ? Mathf.Clamp(width * 0.014f, 8f, 16f) : Mathf.Clamp(width * 0.016f, 20f, 30f);
            float spacingY = portrait ? Mathf.Clamp(height * 0.008f, 8f, 15f) : Mathf.Clamp(height * 0.015f, 10f, 16f);
            float padX = portrait ? Mathf.Clamp(width * 0.018f, 8f, 18f) : Mathf.Clamp(width * 0.028f, 36f, 52f);
            float padY = portrait ? Mathf.Clamp(width * 0.018f, 8f, 18f) : Mathf.Clamp(height * 0.012f, 8f, 14f);
            float cellWidth = (usableWidth - padX * 2f - spacingX * (columns - 1)) / columns;
            float cellHeight = portrait ? cellWidth * 1.30f : cellWidth * 0.92f;

            GridLayoutGroup grid = collectionGrid.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.cellSize = new Vector2(cellWidth, cellHeight);
                grid.spacing = new Vector2(spacingX, spacingY);
                int paddingX = Mathf.RoundToInt(padX);
                int paddingY = Mathf.RoundToInt(padY);
                grid.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY + 4);
            }

            ScrollRect scroll = collectionGrid.GetComponentInParent<ScrollRect>();
            RectTransform scrollRect = scroll != null ? scroll.GetComponent<RectTransform>() : null;
            if (scrollRect != null)
            {
                SetAnchors(scrollRect,
                    new Vector2(scrollMinX, scrollMinY),
                    new Vector2(scrollMaxX, scrollMaxY));
            }

            Image scrollImage = scroll != null ? scroll.GetComponent<Image>() : null;
            if (scrollImage != null)
                scrollImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform counterRect = totalDiscoveredText != null ? totalDiscoveredText.rectTransform : null;
            SetAnchors(counterRect,
                portrait ? new Vector2(0.31f, 0.807f) : new Vector2(0.355f, 0.747f),
                portrait ? new Vector2(0.69f, 0.846f) : new Vector2(0.645f, 0.812f));
            ConfigureText(totalDiscoveredText, portrait ? 24 : 26, compact ? 14 : 16, TextAnchor.MiddleCenter);
        }

        Vector2 ResolveCanvasSize()
        {
            ResponsiveArtworkFrame frame = collectionGrid != null ? collectionGrid.GetComponentInParent<ResponsiveArtworkFrame>() : null;
            RectTransform frameRect = frame != null ? frame.GetComponent<RectTransform>() : null;
            if (frameRect != null && frameRect.rect.width > 1f && frameRect.rect.height > 1f)
                return new Vector2(frameRect.rect.width, frameRect.rect.height);

            Canvas canvas = collectionGrid != null ? collectionGrid.GetComponentInParent<Canvas>() : null;
            RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return new Vector2(canvasRect.rect.width, canvasRect.rect.height);
            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ConfigureText(Text text, int maxSize, int minSize, TextAnchor anchor)
        {
            BuhenARTextStyle.Configure(text, maxSize, minSize, anchor, VerticalWrapMode.Truncate, 1.08f);
        }

        public void SetFilter(int categoryIndex)
        {
            if (categoryIndex < 0) RequestBuildCollectionUI(null);
            else RequestBuildCollectionUI((LearningCategory)categoryIndex);
        }

        void RefreshCounterText(LearningCategory? filter)
        {
            if (totalDiscoveredText == null) return;
            int found = GetDiscoveredCount(filter);
            int total = GetTotalCount(filter);
            totalDiscoveredText.text = found + " / " + total + " ditemukan";
        }
    }
}
