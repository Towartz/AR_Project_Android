using System.Collections.Generic;
using System.IO;
using ARtiGraf.AR;
using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.Quiz;
using ARtiGraf.UI;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

// ============================================================
// UI/UX DESIGN SYSTEM - BuhenAR
// Style      : Soft UI Evolution + Accessible Mobile
// Platform   : Android (1080x1920, touch-first)
// Pattern    : Hero-Centric + Feature Showcase
// WCAG       : AA (contrast >= 4.5:1 for normal text)
// Touch min  : 88px height for all interactive targets
// Typography : Scale 20 / 24 / 28 / 34 / 46 / 58 / 72
// Spacing    : Base 8px grid
// ============================================================

public static class ARtiGrafProjectBuilder
{
    const int ReferenceImageSize         = 384;
    const string ScenesFolder              = "Assets/Scenes";
    const string PrefabsFolder             = "Assets/Prefabs/ARObjects";
    const string ImportedAppleModelPath    = "Assets/Imported/Apple/Apple_Model.fbx";
    const string ImportedAppleTextureFolder = "Assets/Imported/Apple/Apple_Model.fbm";
    const string MaterialsFolder           = "Assets/Materials";
    const string AudioFolder               = "Assets/Audio";
    const string BackgroundMusicPath       = AudioFolder + "/the_mountain-happy-playful-kids-music-450659.mp3";
    const string ReferenceImagesFolder     = "Assets/Art/ReferenceImages";
    const string UiAppsFolder              = "Assets/Art/UI_APPS";
    const string SplashPortraitPath        = UiAppsFolder + "/Splash_portrait.png";
    const string SplashLandscapePath       = UiAppsFolder + "/Splash_landscape.png";
    const string MainMenuPortraitPath      = UiAppsFolder + "/mainmenu_portrait.png";
    const string MainMenuLandscapePath     = UiAppsFolder + "/mainmenu_landscape.png";
    const string GuidePortraitPath         = UiAppsFolder + "/guide_portrait.png";
    const string GuideLandscapePath        = UiAppsFolder + "/guide_landscape.png";
    const string MaterialGuidePortraitPath = UiAppsFolder + "/Material_Guide_portrait.png";
    const string MaterialGuideLandscapePath = UiAppsFolder + "/Material_Guide_Landscape.png";
    const string AboutPortraitPath         = UiAppsFolder + "/about_Portrait.png";
    const string AboutLandscapePath        = UiAppsFolder + "/about_Landscape.png";
    const string ScriptableObjectsFolder   = "Assets/ScriptableObjects";
    const string ContentLibraryPath        = "Assets/Resources/ARtiGrafContentLibrary.asset";
    const string QuizBankPath              = ScriptableObjectsFolder + "/ARtiGrafQuizBank.asset";
    const string AppTitle                  = "BuhenAR";

    // ---- Design Tokens ----
    // Background
    static readonly Color BgPage       = new Color32(244, 247, 252, 255); // #F4F7FC - light, neutral
    static readonly Color BgCard       = new Color32(255, 255, 255, 242); // white 95% - elevated card
    static readonly Color BgCardHover  = new Color32(248, 251, 255, 255); // subtle hover

    // Brand
    static readonly Color Primary      = new Color32(25, 118, 210, 255);  // #1976D2 - WCAG AA on white
    static readonly Color PrimaryLight = new Color32(227, 242, 253, 255); // #E3F2FD - tint bg
    static readonly Color Accent       = new Color32(230, 108, 0, 255);   // #E66C00 - darker orange for WCAG AA
    static readonly Color AccentLight  = new Color32(255, 243, 224, 255); // #FFF3E0

    // Text - all WCAG AA compliant
    static readonly Color TextPrimary  = new Color32(13, 27, 62, 255);    // #0D1B3E - deepest
    static readonly Color TextSecond   = new Color32(44, 62, 98, 255);    // #2C3E62 - body
    static readonly Color TextMuted    = new Color32(85, 107, 145, 255);  // #556B91 - captions

    // Overlay / AR UI
    static readonly Color OverlayDark  = new Color(0.04f, 0.08f, 0.18f, 0.88f);
    static readonly Color OverlayMid   = new Color(0.04f, 0.08f, 0.18f, 0.72f);
    static readonly Color OverlayLight = new Color(0.04f, 0.08f, 0.18f, 0.48f);

    // Status
    static readonly Color Success      = new Color32(22, 163, 74, 255);   // #16A34A
    static readonly Color Error        = new Color32(185, 28, 28, 255);   // #B91C1C

    // Shadow
    static readonly Color ShadowColor  = new Color(0.04f, 0.08f, 0.20f, 0.18f);

    static AudioClip CachedBackgroundMusicClip;
    static bool BackgroundMusicImportChecked;

    struct ButtonParts
    {
        public Button button;
        public Text label;
    }

    class ContentSeed
    {
        public string id;
        public LearningCategory category;
        public string title;
        public string subtitle;
        public string description;
        public string referenceImageName;
        public string objectType;
        public string colorFocus;
        public string fontTypeFocus;
        public string prefabName;
        public string prefabTemplateName;
        public Color colorA;
        public Color colorB;
    }

    // ================================================================
    // MENU ENTRIES
    // ================================================================

    [MenuItem("Tools/BuhenAR/Generate Tutorial Project")]
    public static void GenerateTutorialProject()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(ReferenceImagesFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabsFolder);
        EnsureFolder("Assets/Materials");
        EnsureFolder(AudioFolder);
        EnsureFolder(ScriptableObjectsFolder);

        List<ContentSeed> seeds = CreateSeeds();
        CreateReferenceImages(seeds);
        Dictionary<string, GameObject> prefabs = CreatePrefabs(seeds);
        MaterialContentLibrary contentLibrary = CreateMaterialContentLibrary(seeds, prefabs);
        QuizQuestionBank quizBank = CreateQuizBank();
        ConfigureVuforia();

