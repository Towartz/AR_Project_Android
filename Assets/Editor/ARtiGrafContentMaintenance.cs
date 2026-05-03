using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ARtiGraf.AR;
using ARtiGraf.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ARtiGrafContentMaintenance
{
    const string ContentLibraryPath = "Assets/Resources/ARtiGrafContentLibrary.asset";
    const string ScriptableObjectsFolder = "Assets/ScriptableObjects";
    const string ARScanScenePath = "Assets/Scenes/ARScanScene.unity";
    const string ImportedVuforiaDatabasePath = "Vuforia/MediaBelajarAR_DB.xml";
    const string ImportedVuforiaDatabaseAssetPath = "Assets/StreamingAssets/" + ImportedVuforiaDatabasePath;
    const string ReferenceImagesFolder = "Assets/Art/ReferenceImages";
    const string BarcodeMarkersFolder = "Assets/Art/BarcodeMarkers";
    const string BarcodeTargetsFolder = "Assets/Art/BarcodeTargets";
    const string BlenderPrefabsFolder = "Assets/Prefabs/BlenderDemo";
    const string StandardPrefabsFolder = "Assets/Prefabs/ARObjects";
    const string ReportFileName = "UJIKOM_Content_Report.md";
    const float DefaultTargetWidthMeters = 0.12f;

    public static string ContentLibraryAssetPath => ContentLibraryPath;
    public static string ContentReportPath
    {
        get
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, ReportFileName);
        }
    }

    sealed class ValidationIssue
    {
        public ValidationIssue(bool isError, string message)
        {
            IsError = isError;
            Message = message;
        }

        public bool IsError { get; }
        public string Message { get; }
    }

    sealed class ValidationReport
    {
        readonly List<ValidationIssue> issues = new List<ValidationIssue>();

        public int CheckedContentCount { get; set; }
        public string ReportPath { get; set; }
        public IReadOnlyList<ValidationIssue> Issues => issues;

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < issues.Count; i++)
                {
                    if (issues[i].IsError)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int WarningCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < issues.Count; i++)
                {
                    if (!issues[i].IsError)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void AddError(string message)
        {
            issues.Add(new ValidationIssue(true, message));
        }

        public void AddWarning(string message)
        {
            issues.Add(new ValidationIssue(false, message));
        }
    }

    [MenuItem("Tools/BuhenAR/Content/Sync Library From ScriptableObjects")]
    public static void SyncLibraryFromScriptableObjects()
    {
        if (!TryLoadLibrary(out MaterialContentLibrary library))
        {
            return;
        }

        List<MaterialContentData> contents = LoadAllContents();
        Undo.RecordObject(library, "Sync BuhenAR Content Library");
        library.ReplaceItems(contents);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        Debug.Log("[ARtiGraf] Content library disinkronkan. Total item: " + contents.Count);
    }

    [MenuItem("Tools/BuhenAR/Content/Validate Content Setup")]
    public static void ValidateContentSetup()
    {
        if (!TryLoadLibrary(out MaterialContentLibrary library))
        {
            return;
        }

        ValidationReport report = BuildValidationReport(library);
        WriteValidationReport(report);
        LogValidationSummary(report);
    }

    [MenuItem("Tools/BuhenAR/Content/Sync Library And Validate")]
    public static void SyncLibraryAndValidate()
    {
        SyncLibraryFromScriptableObjects();
        ValidateContentSetup();
    }

    [MenuItem("Tools/BuhenAR/Content/Sync Dashboard Runtime Setup")]
    [MenuItem("Tools/ARtiGraf/Sync Dashboard Runtime Setup")]
    public static void SyncDashboardRuntimeSetup()
    {
        SyncLibraryFromScriptableObjects();
        SyncARSceneScanTargets();
        ValidateContentSetup();
    }

    [MenuItem("Tools/BuhenAR/Content/Sync AR Scene Scan Targets")]
    [MenuItem("Tools/ARtiGraf/Sync AR Scene Scan Targets")]
    public static void SyncARSceneScanTargets()
    {
        if (!TryLoadLibrary(out MaterialContentLibrary library))
        {
            return;
        }

        List<string> targetNames = BuildImportedVuforiaTargetList(library);
        if (targetNames.Count == 0)
        {
            Debug.LogWarning("[ARtiGraf] Tidak ada target Vuforia/reference image yang bisa disync ke ARScanScene.");
        }

        Scene arScene = GetOrOpenARScanScene(out bool openedTemporarily);
        if (!arScene.IsValid())
        {
            Debug.LogError("[ARtiGraf] ARScanScene tidak bisa dibuka: " + ARScanScenePath);
            return;
        }

        ARImageTrackingController trackingController = FindTrackingControllerInScene(arScene);
        if (trackingController == null)
        {
            Debug.LogError("[ARtiGraf] ARImageTrackingController tidak ditemukan di " + ARScanScenePath);
            if (openedTemporarily)
            {
                EditorSceneManager.CloseScene(arScene, true);
            }

            return;
        }

        SerializedObject trackingObject = new SerializedObject(trackingController);
        trackingObject.FindProperty("useImportedVuforiaDeviceDatabase").boolValue = true;
        trackingObject.FindProperty("strictImportedVuforiaDeviceDatabaseOnly").boolValue = true;
        trackingObject.FindProperty("importedVuforiaDeviceDatabasePath").stringValue = ImportedVuforiaDatabasePath;
        trackingObject.FindProperty("importedVuforiaDeviceDatabaseTargets").stringValue = string.Join(",", targetNames);
        trackingObject.FindProperty("enableBarcodeFallback").boolValue = false;
        trackingObject.FindProperty("alwaysCreateBarcodeObserverOnMobile").boolValue = false;
        trackingObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(trackingController);
        EditorSceneManager.MarkSceneDirty(arScene);
        EditorSceneManager.SaveScene(arScene);

        if (openedTemporarily)
        {
            EditorSceneManager.CloseScene(arScene, true);
        }

        Debug.Log("[ARtiGraf] ARScanScene target scanner disync: " + targetNames.Count + " target.");
    }

    public static MaterialContentLibrary LoadLibrary()
    {
        TryLoadLibrary(out MaterialContentLibrary library);
        return library;
    }

    public static List<MaterialContentData> LoadAllContentsForEditor()
    {
        return LoadAllContents();
    }

    public static List<string> BuildImportedVuforiaTargetList(MaterialContentLibrary library)
    {
        List<string> databaseTargets = LoadImportedVuforiaDatabaseTargetNames();
        if (databaseTargets.Count > 0)
        {
            return databaseTargets;
        }

        var targetNames = new List<string>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        if (library != null)
        {
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData content = library.Items[i];
                if (content != null && !string.IsNullOrWhiteSpace(content.ReferenceImageName))
                {
                    AddTargetName(targetNames, seen, content.ReferenceImageName);
                }
            }
        }

        targetNames.Sort(System.StringComparer.OrdinalIgnoreCase);
        return targetNames;
    }

    public static List<string> LoadImportedVuforiaDatabaseTargetNames()
    {
        var targetNames = new List<string>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(ImportedVuforiaDatabaseAssetPath))
        {
            return targetNames;
        }

        try
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(ImportedVuforiaDatabaseAssetPath);
            XmlNodeList imageTargetNodes = xmlDocument.SelectNodes("//ImageTarget");
            if (imageTargetNodes == null)
            {
                return targetNames;
            }

            foreach (XmlNode imageTargetNode in imageTargetNodes)
            {
                string targetName = imageTargetNode?.Attributes?["name"]?.Value;
                AddTargetName(targetNames, seen, targetName);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ARtiGraf] Gagal membaca Vuforia DB XML: " + ImportedVuforiaDatabaseAssetPath + ". " + ex.Message);
        }

        targetNames.Sort(System.StringComparer.OrdinalIgnoreCase);
        return targetNames;
    }

    [MenuItem("Assets/BuhenAR/Content/Auto Configure Selected Content", true)]
    static bool CanAutoConfigureSelectedContent()
    {
        return GetSelectedContents().Count > 0;
    }

    [MenuItem("Assets/BuhenAR/Content/Auto Configure Selected Content")]
    static void AutoConfigureSelectedContent()
    {
        List<MaterialContentData> selectedContents = GetSelectedContents();
        int changedCount = 0;

        for (int i = 0; i < selectedContents.Count; i++)
        {
            if (AutoConfigureContent(selectedContents[i]))
            {
                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[ARtiGraf] Auto configure selesai. Content terubah: " + changedCount);
    }

    public static bool AutoConfigureContent(MaterialContentData content)
    {
        if (content == null)
        {
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(content);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return false;
        }

        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        SerializedObject serializedContent = new SerializedObject(content);

        SerializedProperty idProperty = serializedContent.FindProperty("id");
        SerializedProperty referenceImageProperty = serializedContent.FindProperty("referenceImageTexture");
        SerializedProperty prefabProperty = serializedContent.FindProperty("prefab");
        SerializedProperty referenceImageNameProperty = serializedContent.FindProperty("referenceImageName");
        SerializedProperty targetWidthProperty = serializedContent.FindProperty("targetWidthMeters");

        bool changed = false;

        if (string.IsNullOrWhiteSpace(idProperty.stringValue))
        {
            idProperty.stringValue = assetName;
            changed = true;
        }

        string effectiveId = string.IsNullOrWhiteSpace(idProperty.stringValue) ? assetName : idProperty.stringValue;

        if (referenceImageProperty.objectReferenceValue == null)
        {
            string matchingReferenceImagePath = FindMatchingReferenceImagePath(assetName);
            if (!string.IsNullOrWhiteSpace(matchingReferenceImagePath))
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(matchingReferenceImagePath);
                if (texture != null)
                {
                    referenceImageProperty.objectReferenceValue = texture;
                    changed = true;
                }
            }
        }

        if (prefabProperty.objectReferenceValue == null)
        {
            string matchingPrefabPath = FindMatchingPrefabPath(assetName);
            if (!string.IsNullOrWhiteSpace(matchingPrefabPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(matchingPrefabPath);
                if (prefab != null)
                {
                    prefabProperty.objectReferenceValue = prefab;
                    changed = true;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(referenceImageNameProperty.stringValue) &&
            referenceImageProperty.objectReferenceValue != null)
        {
            referenceImageNameProperty.stringValue = effectiveId;
            changed = true;
        }

        if (targetWidthProperty.floatValue <= 0f)
        {
            targetWidthProperty.floatValue = DefaultTargetWidthMeters;
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        Undo.RecordObject(content, "Auto Configure Material Content");
        serializedContent.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(content);
        return true;
    }

    public static string FindMatchingPrefabPath(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return string.Empty;
        }

        string blenderPrefabPath = BlenderPrefabsFolder + "/" + assetName + ".prefab";
        if (AssetExists<GameObject>(blenderPrefabPath))
        {
            return blenderPrefabPath;
        }

        string standardPrefabPath = StandardPrefabsFolder + "/" + assetName + ".prefab";
        if (AssetExists<GameObject>(standardPrefabPath))
        {
            return standardPrefabPath;
        }

        return string.Empty;
    }

    public static string FindMatchingReferenceImagePath(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return string.Empty;
        }

        string referenceImagePath = ReferenceImagesFolder + "/" + assetName + ".png";
        if (AssetExists<Texture2D>(referenceImagePath))
        {
            return referenceImagePath;
        }

        string markerImagePath = BarcodeMarkersFolder + "/" + assetName + "_marker.png";
        if (AssetExists<Texture2D>(markerImagePath))
        {
            return markerImagePath;
        }

        return string.Empty;
    }

    public static string FindMatchingBarcodeTargetPath(string contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return string.Empty;
        }

        string barcodeTargetPath = BarcodeTargetsFolder + "/" + contentId + ".png";
        if (AssetExists<Texture2D>(barcodeTargetPath))
        {
            return barcodeTargetPath;
        }

        return string.Empty;
    }

    static ValidationReport BuildValidationReport(MaterialContentLibrary library)
    {
        List<MaterialContentData> contents = LoadAllContents();
        List<string> vuforiaTargets = LoadImportedVuforiaDatabaseTargetNames();
        var vuforiaTargetSet = new HashSet<string>(vuforiaTargets, System.StringComparer.OrdinalIgnoreCase);
        ValidationReport report = new ValidationReport
        {
            CheckedContentCount = contents.Count
        };

        var scanKeyMap = new Dictionary<string, List<MaterialContentData>>();

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null)
            {
                continue;
            }

            ValidateSingleContent(content, library, report, scanKeyMap, vuforiaTargetSet);
        }

        ValidateDuplicateScanKeys(scanKeyMap, report);
        ValidateLibraryMembership(contents, library, report);
        return report;
    }

    static void ValidateSingleContent(
        MaterialContentData content,
        MaterialContentLibrary library,
        ValidationReport report,
        Dictionary<string, List<MaterialContentData>> scanKeyMap,
        HashSet<string> vuforiaTargetSet)
    {
        string contentLabel = BuildContentLabel(content);
        string assetPath = AssetDatabase.GetAssetPath(content);
        string assetName = Path.GetFileNameWithoutExtension(assetPath);

        if (string.IsNullOrWhiteSpace(content.Id))
        {
            report.AddError(contentLabel + " tidak punya ID.");
        }

        if (!content.HasPrefab)
        {
            report.AddError(contentLabel + " belum punya prefab.");
        }

        if (content.TargetWidthMeters <= 0f)
        {
            report.AddWarning(contentLabel + " punya targetWidthMeters <= 0. Gunakan nilai default 0.12.");
        }

        if (content.HasReferenceImage)
        {
            if (string.IsNullOrWhiteSpace(content.ReferenceImageName))
            {
                report.AddError(contentLabel + " punya reference image, tetapi referenceImageName masih kosong.");
            }
            else if (vuforiaTargetSet != null &&
                     vuforiaTargetSet.Count > 0 &&
                     !vuforiaTargetSet.Contains(content.ReferenceImageName))
            {
                report.AddWarning(contentLabel + " referenceImageName '" + content.ReferenceImageName +
                                  "' belum ada di Vuforia DB import. Marker ini akan dilewati saat mode device database strict.");
            }
        }
        else
        {
            string barcodeTargetPath = FindMatchingBarcodeTargetPath(content.Id);
            bool backedByVuforiaDb = !string.IsNullOrWhiteSpace(content.ReferenceImageName) &&
                                      vuforiaTargetSet != null &&
                                      vuforiaTargetSet.Contains(content.ReferenceImageName);
            if (string.IsNullOrWhiteSpace(barcodeTargetPath) && !backedByVuforiaDb)
            {
                report.AddWarning(contentLabel + " tidak punya reference image, target Vuforia DB, atau barcode target yang cocok.");
            }
        }

        if (assetName.StartsWith("blend_"))
        {
            string markerImagePath = BarcodeMarkersFolder + "/" + assetName + "_marker.png";
            if (!AssetExists<Texture2D>(markerImagePath))
            {
                report.AddWarning(contentLabel + " tidak menemukan marker card expected di " + markerImagePath);
            }

            string barcodeTargetPath = BarcodeTargetsFolder + "/" + assetName + ".png";
            if (!AssetExists<Texture2D>(barcodeTargetPath))
            {
                report.AddWarning(contentLabel + " tidak menemukan barcode target expected di " + barcodeTargetPath);
            }

            string blenderPrefabPath = BlenderPrefabsFolder + "/" + assetName + ".prefab";
            if (!AssetExists<GameObject>(blenderPrefabPath))
            {
                report.AddWarning(contentLabel + " tidak menemukan prefab Blender expected di " + blenderPrefabPath);
            }
        }

        if (library != null && !library.Contains(content))
        {
            report.AddError(contentLabel + " belum terdaftar di ARtiGrafContentLibrary.");
        }

        AddScanKey(scanKeyMap, content.NormalizedId, content);
        AddScanKey(scanKeyMap, content.NormalizedReferenceImageName, content);
    }

    static void ValidateDuplicateScanKeys(
        Dictionary<string, List<MaterialContentData>> scanKeyMap,
        ValidationReport report)
    {
        foreach (KeyValuePair<string, List<MaterialContentData>> pair in scanKeyMap)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value.Count <= 1)
            {
                continue;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Scan key duplicate '");
            builder.Append(pair.Key);
            builder.Append("' dipakai oleh: ");

            for (int i = 0; i < pair.Value.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(BuildContentLabel(pair.Value[i]));
            }

            report.AddError(builder.ToString());
        }
    }

    static void ValidateLibraryMembership(
        List<MaterialContentData> contents,
        MaterialContentLibrary library,
        ValidationReport report)
    {
        if (library == null)
        {
            return;
        }

        for (int i = 0; i < library.Items.Count; i++)
        {
            MaterialContentData libraryItem = library.Items[i];
            if (libraryItem == null)
            {
                report.AddWarning("ARtiGrafContentLibrary punya slot kosong di index " + i + ".");
                continue;
            }

            if (!contents.Contains(libraryItem))
            {
                report.AddWarning(
                    "ARtiGrafContentLibrary memuat item di luar folder ScriptableObjects: " +
                    BuildContentLabel(libraryItem));
            }
        }
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

    static void WriteValidationReport(ValidationReport report)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string reportPath = Path.Combine(projectRoot ?? string.Empty, ReportFileName);
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("# UJIKOM Content Report");
        builder.AppendLine();
        builder.AppendLine("- Checked content: `" + report.CheckedContentCount + "`");
        builder.AppendLine("- Errors: `" + report.ErrorCount + "`");
        builder.AppendLine("- Warnings: `" + report.WarningCount + "`");
        builder.AppendLine();

        builder.AppendLine("## Errors");
        if (report.ErrorCount == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            for (int i = 0; i < report.Issues.Count; i++)
            {
                if (report.Issues[i].IsError)
                {
                    builder.AppendLine("- " + report.Issues[i].Message);
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Warnings");
        if (report.WarningCount == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            for (int i = 0; i < report.Issues.Count; i++)
            {
                if (!report.Issues[i].IsError)
                {
                    builder.AppendLine("- " + report.Issues[i].Message);
                }
            }
        }

        File.WriteAllText(reportPath, builder.ToString());
        report.ReportPath = reportPath;
        AssetDatabase.Refresh();
    }

    static void LogValidationSummary(ValidationReport report)
    {
        StringBuilder summary = new StringBuilder();
        summary.Append("[ARtiGraf] Content validation selesai. ");
        summary.Append("Checked: ").Append(report.CheckedContentCount);
        summary.Append(", Errors: ").Append(report.ErrorCount);
        summary.Append(", Warnings: ").Append(report.WarningCount);
        summary.Append(". Report: ").Append(report.ReportPath);

        if (report.ErrorCount > 0)
        {
            Debug.LogError(summary.ToString());
            return;
        }

        if (report.WarningCount > 0)
        {
            Debug.LogWarning(summary.ToString());
            return;
        }

        Debug.Log(summary.ToString());
    }

    static bool TryLoadLibrary(out MaterialContentLibrary library)
    {
        library = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        if (library != null)
        {
            return true;
        }

        Debug.LogError("[ARtiGraf] Content library tidak ditemukan di " + ContentLibraryPath);
        return false;
    }

    static List<MaterialContentData> LoadAllContents()
    {
        string[] guids = AssetDatabase.FindAssets("t:MaterialContentData", new[] { ScriptableObjectsFolder });
        List<MaterialContentData> contents = new List<MaterialContentData>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(path);
            if (content != null)
            {
                contents.Add(content);
            }
        }

        contents.Sort(CompareContentAssets);
        return contents;
    }

    static List<MaterialContentData> GetSelectedContents()
    {
        Object[] selectedObjects = Selection.objects;
        List<MaterialContentData> selectedContents = new List<MaterialContentData>();

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            if (selectedObjects[i] is MaterialContentData content)
            {
                selectedContents.Add(content);
            }
        }

        return selectedContents;
    }

    static int CompareContentAssets(MaterialContentData left, MaterialContentData right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int categoryComparison = left.Category.CompareTo(right.Category);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        int idComparison = string.Compare(left.Id, right.Id, System.StringComparison.OrdinalIgnoreCase);
        if (idComparison != 0)
        {
            return idComparison;
        }

        return string.Compare(left.name, right.name, System.StringComparison.OrdinalIgnoreCase);
    }

    static bool AssetExists<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path) != null;
    }

    static void AddTargetName(List<string> targetNames, HashSet<string> seen, string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return;
        }

        string trimmed = targetName.Trim();
        if (seen.Contains(trimmed))
        {
            return;
        }

        seen.Add(trimmed);
        targetNames.Add(trimmed);
    }

    static Scene GetOrOpenARScanScene(out bool openedTemporarily)
    {
        openedTemporarily = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.IsValid() && loadedScene.path == ARScanScenePath)
            {
                return loadedScene;
            }
        }

        if (!File.Exists(ARScanScenePath))
        {
            return default;
        }

        openedTemporarily = true;
        return EditorSceneManager.OpenScene(ARScanScenePath, OpenSceneMode.Additive);
    }

    static ARImageTrackingController FindTrackingControllerInScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            ARImageTrackingController trackingController = roots[i].GetComponentInChildren<ARImageTrackingController>(true);
            if (trackingController != null)
            {
                return trackingController;
            }
        }

        return null;
    }

    static string BuildContentLabel(MaterialContentData content)
    {
        if (content == null)
        {
            return "(null)";
        }

        string id = string.IsNullOrWhiteSpace(content.Id) ? content.name : content.Id;
        return id + " [" + AssetDatabase.GetAssetPath(content) + "]";
    }
}
