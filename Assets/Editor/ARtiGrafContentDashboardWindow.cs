using System.Collections.Generic;
using System.IO;
using System.Text;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

public class ARtiGrafContentDashboardWindow : EditorWindow
{
    enum ContentFilter
    {
        All,
        Typography,
        Color,
        BarcodeOnly,
        BlendDemo,
        Issues
    }

    sealed class DashboardEntry
    {
        public MaterialContentData Content;
        public string AssetName;
        public string AssetPath;
        public string PrefabPath;
        public string ReferenceImagePath;
        public string BarcodeTargetPath;
        public bool IsInLibrary;
        public bool IsBlendDemo;
        public bool IsBarcodeOnly;
        public bool HasDuplicateScanKey;
        public readonly List<string> Notes = new List<string>();

        public bool HasIssues => Notes.Count > 0;
    }

    static readonly string[] FilterLabels =
    {
        "All",
        "Typography",
        "Color",
        "Barcode",
        "Blend",
        "Issues"
    };

    readonly List<DashboardEntry> entries = new List<DashboardEntry>();
    readonly List<string> vuforiaTargetNames = new List<string>();
    Vector2 scrollPosition;
    string searchQuery = string.Empty;
    ContentFilter activeFilter;
    MaterialContentLibrary library;
    GUIStyle noteStyle;
    double lastRefreshTime;
    string quickContentId = string.Empty;
    string quickTitle = string.Empty;
    string quickSubtitle = string.Empty;
    string quickDescription = string.Empty;
    string quickObjectType = string.Empty;
    string quickTargetName = string.Empty;
    LearningCategory quickCategory = LearningCategory.Typography;
    GameObject quickObjectAsset;
    int selectedVuforiaTargetIndex;

    [MenuItem("Tools/BuhenAR/Content/Open Dashboard")]
    [MenuItem("Tools/ARtiGraf/Dashboard")]
    [MenuItem("Window/BuhenAR/Content Dashboard")]
    public static void Open()
    {
        ARtiGrafContentDashboardWindow window = GetWindow<ARtiGrafContentDashboardWindow>("BuhenAR Content");
        window.minSize = new Vector2(980f, 560f);
        window.RefreshData();
    }

    void OnEnable()
    {
        RefreshData();
    }

    void OnFocus()
    {
        RefreshData();
    }