        CreateSplashScene();
        CreateMainMenuScene();
        CreateGuideScene();
        CreateMaterialSelectScene();
        CreateARScanScene(contentLibrary);
        CreateQuizScene(quizBank);
        CreateResultScene();
        CreateAboutScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BuhenAR tutorial project generated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Material Select Scene")]
    public static void RegenerateMaterialSelectScene()
    {
        CreateMaterialSelectScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Material select scene regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate UI Artwork Scenes")]
    public static void RegenerateUiArtworkScenes()
    {
        CreateSplashScene();
        CreateMainMenuScene();
        CreateGuideScene();
        CreateMaterialSelectScene();
        CreateAboutScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UI artwork scenes regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Refresh Content Library")]
    public static void RefreshContentLibrary()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(ReferenceImagesFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabsFolder);
        EnsureFolder("Assets/Materials");
        EnsureFolder(AudioFolder);
        EnsureFolder(ScriptableObjectsFolder);

        List<ContentSeed> seeds = CreateSeeds();
        CreateReferenceImages(seeds);
        Dictionary<string, GameObject> prefabs = CreatePrefabs(seeds);
        MaterialContentLibrary contentLibrary = CreateMaterialContentLibrary(seeds, prefabs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GenerateBlenderDemoAssets.Generate();
        FixBarcodeSceneSetup.Apply();

        contentLibrary = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        CreateMaterialSelectScene();
        if (contentLibrary != null)
        {
            CreateARScanScene(contentLibrary);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Content library refreshed.");
    }

    [MenuItem("Tools/BuhenAR/Refresh 3D Prefabs")]
    public static void Refresh3DPrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabsFolder);
        EnsureFolder("Assets/Materials");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CreatePrefabs(CreateSeeds());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BuhenAR 3D prefabs refreshed.");
    }

    [MenuItem("Tools/BuhenAR/Populate Learning Extras")]
    public static void PopulateLearningExtras()
    {
        string[] guids = AssetDatabase.FindAssets("t:MaterialContentData", new[] { ScriptableObjectsFolder });
        int updatedCount = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(path);
            if (content == null)
            {
                continue;
            }

            SerializedObject contentObject = new SerializedObject(content);
            if (ApplyDefaultLearningExtras(contentObject, content.Title, content.ObjectType,
                    content.FontTypeFocus, (int)content.Category))
            {
                contentObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(content);
                updatedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Learning extras populated for " + updatedCount + " content assets.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate AR Scene")]
    public static void RegenerateARScene()
    {
        MaterialContentLibrary contentLibrary = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        if (contentLibrary == null)
        {
            Debug.LogError("Content library belum ditemukan.");
            return;
        }

        CreateARScanScene(contentLibrary);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("AR scene regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Quiz Scene")]
    public static void RegenerateQuizScene()
    {
        QuizQuestionBank quizBank = AssetDatabase.LoadAssetAtPath<QuizQuestionBank>(QuizBankPath);
        if (quizBank == null)
        {
            Debug.LogError("Quiz bank belum ditemukan.");
            return;
        }

        CreateQuizScene(quizBank);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Quiz scene regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Splash Scene")]
    public static void RegenerateSplashScene()
    {
        CreateSplashScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Splash scene regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Home UI Scenes")]
    public static void RegenerateHomeUiScenes()
    {
        CreateSplashScene();
        CreateMainMenuScene();
        CreateGuideScene();
        CreateMaterialSelectScene();
        CreateAboutScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Splash, main menu, guide, material guide, and about scenes regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Guide Scene")]
    public static void RegenerateGuideScene()
    {
        CreateGuideScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Guide scene regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Result Scene")]
    public static void RegenerateResultScene()
    {
        CreateResultScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Result scene regenerated.");
    }

    [MenuItem("Tools/BuhenAR/Regenerate Application Scenes")]
    public static void RegenerateApplicationScenes()
    {
        MaterialContentLibrary contentLibrary = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        QuizQuestionBank quizBank = AssetDatabase.LoadAssetAtPath<QuizQuestionBank>(QuizBankPath);

        CreateSplashScene();
        CreateMainMenuScene();
        CreateGuideScene();
        CreateMaterialSelectScene();

        if (contentLibrary != null)
        {
            CreateARScanScene(contentLibrary);
        }

        if (quizBank != null)
        {
            CreateQuizScene(quizBank);
        }

        CreateResultScene();
        CreateAboutScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Application scenes regenerated.");
    }

    // ================================================================
    // CONTENT SEEDS
    // ================================================================

    static List<ContentSeed> CreateSeeds()
    {
        return new List<ContentSeed>
        {
            new ContentSeed
            {
                id = "typo_serif",
                category = LearningCategory.Color,
                title = "Kucing",
                subtitle = "Hewan jinak berkumis",
                description = "Kucing adalah hewan peliharaan yang lincah, memiliki empat kaki, ekor, dan kumis. Suaranya mengeong dan gerakannya cepat.",
                referenceImageName = "typo_serif",
                objectType = "Kucing",
                colorFocus = "Cokelat krem",
                fontTypeFocus = "Empat kaki, ekor, dan kumis",
                prefabName = "CatPrefab",
                prefabTemplateName = "CatPrefab",
                colorA = new Color32(244, 196, 120, 255),
                colorB = new Color32(31, 41, 55, 255)
            },
            new ContentSeed
            {
                id = "apel",
                category = LearningCategory.Typography,
                title = "Apel",
                subtitle = "Buah bulat yang renyah",
                description = "Apel adalah buah bulat dengan kulit halus dan tangkai kecil. Warnanya sering merah atau hijau dan mudah dikenali anak-anak.",
                referenceImageName = "apple",
                objectType = "Apel",
                colorFocus = "Merah atau hijau segar",
                fontTypeFocus = "Bulat dengan tangkai kecil",
                prefabName = "ApplePrefab",
                prefabTemplateName = "ApplePrefab",
                colorA = new Color32(231, 76, 60, 255),
                colorB = new Color32(46, 204, 113, 255)
            },
            new ContentSeed
            {
                id = "typo_script",
                category = LearningCategory.Color,
                title = "Kupu-kupu",
                subtitle = "Serangga bersayap indah",
                description = "Kupu-kupu memiliki dua pasang sayap yang tipis dan berwarna menarik. Hewan ini sering hinggap di bunga dan terbang ringan.",
                referenceImageName = "typo_script",
                objectType = "Kupu-kupu",
                colorFocus = "Magenta dan kuning",
                fontTypeFocus = "Sayap lebar dan antena kecil",
                prefabName = "ButterflyPrefab",
                prefabTemplateName = "ButterflyPrefab",
                colorA = new Color32(214, 51, 132, 255),
                colorB = new Color32(253, 224, 71, 255)
            },
            new ContentSeed
            {
                id = "typo_display",
                category = LearningCategory.Typography,
                title = "Jeruk",
                subtitle = "Buah segar kaya vitamin C",
                description = "Jeruk berbentuk bulat dengan kulit berpori dan aroma segar. Buah ini identik dengan warna oranye cerah dan rasa manis asam.",
                referenceImageName = "typo_display",
                objectType = "Jeruk",
                colorFocus = "Oranye cerah",
                fontTypeFocus = "Bulat berkulit berpori",
                prefabName = "OrangePrefab",
                prefabTemplateName = "OrangePrefab",
                colorA = new Color32(249, 115, 22, 255),
                colorB = new Color32(255, 247, 237, 255)
            },
            new ContentSeed
            {
                id = "typo_monospace",
                category = LearningCategory.Color,
                title = "Burung",
                subtitle = "Hewan bersayap yang lincah",
                description = "Burung memiliki paruh, dua sayap, dan dua kaki. Banyak burung dapat terbang dan mengeluarkan suara kicau yang khas.",
                referenceImageName = "typo_monospace",
                objectType = "Burung",
                colorFocus = "Biru elektrik dan putih",
                fontTypeFocus = "Paruh kecil dan sayap terbuka",
                prefabName = "MonospaceBirdPrefab",
                prefabTemplateName = "BirdPrefab",
                colorA = new Color32(34, 211, 238, 255),
                colorB = new Color32(30, 64, 175, 255)
            },
            new ContentSeed
            {
                id = "typo_slab",
                category = LearningCategory.Typography,
                title = "Pisang",
                subtitle = "Buah panjang berkulit kuning",
                description = "Pisang mudah dikenali dari bentuknya yang memanjang dan melengkung. Saat matang, kulitnya berubah kuning dan rasanya manis lembut.",
                referenceImageName = "typo_slab",
                objectType = "Pisang",
                colorFocus = "Kuning dan cokelat muda",
                fontTypeFocus = "Panjang dan sedikit melengkung",
                prefabName = "SlabBananaPrefab",
                prefabTemplateName = "BananaPrefab",
                colorA = new Color32(245, 158, 11, 255),
                colorB = new Color32(120, 53, 15, 255)
            },
            new ContentSeed
            {
                id = "typo_geometric",
                category = LearningCategory.Typography,
                title = "Apel Pola",
                subtitle = "Apel dengan bentuk sederhana",
                description = "Model apel ini dipakai untuk mengenalkan bentuk buah secara sederhana. Anak-anak bisa fokus pada bagian bulat, daun, dan tangkai.",
                referenceImageName = "typo_geometric",
                objectType = "Apel pola",
                colorFocus = "Merah koral dan hijau daun",
                fontTypeFocus = "Bulat sederhana dengan daun",
                prefabName = "GeometricApplePrefab",
                prefabTemplateName = "ApplePrefab",
                colorA = new Color32(248, 113, 113, 255),
                colorB = new Color32(13, 148, 136, 255)
            },
            new ContentSeed
            {
                id = "typo_blackletter",
                category = LearningCategory.Typography,
                title = "Anggur",
                subtitle = "Buah kecil bergerombol",
                description = "Anggur tumbuh dalam satu tangkai dengan banyak butir kecil. Warna buahnya bisa ungu atau hijau dan sering dimakan segar.",
                referenceImageName = "typo_blackletter",
                objectType = "Anggur",
                colorFocus = "Ungu dan hijau",
                fontTypeFocus = "Butiran kecil dalam satu tangkai",
                prefabName = "BlackletterGrapesPrefab",
                prefabTemplateName = "GrapesPrefab",
                colorA = new Color32(88, 28, 135, 255),
                colorB = new Color32(202, 138, 4, 255)
            },
            new ContentSeed
            {
                id = "color_warm",
                category = LearningCategory.Typography,
                title = "Pisang Matang",
                subtitle = "Buah kuning yang manis",
                description = "Pisang matang berwarna kuning cerah dan mudah ditemukan sehari-hari. Bentuknya memanjang sehingga gampang dibedakan dari buah lain.",
                referenceImageName = "color_warm",
                objectType = "Pisang matang",
                colorFocus = "Kuning cerah dan hijau tangkai",
                fontTypeFocus = "Panjang dengan ujung membulat",
                prefabName = "BananaPrefab",
                prefabTemplateName = "BananaPrefab",
                colorA = new Color32(250, 204, 21, 255),
                colorB = new Color32(234, 88, 12, 255)
            },
            new ContentSeed
            {
                id = "color_cool",
                category = LearningCategory.Color,
                title = "Burung Biru",
                subtitle = "Burung kecil yang tenang",
                description = "Burung biru dipakai sebagai contoh hewan bersayap dengan warna lembut. Anak-anak dapat mengamati paruh, sayap, dan ekornya.",
                referenceImageName = "color_cool",
                objectType = "Burung biru",
                colorFocus = "Biru dan hijau",
                fontTypeFocus = "Sayap, paruh, dan ekor",
                prefabName = "BirdPrefab",
                prefabTemplateName = "BirdPrefab",
                colorA = new Color32(14, 165, 233, 255),
                colorB = new Color32(34, 197, 94, 255)
            },
            new ContentSeed
            {
                id = "color_contrast",
                category = LearningCategory.Color,
                title = "Ikan Badut",
                subtitle = "Ikan laut bergaris cerah",
                description = "Ikan badut hidup di laut dan mudah dikenali dari pola garis putih pada tubuh oranyenya. Siripnya membantu bergerak lincah di air.",
                referenceImageName = "color_contrast",
                objectType = "Ikan badut",
                colorFocus = "Oranye, putih, hitam",
                fontTypeFocus = "Sirip kecil dan tubuh oval",
                prefabName = "FishPrefab",
                prefabTemplateName = "FishPrefab",
                colorA = new Color32(249, 115, 22, 255),
                colorB = new Color32(255, 255, 255, 255)
            },
            new ContentSeed
            {
                id = "color_harmony",
                category = LearningCategory.Typography,
                title = "Anggur Ungu",
                subtitle = "Buah mungil dalam satu kelompok",
                description = "Anggur ungu terlihat menarik karena butirannya berkumpul rapat. Anak-anak dapat mengenali bentuk kelompok dan warna buahnya sekaligus.",
                referenceImageName = "color_harmony",
                objectType = "Anggur ungu",
                colorFocus = "Ungu tua dan hijau",
                fontTypeFocus = "Kelompok buah kecil rapat",
                prefabName = "GrapesPrefab",
                prefabTemplateName = "GrapesPrefab",
                colorA = new Color32(124, 58, 237, 255),
                colorB = new Color32(74, 222, 128, 255)
            },
            new ContentSeed
            {
                id = "color_primary",
                category = LearningCategory.Color,
                title = "Kupu-kupu Ceria",
                subtitle = "Serangga kecil dengan sayap lebar",
                description = "Kupu-kupu ceria menampilkan bentuk sayap yang simetris dan menarik. Hewan ini mudah dipakai untuk mengenalkan bagian tubuh serangga.",
                referenceImageName = "color_primary",
                objectType = "Kupu-kupu ceria",
                colorFocus = "Merah, kuning, biru",
                fontTypeFocus = "Sayap simetris dan antena tipis",
                prefabName = "PrimaryButterflyPrefab",
                prefabTemplateName = "ButterflyPrefab",
                colorA = new Color32(239, 68, 68, 255),
                colorB = new Color32(59, 130, 246, 255)
            },
            new ContentSeed
            {
                id = "color_secondary",
                category = LearningCategory.Typography,
                title = "Jeruk Cerah",
                subtitle = "Buah bulat dengan kulit tebal",
                description = "Jeruk cerah membantu anak mengenali buah berbentuk bulat dengan kulit yang agak tebal. Aromanya segar dan warna kulitnya mudah dikenali.",
                referenceImageName = "color_secondary",
                objectType = "Jeruk cerah",
                colorFocus = "Oranye dan hijau daun",
                fontTypeFocus = "Bulat dengan kulit tebal",
                prefabName = "SecondaryOrangePrefab",
                prefabTemplateName = "OrangePrefab",
                colorA = new Color32(168, 85, 247, 255),
                colorB = new Color32(34, 197, 94, 255)
            },
            new ContentSeed
            {
                id = "color_pastel",
                category = LearningCategory.Color,
                title = "Burung Pastel",
                subtitle = "Burung lembut berwarna cerah",
                description = "Burung pastel memberi tampilan hewan yang ramah dan ringan. Bentuk tubuh kecil, paruh, dan sayapnya cocok untuk pengenalan visual anak.",
                referenceImageName = "color_pastel",
                objectType = "Burung pastel",
                colorFocus = "Pink muda dan mint",
                fontTypeFocus = "Tubuh kecil dengan sayap lembut",
                prefabName = "PastelBirdPrefab",
                prefabTemplateName = "BirdPrefab",
                colorA = new Color32(251, 182, 206, 255),
                colorB = new Color32(167, 243, 208, 255)
            },
            new ContentSeed
            {
                id = "color_earth",
                category = LearningCategory.Color,
                title = "Kucing Oren",
                subtitle = "Kucing hangat berwarna tanah",
                description = "Kucing oren tampak akrab dan mudah dikenali dari telinga runcing, ekor panjang, dan tubuh berkaki empat. Warna bulunya terasa hangat.",
                referenceImageName = "color_earth",
                objectType = "Kucing oren",
                colorFocus = "Cokelat tanah dan krem",
                fontTypeFocus = "Telinga runcing dan ekor panjang",
                prefabName = "EarthCatPrefab",
                prefabTemplateName = "CatPrefab",
                colorA = new Color32(133, 77, 14, 255),
                colorB = new Color32(101, 163, 13, 255)
            }
        };
    }

    // ================================================================
    // REFERENCE IMAGES
    // ================================================================

    static void CreateReferenceImages(List<ContentSeed> seeds)
    {
        for (int i = 0; i < seeds.Count; i++)
        {
            ContentSeed seed = seeds[i];
            string assetPath = ReferenceImagesFolder + "/" + seed.referenceImageName + ".png";
            int maxTextureSize = seed.referenceImageName == "apple" ? 1024 : ReferenceImageSize;
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            if (seed.referenceImageName == "apple" && File.Exists(fullPath))
            {
                AssetDatabase.ImportAsset(assetPath);
                ConfigureReferenceImageAsset(assetPath, maxTextureSize);
                continue;
            }

            Texture2D texture = BuildReferenceTexture(seed);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);
            ConfigureReferenceImageAsset(assetPath, maxTextureSize);
        }
    }

    static void ConfigureReferenceImageAsset(string assetPath, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            ConfigureTrackingTextureImporter(importer, maxTextureSize);
            importer.SaveAndReimport();
        }
    }

    static Texture2D BuildReferenceTexture(ContentSeed seed)
    {
        const int size = ReferenceImageSize;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        int hash = ComputeSeedHash(seed.id);
        float circleX = 0.26f + ((hash & 0x07) / 7f) * 0.46f;
        float circleY = 0.24f + (((hash >> 3) & 0x07) / 7f) * 0.5f;
        float circleRadius = 0.09f + (((hash >> 6) & 0x03) * 0.018f);
        int stripeStep = 26 + ((hash >> 8) & 0x0F);
        int stripeThickness = 6 + ((hash >> 12) & 0x07);
        int moduleSize = 26 + ((hash >> 15) & 0x0F);
        int inset = 28 + ((hash >> 19) & 0x0F);
        bool invertStripeDirection = ((hash >> 23) & 0x01) == 1;
        Color ink = new Color(0.07f, 0.09f, 0.16f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float xf = x / (float)(size - 1);
                float yf = y / (float)(size - 1);
                float diagonalBlend = Mathf.Clamp01((yf * 0.7f) + (xf * 0.3f));
                Color baseColor = Color.Lerp(seed.colorA, seed.colorB, diagonalBlend);

                if ((x / 32 + y / 32) % 2 == 0)
                    baseColor *= 0.82f;

                int diagonalPrimary = invertStripeDirection ? x + y : x - y + size;
                if ((diagonalPrimary % stripeStep) < stripeThickness)
                    baseColor = Color.Lerp(baseColor, Color.white, 0.72f);

                if (x > 72 && x < size - 72 && y > 72 && y < size - 72 && ((x + hash) / 44) % 2 == 0)
                    baseColor = Color.Lerp(baseColor, ink, 0.16f);

                float circle = Mathf.Pow(xf - circleX, 2f) + Mathf.Pow(yf - circleY, 2f);
                if (circle < circleRadius * circleRadius)
                {
                    baseColor = Color.Lerp(baseColor, Color.white, 0.76f);
                }

                bool topLeftBlock = x > inset && x < inset + moduleSize * 2 && y > inset && y < inset + moduleSize * 3;
                bool bottomRightBlock = x > size - inset - moduleSize * 3 && x < size - inset &&
                                        y > size - inset - moduleSize * 2 && y < size - inset;
                if (topLeftBlock || bottomRightBlock)
                {
                    int gridX = (x - inset) / Mathf.Max(1, moduleSize);
                    int gridY = (y - inset) / Mathf.Max(1, moduleSize);
                    if ((gridX + gridY + hash) % 2 == 0)
                    {
                        baseColor = ink;
                    }
                    else
                    {
                        baseColor = Color.white;
                    }
                }

                if (x < 22 || x > size - 23 || y < 22 || y > size - 23)
                {
                    baseColor = Color.Lerp(baseColor, ink, 0.55f);
                }

                if ((x > size * 0.63f && x < size * 0.82f && y > size * 0.16f && y < size * 0.28f) ||
                    (x > size * 0.15f && x < size * 0.30f && y > size * 0.67f && y < size * 0.82f))
                {
                    baseColor = Color.Lerp(baseColor, seed.colorA.grayscale > seed.colorB.grayscale ? seed.colorB : seed.colorA, 0.55f);
                }

                pixels[y * size + x] = baseColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    static int ComputeSeedHash(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < value.Length; i++)
            {
                hash = hash * 31 + value[i];
            }

            return hash & 0x7fffffff;
        }
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

    // ================================================================
    // PREFABS
    // ================================================================

    static Dictionary<string, GameObject> CreatePrefabs(List<ContentSeed> seeds)
    {
        var prefabs = new Dictionary<string, GameObject>();
        var createdNames = new HashSet<string>();

        for (int i = 0; i < seeds.Count; i++)
        {
            ContentSeed seed = seeds[i];
            if (createdNames.Contains(seed.prefabName))
            {
                prefabs[seed.prefabName] = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsFolder + "/" + seed.prefabName + ".prefab");
                continue;
            }

            createdNames.Add(seed.prefabName);
            GameObject root = BuildPrefabRoot(seed);
            string prefabPath = PrefabsFolder + "/" + seed.prefabName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            prefabs[seed.prefabName] = prefab;
        }

        return prefabs;
    }

    static GameObject BuildPrefabRoot(ContentSeed seed)
    {
        GameObject root = new GameObject(seed.prefabName.Replace("Prefab", string.Empty));
        root.transform.localScale = Vector3.one * 0.18f;
        string templateName = string.IsNullOrWhiteSpace(seed.prefabTemplateName) ? seed.prefabName : seed.prefabTemplateName;

        switch (templateName)
        {
            case "ApplePrefab":
                if (!TryBuildImportedModel(root, ImportedAppleModelPath, "Apple", seed.colorA, seed.colorB))
                {
                    BuildApple(root, seed.colorA, seed.colorB);
                }
                break;
            case "OrangePrefab":    BuildOrange(root, seed.colorA);                  break;
            case "BananaPrefab":    BuildBanana(root, seed.colorA);                  break;
            case "GrapesPrefab":    BuildGrapes(root, seed.colorA, seed.colorB);     break;
            case "ButterflyPrefab": BuildButterfly(root, seed.colorA, seed.colorB);  break;
            case "FishPrefab":      BuildFish(root, seed.colorA, seed.colorB);       break;
            case "BirdPrefab":      BuildBird(root, seed.colorA, seed.colorB);       break;
            case "CatPrefab":       BuildCat(root, seed.colorA, seed.colorB);        break;
        }

        return root;
    }

    static bool TryBuildImportedModel(GameObject root, string modelPath, string materialPrefix, Color bodyFallbackColor, Color accentFallbackColor)
    {
        if (root == null || string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            return false;
        }

        string textureFolder = GetImportedModelTextureFolder(modelPath);
        ImportedTextureSet textures = FindImportedTextureSet(textureFolder);
        EnsureImportedTextureImportSettings(textures);

        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            Debug.LogWarning("Imported FBX belum bisa di-load: " + modelPath);
            return false;
        }

        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (modelInstance == null)
        {
            Debug.LogWarning("Imported FBX gagal di-instantiate: " + modelPath);
            return false;
        }

        modelInstance.name = Path.GetFileNameWithoutExtension(modelPath);
        modelInstance.transform.SetParent(root.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        ApplyImportedModelMaterials(modelInstance, materialPrefix, textures, bodyFallbackColor, accentFallbackColor);
        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        FitImportedModelToPrefab(root.transform, modelInstance.transform, 1f);
        AddImportedModelCollider(root, modelInstance);
        return true;
    }

    struct ImportedTextureSet
    {
        public string baseColorPath;
        public string normalPath;
        public string roughnessPath;
    }

    static string GetImportedModelTextureFolder(string modelPath)
    {
        string directory = Path.GetDirectoryName(modelPath);
        string modelName = Path.GetFileNameWithoutExtension(modelPath);
        return string.IsNullOrEmpty(directory) ? modelName + ".fbm" : directory + "/" + modelName + ".fbm";
    }

    static ImportedTextureSet FindImportedTextureSet(string textureFolder)
    {
        ImportedTextureSet textures = default;
        if (string.IsNullOrWhiteSpace(textureFolder) || !AssetDatabase.IsValidFolder(textureFolder))
        {
            return textures;
        }

        AssetDatabase.ImportAsset(textureFolder, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ImportRecursive);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(textures.baseColorPath) &&
                (name.Contains("basecolor") || name.Contains("base_color") || name.Contains("albedo") ||
                 name.Contains("diffuse") || name.Contains("color")))
            {
                textures.baseColorPath = path;
                continue;
            }

            if (string.IsNullOrEmpty(textures.normalPath) &&
                (name.Contains("normal") || name.Contains("nrm")))
            {
                textures.normalPath = path;
                continue;
            }

            if (string.IsNullOrEmpty(textures.roughnessPath) &&
                (name.Contains("rough") || name.Contains("roughness")))
            {
                textures.roughnessPath = path;
            }
        }

        return textures;
    }

    static void EnsureImportedTextureImportSettings(ImportedTextureSet textures)
    {
        ConfigureTextureImporter(textures.baseColorPath, TextureImporterType.Default, false, true);
        ConfigureTextureImporter(textures.normalPath, TextureImporterType.NormalMap, true, false);
        ConfigureTextureImporter(textures.roughnessPath, TextureImporterType.Default, false, false);
    }

    static void ConfigureTextureImporter(string path, TextureImporterType textureType, bool normalMap, bool sRgb)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (importer.textureType != textureType)
        {
            importer.textureType = textureType;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.maxTextureSize != 1024)
        {
            importer.maxTextureSize = 1024;
            changed = true;
        }

        if (!normalMap && importer.sRGBTexture != sRgb)
        {
            importer.sRGBTexture = sRgb;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    static void ApplyImportedModelMaterials(GameObject modelInstance, string materialPrefix, ImportedTextureSet textures,
                                            Color bodyFallbackColor, Color accentFallbackColor)
    {
        if (modelInstance == null)
        {
            return;
        }

        string safePrefix = string.IsNullOrWhiteSpace(materialPrefix) ? modelInstance.name : materialPrefix.Trim();
        Material bodyMaterial = CreateImportedBodyMaterial(safePrefix + "BodyMaterial", textures, bodyFallbackColor);
        Material leafMaterial = CreateMaterial(safePrefix + "LeafMaterial", accentFallbackColor);
        Material stemMaterial = CreateMaterial(safePrefix + "StemMaterial", new Color32(113, 57, 28, 255));

        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string rendererName = renderer.name.ToLowerInvariant();
            Material material = bodyMaterial;
            if (rendererName.Contains("pucuk") || rendererName.Contains("leaf"))
            {
                material = leafMaterial;
            }
            else if (rendererName.Contains("cube") || rendererName.Contains("stem") || rendererName.Contains("tangkai"))
            {
                material = stemMaterial;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                continue;
            }

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    static Material CreateImportedBodyMaterial(string materialName, ImportedTextureSet textures, Color fallbackColor)
    {
        Material material = CreateMaterial(materialName, fallbackColor);
        Texture2D baseTexture = string.IsNullOrWhiteSpace(textures.baseColorPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Texture2D>(textures.baseColorPath);
        Texture2D normalTexture = string.IsNullOrWhiteSpace(textures.normalPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Texture2D>(textures.normalPath);

        if (baseTexture != null)
        {
            material.SetTexture("_BaseMap", baseTexture);
            material.SetTexture("_MainTex", baseTexture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
        }

        if (normalTexture != null)
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.EnableKeyword("_NORMALMAP");
        }

        material.SetFloat("_Smoothness", 0.28f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void FitImportedModelToPrefab(Transform root, Transform model, float targetMaxLocalDimension)
    {
        if (root == null || model == null)
        {
            return;
        }

        Bounds rawBounds = CalculateLocalRendererBounds(root, model.gameObject);
        float maxDimension = Mathf.Max(rawBounds.size.x, Mathf.Max(rawBounds.size.y, rawBounds.size.z));
        if (maxDimension > 0.0001f)
        {
            model.localScale = Vector3.one * (targetMaxLocalDimension / maxDimension);
        }

        Bounds fittedBounds = CalculateLocalRendererBounds(root, model.gameObject);
        model.localPosition += new Vector3(
            -fittedBounds.center.x,
            -fittedBounds.min.y + 0.025f,
            -fittedBounds.center.z);
    }

    static void AddImportedModelCollider(GameObject root, GameObject model)
    {
        Bounds bounds = CalculateLocalRendererBounds(root.transform, model);
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;
    }

    static Bounds CalculateLocalRendererBounds(Transform root, GameObject model)
    {
        Renderer[] renderers = model != null
            ? model.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();

        Matrix4x4 worldToLocal = root.worldToLocalMatrix;
        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds worldBounds = renderers[i].bounds;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
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

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    // ================================================================
    // SCRIPTABLE OBJECTS
    // ================================================================

    static MaterialContentLibrary CreateMaterialContentLibrary(List<ContentSeed> seeds, Dictionary<string, GameObject> prefabs)
    {
        MaterialContentLibrary library = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<MaterialContentLibrary>();
            AssetDatabase.CreateAsset(library, ContentLibraryPath);
        }

        SerializedObject libraryObject = new SerializedObject(library);
        SerializedProperty items = libraryObject.FindProperty("items");
        items.ClearArray();

        for (int i = 0; i < seeds.Count; i++)
        {
            ContentSeed seed = seeds[i];
            string assetPath = ScriptableObjectsFolder + "/" + seed.id + ".asset";
            MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(assetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<MaterialContentData>();
                AssetDatabase.CreateAsset(content, assetPath);
            }

            SerializedObject contentObject = new SerializedObject(content);
            contentObject.FindProperty("id").stringValue = seed.id;
            contentObject.FindProperty("category").enumValueIndex = (int)seed.category;
            contentObject.FindProperty("title").stringValue = seed.title;
            contentObject.FindProperty("subtitle").stringValue = seed.subtitle;
            contentObject.FindProperty("description").stringValue = seed.description;
            contentObject.FindProperty("referenceImageName").stringValue = seed.referenceImageName;
            contentObject.FindProperty("referenceImageTexture").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Texture2D>(ReferenceImagesFolder + "/" + seed.referenceImageName + ".png");
            contentObject.FindProperty("objectType").stringValue = seed.objectType;
            contentObject.FindProperty("colorFocus").stringValue = seed.colorFocus;
            contentObject.FindProperty("fontTypeFocus").stringValue = seed.fontTypeFocus;
            contentObject.FindProperty("targetWidthMeters").floatValue = 0.12f;
            contentObject.FindProperty("prefab").objectReferenceValue = prefabs[seed.prefabName];
            ApplyDefaultLearningExtras(contentObject, seed.title, seed.objectType, seed.fontTypeFocus, (int)seed.category);
            contentObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);

            int arrayIndex = items.arraySize;
            items.InsertArrayElementAtIndex(arrayIndex);
            items.GetArrayElementAtIndex(arrayIndex).objectReferenceValue = content;
        }

        libraryObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
        return library;
    }

    static bool ApplyDefaultLearningExtras(SerializedObject contentObject, string title, string objectType, string featureFocus, int categoryIndex)
    {
        bool changed = false;
        string safeTitle = string.IsNullOrWhiteSpace(title) ? "Objek" : title.Trim();
        string safeObjectType = string.IsNullOrWhiteSpace(objectType) ? safeTitle : objectType.Trim();
        string safeFeature = string.IsNullOrWhiteSpace(featureFocus) ? "bentuknya yang mudah dikenali" : featureFocus.Trim();

        SerializedProperty funFactProperty = contentObject.FindProperty("funFact");
        if (funFactProperty != null && string.IsNullOrWhiteSpace(funFactProperty.stringValue))
        {
            funFactProperty.stringValue = "Fakta menarik: " + safeTitle + " mudah dikenali dari " + LowerFirst(safeFeature) + ".";
            changed = true;
        }

        SerializedProperty questionProperty = contentObject.FindProperty("quizQuestions");
        if (questionProperty != null && IsStringArrayEmpty(questionProperty))
        {
            SetStringArray(questionProperty, new[] { "Apa nama objek pada kartu ini?" });
            changed = true;
        }

        SerializedProperty answerProperty = contentObject.FindProperty("quizAnswers");
        if (answerProperty != null && IsStringArrayEmpty(answerProperty))
        {
            SetStringArray(answerProperty, new[] { safeTitle });
            changed = true;
        }

        SerializedProperty wrongOptionsProperty = contentObject.FindProperty("quizWrongOptions");
        if (wrongOptionsProperty != null && IsStringArrayEmpty(wrongOptionsProperty))
        {
            SetStringArray(wrongOptionsProperty, BuildDefaultWrongOptions(safeObjectType, categoryIndex));
            changed = true;
        }

        return changed;
    }

    static string[] BuildDefaultWrongOptions(string objectType, int categoryIndex)
    {
        string[] fruitOptions = { "Jeruk", "Pisang", "Anggur", "Ceri" };
        string[] animalOptions = { "Kucing", "Kupu-kupu", "Burung", "Ikan" };
        string[] source = categoryIndex == (int)LearningCategory.Typography ? fruitOptions : animalOptions;

        var options = new List<string>();
        for (int i = 0; i < source.Length; i++)
        {
            if (MaterialContentKeyUtility.Normalize(source[i]) != MaterialContentKeyUtility.Normalize(objectType))
            {
                options.Add(source[i]);
            }
        }

        return options.ToArray();
    }

    static bool IsStringArrayEmpty(SerializedProperty property)
    {
        if (property == null || !property.isArray || property.arraySize == 0)
        {
            return true;
        }

        for (int i = 0; i < property.arraySize; i++)
        {
            if (!string.IsNullOrWhiteSpace(property.GetArrayElementAtIndex(i).stringValue))
            {
                return false;
            }
        }

        return true;
    }

    static void SetStringArray(SerializedProperty property, string[] values)
    {
        property.ClearArray();
        for (int i = 0; i < values.Length; i++)
        {
            property.InsertArrayElementAtIndex(i);
            property.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }

    static string LowerFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length == 1)
        {
            return value.ToLowerInvariant();
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    static QuizQuestionBank CreateQuizBank()
    {
        QuizQuestionBank bank = AssetDatabase.LoadAssetAtPath<QuizQuestionBank>(QuizBankPath);
        if (bank == null)
        {
            bank = ScriptableObject.CreateInstance<QuizQuestionBank>();
            AssetDatabase.CreateAsset(bank, QuizBankPath);
        }

        SerializedObject bankObject = new SerializedObject(bank);
        SerializedProperty questions = bankObject.FindProperty("questions");
        questions.ClearArray();

        AddQuestion(questions,
                    "Buah mana yang bentuknya panjang dan biasanya berkulit kuning?",
                    new[] { "Pisang", "Anggur", "Apel", "Jeruk" },
                    0, "Pisang mudah dikenali dari bentuknya yang panjang dan kulitnya yang kuning saat matang.");
        AddQuestion(questions,
                    "Hewan mana yang memiliki dua sayap tipis dan sering hinggap di bunga?",
                    new[] { "Kupu-kupu", "Kucing", "Ikan badut", "Burung" },
                    0, "Kupu-kupu memiliki sayap lebar yang tipis dan sering terlihat di sekitar bunga.");
        AddQuestion(questions,
                    "Buah mana yang tumbuh dalam satu tangkai dengan banyak butiran kecil?",
                    new[] { "Anggur", "Pisang", "Jeruk", "Apel" },
                    0, "Anggur tumbuh bergerombol dalam satu tangkai kecil.");
        AddQuestion(questions,
                    "Hewan mana yang hidup di air dan bergerak memakai sirip?",
                    new[] { "Ikan badut", "Burung pastel", "Kucing oren", "Kupu-kupu ceria" },
                    0, "Ikan badut adalah hewan air yang bergerak menggunakan sirip.");
        AddQuestion(questions,
                    "Buah mana yang biasanya bulat dan punya tangkai kecil di bagian atas?",
                    new[] { "Apel", "Pisang", "Anggur", "Kupu-kupu" },
                    0, "Apel memiliki bentuk bulat dengan tangkai kecil di bagian atas buah.");

        bankObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bank);
        return bank;
    }

    static void AddQuestion(SerializedProperty questions, string question, string[] options, int correctIndex, string explanation)
    {
        int questionIndex = questions.arraySize;
        questions.InsertArrayElementAtIndex(questionIndex);

        SerializedProperty questionProperty = questions.GetArrayElementAtIndex(questionIndex);
        questionProperty.FindPropertyRelative("question").stringValue = question;
        questionProperty.FindPropertyRelative("correctIndex").intValue = correctIndex;
        questionProperty.FindPropertyRelative("explanation").stringValue = explanation;

        SerializedProperty optionsProperty = questionProperty.FindPropertyRelative("options");
        optionsProperty.arraySize = options.Length;
        for (int i = 0; i < options.Length; i++)
            optionsProperty.GetArrayElementAtIndex(i).stringValue = options[i];
    }

    // ================================================================
    // SCENE: SPLASH
    // Clean centered brand identity. No buttons needed.
    // ================================================================

    static void CreateSplashScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("FallbackBackground", canvas.transform, BgPage);

        GameObject artworkRoot = new GameObject("SplashArtworkRoot", typeof(RectTransform), typeof(CanvasGroup));
        artworkRoot.transform.SetParent(canvas.transform, false);
        StretchToParent(artworkRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        UnityEngine.UI.Image portraitArtwork = CreateFullscreenImage("SplashPortrait", artworkRoot.transform, LoadUiSprite(SplashPortraitPath));
        UnityEngine.UI.Image landscapeArtwork = CreateFullscreenImage("SplashLandscape", artworkRoot.transform, LoadUiSprite(SplashLandscapePath));

        GameObject controllerObject = new GameObject("SplashController");
        SplashController controller = controllerObject.AddComponent<SplashController>();
        controller.ConfigureArtwork(artworkRoot.GetComponent<RectTransform>(), portraitArtwork, landscapeArtwork);
        EditorUtility.SetDirty(controller);

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/SplashScene.unity");
    }

    // ================================================================
    // SCENE: MAIN MENU
    // Full-width nav buttons. 96px min touch target.
    // Clear brand header. One action per button.
    // ================================================================

    static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("FallbackBackground", canvas.transform, BgPage);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();

        GameObject portraitRoot = CreateMainMenuArtworkRoot(
            "MainMenuPortraitRoot",
            canvas.transform,
            LoadUiSprite(MainMenuPortraitPath),
            navigation,
            new Vector2(0.205f, 0.508f), new Vector2(0.790f, 0.618f),
            new Vector2(0.205f, 0.400f), new Vector2(0.790f, 0.508f),
            new Vector2(0.205f, 0.292f), new Vector2(0.790f, 0.400f),
            new Vector2(0.205f, 0.188f), new Vector2(0.790f, 0.292f));

        GameObject landscapeRoot = CreateMainMenuArtworkRoot(
            "MainMenuLandscapeRoot",
            canvas.transform,
            LoadUiSprite(MainMenuLandscapePath),
            navigation,
            new Vector2(0.272f, 0.566f), new Vector2(0.728f, 0.691f),
            new Vector2(0.272f, 0.450f), new Vector2(0.728f, 0.575f),
            new Vector2(0.272f, 0.335f), new Vector2(0.728f, 0.459f),
            new Vector2(0.272f, 0.218f), new Vector2(0.728f, 0.344f));

        ResponsiveLayoutController responsive = canvas.gameObject.AddComponent<ResponsiveLayoutController>();
        responsive.Configure(portraitRoot, landscapeRoot);
        EditorUtility.SetDirty(responsive);

        CreateTopRightMusicToggle(canvas.transform);

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/MainMenuScene.unity");
    }

    // ================================================================
    // SCENE: GUIDE
    // Single-purpose page. Clear heading. Readable body. One CTA.
    // ================================================================

    static void CreateGuideScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("FallbackBackground", canvas.transform, BgPage);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();

        GameObject portraitRoot = CreateGuideArtworkRoot(
            "GuidePortraitRoot",
            canvas.transform,
            LoadUiSprite(GuidePortraitPath),
            navigation,
            new Vector2(0.183f, 0.143f),
            new Vector2(0.815f, 0.232f));

        GameObject landscapeRoot = CreateGuideArtworkRoot(
            "GuideLandscapeRoot",
            canvas.transform,
            LoadUiSprite(GuideLandscapePath),
            navigation,
            new Vector2(0.240f, 0.159f),
            new Vector2(0.762f, 0.295f));

        ResponsiveLayoutController responsive = canvas.gameObject.AddComponent<ResponsiveLayoutController>();
        responsive.Configure(portraitRoot, landscapeRoot);
        EditorUtility.SetDirty(responsive);

        CreateTopRightMusicToggle(canvas.transform);

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/GuideScene.unity");
    }

    // ================================================================
    // SCENE: MATERIAL SELECT
    // Repurposed as a short tutorial / quick-start page before AR camera.
    // The app now uses one simple "Buah & Hewan" path so children go to camera fast.
    // ================================================================

    static void CreateMaterialSelectScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("FallbackBackground", canvas.transform, BgPage);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        PlayModeOptionsController playOptions = canvas.gameObject.AddComponent<PlayModeOptionsController>();

        GameObject portraitRoot = CreateMaterialGuideArtworkRoot(
            "MaterialGuidePortraitRoot",
            canvas.transform,
            LoadUiSprite(MaterialGuidePortraitPath),
            navigation,
            playOptions,
            new Vector2(0.180f, 0.150f),
            new Vector2(0.820f, 0.270f),
            new Vector2(0.065f, 0.880f),
            new Vector2(0.315f, 0.955f));

        GameObject landscapeRoot = CreateMaterialGuideArtworkRoot(
            "MaterialGuideLandscapeRoot",
            canvas.transform,
            LoadUiSprite(MaterialGuideLandscapePath),
            navigation,
            playOptions,
            new Vector2(0.235f, 0.170f),
            new Vector2(0.765f, 0.330f),
            new Vector2(0.040f, 0.865f),
            new Vector2(0.185f, 0.955f));

        ResponsiveLayoutController responsive = canvas.gameObject.AddComponent<ResponsiveLayoutController>();
        responsive.Configure(portraitRoot, landscapeRoot);
        EditorUtility.SetDirty(responsive);

        CreateTopRightMusicToggle(canvas.transform);
        CreatePlayModeOptionsOverlay(canvas.transform, navigation, playOptions);

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/MaterialSelectScene.unity");
    }

    // ================================================================
    // SCENE: AR SCAN
    // AR-first layout. Minimal chrome overlay.
    // Status bar top, info panel bottom.
    // All touch targets >= 88px.
    // ================================================================

    static void CreateARScanScene(MaterialContentLibrary contentLibrary)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateBackgroundMusicHost();

        GameObject vuforiaRoot = new GameObject("VuforiaRoot");
        Camera arCamera = CreateARCamera(vuforiaRoot.transform);
        VuforiaBehaviour vuforiaBehaviour = arCamera.GetComponent<VuforiaBehaviour>();

        GameObject contentRoot = new GameObject("ImageTargetsRoot");
        contentRoot.transform.SetParent(vuforiaRoot.transform, false);
        GameObject managers = new GameObject("Managers");

        SceneNavigationController navigation = managers.AddComponent<SceneNavigationController>();
        UIOverlayController overlay = managers.AddComponent<UIOverlayController>();
        MaterialContentController contentController = managers.AddComponent<MaterialContentController>();
        ARImageTrackingController trackingController = managers.AddComponent<ARImageTrackingController>();
        AudioSource learningAudioSource = managers.AddComponent<AudioSource>();
        ARLearningFeedbackController learningFeedback = managers.AddComponent<ARLearningFeedbackController>();
        learningAudioSource.playOnAwake = false;
        learningAudioSource.spatialBlend = 0f;
        learningAudioSource.volume = 1f;

        contentRoot.transform.position = Vector3.zero;
        managers.transform.position = Vector3.zero;

        CreateDirectionalLight();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas_Overlay");

        // Top bar - darker overlay for AR context
        GameObject topBar = CreatePanel("TopBar", canvas.transform, OverlayDark);
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 0.9f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.offsetMin = Vector2.zero;
        topBarRect.offsetMax = Vector2.zero;

        // Title centered
        Text topTitle = CreateText("TopTitle", topBar.transform, 30, FontStyle.Bold, TextAnchor.MiddleCenter,
                                   "Scan AR", Color.white);
        StretchCenter(topTitle.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(300f, 64f));

        Text contextText = CreateText("ContextText", topBar.transform, 18, FontStyle.Normal, TextAnchor.MiddleCenter,
                                      "Mode scan semua materi", new Color(1f, 1f, 1f, 0.82f));
        StretchCenter(contextText.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(420f, 36f));

        // Back button - min 88px touch target
        ButtonParts backButton = CreateButton("BackButton", topBar.transform, "Kembali", Accent, Color.white);
        AnchorToCorner(backButton.button.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(192f, 72f));
        backButton.label.fontSize = 26;
        UnityEventTools.AddPersistentListener(backButton.button.onClick, navigation.OpenARBackDestination);

        // Help button - right side
        ButtonParts helpButton = CreateButton("HelpButton", topBar.transform, "Bantuan", Primary, Color.white);
        AnchorToCorner(helpButton.button.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(192f, 72f));
        helpButton.label.fontSize = 26;

        CreateMusicToggleButton(topBar.transform, new Vector2(1f, 0.5f), new Vector2(-232f, 0f), new Vector2(168f, 72f));

        // Status strip - compact, just below top bar
        GameObject statusPanel = CreatePanel("StatusPanel", canvas.transform, OverlayMid);
        RectTransform statusPanelRect = statusPanel.GetComponent<RectTransform>();
        statusPanelRect.anchorMin = new Vector2(0.1f, 0.83f);
        statusPanelRect.anchorMax = new Vector2(0.9f, 0.89f);
        statusPanelRect.offsetMin = Vector2.zero;
        statusPanelRect.offsetMax = Vector2.zero;

        Text statusText = CreateText("StatusText", statusPanel.transform, 24, FontStyle.Normal, TextAnchor.MiddleCenter,
                                     "Arahkan kamera ke kartu referensi atau marker card demo.", Color.white);
        StretchToParent(statusText.rectTransform, new Vector2(20f, 8f), new Vector2(-20f, -8f));

        // Scan guide - center frame hint
        GameObject guidePanel = CreatePanel("ScanGuideFrame", canvas.transform, OverlayLight);
        RectTransform guideRect = guidePanel.GetComponent<RectTransform>();
        guideRect.anchorMin = new Vector2(0.12f, 0.4f);
        guideRect.anchorMax = new Vector2(0.88f, 0.54f);
        guideRect.offsetMin = Vector2.zero;
        guideRect.offsetMax = Vector2.zero;

        Text scanGuide = CreateText("ScanGuide", guidePanel.transform, 26, FontStyle.Normal, TextAnchor.MiddleCenter,
                                    "Arahkan kamera ke kartu marker buah atau hewan.\nPastikan kartu penuh di frame kamera dan pencahayaan cukup.", Color.white);
        StretchToParent(scanGuide.rectTransform, new Vector2(24f, 16f), new Vector2(-24f, -16f));

        // Info panel - bottom sheet style, 30% height
        GameObject infoPanel = CreatePanel("InfoPanel", canvas.transform, new Color(1f, 1f, 1f, 0.97f));
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.0f, 0.0f);
        infoRect.anchorMax = new Vector2(1.0f, 0.34f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;
        AddCardShadow(infoPanel);

        // Category label - small, colored
        Text categoryText = CreateText("CategoryText", infoPanel.transform, 20, FontStyle.Bold, TextAnchor.UpperLeft,
                                       "", Primary);
        AnchorFill(categoryText.rectTransform,
                   new Vector2(28f, -18f), new Vector2(-28f, -10f),
                   new Vector2(0f, 1f), new Vector2(1f, 1f),
                   new Vector2(0f, 1f));

        // Title - strong
        Text titleText = CreateText("TitleText", infoPanel.transform, 38, FontStyle.Bold, TextAnchor.UpperLeft,
                                    "", TextPrimary);
        AnchorFill(titleText.rectTransform,
                   new Vector2(28f, -52f), new Vector2(-28f, -18f),
                   new Vector2(0f, 1f), new Vector2(1f, 1f),
                   new Vector2(0f, 1f));

        // Subtitle - accent color
        Text subtitleText = CreateText("SubtitleText", infoPanel.transform, 24, FontStyle.Italic, TextAnchor.UpperLeft,
                                       "", Accent);
        AnchorFill(subtitleText.rectTransform,
                   new Vector2(28f, -96f), new Vector2(-28f, -52f),
                   new Vector2(0f, 1f), new Vector2(1f, 1f),
                   new Vector2(0f, 1f));

        // Description - body text
        Text descriptionText = CreateText("DescriptionText", infoPanel.transform, 24, FontStyle.Normal, TextAnchor.UpperLeft,
                                          "", TextSecond);
        AnchorFill(descriptionText.rectTransform,
                   new Vector2(28f, -146f), new Vector2(-28f, -100f),
                   new Vector2(0f, 1f), new Vector2(1f, 1f),
                   new Vector2(0f, 1f), new Vector2(0f, 1f));

        // Object type - small muted bottom label
        Text objectTypeText = CreateText("ObjectTypeText", infoPanel.transform, 20, FontStyle.Bold, TextAnchor.LowerLeft,
                                         "", TextMuted);
        AnchorFill(objectTypeText.rectTransform,
                   new Vector2(28f, 20f), new Vector2(-28f, 20f),
                   new Vector2(0f, 0f), new Vector2(1f, 0f),
                   new Vector2(0f, 0f));

        Text focusText = CreateText("FocusText", infoPanel.transform, 20, FontStyle.Bold, TextAnchor.LowerLeft,
                                    "", Accent);
        AnchorFill(focusText.rectTransform,
                   new Vector2(28f, 52f), new Vector2(-28f, 52f),
                   new Vector2(0f, 0f), new Vector2(1f, 0f),
                   new Vector2(0f, 0f));

        // Info toggle - floating button, anchored right beside info panel
        ButtonParts infoToggleButton = CreateButton("InfoToggleButton", canvas.transform, "Info", Accent, Color.white);
        RectTransform infoToggleRect = infoToggleButton.button.GetComponent<RectTransform>();
        infoToggleRect.anchorMin = new Vector2(0.78f, 0.35f);
        infoToggleRect.anchorMax = new Vector2(0.96f, 0.42f);
        infoToggleRect.offsetMin = Vector2.zero;
        infoToggleRect.offsetMax = Vector2.zero;
        infoToggleButton.label.fontSize = 26;

        // Learning feedback: Quiz Hunt CTA, star badge, and fun fact popup.
        ButtonParts quizButton = CreateButton("QuizButton", canvas.transform, "Quiz Hunt", Success, Color.white);
        RectTransform quizButtonRect = quizButton.button.GetComponent<RectTransform>();
        quizButtonRect.anchorMin = new Vector2(0.04f, 0.35f);
        quizButtonRect.anchorMax = new Vector2(0.28f, 0.42f);
        quizButtonRect.offsetMin = Vector2.zero;
        quizButtonRect.offsetMax = Vector2.zero;
        quizButton.label.fontSize = 24;
        AddCardShadow(quizButton.button.gameObject);
        quizButton.button.gameObject.SetActive(false);

        GameObject starBadge = CreatePanel("StarBadge", canvas.transform, new Color(1f, 0.85f, 0.18f, 0.95f));
        RectTransform starBadgeRect = starBadge.GetComponent<RectTransform>();
        starBadgeRect.anchorMin = new Vector2(0.08f, 0.455f);
        starBadgeRect.anchorMax = new Vector2(0.50f, 0.525f);
        starBadgeRect.offsetMin = Vector2.zero;
        starBadgeRect.offsetMax = Vector2.zero;
        AddCardShadow(starBadge);

        Text starBadgeText = CreateText("StarBadgeText", starBadge.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter,
                                        "Bintang didapat", TextPrimary);
        StretchToParent(starBadgeText.rectTransform, new Vector2(16f, 6f), new Vector2(-16f, -6f));
        starBadge.SetActive(false);

        GameObject funFactPanel = CreatePanel("FunFactPanel", canvas.transform, new Color(1f, 0.95f, 0.78f, 0.96f));
        RectTransform funFactRect = funFactPanel.GetComponent<RectTransform>();
        funFactRect.anchorMin = new Vector2(0.08f, 0.58f);
        funFactRect.anchorMax = new Vector2(0.92f, 0.75f);
        funFactRect.offsetMin = Vector2.zero;
        funFactRect.offsetMax = Vector2.zero;
        AddCardShadow(funFactPanel);

        Text funFactText = CreateText("FunFactText", funFactPanel.transform, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
                                      "", TextPrimary);
        StretchToParent(funFactText.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f));
        funFactPanel.SetActive(false);

        // Help overlay panel
        GameObject helpPanel = CreatePanel("HelpPanel", canvas.transform, new Color(0.02f, 0.06f, 0.16f, 0.88f));
        RectTransform helpRect = helpPanel.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(0.08f, 0.22f);
        helpRect.anchorMax = new Vector2(0.92f, 0.78f);
        helpRect.offsetMin = Vector2.zero;
        helpRect.offsetMax = Vector2.zero;
        helpPanel.SetActive(false);

        Text helpText = CreateText("HelpText", helpPanel.transform, 26, FontStyle.Normal, TextAnchor.MiddleCenter,
                                   "Gunakan kartu marker buah atau hewan sebagai target scan.\n\nContoh target:\napel, jeruk, kucing, atau kupu-kupu\n\nPastikan pencahayaan cukup, kartu tidak blur, dan seluruh marker masuk frame kamera.",
                                   Color.white);
        StretchToParent(helpText.rectTransform, new Vector2(40f, 48f), new Vector2(-40f, -136f));

        // Close help button - 88px
        ButtonParts closeHelpButton = CreateButton("CloseHelpButton", helpPanel.transform, "Tutup", Accent, Color.white);
        RectTransform closeHelpRect = closeHelpButton.button.GetComponent<RectTransform>();
        closeHelpRect.anchorMin = new Vector2(0.3f, 0.08f);
        closeHelpRect.anchorMax = new Vector2(0.7f, 0.2f);
        closeHelpRect.offsetMin = Vector2.zero;
        closeHelpRect.offsetMax = Vector2.zero;
        closeHelpButton.label.fontSize = 28;

        UnityEventTools.AddPersistentListener(helpButton.button.onClick, overlay.ToggleHelpPanel);
        UnityEventTools.AddPersistentListener(closeHelpButton.button.onClick, overlay.ToggleHelpPanel);
        UnityEventTools.AddPersistentListener(infoToggleButton.button.onClick, overlay.ToggleInfoPanel);
        UnityEventTools.AddPersistentListener(quizButton.button.onClick, learningFeedback.OnQuizButtonPressed);

        SerializedObject overlayObject = new SerializedObject(overlay);
        overlayObject.FindProperty("statusText").objectReferenceValue = statusText;
        overlayObject.FindProperty("titleText").objectReferenceValue = titleText;
        overlayObject.FindProperty("subtitleText").objectReferenceValue = subtitleText;
        overlayObject.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        overlayObject.FindProperty("categoryText").objectReferenceValue = categoryText;
        overlayObject.FindProperty("objectTypeText").objectReferenceValue = objectTypeText;
        overlayObject.FindProperty("focusText").objectReferenceValue = focusText;
        overlayObject.FindProperty("contextText").objectReferenceValue = contextText;
        overlayObject.FindProperty("infoPanel").objectReferenceValue = infoPanel;
        overlayObject.FindProperty("scanGuide").objectReferenceValue = guidePanel;
        overlayObject.FindProperty("helpPanel").objectReferenceValue = helpPanel;
        overlayObject.FindProperty("overlayCanvas").objectReferenceValue = canvas;
        overlayObject.FindProperty("topBarRect").objectReferenceValue = topBarRect;
        overlayObject.FindProperty("statusPanelRect").objectReferenceValue = statusPanelRect;
        overlayObject.FindProperty("scanGuideRect").objectReferenceValue = guideRect;
        overlayObject.FindProperty("infoPanelRect").objectReferenceValue = infoRect;
        overlayObject.FindProperty("infoToggleButtonRect").objectReferenceValue = infoToggleRect;
        overlayObject.FindProperty("helpPanelRect").objectReferenceValue = helpRect;
        overlayObject.FindProperty("backButtonRect").objectReferenceValue = backButton.button.GetComponent<RectTransform>();
        overlayObject.FindProperty("helpButtonRect").objectReferenceValue = helpButton.button.GetComponent<RectTransform>();
        overlayObject.FindProperty("musicButtonRect").objectReferenceValue = GameObject.Find("MusicToggleButton")?.GetComponent<RectTransform>();
        overlayObject.FindProperty("topTitleText").objectReferenceValue = topTitle;
        overlayObject.FindProperty("scanGuideText").objectReferenceValue = scanGuide;
        overlayObject.FindProperty("helpText").objectReferenceValue = helpText;
        overlayObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject learningObject = new SerializedObject(learningFeedback);
        learningObject.FindProperty("audioSource").objectReferenceValue = learningAudioSource;
        learningObject.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        learningObject.FindProperty("funFactPanel").objectReferenceValue = funFactPanel;
        learningObject.FindProperty("funFactText").objectReferenceValue = funFactText;
        learningObject.FindProperty("quizButton").objectReferenceValue = quizButton.button.gameObject;
        learningObject.FindProperty("quizButtonLabel").objectReferenceValue = quizButton.label;
        learningObject.FindProperty("starIcon").objectReferenceValue = starBadge;
        learningObject.FindProperty("starCountText").objectReferenceValue = starBadgeText;
        learningObject.FindProperty("navigator").objectReferenceValue = navigation;
        learningObject.FindProperty("quizButtonRect").objectReferenceValue = quizButtonRect;
        learningObject.FindProperty("funFactPanelRect").objectReferenceValue = funFactRect;
        learningObject.FindProperty("starIconRect").objectReferenceValue = starBadgeRect;
        learningObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject contentObject = new SerializedObject(contentController);
        contentObject.FindProperty("library").objectReferenceValue = contentLibrary;
        contentObject.FindProperty("overlayController").objectReferenceValue = overlay;
        contentObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject trackingObject = new SerializedObject(trackingController);
        trackingObject.FindProperty("vuforiaBehaviour").objectReferenceValue = vuforiaBehaviour;
        trackingObject.FindProperty("contentController").objectReferenceValue = contentController;
        trackingObject.FindProperty("learningFeedback").objectReferenceValue = learningFeedback;
        trackingObject.FindProperty("library").objectReferenceValue = contentLibrary;
        trackingObject.FindProperty("targetsRoot").objectReferenceValue = contentRoot.transform;
        trackingObject.FindProperty("useImportedVuforiaDeviceDatabase").boolValue = true;
        trackingObject.FindProperty("strictImportedVuforiaDeviceDatabaseOnly").boolValue = true;
        trackingObject.FindProperty("importedVuforiaDeviceDatabasePath").stringValue = "Vuforia/MediaBelajarAR_DB.xml";
        trackingObject.FindProperty("importedVuforiaDeviceDatabaseTargets").stringValue =
            string.Join(",", ARtiGrafContentMaintenance.BuildImportedVuforiaTargetList(contentLibrary));
        trackingObject.FindProperty("enableBarcodeFallback").boolValue = false;
        trackingObject.FindProperty("alwaysCreateBarcodeObserverOnMobile").boolValue = false;
        trackingObject.FindProperty("preferTrackedImageTargetsOverBarcodeResults").boolValue = true;
        trackingObject.FindProperty("requireTrackedStatusForBarcodeSwitch").boolValue = true;
        trackingObject.FindProperty("barcodeSwitchCooldownSeconds").floatValue = 0.35f;
        trackingObject.FindProperty("forceBarcodeOnlyScanning").boolValue = false;
        trackingObject.FindProperty("stabilizeImageTargets").boolValue = true;
        trackingObject.FindProperty("freezePoseWhileLimited").boolValue = true;
        trackingObject.FindProperty("showRuntimeTrackingDiagnostics").boolValue = true;
        trackingObject.FindProperty("imageTargetLostGraceSeconds").floatValue = 0.55f;
        trackingObject.FindProperty("positionSmoothTime").floatValue = 0.18f;
        trackingObject.FindProperty("rotationLerpSpeed").floatValue = 6.5f;
        trackingObject.FindProperty("positionSnapDistanceMeters").floatValue = 0.34f;
        trackingObject.FindProperty("rotationSnapAngleDegrees").floatValue = 48f;
        trackingObject.FindProperty("positionJitterDeadzoneMeters").floatValue = 0.009f;
        trackingObject.FindProperty("rotationJitterDeadzoneDegrees").floatValue = 3.5f;
        trackingObject.FindProperty("minimumAnchorUpdateIntervalSeconds").floatValue = 0.025f;
        trackingObject.FindProperty("imageTargetContentOffset").vector3Value = new Vector3(0f, 0.035f, 0f);
        trackingObject.ApplyModifiedPropertiesWithoutUndo();

        infoPanel.SetActive(false);
        EditorUtility.SetDirty(overlay);
        EditorUtility.SetDirty(learningFeedback);
        EditorUtility.SetDirty(learningAudioSource);
        EditorUtility.SetDirty(contentController);
        EditorUtility.SetDirty(trackingController);
        EditorSceneManager.MarkSceneDirty(scene);

        string scenePath = ScenesFolder + "/ARScanScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        ForceSceneLibraryReferences(scenePath);
    }

    // ================================================================
    // SCENE: QUIZ
    // Clear question area. Large option buttons (96px).
    // Progress visible. Feedback inline.
    // ================================================================

    static void CreateQuizScene(QuizQuestionBank quizBank)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("Background", canvas.transform, BgPage);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        QuizManager quizManager = canvas.gameObject.AddComponent<QuizManager>();

        // Title
        Text title = CreateText("Title", canvas.transform, 52, FontStyle.Bold, TextAnchor.MiddleCenter,
                                "Quiz Buah dan Hewan", TextPrimary);
        StretchCenter(title.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(800f, 88f));

        // Progress indicator - prominent
        Text progressText = CreateText("ProgressText", canvas.transform, 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                                       "", Primary);
        StretchCenter(progressText.rectTransform, new Vector2(0.5f, 0.84f), new Vector2(300f, 56f));

        // Quiz card
        GameObject panel = CreatePanel("QuizPanel", canvas.transform, BgCard);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.15f);
        panelRect.anchorMax = new Vector2(0.94f, 0.78f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        AddCardShadow(panel);

        // Question text - generous padding
        Text questionText = CreateText("QuestionText", panel.transform, 34, FontStyle.Bold, TextAnchor.UpperLeft,
                                       "", TextPrimary);
        AnchorFill(questionText.rectTransform,
                   new Vector2(32f, -32f), new Vector2(-32f, -24f),
                   new Vector2(0f, 1f), new Vector2(1f, 1f),
                   new Vector2(0f, 1f), new Vector2(0f, 1f));

        // Options vertical list
        GameObject optionsRoot = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
        optionsRoot.transform.SetParent(panel.transform, false);
        RectTransform optionsRect = optionsRoot.GetComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0.05f, 0.2f);
        optionsRect.anchorMax = new Vector2(0.95f, 0.64f);
        optionsRect.offsetMin = Vector2.zero;
        optionsRect.offsetMax = Vector2.zero;
        VerticalLayoutGroup optionsLayout = optionsRoot.GetComponent<VerticalLayoutGroup>();
        optionsLayout.spacing = 16f;
        optionsLayout.childAlignment = TextAnchor.UpperCenter;
        optionsLayout.childControlHeight = false;
        optionsLayout.childForceExpandHeight = false;
        optionsLayout.childControlWidth = true;

        // Option buttons - 96px touch target
        Button[] optionButtons = new Button[4];
        Text[] optionLabels = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            ButtonParts parts = CreateButton("Option" + i, optionsRoot.transform, "Pilihan " + (i + 1), PrimaryLight, Primary);
            optionButtons[i] = parts.button;
            optionLabels[i] = parts.label;
            parts.label.fontSize = 28;
            RectTransform buttonRect = parts.button.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 96f);
        }

