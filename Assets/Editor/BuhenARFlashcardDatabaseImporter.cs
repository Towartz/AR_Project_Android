using System;
using System.Collections.Generic;
using System.IO;
using ARtiGraf.AR;
using ARtiGraf.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BuhenARFlashcardDatabaseImporter
{
    const string ExportRoot = "/home/twentyone/Pictures/UJIKOM_FLASHCARD/EXPORT";
    const string ReferenceFolder = "Assets/Art/ReferenceImages/Flashcards";
    const string MaterialFolder = "Assets/Materials/Flashcards";
    const string PrefabFolder = "Assets/Prefabs/Flashcards";
    const string ScriptableFolder = "Assets/ScriptableObjects";
    const string LibraryPath = "Assets/Resources/ARtiGrafContentLibrary.asset";
    const string ARScanScenePath = "Assets/Scenes/ARScanScene.unity";
    const string ReportPath = "UJIKOM_AZ_Database.md";
    const float TargetWidthMeters = 0.075f;

    sealed class FlashcardEntry
    {
        public string Letter;
        public string Id;
        public string Title;
        public string TargetName;
        public string SheetName;
        public int SheetIndex;
        public bool IsAnimal;
        public string Subtitle;
        public string Description;
        public string ColorFocus;
        public string ShapeFocus;
        public string FunFact;
        public string SpecificPrefabPath;
    }

    [MenuItem("Tools/BuhenAR/Content/Import A-Z Flashcards From EXPORT")]
    public static void ImportDefaultExport()
    {
        EnsureFolder(ReferenceFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(ScriptableFolder);

        List<FlashcardEntry> entries = BuildEntries();
        var importedContents = new List<MaterialContentData>(entries.Count);
        var targetNames = new List<string>(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            FlashcardEntry entry = entries[i];
            string sourcePath = Path.Combine(ExportRoot, entry.SheetName);
            string referencePath = ReferenceFolder + "/" + entry.TargetName + ".jpg";

            Texture2D referenceTexture = ExtractReferenceImage(entry, sourcePath, referencePath);
            GameObject prefab = ResolveOrCreatePrefab(entry, referenceTexture);
            MaterialContentData content = CreateOrUpdateContent(entry, referenceTexture, prefab);

            if (content != null)
            {
                importedContents.Add(content);
                targetNames.Add(entry.TargetName);
            }
        }

        ReplaceLibraryWith(importedContents);
        ConfigureARScannerForAtoZ(targetNames);
        WriteReport(entries);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuhenAR] Import A-Z flashcard selesai. Total konten: " + importedContents.Count);
    }

    static List<FlashcardEntry> BuildEntries()
    {
        return new List<FlashcardEntry>
        {
            Entry("A", "apel", "Apel", "apple", "FLASHCARD_A-E.jpg", 0, false,
                "Buah huruf A", "Buah bulat dengan kulit halus, bertangkai kecil, dan sering berwarna merah atau hijau.",
                "Merah atau hijau", "Bulat, bertangkai, kulit halus", "Dimulai dari huruf A dan sering dimakan sebagai camilan sehat.",
                "Assets/Prefabs/ARObjects/ApplePrefab.prefab"),
            Entry("B", "buaya", "Buaya", "B_Buaya", "FLASHCARD_A-E.jpg", 1, true,
                "Hewan huruf B", "Hewan reptil besar yang hidup di air dan darat, memiliki ekor panjang serta gigi tajam.",
                "Hijau", "Bertubuh panjang, bersisik, berekor kuat", "Dimulai dari huruf B dan termasuk reptil."),
            Entry("C", "ceri", "Ceri", "C_Ceri", "FLASHCARD_A-E.jpg", 2, false,
                "Buah huruf C", "Buah kecil berbentuk bulat, biasanya berwarna merah, dan sering tumbuh berpasangan pada tangkai.",
                "Merah", "Kecil, bulat, bertangkai", "Dimulai dari huruf C dan bentuknya kecil."),
            Entry("D", "domba", "Domba", "D_Domba", "FLASHCARD_A-E.jpg", 3, true,
                "Hewan huruf D", "Hewan ternak berbulu tebal yang sering dipelihara dan dapat menghasilkan wol.",
                "Putih", "Bulu tebal, berkaki empat", "Dimulai dari huruf D dan bulunya lembut."),
            Entry("E", "elang", "Elang", "E_Elang", "FLASHCARD_A-E.jpg", 4, true,
                "Hewan huruf E", "Burung pemangsa yang dapat terbang tinggi, memiliki sayap lebar dan penglihatan tajam.",
                "Cokelat dan putih", "Bersayap lebar, bercakar kuat", "Dimulai dari huruf E dan pandai terbang tinggi."),

            Entry("F", "flamingo", "Flamingo", "F_Flamingo", "FLASHCARD_F-J.jpg", 0, true,
                "Hewan huruf F", "Burung berkaki panjang dengan leher melengkung dan warna tubuh merah muda.",
                "Merah muda", "Kaki panjang, leher melengkung", "Dimulai dari huruf F dan sering berdiri dengan satu kaki."),
            Entry("G", "gajah", "Gajah", "G_Gajah", "FLASHCARD_F-J.jpg", 1, true,
                "Hewan huruf G", "Hewan darat besar yang memiliki belalai panjang, telinga lebar, dan gading.",
                "Abu-abu", "Besar, berbelalai, bertelinga lebar", "Dimulai dari huruf G dan punya belalai."),
            Entry("H", "harimau", "Harimau", "H_Harimau", "FLASHCARD_F-J.jpg", 2, true,
                "Hewan huruf H", "Hewan buas mirip kucing besar dengan loreng hitam di tubuhnya.",
                "Oranye dan hitam", "Bertubuh loreng, berkaki empat", "Dimulai dari huruf H dan memiliki loreng."),
            Entry("I", "ikan", "Ikan", "I_Ikan", "FLASHCARD_F-J.jpg", 3, true,
                "Hewan huruf I", "Hewan air yang bernapas dengan insang dan bergerak menggunakan sirip.",
                "Biru dan oranye", "Bersirip, hidup di air", "Dimulai dari huruf I dan berenang di air.",
                "Assets/Prefabs/ARObjects/FishPrefab.prefab"),
            Entry("J", "jagung", "Jagung", "J_Jagung", "FLASHCARD_F-J.jpg", 4, false,
                "Tanaman huruf J", "Tanaman pangan berbiji kuning yang tersusun rapi pada tongkol.",
                "Kuning dan hijau", "Bertongkol, berbiji banyak", "Dimulai dari huruf J dan memiliki banyak biji."),

            Entry("K", "kelinci", "Kelinci", "K_Kelinci", "FLASHCARD_K-O.jpg", 0, true,
                "Hewan huruf K", "Hewan kecil berbulu lembut dengan telinga panjang dan suka melompat.",
                "Putih", "Telinga panjang, berbulu lembut", "Dimulai dari huruf K dan suka melompat."),
            Entry("L", "lemon", "Lemon", "L_Lemon", "FLASHCARD_K-O.jpg", 1, false,
                "Buah huruf L", "Buah berwarna kuning dengan rasa asam dan aroma segar.",
                "Kuning", "Bulat lonjong, kulit tebal", "Dimulai dari huruf L dan rasanya asam."),
            Entry("M", "mangga", "Mangga", "M_Mangga", "FLASHCARD_K-O.jpg", 2, false,
                "Buah huruf M", "Buah tropis yang dagingnya manis, biasanya berwarna kuning atau oranye saat matang.",
                "Kuning atau oranye", "Lonjong, berdaging manis", "Dimulai dari huruf M dan disukai banyak anak."),
            Entry("N", "nanas", "Nanas", "N_Nanas", "FLASHCARD_K-O.jpg", 3, false,
                "Buah huruf N", "Buah tropis dengan kulit berduri, mahkota daun di atas, dan rasa manis asam.",
                "Kuning dan hijau", "Berduri, bermahkota daun", "Dimulai dari huruf N dan punya mahkota daun."),
            Entry("O", "orang_utan", "Orang Utan", "O_OrangUtan", "FLASHCARD_K-O.jpg", 4, true,
                "Hewan huruf O", "Hewan primata berbulu cokelat kemerahan yang pandai memanjat pohon.",
                "Cokelat kemerahan", "Berbulu panjang, berlengan kuat", "Dimulai dari huruf O dan pandai memanjat."),

            Entry("P", "pepaya", "Pepaya", "P_Pepaya", "FLASHCARD_P-U.jpg", 0, false,
                "Buah huruf P", "Buah lonjong dengan daging oranye dan banyak biji kecil berwarna hitam.",
                "Hijau dan oranye", "Lonjong, berbiji banyak", "Dimulai dari huruf P dan dagingnya lembut."),
            Entry("Q", "quail", "Quail", "Q_Quail", "FLASHCARD_P-U.jpg", 1, true,
                "Hewan huruf Q", "Burung kecil bercorak cokelat yang sering dikenal sebagai burung puyuh.",
                "Cokelat bercorak", "Burung kecil, tubuh berbintik", "Dimulai dari huruf Q dan mirip burung puyuh."),
            Entry("R", "rambutan", "Rambutan", "R_Rambutan", "FLASHCARD_P-U.jpg", 2, false,
                "Buah huruf R", "Buah berkulit merah dengan rambut halus di luar dan daging putih di dalam.",
                "Merah", "Berambut, bulat, berdaging putih", "Dimulai dari huruf R dan kulitnya berambut."),
            Entry("S", "sapi", "Sapi", "S_Sapi", "FLASHCARD_P-U.jpg", 3, true,
                "Hewan huruf S", "Hewan ternak berkaki empat yang dapat menghasilkan susu.",
                "Putih dan hitam", "Berkaki empat, bertubuh besar", "Dimulai dari huruf S dan menghasilkan susu."),
            Entry("T", "timun", "Timun", "T_Timun", "FLASHCARD_P-U.jpg", 4, false,
                "Sayur huruf T", "Sayuran hijau berbentuk lonjong dengan daging segar dan banyak biji kecil.",
                "Hijau", "Lonjong, segar, berbiji kecil", "Dimulai dari huruf T dan sering dimakan segar."),

            Entry("U", "ular", "Ular", "U_Ular", "FLASHCARD_V-Y.jpg", 0, true,
                "Hewan huruf U", "Hewan reptil panjang yang bergerak melata dan tidak memiliki kaki.",
                "Hijau kecokelatan", "Panjang, melata, tidak berkaki", "Dimulai dari huruf U dan bergerak melata."),
            Entry("V", "vampire_bat", "Vampire Bat", "V_VampireBat", "FLASHCARD_V-Y.jpg", 1, true,
                "Hewan huruf V", "Kelelawar yang aktif pada malam hari, memiliki sayap, dan tidur menggantung.",
                "Cokelat gelap", "Bersayap, menggantung, nokturnal", "Dimulai dari huruf V dan termasuk kelelawar."),
            Entry("W", "wortel", "Wortel", "W_Wortel", "FLASHCARD_V-Y.jpg", 2, false,
                "Sayur huruf W", "Sayuran akar berwarna oranye dengan daun hijau di bagian atas.",
                "Oranye dan hijau", "Akar memanjang, berdaun", "Dimulai dari huruf W dan baik untuk belajar warna oranye."),
            Entry("X", "xenops", "Xenops", "X_Xenops", "FLASHCARD_V-Y.jpg", 3, true,
                "Hewan huruf X", "Burung kecil yang suka mencari serangga di batang pohon.",
                "Cokelat", "Burung kecil, paruh ramping", "Dimulai dari huruf X dan termasuk burung."),
            Entry("Y", "yak", "Yak", "Y_Yak", "FLASHCARD_V-Y.jpg", 4, true,
                "Hewan huruf Y", "Hewan besar mirip sapi dengan bulu tebal dan tanduk.",
                "Cokelat gelap", "Besar, berbulu tebal, bertanduk", "Dimulai dari huruf Y dan hidup di daerah dingin."),

            Entry("Z", "zebra", "Zebra", "Z_Zebra", "FLASHCARD_Z_PACKAGE.jpg", 0, true,
                "Hewan huruf Z", "Hewan mirip kuda yang memiliki belang hitam dan putih di seluruh tubuhnya.",
                "Hitam dan putih", "Bertubuh belang, berkaki empat", "Dimulai dari huruf Z dan memiliki pola belang.")
        };
    }

    static FlashcardEntry Entry(
        string letter,
        string id,
        string title,
        string targetName,
        string sheetName,
        int sheetIndex,
        bool isAnimal,
        string subtitle,
        string description,
        string colorFocus,
        string shapeFocus,
        string funFact,
        string specificPrefabPath = "")
    {
        return new FlashcardEntry
        {
            Letter = letter,
            Id = id,
            Title = title,
            TargetName = targetName,
            SheetName = sheetName,
            SheetIndex = sheetIndex,
            IsAnimal = isAnimal,
            Subtitle = subtitle,
            Description = description,
            ColorFocus = colorFocus,
            ShapeFocus = shapeFocus,
            FunFact = funFact,
            SpecificPrefabPath = specificPrefabPath
        };
    }

    static Texture2D ExtractReferenceImage(FlashcardEntry entry, string sourcePath, string targetAssetPath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("[BuhenAR] Source flashcard tidak ditemukan: " + sourcePath);
            return null;
        }

        Texture2D sourceTexture = LoadTextureFromFile(sourcePath);
        if (sourceTexture == null)
        {
            Debug.LogError("[BuhenAR] Gagal load source flashcard: " + sourcePath);
            return null;
        }

        int expectedCards = entry.SheetName.Contains("Z_PACKAGE", StringComparison.OrdinalIgnoreCase) ? 1 : 5;
        List<RectInt> cardRects = DetectFrontCardRects(sourceTexture, expectedCards);
        if (cardRects.Count <= entry.SheetIndex)
        {
            cardRects = BuildFallbackRects(sourceTexture, expectedCards, entry.SheetName.Contains("Z_PACKAGE", StringComparison.OrdinalIgnoreCase));
        }

        if (cardRects.Count <= entry.SheetIndex)
        {
            Debug.LogError("[BuhenAR] Crop rect tidak ditemukan untuk " + entry.Title + " dari " + entry.SheetName);
            return null;
        }

        RectInt cropRect = cardRects[entry.SheetIndex];
        Texture2D croppedTexture = CropFromTopLeft(sourceTexture, cropRect);
        byte[] jpgBytes = croppedTexture.EncodeToJPG(92);
        File.WriteAllBytes(targetAssetPath, jpgBytes);
        Object.DestroyImmediate(sourceTexture);
        Object.DestroyImmediate(croppedTexture);

        AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(targetAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(targetAssetPath);
    }

    static Texture2D LoadTextureFromFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        return ImageConversion.LoadImage(texture, bytes) ? texture : null;
    }

    static List<RectInt> DetectFrontCardRects(Texture2D texture, int expectedCount)
    {
        var rects = new List<RectInt>();
        int width = texture.width;
        int height = texture.height;
        int scanTopHeight = Mathf.RoundToInt(height * 0.54f);
        int step = 4;
        int minColumnHits = Mathf.Max(10, scanTopHeight / 90);
        bool[] activeColumns = new bool[width];

        for (int x = 0; x < width; x += step)
        {
            int hits = 0;
            for (int topY = 0; topY < scanTopHeight; topY += step)
            {
                Color32 pixel = texture.GetPixel(x, height - 1 - topY);
                if (IsNonWhite(pixel))
                {
                    hits++;
                }
            }

            bool active = hits >= minColumnHits;
            for (int fill = 0; fill < step && x + fill < width; fill++)
            {
                activeColumns[x + fill] = active;
            }
        }

        List<Vector2Int> runs = BuildColumnRuns(activeColumns, 24);
        for (int i = 0; i < runs.Count; i++)
        {
            int xMin = Mathf.Max(0, runs[i].x - 12);
            int xMax = Mathf.Min(width - 1, runs[i].y + 12);
            if (xMax - xMin < 360)
            {
                continue;
            }

            if (TryBuildRectFromRun(texture, xMin, xMax, scanTopHeight, out RectInt rect))
            {
                rects.Add(rect);
            }
        }

        rects.Sort((left, right) => left.x.CompareTo(right.x));
        if (rects.Count > expectedCount)
        {
            if (expectedCount == 1)
            {
                RectInt rightMost = rects[0];
                for (int i = 1; i < rects.Count; i++)
                {
                    if (rects[i].x > rightMost.x)
                    {
                        rightMost = rects[i];
                    }
                }

                rects.Clear();
                rects.Add(rightMost);
            }
            else
            {
                rects.RemoveRange(expectedCount, rects.Count - expectedCount);
            }
        }

        return rects;
    }

    static List<Vector2Int> BuildColumnRuns(bool[] activeColumns, int bridgeGap)
    {
        var runs = new List<Vector2Int>();
        int start = -1;
        int lastActive = -1;

        for (int x = 0; x < activeColumns.Length; x++)
        {
            if (activeColumns[x])
            {
                if (start < 0)
                {
                    start = x;
                }

                lastActive = x;
                continue;
            }

            if (start >= 0 && x - lastActive > bridgeGap)
            {
                runs.Add(new Vector2Int(start, lastActive));
                start = -1;
                lastActive = -1;
            }
        }

        if (start >= 0)
        {
            runs.Add(new Vector2Int(start, lastActive));
        }

        return runs;
    }

    static bool TryBuildRectFromRun(Texture2D texture, int xMin, int xMax, int scanTopHeight, out RectInt rect)
    {
        int height = texture.height;
        int yMin = scanTopHeight;
        int yMax = 0;
        int step = 4;
        int minRowHits = Mathf.Max(12, (xMax - xMin) / 70);

        for (int topY = 0; topY < scanTopHeight; topY += step)
        {
            int hits = 0;
            for (int x = xMin; x <= xMax; x += step)
            {
                Color32 pixel = texture.GetPixel(x, height - 1 - topY);
                if (IsNonWhite(pixel))
                {
                    hits++;
                }
            }

            if (hits >= minRowHits)
            {
                yMin = Mathf.Min(yMin, topY);
                yMax = Mathf.Max(yMax, topY);
            }
        }

        if (yMax <= yMin || yMax - yMin < 600)
        {
            rect = default;
            return false;
        }

        int pad = 16;
        int top = Mathf.Max(0, yMin - pad);
        int bottom = Mathf.Min(height - 1, yMax + pad);
        rect = new RectInt(
            Mathf.Max(0, xMin - pad),
            top,
            Mathf.Min(texture.width - 1, xMax + pad) - Mathf.Max(0, xMin - pad),
            bottom - top);
        return true;
    }

    static bool IsNonWhite(Color32 pixel)
    {
        return pixel.r < 242 || pixel.g < 242 || pixel.b < 242;
    }

    static List<RectInt> BuildFallbackRects(Texture2D texture, int expectedCount, bool zPackage)
    {
        var rects = new List<RectInt>();
        if (zPackage)
        {
            rects.Add(new RectInt(
                Mathf.RoundToInt(texture.width * 0.765f),
                Mathf.RoundToInt(texture.height * 0.075f),
                Mathf.RoundToInt(texture.width * 0.17f),
                Mathf.RoundToInt(texture.height * 0.41f)));
            return rects;
        }

        int cardWidth = Mathf.RoundToInt(texture.width * 0.165f);
        int cardHeight = Mathf.RoundToInt(texture.height * 0.405f);
        int top = Mathf.RoundToInt(texture.height * 0.075f);
        int firstLeft = Mathf.RoundToInt(texture.width * 0.043f);
        int step = Mathf.RoundToInt(texture.width * 0.181f);
        for (int i = 0; i < expectedCount; i++)
        {
            rects.Add(new RectInt(firstLeft + (i * step), top, cardWidth, cardHeight));
        }

        return rects;
    }

    static Texture2D CropFromTopLeft(Texture2D source, RectInt topLeftRect)
    {
        int x = Mathf.Clamp(topLeftRect.x, 0, source.width - 1);
        int y = Mathf.Clamp(source.height - topLeftRect.y - topLeftRect.height, 0, source.height - 1);
        int width = Mathf.Clamp(topLeftRect.width, 1, source.width - x);
        int height = Mathf.Clamp(topLeftRect.height, 1, source.height - y);

        Color[] pixels = source.GetPixels(x, y, width, height);
        var cropped = new Texture2D(width, height, TextureFormat.RGB24, false);
        cropped.SetPixels(pixels);
        cropped.Apply(false, false);
        return cropped;
    }

    static GameObject ResolveOrCreatePrefab(FlashcardEntry entry, Texture2D referenceTexture)
    {
        if (!string.IsNullOrWhiteSpace(entry.SpecificPrefabPath))
        {
            GameObject specificPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.SpecificPrefabPath);
            if (specificPrefab != null)
            {
                return specificPrefab;
            }
        }

        string materialPath = MaterialFolder + "/" + entry.Id + "_card.mat";
        Material cardMaterial = CreateOrUpdateCardMaterial(materialPath, referenceTexture);
        Material backMaterial = CreateOrUpdateSolidMaterial(MaterialFolder + "/flashcard_back.mat", new Color32(255, 244, 226, 255));

        string prefabPath = PrefabFolder + "/" + entry.Id + "_FlashcardPrefab.prefab";
        GameObject root = new GameObject(entry.Id + "_FlashcardPrefab");

        GameObject baseTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseTile.name = "CardBase";
        baseTile.transform.SetParent(root.transform, false);
        baseTile.transform.localPosition = new Vector3(0f, 0.002f, 0f);
        baseTile.transform.localScale = new Vector3(0.13f, 0.004f, 0.20f);
        Renderer baseRenderer = baseTile.GetComponent<Renderer>();
        if (baseRenderer != null)
        {
            baseRenderer.sharedMaterial = backMaterial;
        }

        GameObject front = GameObject.CreatePrimitive(PrimitiveType.Quad);
        front.name = "CardFrontImage";
        front.transform.SetParent(root.transform, false);
        front.transform.localPosition = new Vector3(0f, 0.006f, 0f);
        front.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        front.transform.localScale = new Vector3(0.12f, 0.185f, 1f);
        Renderer frontRenderer = front.GetComponent<Renderer>();
        if (frontRenderer != null)
        {
            frontRenderer.sharedMaterial = cardMaterial;
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "LearningPop";
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        marker.transform.localScale = Vector3.one * 0.028f;
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.sharedMaterial = CreateOrUpdateSolidMaterial(
                MaterialFolder + "/" + entry.Id + "_accent.mat",
                entry.IsAnimal ? new Color32(85, 160, 255, 255) : new Color32(255, 170, 65, 255));
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    static Material CreateOrUpdateCardMaterial(string assetPath, Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CreateOrUpdateSolidMaterial(string assetPath, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static MaterialContentData CreateOrUpdateContent(FlashcardEntry entry, Texture2D referenceTexture, GameObject prefab)
    {
        string assetPath = ScriptableFolder + "/" + entry.Id + ".asset";
        MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(assetPath);
        if (content == null)
        {
            content = ScriptableObject.CreateInstance<MaterialContentData>();
            AssetDatabase.CreateAsset(content, assetPath);
        }

        SerializedObject serializedContent = new SerializedObject(content);
        serializedContent.FindProperty("id").stringValue = entry.Id;
        serializedContent.FindProperty("category").enumValueIndex = entry.IsAnimal ? (int)LearningCategory.Color : (int)LearningCategory.Typography;
        serializedContent.FindProperty("title").stringValue = entry.Title;
        serializedContent.FindProperty("subtitle").stringValue = entry.Subtitle;
        serializedContent.FindProperty("description").stringValue = entry.Description;
        serializedContent.FindProperty("thumbnail").objectReferenceValue = referenceTexture != null
            ? AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(referenceTexture))
            : null;
        serializedContent.FindProperty("referenceImageTexture").objectReferenceValue = referenceTexture;
        serializedContent.FindProperty("prefab").objectReferenceValue = prefab;
        serializedContent.FindProperty("referenceImageName").stringValue = entry.TargetName;
        serializedContent.FindProperty("targetWidthMeters").floatValue = TargetWidthMeters;
        serializedContent.FindProperty("objectType").stringValue = entry.IsAnimal ? "Hewan" : "Buah/Sayur";
        serializedContent.FindProperty("colorFocus").stringValue = entry.ColorFocus;
        serializedContent.FindProperty("fontTypeFocus").stringValue = entry.ShapeFocus;
        serializedContent.FindProperty("funFact").stringValue = entry.FunFact;
        serializedContent.FindProperty("spellOverride").stringValue = entry.Title;
        SetArray(serializedContent.FindProperty("quizQuestions"), new[]
        {
            "Huruf awal dari " + entry.Title + " adalah?",
            "Manakah ciri yang cocok untuk kartu ini?"
        });
        SetArray(serializedContent.FindProperty("quizAnswers"), new[]
        {
            entry.Letter,
            entry.ShapeFocus
        });
        SetArray(serializedContent.FindProperty("quizWrongOptions"), new[]
        {
            BuildWrongLetterOptions(entry.Letter),
            entry.IsAnimal ? "Bentuk buah bulat kecil" : "Bersayap dan dapat terbang tinggi"
        });
        serializedContent.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(content);
        return content;
    }

    static string BuildWrongLetterOptions(string correctLetter)
    {
        char correct = string.IsNullOrWhiteSpace(correctLetter) ? 'A' : correctLetter[0];
        char first = correct == 'A' ? 'B' : 'A';
        char second = correct == 'Z' ? 'Y' : (char)(correct + 1);
        return first + "|" + second;
    }

    static void SetArray(SerializedProperty property, string[] values)
    {
        if (property == null)
        {
            return;
        }

        property.arraySize = values == null ? 0 : values.Length;
        for (int i = 0; values != null && i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }

    static void ReplaceLibraryWith(List<MaterialContentData> contents)
    {
        MaterialContentLibrary library = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<MaterialContentLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.ReplaceItems(contents);
        EditorUtility.SetDirty(library);
    }

    static void ConfigureARScannerForAtoZ(List<string> targetNames)
    {
        Scene scene = GetOrOpenScene(ARScanScenePath, out bool openedTemporarily);
        if (!scene.IsValid())
        {
            Debug.LogWarning("[BuhenAR] ARScanScene tidak ditemukan, scanner tidak disync.");
            return;
        }

        ARImageTrackingController trackingController = FindTrackingController(scene);
        if (trackingController == null)
        {
            Debug.LogWarning("[BuhenAR] ARImageTrackingController tidak ditemukan di ARScanScene.");
            return;
        }

        SerializedObject serializedTracking = new SerializedObject(trackingController);
        serializedTracking.FindProperty("useImportedVuforiaDeviceDatabase").boolValue = true;
        serializedTracking.FindProperty("strictImportedVuforiaDeviceDatabaseOnly").boolValue = false;
        serializedTracking.FindProperty("importedVuforiaDeviceDatabaseTargets").stringValue = string.Join(",", targetNames);
        serializedTracking.FindProperty("enableBarcodeFallback").boolValue = false;
        serializedTracking.FindProperty("alwaysCreateBarcodeObserverOnMobile").boolValue = false;
        serializedTracking.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(trackingController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedTemporarily)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    static Scene GetOrOpenScene(string scenePath, out bool openedTemporarily)
    {
        openedTemporarily = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.IsValid() && loadedScene.path == scenePath)
            {
                return loadedScene;
            }
        }

        if (!File.Exists(scenePath))
        {
            return default;
        }

        openedTemporarily = true;
        return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
    }

    static ARImageTrackingController FindTrackingController(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            ARImageTrackingController controller = roots[i].GetComponentInChildren<ARImageTrackingController>(true);
            if (controller != null)
            {
                return controller;
            }
        }

        return null;
    }

    static void WriteReport(List<FlashcardEntry> entries)
    {
        using (var writer = new StreamWriter(ReportPath, false))
        {
            writer.WriteLine("# BuhenAR A-Z Database");
            writer.WriteLine();
            writer.WriteLine("Generated dari `/home/twentyone/Pictures/UJIKOM_FLASHCARD/EXPORT`.");
            writer.WriteLine();
            writer.WriteLine("Target image hasil crop ada di `Assets/Art/ReferenceImages/Flashcards/`.");
            writer.WriteLine("Content aktif runtime sekarang dibatasi ke 26 item A-Z agar tidak mismatch ke materi lama/demo.");
            writer.WriteLine();
            writer.WriteLine("Catatan Vuforia: untuk device database paling stabil, upload file crop A-Z ini ke Target Manager dan pakai nama target yang sama dengan kolom `Target Name`.");
            writer.WriteLine("Untuk target apple lama, nama `apple` tetap dipertahankan agar DB yang sudah ada tidak putus.");
            writer.WriteLine();
            writer.WriteLine("| Huruf | Nama | Kategori | Target Name | Asset |");
            writer.WriteLine("|---|---|---|---|---|");
            for (int i = 0; i < entries.Count; i++)
            {
                FlashcardEntry entry = entries[i];
                writer.WriteLine("| " + entry.Letter + " | " + entry.Title + " | " +
                                 (entry.IsAnimal ? "Hewan" : "Buah/Sayur") + " | " +
                                 entry.TargetName + " | `Assets/ScriptableObjects/" + entry.Id + ".asset` |");
            }
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
}
