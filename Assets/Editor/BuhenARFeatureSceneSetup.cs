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
        GameObject overlay = CreatePlayModeOptionsOverlay(canvas.transform, controller);
        overlay.transform.SetAsLastSibling();

        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("optionsPanel").objectReferenceValue = overlay;
        controllerObject.FindProperty("navigator").objectReferenceValue = navigation;
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        int wiredCount = 0;
        foreach (Button button in FindSceneButtonsNamed("AyoBermainHotspot"))
        {
            ClearButtonClick(button);
            UnityEventTools.AddPersistentListener(button.onClick, controller.ShowOptions);
            wiredCount++;
        }

        if (wiredCount == 0)
            Debug.LogWarning("[BuhenAR] AyoBermainHotspot tidak ditemukan di MaterialSelectScene.");

        overlay.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
        CreatePanel(canvas.transform, "Background", Navy, Vector2.zero, Vector2.one);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        CollectionManager manager = canvas.gameObject.AddComponent<CollectionManager>();
        TTSController tts = canvas.gameObject.AddComponent<TTSController>();
        canvas.gameObject.AddComponent<AudioSource>();
        _ = tts;

        Text title = CreateText(canvas.transform, "Title", "Koleksi Buah & Hewan", 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(title.rectTransform, new Vector2(0.06f, 0.9f), new Vector2(0.94f, 0.98f));

        Text totalText = CreateText(canvas.transform, "TotalDiscoveredText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Cream);
        SetAnchors(totalText.rectTransform, new Vector2(0.18f, 0.84f), new Vector2(0.82f, 0.89f));

        Button back = CreateButton(canvas.transform, "BackButton", "Kembali", Orange, Color.white, 24, out _, out _).GetComponent<Button>();
        RectTransform backRect = back.GetComponent<RectTransform>();
        SetAnchors(backRect, new Vector2(0.04f, 0.91f), new Vector2(0.22f, 0.975f));
        UnityEventTools.AddPersistentListener(back.onClick, navigation.OpenMaterialSelect);

        GameObject scrollRoot = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollRoot.transform.SetParent(canvas.transform, false);
        SetAnchors(scrollRoot.GetComponent<RectTransform>(), new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.82f));
        scrollRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollRoot.transform, false);
        SetAnchors(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
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
        grid.cellSize = new Vector2(150f, 198f);
        grid.spacing = new Vector2(14f, 18f);
        grid.padding = new RectOffset(18, 18, 18, 18);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject template = CreateCollectionItemTemplate(canvas.transform);
        template.SetActive(false);
        CollectionDetailPopup popup = CreateCollectionPopup(canvas.transform);

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
        CreatePanel(canvas.transform, "Background", new Color32(255, 246, 218, 255), Vector2.zero, Vector2.one);

        SceneNavigationController navigation = canvas.gameObject.AddComponent<SceneNavigationController>();
        QuizHuntController hunt = canvas.gameObject.AddComponent<QuizHuntController>();

        Text title = CreateText(canvas.transform, "Title", "Quiz Hunt", 48, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(title.rectTransform, new Vector2(0.08f, 0.9f), new Vector2(0.92f, 0.98f));

        Text progress = CreateText(canvas.transform, "ProgressText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Blue);
        SetAnchors(progress.rectTransform, new Vector2(0.18f, 0.84f), new Vector2(0.82f, 0.89f));

        GameObject cluePanel = CreatePanel(canvas.transform, "CluePanel", SoftPanel, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.82f));
        Text clue = CreateText(cluePanel.transform, "ClueText", "", 31, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(clue.rectTransform, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.94f));
        Text subClue = CreateText(cluePanel.transform, "ClueSubText", "", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Orange);
        SetAnchors(subClue.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.32f));

        Text score = CreateText(canvas.transform, "ScoreText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(score.rectTransform, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.47f));
        Text timer = CreateText(canvas.transform, "TimerText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Orange);
        SetAnchors(timer.rectTransform, new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.42f));

        GameObject hintPanel = CreatePanel(canvas.transform, "HintPanel", new Color32(255, 230, 153, 245), new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.36f));
        Text hintText = CreateText(hintPanel.transform, "HintText", "", 21, FontStyle.Bold, TextAnchor.MiddleCenter, Navy);
        SetAnchors(hintText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
        hintPanel.SetActive(false);

        Button scan = CreateButton(canvas.transform, "ScanButton", "Scan Sekarang", Orange, Color.white, 30, out _, out _).GetComponent<Button>();
        SetAnchors(scan.GetComponent<RectTransform>(), new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.25f));
        UnityEventTools.AddPersistentListener(scan.onClick, hunt.OnScanButtonPressed);

        Button hint = CreateButton(canvas.transform, "HintButton", "Petunjuk", Blue, Color.white, 25, out _, out _).GetComponent<Button>();
        SetAnchors(hint.GetComponent<RectTransform>(), new Vector2(0.08f, 0.06f), new Vector2(0.35f, 0.13f));
        UnityEventTools.AddPersistentListener(hint.onClick, hunt.OnShowHintPressed);

        Button skip = CreateButton(canvas.transform, "SkipButton", "Lewati", new Color32(128, 85, 30, 255), Color.white, 25, out _, out _).GetComponent<Button>();
        SetAnchors(skip.GetComponent<RectTransform>(), new Vector2(0.365f, 0.06f), new Vector2(0.635f, 0.13f));
        UnityEventTools.AddPersistentListener(skip.onClick, hunt.OnSkipPressed);

        Button menu = CreateButton(canvas.transform, "MenuButton", "Pilih Mode", Navy, Color.white, 25, out _, out _).GetComponent<Button>();
        SetAnchors(menu.GetComponent<RectTransform>(), new Vector2(0.65f, 0.06f), new Vector2(0.92f, 0.13f));
        UnityEventTools.AddPersistentListener(menu.onClick, hunt.OnMainMenuPressed);

        Text successTitle;
        Text successDesc;
        GameObject successPanel = CreateMessagePanel(canvas.transform, "SuccessPanel", new Color32(220, 252, 231, 250), "Benar!", out successTitle, out successDesc);
        Text failText;
        GameObject failPanel = CreateSingleTextPanel(canvas.transform, "FailPanel", new Color32(254, 226, 226, 250), out failText);
        Text resultScore;
        Text resultStars;
        GameObject resultPanel = CreateResultPanel(canvas.transform, out resultScore, out resultStars, hunt);

        SerializedObject huntObject = new SerializedObject(hunt);
        huntObject.FindProperty("library").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        huntObject.FindProperty("clueText").objectReferenceValue = clue;
        huntObject.FindProperty("clueSubText").objectReferenceValue = subClue;
        huntObject.FindProperty("progressText").objectReferenceValue = progress;
        huntObject.FindProperty("timerText").objectReferenceValue = timer;
        huntObject.FindProperty("scoreText").objectReferenceValue = score;
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