        // Feedback - bottom of card
        Text feedbackText = CreateText("FeedbackText", panel.transform, 26, FontStyle.Italic, TextAnchor.LowerLeft,
                                       "", Accent);
        AnchorFill(feedbackText.rectTransform,
                   new Vector2(32f, 28f), new Vector2(-32f, 28f),
                   new Vector2(0f, 0f), new Vector2(1f, 0f),
                   new Vector2(0f, 0f));

        // Back button - bottom left, clear but not dominant
        ButtonParts backButton = CreateButton("BackButton", canvas.transform, "Pilih Mode", PrimaryLight, Primary);
        RectTransform backRect = backButton.button.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.06f, 0.05f);
        backRect.anchorMax = new Vector2(0.30f, 0.12f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;
        backButton.label.fontSize = 26;
        UnityEventTools.AddPersistentListener(backButton.button.onClick, navigation.OpenMaterialSelect);

        CreateTopRightMusicToggle(canvas.transform);

        SerializedObject quizObject = new SerializedObject(quizManager);
        quizObject.FindProperty("questionBank").objectReferenceValue = quizBank;
        quizObject.FindProperty("contentLibrary").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        quizObject.FindProperty("questionText").objectReferenceValue = questionText;
        quizObject.FindProperty("progressText").objectReferenceValue = progressText;
        quizObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;

        SerializedProperty buttonsProperty = quizObject.FindProperty("optionButtons");
        buttonsProperty.arraySize = optionButtons.Length;
        SerializedProperty labelsProperty = quizObject.FindProperty("optionLabels");
        labelsProperty.arraySize = optionLabels.Length;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = optionButtons[i];
            labelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = optionLabels[i];
        }
        quizObject.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int answerIndex = i;
            UnityEventTools.AddIntPersistentListener(optionButtons[i].onClick, quizManager.SelectAnswer, answerIndex);
        }

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/QuizScene.unity");
    }

    // ================================================================
    // SCENE: RESULT
        // Big score number. Clear percentage. Strong next-step CTA stack.
        // ================================================================

    static void CreateResultScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("Background", canvas.transform, BgPage);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        ResultSceneController resultController = canvas.gameObject.AddComponent<ResultSceneController>();

        Text title = CreateText("Title", canvas.transform, 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                                "Hasil Quiz", TextPrimary);
        StretchCenter(title.rectTransform, new Vector2(0.5f, 0.87f), new Vector2(520f, 96f));

        GameObject panel = CreatePanel("ResultPanel", canvas.transform, BgCard);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.28f);
        panelRect.anchorMax = new Vector2(0.9f, 0.76f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        AddCardShadow(panel);

        // Score - large, primary color
        Text scoreText = CreateText("ScoreText", panel.transform, 80, FontStyle.Bold, TextAnchor.MiddleCenter, "", Primary);
        StretchCenter(scoreText.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(480f, 120f));

        Text summaryText = CreateText("SummaryText", panel.transform, 28, FontStyle.Bold, TextAnchor.MiddleCenter, "", Accent);
        StretchCenter(summaryText.rectTransform, new Vector2(0.5f, 0.60f), new Vector2(420f, 56f));

        // Feedback - body, centered
        Text feedbackText = CreateText("FeedbackText", panel.transform, 30, FontStyle.Normal, TextAnchor.MiddleCenter,
                                       "", TextSecond);
        StretchCenter(feedbackText.rectTransform, new Vector2(0.5f, 0.43f), new Vector2(600f, 110f));

        Text nextStepText = CreateText("NextStepText", panel.transform, 24, FontStyle.Normal, TextAnchor.MiddleCenter,
                                       "", TextMuted);
        StretchCenter(nextStepText.rectTransform, new Vector2(0.5f, 0.29f), new Vector2(620f, 96f));

        ButtonParts reviewButton = CreateButton("ReviewButton", panel.transform, "Lanjut Review Materi", Accent, Color.white);
        RectTransform reviewRect = reviewButton.button.GetComponent<RectTransform>();
        reviewRect.anchorMin = new Vector2(0.08f, 0.14f);
        reviewRect.anchorMax = new Vector2(0.92f, 0.28f);
        reviewRect.offsetMin = Vector2.zero;
        reviewRect.offsetMax = Vector2.zero;
        reviewButton.label.fontSize = 28;
        UnityEventTools.AddPersistentListener(reviewButton.button.onClick, navigation.OpenSelectedCategoryAR);

        ButtonParts retryButton = CreateButton("RetryButton", panel.transform, "Ulangi Quiz", Primary, Color.white);
        RectTransform retryRect = retryButton.button.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0.08f, 0.02f);
        retryRect.anchorMax = new Vector2(0.48f, 0.12f);
        retryRect.offsetMin = Vector2.zero;
        retryRect.offsetMax = Vector2.zero;
        retryButton.label.fontSize = 24;
        UnityEventTools.AddPersistentListener(retryButton.button.onClick, navigation.RestartQuiz);

        ButtonParts menuButton = CreateButton("MenuButton", panel.transform, "Main Menu", PrimaryLight, Primary);
        RectTransform menuRect = menuButton.button.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.52f, 0.02f);
        menuRect.anchorMax = new Vector2(0.92f, 0.12f);
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;
        menuButton.label.fontSize = 24;
        UnityEventTools.AddPersistentListener(menuButton.button.onClick, navigation.OpenMainMenu);

        CreateTopRightMusicToggle(canvas.transform);

        SerializedObject resultObject = new SerializedObject(resultController);
        resultObject.FindProperty("scoreText").objectReferenceValue = scoreText;
        resultObject.FindProperty("summaryText").objectReferenceValue = summaryText;
        resultObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        resultObject.FindProperty("nextStepText").objectReferenceValue = nextStepText;
        resultObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/ResultScene.unity");
    }

    // ================================================================
    // SCENE: ABOUT
    // Single card. Clean body text. One back button.
    // ================================================================

    static void CreateAboutScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUICamera();
        CreateBackgroundMusicHost();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        CreateFullscreenPanel("FallbackBackground", canvas.transform, BgPage);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();

        GameObject portraitRoot = CreateAboutArtworkRoot(
            "AboutPortraitRoot",
            canvas.transform,
            LoadUiSprite(AboutPortraitPath),
            navigation,
            new Vector2(0.190f, 0.130f),
            new Vector2(0.810f, 0.220f));

        GameObject landscapeRoot = CreateAboutArtworkRoot(
            "AboutLandscapeRoot",
            canvas.transform,
            LoadUiSprite(AboutLandscapePath),
            navigation,
            new Vector2(0.285f, 0.130f),
            new Vector2(0.715f, 0.255f));

        ResponsiveLayoutController responsive = canvas.gameObject.AddComponent<ResponsiveLayoutController>();
        responsive.Configure(portraitRoot, landscapeRoot);
        EditorUtility.SetDirty(responsive);

        CreateTopRightMusicToggle(canvas.transform);

        EditorSceneManager.SaveScene(scene, ScenesFolder + "/AboutScene.unity");
    }

    // ================================================================
    // BUILD SETTINGS
    // ================================================================

    static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesFolder + "/SplashScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/MainMenuScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/GuideScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/MaterialSelectScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/ARScanScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/CollectionScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/QuizHuntScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/ResultScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/AboutScene.unity", true)
        };
    }

    static void ForceSceneLibraryReferences(string scenePath)
    {
        string libraryGuid = AssetDatabase.AssetPathToGUID(ContentLibraryPath);
        if (string.IsNullOrWhiteSpace(libraryGuid) || !File.Exists(scenePath))
        {
            return;
        }

        const string missingReference = "  library: {fileID: 0}";
        string serializedReference = "  library: {fileID: 11400000, guid: " + libraryGuid + ", type: 2}";
        string sceneText = File.ReadAllText(scenePath);
        if (!sceneText.Contains(missingReference))
        {
            return;
        }

        sceneText = sceneText.Replace(missingReference, serializedReference);
        File.WriteAllText(scenePath, sceneText);
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
    }

    // ================================================================
    // HELPERS - CORE
    // ================================================================

    static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;

        string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(assetPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    static Camera CreateUICamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.backgroundColor = BgPage;
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    static Camera CreateARCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent, false);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;

        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<VuforiaBehaviour>();
        cameraObject.AddComponent<DefaultInitializationErrorHandler>();

        return camera;
    }

    static void ConfigureVuforia()
    {
        VuforiaConfiguration configuration = VuforiaConfiguration.Instance;
        configuration.Vuforia.DelayedInitialization = false;
        configuration.Vuforia.MaxSimultaneousImageTargets = 1;

        configuration.DeviceTracker.AutoInitAndStartTracker = false;
        configuration.DeviceTracker.ARCoreRequirementSetting =
        VuforiaConfiguration.DeviceTrackerConfiguration.ARCoreRequirement.DONT_USE;

        EditorUtility.SetDirty(configuration);
    }

    static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    // ================================================================
    // HELPERS - CANVAS & PANELS
    // ================================================================

    static AudioClip LoadBackgroundMusicClip()
    {
        if (CachedBackgroundMusicClip != null)
            return CachedBackgroundMusicClip;

        if (!File.Exists(BackgroundMusicPath))
        {
            Debug.LogWarning("Background music belum ditemukan: " + BackgroundMusicPath);
            return null;
        }

        if (!BackgroundMusicImportChecked)
        {
            BackgroundMusicImportChecked = true;

            AudioImporter importer = AssetImporter.GetAtPath(BackgroundMusicPath) as AudioImporter;
            if (importer != null)
            {
                bool changed = false;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                if (settings.loadType != AudioClipLoadType.Streaming)
                {
                    settings.loadType = AudioClipLoadType.Streaming;
                    changed = true;
                }
                if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    changed = true;
                }
                if (!Mathf.Approximately(settings.quality, 0.65f))
                {
                    settings.quality = 0.65f;
                    changed = true;
                }
                if (settings.preloadAudioData)
                {
                    settings.preloadAudioData = false;
                    changed = true;
                }
                if (!importer.loadInBackground)
                {
                    importer.loadInBackground = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
        }

        CachedBackgroundMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BackgroundMusicPath);
        return CachedBackgroundMusicClip;
    }

    static void CreateBackgroundMusicHost()
    {
        GameObject musicObject = new GameObject("BackgroundMusic");
        AudioSource audioSource = musicObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.35f;

        BackgroundMusicController controller = musicObject.AddComponent<BackgroundMusicController>();
        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("musicClip").objectReferenceValue = LoadBackgroundMusicClip();
        controllerObject.FindProperty("volume").floatValue = 0.35f;
        controllerObject.FindProperty("playOnAwake").boolValue = true;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    static void CreateTopRightMusicToggle(Transform parent)
    {
        CreateMusicToggleButton(parent, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(176f, 72f));
    }

    static void CreateMusicToggleButton(Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        ButtonParts musicButton = CreateButton("MusicToggleButton", parent, "Musik ON", new Color(1f, 1f, 1f, 0.90f), TextPrimary);
        musicButton.label.fontSize = 22;
        AnchorToCorner(musicButton.button.GetComponent<RectTransform>(), anchor, anchoredPosition, size);
        AddCardShadow(musicButton.button.gameObject);

        MusicToggleButton toggle = musicButton.button.gameObject.AddComponent<MusicToggleButton>();
        SerializedObject toggleObject = new SerializedObject(toggle);
        toggleObject.FindProperty("button").objectReferenceValue = musicButton.button;
        toggleObject.FindProperty("label").objectReferenceValue = musicButton.label;
        toggleObject.FindProperty("unmutedLabel").stringValue = "Musik ON";
        toggleObject.FindProperty("mutedLabel").stringValue = "Musik OFF";
        toggleObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(toggle);
    }

    static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static GameObject CreateFullscreenPanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreatePanel(name, parent, color);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<UnityEngine.UI.Image>().color = color;
        return panel;
    }

    static Sprite LoadUiSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.streamingMipmaps)
            {
                importer.streamingMipmaps = false;
                changed = true;
            }
            if (importer.maxTextureSize != 2048)
            {
                importer.maxTextureSize = 2048;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            Debug.LogWarning("UI sprite belum ditemukan atau belum ter-import: " + assetPath);
        return sprite;
    }

    static UnityEngine.UI.Image CreateFullscreenImage(string name, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        imageObject.transform.SetParent(parent, false);

        UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;

        StretchToParent(imageObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        return image;
    }

    static GameObject CreateAspectPreservedArtworkFrame(string name, Transform parent, Sprite sprite)
    {
        GameObject frameObject = new GameObject(name, typeof(RectTransform), typeof(AspectRatioFitter));
        frameObject.transform.SetParent(parent, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        StretchToParent(frameRect, Vector2.zero, Vector2.zero);

        AspectRatioFitter fitter = frameObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = GetSpriteAspectRatio(sprite);
        return frameObject;
    }

    static float GetSpriteAspectRatio(Sprite sprite)
    {
        if (sprite == null || sprite.rect.height <= 0f)
        {
            return 9f / 16f;
        }

        return sprite.rect.width / sprite.rect.height;
    }

    static GameObject CreateMainMenuArtworkRoot(
        string name,
        Transform parent,
        Sprite sprite,
        SceneNavigationController navigation,
        Vector2 belajarMin, Vector2 belajarMax,
        Vector2 petunjukMin, Vector2 petunjukMax,
        Vector2 tentangMin, Vector2 tentangMax,
        Vector2 keluarMin, Vector2 keluarMax)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        StretchToParent(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Transform artworkFrame = CreateAspectPreservedArtworkFrame("ArtworkFrame", root.transform, sprite).transform;
        CreateFullscreenImage("Artwork", artworkFrame, sprite);
        CreateTransparentHotspot("BelajarHotspot", artworkFrame, belajarMin, belajarMax, navigation.OpenMaterialSelect);
        CreateTransparentHotspot("PetunjukHotspot", artworkFrame, petunjukMin, petunjukMax, navigation.OpenGuide);
        CreateTransparentHotspot("TentangHotspot", artworkFrame, tentangMin, tentangMax, navigation.OpenAbout);
        CreateTransparentHotspot("KeluarHotspot", artworkFrame, keluarMin, keluarMax, navigation.QuitApplication);

        return root;
    }

    static GameObject CreateGuideArtworkRoot(
        string name,
        Transform parent,
        Sprite sprite,
        SceneNavigationController navigation,
        Vector2 backButtonMin,
        Vector2 backButtonMax)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        StretchToParent(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Transform artworkFrame = CreateAspectPreservedArtworkFrame("ArtworkFrame", root.transform, sprite).transform;
        CreateFullscreenImage("Artwork", artworkFrame, sprite);
        CreateTransparentHotspot("BackToMenuHotspot", artworkFrame, backButtonMin, backButtonMax, navigation.OpenMainMenu);

        return root;
    }

    static GameObject CreateMaterialGuideArtworkRoot(
        string name,
        Transform parent,
        Sprite sprite,
        SceneNavigationController navigation,
        PlayModeOptionsController playOptions,
        Vector2 playButtonMin,
        Vector2 playButtonMax,
        Vector2 backButtonMin,
        Vector2 backButtonMax)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        StretchToParent(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Transform artworkFrame = CreateAspectPreservedArtworkFrame("ArtworkFrame", root.transform, sprite).transform;
        CreateFullscreenImage("Artwork", artworkFrame, sprite);
        CreateArtworkBackButton(artworkFrame, backButtonMin, backButtonMax, navigation.OpenMainMenu);
        CreateTransparentHotspot("AyoBermainHotspot", artworkFrame, playButtonMin, playButtonMax, playOptions.ShowOptions);

        return root;
    }

    static GameObject CreatePlayModeOptionsOverlay(
        Transform parent,
        SceneNavigationController navigation,
        PlayModeOptionsController controller)
    {
        GameObject overlay = CreateFullscreenPanel("PlayModeOptionsOverlay", parent, new Color(0.02f, 0.05f, 0.12f, 0.70f));
        overlay.transform.SetAsLastSibling();

        GameObject card = CreatePanel("PlayModeOptionsCard", overlay.transform, BgCard);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.08f, 0.15f);
        cardRect.anchorMax = new Vector2(0.92f, 0.84f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        AddCardShadow(card);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 34, 34);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreateText("Title", card.transform, 38, FontStyle.Bold, TextAnchor.MiddleCenter,
                                "Pilih Mode Bermain", TextPrimary);
        SetLayoutHeight(title.gameObject, 64f);

        Text subtitle = CreateText("Subtitle", card.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter,
                                   "Setelah petunjuk, pilih mau langsung scan atau latihan dulu.", TextSecond);
        SetLayoutHeight(subtitle.gameObject, 64f);

        CreatePlayOptionButton(card.transform, "ScanAROptionButton", "Scan Kartu AR", Accent, Color.white, controller.OpenAR);
        CreatePlayOptionButton(card.transform, "CollectionOptionButton", "Koleksi", Success, Color.white, controller.OpenCollection);
        CreatePlayOptionButton(card.transform, "HuntOptionButton", "Quiz Hunt", new Color32(147, 92, 246, 255), Color.white, controller.OpenQuizHunt);
        CreatePlayOptionButton(card.transform, "CloseOptionsButton", "Tutup", new Color32(100, 116, 139, 255), Color.white, controller.HideOptions);

        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("optionsPanel").objectReferenceValue = overlay;
        controllerObject.FindProperty("navigator").objectReferenceValue = navigation;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        overlay.SetActive(false);
        return overlay;
    }

    static void CreatePlayOptionButton(Transform parent, string name, string label, Color backgroundColor,
                                       Color textColor, UnityEngine.Events.UnityAction action)
    {
        ButtonParts parts = CreateButton(name, parent, label, backgroundColor, textColor);
        SetLayoutHeight(parts.button.gameObject, 76f);
        UnityEventTools.AddPersistentListener(parts.button.onClick, action);
    }

    static void SetLayoutHeight(GameObject go, float height)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    static void CreateArtworkBackButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                        UnityEngine.Events.UnityAction action)
    {
        ButtonParts backButton = CreateButton("BackToMenuButton", parent, "Kembali", Accent, Color.white);
        RectTransform rect = backButton.button.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        backButton.label.fontSize = 22;
        backButton.label.resizeTextForBestFit = true;
        backButton.label.resizeTextMinSize = 13;
        backButton.label.resizeTextMaxSize = 22;
        AddCardShadow(backButton.button.gameObject);
        UnityEventTools.AddPersistentListener(backButton.button.onClick, action);
    }

    static GameObject CreateAboutArtworkRoot(
        string name,
        Transform parent,
        Sprite sprite,
        SceneNavigationController navigation,
        Vector2 backButtonMin,
        Vector2 backButtonMax)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        StretchToParent(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Transform artworkFrame = CreateAspectPreservedArtworkFrame("ArtworkFrame", root.transform, sprite).transform;
        CreateFullscreenImage("Artwork", artworkFrame, sprite);
        CreateTransparentHotspot("BackToMenuHotspot", artworkFrame, backButtonMin, backButtonMax, navigation.OpenMainMenu);

        return root;
    }

    static void CreateTransparentHotspot(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                         UnityEngine.Events.UnityAction action)
    {
        CreateTransparentHotspot(name, parent, anchorMin, anchorMax, action);
    }

    static void CreateTransparentHotspot(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                         UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.14f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    // Add a subtle shadow to a panel - unified shadow spec
    static void AddCardShadow(GameObject panel)
    {
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = ShadowColor;
        shadow.effectDistance = new Vector2(0f, -12f);
    }

    // ================================================================
    // HELPERS - TEXT
    // ================================================================

    static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment, string content, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        BuhenARTextStyle.Configure(text, fontSize, Mathf.Max(12, fontSize - 14), alignment, VerticalWrapMode.Overflow, 1.12f);
        return text;
    }

    // ================================================================
    // HELPERS - BUTTONS
    // ================================================================

    // Base button factory
    static ButtonParts CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
        image.color = backgroundColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor    = backgroundColor;
        colors.highlightedColor = new Color(backgroundColor.r * 1.06f, backgroundColor.g * 1.06f, backgroundColor.b * 1.06f, backgroundColor.a);
        colors.pressedColor   = new Color(backgroundColor.r * 0.90f, backgroundColor.g * 0.90f, backgroundColor.b * 0.90f, backgroundColor.a);
        colors.selectedColor  = backgroundColor;
        colors.fadeDuration   = 0.1f;
        button.colors = colors;

        // Label inside button
        Text buttonText = CreateText("Label", buttonObject.transform, 28, FontStyle.Bold, TextAnchor.MiddleCenter, label, textColor);
        StretchToParent(buttonText.rectTransform, new Vector2(20f, 12f), new Vector2(-20f, -12f));

        // Default height - 88px minimum touch target
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 88f);

        return new ButtonParts { button = button, label = buttonText };
    }

    // Nav button: full width, explicit height override
    static void CreateNavigationButton(Transform parent, string label, UnityEngine.Events.UnityAction action,
                                       Color backgroundColor, Color textColor, float height = 88f)
    {
        ButtonParts parts = CreateButton(label.Replace(" ", "") + "Button", parent, label, backgroundColor, textColor);
        RectTransform rect = parts.button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, height);
        UnityEventTools.AddPersistentListener(parts.button.onClick, action);
    }

    // Legacy overload for callers that don't pass colors
    static void CreateNavigationButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        CreateNavigationButton(parent, label, action, Primary, Color.white, 88f);
    }

    // Legacy overload for callers that pass colors but not height
    static void CreateNavigationButton(Transform parent, string label, UnityEngine.Events.UnityAction action,
                                       Color backgroundColor, Color textColor)
    {
        CreateNavigationButton(parent, label, action, backgroundColor, textColor, 88f);
    }

    // Bottom button anchored to lower portion of parent
    static void CreateBottomButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        ButtonParts parts = CreateButton(label.Replace(" ", "") + "Button", parent, label, Primary, Color.white);
        RectTransform rect = parts.button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.06f);
        rect.anchorMax = new Vector2(0.9f, 0.18f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        UnityEventTools.AddPersistentListener(parts.button.onClick, action);
    }

    // Bottom-left back button
    static void CreateCornerBackButton(Transform parent, UnityEngine.Events.UnityAction action)
    {
        ButtonParts parts = CreateButton("BackButton", parent, "Kembali", PrimaryLight, Primary);
        parts.label.fontSize = 26;
        RectTransform rect = parts.button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.03f, 0.04f);
        rect.anchorMax = new Vector2(0.28f, 0.11f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        UnityEventTools.AddPersistentListener(parts.button.onClick, action);
    }

    // ================================================================
    // HELPERS - MATERIAL SELECT CARD
    // Large touch-target card for material selection.
    // ================================================================

    static void CreateMaterialCard(
        Transform parent,
        string title,
        string description,
        string badge,
        string heroLetters,
        string ctaLabel,
        UnityEngine.Events.UnityAction action,
        Color background,
        Color accent,
        Color accentTextColor)
    {
        GameObject card = CreatePanel(title.Replace(" ", "") + "Card", parent, background);
        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.minHeight = 300f;
        layoutElement.preferredHeight = 320f;
        layoutElement.flexibleWidth = 1f;
        AddCardShadow(card);

        // Top accent stripe - brand color band
        GameObject topStripe = CreatePanel("TopStripe", card.transform, accent);
        RectTransform topStripeRect = topStripe.GetComponent<RectTransform>();
        topStripeRect.anchorMin = new Vector2(0f, 0.96f);
        topStripeRect.anchorMax = new Vector2(1f, 1f);
        topStripeRect.offsetMin = Vector2.zero;
        topStripeRect.offsetMax = Vector2.zero;

        // Badge pill
        GameObject badgePanel = CreatePanel("BadgePanel", card.transform, new Color(accent.r, accent.g, accent.b, 0.16f));
        RectTransform badgeRect = badgePanel.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.05f, 0.76f);
        badgeRect.anchorMax = new Vector2(0.30f, 0.89f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;

        Text badgeText = CreateText("Badge", badgePanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter, badge, accent);
        StretchToParent(badgeText.rectTransform, new Vector2(12f, 6f), new Vector2(-12f, -6f));

        // Left visual block - clear recognition without stealing too much space
        GameObject visualPanel = CreatePanel("VisualPanel", card.transform, new Color(1f, 1f, 1f, 0.56f));
        RectTransform visualRect = visualPanel.GetComponent<RectTransform>();
        visualRect.anchorMin = new Vector2(0.05f, 0.18f);
        visualRect.anchorMax = new Vector2(0.28f, 0.72f);
        visualRect.offsetMin = Vector2.zero;
        visualRect.offsetMax = Vector2.zero;

        Text heroText = CreateText("HeroText", visualPanel.transform, 82, FontStyle.Bold, TextAnchor.MiddleCenter,
                                   heroLetters, accent);
        StretchToParent(heroText.rectTransform, Vector2.zero, Vector2.zero);

        // Title
        Text titleText = CreateText("Title", card.transform, 40, FontStyle.Bold, TextAnchor.UpperLeft, title, TextPrimary);
        titleText.rectTransform.anchorMin = new Vector2(0.34f, 0.58f);
        titleText.rectTransform.anchorMax = new Vector2(0.94f, 0.83f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;

        // Body description
        Text bodyText = CreateText("Body", card.transform, 24, FontStyle.Normal, TextAnchor.UpperLeft, description, TextSecond);
        bodyText.rectTransform.anchorMin = new Vector2(0.34f, 0.30f);
        bodyText.rectTransform.anchorMax = new Vector2(0.94f, 0.58f);
        bodyText.rectTransform.offsetMin = Vector2.zero;
        bodyText.rectTransform.offsetMax = Vector2.zero;

        // CTA button - large and unmissable
        ButtonParts button = CreateButton("SelectButton", card.transform, ctaLabel, accent, accentTextColor);
        RectTransform buttonRect = button.button.GetComponent<RectTransform>();
        button.label.fontSize = 28;
        buttonRect.anchorMin = new Vector2(0.34f, 0.08f);
        buttonRect.anchorMax = new Vector2(0.94f, 0.24f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        UnityEventTools.AddPersistentListener(button.button.onClick, action);
    }

    // ================================================================
    // HELPERS - RECT LAYOUT
    // ================================================================

    static void StretchCenter(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    static void StretchToParent(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
    }

    static void AnchorToCorner(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    static void AnchorFill(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax,
                           Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2? anchoredPosition = null)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.anchoredPosition = anchoredPosition ?? Vector2.zero;
    }

    // ================================================================
    // HELPERS - MATERIALS & PRIMITIVE OBJECTS
    // ================================================================

    static Material CreateMaterial(string name, Color color)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject CreatePrimitiveChild(PrimitiveType primitiveType, string name, Transform parent,
                                           Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject child = GameObject.CreatePrimitive(primitiveType);
        child.name = name;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localScale = localScale;
        child.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name + "Material", color);
        return child;
    }

    // ================================================================
    // 3D PREFAB BUILDERS
    // ================================================================

    static void BuildApple(GameObject root, Color fruitColor, Color leafColor)
    {
        CreatePrimitiveChild(PrimitiveType.Sphere,   "AppleBody",  root.transform, Vector3.zero,              new Vector3(1f, 0.92f, 1f),        fruitColor);
        CreatePrimitiveChild(PrimitiveType.Cylinder, "AppleStem",  root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.08f, 0.18f, 0.08f),  new Color32(120, 53, 15, 255));
        GameObject leaf = CreatePrimitiveChild(PrimitiveType.Sphere, "AppleLeaf", root.transform, new Vector3(0.18f, 0.7f, 0f), new Vector3(0.28f, 0.08f, 0.18f), leafColor);
        leaf.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
    }

    static void BuildOrange(GameObject root, Color orangeColor)
    {
        CreatePrimitiveChild(PrimitiveType.Sphere, "OrangeBody", root.transform, Vector3.zero, Vector3.one, orangeColor);
        GameObject leaf = CreatePrimitiveChild(PrimitiveType.Sphere, "OrangeLeaf", root.transform, new Vector3(0.16f, 0.66f, 0f), new Vector3(0.24f, 0.08f, 0.16f), new Color32(34, 197, 94, 255));
        leaf.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
    }

    static void BuildBanana(GameObject root, Color bananaColor)
    {
        GameObject banana = CreatePrimitiveChild(PrimitiveType.Capsule, "BananaBody", root.transform, Vector3.zero, new Vector3(0.6f, 1.45f, 0.48f), bananaColor);
        banana.transform.localRotation = Quaternion.Euler(0f, 0f, -34f);
    }

    static void BuildGrapes(GameObject root, Color grapeColor, Color leafColor)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.55f, 0f),
            new Vector3(-0.22f, 0.2f, 0f),
            new Vector3(0.22f, 0.2f, 0f),
            new Vector3(0f, 0.15f, 0.22f),
            new Vector3(0f, -0.15f, 0f),
            new Vector3(-0.2f, -0.18f, 0.12f),
            new Vector3(0.2f, -0.18f, 0.12f)
        };

        for (int i = 0; i < positions.Length; i++)
            CreatePrimitiveChild(PrimitiveType.Sphere, "Grape" + i, root.transform, positions[i], Vector3.one * 0.48f, grapeColor);

        CreatePrimitiveChild(PrimitiveType.Cylinder, "GrapeStem", root.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.08f, 0.2f, 0.08f), new Color32(120, 53, 15, 255));
        CreatePrimitiveChild(PrimitiveType.Sphere,   "GrapeLeaf", root.transform, new Vector3(0.2f, 0.88f, 0f), new Vector3(0.34f, 0.1f, 0.26f), leafColor);
    }

    static void BuildButterfly(GameObject root, Color wingColor, Color accentColor)
    {
        CreatePrimitiveChild(PrimitiveType.Capsule, "Body",            root.transform, Vector3.zero,             new Vector3(0.2f, 0.7f, 0.2f),     new Color32(55, 65, 81, 255));
        GameObject lwt = CreatePrimitiveChild(PrimitiveType.Cube, "LeftWingTop",    root.transform, new Vector3(-0.42f, 0.18f, 0f), new Vector3(0.52f, 0.42f, 0.08f), wingColor);
        GameObject rwt = CreatePrimitiveChild(PrimitiveType.Cube, "RightWingTop",   root.transform, new Vector3(0.42f, 0.18f, 0f),  new Vector3(0.52f, 0.42f, 0.08f), wingColor);
        GameObject lwb = CreatePrimitiveChild(PrimitiveType.Cube, "LeftWingBottom", root.transform, new Vector3(-0.34f, -0.22f, 0f),new Vector3(0.36f, 0.32f, 0.08f), accentColor);
        GameObject rwb = CreatePrimitiveChild(PrimitiveType.Cube, "RightWingBottom",root.transform, new Vector3(0.34f, -0.22f, 0f), new Vector3(0.36f, 0.32f, 0.08f), accentColor);
        lwt.transform.localRotation = Quaternion.Euler(0f, 0f,  18f);
        rwt.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        lwb.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        rwb.transform.localRotation = Quaternion.Euler(0f, 0f,  12f);
    }

    static void BuildFish(GameObject root, Color bodyColor, Color stripeColor)
    {
        CreatePrimitiveChild(PrimitiveType.Capsule, "FishBody",    root.transform, Vector3.zero,               new Vector3(0.5f, 1.2f, 0.46f),   bodyColor).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveChild(PrimitiveType.Cube,    "FishTail",    root.transform, new Vector3(-0.76f, 0f, 0f), new Vector3(0.22f, 0.54f, 0.08f), bodyColor).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        CreatePrimitiveChild(PrimitiveType.Cube,    "FishStripeA", root.transform, new Vector3(-0.18f, 0f, 0.18f), new Vector3(0.16f, 0.82f, 0.1f), stripeColor).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveChild(PrimitiveType.Cube,    "FishStripeB", root.transform, new Vector3(0.2f, 0f, 0.18f),  new Vector3(0.16f, 0.82f, 0.1f), stripeColor).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    static void BuildBird(GameObject root, Color bodyColor, Color accentColor)
    {
        CreatePrimitiveChild(PrimitiveType.Capsule, "BirdBody", root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(0.6f, 1.05f, 0.6f), bodyColor);
        CreatePrimitiveChild(PrimitiveType.Sphere,  "BirdHead", root.transform, new Vector3(0f, 0.78f, 0f),  Vector3.one * 0.48f,            accentColor);
        CreatePrimitiveChild(PrimitiveType.Cube,    "BirdTail", root.transform, new Vector3(-0.42f, -0.08f, 0f), new Vector3(0.2f, 0.5f, 0.08f), accentColor).transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
        CreatePrimitiveChild(PrimitiveType.Cube,    "BirdBeak", root.transform, new Vector3(0.34f, 0.78f, 0f),   new Vector3(0.18f, 0.12f, 0.12f), new Color32(245, 158, 11, 255));
    }

    static void BuildCat(GameObject root, Color furColor, Color accentColor)
    {
        CreatePrimitiveChild(PrimitiveType.Capsule, "CatBody",  root.transform, new Vector3(0f, -0.15f, 0f), new Vector3(0.7f, 1.05f, 0.7f), furColor);
        CreatePrimitiveChild(PrimitiveType.Sphere,  "CatHead",  root.transform, new Vector3(0f, 0.82f, 0f),  Vector3.one * 0.6f,             furColor);
        CreatePrimitiveChild(PrimitiveType.Cube,    "LeftEar",  root.transform, new Vector3(-0.24f, 1.34f, 0f), new Vector3(0.22f, 0.24f, 0.08f), accentColor).transform.localRotation = Quaternion.Euler(0f, 0f,  32f);
        CreatePrimitiveChild(PrimitiveType.Cube,    "RightEar", root.transform, new Vector3(0.24f, 1.34f, 0f),  new Vector3(0.22f, 0.24f, 0.08f), accentColor).transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
        CreatePrimitiveChild(PrimitiveType.Cylinder,"CatTail",  root.transform, new Vector3(-0.48f, 0.18f, 0f), new Vector3(0.08f, 0.5f, 0.08f),  furColor).transform.localRotation = Quaternion.Euler(0f, 0f, 46f);
    }
}
