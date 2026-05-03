using System.IO;
using System.Text;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

public class ARtiGrafCreateContentWizardWindow : EditorWindow
{
    const string ScriptableObjectsFolder = "Assets/ScriptableObjects";

    string contentId = string.Empty;
    string contentTitle = string.Empty;
    string subtitle = string.Empty;
    string description = string.Empty;
    string objectType = string.Empty;
    string colorFocus = string.Empty;
    string fontTypeFocus = string.Empty;
    string referenceImageName = string.Empty;
    LearningCategory category = LearningCategory.Typography;
    GameObject prefab;
    Texture2D referenceImageTexture;
    float targetWidthMeters = 0.12f;
    bool barcodeOnly;
    bool autoAddToLibrary = true;
    bool autoSyncRuntimeSetup = true;
    bool validateAfterCreate = true;
    bool selectCreatedAsset = true;
    bool useIdAsReferenceImageName = true;
    Vector2 scrollPosition;

    [MenuItem("Tools/BuhenAR/Content/Create New Content")]
    public static void Open()
    {
        ARtiGrafCreateContentWizardWindow window = GetWindow<ARtiGrafCreateContentWizardWindow>("Create Content");
        window.minSize = new Vector2(560f, 640f);
        window.TryPrefillFromSelection();
    }

    [MenuItem("Assets/BuhenAR/Content/Create New Content From Selection", true)]
    static bool CanOpenFromSelection()
    {
        return Selection.activeObject != null;
    }

    [MenuItem("Assets/BuhenAR/Content/Create New Content From Selection")]
    static void OpenFromSelection()
    {
        Open();
    }

