using System.Collections.Generic;
using ARtiGraf.AR;
using ARtiGraf.Collection;
using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.QuizHunt;
using ARtiGraf.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuhenARFeatureSceneSetup
{
    const string ScenesFolder = "Assets/Scenes";
    const string ARScanScenePath = ScenesFolder + "/ARScanScene.unity";
    const string MainMenuScenePath = ScenesFolder + "/MainMenuScene.unity";
    const string MaterialSelectScenePath = ScenesFolder + "/MaterialSelectScene.unity";
    const string QuizScenePath = ScenesFolder + "/QuizScene.unity";
    const string CollectionScenePath = ScenesFolder + "/CollectionScene.unity";
    const string QuizHuntScenePath = ScenesFolder + "/QuizHuntScene.unity";
    const string ContentLibraryPath = "Assets/Resources/ARtiGrafContentLibrary.asset";
    const string UiAppsFolder = "Assets/Art/UI_APPS";
    const string SelectionMenuArtworkPath = UiAppsFolder + "/Selection_menu.png";
    const string SelectionMenuLandscapeArtworkPath = UiAppsFolder + "/Selection_menu_landscape.png";
    const string CollectionArtworkPath = UiAppsFolder + "/Koleksi.png";
    const string CollectionLandscapeArtworkPath = UiAppsFolder + "/koleksi_landscape.png";
    const string QuizHuntPortraitArtworkPath = UiAppsFolder + "/quiz_hunt.png";
    const string QuizHuntLandscapeArtworkPath = UiAppsFolder + "/quiz_hunt_landscape.png";

    static readonly Color Navy = new Color32(7, 18, 46, 255);
    static readonly Color Blue = new Color32(9, 69, 190, 255);
    static readonly Color Orange = new Color32(255, 103, 0, 255);
    static readonly Color Cream = new Color32(255, 248, 229, 255);
    static readonly Color SoftPanel = new Color32(255, 255, 255, 238);

    [MenuItem("Tools/BuhenAR/Apply Feature Scene Setup")]
    public static void ApplyFeatureSceneSetup()
    {
        UpgradeARScanScene();
        UpgradeMainMenuScene();
        UpgradeMaterialSelectScene();
        UpgradeQuizSceneNavigation();
        CreateCollectionScene();
        CreateQuizHuntScene();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[BuhenAR] Feature scene setup selesai.");
    }

    public static void ApplyAndSave()
    {
        ApplyFeatureSceneSetup();
    }

    static void UpgradeARScanScene()
    {
        if (!System.IO.File.Exists(ARScanScenePath)) return;

        Scene scene = EditorSceneManager.OpenScene(ARScanScenePath, OpenSceneMode.Single);
        ARImageTrackingController scanner = FindSceneObject<ARImageTrackingController>();
        UIOverlayController overlay = FindSceneObject<UIOverlayController>();
        ARLearningFeedbackController feedback = FindSceneObject<ARLearningFeedbackController>();
        SceneNavigationController navigation = FindSceneObject<SceneNavigationController>();
        if (scanner == null) return;

        ARTouchInteractionController touch = scanner.GetComponent<ARTouchInteractionController>();
        if (touch == null) touch = scanner.gameObject.AddComponent<ARTouchInteractionController>();

        SerializedObject scannerObject = new SerializedObject(scanner);
        scannerObject.FindProperty("touchController").objectReferenceValue = touch;
        scannerObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject infoButton = GameObject.Find("InfoToggleButton");
        GameObject infoPanel = GameObject.Find("InfoPanel");
        if (feedback != null)
        {
            SerializedObject feedbackObject = new SerializedObject(feedback);
            feedbackObject.FindProperty("infoButton").objectReferenceValue = infoButton;
            feedbackObject.FindProperty("infoPanel").objectReferenceValue = infoPanel;
            feedbackObject.ApplyModifiedPropertiesWithoutUndo();
        }

        RectTransform flashRect = EnsureFlashButton(scanner);
        if (navigation != null)
        {
            foreach (Button button in FindSceneButtonsNamed("BackButton"))
            {
                ClearButtonClick(button);
                UnityEventTools.AddPersistentListener(button.onClick, navigation.OpenARBackDestination);
            }
        }

        if (overlay != null)
        {
            SerializedObject overlayObject = new SerializedObject(overlay);
            SerializedProperty flashProperty = overlayObject.FindProperty("flashButtonRect");
            if (flashProperty != null) flashProperty.objectReferenceValue = flashRect;
            overlayObject.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static RectTransform EnsureFlashButton(ARImageTrackingController scanner)
    {
        GameObject existing = GameObject.Find("FlashToggleButton");
        if (existing != null)
        {
            ScannerFlashButton flash = existing.GetComponent<ScannerFlashButton>();
            if (flash == null) flash = existing.AddComponent<ScannerFlashButton>();
            WireFlashButton(flash, scanner, existing.GetComponent<Button>(), existing.GetComponentInChildren<Text>(true));
            return existing.GetComponent<RectTransform>();
        }

        GameObject topBar = GameObject.Find("TopBar");
        Transform parent = topBar != null ? topBar.transform : FindSceneObject<Canvas>()?.transform;
        GameObject buttonObject = CreateButton(parent, "FlashToggleButton", "Flash", Orange, Color.white, 24, out Button button, out Text label);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-420f, 0f);
        rect.sizeDelta = new Vector2(168f, 72f);

        ScannerFlashButton flashButton = buttonObject.AddComponent<ScannerFlashButton>();
        WireFlashButton(flashButton, scanner, button, label);
        return rect;
    }

    static void WireFlashButton(ScannerFlashButton flashButton, ARImageTrackingController scanner, Button button, Text label)
    {
        SerializedObject flashObject = new SerializedObject(flashButton);
        flashObject.FindProperty("scanner").objectReferenceValue = scanner;
        flashObject.FindProperty("button").objectReferenceValue = button;
        flashObject.FindProperty("label").objectReferenceValue = label;
        flashObject.ApplyModifiedPropertiesWithoutUndo();
    }

    static void UpgradeMainMenuScene()
    {
        if (!System.IO.File.Exists(MainMenuScenePath)) return;

        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        DestroySceneObjectsNamed("FeatureButtonsPanel");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void UpgradeMaterialSelectScene()
    {
        if (!System.IO.File.Exists(MaterialSelectScenePath)) return;

        Scene scene = EditorSceneManager.OpenScene(MaterialSelectScenePath, OpenSceneMode.Single);
        Canvas canvas = FindSceneObject<Canvas>();
        if (canvas == null) return;

        SceneNavigationController navigation = canvas.GetComponent<SceneNavigationController>();
        if (navigation == null) navigation = canvas.gameObject.AddComponent<SceneNavigationController>();

        PlayModeOptionsController controller = canvas.GetComponent<PlayModeOptionsController>();
        if (controller == null) controller = canvas.gameObject.AddComponent<PlayModeOptionsController>();

        DestroySceneObjectsNamed("PlayModeOptionsOverlay");
        DestroySceneObjectsNamed("SelectionMenuRoot");
        DestroySceneObjectsNamed("MaterialGuidePortraitRoot");
        DestroySceneObjectsNamed("MaterialGuideLandscapeRoot");
        GameObject selectionRoot = CreateSelectionMenuRoot(canvas.transform, navigation, controller);
        selectionRoot.transform.SetAsLastSibling();

        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("optionsPanel").objectReferenceValue = null;
        controllerObject.FindProperty("navigator").objectReferenceValue = navigation;
        SerializedProperty hideOnAwake = controllerObject.FindProperty("hideOnAwake");
        if (hideOnAwake != null) hideOnAwake.boolValue = false;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static GameObject CreateSelectionMenuRoot(Transform parent, SceneNavigationController navigation, PlayModeOptionsController controller)
    {
        Sprite portraitArtwork = LoadUiSprite(SelectionMenuArtworkPath);
        Sprite landscapeArtwork = LoadUiSprite(SelectionMenuLandscapeArtworkPath);
        GameObject frame = CreateResponsiveArtworkFrame(parent, "SelectionMenuRoot", portraitArtwork, landscapeArtwork);

        GameObject portraitHotspots = CreateFullRect(frame.transform, "SelectionPortraitHotspots");
        GameObject landscapeHotspots = CreateFullRect(frame.transform, "SelectionLandscapeHotspots");

        CreateTransparentButton(portraitHotspots.transform, "ScanAROptionButton",
            new Vector2(0.14f, 0.48f), new Vector2(0.86f, 0.595f), controller.OpenAR);
        CreateTransparentButton(portraitHotspots.transform, "CollectionOptionButton",
            new Vector2(0.14f, 0.355f), new Vector2(0.86f, 0.472f), controller.OpenCollection);
        CreateTransparentButton(portraitHotspots.transform, "HuntOptionButton",
            new Vector2(0.14f, 0.228f), new Vector2(0.86f, 0.345f), controller.OpenQuizHunt);
        CreateTransparentButton(portraitHotspots.transform, "CloseOptionsButton",
            new Vector2(0.14f, 0.10f), new Vector2(0.86f, 0.218f), navigation.OpenMainMenu);

        CreateTransparentButton(landscapeHotspots.transform, "ScanAROptionButton",
            new Vector2(0.125f, 0.382f), new Vector2(0.49f, 0.585f), controller.OpenAR);
        CreateTransparentButton(landscapeHotspots.transform, "CollectionOptionButton",
            new Vector2(0.505f, 0.382f), new Vector2(0.875f, 0.585f), controller.OpenCollection);
        CreateTransparentButton(landscapeHotspots.transform, "HuntOptionButton",
            new Vector2(0.125f, 0.155f), new Vector2(0.49f, 0.362f), controller.OpenQuizHunt);
        CreateTransparentButton(landscapeHotspots.transform, "CloseOptionsButton",
            new Vector2(0.505f, 0.155f), new Vector2(0.875f, 0.362f), navigation.OpenMainMenu);

        ResponsiveLayoutController layout = frame.AddComponent<ResponsiveLayoutController>();
        layout.Configure(portraitHotspots, landscapeHotspots);

        return frame;
    }

    static void UpgradeQuizSceneNavigation()
    {
        if (!System.IO.File.Exists(QuizScenePath)) return;

        Scene scene = EditorSceneManager.OpenScene(QuizScenePath, OpenSceneMode.Single);
        Canvas canvas = FindSceneObject<Canvas>();
        if (canvas == null) return;

        SceneNavigationController navigation = canvas.GetComponent<SceneNavigationController>();
        if (navigation == null) navigation = canvas.gameObject.AddComponent<SceneNavigationController>();

        foreach (Button button in FindSceneButtonsNamed("BackButton"))
        {
            ClearButtonClick(button);
            UnityEventTools.AddPersistentListener(button.onClick, navigation.OpenMaterialSelect);
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "Pilih Mode";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void CreateCollectionScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUiCamera();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        Sprite portraitArtwork = LoadUiSprite(CollectionArtworkPath);
        Sprite landscapeArtwork = LoadUiSprite(CollectionLandscapeArtworkPath);
        GameObject frame = CreateResponsiveArtworkFrame(canvas.transform, "CollectionArtworkFrame", portraitArtwork, landscapeArtwork);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        CollectionManager manager = canvas.gameObject.AddComponent<CollectionManager>();
        TTSController tts = canvas.gameObject.AddComponent<TTSController>();
        canvas.gameObject.AddComponent<AudioSource>();
        _ = tts;

        Text totalText = CreateText(frame.transform, "TotalDiscoveredText", "", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(totalText.rectTransform, new Vector2(0.31f, 0.807f), new Vector2(0.69f, 0.846f));

        CreateTransparentButton(frame.transform, "BackButton",
            new Vector2(0.065f, 0.912f), new Vector2(0.305f, 0.965f), navigation.OpenMaterialSelect);

        GameObject scrollRoot = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollRoot.transform.SetParent(frame.transform, false);
        SetAnchors(scrollRoot.GetComponent<RectTransform>(), new Vector2(0.19f, 0.13f), new Vector2(0.81f, 0.78f));
        scrollRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollRoot.transform, false);
        SetAnchors(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(118f, 154f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject template = CreateCollectionItemTemplate(frame.transform);
        template.SetActive(false);
        CollectionDetailPopup popup = CreateCollectionPopup(frame.transform);

        SerializedObject managerObject = new SerializedObject(manager);
        managerObject.FindProperty("library").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        managerObject.FindProperty("collectionGrid").objectReferenceValue = content.transform;
        managerObject.FindProperty("collectionItemPrefab").objectReferenceValue = template;
        managerObject.FindProperty("totalDiscoveredText").objectReferenceValue = totalText;
        managerObject.ApplyModifiedPropertiesWithoutUndo();

        _ = popup;
        EditorSceneManager.SaveScene(scene, CollectionScenePath);
    }

    static void CreateQuizHuntScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateUiCamera();
        CreateEventSystem();
        Canvas canvas = CreateCanvas("Canvas");
        Sprite portraitArtwork = LoadUiSprite(QuizHuntPortraitArtworkPath);
        Sprite landscapeArtwork = LoadUiSprite(QuizHuntLandscapeArtworkPath);
        GameObject frame = CreateResponsiveArtworkFrame(canvas.transform, "QuizHuntArtworkFrame", portraitArtwork, landscapeArtwork);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        QuizHuntController hunt = canvas.gameObject.AddComponent<QuizHuntController>();

        Text progress = CreateText(frame.transform, "ProgressText", "", 21, FontStyle.Bold, TextAnchor.MiddleCenter, Blue);
        SetAnchors(progress.rectTransform, new Vector2(0.40f, 0.735f), new Vector2(0.60f, 0.775f));

        GameObject cluePanel = CreatePanel(frame.transform, "CluePanel", new Color(1f, 1f, 1f, 0f), new Vector2(0.13f, 0.435f), new Vector2(0.87f, 0.695f));
        Text clue = CreateText(cluePanel.transform, "ClueText", "", 27, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(clue.rectTransform, new Vector2(0.07f, 0.45f), new Vector2(0.93f, 0.88f));
        Text subClue = CreateText(cluePanel.transform, "ClueSubText", "", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Orange);
        SetAnchors(subClue.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.25f));

        Text score = CreateText(frame.transform, "ScoreText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(score.rectTransform, new Vector2(0.07f, 0.287f), new Vector2(0.93f, 0.355f));
        Text pointsValue = CreateText(frame.transform, "PointsValueText", "0", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(pointsValue.rectTransform, new Vector2(0.205f, 0.300f), new Vector2(0.305f, 0.330f));
        Text correctValue = CreateText(frame.transform, "CorrectValueText", "0", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(correctValue.rectTransform, new Vector2(0.505f, 0.300f), new Vector2(0.595f, 0.330f));
        Text streakValue = CreateText(frame.transform, "StreakValueText", "0", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(streakValue.rectTransform, new Vector2(0.792f, 0.300f), new Vector2(0.890f, 0.330f));
        Text timer = CreateText(frame.transform, "TimerText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Orange);
        SetAnchors(timer.rectTransform, new Vector2(0.08f, 0.245f), new Vector2(0.92f, 0.285f));

        GameObject hintPanel = CreatePanel(frame.transform, "HintPanel", new Color32(255, 255, 255, 18), new Vector2(0.14f, 0.385f), new Vector2(0.86f, 0.44f));
        Text hintText = CreateText(hintPanel.transform, "HintText", "", 21, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(hintText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
        hintPanel.SetActive(false);

        CreateTransparentButton(frame.transform, "ScanButton", new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.276f), hunt.OnScanButtonPressed);

        CreateTransparentButton(frame.transform, "HintButton", new Vector2(0.065f, 0.055f), new Vector2(0.33f, 0.14f), hunt.OnShowHintPressed);

        CreateTransparentButton(frame.transform, "SkipButton", new Vector2(0.365f, 0.055f), new Vector2(0.635f, 0.14f), hunt.OnSkipPressed);

        CreateTransparentButton(frame.transform, "MenuButton", new Vector2(0.67f, 0.055f), new Vector2(0.935f, 0.14f), hunt.OnMainMenuPressed);

        Text successTitle;
        Text successDesc;
        GameObject successPanel = CreateMessagePanel(frame.transform, "SuccessPanel", new Color32(220, 252, 231, 250), "Benar!", out successTitle, out successDesc);
        Text failText;
        GameObject failPanel = CreateSingleTextPanel(frame.transform, "FailPanel", new Color32(254, 226, 226, 250), out failText);
        Text resultScore;
        Text resultStars;
        GameObject resultPanel = CreateResultPanel(frame.transform, out resultScore, out resultStars, hunt);

        SerializedObject huntObject = new SerializedObject(hunt);
        huntObject.FindProperty("library").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        huntObject.FindProperty("clueText").objectReferenceValue = clue;
        huntObject.FindProperty("clueSubText").objectReferenceValue = subClue;
        huntObject.FindProperty("progressText").objectReferenceValue = progress;
        huntObject.FindProperty("timerText").objectReferenceValue = timer;
        huntObject.FindProperty("scoreText").objectReferenceValue = score;
        huntObject.FindProperty("pointsValueText").objectReferenceValue = pointsValue;
        huntObject.FindProperty("correctValueText").objectReferenceValue = correctValue;
        huntObject.FindProperty("streakValueText").objectReferenceValue = streakValue;
        huntObject.FindProperty("hintPanel").objectReferenceValue = hintPanel;
        huntObject.FindProperty("hintText").objectReferenceValue = hintText;
        huntObject.FindProperty("successPanel").objectReferenceValue = successPanel;
        huntObject.FindProperty("successTitleText").objectReferenceValue = successTitle;
        huntObject.FindProperty("successDescText").objectReferenceValue = successDesc;
        huntObject.FindProperty("failPanel").objectReferenceValue = failPanel;
        huntObject.FindProperty("failText").objectReferenceValue = failText;
        huntObject.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        huntObject.FindProperty("resultScoreText").objectReferenceValue = resultScore;
        huntObject.FindProperty("resultStarsText").objectReferenceValue = resultStars;
        huntObject.FindProperty("navigator").objectReferenceValue = navigation;
        huntObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, QuizHuntScenePath);
    }

    static GameObject CreateCollectionItemTemplate(Transform parent)
    {
        GameObject item = new GameObject("CollectionItemTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CollectionItemUI));
        item.transform.SetParent(parent, false);
        item.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.94f);

        Image thumbnail = CreateImage(item.transform, "Thumbnail", new Color(1f, 1f, 1f, 0.95f));
        SetAnchors(thumbnail.rectTransform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.92f));

        Image silhouette = CreateImage(item.transform, "Silhouette", new Color(0f, 0f, 0f, 0.38f));
        SetAnchors(silhouette.rectTransform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.92f));

        Text name = CreateText(item.transform, "Name", "???", 21, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(name.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.32f));

        Text question = CreateText(item.transform, "QuestionMark", "?", 48, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(question.rectTransform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.92f));

        GameObject star = CreatePanel(item.transform, "StarBadge", new Color32(255, 213, 48, 245), new Vector2(0.66f, 0.78f), new Vector2(0.96f, 0.98f));
        Text starText = CreateText(star.transform, "StarText", "*", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(starText.rectTransform, Vector2.zero, Vector2.one);

        GameObject badge = CreatePanel(item.transform, "DiscoveredBadge", new Color32(21, 136, 81, 220), new Vector2(0.04f, 0.78f), new Vector2(0.34f, 0.98f));
        Text badgeText = CreateText(badge.transform, "BadgeText", "OK", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(badgeText.rectTransform, Vector2.zero, Vector2.one);

        CollectionItemUI itemUi = item.GetComponent<CollectionItemUI>();
        SerializedObject itemObject = new SerializedObject(itemUi);
        itemObject.FindProperty("thumbnailImage").objectReferenceValue = thumbnail;
        itemObject.FindProperty("silhouetteOverlay").objectReferenceValue = silhouette;
        itemObject.FindProperty("itemNameText").objectReferenceValue = name;
        itemObject.FindProperty("questionMarkText").objectReferenceValue = question;
        itemObject.FindProperty("starBadge").objectReferenceValue = star;
        itemObject.FindProperty("discoveredBadge").objectReferenceValue = badge;
        itemObject.ApplyModifiedPropertiesWithoutUndo();

        Button button = item.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, itemUi.OnItemTapped);
        return item;
    }

    static CollectionDetailPopup CreateCollectionPopup(Transform parent)
    {
        GameObject host = new GameObject("CollectionDetailPopup", typeof(RectTransform), typeof(CollectionDetailPopup));
        host.transform.SetParent(parent, false);
        SetAnchors(host.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject panel = CreatePanel(host.transform, "PopupPanel", new Color32(7, 18, 46, 245), new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f));
        Image thumbnail = CreateImage(panel.transform, "Thumbnail", Color.white);
        SetAnchors(thumbnail.rectTransform, new Vector2(0.31f, 0.58f), new Vector2(0.69f, 0.92f));
        Text title = CreateText(panel.transform, "Title", "", 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(title.rectTransform, new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.58f));
        Text subtitle = CreateText(panel.transform, "Subtitle", "", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Orange);
        SetAnchors(subtitle.rectTransform, new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.48f));
        Text desc = CreateText(panel.transform, "Description", "", 22, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(desc.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.39f));
        Text fact = CreateText(panel.transform, "FunFact", "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        SetAnchors(fact.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.22f));
        GameObject star = CreatePanel(panel.transform, "StarBadge", new Color32(255, 213, 48, 245), new Vector2(0.72f, 0.84f), new Vector2(0.92f, 0.94f));
        Text starText = CreateText(star.transform, "StarText", "Bintang", 15, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(starText.rectTransform, Vector2.zero, Vector2.one);
        Button tts = CreateButton(panel.transform, "TTSButton", "Dengar", Orange, Color.white, 22, out _, out Text ttsLabel).GetComponent<Button>();
        SetAnchors(tts.GetComponent<RectTransform>(), new Vector2(0.08f, 0.035f), new Vector2(0.47f, 0.11f));
        Button close = CreateButton(panel.transform, "CloseButton", "Tutup", Blue, Color.white, 22, out _, out _).GetComponent<Button>();
        SetAnchors(close.GetComponent<RectTransform>(), new Vector2(0.53f, 0.035f), new Vector2(0.92f, 0.11f));

        CollectionDetailPopup popup = host.GetComponent<CollectionDetailPopup>();
        SerializedObject popupObject = new SerializedObject(popup);
        popupObject.FindProperty("popupPanel").objectReferenceValue = panel;
        popupObject.FindProperty("thumbnailImage").objectReferenceValue = thumbnail;
        popupObject.FindProperty("titleText").objectReferenceValue = title;
        popupObject.FindProperty("subtitleText").objectReferenceValue = subtitle;
        popupObject.FindProperty("descriptionText").objectReferenceValue = desc;
        popupObject.FindProperty("funFactText").objectReferenceValue = fact;
        popupObject.FindProperty("starBadge").objectReferenceValue = star;
        popupObject.FindProperty("ttsButton").objectReferenceValue = tts;
        popupObject.FindProperty("ttsButtonLabel").objectReferenceValue = ttsLabel;
        popupObject.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(tts.onClick, popup.OnTTSButtonPressed);
        UnityEventTools.AddPersistentListener(close.onClick, popup.OnClosePressed);
        panel.SetActive(false);
        return popup;
    }

    static GameObject CreateMessagePanel(Transform parent, string name, Color color, string titleText, out Text title, out Text desc)
    {
        GameObject panel = CreatePanel(parent, name, color, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.48f));
        title = CreateText(panel.transform, "Title", titleText, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(title.rectTransform, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.92f));
        desc = CreateText(panel.transform, "Desc", "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(desc.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.52f));
        panel.SetActive(false);
        return panel;
    }

    static GameObject CreateSingleTextPanel(Transform parent, string name, Color color, out Text text)
    {
        GameObject panel = CreatePanel(parent, name, color, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.42f));
        text = CreateText(panel.transform, "Text", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(text.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
        panel.SetActive(false);
        return panel;
    }

    static GameObject CreateResultPanel(Transform parent, out Text score, out Text stars, QuizHuntController hunt)
    {
        GameObject panel = CreatePanel(parent, "ResultPanel", new Color32(7, 18, 46, 248), new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.78f));
        Text title = CreateText(panel.transform, "Title", "Hasil Hunt", 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(title.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.94f));
        score = CreateText(panel.transform, "Score", "", 32, FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        SetAnchors(score.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.72f));
        stars = CreateText(panel.transform, "Stars", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Orange);
        SetAnchors(stars.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.52f));
        Button again = CreateButton(panel.transform, "PlayAgainButton", "Main Lagi", Orange, Color.white, 24, out _, out _).GetComponent<Button>();
        SetAnchors(again.GetComponent<RectTransform>(), new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.28f));
        Button menu = CreateButton(panel.transform, "MainMenuButton", "Pilih Mode", Blue, Color.white, 24, out _, out _).GetComponent<Button>();
        SetAnchors(menu.GetComponent<RectTransform>(), new Vector2(0.12f, 0.035f), new Vector2(0.88f, 0.145f));
        UnityEventTools.AddPersistentListener(again.onClick, hunt.OnPlayAgainPressed);
        UnityEventTools.AddPersistentListener(menu.onClick, hunt.OnMainMenuPressed);
        panel.SetActive(false);
        return panel;
    }

    static GameObject CreatePlayModeOptionsOverlay(Transform parent, PlayModeOptionsController controller)
    {
        GameObject overlay = CreatePanel(parent, "PlayModeOptionsOverlay", new Color(0.02f, 0.05f, 0.12f, 0.70f), Vector2.zero, Vector2.one);
        GameObject card = CreatePanel(overlay.transform, "PlayModeOptionsCard", new Color32(255, 255, 255, 246), new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.84f));

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 34, 34);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreateText(card.transform, "Title", "Pilih Mode Bermain", 38, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetLayoutHeight(title.gameObject, 64f);

        Text subtitle = CreateText(card.transform, "Subtitle", "Setelah petunjuk, pilih mau langsung scan atau latihan dulu.", 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color32(44, 62, 98, 255));
        SetLayoutHeight(subtitle.gameObject, 64f);

        CreatePlayOptionButton(card.transform, "ScanAROptionButton", "Scan Kartu AR", Orange, controller.OpenAR);
        CreatePlayOptionButton(card.transform, "CollectionOptionButton", "Koleksi", new Color32(21, 136, 81, 255), controller.OpenCollection);
        CreatePlayOptionButton(card.transform, "HuntOptionButton", "Quiz Hunt", new Color32(147, 92, 246, 255), controller.OpenQuizHunt);
        CreatePlayOptionButton(card.transform, "CloseOptionsButton", "Tutup", new Color32(100, 116, 139, 255), controller.HideOptions);

        return overlay;
    }

    static void CreatePlayOptionButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject go = CreateButton(parent, name, label, color, Color.white, 24, out Button button, out _);
        SetLayoutHeight(go, 76f);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    static void SetLayoutHeight(GameObject go, float height)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    static void CreateFeatureButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject go = CreateButton(parent, label + "FeatureButton", label, color, Color.white, 23, out Button button, out _);
        go.AddComponent<LayoutElement>().minHeight = 76f;
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    static GameObject CreateButton(Transform parent, string name, string text, Color bg, Color fg, int fontSize, out Button button, out Text label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = bg;
        button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = bg;
        colors.highlightedColor = Color.Lerp(bg, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(bg, Color.black, 0.12f);
        colors.selectedColor = bg;
        button.colors = colors;

        label = CreateText(go.transform, "Label", text, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, fg);
        SetAnchors(label.rectTransform, Vector2.zero, Vector2.one);
        return go;
    }

    static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static void CreateUiCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Cream;
        camera.orthographic = true;
    }

    static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        _ = eventSystem;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        SetAnchors(panel.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return panel;
    }

    static GameObject CreateResponsiveArtworkFrame(Transform parent, string name, Sprite portraitSprite, Sprite landscapeSprite)
    {
        GameObject frameObject = new GameObject(name, typeof(RectTransform), typeof(AspectRatioFitter), typeof(ResponsiveArtworkFrame));
        frameObject.transform.SetParent(parent, false);
        SetAnchors(frameObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        AspectRatioFitter fitter = frameObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = portraitSprite != null && portraitSprite.rect.height > 0f
            ? portraitSprite.rect.width / portraitSprite.rect.height
            : 9f / 16f;

        GameObject imageObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(frameObject.transform, false);
        SetAnchors(imageObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = portraitSprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;

        ResponsiveArtworkFrame responsive = frameObject.GetComponent<ResponsiveArtworkFrame>();
        responsive.Configure(image, fitter, portraitSprite, landscapeSprite);
        EditorUtility.SetDirty(responsive);
        return frameObject;
    }

    static GameObject CreateFullRect(Transform parent, string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        SetAnchors(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return root;
    }

    static Button CreateTransparentButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                          UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetAnchors(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    static Sprite LoadUiSprite(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("[BuhenAR] UI artwork tidak ditemukan: " + path);
            return null;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = GetDefaultFont();
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        BuhenARTextStyle.Configure(label, fontSize, Mathf.Max(12, fontSize - 14), alignment, VerticalWrapMode.Truncate, 1.12f);
        return label;
    }

    static Font GetDefaultFont()
    {
        Font font = Resources.Load<Font>("Fonts/Nunito/Nunito-SemiBold");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        if (rect == null) return;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static T FindSceneObject<T>() where T : Object
    {
        return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }

    static IEnumerable<Button> FindSceneButtonsNamed(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (button != null && button.gameObject.scene == activeScene && button.gameObject.name == objectName)
                yield return button;
        }
    }

    static void DestroySceneObjectsNamed(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (transform != null && transform.gameObject.scene == activeScene && transform.gameObject.name == objectName)
                Object.DestroyImmediate(transform.gameObject);
        }
    }

    static void ClearButtonClick(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        EditorUtility.SetDirty(button);
    }

    static void UpdateBuildSettings()
    {
        string[] desired =
        {
            ScenesFolder + "/SplashScene.unity",
            MainMenuScenePath,
            ScenesFolder + "/GuideScene.unity",
            ScenesFolder + "/MaterialSelectScene.unity",
            ARScanScenePath,
            CollectionScenePath,
            QuizHuntScenePath,
            ScenesFolder + "/ResultScene.unity",
            ScenesFolder + "/AboutScene.unity"
        };

        var scenes = new List<EditorBuildSettingsScene>();
        foreach (string path in desired)
        {
            if (System.IO.File.Exists(path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