    void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();
        DrawQuickCreatePanel();
        DrawSummary();
        DrawContentList();
    }

    void EnsureStyles()
    {
        if (noteStyle != null)
        {
            return;
        }

        noteStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            richText = true
        };
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            RefreshData();
        }

        if (GUILayout.Button("New Content", EditorStyles.toolbarButton, GUILayout.Width(92f)))
        {
            ARtiGrafCreateContentWizardWindow.Open();
        }

        if (GUILayout.Button("Android Build", EditorStyles.toolbarButton, GUILayout.Width(96f)))
        {
            ARtiGrafAndroidBuildWindow.Open();
        }

        if (GUILayout.Button("Sync Library", EditorStyles.toolbarButton, GUILayout.Width(90f)))
        {
            ARtiGrafContentMaintenance.SyncLibraryFromScriptableObjects();
            RefreshData();
        }

        if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            ARtiGrafContentMaintenance.ValidateContentSetup();
            RefreshData();
        }

        if (GUILayout.Button("Sync + Validate", EditorStyles.toolbarButton, GUILayout.Width(110f)))
        {
            ARtiGrafContentMaintenance.SyncLibraryAndValidate();
            RefreshData();
        }

        if (GUILayout.Button("Sync Runtime", EditorStyles.toolbarButton, GUILayout.Width(94f)))
        {
            ARtiGrafContentMaintenance.SyncDashboardRuntimeSetup();
            RefreshData();
        }

        if (File.Exists(ARtiGrafContentMaintenance.ContentReportPath) &&
            GUILayout.Button("Reveal Report", EditorStyles.toolbarButton, GUILayout.Width(95f)))
        {
            EditorUtility.RevealInFinder(ARtiGrafContentMaintenance.ContentReportPath);
        }

        GUILayout.Space(12f);
        GUILayout.Label("Search", GUILayout.Width(42f));
        string nextSearch = GUILayout.TextField(searchQuery, GUILayout.MinWidth(180f));
        if (nextSearch != searchQuery)
        {
            searchQuery = nextSearch;
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("Updated: " + FormatLastRefreshTime(), EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();

        activeFilter = (ContentFilter)GUILayout.Toolbar((int)activeFilter, FilterLabels);
    }

    void DrawQuickCreatePanel()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tambah Object Cepat", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Workflow: pilih FBX/prefab 3D, pilih target marker dari Vuforia DB import, isi nama, lalu klik Create/Update + Sync. Reference Image Texture boleh kosong kalau target sudah ada di Vuforia DB.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        quickObjectAsset = (GameObject)EditorGUILayout.ObjectField(
            "FBX / Prefab 3D",
            quickObjectAsset,
            typeof(GameObject),
            false);

        EditorGUILayout.BeginVertical(GUILayout.Width(360f));
        if (vuforiaTargetNames.Count > 0)
        {
            selectedVuforiaTargetIndex = Mathf.Clamp(selectedVuforiaTargetIndex, 0, vuforiaTargetNames.Count - 1);
            int nextIndex = EditorGUILayout.Popup("Vuforia Target", selectedVuforiaTargetIndex, vuforiaTargetNames.ToArray());
            if (nextIndex != selectedVuforiaTargetIndex || string.IsNullOrWhiteSpace(quickTargetName))
            {
                selectedVuforiaTargetIndex = nextIndex;
                ApplyQuickTargetName(vuforiaTargetNames[selectedVuforiaTargetIndex]);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Belum ada target di Vuforia DB import. Import .unitypackage DB dulu, lalu Refresh.", MessageType.Warning);
        }

        quickTargetName = EditorGUILayout.TextField("Target Name", quickTargetName);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        quickContentId = SanitizeContentId(EditorGUILayout.TextField("Content ID", quickContentId));
        quickTitle = EditorGUILayout.TextField("Nama Tampilan", quickTitle);
        quickCategory = (LearningCategory)EditorGUILayout.EnumPopup("Kategori", quickCategory);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        quickObjectType = EditorGUILayout.TextField("Object Type", quickObjectType);
        quickSubtitle = EditorGUILayout.TextField("Subtitle", quickSubtitle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Deskripsi");
        quickDescription = EditorGUILayout.TextArea(quickDescription, GUILayout.MinHeight(42f));

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(quickObjectAsset == null || string.IsNullOrWhiteSpace(quickTargetName));
        if (GUILayout.Button("Create / Update + Sync Runtime", GUILayout.Height(34f)))
        {
            CreateOrUpdateQuickContent();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Reset Form", GUILayout.Height(34f), GUILayout.Width(110f)))
        {
            ResetQuickForm();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    void DrawSummary()
    {
        int typographyCount = 0;
        int colorCount = 0;
        int issueCount = 0;
        int libraryCount = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            DashboardEntry entry = entries[i];
            if (entry.Content != null)
            {
                if (entry.Content.Category == LearningCategory.Typography)
                {
                    typographyCount++;
                }
                else
                {
                    colorCount++;
                }
            }

            if (entry.IsInLibrary)
            {
                libraryCount++;
            }

            if (entry.HasIssues)
            {
                issueCount++;
            }
        }

        string message =
            "Total " + entries.Count +
            "  |  Typography " + typographyCount +
            "  |  Color " + colorCount +
            "  |  In Library " + libraryCount +
            "  |  Vuforia DB Targets " + vuforiaTargetNames.Count +
            "  |  Issues " + issueCount;

        EditorGUILayout.HelpBox(message, issueCount > 0 ? MessageType.Warning : MessageType.Info);
    }

    void DrawContentList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        int visibleCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            DashboardEntry entry = entries[i];
            if (!PassesFilter(entry) || !MatchesSearch(entry))
            {
                continue;
            }

            visibleCount++;
            DrawEntry(entry);
        }

        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox("Tidak ada content yang cocok dengan filter atau pencarian saat ini.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawEntry(DashboardEntry entry)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField(entry.Content, typeof(MaterialContentData), false);
        GUILayout.FlexibleSpace();
        DrawStatusChip(entry.IsInLibrary ? "LIB" : "NO-LIB", entry.IsInLibrary ? Color.green : Color.red);
        DrawStatusChip(entry.Content != null && entry.Content.HasPrefab ? "PREFAB" : "NO-PREFAB",
            entry.Content != null && entry.Content.HasPrefab ? new Color(0.18f, 0.65f, 0.28f) : new Color(0.77f, 0.22f, 0.22f));
        DrawStatusChip(entry.Content != null && entry.Content.HasReferenceImage ? "IMAGE" : "NO-IMAGE",
            entry.Content != null && entry.Content.HasReferenceImage ? new Color(0.16f, 0.45f, 0.82f) : new Color(0.55f, 0.55f, 0.55f));
        DrawStatusChip(!string.IsNullOrWhiteSpace(entry.BarcodeTargetPath) ? "BARCODE" : "NO-BARCODE",
            !string.IsNullOrWhiteSpace(entry.BarcodeTargetPath) ? new Color(0.78f, 0.48f, 0.12f) : new Color(0.55f, 0.55f, 0.55f));
        if (entry.HasDuplicateScanKey)
        {
            DrawStatusChip("DUPLICATE", new Color(0.75f, 0.2f, 0.2f));
        }
        EditorGUILayout.EndHorizontal();

        if (entry.Content != null)
        {
            EditorGUILayout.LabelField(
                entry.Content.Id + "  |  " + entry.Content.Category + "  |  " +
                (entry.Content.IsBarcodeOnly ? "Barcode-only" : "Image target"),
                EditorStyles.boldLabel);
        }

        EditorGUILayout.LabelField("Asset", entry.AssetPath);
        EditorGUILayout.LabelField("Prefab", string.IsNullOrWhiteSpace(entry.PrefabPath) ? "-" : entry.PrefabPath);
        EditorGUILayout.LabelField("Reference Image", string.IsNullOrWhiteSpace(entry.ReferenceImagePath) ? "-" : entry.ReferenceImagePath);
        EditorGUILayout.LabelField("Barcode Target", string.IsNullOrWhiteSpace(entry.BarcodeTargetPath) ? "-" : entry.BarcodeTargetPath);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Asset"))
        {
            Selection.activeObject = entry.Content;
            EditorGUIUtility.PingObject(entry.Content);
        }

        if (GUILayout.Button("Auto Configure"))
        {
            if (ARtiGrafContentMaintenance.AutoConfigureContent(entry.Content))
            {
                AssetDatabase.SaveAssets();
                RefreshData();
                GUIUtility.ExitGUI();
            }
        }

        if (GUILayout.Button("Edit"))
        {
            Selection.activeObject = entry.Content;
            ARtiGrafCreateContentWizardWindow.Open();
        }

        if (GUILayout.Button("Ping Prefab"))
        {
            PingAssetAtPath(entry.PrefabPath);
        }

        if (GUILayout.Button("Ping Marker"))
        {
            PingAssetAtPath(entry.ReferenceImagePath);
        }

        if (GUILayout.Button("Ping Barcode"))
        {
            PingAssetAtPath(entry.BarcodeTargetPath);
        }

        EditorGUILayout.EndHorizontal();

        if (entry.HasIssues)
        {
            EditorGUILayout.Space(2f);
            for (int i = 0; i < entry.Notes.Count; i++)
            {
                EditorGUILayout.LabelField("- " + entry.Notes[i], noteStyle);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4f);
    }

    void DrawStatusChip(string label, Color color)
    {
        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Button(label, GUILayout.Width(78f));
        GUI.backgroundColor = previousBackgroundColor;
    }

    void RefreshData()
    {
        library = ARtiGrafContentMaintenance.LoadLibrary();
        List<MaterialContentData> contents = ARtiGrafContentMaintenance.LoadAllContentsForEditor();
        List<string> importedTargets = ARtiGrafContentMaintenance.LoadImportedVuforiaDatabaseTargetNames();
        vuforiaTargetNames.Clear();
        vuforiaTargetNames.AddRange(importedTargets);
        if (string.IsNullOrWhiteSpace(quickTargetName) && vuforiaTargetNames.Count > 0)
        {
            ApplyQuickTargetName(vuforiaTargetNames[Mathf.Clamp(selectedVuforiaTargetIndex, 0, vuforiaTargetNames.Count - 1)]);
        }

        entries.Clear();

        Dictionary<string, List<MaterialContentData>> scanKeyMap = BuildScanKeyMap(contents);

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null)
            {
                continue;
            }

            DashboardEntry entry = BuildEntry(content, scanKeyMap);
            entries.Add(entry);
        }

        lastRefreshTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    void ApplyQuickTargetName(string targetName)
    {
        quickTargetName = targetName;
        if (string.IsNullOrWhiteSpace(quickContentId))
        {
            quickContentId = SanitizeContentId(targetName);
        }

        if (string.IsNullOrWhiteSpace(quickTitle))
        {
            quickTitle = BuildFriendlyTitle(quickContentId);
        }

        if (string.IsNullOrWhiteSpace(quickObjectType))
        {
            quickObjectType = quickTitle;
        }
    }

    void CreateOrUpdateQuickContent()
    {
        if (quickObjectAsset == null)
        {
            EditorUtility.DisplayDialog("BuhenAR Dashboard", "FBX atau prefab 3D belum dipilih.", "OK");
            return;
        }

        string targetName = string.IsNullOrWhiteSpace(quickTargetName) ? string.Empty : quickTargetName.Trim();
        if (string.IsNullOrWhiteSpace(targetName))
        {
            EditorUtility.DisplayDialog("BuhenAR Dashboard", "Target marker Vuforia belum dipilih/diisi.", "OK");
            return;
        }

        string contentId = SanitizeContentId(string.IsNullOrWhiteSpace(quickContentId) ? targetName : quickContentId);
        if (string.IsNullOrWhiteSpace(contentId))
        {
            EditorUtility.DisplayDialog("BuhenAR Dashboard", "Content ID tidak valid.", "OK");
            return;
        }

        EnsureFolder("Assets/ScriptableObjects");
        string assetPath = "Assets/ScriptableObjects/" + contentId + ".asset";
        MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(assetPath);
        bool isNew = content == null;
        if (isNew)
        {
            content = ScriptableObject.CreateInstance<MaterialContentData>();
            AssetDatabase.CreateAsset(content, assetPath);
        }

        string displayTitle = string.IsNullOrWhiteSpace(quickTitle) ? BuildFriendlyTitle(contentId) : quickTitle.Trim();
        SerializedObject serializedContent = new SerializedObject(content);
        serializedContent.FindProperty("id").stringValue = contentId;
        serializedContent.FindProperty("category").enumValueIndex = (int)quickCategory;
        serializedContent.FindProperty("title").stringValue = displayTitle;
        serializedContent.FindProperty("subtitle").stringValue =
            string.IsNullOrWhiteSpace(quickSubtitle) ? "Konten belajar " + displayTitle : quickSubtitle.Trim();
        serializedContent.FindProperty("description").stringValue =
            string.IsNullOrWhiteSpace(quickDescription)
                ? "Arahkan kamera ke kartu " + displayTitle + " untuk memunculkan object 3D."
                : quickDescription.Trim();
        serializedContent.FindProperty("referenceImageTexture").objectReferenceValue = null;
        serializedContent.FindProperty("prefab").objectReferenceValue = quickObjectAsset;
        serializedContent.FindProperty("referenceImageName").stringValue = targetName;
        serializedContent.FindProperty("targetWidthMeters").floatValue = 0.12f;
        serializedContent.FindProperty("objectType").stringValue =
            string.IsNullOrWhiteSpace(quickObjectType) ? displayTitle : quickObjectType.Trim();
        serializedContent.FindProperty("colorFocus").stringValue = string.Empty;
        serializedContent.FindProperty("fontTypeFocus").stringValue = "Vuforia DB target: " + targetName;
        serializedContent.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(content);

        AssetDatabase.SaveAssets();
        ARtiGrafContentMaintenance.SyncDashboardRuntimeSetup();
        Selection.activeObject = content;
        EditorGUIUtility.PingObject(content);
        RefreshData();
        Debug.Log("[ARtiGraf] Dashboard " + (isNew ? "membuat" : "mengupdate") + " content: " + assetPath);
    }

    void ResetQuickForm()
    {
        quickContentId = string.Empty;
        quickTitle = string.Empty;
        quickSubtitle = string.Empty;
        quickDescription = string.Empty;
        quickObjectType = string.Empty;
        quickTargetName = string.Empty;
        quickObjectAsset = null;
        if (vuforiaTargetNames.Count > 0)
        {
            selectedVuforiaTargetIndex = 0;
            ApplyQuickTargetName(vuforiaTargetNames[0]);
        }
    }

    DashboardEntry BuildEntry(
        MaterialContentData content,
        Dictionary<string, List<MaterialContentData>> scanKeyMap)
    {
        string assetPath = AssetDatabase.GetAssetPath(content);
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        string prefabPath = content.Prefab != null
            ? AssetDatabase.GetAssetPath(content.Prefab)
            : ARtiGrafContentMaintenance.FindMatchingPrefabPath(assetName);
        string referenceImagePath = content.ReferenceImageTexture != null
            ? AssetDatabase.GetAssetPath(content.ReferenceImageTexture)
            : ARtiGrafContentMaintenance.FindMatchingReferenceImagePath(assetName);
        string barcodeTargetPath = ARtiGrafContentMaintenance.FindMatchingBarcodeTargetPath(content.Id);

        DashboardEntry entry = new DashboardEntry
        {
            Content = content,
            AssetName = assetName,
            AssetPath = assetPath,
            PrefabPath = prefabPath,
            ReferenceImagePath = referenceImagePath,
            BarcodeTargetPath = barcodeTargetPath,
            IsInLibrary = library != null && library.Contains(content),
            IsBlendDemo = assetName.StartsWith("blend_", System.StringComparison.OrdinalIgnoreCase),
            IsBarcodeOnly = content.IsBarcodeOnly,
            HasDuplicateScanKey = HasDuplicateScanKey(content, scanKeyMap)
        };

        if (string.IsNullOrWhiteSpace(content.Id))
        {
            entry.Notes.Add("ID masih kosong.");
        }

        if (!content.HasPrefab)
        {
            entry.Notes.Add("Prefab belum terhubung.");
        }

        if (content.HasReferenceImage && string.IsNullOrWhiteSpace(content.ReferenceImageName))
        {
            entry.Notes.Add("referenceImageName masih kosong padahal reference image sudah ada.");
        }

        if (content.TargetWidthMeters <= 0f)
        {
            entry.Notes.Add("targetWidthMeters <= 0.");
        }

        if (!entry.IsInLibrary)
        {
            entry.Notes.Add("Belum terdaftar di BuhenAR content library.");
        }

        if (content.IsBarcodeOnly && string.IsNullOrWhiteSpace(barcodeTargetPath))
        {
            entry.Notes.Add("Konten barcode-only belum punya barcode target yang cocok.");
        }

        if (entry.IsBlendDemo)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                entry.Notes.Add("Prefab Blender demo expected belum ditemukan.");
            }

            if (string.IsNullOrWhiteSpace(referenceImagePath))
            {
                entry.Notes.Add("Marker image expected belum ditemukan.");
            }

            if (string.IsNullOrWhiteSpace(barcodeTargetPath))
            {
                entry.Notes.Add("Barcode target expected belum ditemukan.");
            }
        }

        if (entry.HasDuplicateScanKey)
        {
            entry.Notes.Add("ID atau referenceImageName bentrok dengan content lain.");
        }

        return entry;
    }

    Dictionary<string, List<MaterialContentData>> BuildScanKeyMap(List<MaterialContentData> contents)
    {
        var scanKeyMap = new Dictionary<string, List<MaterialContentData>>();

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null)
            {
                continue;
            }

            AddScanKey(scanKeyMap, content.NormalizedId, content);
            AddScanKey(scanKeyMap, content.NormalizedReferenceImageName, content);
        }

        return scanKeyMap;
    }

    static void AddScanKey(
        Dictionary<string, List<MaterialContentData>> scanKeyMap,
        string normalizedKey,
        MaterialContentData content)
    {
        if (string.IsNullOrWhiteSpace(normalizedKey) || content == null)
        {
            return;
        }

        if (!scanKeyMap.TryGetValue(normalizedKey, out List<MaterialContentData> mappedContents))
        {
            mappedContents = new List<MaterialContentData>();
            scanKeyMap[normalizedKey] = mappedContents;
        }

        if (!mappedContents.Contains(content))
        {
            mappedContents.Add(content);
        }
    }

    static bool HasDuplicateScanKey(
        MaterialContentData content,
        Dictionary<string, List<MaterialContentData>> scanKeyMap)
    {
        if (content == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(content.NormalizedId) &&
            scanKeyMap.TryGetValue(content.NormalizedId, out List<MaterialContentData> idContents) &&
            idContents.Count > 1)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(content.NormalizedReferenceImageName) &&
            scanKeyMap.TryGetValue(content.NormalizedReferenceImageName, out List<MaterialContentData> referenceContents) &&
            referenceContents.Count > 1)
        {
            return true;
        }

        return false;
    }

    bool PassesFilter(DashboardEntry entry)
    {
        switch (activeFilter)
        {
            case ContentFilter.Typography:
                return entry.Content != null && entry.Content.Category == LearningCategory.Typography;
            case ContentFilter.Color:
                return entry.Content != null && entry.Content.Category == LearningCategory.Color;
            case ContentFilter.BarcodeOnly:
                return entry.IsBarcodeOnly;
            case ContentFilter.BlendDemo:
                return entry.IsBlendDemo;
            case ContentFilter.Issues:
                return entry.HasIssues;
            default:
                return true;
        }
    }

    bool MatchesSearch(DashboardEntry entry)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return true;
        }

        string query = searchQuery.Trim().ToLowerInvariant();
        if (entry.Content == null)
        {
            return false;
        }

        return ContainsIgnoreCase(entry.Content.Id, query) ||
               ContainsIgnoreCase(entry.Content.Title, query) ||
               ContainsIgnoreCase(entry.Content.ReferenceImageName, query) ||
               ContainsIgnoreCase(entry.AssetName, query) ||
               ContainsIgnoreCase(entry.AssetPath, query) ||
               ContainsIgnoreCase(entry.PrefabPath, query) ||
               ContainsIgnoreCase(entry.ReferenceImagePath, query) ||
               ContainsIgnoreCase(entry.BarcodeTargetPath, query);
    }

    static bool ContainsIgnoreCase(string value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.ToLowerInvariant().Contains(query);
    }

    static void PingAssetAtPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
        }
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    static string SanitizeContentId(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(rawValue.Length);
        bool lastWasSeparator = false;

        for (int i = 0; i < rawValue.Length; i++)
        {
            char character = char.ToLowerInvariant(rawValue[i]);
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasSeparator = false;
                continue;
            }

            if (lastWasSeparator)
            {
                continue;
            }

            builder.Append('_');
            lastWasSeparator = true;
        }

        return builder.ToString().Trim('_');
    }

    static string BuildFriendlyTitle(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return string.Empty;
        }

        string[] parts = rawId.Split('_');
        StringBuilder builder = new StringBuilder(rawId.Length);

        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            string part = parts[i];
            builder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                builder.Append(part.Substring(1));
            }
        }

        return builder.ToString();
    }

    string FormatLastRefreshTime()
    {
        if (lastRefreshTime <= 0d)
        {
            return "-";
        }

        double deltaSeconds = EditorApplication.timeSinceStartup - lastRefreshTime;
        return deltaSeconds < 1d
            ? "just now"
            : Mathf.RoundToInt((float)deltaSeconds) + "s ago";
    }
}
