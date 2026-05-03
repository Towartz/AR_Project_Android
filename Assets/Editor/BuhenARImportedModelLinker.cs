using System.Collections.Generic;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuhenARImportedModelLinker
{
    const string ImportedFolder = "Assets/Imported/AZModels";
    const string PrefabFolder = "Assets/Prefabs/ARObjects/AZModels";
    const string MaterialFolder = "Assets/Materials/AZModels";
    const string ScriptableFolder = "Assets/ScriptableObjects";
    const float RootScale = 0.18f;
    const float TargetVisualHeight = 1.1f;

    sealed class ModelEntry
    {
        public string Id;
        public string FbxName;
        public string PrefabName;
        public Color FallbackColor;
        public Color AccentColor;
    }

    static readonly ModelEntry[] Entries =
    {
        Entry("buaya", "Buaya", "BuayaModelPrefab", new Color32(83, 137, 59, 255), new Color32(39, 86, 44, 255)),
        Entry("ceri", "Cherry", "CeriModelPrefab", new Color32(214, 36, 60, 255), new Color32(57, 128, 55, 255)),
        Entry("domba", "Domba", "DombaModelPrefab", new Color32(245, 245, 238, 255), new Color32(92, 80, 72, 255)),
        Entry("elang", "Eagle", "ElangModelPrefab", new Color32(104, 75, 52, 255), new Color32(240, 178, 45, 255)),
        Entry("flamingo", "Flamingo", "FlamingoModelPrefab", new Color32(244, 118, 157, 255), new Color32(35, 35, 42, 255)),
        Entry("gajah", "Gajah", "GajahModelPrefab", new Color32(135, 143, 150, 255), new Color32(232, 230, 214, 255)),
        Entry("harimau", "Harimau", "HarimauModelPrefab", new Color32(226, 119, 35, 255), new Color32(28, 24, 20, 255)),
        Entry("ikan", "Ikan", "IkanModelPrefab", new Color32(49, 151, 218, 255), new Color32(255, 173, 45, 255)),
        Entry("jagung", "Jagung", "JagungModelPrefab", new Color32(244, 194, 54, 255), new Color32(57, 154, 82, 255)),
        Entry("kelinci", "Kelinci", "KelinciModelPrefab", new Color32(232, 222, 207, 255), new Color32(126, 91, 70, 255)),
        Entry("lemon", "Lemon", "LemonModelPrefab", new Color32(244, 218, 61, 255), new Color32(72, 146, 73, 255)),
        Entry("mangga", "Mangga", "ManggaModelPrefab", new Color32(242, 165, 52, 255), new Color32(61, 135, 59, 255)),
        Entry("nanas", "Nanas", "NanasModelPrefab", new Color32(232, 178, 53, 255), new Color32(56, 136, 74, 255)),
        Entry("orang_utan", "OrangUtan", "OrangUtanModelPrefab", new Color32(171, 91, 48, 255), new Color32(62, 43, 32, 255)),
        Entry("pepaya", "Pepaya", "PepayaModelPrefab", new Color32(239, 133, 48, 255), new Color32(63, 149, 82, 255)),
        Entry("quail", "Quail", "QuailModelPrefab", new Color32(148, 105, 69, 255), new Color32(224, 206, 176, 255)),
        Entry("rambutan", "Rambutan", "RambutanModelPrefab", new Color32(213, 51, 46, 255), new Color32(83, 156, 61, 255)),
        Entry("sapi", "Sapi", "SapiModelPrefab", new Color32(237, 232, 220, 255), new Color32(48, 42, 36, 255)),
        Entry("timun", "Timun", "TimunModelPrefab", new Color32(73, 159, 78, 255), new Color32(191, 225, 119, 255)),
        Entry("ular", "Ular", "UlarModelPrefab", new Color32(73, 142, 64, 255), new Color32(226, 214, 80, 255)),
        Entry("vampire_bat", "Vampire_bat", "VampireBatModelPrefab", new Color32(55, 47, 61, 255), new Color32(129, 78, 87, 255)),
        Entry("wortel", "Wortel", "WortelModelPrefab", new Color32(233, 113, 36, 255), new Color32(66, 151, 73, 255)),
        Entry("xenops", "Xenops", "XenopsModelPrefab", new Color32(185, 118, 59, 255), new Color32(235, 215, 150, 255)),
        Entry("yak", "Yak", "YakModelPrefab", new Color32(107, 73, 50, 255), new Color32(235, 223, 197, 255)),
        Entry("zebra", "Zebra", "ZebraModelPrefab", new Color32(236, 235, 226, 255), new Color32(31, 30, 32, 255)),
    };

    [MenuItem("Tools/BuhenAR/Content/Link Imported B-Z Models")]
    public static void LinkImportedModels()
    {
        EnsureFolder("Assets/Prefabs", "ARObjects");
        EnsureFolder("Assets/Prefabs/ARObjects", "AZModels");
        EnsureFolder("Assets/Materials", "AZModels");

        int linked = 0;
        var missing = new List<string>();
        for (int i = 0; i < Entries.Length; i++)
        {
            ModelEntry entry = Entries[i];
            string fbxPath = ImportedFolder + "/" + entry.FbxName + ".fbx";
            GameObject prefab = CreatePrefab(entry, fbxPath);
            if (prefab == null)
            {
                missing.Add(fbxPath);
                continue;
            }

            if (LinkContent(entry.Id, prefab))
                linked++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (missing.Count > 0)
            Debug.LogWarning("[BuhenAR] Model FBX belum ada: " + string.Join(", ", missing));
        Debug.Log("[BuhenAR] Model B-Z terhubung: " + linked + " item.");
    }

    public static void LinkAndSave()
    {
        LinkImportedModels();
    }

    static ModelEntry Entry(string id, string fbxName, string prefabName, Color color, Color accent)
    {
        return new ModelEntry
        {
            Id = id,
            FbxName = fbxName,
            PrefabName = prefabName,
            FallbackColor = color,
            AccentColor = accent
        };
    }

    static GameObject CreatePrefab(ModelEntry entry, string fbxPath)
    {
        if (!System.IO.File.Exists(fbxPath))
            return null;

        AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelAsset == null)
        {
            Debug.LogWarning("[BuhenAR] FBX tidak bisa dimuat: " + fbxPath);
            return null;
        }

        GameObject root = new GameObject(entry.PrefabName);
        root.transform.localScale = Vector3.one * RootScale;

        GameObject instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (instance == null)
        {
            Object.DestroyImmediate(root);
            return null;
        }

        instance.name = entry.FbxName;
        instance.transform.SetParent(root.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        NormalizeInstance(root.transform, instance.transform);
        ConfigureRenderers(instance, entry);
        AddCollider(root, instance);

        string prefabPath = PrefabFolder + "/" + entry.PrefabName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void NormalizeInstance(Transform root, Transform instance)
    {
        Bounds bounds = CalculateBounds(instance.gameObject);
        if (bounds.size == Vector3.zero)
            return;

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension <= 0.0001f)
            return;

        float scale = TargetVisualHeight / maxDimension;
        instance.localScale = Vector3.one * scale;

        Bounds scaledBounds = CalculateBounds(root.gameObject);
        Vector3 worldCenter = scaledBounds.center;
        Vector3 worldMin = scaledBounds.min;
        Vector3 localCenter = root.InverseTransformPoint(worldCenter);
        Vector3 localMin = root.InverseTransformPoint(worldMin);
        instance.localPosition -= new Vector3(localCenter.x, localMin.y, localCenter.z);
    }

    static Bounds CalculateBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static void ConfigureRenderers(GameObject instance, ModelEntry entry)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            bool hasVertexColors = RendererHasVertexColors(renderer);
            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material source = materials[m];
                if (HasUsableTexture(source))
                {
                    materials[m] = source;
                    continue;
                }

                if (hasVertexColors)
                {
                    materials[m] = GetOrCreateVertexColorMaterial(entry);
                    continue;
                }

                if (IsBrokenMaterial(source))
                {
                    materials[m] = GetOrCreateFallbackMaterial(entry, renderer.name, m);
                    continue;
                }

                materials[m] = GetOrCreateMaterialCopy(entry, renderer.name, m, source);
            }

            renderer.sharedMaterials = materials;
        }
    }

    static bool RendererHasVertexColors(Renderer renderer)
    {
        Mesh mesh = null;
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null)
            mesh = meshFilter.sharedMesh;
        if (mesh == null && renderer is SkinnedMeshRenderer skinned)
            mesh = skinned.sharedMesh;

        return mesh != null && mesh.colors != null && mesh.colors.Length == mesh.vertexCount;
    }

    static bool HasUsableTexture(Material material)
    {
        if (material == null) return false;
        return GetTexture(material, "_BaseMap") != null || GetTexture(material, "_MainTex") != null;
    }

    static Texture GetTexture(Material material, string property)
    {
        return material != null && material.HasProperty(property) ? material.GetTexture(property) : null;
    }

    static bool IsBrokenMaterial(Material material)
    {
        if (material == null) return true;
        Color color = GetMaterialColor(material);
        return color.a <= 0.01f || IsAlmostWhite(color) || IsAlmostBlack(color) || IsFlatNeutralPlaceholder(color);
    }

    static bool IsAlmostWhite(Color color)
    {
        return color.r > 0.92f && color.g > 0.92f && color.b > 0.92f;
    }

    static bool IsAlmostBlack(Color color)
    {
        return color.r < 0.025f && color.g < 0.025f && color.b < 0.025f;
    }

    static bool IsFlatNeutralPlaceholder(Color color)
    {
        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        float average = (color.r + color.g + color.b) / 3f;

        // Blender exports sometimes arrive as flat grey/white materials instead of
        // the authored viewport color. Treat those as missing texture/material data.
        return max - min < 0.04f && average > 0.82f;
    }

    static Color GetMaterialColor(Material material)
    {
        if (material == null) return Color.white;
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }

    static Material GetOrCreateVertexColorMaterial(ModelEntry entry)
    {
        string assetPath = MaterialFolder + "/" + entry.Id + "_vertex_color.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            Shader shader = Shader.Find("BuhenAR/Vertex Color Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }

        ApplyColor(material, Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material GetOrCreateFallbackMaterial(ModelEntry entry, string rendererName, int materialIndex)
    {
        string safeRenderer = Sanitize(rendererName);
        string assetPath = MaterialFolder + "/" + entry.Id + "_" + safeRenderer + "_" + materialIndex + "_fallback.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(ResolveLitShader());
            AssetDatabase.CreateAsset(material, assetPath);
        }

        ApplyColor(material, ChooseFallbackColor(entry, rendererName, materialIndex));
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material GetOrCreateMaterialCopy(ModelEntry entry, string rendererName, int materialIndex, Material source)
    {
        string safeRenderer = Sanitize(rendererName);
        string assetPath = MaterialFolder + "/" + entry.Id + "_" + safeRenderer + "_" + materialIndex + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(ResolveLitShader());
            AssetDatabase.CreateAsset(material, assetPath);
        }

        ApplyColor(material, GetMaterialColor(source));
        Texture texture = GetTexture(source, "_BaseMap") ?? GetTexture(source, "_MainTex");
        if (texture != null)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Shader ResolveLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return shader;
    }

    static void ApplyColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
    }

    static Color ChooseFallbackColor(ModelEntry entry, string rendererName, int materialIndex)
    {
        string key = rendererName != null ? rendererName.ToLowerInvariant() : string.Empty;

        if (key.Contains("eye") || key.Contains("pupil") || key.Contains("stripe") || key.Contains("belang"))
            return entry.AccentColor;
        if (key.Contains("leaf") || key.Contains("daun") || key.Contains("stem") || key.Contains("tangkai"))
            return entry.AccentColor;
        if (key.Contains("beak") || key.Contains("paruh") || key.Contains("claw") || key.Contains("cakar"))
            return entry.AccentColor;
        if (materialIndex % 3 == 1)
            return Color.Lerp(entry.FallbackColor, entry.AccentColor, 0.55f);
        if (materialIndex % 3 == 2)
            return Color.Lerp(entry.FallbackColor, Color.white, 0.22f);

        return entry.FallbackColor;
    }

    static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "renderer";
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        }

        return new string(chars);
    }

    static void AddCollider(GameObject root, GameObject instance)
    {
        Bounds bounds = CalculateBounds(root);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null) collider = root.AddComponent<BoxCollider>();
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        Vector3 localMin = root.transform.InverseTransformPoint(bounds.min);
        Vector3 localMax = root.transform.InverseTransformPoint(bounds.max);
        collider.size = new Vector3(
            Mathf.Abs(localMax.x - localMin.x),
            Mathf.Abs(localMax.y - localMin.y),
            Mathf.Abs(localMax.z - localMin.z));

        if (collider.size.sqrMagnitude <= 0.0001f)
        {
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = Vector3.one;
        }

        _ = instance;
    }

    static bool LinkContent(string id, GameObject prefab)
    {
        string path = ScriptableFolder + "/" + id + ".asset";
        MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(path);
        if (content == null)
        {
            Debug.LogWarning("[BuhenAR] Content asset tidak ditemukan: " + path);
            return false;
        }

        SerializedObject serialized = new SerializedObject(content);
        serialized.FindProperty("prefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(content);
        return true;
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }
}
