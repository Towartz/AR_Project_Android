using System.Collections.Generic;
using System.IO;
using ARtiGraf.AR;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

public static class GenerateBlenderDemoAssets
{
    const int MarkerTextureSize = 512;
    const string ContentLibraryPath = "Assets/Resources/ARtiGrafContentLibrary.asset";
    const string ModelFolder = "Assets/Imported/BlenderDemo";
    const string PrefabFolder = "Assets/Prefabs/BlenderDemo";
    const string MaterialFolder = "Assets/Materials/BlenderDemo";
    const string BarcodeFolder = "Assets/Art/BarcodeTargets";
    const string MarkerFolder = "Assets/Art/BarcodeMarkers";
    const string ScriptableObjectsFolder = "Assets/ScriptableObjects";
    static readonly Vector3 DefaultTiltEuler = new Vector3(-18f, 28f, 0f);

    struct DemoShape
    {
        public DemoShape(string id, string title, string subtitle, string objectType, Color color, Vector3 scale, LearningCategory category)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            ObjectType = objectType;
            Color = color;
            Scale = scale;
            Category = category;
        }

        public string Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string ObjectType { get; }
        public Color Color { get; }
        public Vector3 Scale { get; }
        public LearningCategory Category { get; }
    }

    static readonly DemoShape[] DemoShapes =
    {
        new DemoShape("blend_cube", "Blender Cube", "Cube dasar untuk demo AR", "Cube", new Color32(37, 99, 235, 255), new Vector3(0.05f, 0.05f, 0.05f), LearningCategory.Typography),
        new DemoShape("blend_roundcube", "Blender Round Cube", "Cube bevel untuk demo AR", "Round Cube", new Color32(8, 145, 178, 255), new Vector3(0.055f, 0.055f, 0.055f), LearningCategory.Typography),
        new DemoShape("blend_apple", "Blender Apple", "Model apel dari Blender untuk demo AR", "Apple", new Color32(220, 38, 38, 255), new Vector3(0.065f, 0.065f, 0.065f), LearningCategory.Typography),
        new DemoShape("blend_pyramid", "Blender Pyramid", "Pyramid sederhana untuk demo AR", "Pyramid", new Color32(234, 88, 12, 255), new Vector3(0.06f, 0.06f, 0.06f), LearningCategory.Color),
        new DemoShape("blend_torus", "Blender Torus", "Torus untuk demo AR", "Torus", new Color32(147, 51, 234, 255), new Vector3(0.05f, 0.05f, 0.05f), LearningCategory.Color),
        new DemoShape("blend_cylinder", "Blender Cylinder", "Cylinder dasar untuk demo AR", "Cylinder", new Color32(22, 163, 74, 255), new Vector3(0.055f, 0.065f, 0.055f), LearningCategory.Color),
    };

    [MenuItem("Tools/BuhenAR/Generate Blender Demo Assets")]
    public static void Generate()
    {
        MaterialContentLibrary library = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        if (library == null)
        {
            throw new System.InvalidOperationException("Content library belum ditemukan.");
        }

        EnsureFolder("Assets", "Imported");
        EnsureFolder("Assets/Imported", "BlenderDemo");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "BlenderDemo");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "BlenderDemo");
        EnsureFolder("Assets/Art", "BarcodeTargets");
        EnsureFolder("Assets/Art", "BarcodeMarkers");

        AssetDatabase.Refresh();

        SerializedObject libraryObject = new SerializedObject(library);
        SerializedProperty itemsProperty = libraryObject.FindProperty("items");

        for (int i = 0; i < DemoShapes.Length; i++)
        {
            DemoShape shape = DemoShapes[i];
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(shape));
            if (modelAsset == null)
            {
                throw new System.InvalidOperationException("Model Blender belum ditemukan: " + ModelPath(shape));
            }

            Texture2D barcodeTexture = LoadReadableTrackingTexture(BarcodePath(shape), MarkerTextureSize);
            Texture2D markerTexture = CreateOrUpdateMarker(shape, barcodeTexture);

            Material material = CreateOrUpdateMaterial(shape);
            GameObject prefab = CreateOrUpdatePrefab(shape, modelAsset, material);
            MaterialContentData content = CreateOrUpdateContent(shape, prefab, markerTexture);
            AddToLibraryIfMissing(itemsProperty, content);
        }

        libraryObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Blender demo assets generated.");
    }

    static string ModelPath(DemoShape shape)
    {
        return ModelFolder + "/" + shape.Id + ".fbx";
    }

    static string MaterialPath(DemoShape shape)
    {
        return MaterialFolder + "/" + shape.Id + ".mat";
    }

    static string PrefabPath(DemoShape shape)
    {
        return PrefabFolder + "/" + shape.Id + ".prefab";
    }

    static string ContentPath(DemoShape shape)
    {
        return ScriptableObjectsFolder + "/" + shape.Id + ".asset";
    }

    static string BarcodePath(DemoShape shape)
    {
        return BarcodeFolder + "/" + shape.Id + ".png";
    }

    static string MarkerPath(DemoShape shape)
    {
        return MarkerFolder + "/" + shape.Id + "_marker.png";
    }

    static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static Material CreateOrUpdateMaterial(DemoShape shape)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(shape));
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath(shape));
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", shape.Color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", shape.Color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Texture2D CreateOrUpdateMarker(DemoShape shape, Texture2D barcodeTexture)
    {
        if (barcodeTexture == null)
        {
            throw new System.InvalidOperationException("Barcode texture belum ditemukan: " + BarcodePath(shape));
        }

        Texture2D markerTexture = BuildMarkerTexture(shape, barcodeTexture);
        byte[] bytes = markerTexture.EncodeToPNG();
        File.WriteAllBytes(MarkerPath(shape), bytes);
        Object.DestroyImmediate(markerTexture);

        AssetDatabase.ImportAsset(MarkerPath(shape), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(MarkerPath(shape)) as TextureImporter;
        if (importer != null)
        {
            ConfigureTrackingTextureImporter(importer, MarkerTextureSize);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath(shape));
    }

    static Texture2D LoadReadableTrackingTexture(string assetPath, int maxTextureSize)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool requiresReimport =
                !importer.isReadable ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.mipmapEnabled ||
                importer.streamingMipmaps ||
                importer.npotScale != TextureImporterNPOTScale.None ||
                importer.maxTextureSize != maxTextureSize;

            if (requiresReimport)
            {
                ConfigureTrackingTextureImporter(importer, maxTextureSize);
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    static Texture2D BuildMarkerTexture(DemoShape shape, Texture2D barcodeTexture)
    {
        const int size = MarkerTextureSize;
        int Scale(int value)
        {
            return Mathf.Max(1, Mathf.RoundToInt(value * (size / 1024f)));
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        int markerHash = ComputeMarkerHash(shape.Id);
        Color background = new Color(0.985f, 0.982f, 0.972f, 1f);
        Color accent = shape.Color;
        Color accentSoft = Color.Lerp(accent, Color.white, 0.72f);
        Color accentMist = Color.Lerp(accent, background, 0.88f);
        Color accentDeep = Color.Lerp(accent, Color.black, 0.3f);
        Color accentNight = Color.Lerp(accentDeep, new Color(0.04f, 0.06f, 0.1f, 1f), 0.55f);
        Color ink = new Color(0.08f, 0.09f, 0.12f, 1f);
        Color guide = Color.Lerp(ink, background, 0.86f);
        Color panel = Color.Lerp(Color.white, accentMist, 0.24f);

        FillVerticalGradient(texture, 0, 0, size, size, background, Color.Lerp(background, accentMist, 0.65f));
        DrawInsetFrame(texture, Scale(26), Scale(26), size - Scale(52), size - Scale(52), Scale(18), ink);
        DrawRoundedRect(texture, Scale(44), Scale(44), size - Scale(88), size - Scale(88), Scale(38), panel);
        DrawTopBanner(texture, shape, accent, accentDeep, accentNight, guide, Scale);
        DrawBottomInfoBand(texture, shape, accent, accentSoft, accentDeep, ink, guide, markerHash, Scale);
        DrawSideRail(texture, true, markerHash, accent, accentDeep, guide, ink, Scale);
        DrawSideRail(texture, false, markerHash, accent, accentDeep, guide, ink, Scale);
        DrawHeroBadge(texture, shape, accent, accentSoft, accentDeep, ink, markerHash, Scale);
        DrawDecorativeOrbit(texture, Scale(844), Scale(862), Scale(118), accentMist, accentSoft, Scale);
        DrawDecorativeOrbit(texture, Scale(176), Scale(250), Scale(84), accentSoft, guide, Scale);

        int qrSize = Scale(470);
        int qrX = (size - qrSize) / 2;
        int qrY = (size - qrSize) / 2 - Scale(6);

        DrawRoundedRect(texture, qrX - Scale(46), qrY - Scale(46), qrSize + Scale(92), qrSize + Scale(92), Scale(34), Color.white);
        DrawRoundedRectOutline(texture, qrX - Scale(46), qrY - Scale(46), qrSize + Scale(92), qrSize + Scale(92), Scale(34), Scale(12), accentNight);
        DrawRoundedRectOutline(texture, qrX - Scale(24), qrY - Scale(24), qrSize + Scale(48), qrSize + Scale(48), Scale(24), Scale(8), accent);
        FillRect(texture, qrX - Scale(60), qrY - Scale(60), qrSize + Scale(120), Scale(16), ink);
        FillRect(texture, qrX - Scale(60), qrY + qrSize + Scale(44), qrSize + Scale(120), Scale(16), accent);
        FillRect(texture, qrX - Scale(60), qrY - Scale(44), Scale(16), qrSize + Scale(88), accent);
        FillRect(texture, qrX + qrSize + Scale(44), qrY - Scale(44), Scale(16), qrSize + Scale(88), ink);
        BlitTexture(texture, barcodeTexture, qrX, qrY, qrSize, qrSize);

        DrawCornerL(texture, qrX - Scale(82), qrY + qrSize + Scale(70), Scale(110), Scale(18), ink);
        DrawCornerL(texture, qrX + qrSize - Scale(28), qrY - Scale(84), Scale(108), Scale(18), accent);
        DrawCornerL(texture, qrX - Scale(82), qrY - Scale(12), Scale(76), Scale(14), accentDeep);
        DrawPayloadSignature(texture, markerHash, qrX, qrY, qrSize, accentDeep, ink, Scale);
        DrawPayloadTicks(texture, markerHash, qrX, qrY, qrSize, accent, accentDeep, guide, Scale);

        texture.Apply();
        return texture;
    }

    static void ConfigureTrackingTextureImporter(TextureImporter importer, int maxTextureSize)
    {
        importer.textureType = TextureImporterType.Default;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = false;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.streamingMipmaps = false;
        importer.maxTextureSize = maxTextureSize;
        importer.npotScale = TextureImporterNPOTScale.None;
    }

    static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);
        int minX = Mathf.Max(0, x);
        int minY = Mathf.Max(0, y);

        for (int py = minY; py < maxY; py++)
        {
            for (int px = minX; px < maxX; px++)
            {
                texture.SetPixel(px, py, color);
            }
        }
    }

    static void FillVerticalGradient(Texture2D texture, int x, int y, int width, int height, Color topColor, Color bottomColor)
    {
        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);
        int minX = Mathf.Max(0, x);
        int minY = Mathf.Max(0, y);

        for (int py = minY; py < maxY; py++)
        {
            float t = height <= 1 ? 0f : (py - y) / (float)Mathf.Max(1, height - 1);
            Color color = Color.Lerp(bottomColor, topColor, t);
            for (int px = minX; px < maxX; px++)
            {
                texture.SetPixel(px, py, color);
            }
        }
    }

    static void DrawRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color color)
    {
        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);
        int minX = Mathf.Max(0, x);
        int minY = Mathf.Max(0, y);
        int clampedRadius = Mathf.Max(0, Mathf.Min(radius, Mathf.Min(width, height) / 2));
        int radiusSquared = clampedRadius * clampedRadius;

        for (int py = minY; py < maxY; py++)
        {
            for (int px = minX; px < maxX; px++)
            {
                if (IsInsideRoundedRect(px, py, x, y, width, height, clampedRadius, radiusSquared))
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }

    static void DrawRoundedRectOutline(Texture2D texture, int x, int y, int width, int height, int radius, int thickness, Color color)
    {
        DrawRoundedRect(texture, x, y, width, height, radius, color);
        int innerX = x + thickness;
        int innerY = y + thickness;
        int innerWidth = width - (thickness * 2);
        int innerHeight = height - (thickness * 2);

        if (innerWidth <= 0 || innerHeight <= 0)
        {
            return;
        }

        DrawRoundedRect(texture, innerX, innerY, innerWidth, innerHeight, Mathf.Max(0, radius - thickness), Color.clear);

        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);
        int minX = Mathf.Max(0, x);
        int minY = Mathf.Max(0, y);
        int outerRadius = Mathf.Max(0, Mathf.Min(radius, Mathf.Min(width, height) / 2));
        int innerRadius = Mathf.Max(0, Mathf.Min(radius - thickness, Mathf.Min(innerWidth, innerHeight) / 2));
        int outerRadiusSquared = outerRadius * outerRadius;
        int innerRadiusSquared = innerRadius * innerRadius;

        for (int py = minY; py < maxY; py++)
        {
            for (int px = minX; px < maxX; px++)
            {
                bool inOuter = IsInsideRoundedRect(px, py, x, y, width, height, outerRadius, outerRadiusSquared);
                bool inInner = IsInsideRoundedRect(px, py, innerX, innerY, innerWidth, innerHeight, innerRadius, innerRadiusSquared);
                if (inOuter && !inInner)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }

    static bool IsInsideRoundedRect(int px, int py, int x, int y, int width, int height, int radius, int radiusSquared)
    {
        if (radius <= 0)
        {
            return px >= x && px < x + width && py >= y && py < y + height;
        }

        int left = x;
        int right = x + width - 1;
        int bottom = y;
        int top = y + height - 1;

        if (px >= left + radius && px <= right - radius)
        {
            return py >= bottom && py <= top;
        }

        if (py >= bottom + radius && py <= top - radius)
        {
            return px >= left && px <= right;
        }

        int cornerCenterX = px < left + radius ? left + radius : right - radius;
        int cornerCenterY = py < bottom + radius ? bottom + radius : top - radius;
        int dx = px - cornerCenterX;
        int dy = py - cornerCenterY;
        return (dx * dx) + (dy * dy) <= radiusSquared;
    }

    static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if ((x * x) + (y * y) <= radiusSquared)
                {
                    int px = centerX + x;
                    int py = centerY + y;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    static void DrawRing(Texture2D texture, int centerX, int centerY, int outerRadius, int innerRadius, Color color)
    {
        int outerSquared = outerRadius * outerRadius;
        int innerSquared = innerRadius * innerRadius;

        for (int y = -outerRadius; y <= outerRadius; y++)
        {
            for (int x = -outerRadius; x <= outerRadius; x++)
            {
                int distanceSquared = (x * x) + (y * y);
                if (distanceSquared > outerSquared || distanceSquared < innerSquared)
                {
                    continue;
                }

                int px = centerX + x;
                int py = centerY + y;
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }

    static void DrawCornerL(Texture2D texture, int x, int y, int length, int thickness, Color color)
    {
        FillRect(texture, x, y, length, thickness, color);
        FillRect(texture, x, y - length + thickness, thickness, length, color);
    }

    static void DrawInsetFrame(Texture2D texture, int x, int y, int width, int height, int thickness, Color color)
    {
        int radius = Mathf.Max(12, thickness * 2);
        DrawRoundedRectOutline(texture, x, y, width, height, radius, thickness, color);

        Color softColor = Color.Lerp(color, Color.white, 0.72f);
        int inset = Mathf.Max(4, thickness / 2);
        int innerThickness = Mathf.Max(2, thickness / 3);
        DrawRoundedRectOutline(
            texture,
            x + inset,
            y + inset,
            width - (inset * 2),
            height - (inset * 2),
            Mathf.Max(8, radius - inset),
            innerThickness,
            softColor);
    }

    static void DrawTopBanner(Texture2D texture, DemoShape shape, Color accent, Color accentDeep, Color accentNight, Color guide, System.Func<int, int> scale)
    {
        int margin = scale(72);
        int bannerY = texture.height - scale(152);
        int bannerHeight = scale(64);
        int badgeWidth = scale(214);
        int accentWidth = scale(148);

        DrawRoundedRect(texture, margin, bannerY, texture.width - (margin * 2), bannerHeight, scale(26), accentNight);
        DrawRoundedRect(texture, margin + scale(14), bannerY + scale(12), badgeWidth, bannerHeight - scale(24), scale(20), accent);
        DrawRoundedRect(texture, texture.width - margin - accentWidth, bannerY + scale(12), accentWidth, bannerHeight - scale(24), scale(18), accentDeep);

        int dataStartX = margin + badgeWidth + scale(42);
        for (int i = 0; i < 4; i++)
        {
            int segmentWidth = scale(46 + ((ComputeMarkerHash(shape.Id) >> i) & 0x1F));
            FillRect(texture, dataStartX + (i * scale(66)), bannerY + scale(24), segmentWidth, scale(8), guide);
            FillRect(texture, dataStartX + (i * scale(66)), bannerY + scale(38), Mathf.Max(scale(22), segmentWidth - scale(18)), scale(8), Color.white);
        }
    }

    static void DrawBottomInfoBand(Texture2D texture, DemoShape shape, Color accent, Color accentSoft, Color accentDeep, Color ink, Color guide, int markerHash, System.Func<int, int> scale)
    {
        int x = scale(72);
        int y = scale(64);
        int width = texture.width - scale(144);
        int height = scale(138);

        DrawRoundedRect(texture, x, y, width, height, scale(28), Color.Lerp(Color.white, accentSoft, 0.22f));
        DrawRoundedRectOutline(texture, x, y, width, height, scale(28), scale(10), accentDeep);

        int chipCount = 5;
        int chipWidth = scale(86);
        int chipHeight = scale(34);
        int chipGap = scale(18);
        int chipStartX = x + scale(26);
        int chipY = y + height - scale(50);

        for (int i = 0; i < chipCount; i++)
        {
            Color chipColor = ((markerHash >> i) & 1) == 0 ? accentDeep : accent;
            DrawRoundedRect(texture, chipStartX + (i * (chipWidth + chipGap)), chipY, chipWidth, chipHeight, scale(14), chipColor);
        }

        int lineX = x + scale(28);
        int lineY = y + scale(78);
        int[] lineWidths =
        {
            scale(210),
            scale(158),
            scale(188)
        };

        for (int i = 0; i < lineWidths.Length; i++)
        {
            FillRect(texture, lineX, lineY + (i * scale(18)), lineWidths[i], scale(8), i == 0 ? ink : guide);
        }

        int meterWidth = scale(180);
        int meterX = x + width - meterWidth - scale(26);
        int meterY = y + scale(34);
        DrawRoundedRect(texture, meterX, meterY, meterWidth, scale(24), scale(12), guide);
        FillRect(texture, meterX + scale(8), meterY + scale(6), Mathf.Clamp((markerHash % meterWidth), scale(48), meterWidth - scale(16)), scale(12), accent);
    }

    static void DrawSideRail(Texture2D texture, bool leftSide, int markerHash, Color accent, Color accentDeep, Color guide, Color ink, System.Func<int, int> scale)
    {
        int railWidth = scale(54);
        int x = leftSide ? scale(44) : texture.width - scale(98);
        int y = scale(194);
        int height = texture.height - scale(388);

        DrawRoundedRect(texture, x, y, railWidth, height, scale(24), ink);
        for (int i = 0; i < 10; i++)
        {
            int segmentY = y + scale(28) + (i * scale(52));
            Color segmentColor = ((markerHash >> (i % 14)) & 1) == 0
                ? (leftSide ? accent : accentDeep)
                : guide;
            DrawRoundedRect(texture, x + scale(12), segmentY, railWidth - scale(24), scale(26), scale(12), segmentColor);
        }
    }

    static void DrawHeroBadge(Texture2D texture, DemoShape shape, Color accent, Color accentSoft, Color accentDeep, Color ink, int markerHash, System.Func<int, int> scale)
    {
        int badgeX = scale(74);
        int badgeY = texture.height - scale(346);
        int badgeWidth = scale(212);
        int badgeHeight = scale(212);

        DrawRoundedRect(texture, badgeX, badgeY, badgeWidth, badgeHeight, scale(42), accent);
        DrawRoundedRect(texture, badgeX + scale(18), badgeY + scale(18), badgeWidth - scale(36), badgeHeight - scale(36), scale(34), Color.Lerp(accentSoft, Color.white, 0.24f));
        DrawShapeGlyph(texture, shape, badgeX + (badgeWidth / 2), badgeY + (badgeHeight / 2), scale(60), ink, accentDeep);

        int dotRadius = scale(8) + (markerHash % scale(10));
        DrawCircle(texture, badgeX + badgeWidth - scale(26), badgeY + scale(28), dotRadius, accentDeep);
        DrawRing(texture, badgeX + scale(36), badgeY + badgeHeight - scale(38), scale(16), scale(8), ink);
    }

    static void DrawDecorativeOrbit(Texture2D texture, int centerX, int centerY, int radius, Color orbitColor, Color dotColor, System.Func<int, int> scale)
    {
        DrawRing(texture, centerX, centerY, radius, Mathf.Max(scale(6), radius - scale(8)), orbitColor);
        DrawCircle(texture, centerX + (radius / 2), centerY + (radius / 3), scale(12), dotColor);
        DrawCircle(texture, centerX - (radius / 3), centerY - (radius / 2), scale(9), dotColor);
    }

    static void DrawHashGuides(Texture2D texture, int markerHash, Color accent, Color accentDeep, Color guide, System.Func<int, int> scale)
    {
        int leftX = scale(64);
        int rightX = texture.width - scale(132);
        int topY = texture.height - scale(122);
        int bottomY = scale(150);
        int cell = scale(26);

        for (int i = 0; i < 6; i++)
        {
            Color leftColor = ((markerHash >> i) & 1) == 0 ? accentDeep : guide;
            Color rightColor = ((markerHash >> (i + 6)) & 1) == 0 ? accent : guide;
            FillRect(texture, leftX, bottomY + (i * cell), scale(46), scale(12), leftColor);
            FillRect(texture, rightX, topY - (i * cell), scale(52), scale(12), rightColor);
        }

        int topStartX = scale(320);
        for (int i = 0; i < 7; i++)
        {
            Color barColor = ((markerHash >> (i + 2)) & 1) == 0 ? accent : accentDeep;
            FillRect(texture, topStartX + (i * scale(28)), texture.height - scale(92), scale(14), scale(54), barColor);
        }
    }

    static void DrawCornerBlocks(Texture2D texture, int markerHash, Color accent, Color accentDeep, Color ink, System.Func<int, int> scale)
    {
        int cell = scale(28);
        int block = scale(18);
        int[,] corners =
        {
            { scale(92), texture.height - scale(282) },
            { texture.width - scale(210), texture.height - scale(282) },
            { scale(92), scale(90) },
            { texture.width - scale(210), scale(90) }
        };

        for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    int bitIndex = (cornerIndex * 4) + x + y;
                    if (((markerHash >> (bitIndex % 15)) & 1) == 0)
                    {
                        continue;
                    }

                    Color color = (x + y + cornerIndex) % 3 == 0 ? accent : ((x + y) % 2 == 0 ? accentDeep : ink);
                    FillRect(texture, corners[cornerIndex, 0] + (x * cell), corners[cornerIndex, 1] + (y * cell), block, block, color);
                }
            }
        }
    }

    static void DrawPayloadSignature(Texture2D texture, int markerHash, int qrX, int qrY, int qrSize, Color accentDeep, Color ink, System.Func<int, int> scale)
    {
        int cell = scale(18);
        int signatureWidth = scale(12);
        int signatureHeight = scale(44);
        int startX = qrX + scale(42);
        int topY = qrY + qrSize + scale(52);
        int bottomY = qrY - scale(78);

        for (int i = 0; i < 6; i++)
        {
            Color topColor = ((markerHash >> i) & 1) == 0 ? accentDeep : ink;
            Color bottomColor = ((markerHash >> (i + 6)) & 1) == 0 ? ink : accentDeep;
            FillRect(texture, startX + (i * cell), topY, signatureWidth, signatureHeight, topColor);
            FillRect(texture, startX + (i * cell), bottomY, signatureWidth, signatureHeight, bottomColor);
        }
    }

    static void DrawPayloadTicks(Texture2D texture, int markerHash, int qrX, int qrY, int qrSize, Color accent, Color accentDeep, Color guide, System.Func<int, int> scale)
    {
        int leftX = qrX - scale(78);
        int rightX = qrX + qrSize + scale(48);
        int startY = qrY + scale(26);

        for (int i = 0; i < 7; i++)
        {
            int tickHeight = ((markerHash >> (i + 4)) & 1) == 0 ? scale(20) : scale(34);
            Color tickColor = i % 2 == 0 ? accent : accentDeep;
            DrawRoundedRect(texture, leftX, startY + (i * scale(58)), scale(18), tickHeight, scale(8), tickColor);
            DrawRoundedRect(texture, rightX, startY + (i * scale(58)), scale(18), tickHeight, scale(8), i % 3 == 0 ? guide : tickColor);
        }
    }

    static int ComputeMarkerHash(string value)
    {
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
            {
                hash = (hash * 31) + value[i];
            }

            return hash & int.MaxValue;
        }
    }

    static void BlitTexture(Texture2D target, Texture2D source, int x, int y, int width, int height)
    {
        for (int py = 0; py < height; py++)
        {
            float v = py / (float)(height - 1);
            for (int px = 0; px < width; px++)
            {
                float u = px / (float)(width - 1);
                Color color = source.GetPixelBilinear(u, v);
                target.SetPixel(x + px, y + py, color);
            }
        }
    }

    static void DrawShapeGlyph(Texture2D texture, DemoShape shape, int centerX, int centerY, int size, Color primary, Color secondary)
    {
        switch (shape.Id)
        {
            case "blend_cube":
                DrawRoundedRect(texture, centerX - size / 2, centerY - size / 2, size, size, Mathf.Max(6, size / 7), primary);
                DrawRoundedRectOutline(texture, centerX - size / 2 + size / 6, centerY - size / 2 + size / 6, size, size, Mathf.Max(6, size / 7), Mathf.Max(4, size / 10), secondary);
                break;
            case "blend_roundcube":
                DrawRoundedRect(texture, centerX - size / 2, centerY - size / 2, size, size, Mathf.Max(10, size / 3), primary);
                DrawRoundedRectOutline(texture, centerX - size / 2 + size / 6, centerY - size / 2 + size / 6, size, size, Mathf.Max(8, size / 3), Mathf.Max(4, size / 10), secondary);
                break;
            case "blend_apple":
                DrawCircle(texture, centerX - size / 5, centerY, size / 3, primary);
                DrawCircle(texture, centerX + size / 5, centerY, size / 3, primary);
                DrawCircle(texture, centerX, centerY - size / 4, size / 4, primary);
                DrawRoundedRect(texture, centerX - size / 10, centerY + size / 5, size / 5, size / 4, Mathf.Max(4, size / 10), secondary);
                DrawRoundedRect(texture, centerX + size / 10, centerY + size / 4, size / 3, size / 8, Mathf.Max(3, size / 12), secondary);
                break;
            case "blend_pyramid":
                DrawTriangle(texture, new Vector2Int(centerX, centerY + size / 2), new Vector2Int(centerX - size / 2, centerY - size / 2), new Vector2Int(centerX + size / 2, centerY - size / 2), primary);
                DrawTriangle(texture, new Vector2Int(centerX, centerY + size / 2), new Vector2Int(centerX, centerY - size / 6), new Vector2Int(centerX + size / 2, centerY - size / 2), secondary);
                break;
            case "blend_torus":
                DrawRing(texture, centerX, centerY, size / 2, Mathf.Max(2, size / 4), primary);
                DrawRing(texture, centerX + size / 10, centerY + size / 10, Mathf.Max(6, size / 4), Mathf.Max(2, size / 6), secondary);
                break;
            case "blend_cylinder":
                DrawRoundedRect(texture, centerX - size / 3, centerY - size / 2, (size * 2) / 3, size, Mathf.Max(8, size / 6), primary);
                DrawRing(texture, centerX, centerY + size / 2 - size / 8, size / 3, Mathf.Max(2, size / 4), secondary);
                DrawRing(texture, centerX, centerY - size / 2 + size / 8, size / 3, Mathf.Max(2, size / 4), secondary);
                break;
            default:
                DrawRing(texture, centerX, centerY, size / 2, Mathf.Max(2, size / 4), primary);
                break;
        }
    }

    static void DrawTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
    {
        int minX = Mathf.Max(0, Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
        int maxX = Mathf.Min(texture.width - 1, Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
        int minY = Mathf.Max(0, Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
        int maxY = Mathf.Min(texture.height - 1, Mathf.Max(a.y, Mathf.Max(b.y, c.y)));

        float area = Edge(a, b, c);
        if (Mathf.Approximately(area, 0f))
        {
            return;
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                float w0 = Edge(b, c, p);
                float w1 = Edge(c, a, p);
                float w2 = Edge(a, b, p);
                bool hasNeg = w0 < 0 || w1 < 0 || w2 < 0;
                bool hasPos = w0 > 0 || w1 > 0 || w2 > 0;
                if (!(hasNeg && hasPos))
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    static float Edge(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        return ((c.x - a.x) * (b.y - a.y)) - ((c.y - a.y) * (b.x - a.x));
    }

    static GameObject CreateOrUpdatePrefab(DemoShape shape, GameObject modelAsset, Material material)
    {
        GameObject root = new GameObject(shape.Title.Replace(" ", string.Empty));
        DemoSpinObject spin = root.AddComponent<DemoSpinObject>();
        SerializedObject spinObject = new SerializedObject(spin);
        spinObject.FindProperty("eulerDegreesPerSecond").vector3Value = new Vector3(0f, 20f, 0f);
        spinObject.ApplyModifiedPropertiesWithoutUndo();
        root.transform.localRotation = Quaternion.Euler(DefaultTiltEuler);

        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (modelInstance == null)
        {
            Object.DestroyImmediate(root);
            throw new System.InvalidOperationException("Gagal instantiate model: " + modelAsset.name);
        }

        modelInstance.name = "Model";
        modelInstance.transform.SetParent(root.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] materials = renderer.sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        FitModelToMarker(shape, root, modelInstance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(shape));
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void FitModelToMarker(DemoShape shape, GameObject root, GameObject modelInstance)
    {
        Bounds rawBounds = CalculateLocalBounds(root.transform, modelInstance.GetComponentsInChildren<Renderer>(true));
        float maxDimension = Mathf.Max(rawBounds.size.x, Mathf.Max(rawBounds.size.y, rawBounds.size.z));
        if (maxDimension <= 0.0001f)
        {
            modelInstance.transform.localScale = shape.Scale;
            return;
        }

        float targetMaxDimension = Mathf.Max(shape.Scale.x, Mathf.Max(shape.Scale.y, shape.Scale.z));
        float fitScale = targetMaxDimension / maxDimension;
        modelInstance.transform.localScale = Vector3.one * fitScale;

        Bounds fittedBounds = CalculateLocalBounds(root.transform, modelInstance.GetComponentsInChildren<Renderer>(true));
        modelInstance.transform.localPosition = new Vector3(
            -fittedBounds.center.x,
            -fittedBounds.min.y + 0.004f,
            -fittedBounds.center.z);
    }

    static Bounds CalculateLocalBounds(Transform root, Renderer[] renderers)
    {
        Matrix4x4 worldToLocal = root.worldToLocalMatrix;
        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            Vector3 center = rendererBounds.center;
            Vector3 extents = rendererBounds.extents;
            Vector3[] corners =
            {
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z),
            };

            for (int j = 0; j < corners.Length; j++)
            {
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corners[j]);
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
    }

    static MaterialContentData CreateOrUpdateContent(DemoShape shape, GameObject prefab, Texture2D referenceTexture)
    {
        MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(ContentPath(shape));
        if (content == null)
        {
            content = ScriptableObject.CreateInstance<MaterialContentData>();
            AssetDatabase.CreateAsset(content, ContentPath(shape));
        }

        SerializedObject contentObject = new SerializedObject(content);
        contentObject.FindProperty("id").stringValue = shape.Id;
        contentObject.FindProperty("category").enumValueIndex = (int)shape.Category;
        contentObject.FindProperty("title").stringValue = shape.Title;
        contentObject.FindProperty("subtitle").stringValue = shape.Subtitle;
        contentObject.FindProperty("description").stringValue = "Scan barcode " + shape.Id + " untuk memunculkan objek " + shape.ObjectType + " hasil generate Blender sebagai demo AR.";
        contentObject.FindProperty("thumbnail").objectReferenceValue = null;
        contentObject.FindProperty("referenceImageTexture").objectReferenceValue = referenceTexture;
        contentObject.FindProperty("prefab").objectReferenceValue = prefab;
        contentObject.FindProperty("referenceImageName").stringValue = referenceTexture != null ? shape.Id : string.Empty;
        contentObject.FindProperty("targetWidthMeters").floatValue = 0.12f;
        contentObject.FindProperty("objectType").stringValue = shape.ObjectType;
        contentObject.FindProperty("colorFocus").stringValue = ColorUtility.ToHtmlStringRGB(shape.Color);
        contentObject.FindProperty("fontTypeFocus").stringValue = "Blender Demo";
        contentObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(content);
        return content;
    }

    static void AddToLibraryIfMissing(SerializedProperty itemsProperty, MaterialContentData content)
    {
        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            if (itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue == content)
            {
                return;
            }
        }

        itemsProperty.InsertArrayElementAtIndex(itemsProperty.arraySize);
        itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1).objectReferenceValue = content;
    }
}
