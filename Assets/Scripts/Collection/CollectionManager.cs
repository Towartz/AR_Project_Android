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

            float scrollMinX = portrait ? (compact ? 0.035f : 0.06f) : 0.10f;
            float scrollMaxX = portrait ? (compact ? 0.965f : 0.94f) : 0.90f;
            int columns = portrait
                ? (width < 430f ? 2 : width < 900f ? 3 : 4)
                : (width < 900f ? 4 : 5);
            float usableWidth = width * (scrollMaxX - scrollMinX);
            float spacingX = portrait ? Mathf.Clamp(width * 0.02f, 10f, 22f) : Mathf.Clamp(width * 0.014f, 14f, 26f);
            float pad = portrait ? Mathf.Clamp(width * 0.035f, 12f, 28f) : Mathf.Clamp(width * 0.03f, 18f, 34f);
            float cellWidth = (usableWidth - pad * 2f - spacingX * (columns - 1)) / columns;
            float cellHeight = cellWidth * 1.36f;

            GridLayoutGroup grid = collectionGrid.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.cellSize = new Vector2(cellWidth, cellHeight);
                grid.spacing = new Vector2(spacingX, portrait ? Mathf.Clamp(height * 0.012f, 12f, 24f) : 18f);
                int padding = Mathf.RoundToInt(pad);
                grid.padding = new RectOffset(padding, padding, padding, padding + 4);
            }

            ScrollRect scroll = collectionGrid.GetComponentInParent<ScrollRect>();
            RectTransform scrollRect = scroll != null ? scroll.GetComponent<RectTransform>() : null;
            if (scrollRect != null)
            {
                SetAnchors(scrollRect,
                    portrait ? new Vector2(scrollMinX, compact ? 0.055f : 0.07f) : new Vector2(scrollMinX, 0.08f),
                    portrait ? new Vector2(scrollMaxX, 0.78f) : new Vector2(scrollMaxX, 0.80f));
            }

            Image scrollImage = scroll != null ? scroll.GetComponent<Image>() : null;
            if (scrollImage != null)
                scrollImage.color = new Color(1f, 1f, 1f, portrait ? 0.08f : 0.10f);

            RectTransform counterRect = totalDiscoveredText != null ? totalDiscoveredText.rectTransform : null;
            SetAnchors(counterRect,
                portrait ? new Vector2(0.12f, 0.82f) : new Vector2(0.25f, 0.84f),
                portrait ? new Vector2(0.88f, 0.87f) : new Vector2(0.75f, 0.89f));
            ConfigureText(totalDiscoveredText, portrait ? 30 : 30, compact ? 16 : 17, TextAnchor.MiddleCenter);
        }

        Vector2 ResolveCanvasSize()
        {
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