    void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            TryPrefillFromSelection();
        }
    }

    void OnGUI()
    {
        DrawToolbar();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        DrawIdentitySection();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Linked Assets", EditorStyles.boldLabel);
        DrawLinkedAssetsSection();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display Content", EditorStyles.boldLabel);
        DrawDisplaySection();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Workflow Options", EditorStyles.boldLabel);
        DrawWorkflowSection();

        EditorGUILayout.Space();
        DrawStatusSection();

        EditorGUILayout.Space();
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Prefill From Selection", EditorStyles.toolbarButton, GUILayout.Width(130f)))
        {
            TryPrefillFromSelection();
        }

        if (GUILayout.Button("Auto Link By ID", EditorStyles.toolbarButton, GUILayout.Width(105f)))
        {
            AutoLinkById();
        }

        if (GUILayout.Button("Open Dashboard", EditorStyles.toolbarButton, GUILayout.Width(100f)))
        {
            ARtiGrafContentDashboardWindow.Open();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void DrawIdentitySection()
    {
        EditorGUI.BeginChangeCheck();
        string nextContentId = EditorGUILayout.TextField("Content ID", contentId);
        if (EditorGUI.EndChangeCheck())
        {
            contentId = SanitizeContentId(nextContentId);
            if (string.IsNullOrWhiteSpace(contentTitle))
            {
                contentTitle = BuildFriendlyTitle(contentId);
            }

            if (string.IsNullOrWhiteSpace(referenceImageName))
            {
                referenceImageName = contentId;
            }

            category = InferCategory(contentId);
        }

        category = (LearningCategory)EditorGUILayout.EnumPopup("Category", category);
        barcodeOnly = EditorGUILayout.Toggle("Barcode Only", barcodeOnly);
        targetWidthMeters = EditorGUILayout.FloatField("Target Width (m)", targetWidthMeters);
    }

    void DrawLinkedAssetsSection()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        EditorGUI.BeginDisabledGroup(barcodeOnly);
        referenceImageTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Reference Image",
            referenceImageTexture,
            typeof(Texture2D),
            false);
        useIdAsReferenceImageName = EditorGUILayout.Toggle("Use ID As Ref Name", useIdAsReferenceImageName);
        if (useIdAsReferenceImageName)
        {
            referenceImageName = SanitizeContentId(contentId);
            EditorGUILayout.TextField("Reference Image Name", referenceImageName);
        }
        else
        {
            referenceImageName = EditorGUILayout.TextField("Reference Image Name", referenceImageName);
        }
        EditorGUI.EndDisabledGroup();

        if (barcodeOnly)
        {
            EditorGUILayout.HelpBox(
                "Mode barcode-only akan mengosongkan reference image dan referenceImageName saat asset dibuat.",
                MessageType.Info);
        }
    }

    void DrawDisplaySection()
    {
        contentTitle = EditorGUILayout.TextField("Title", contentTitle);
        subtitle = EditorGUILayout.TextField("Subtitle", subtitle);

        EditorGUILayout.LabelField("Description");
        description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(72f));

        objectType = EditorGUILayout.TextField("Object Type", objectType);
        colorFocus = EditorGUILayout.TextField("Color Focus", colorFocus);
        fontTypeFocus = EditorGUILayout.TextField("Font Focus", fontTypeFocus);
    }

    void DrawWorkflowSection()
    {
        autoAddToLibrary = EditorGUILayout.Toggle("Auto Add To Library", autoAddToLibrary);
        autoSyncRuntimeSetup = EditorGUILayout.Toggle("Auto Sync AR Scene", autoSyncRuntimeSetup);
        validateAfterCreate = EditorGUILayout.Toggle("Validate After Create", validateAfterCreate);
        selectCreatedAsset = EditorGUILayout.Toggle("Select Created Asset", selectCreatedAsset);
    }

    void DrawStatusSection()
    {
        string assetPath = BuildAssetPath();
        bool assetExists = AssetDatabase.LoadAssetAtPath<MaterialContentData>(assetPath) != null;

        string matchingPrefabPath = string.IsNullOrWhiteSpace(contentId)
            ? string.Empty
            : ARtiGrafContentMaintenance.FindMatchingPrefabPath(contentId);
        string matchingReferenceImagePath = string.IsNullOrWhiteSpace(contentId)
            ? string.Empty
            : ARtiGrafContentMaintenance.FindMatchingReferenceImagePath(contentId);
        string matchingBarcodePath = string.IsNullOrWhiteSpace(contentId)
            ? string.Empty
            : ARtiGrafContentMaintenance.FindMatchingBarcodeTargetPath(contentId);

        EditorGUILayout.HelpBox(
            "Asset Path: " + assetPath + (assetExists ? "  (existing asset akan diupdate)" : "  (asset baru akan dibuat)"),
            assetExists ? MessageType.Warning : MessageType.Info);

        if (string.IsNullOrWhiteSpace(contentId))
        {
            EditorGUILayout.HelpBox("Content ID wajib diisi.", MessageType.Warning);
        }

        if (prefab == null && !string.IsNullOrWhiteSpace(matchingPrefabPath))
        {
            EditorGUILayout.HelpBox("Prefab candidate ditemukan: " + matchingPrefabPath, MessageType.Info);
        }

        if (!barcodeOnly && referenceImageTexture == null && !string.IsNullOrWhiteSpace(matchingReferenceImagePath))
        {
            EditorGUILayout.HelpBox("Reference image candidate ditemukan: " + matchingReferenceImagePath, MessageType.Info);
        }

        if (!string.IsNullOrWhiteSpace(matchingBarcodePath))
        {
            EditorGUILayout.HelpBox("Barcode target candidate ditemukan: " + matchingBarcodePath, MessageType.None);
        }
    }

    void DrawActionButtons()
    {
        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(contentId));

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create / Update Content", GUILayout.Height(38f)))
        {
            CreateOrUpdateContent();
        }

        if (GUILayout.Button("Create + Sync + Validate", GUILayout.Height(38f)))
        {
            bool previousAutoAddToLibrary = autoAddToLibrary;
            bool previousValidateAfterCreate = validateAfterCreate;
            autoAddToLibrary = true;
            validateAfterCreate = true;
            CreateOrUpdateContent();
            autoAddToLibrary = previousAutoAddToLibrary;
            validateAfterCreate = previousValidateAfterCreate;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
    }

    void TryPrefillFromSelection()
    {
        Object[] selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            return;
        }

        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i] is MaterialContentData existingContent)
            {
                PrefillFromExistingContent(existingContent);
                return;
            }
        }

        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i] is GameObject selectedPrefab)
            {
                prefab = selectedPrefab;
                if (string.IsNullOrWhiteSpace(contentId))
                {
                    contentId = SanitizeContentId(selectedPrefab.name);
                }
            }
            else if (selection[i] is Texture2D selectedTexture)
            {
                referenceImageTexture = selectedTexture;
                if (string.IsNullOrWhiteSpace(contentId))
                {
                    contentId = SanitizeContentId(selectedTexture.name.Replace("_marker", string.Empty));
                }
            }
        }

        ApplyCommonPrefillDefaults();
    }

    void PrefillFromExistingContent(MaterialContentData existingContent)
    {
        if (existingContent == null)
        {
            return;
        }

        contentId = existingContent.Id;
        contentTitle = existingContent.Title;
        subtitle = existingContent.Subtitle;
        description = existingContent.Description;
        objectType = existingContent.ObjectType;
        colorFocus = existingContent.ColorFocus;
        fontTypeFocus = existingContent.FontTypeFocus;
        referenceImageName = existingContent.ReferenceImageName;
        category = existingContent.Category;
        prefab = existingContent.Prefab;
        referenceImageTexture = existingContent.ReferenceImageTexture;
        targetWidthMeters = existingContent.TargetWidthMeters > 0f ? existingContent.TargetWidthMeters : 0.12f;
        barcodeOnly = existingContent.IsBarcodeOnly;
        useIdAsReferenceImageName = string.Equals(existingContent.ReferenceImageName, existingContent.Id);
    }

    void ApplyCommonPrefillDefaults()
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return;
        }

        contentId = SanitizeContentId(contentId);
        if (string.IsNullOrWhiteSpace(contentTitle))
        {
            contentTitle = BuildFriendlyTitle(contentId);
        }

        if (string.IsNullOrWhiteSpace(subtitle))
        {
            subtitle = "Konten AR baru";
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = "Scan marker atau barcode " + contentId + " untuk memunculkan konten ini.";
        }

        if (string.IsNullOrWhiteSpace(objectType))
        {
            objectType = BuildFriendlyTitle(contentId);
        }

        if (string.IsNullOrWhiteSpace(referenceImageName))
        {
            referenceImageName = contentId;
        }

        category = InferCategory(contentId);
        AutoLinkById();
    }

    void AutoLinkById()
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return;
        }

        string matchingPrefabPath = ARtiGrafContentMaintenance.FindMatchingPrefabPath(contentId);
        if (prefab == null && !string.IsNullOrWhiteSpace(matchingPrefabPath))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(matchingPrefabPath);
        }

        if (!barcodeOnly)
        {
            string matchingReferenceImagePath = ARtiGrafContentMaintenance.FindMatchingReferenceImagePath(contentId);
            if (referenceImageTexture == null && !string.IsNullOrWhiteSpace(matchingReferenceImagePath))
            {
                referenceImageTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(matchingReferenceImagePath);
            }

            if (useIdAsReferenceImageName || string.IsNullOrWhiteSpace(referenceImageName))
            {
                referenceImageName = contentId;
            }
        }
    }

    void CreateOrUpdateContent()
    {
        EnsureScriptableObjectsFolder();

        string assetPath = BuildAssetPath();
        MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(assetPath);
        bool isNewAsset = content == null;

        if (isNewAsset)
        {
            content = ScriptableObject.CreateInstance<MaterialContentData>();
            AssetDatabase.CreateAsset(content, assetPath);
        }

        SerializedObject serializedContent = new SerializedObject(content);
        serializedContent.FindProperty("id").stringValue = contentId;
        serializedContent.FindProperty("category").enumValueIndex = (int)category;
        serializedContent.FindProperty("title").stringValue = contentTitle;
        serializedContent.FindProperty("subtitle").stringValue = subtitle;
        serializedContent.FindProperty("description").stringValue = description;
        serializedContent.FindProperty("prefab").objectReferenceValue = prefab;
        serializedContent.FindProperty("objectType").stringValue = objectType;
        serializedContent.FindProperty("colorFocus").stringValue = colorFocus;
        serializedContent.FindProperty("fontTypeFocus").stringValue = fontTypeFocus;
        serializedContent.FindProperty("targetWidthMeters").floatValue = targetWidthMeters > 0f ? targetWidthMeters : 0.12f;

        if (barcodeOnly)
        {
            serializedContent.FindProperty("referenceImageTexture").objectReferenceValue = null;
            serializedContent.FindProperty("referenceImageName").stringValue = string.Empty;
        }
        else
        {
            serializedContent.FindProperty("referenceImageTexture").objectReferenceValue = referenceImageTexture;
            serializedContent.FindProperty("referenceImageName").stringValue =
                useIdAsReferenceImageName ? contentId : referenceImageName;
        }

        serializedContent.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(content);
        AssetDatabase.SaveAssets();

        if (autoAddToLibrary)
        {
            ARtiGrafContentMaintenance.SyncLibraryFromScriptableObjects();
        }

        if (autoSyncRuntimeSetup)
        {
            ARtiGrafContentMaintenance.SyncARSceneScanTargets();
        }

        if (validateAfterCreate)
        {
            ARtiGrafContentMaintenance.ValidateContentSetup();
        }

        if (selectCreatedAsset)
        {
            Selection.activeObject = content;
            EditorGUIUtility.PingObject(content);
        }

        Debug.Log("[ARtiGraf] Content " + (isNewAsset ? "dibuat" : "diupdate") + ": " + assetPath);
    }

    void EnsureScriptableObjectsFolder()
    {
        if (AssetDatabase.IsValidFolder(ScriptableObjectsFolder))
        {
            return;
        }

        string parent = "Assets";
        string child = "ScriptableObjects";
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    string BuildAssetPath()
    {
        return ScriptableObjectsFolder + "/" + contentId + ".asset";
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

    static LearningCategory InferCategory(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return LearningCategory.Typography;
        }

        return rawId.StartsWith("color_", System.StringComparison.OrdinalIgnoreCase)
            ? LearningCategory.Color
            : LearningCategory.Typography;
    }
}
