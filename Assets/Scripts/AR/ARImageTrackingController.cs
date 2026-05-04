using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ARtiGraf.Core;
using ARtiGraf.Data;
using Vuforia;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARtiGraf.AR
{
    public class ARImageTrackingController : MonoBehaviour
    {
        const string DefaultLibraryResourceName = "ARtiGrafContentLibrary";
        const string DefaultVuforiaDeviceDatabasePath = "Vuforia/MediaBelajarAR_DB.xml";

        static readonly HashSet<BarcodeBehaviour.BarcodeType> SupportedBarcodeTypes = new HashSet<BarcodeBehaviour.BarcodeType>
        {
            BarcodeBehaviour.BarcodeType.QRCODE,
            BarcodeBehaviour.BarcodeType.MICROQRCODE,
            BarcodeBehaviour.BarcodeType.CODE128,
            BarcodeBehaviour.BarcodeType.CODE39,
            BarcodeBehaviour.BarcodeType.DATAMATRIX,
            BarcodeBehaviour.BarcodeType.PDF417,
            BarcodeBehaviour.BarcodeType.AZTEC
        };

        [SerializeField] VuforiaBehaviour vuforiaBehaviour;
        [SerializeField] MaterialContentController contentController;
        [SerializeField] MaterialContentLibrary library;
        [SerializeField] Transform targetsRoot;
        [SerializeField] float defaultTargetWidthMeters = 0.12f;
        [SerializeField] bool enableBarcodeFallback = false;
        [SerializeField] bool alwaysCreateBarcodeObserverOnMobile = false;
        [SerializeField] bool detectMultipleBarcodes = false;
        [SerializeField] bool useImportedVuforiaDeviceDatabase = false;
        [SerializeField] bool strictImportedVuforiaDeviceDatabaseOnly = true;
        [SerializeField] string importedVuforiaDeviceDatabasePath = DefaultVuforiaDeviceDatabasePath;
        [SerializeField] string importedVuforiaDeviceDatabaseTargets = "apple";
        [SerializeField] bool preferBarcodeTrackingForDemoMarkers = false;
        [SerializeField] bool anchorBarcodeContentToMarker = true;
        [SerializeField] bool preferTrackedImageTargetsOverBarcodeResults = true;
        [SerializeField] bool requireTrackedStatusForBarcodeSwitch = true;
        [SerializeField] bool enablePreviewModeWithoutTracking = true;
        [SerializeField] bool disableScannerForExperiment = false;
        [SerializeField] bool forceBarcodeOnlyScanning = false;
        [SerializeField] float previewModeIdleDelaySeconds = 1.1f;
        [SerializeField] bool previewModePreferLastViewedContent = true;
        [SerializeField] bool enableEditorWebcamBackground = true;
        [SerializeField] float editorWebcamStartupTimeoutSeconds = 4f;
        [SerializeField] string editorLinuxDirectCameraDevicePath = "/dev/video0";
        [SerializeField] string editorLinuxDirectCameraFfmpegPath = "/usr/bin/ffmpeg";
        [SerializeField] string editorLinuxDirectCameraInputFormat = "mjpeg";
        [SerializeField] int editorLinuxDirectCameraWidth = 640;
        [SerializeField] int editorLinuxDirectCameraHeight = 480;
        [SerializeField] int editorLinuxDirectCameraFrameRate = 30;
        [SerializeField] bool enableDesktopPreviewControls = true;
        [SerializeField] bool showBarcodeOutline = false;
        [SerializeField] bool stabilizeImageTargets = true;
        [SerializeField] bool freezePoseWhileLimited = true;
        [SerializeField] bool allowInitialLimitedImageTargetActivation = true;
        [SerializeField] bool allowExtendedTrackingForImageTargets = false;
        [SerializeField] bool configureRuntimeCameraFocus = true;
        [SerializeField] bool enableTapToRefocus = true;
        [SerializeField] bool showRuntimeTrackingDiagnostics = true;
        [SerializeField] bool allowTouchPitchRotation = true;
        [SerializeField] bool allowFullTouchRotation360 = true;
        [SerializeField] float imageTargetLostGraceSeconds = 0.55f;
        [SerializeField] float barcodeSwitchCooldownSeconds = 0.35f;
        [SerializeField] float positionSmoothTime = 0.18f;
        [SerializeField] float rotationLerpSpeed = 6.5f;
        [SerializeField] float positionSnapDistanceMeters = 0.34f;
        [SerializeField] float rotationSnapAngleDegrees = 48f;
        [SerializeField] float positionJitterDeadzoneMeters = 0.009f;
        [SerializeField] float rotationJitterDeadzoneDegrees = 3.5f;
        [SerializeField] float minimumAnchorUpdateIntervalSeconds = 0.025f;
        [SerializeField] float autofocusCooldownSeconds = 0.45f;
        [SerializeField] float autofocusRestoreDelaySeconds = 0.35f;
        [SerializeField] bool restoreFlashOffOnExit = true;
        [SerializeField] float quizHuntAutoReturnDelaySeconds = 0f;

        [Header("Interaction Controllers")]
        [SerializeField] ARTouchInteractionController touchController;
        [SerializeField] ARtiGraf.UI.ScanFeedbackUI scanFeedbackUI;
        [SerializeField] ARLearningFeedbackController learningFeedback;
        [SerializeField] float touchYawDegreesPerPixel = 0.18f;
        [SerializeField] float touchPitchDegreesPerPixel = 0.12f;
        [SerializeField] float touchPanMetersPerPixel = 0.00035f;
        [SerializeField] float pinchScaleSensitivity = 0.0045f;
        [SerializeField] Vector2 interactionScaleRange = new Vector2(0.75f, 1.85f);
        [SerializeField] Vector2 interactionPanRange = new Vector2(0.09f, 0.09f);
        [SerializeField] Vector2 previewPanRange = new Vector2(0.24f, 0.18f);
        [SerializeField] float desktopRotateDegreesPerPixel = 0.24f;
        [SerializeField] float desktopPanMetersPerPixel = 0.0012f;
        [SerializeField] float interactionPitchClampDegrees = 55f;
        [SerializeField] float touchSelectionPaddingPixels = 72f;
        [SerializeField] float touchSelectionMaxDistancePixels = 240f;

        [Header("Placement")]
        [SerializeField] bool normalizeSpawnedContentPivot = true;
        [SerializeField] Vector3 imageTargetContentOffset = new Vector3(0f, 0.035f, 0f);

        [Header("Barcode Flashcard Settings")]
        [Tooltip("If the QR code is on the back of a flashcard, flip the 3D object to appear on the front side.")]
        [SerializeField] bool flipBarcodeContentToFront = true;
        [Tooltip("The offset applied to push the object through the card thickness to the front side.")]
        [SerializeField] Vector3 barcodeFrontSideOffset = new Vector3(0f, -0.01f, 0f);
        [Tooltip("The rotation applied to face the object correctly on the front side.")]
        [SerializeField] Vector3 barcodeFrontSideEulerAngles = new Vector3(0f, 180f, 0f);

        [Header("Preview")]
        [SerializeField] Vector3 barcodePreviewOffset = new Vector3(0f, -0.05f, 0.6f);
        [SerializeField] Vector3 barcodePreviewEulerAngles = Vector3.zero;

        readonly Dictionary<ObserverBehaviour, MaterialContentData> registeredImageTargets = new Dictionary<ObserverBehaviour, MaterialContentData>();
        readonly Dictionary<ObserverBehaviour, MaterialContentData> activeContent = new Dictionary<ObserverBehaviour, MaterialContentData>();
        readonly Dictionary<ObserverBehaviour, GameObject> spawnedPrefabs = new Dictionary<ObserverBehaviour, GameObject>();
        readonly Dictionary<ObserverBehaviour, string> spawnedContentIds = new Dictionary<ObserverBehaviour, string>();
        readonly Dictionary<ObserverBehaviour, string> activeBarcodePayloads = new Dictionary<ObserverBehaviour, string>();
        readonly Dictionary<ObserverBehaviour, Transform> contentAnchors = new Dictionary<ObserverBehaviour, Transform>();
        readonly Dictionary<ObserverBehaviour, Vector3> anchorPositionVelocities = new Dictionary<ObserverBehaviour, Vector3>();
        readonly Dictionary<ObserverBehaviour, float> anchorLastPoseUpdateTimes = new Dictionary<ObserverBehaviour, float>();
        readonly Dictionary<ObserverBehaviour, Status> currentStatuses = new Dictionary<ObserverBehaviour, Status>();
        readonly Dictionary<ObserverBehaviour, float> trackingGraceDeadlines = new Dictionary<ObserverBehaviour, float>();
        readonly Dictionary<ObserverBehaviour, Transform> spawnedInteractionRoots = new Dictionary<ObserverBehaviour, Transform>();
        readonly Dictionary<ObserverBehaviour, Vector3> spawnedBaseScales = new Dictionary<ObserverBehaviour, Vector3>();
        readonly Dictionary<ObserverBehaviour, Vector3> spawnedBaseLocalPositions = new Dictionary<ObserverBehaviour, Vector3>();
        readonly Dictionary<ObserverBehaviour, Vector2> spawnedUserRotationAngles = new Dictionary<ObserverBehaviour, Vector2>();
        readonly Dictionary<ObserverBehaviour, string> suppressedBarcodePayloads = new Dictionary<ObserverBehaviour, string>();
        readonly Dictionary<ObserverBehaviour, float> barcodeSwitchCooldownDeadlines = new Dictionary<ObserverBehaviour, float>();
        readonly Dictionary<ObserverBehaviour, string> observerTrackingNames = new Dictionary<ObserverBehaviour, string>();
        readonly HashSet<string> importedVuforiaDatabaseTargetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool targetsCreated;
        bool imageTargetCreationUnsupported;
        bool importedVuforiaDatabaseScanned;
        Transform barcodePreviewAnchor;
        ObserverBehaviour activeInteractionObserver;
        GameObject activeInteractionObject;
        Transform activeInteractionRoot;
        bool activeInteractionIsPreview;
        GameObject previewPlacementObject;
        Transform previewInteractionRoot;
        MaterialContentData previewContent;
        LinuxEditorDirectCameraFeed editorLinuxDirectCameraFeed;
        Texture2D editorLinuxDirectCameraTexture;
        Vector3 previewBaseScale = Vector3.one;
        Vector3 previewBaseLocalPosition = Vector3.zero;
        Vector2 previewUserRotationAngles = Vector2.zero;
        Vector3 previousMousePosition;
        Coroutine editorWebcamStartupRoutine;
        float previewModeReadyAt = -1f;
        bool editorWebcamReady;
        bool editorManualPreviewRequested;
        bool hasResolvedTrackableContentThisSession;
        float nextAutoFocusRequestTime;
        float restoreContinuousAutoFocusAt = -1f;
        bool continuousAutoFocusSupported;
        bool enhancedTouchEnabled;
        bool flashEnabled;
        bool quizHuntScanResultSent;
        Coroutine quizHuntReturnRoutine;
        MaterialContentData lastResolvedPreviewContent;
        string cachedImportedVuforiaDatabasePath = string.Empty;

        void Awake()
        {
            EnsureLibrary();
            EnsureTouchController();
            if (touchController != null)
            {
                touchController.OnPanDelta += HandlePanDelta;
                touchController.OnScaleDelta += HandleScaleDelta;
                touchController.OnRotateDelta += HandleRotateDelta;
                touchController.OnTap += HandleTap;
            }
        }

        void Start()
        {
            EnsureLibrary();
            contentController?.InitializeOverlayContext();

            if (contentController != null)
            {
                contentController.SetStatus(BuildInitialStatusMessage());
                contentController.HideContent();
            }

            if (IsPreviewOnlyModeActive())
            {
                if (ShouldUsePreviewOnlyMode())
                {
                    InitializeEditorPreviewRuntime();
                }
                else
                {
                    DisableScannerRuntime();
                    contentController?.SetEditorCameraFeedActive(false);
                    contentController?.SetEditorCameraFeedTexture(null, false, false, 0);
                    editorManualPreviewRequested = true;
                    hasResolvedTrackableContentThisSession = true;
                }

                previewModeReadyAt = Time.unscaledTime;
                UpdatePreviewMode();
                return;
            }

            if (VuforiaApplication.Instance != null)
            {
                VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;

                if (VuforiaApplication.Instance.IsRunning)
                {
                    ConfigureRuntimeCameraFocus();
                    ApplyFlashState(false);
                    CreateTargets();
                }
            }
        }

        void OnDestroy()
        {
            if (VuforiaApplication.Instance != null)
            {
                VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
            }

            if (touchController != null)
            {
                touchController.OnPanDelta -= HandlePanDelta;
                touchController.OnScaleDelta -= HandleScaleDelta;
                touchController.OnRotateDelta -= HandleRotateDelta;
                touchController.OnTap -= HandleTap;
            }

            if (restoreFlashOffOnExit && flashEnabled)
            {
                flashEnabled = false;
                ApplyFlashState(false);
            }

            ShutdownEditorPreviewRuntime();
            ExitPreviewMode(false);
            if (quizHuntReturnRoutine != null)
            {
                StopCoroutine(quizHuntReturnRoutine);
                quizHuntReturnRoutine = null;
            }
            UnregisterAllTargets();
        }

        void OnVuforiaStarted()
        {
            ConfigureRuntimeCameraFocus();
            ApplyFlashState(false);
            CreateTargets();
        }

        void EnsureTouchController()
        {
            if (touchController == null)
            {
                touchController = GetComponent<ARTouchInteractionController>();
            }

            if (touchController == null)
            {
                touchController = gameObject.AddComponent<ARTouchInteractionController>();
            }

            touchController.enableDesktopPreviewControls = enableDesktopPreviewControls;
        }

        public bool IsFlashEnabled => flashEnabled;

        public bool ToggleFlash()
        {
            return SetFlashEnabled(!flashEnabled);
        }

        public bool SetFlashEnabled(bool enabled)
        {
            flashEnabled = enabled;
            return ApplyFlashState(true);
        }

        bool ApplyFlashState(bool showStatus)
        {
            CameraDevice cameraDevice = vuforiaBehaviour != null ? vuforiaBehaviour.CameraDevice : null;
            if (cameraDevice == null || !cameraDevice.IsActive)
            {
                if (showStatus)
                {
                    contentController?.SetStatus("Flash kamera belum siap. Tunggu scanner aktif sebentar.");
                }
                return false;
            }

            if (!cameraDevice.IsFlashSupported())
            {
                flashEnabled = false;
                if (showStatus)
                {
                    contentController?.SetStatus("Flash tidak didukung di kamera/perangkat ini.");
                }
                return false;
            }

            bool applied = cameraDevice.SetFlash(flashEnabled);
            if (showStatus)
            {
                contentController?.SetStatus(applied
                    ? (flashEnabled ? "Flash kamera aktif." : "Flash kamera mati.")
                    : "Gagal mengubah flash kamera di perangkat ini.");
            }

            return applied;
        }

        static bool ShouldUsePreviewOnlyMode()
        {
#if UNITY_EDITOR
            return Application.platform == RuntimePlatform.LinuxEditor;
#else
            return false;
#endif
        }

        bool IsPreviewOnlyModeActive()
        {
            return ShouldUsePreviewOnlyMode() || disableScannerForExperiment;
        }

        void DisableScannerRuntime()
        {
            if (vuforiaBehaviour != null && vuforiaBehaviour.enabled)
            {
                vuforiaBehaviour.enabled = false;
            }
        }

        void LateUpdate()
        {
            UpdateEditorPreviewRuntime();
            UpdateTrackedBarcodeContent();
            ExpireImageTargetGracePeriods();
            UpdateStabilizedAnchors();
            UpdatePreviewMode();
            UpdatePendingCameraFocusRestore();
        }

        void CreateTargets()
        {
            if (targetsCreated)
            {
                return;
            }

            if (vuforiaBehaviour == null)
            {
                contentController?.SetStatus("Komponen scanner belum terhubung di scene.");
                return;
            }

            if (!EnsureLibrary())
            {
                contentController?.SetStatus("Content library belum terhubung di scene.");
                return;
            }

            ObserverFactory observerFactory = vuforiaBehaviour.ObserverFactory;
            if (observerFactory == null)
            {
                contentController?.SetStatus("Mesin scanner belum siap membuat target.");
                return;
            }

            Transform parent = targetsRoot != null ? targetsRoot : transform;
            int createdImageTargets = 0;
            bool barcodeReady = false;
            bool hasBarcodeOnlyTargets = false;
            var createdTrackingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var createdTargetLabels = new List<string>();

            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData content = library.Items[i];
                if (!IsRuntimeContentAllowed(content))
                {
                    continue;
                }

                if (ARtiGraf.Core.AppSession.SelectedCategory.HasValue &&
                    content.Category != ARtiGraf.Core.AppSession.SelectedCategory.Value)
                {
                    continue;
                }

                if (forceBarcodeOnlyScanning || ShouldPreferBarcodeTracking(content))
                {
                    hasBarcodeOnlyTargets = true;
                    continue;
                }

                float targetWidth = content.TargetWidthMeters > 0f ? content.TargetWidthMeters : defaultTargetWidthMeters;
                ImageTargetBehaviour imageTarget;

                if (TryCreateImageTargetFromImportedDatabase(observerFactory, content, out imageTarget))
                {
                    // Device database target found and created successfully.
                }
                else if (ShouldSkipRuntimeTextureTarget(content))
                {
                    continue;
                }
                else if (content.ReferenceImageTexture != null)
                {
                    try
                    {
                        imageTarget = observerFactory.CreateImageTarget(
                            content.ReferenceImageTexture,
                            targetWidth,
                            content.ReferenceImageName
                        );
                    }
                    catch (Exception ex)
                    {
                        imageTargetCreationUnsupported = true;
                        Debug.LogWarning(
                            "[ARtiGraf] Runtime image target tidak tersedia. " +
                            "Aplikasi akan lanjut memakai barcode fallback. " +
                            "Target yang gagal: " + content.ReferenceImageName + ". " + ex.Message);
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "[ARtiGraf] Content dilewati karena tidak punya reference texture atau target device database: " +
                        content.Id);
                    continue;
                }

                if (imageTarget == null)
                {
                    continue;
                }

                RegisterImageTarget(imageTarget, content, content.ReferenceImageName, parent);
                if (!string.IsNullOrWhiteSpace(content.ReferenceImageName))
                {
                    createdTrackingNames.Add(content.ReferenceImageName);
                }
                createdTargetLabels.Add(GetTrackingDisplayLabel(content, content.ReferenceImageName));
                createdImageTargets++;
            }

            createdImageTargets += CreateImportedAliasImageTargets(observerFactory, parent, createdTrackingNames, createdTargetLabels);

            bool shouldCreateBarcodeFallback = enableBarcodeFallback &&
                (!Application.isMobilePlatform ||
                 alwaysCreateBarcodeObserverOnMobile ||
                 createdImageTargets == 0 ||
                 imageTargetCreationUnsupported ||
                 hasBarcodeOnlyTargets);
            if (shouldCreateBarcodeFallback)
            {
                bool allowMultipleBarcodes = detectMultipleBarcodes && !Application.isMobilePlatform;
                BarcodeBehaviour barcodeObserver = observerFactory.CreateBarcodeBehaviour(SupportedBarcodeTypes, allowMultipleBarcodes);
                if (barcodeObserver != null)
                {
                    Debug.Log("[ARtiGraf] Barcode observer berhasil dibuat.");
                    barcodeObserver.gameObject.name = "BarcodeObserver";
                    barcodeObserver.transform.SetParent(parent, false);
                    barcodeObserver.OnTargetStatusChanged += OnTargetStatusChanged;
                    barcodeObserver.OnBehaviourDestroyed += OnObserverDestroyed;
                    AttachBarcodeOutline(barcodeObserver);
                    barcodeReady = true;
                }
            }

            targetsCreated = true;
            if (createdImageTargets > 0 && barcodeReady)
            {
                contentController?.SetStatus(BuildScannerReadyStatus(createdImageTargets, barcodeReady, createdTargetLabels));
            }
            else if (barcodeReady && imageTargetCreationUnsupported)
            {
                contentController?.SetStatus("Image target device database belum lengkap, jadi scanner beralih ke QR atau barcode. Gunakan marker lama atau QR buah dan hewan yang sesuai.");
            }
            else if (createdImageTargets > 0)
            {
                contentController?.SetStatus(BuildScannerReadyStatus(createdImageTargets, barcodeReady, createdTargetLabels));
            }
            else if (barcodeReady)
            {
                contentController?.SetStatus(forceBarcodeOnlyScanning
                    ? "Mode barcode aktif. Scan QR atau barcode " + BuildExampleHint() + "."
                    : "Scanner barcode siap. Scan QR atau barcode " + BuildExampleHint() + ".");
            }
            else
            {
                contentController?.SetStatus("Tidak ada target scan yang berhasil dibuat.");
            }
        }

        int CreateImportedAliasImageTargets(
            ObserverFactory observerFactory,
            Transform parent,
            HashSet<string> createdTrackingNames,
            List<string> createdTargetLabels)
        {
            if (observerFactory == null || !useImportedVuforiaDeviceDatabase)
            {
                return 0;
            }

            string databasePath = GetImportedVuforiaDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath) || importedVuforiaDatabaseTargetNames.Count == 0)
            {
                return 0;
            }

            int createdCount = 0;
            foreach (string targetName in importedVuforiaDatabaseTargetNames)
            {
                if (string.IsNullOrWhiteSpace(targetName) ||
                    (createdTrackingNames != null && createdTrackingNames.Contains(targetName)))
                {
                    continue;
                }

                if (!TryResolveRuntimeContentForImportedTarget(targetName, out MaterialContentData content))
                {
                    continue;
                }

                try
                {
                    ImageTargetBehaviour imageTarget = observerFactory.CreateImageTarget(databasePath, targetName);
                    if (imageTarget == null)
                    {
                        continue;
                    }

                    RegisterImageTarget(imageTarget, content, targetName, parent);
                    createdTrackingNames?.Add(targetName);
                    createdTargetLabels?.Add(GetTrackingDisplayLabel(content, targetName));
                    createdCount++;
                    Debug.Log("[ARtiGraf] Device database target legacy dipetakan: " + targetName + " -> " + content.Id);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        "[ARtiGraf] Gagal membuat image target legacy dari device database untuk " +
                        targetName + ". " + ex.Message);
                }
            }

            return createdCount;
        }

        bool TryResolveRuntimeContentForImportedTarget(string targetName, out MaterialContentData content)
        {
            content = null;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            if (contentController != null && contentController.TryGetContentForScanPayload(targetName, out content))
            {
                return true;
            }

            if (!EnsureLibrary() || !library.TryGetByScanPayload(targetName, out content))
            {
                content = null;
                return false;
            }

            if (!IsRuntimeContentAllowed(content))
            {
                content = null;
                return false;
            }

            if (ARtiGraf.Core.AppSession.SelectedCategory.HasValue &&
                content.Category != ARtiGraf.Core.AppSession.SelectedCategory.Value)
            {
                content = null;
                return false;
            }

            return true;
        }

        void RegisterImageTarget(
            ImageTargetBehaviour imageTarget,
            MaterialContentData content,
            string trackingName,
            Transform parent)
        {
            if (imageTarget == null || content == null)
            {
                return;
            }

            string resolvedTrackingName = string.IsNullOrWhiteSpace(trackingName)
                ? content.ReferenceImageName
                : trackingName;

            imageTarget.gameObject.name = "ImageTarget_" + resolvedTrackingName;
            imageTarget.transform.SetParent(parent, false);
            imageTarget.OnTargetStatusChanged += OnTargetStatusChanged;
            imageTarget.OnBehaviourDestroyed += OnObserverDestroyed;
            registeredImageTargets[imageTarget] = content;
            observerTrackingNames[imageTarget] = resolvedTrackingName;

            // Pre-create object once so target activation only needs to enable and align it.
            CreatePrefabInstance(imageTarget, content);
        }

        bool EnsureLibrary()
        {
            if (library != null)
            {
                return true;
            }

            if (contentController != null && contentController.EnsureLibrary())
            {
                library = contentController.Library;
            }

            if (library == null)
            {
                library = Resources.Load<MaterialContentLibrary>(DefaultLibraryResourceName);
            }

            return library != null;
        }

        bool IsRuntimeContentAllowed(MaterialContentData content)
        {
            if (contentController != null)
            {
                return contentController.IsRuntimeContentAllowed(content);
            }

            return content != null &&
                   content.HasPrefab &&
                   !content.IsDemoContent;
        }

        void CreatePrefabInstance(ObserverBehaviour observer, MaterialContentData content)
        {
            if (observer == null || content == null || content.Prefab == null)
            {
                return;
            }

            if (spawnedContentIds.TryGetValue(observer, out string existingContentId) &&
                existingContentId == content.Id &&
                spawnedPrefabs.ContainsKey(observer))
            {
                return;
            }

            if (spawnedPrefabs.TryGetValue(observer, out GameObject existingPrefab))
            {
                Destroy(existingPrefab);
                spawnedPrefabs.Remove(observer);
                spawnedContentIds.Remove(observer);
                spawnedInteractionRoots.Remove(observer);
                spawnedBaseScales.Remove(observer);
                spawnedBaseLocalPositions.Remove(observer);
                spawnedUserRotationAngles.Remove(observer);
            }

            Transform parent = GetContentParent(observer);
            GameObject placementRoot = new GameObject(content.Id + "_PlacementRoot");
            Transform placementTransform = placementRoot.transform;
            placementTransform.SetParent(parent, false);
            bool isBarcode = observer is BarcodeBehaviour;
            bool isAnchoredBarcode = isBarcode && ShouldAnchorBarcodeToMarker(observer);

            if (isBarcode && !isAnchoredBarcode)
            {
                placementTransform.localPosition = Vector3.zero;
                placementTransform.localRotation = Quaternion.identity;
            }
            else if (isAnchoredBarcode && flipBarcodeContentToFront)
            {
                placementTransform.localPosition = barcodeFrontSideOffset;
                placementTransform.localRotation = Quaternion.Euler(barcodeFrontSideEulerAngles);
            }
            else
            {
                placementTransform.localPosition = imageTargetContentOffset;
                placementTransform.localRotation = Quaternion.identity;
            }
            placementTransform.localScale = Vector3.one;

            GameObject interactionRootObject = new GameObject(content.Id + "_InteractionRoot");
            Transform interactionRoot = interactionRootObject.transform;
            interactionRoot.SetParent(placementTransform, false);
            interactionRoot.localPosition = Vector3.zero;
            interactionRoot.localRotation = Quaternion.identity;
            interactionRoot.localScale = Vector3.one;

            GameObject spawnedObject = Instantiate(content.Prefab, interactionRoot);
            NormalizeSpawnedContentPlacement(interactionRoot, spawnedObject.transform);

            placementRoot.SetActive(false);
            spawnedPrefabs[observer] = placementRoot;
            spawnedInteractionRoots[observer] = interactionRoot;
            spawnedContentIds[observer] = content.Id;
            spawnedBaseScales[observer] = interactionRoot.localScale;
            spawnedBaseLocalPositions[observer] = placementTransform.localPosition;
            spawnedUserRotationAngles[observer] = Vector2.zero;
        }

        void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
        {
            if (behaviour == null)
            {
                return;
            }

            bool hadPreviousStatus = currentStatuses.TryGetValue(behaviour, out Status previousStatus);
            bool wasActivelyTracked = hadPreviousStatus && IsActivelyTracked(behaviour, previousStatus);
            if (!hadPreviousStatus || previousStatus != targetStatus.Status)
            {
                Debug.Log("[ARtiGraf] Target status berubah: " + behaviour.name + " -> " + targetStatus.Status);
                if (showRuntimeTrackingDiagnostics &&
                    !(behaviour is BarcodeBehaviour) &&
                    !activeContent.ContainsKey(behaviour) &&
                    (targetStatus.Status == Status.LIMITED || !IsTracking(targetStatus.Status)))
                {
                    contentController?.SetStatus(
                        "Target " + GetObserverTrackingLabel(behaviour) +
                        " terbaca status " + targetStatus.Status +
                        ". Tahan kartu lebih dekat, rata, dan terang sampai TRACKED.");
                }
            }
            currentStatuses[behaviour] = targetStatus.Status;
            bool allowLimitedInitialActivation = ShouldAllowInitialLimitedImageTargetActivation(behaviour, targetStatus.Status);
            if (IsActivelyTracked(behaviour, targetStatus.Status) || allowLimitedInitialActivation)
            {
                if (!(behaviour is BarcodeBehaviour) && !wasActivelyTracked)
                {
                    RequestAutoFocusIfNeeded();
                }

                trackingGraceDeadlines.Remove(behaviour);
                UpdateResolvedContent(behaviour, targetStatus.Status, forceOverlayRefresh: true);
                return;
            }

            if (ShouldHoldImageTargetDuringGrace(behaviour, targetStatus.Status))
            {
                trackingGraceDeadlines[behaviour] = Time.time + imageTargetLostGraceSeconds;
                return;
            }

            trackingGraceDeadlines.Remove(behaviour);
            if (!IsTracking(targetStatus))
            {
                HandleTrackingLost(behaviour);
                return;
            }
        }

        bool ShouldAllowInitialLimitedImageTargetActivation(ObserverBehaviour behaviour, Status status)
        {
            return allowInitialLimitedImageTargetActivation &&
                   behaviour != null &&
                   !(behaviour is BarcodeBehaviour) &&
                   status == Status.LIMITED &&
                   registeredImageTargets.ContainsKey(behaviour) &&
                   !activeContent.ContainsKey(behaviour);
        }

        void HandleTrackingLost(ObserverBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            DeactivateSpawnedInstance(behaviour);
            activeContent.Remove(behaviour);
            activeBarcodePayloads.Remove(behaviour);
            suppressedBarcodePayloads.Remove(behaviour);
            barcodeSwitchCooldownDeadlines.Remove(behaviour);
            trackingGraceDeadlines.Remove(behaviour);
            previewModeReadyAt = -1f;
            contentController?.SetStatus(behaviour is BarcodeBehaviour
                ? "Marker hilang. Coba arahkan ulang ke " + BuildExampleHint() + "."
                : "Tracking hilang (" + GetObserverTrackingLabel(behaviour) + "). Arahkan ulang ke marker " + BuildExampleHint() + ".");
            learningFeedback?.OnContentLost();
            RefreshOverlayState();
        }

        void UpdateResolvedContent(ObserverBehaviour behaviour, Status currentStatus, bool forceOverlayRefresh)
        {
            if (behaviour == null)
            {
                return;
            }

            if (!TryResolveContent(behaviour, out MaterialContentData content, out string barcodePayload))
            {
                if (behaviour is BarcodeBehaviour &&
                    activeContent.TryGetValue(behaviour, out MaterialContentData retainedContent) &&
                    retainedContent != null)
                {
                    contentController?.SetStatus("Marker sedang dibaca ulang. Tahan kartu sebentar agar objek tetap stabil.");
                    return;
                }

                if (spawnedPrefabs.TryGetValue(behaviour, out GameObject unknownObject))
                {
                    unknownObject.SetActive(false);
                }

                activeContent.Remove(behaviour);
                activeBarcodePayloads.Remove(behaviour);
                suppressedBarcodePayloads.Remove(behaviour);
                barcodeSwitchCooldownDeadlines.Remove(behaviour);
                contentController?.SetStatus(behaviour is BarcodeBehaviour
                    ? "Marker terdeteksi, tetapi belum cocok dengan materi. Coba " + BuildExampleHint() + "."
                    : "Target " + GetObserverTrackingLabel(behaviour) + " terdeteksi, tetapi materi tidak ditemukan.");
                RefreshOverlayState();
                return;
            }

            bool hasActiveContent = activeContent.TryGetValue(behaviour, out MaterialContentData currentContent);
            bool contentChanged = !hasActiveContent || currentContent != content;
            bool barcodePayloadChanged = behaviour is BarcodeBehaviour &&
                (!activeBarcodePayloads.TryGetValue(behaviour, out string previousPayload) ||
                 !string.Equals(previousPayload, barcodePayload, StringComparison.Ordinal));

            if (ShouldSuppressBarcodeResolution(behaviour, content, barcodePayload, currentStatus, contentChanged, barcodePayloadChanged))
            {
                return;
            }

            previewModeReadyAt = -1f;
            ExitPreviewMode(false);
            ReleaseInactivePrefabs(behaviour);
            CreatePrefabInstance(behaviour, content);
            if (spawnedPrefabs.TryGetValue(behaviour, out GameObject spawnedObject))
            {
                SyncAnchorToObserver(behaviour, true);
                spawnedObject.SetActive(true);
            }

            activeContent[behaviour] = content;
            hasResolvedTrackableContentThisSession = true;
            lastResolvedPreviewContent = content;
            if (behaviour is BarcodeBehaviour)
            {
                activeBarcodePayloads[behaviour] = barcodePayload;
                if (contentChanged || barcodePayloadChanged)
                {
                    barcodeSwitchCooldownDeadlines[behaviour] = Time.unscaledTime + Mathf.Max(0f, barcodeSwitchCooldownSeconds);
                }
            }

            suppressedBarcodePayloads.Remove(behaviour);

            if (!forceOverlayRefresh && !contentChanged && !barcodePayloadChanged)
            {
                return;
            }

            if (behaviour is BarcodeBehaviour && barcodePayloadChanged)
            {
                Debug.Log("[ARtiGraf] Barcode aktif berganti: " + barcodePayload + " -> " + content.Id);
            }

            contentController?.ShowContent(content);
            learningFeedback?.OnContentDetected(content,
                spawnedInteractionRoots.TryGetValue(behaviour, out Transform lr) ? lr : null);

            ReportQuizHuntScanResultIfNeeded(content);

            string statusMsg = behaviour is BarcodeBehaviour
                ? "Marker cocok dengan \"" + GetContentDisplayName(content) + "\". Objek 3D aktif diperbarui."
                : currentStatus == Status.LIMITED && freezePoseWhileLimited
                    ? "Tracking terbatas. Objek ditahan agar tidak bergetar, coba stabilkan kamera."
                    : "Tracking aktif. Panel materi diperbarui.";
            contentController?.SetStatus(statusMsg);
            if (scanFeedbackUI != null && (contentChanged || barcodePayloadChanged))
            {
                scanFeedbackUI.ShowFeedback(content, statusMsg);
            }
        }

        void ReportQuizHuntScanResultIfNeeded(MaterialContentData content)
        {
            if (content == null || quizHuntScanResultSent || !AppSession.IsQuizHuntActive)
            {
                return;
            }

            quizHuntScanResultSent = true;
            AppSession.SetQuizHuntScanResult(content.Id);
            contentController?.SetStatus("Kartu terbaca. Objek bisa diputar, digeser, dan zoom. Tekan Kembali untuk cek jawaban.");

            if (quizHuntReturnRoutine != null)
            {
                StopCoroutine(quizHuntReturnRoutine);
                quizHuntReturnRoutine = null;
            }

            if (quizHuntAutoReturnDelaySeconds > 0f)
            {
                quizHuntReturnRoutine = StartCoroutine(ReturnToQuizHuntAfterDelay());
            }
        }

        IEnumerator ReturnToQuizHuntAfterDelay()
        {
            yield return new WaitForSecondsRealtime(quizHuntAutoReturnDelaySeconds);

            SceneNavigationController navigator = FindAnyObjectByType<SceneNavigationController>(FindObjectsInactive.Include);
            if (navigator != null)
            {
                navigator.OpenQuizHunt();
                yield break;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("QuizHuntScene", LoadSceneMode.Single);
            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = true;
                while (!loadOperation.isDone)
                    yield return null;
            }
            else
            {
                SceneManager.LoadScene("QuizHuntScene");
            }
        }

        static bool IsTracking(TargetStatus targetStatus)
        {
            return targetStatus.Status == Status.TRACKED ||
                   targetStatus.Status == Status.EXTENDED_TRACKED ||
                   targetStatus.Status == Status.LIMITED;
        }

        static bool IsTracking(Status status)
        {
            return status == Status.TRACKED ||
                   status == Status.EXTENDED_TRACKED ||
                   status == Status.LIMITED;
        }

        bool IsActivelyTracked(ObserverBehaviour behaviour, Status status)
        {
            if (behaviour == null)
            {
                return false;
            }

            if (behaviour is BarcodeBehaviour)
            {
                return status == Status.TRACKED || status == Status.LIMITED;
            }

            if (status == Status.TRACKED)
            {
                return true;
            }

            return status == Status.EXTENDED_TRACKED && allowExtendedTrackingForImageTargets;
        }

        bool ShouldHoldImageTargetDuringGrace(ObserverBehaviour behaviour, Status status)
        {
            return behaviour != null &&
                   !(behaviour is BarcodeBehaviour) &&
                   imageTargetLostGraceSeconds > 0f &&
                   activeContent.ContainsKey(behaviour) &&
                   (status == Status.LIMITED || status == Status.EXTENDED_TRACKED);
        }

        void OnObserverDestroyed(ObserverBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            behaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
            behaviour.OnBehaviourDestroyed -= OnObserverDestroyed;
            ReleaseSpawnedInstance(behaviour);
            registeredImageTargets.Remove(behaviour);
            activeContent.Remove(behaviour);
            activeBarcodePayloads.Remove(behaviour);
            suppressedBarcodePayloads.Remove(behaviour);
            barcodeSwitchCooldownDeadlines.Remove(behaviour);
            currentStatuses.Remove(behaviour);
            trackingGraceDeadlines.Remove(behaviour);
            spawnedBaseScales.Remove(behaviour);
            spawnedBaseLocalPositions.Remove(behaviour);
            spawnedInteractionRoots.Remove(behaviour);
            spawnedUserRotationAngles.Remove(behaviour);
            observerTrackingNames.Remove(behaviour);

            if (contentAnchors.TryGetValue(behaviour, out Transform anchor) && anchor != null)
            {
                Destroy(anchor.gameObject);
            }

            contentAnchors.Remove(behaviour);
            anchorPositionVelocities.Remove(behaviour);
            anchorLastPoseUpdateTimes.Remove(behaviour);
        }

        void ReleaseInactivePrefabs(ObserverBehaviour activeBehaviour)
        {
            var observers = new List<ObserverBehaviour>(spawnedPrefabs.Keys);
            for (int i = 0; i < observers.Count; i++)
            {
                ObserverBehaviour observer = observers[i];
                if (observer == null || observer == activeBehaviour)
                {
                    continue;
                }

                DeactivateSpawnedInstance(observer);
                activeContent.Remove(observer);
                activeBarcodePayloads.Remove(observer);
                suppressedBarcodePayloads.Remove(observer);
                barcodeSwitchCooldownDeadlines.Remove(observer);
            }
        }

        void DeactivateSpawnedInstance(ObserverBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            if (spawnedPrefabs.TryGetValue(behaviour, out GameObject spawned) && spawned != null)
            {
                spawned.SetActive(false);
            }

            activeBarcodePayloads.Remove(behaviour);
            suppressedBarcodePayloads.Remove(behaviour);
            barcodeSwitchCooldownDeadlines.Remove(behaviour);
            if (activeInteractionObserver == behaviour)
            {
                ResetTouchInteractionState();
            }
        }

        void ReleaseSpawnedInstance(ObserverBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            if (spawnedPrefabs.TryGetValue(behaviour, out GameObject spawned))
            {
                if (spawned != null)
                {
                    Destroy(spawned);
                }
            }

            spawnedPrefabs.Remove(behaviour);
            spawnedContentIds.Remove(behaviour);
            spawnedBaseScales.Remove(behaviour);
            spawnedBaseLocalPositions.Remove(behaviour);
            spawnedInteractionRoots.Remove(behaviour);
            spawnedUserRotationAngles.Remove(behaviour);
            barcodeSwitchCooldownDeadlines.Remove(behaviour);
        }

        void AttachBarcodeOutline(BarcodeBehaviour barcodeObserver)
        {
            if (!showBarcodeOutline)
            {
                BarcodeOutlineBehaviour existingOutline = barcodeObserver.GetComponent<BarcodeOutlineBehaviour>();
                if (existingOutline != null)
                {
                    existingOutline.enabled = false;
                }

                return;
            }

            BarcodeOutlineBehaviour outline = barcodeObserver.GetComponent<BarcodeOutlineBehaviour>();
            if (outline == null)
            {
                outline = barcodeObserver.gameObject.AddComponent<BarcodeOutlineBehaviour>();
            }

            if (outline.Materials == null || outline.Materials.Length == 0)
            {
                outline.Materials = VuforiaConfiguration.Instance.RuntimeResources.Register.BarcodeOutlineMaterials;
            }
        }

        Transform GetContentParent(ObserverBehaviour observer)
        {
            if (observer is BarcodeBehaviour)
            {
                return ShouldAnchorBarcodeToMarker(observer)
                    ? GetOrCreateImageTargetAnchor(observer)
                    : GetBarcodePreviewAnchor();
            }

            if (stabilizeImageTargets)
            {
                return GetOrCreateImageTargetAnchor(observer);
            }

            return observer.transform;
        }

        Transform GetOrCreateImageTargetAnchor(ObserverBehaviour observer)
        {
            if (observer == null)
            {
                return transform;
            }

            if (contentAnchors.TryGetValue(observer, out Transform anchor) && anchor != null)
            {
                return anchor;
            }

            GameObject anchorObject = new GameObject("ContentAnchor_" + observer.name);
            anchor = anchorObject.transform;
            Transform parent = targetsRoot != null ? targetsRoot : transform;
            anchor.SetParent(parent, false);
            contentAnchors[observer] = anchor;
            anchorPositionVelocities[observer] = Vector3.zero;
            anchorLastPoseUpdateTimes[observer] = Time.time;
            SnapAnchorToObserver(observer, anchor);
            return anchor;
        }

        Transform GetBarcodePreviewAnchor()
        {
            if (barcodePreviewAnchor != null)
            {
                return barcodePreviewAnchor;
            }

            Camera previewCamera = Camera.main;
            if (previewCamera == null && vuforiaBehaviour != null)
            {
                previewCamera = vuforiaBehaviour.GetComponent<Camera>();
            }

            if (previewCamera == null)
            {
                return transform;
            }

            GameObject anchorObject = new GameObject("BarcodePreviewAnchor");
            barcodePreviewAnchor = anchorObject.transform;
            barcodePreviewAnchor.SetParent(previewCamera.transform, false);
            barcodePreviewAnchor.localPosition = barcodePreviewOffset;
            barcodePreviewAnchor.localRotation = Quaternion.Euler(barcodePreviewEulerAngles);
            return barcodePreviewAnchor;
        }

        void UpdateStabilizedAnchors()
        {
            if (!stabilizeImageTargets || contentAnchors.Count == 0)
            {
                return;
            }

            var observers = new List<ObserverBehaviour>(contentAnchors.Keys);
            for (int i = 0; i < observers.Count; i++)
            {
                ObserverBehaviour observer = observers[i];
                if (observer == null || !activeContent.ContainsKey(observer) || !ShouldUpdateAnchorPose(observer))
                {
                    continue;
                }

                SyncAnchorToObserver(observer, false);
            }
        }

        void UpdateTrackedBarcodeContent()
        {
            if (currentStatuses.Count == 0)
            {
                return;
            }

            var observers = new List<ObserverBehaviour>(currentStatuses.Keys);
            for (int i = 0; i < observers.Count; i++)
            {
                ObserverBehaviour observer = observers[i];
                if (!(observer is BarcodeBehaviour) ||
                    !currentStatuses.TryGetValue(observer, out Status status) ||
                    !IsTracking(status))
                {
                    continue;
                }

                UpdateResolvedContent(observer, status, forceOverlayRefresh: false);
            }
        }

        void ExpireImageTargetGracePeriods()
        {
            if (trackingGraceDeadlines.Count == 0)
            {
                return;
            }

            var observers = new List<ObserverBehaviour>(trackingGraceDeadlines.Keys);
            for (int i = 0; i < observers.Count; i++)
            {
                ObserverBehaviour observer = observers[i];
                if (observer == null)
                {
                    trackingGraceDeadlines.Remove(observer);
                    continue;
                }

                if (!trackingGraceDeadlines.TryGetValue(observer, out float deadline))
                {
                    continue;
                }

                if (Time.time < deadline)
                {
                    continue;
                }

                trackingGraceDeadlines.Remove(observer);
                HandleTrackingLost(observer);
            }
        }

        bool ShouldUpdateAnchorPose(ObserverBehaviour observer)
        {
            if (observer == null)
            {
                return false;
            }

            if (!currentStatuses.TryGetValue(observer, out Status status))
            {
                return false;
            }

            if (observer is BarcodeBehaviour)
            {
                if (!ShouldAnchorBarcodeToMarker(observer))
                {
                    return false;
                }

                if (status == Status.TRACKED)
                {
                    return true;
                }

                return status == Status.LIMITED && !freezePoseWhileLimited;
            }

            if (status == Status.TRACKED || status == Status.EXTENDED_TRACKED)
            {
                return true;
            }

            return status == Status.LIMITED && !freezePoseWhileLimited;
        }

        void SyncAnchorToObserver(ObserverBehaviour observer, bool instant)
        {
            bool canUseAnchor = observer != null &&
                (stabilizeImageTargets || ShouldAnchorBarcodeToMarker(observer));
            if (!canUseAnchor)
            {
                return;
            }

            Transform anchor = GetOrCreateImageTargetAnchor(observer);
            Vector3 targetPosition = observer.transform.position;
            Quaternion targetRotation = observer.transform.rotation;
            Vector3 targetScale = observer.transform.lossyScale;
            float positionDelta = Vector3.Distance(anchor.position, targetPosition);
            float rotationDelta = Quaternion.Angle(anchor.rotation, targetRotation);

            if (!instant &&
                minimumAnchorUpdateIntervalSeconds > 0f &&
                anchorLastPoseUpdateTimes.TryGetValue(observer, out float lastPoseUpdateTime) &&
                Time.time - lastPoseUpdateTime < minimumAnchorUpdateIntervalSeconds)
            {
                anchor.localScale = GetAnchorScale(observer, targetScale);
                return;
            }

            if (!instant &&
                positionDelta <= positionJitterDeadzoneMeters &&
                rotationDelta <= rotationJitterDeadzoneDegrees)
            {
                anchor.localScale = GetAnchorScale(observer, targetScale);
                anchorPositionVelocities[observer] = Vector3.zero;
                return;
            }

            if (instant ||
                positionSmoothTime <= 0f ||
                positionDelta > positionSnapDistanceMeters ||
                rotationDelta > rotationSnapAngleDegrees)
            {
                anchor.position = targetPosition;
                anchor.rotation = targetRotation;
                anchor.localScale = GetAnchorScale(observer, targetScale);
                anchorPositionVelocities[observer] = Vector3.zero;
                anchorLastPoseUpdateTimes[observer] = Time.time;
                return;
            }

            Vector3 velocity = anchorPositionVelocities.TryGetValue(observer, out Vector3 currentVelocity)
                ? currentVelocity
                : Vector3.zero;
            anchor.position = Vector3.SmoothDamp(anchor.position, targetPosition, ref velocity, positionSmoothTime, Mathf.Infinity, Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
            anchor.rotation = Quaternion.Slerp(anchor.rotation, targetRotation, rotationT);
            anchor.localScale = GetAnchorScale(observer, targetScale);
            anchorPositionVelocities[observer] = velocity;
            anchorLastPoseUpdateTimes[observer] = Time.time;
        }

        void HandlePanDelta(Vector2 delta)
        {
            if (TryGetInteractiveTarget(out ObserverBehaviour observer, out GameObject activeObject, out Transform interactionRoot, out bool isPreviewTarget))
            {
                DisableAutoSpin(activeObject);
                ApplyTouchPan(observer, activeObject.transform, delta, isPreviewTarget);
            }
        }

        void HandleScaleDelta(float delta)
        {
            if (TryGetInteractiveTarget(out ObserverBehaviour observer, out GameObject activeObject, out Transform interactionRoot, out bool isPreviewTarget))
            {
                DisableAutoSpin(activeObject);
                ApplyTouchScale(observer, interactionRoot, delta, isPreviewTarget);
            }
        }

        void HandleRotateDelta(Vector2 delta)
        {
            if (TryGetInteractiveTarget(out ObserverBehaviour observer, out GameObject activeObject, out Transform interactionRoot, out bool isPreviewTarget))
            {
                DisableAutoSpin(activeObject);
                ApplyTouchRotation(observer, interactionRoot, delta, isPreviewTarget);
            }
        }

        void HandleTap(Vector2 screenPosition)
        {
            if (enableTapToRefocus)
            {
                RequestAutoFocusIfNeeded();
            }

            if (learningFeedback != null &&
                TryGetInteractiveTarget(out _, out _, out Transform tappedRoot, out _))
            {
                learningFeedback.OnARObjectTapped(tappedRoot);
            }
        }

        bool TryGetInteractiveTarget(out ObserverBehaviour observer, out GameObject activeObject, out Transform interactionRoot, out bool isPreviewTarget)
        {
            if (previewPlacementObject != null &&
                previewPlacementObject.activeInHierarchy &&
                previewInteractionRoot != null)
            {
                observer = null;
                activeObject = previewPlacementObject;
                interactionRoot = previewInteractionRoot;
                isPreviewTarget = true;
                return true;
            }

            foreach (KeyValuePair<ObserverBehaviour, MaterialContentData> pair in activeContent)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                if (spawnedPrefabs.TryGetValue(pair.Key, out activeObject) &&
                    activeObject != null &&
                    activeObject.activeInHierarchy &&
                    spawnedInteractionRoots.TryGetValue(pair.Key, out interactionRoot) &&
                    interactionRoot != null)
                {
                    observer = pair.Key;
                    isPreviewTarget = false;
                    return true;
                }
            }

            observer = null;
            activeObject = null;
            interactionRoot = null;
            isPreviewTarget = false;
            return false;
        }

        static void DisableAutoSpin(GameObject activeObject)
        {
            if (activeObject == null)
            {
                return;
            }

            DemoSpinObject spinObject = activeObject.GetComponent<DemoSpinObject>();
            if (spinObject == null)
            {
                spinObject = activeObject.GetComponentInChildren<DemoSpinObject>(true);
            }

            if (spinObject != null)
            {
                spinObject.enabled = false;
            }
        }

        void ResetTouchInteractionState()
        {
            activeInteractionObserver = null;
            activeInteractionObject = null;
            activeInteractionRoot = null;
            activeInteractionIsPreview = false;
        }

        void NormalizeSpawnedContentPlacement(Transform placementRoot, Transform contentTransform)
        {
            if (!normalizeSpawnedContentPivot || placementRoot == null || contentTransform == null)
            {
                return;
            }

            if (!TryGetRendererBoundsInLocalSpace(placementRoot, contentTransform.gameObject, out Bounds localBounds))
            {
                return;
            }

            Vector3 alignmentOffset = new Vector3(
                -localBounds.center.x,
                -localBounds.min.y,
                -localBounds.center.z);

            contentTransform.localPosition += alignmentOffset;
        }

        void NormalizePreviewContentPlacement(Transform pivotRoot, Transform contentTransform)
        {
            if (pivotRoot == null || contentTransform == null)
            {
                return;
            }

            if (!TryGetRendererBoundsInLocalSpace(pivotRoot, contentTransform.gameObject, out Bounds localBounds))
            {
                return;
            }

            Vector3 centeredOffset = -localBounds.center;
            contentTransform.localPosition += centeredOffset;

            Vector3 pivotOffset = new Vector3(0f, Mathf.Max(0f, localBounds.center.y - localBounds.min.y) * 0.28f, 0f);
            pivotRoot.localPosition += pivotOffset;
        }

        static bool TryGetRendererBoundsInLocalSpace(Transform relativeTo, GameObject rootObject, out Bounds localBounds)
        {
            Renderer[] renderers = rootObject != null
                ? rootObject.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();

            bool hasBounds = false;
            localBounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                            Vector3 localCorner = relativeTo.InverseTransformPoint(worldCorner);
                            if (!hasBounds)
                            {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        static void SnapAnchorToObserver(ObserverBehaviour observer, Transform anchor)
        {
            if (observer == null || anchor == null)
            {
                return;
            }

            anchor.position = observer.transform.position;
            anchor.rotation = observer.transform.rotation;
            anchor.localScale = GetAnchorScale(observer, observer.transform.lossyScale);
        }

        static Vector3 GetAnchorScale(ObserverBehaviour observer, Vector3 observerScale)
        {
            if (observer is BarcodeBehaviour)
            {
                return Vector3.one;
            }

            return observerScale;
        }

        void UnregisterAllTargets()
        {
            var observers = new List<ObserverBehaviour>(registeredImageTargets.Count + activeContent.Count + spawnedPrefabs.Count);
            foreach (ObserverBehaviour behaviour in registeredImageTargets.Keys)
            {
                if (behaviour != null && !observers.Contains(behaviour))
                {
                    observers.Add(behaviour);
                }
            }

            foreach (ObserverBehaviour behaviour in activeContent.Keys)
            {
                if (behaviour != null && !observers.Contains(behaviour))
                {
                    observers.Add(behaviour);
                }
            }

            foreach (ObserverBehaviour behaviour in spawnedPrefabs.Keys)
            {
                if (behaviour != null && !observers.Contains(behaviour))
                {
                    observers.Add(behaviour);
                }
            }

            for (int i = 0; i < observers.Count; i++)
            {
                OnObserverDestroyed(observers[i]);
            }
        }

        void RefreshOverlayState()
        {
            foreach (KeyValuePair<ObserverBehaviour, MaterialContentData> pair in activeContent)
            {
                if (pair.Value != null)
                {
                    contentController?.ShowContent(pair.Value);
                    return;
                }
            }

            if (previewPlacementObject != null && previewPlacementObject.activeInHierarchy && previewContent != null)
            {
                contentController?.PreviewContent(previewContent);
                contentController?.SetPreviewModeActive(true);
                return;
            }

            contentController?.HideContent();
        }

        void InitializeEditorPreviewRuntime()
        {
            contentController?.SetEditorCameraFeedActive(false);
            contentController?.SetEditorCameraFeedTexture(null, false, false, 0);
            editorWebcamReady = false;
            editorManualPreviewRequested = false;

            if (!enableEditorWebcamBackground)
            {
                contentController?.SetStatus("Preview editor aktif di Linux tanpa webcam. Panah kiri/kanan ganti materi.");
                return;
            }

            if (editorWebcamStartupRoutine != null)
            {
                StopCoroutine(editorWebcamStartupRoutine);
            }

            editorWebcamStartupRoutine = StartCoroutine(BootstrapEditorWebcam());
        }

        void UpdateEditorPreviewRuntime()
        {
            if (!ShouldUsePreviewOnlyMode())
            {
                return;
            }

            UpdateEditorWebcamBackground();
        }

        void ShutdownEditorPreviewRuntime()
        {
            if (editorWebcamStartupRoutine != null)
            {
                StopCoroutine(editorWebcamStartupRoutine);
                editorWebcamStartupRoutine = null;
            }

            if (editorLinuxDirectCameraFeed != null)
            {
                editorLinuxDirectCameraFeed.Dispose();
                editorLinuxDirectCameraFeed = null;
            }

            if (editorLinuxDirectCameraTexture != null)
            {
                Destroy(editorLinuxDirectCameraTexture);
                editorLinuxDirectCameraTexture = null;
            }

            editorWebcamReady = false;
            editorManualPreviewRequested = false;
            contentController?.SetEditorCameraFeedActive(false);
            contentController?.SetEditorCameraFeedTexture(null, false, false, 0);
        }

        IEnumerator BootstrapEditorWebcam()
        {
            editorWebcamReady = false;
            contentController?.SetStatus("Menyalakan webcam editor lewat ffmpeg pada " + editorLinuxDirectCameraDevicePath + "...");

            if (editorLinuxDirectCameraFeed != null)
            {
                editorLinuxDirectCameraFeed.Dispose();
                editorLinuxDirectCameraFeed = null;
            }

            editorLinuxDirectCameraFeed = new LinuxEditorDirectCameraFeed(
                editorLinuxDirectCameraFfmpegPath,
                editorLinuxDirectCameraDevicePath,
                editorLinuxDirectCameraInputFormat,
                editorLinuxDirectCameraWidth,
                editorLinuxDirectCameraHeight,
                editorLinuxDirectCameraFrameRate);

            if (!editorLinuxDirectCameraFeed.Start(out string error))
            {
                contentController?.SetStatus("Gagal start webcam editor: " + error);
                editorWebcamStartupRoutine = null;
                yield break;
            }

            float timeoutAt = Time.realtimeSinceStartup + Mathf.Max(1f, editorWebcamStartupTimeoutSeconds);
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                if (editorLinuxDirectCameraFeed == null)
                {
                    break;
                }

                Texture2D feedTexture = editorLinuxDirectCameraTexture;
                if (editorLinuxDirectCameraFeed.TryUpdateTexture(ref feedTexture))
                {
                    editorLinuxDirectCameraTexture = feedTexture;
                    editorWebcamReady = true;
                    contentController?.SetEditorCameraFeedActive(true);
                    contentController?.SetEditorCameraFeedTexture(editorLinuxDirectCameraTexture, true, false, 0);
                    contentController?.SetStatus("Webcam editor aktif dari " + editorLinuxDirectCameraDevicePath + ". Scan marker Linux editor belum didukung; panah kiri/kanan untuk preview manual.");
                    editorWebcamStartupRoutine = null;
                    yield break;
                }

                yield return null;
            }

            string lastError = editorLinuxDirectCameraFeed != null ? editorLinuxDirectCameraFeed.LastError : null;
            if (editorLinuxDirectCameraFeed != null)
            {
                editorLinuxDirectCameraFeed.Dispose();
                editorLinuxDirectCameraFeed = null;
            }

            editorWebcamReady = false;
            contentController?.SetEditorCameraFeedActive(false);
            contentController?.SetEditorCameraFeedTexture(null, false, false, 0);
            contentController?.SetStatus("Feed " + editorLinuxDirectCameraDevicePath + " gagal masuk ke Unity. " +
                (string.IsNullOrWhiteSpace(lastError) ? "ffmpeg tidak mengirim frame." : lastError));
            editorWebcamStartupRoutine = null;
        }

        void UpdateEditorWebcamBackground()
        {
            if (editorLinuxDirectCameraFeed == null || !editorWebcamReady)
            {
                return;
            }

            Texture2D feedTexture = editorLinuxDirectCameraTexture;
            if (editorLinuxDirectCameraFeed.TryUpdateTexture(ref feedTexture))
            {
                editorLinuxDirectCameraTexture = feedTexture;
            }

            contentController?.SetEditorCameraFeedActive(editorLinuxDirectCameraTexture != null);
            contentController?.SetEditorCameraFeedTexture(editorLinuxDirectCameraTexture, editorLinuxDirectCameraTexture != null, false, 0);
        }

        void UpdatePreviewMode()
        {
            if (!enablePreviewModeWithoutTracking || !EnsureLibrary())
            {
                ExitPreviewMode(false);
                return;
            }

            if (ShouldUsePreviewOnlyMode() && enableEditorWebcamBackground && !editorWebcamReady)
            {
                ExitPreviewMode(false);
                return;
            }

            if (ShouldUsePreviewOnlyMode() && enableEditorWebcamBackground && !editorManualPreviewRequested)
            {
                ExitPreviewMode(false);
                return;
            }

            if (!ShouldAllowAutomaticPreview())
            {
                previewModeReadyAt = -1f;
                ExitPreviewMode(false);
                return;
            }

            if (HasAnyTrackedContentOrGrace())
            {
                previewModeReadyAt = -1f;
                ExitPreviewMode(false);
                return;
            }

            if (previewPlacementObject != null)
            {
                return;
            }

            if (previewModeReadyAt < 0f)
            {
                previewModeReadyAt = Time.unscaledTime + previewModeIdleDelaySeconds;
                return;
            }

            if (Time.unscaledTime < previewModeReadyAt)
            {
                return;
            }

            if (!TryGetPreviewContent(out MaterialContentData content))
            {
                return;
            }

            EnterPreviewMode(content);
        }

        bool ShouldAllowAutomaticPreview()
        {
            if (disableScannerForExperiment)
            {
                return true;
            }

            if (!hasResolvedTrackableContentThisSession)
            {
                return false;
            }

            return !ShouldUsePreviewOnlyMode() || editorManualPreviewRequested;
        }

        bool HasAnyTrackedContentOrGrace()
        {
            if (activeContent.Count > 0 || trackingGraceDeadlines.Count > 0)
            {
                return true;
            }

            foreach (KeyValuePair<ObserverBehaviour, Status> pair in currentStatuses)
            {
                if (pair.Key != null && IsTracking(pair.Value))
                {
                    return true;
                }
            }

            return false;
        }

        bool TryGetPreviewContent(out MaterialContentData content)
        {
            content = null;

            if (!hasResolvedTrackableContentThisSession)
            {
                return false;
            }

            if (lastResolvedPreviewContent != null && IsContentEligibleForPreview(lastResolvedPreviewContent))
            {
                content = lastResolvedPreviewContent;
                return true;
            }

            if (previewModePreferLastViewedContent &&
                !string.IsNullOrWhiteSpace(ARtiGraf.Core.AppSession.LastViewedContentId) &&
                library.TryGetById(ARtiGraf.Core.AppSession.LastViewedContentId, out MaterialContentData lastViewedContent) &&
                IsContentEligibleForPreview(lastViewedContent))
            {
                content = lastViewedContent;
                return true;
            }

            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData candidate = library.Items[i];
                if (IsContentEligibleForPreview(candidate))
                {
                    content = candidate;
                    return true;
                }
            }

            return false;
        }

        bool IsContentEligibleForPreview(MaterialContentData content)
        {
            if (!IsRuntimeContentAllowed(content))
            {
                return false;
            }

            if (!ARtiGraf.Core.AppSession.SelectedCategory.HasValue)
            {
                return true;
            }

            return content.Category == ARtiGraf.Core.AppSession.SelectedCategory.Value;
        }

        static string GetContentDisplayName(MaterialContentData content)
        {
            if (content == null)
            {
                return "materi";
            }

            return string.IsNullOrWhiteSpace(content.Title)
                ? content.Id
                : content.Title;
        }

        void EnterPreviewMode(MaterialContentData content)
        {
            if (content == null || content.Prefab == null)
            {
                return;
            }

            if (ShouldUsePreviewOnlyMode())
            {
                editorManualPreviewRequested = true;
            }

            if (previewPlacementObject != null && previewContent == content)
            {
                return;
            }

            ExitPreviewMode(false);

            Transform parent = GetBarcodePreviewAnchor();
            if (parent == null)
            {
                return;
            }

            previewPlacementObject = new GameObject(content.Id + "_PreviewPlacementRoot");
            Transform placementTransform = previewPlacementObject.transform;
            placementTransform.SetParent(parent, false);
            placementTransform.localPosition = Vector3.zero;
            placementTransform.localRotation = Quaternion.identity;
            placementTransform.localScale = Vector3.one;

            GameObject interactionRootObject = new GameObject(content.Id + "_PreviewInteractionRoot");
            previewInteractionRoot = interactionRootObject.transform;
            previewInteractionRoot.SetParent(placementTransform, false);
            previewInteractionRoot.localPosition = Vector3.zero;
            previewInteractionRoot.localRotation = Quaternion.identity;
            previewInteractionRoot.localScale = Vector3.one;

            GameObject spawnedObject = Instantiate(content.Prefab, previewInteractionRoot);
            NormalizePreviewContentPlacement(previewInteractionRoot, spawnedObject.transform);
            previewContent = content;
            previewBaseScale = previewInteractionRoot.localScale;
            previewBaseLocalPosition = placementTransform.localPosition;
            previewUserRotationAngles = Vector2.zero;

            contentController?.SetPreviewModeActive(true);
            contentController?.PreviewContent(content);
            contentController?.SetStatus(BuildPreviewStatusMessage(content));
        }

        void ExitPreviewMode(bool refreshOverlay)
        {
            if (previewPlacementObject != null)
            {
                Destroy(previewPlacementObject);
            }

            previewPlacementObject = null;
            previewInteractionRoot = null;
            previewContent = null;
            previewBaseScale = Vector3.one;
            previewBaseLocalPosition = Vector3.zero;
            previewUserRotationAngles = Vector2.zero;
            contentController?.SetPreviewModeActive(false);

            if (refreshOverlay)
            {
                RefreshOverlayState();
            }
        }

        void CyclePreviewContent(int direction)
        {
            if (!EnsureLibrary() || library.Items.Count == 0)
            {
                return;
            }

            var eligibleContents = new List<MaterialContentData>();
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData candidate = library.Items[i];
                if (IsContentEligibleForPreview(candidate))
                {
                    eligibleContents.Add(candidate);
                }
            }

            if (eligibleContents.Count == 0)
            {
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < eligibleContents.Count; i++)
            {
                if (eligibleContents[i] == previewContent)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                int fallbackIndex = direction >= 0 ? 0 : eligibleContents.Count - 1;
                EnterPreviewMode(eligibleContents[fallbackIndex]);
                return;
            }

            int nextIndex = (currentIndex + direction + eligibleContents.Count) % eligibleContents.Count;
            EnterPreviewMode(eligibleContents[nextIndex]);
        }

        void ResetPreviewTransform()
        {
            if (previewPlacementObject == null || previewInteractionRoot == null)
            {
                return;
            }

            previewPlacementObject.transform.localPosition = previewBaseLocalPosition;
            previewInteractionRoot.localScale = previewBaseScale;
            previewUserRotationAngles = Vector2.zero;
            previewInteractionRoot.localRotation = Quaternion.identity;
            contentController?.SetStatus(BuildPreviewStatusMessage(previewContent));
        }

        void HideEditorManualPreview()
        {
            if (!ShouldUsePreviewOnlyMode())
            {
                return;
            }

            editorManualPreviewRequested = false;
            ExitPreviewMode(false);
            contentController?.SetStatus(editorWebcamReady
                ? "Webcam editor aktif. Preview disembunyikan; panah kiri/kanan untuk munculkan lagi."
                : "Preview editor aktif tanpa webcam. Panah kiri/kanan untuk munculkan preview manual.");
        }

        bool ShouldSuppressBarcodeResolution(
            ObserverBehaviour behaviour,
            MaterialContentData resolvedContent,
            string barcodePayload,
            Status currentStatus,
            bool contentChanged,
            bool barcodePayloadChanged)
        {
            if (!(behaviour is BarcodeBehaviour))
            {
                return false;
            }

            if (ShouldPrioritizeBarcodeResolution(resolvedContent))
            {
                return false;
            }

            if (HasPendingBarcodeCooldown(behaviour, contentChanged, barcodePayloadChanged))
            {
                contentController?.SetStatus("Marker baru terbaca. Tahan sebentar agar perpindahan objek tetap stabil.");
                return true;
            }

            if (requireTrackedStatusForBarcodeSwitch &&
                currentStatus != Status.TRACKED &&
                activeContent.ContainsKey(behaviour) &&
                (contentChanged || barcodePayloadChanged))
            {
                if (activeContent.ContainsKey(behaviour))
                {
                    contentController?.SetStatus("Marker sedang dibaca. Tunggu status TRACKED penuh sebelum objek diganti.");
                    return true;
                }

                RememberSuppressedBarcodePayload(
                    behaviour,
                    barcodePayload,
                    "Barcode dibaca saat status masih LIMITED. Menunggu TRACKED penuh sebelum ganti object.");
                return true;
            }

            if (preferTrackedImageTargetsOverBarcodeResults &&
                TryGetDominantTrackedImageContent(behaviour, out ObserverBehaviour imageObserver, out MaterialContentData imageContent))
            {
                if (imageContent == resolvedContent)
                {
                    RememberSuppressedBarcodePayload(
                        behaviour,
                        barcodePayload,
                        "Image target aktif sudah cocok dengan barcode. Image target diprioritaskan.");
                    return true;
                }

                string imageId = imageContent != null ? imageContent.Id : imageObserver.name;
                RememberSuppressedBarcodePayload(
                    behaviour,
                    barcodePayload,
                    "Barcode \"" + barcodePayload + "\" diabaikan karena marker aktif adalah \"" + imageId + "\".");
                return true;
            }

            return false;
        }

        bool HasPendingBarcodeCooldown(
            ObserverBehaviour behaviour,
            bool contentChanged,
            bool barcodePayloadChanged)
        {
            if (!(contentChanged || barcodePayloadChanged) ||
                barcodeSwitchCooldownSeconds <= 0f ||
                !activeContent.ContainsKey(behaviour))
            {
                return false;
            }

            return barcodeSwitchCooldownDeadlines.TryGetValue(behaviour, out float deadline) &&
                   Time.unscaledTime < deadline;
        }

        bool TryGetDominantTrackedImageContent(
            ObserverBehaviour excludedObserver,
            out ObserverBehaviour imageObserver,
            out MaterialContentData imageContent)
        {
            foreach (KeyValuePair<ObserverBehaviour, MaterialContentData> pair in activeContent)
            {
                ObserverBehaviour observer = pair.Key;
                if (observer == null || observer == excludedObserver || observer is BarcodeBehaviour)
                {
                    continue;
                }

                if (pair.Value == null)
                {
                    continue;
                }

                if (currentStatuses.TryGetValue(observer, out Status status) && IsTracking(status))
                {
                    imageObserver = observer;
                    imageContent = pair.Value;
                    return true;
                }

                if (trackingGraceDeadlines.ContainsKey(observer))
                {
                    imageObserver = observer;
                    imageContent = pair.Value;
                    return true;
                }
            }

            imageObserver = null;
            imageContent = null;
            return false;
        }

        void RememberSuppressedBarcodePayload(ObserverBehaviour behaviour, string barcodePayload, string debugReason)
        {
            if (behaviour == null)
            {
                return;
            }

            bool payloadChanged = !suppressedBarcodePayloads.TryGetValue(behaviour, out string previousPayload) ||
                !string.Equals(previousPayload, barcodePayload, StringComparison.Ordinal);

            suppressedBarcodePayloads[behaviour] = barcodePayload ?? string.Empty;
            DeactivateSpawnedInstance(behaviour);
            activeContent.Remove(behaviour);

            if (payloadChanged && !string.IsNullOrWhiteSpace(debugReason))
            {
                Debug.Log("[ARtiGraf] " + debugReason);
            }

            RefreshOverlayState();
        }

        bool TryResolveContent(ObserverBehaviour behaviour, out MaterialContentData content, out string barcodePayload)
        {
            barcodePayload = string.Empty;
            if (registeredImageTargets.TryGetValue(behaviour, out content))
            {
                return true;
            }

            if (behaviour is BarcodeBehaviour barcodeBehaviour && barcodeBehaviour.InstanceData != null)
            {
                barcodePayload = barcodeBehaviour.InstanceData.Text;
                return contentController != null &&
                       contentController.TryGetContentForScanPayload(barcodePayload, out content);
            }

            content = null;
            return false;
        }

        bool ShouldPrioritizeBarcodeResolution(MaterialContentData content)
        {
            return preferBarcodeTrackingForDemoMarkers &&
                   content != null &&
                   content.IsDemoContent;
        }

        bool ShouldPreferBarcodeTracking(MaterialContentData content)
        {
            return ShouldPrioritizeBarcodeResolution(content) &&
                   !IsBackedByImportedVuforiaDatabase(content);
        }

        bool ShouldSkipRuntimeTextureTarget(MaterialContentData content)
        {
            return useImportedVuforiaDeviceDatabase &&
                   strictImportedVuforiaDeviceDatabaseOnly &&
                   !IsBackedByImportedVuforiaDatabase(content);
        }

        bool TryCreateImageTargetFromImportedDatabase(
            ObserverFactory observerFactory,
            MaterialContentData content,
            out ImageTargetBehaviour imageTarget)
        {
            imageTarget = null;
            if (observerFactory == null || !IsBackedByImportedVuforiaDatabase(content))
            {
                return false;
            }

            string databasePath = GetImportedVuforiaDatabasePath();
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                return false;
            }

            try
            {
                imageTarget = observerFactory.CreateImageTarget(databasePath, content.ReferenceImageName);
                if (imageTarget != null)
                {
                    Debug.Log("[ARtiGraf] Device database target aktif: " + content.ReferenceImageName +
                              " dari " + databasePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[ARtiGraf] Gagal membuat image target dari device database untuk " +
                    content.ReferenceImageName + ". Fallback ke texture/barcode. " + ex.Message);
            }

            return false;
        }

        bool IsBackedByImportedVuforiaDatabase(MaterialContentData content)
        {
            if (!useImportedVuforiaDeviceDatabase ||
                content == null ||
                string.IsNullOrWhiteSpace(content.ReferenceImageName))
            {
                return false;
            }

            EnsureImportedVuforiaDatabaseMetadata();
            return importedVuforiaDatabaseTargetNames.Contains(content.ReferenceImageName);
        }

        void EnsureImportedVuforiaDatabaseMetadata()
        {
            if (importedVuforiaDatabaseScanned)
            {
                return;
            }

            importedVuforiaDatabaseScanned = true;
            importedVuforiaDatabaseTargetNames.Clear();
            cachedImportedVuforiaDatabasePath = NormalizeImportedVuforiaDatabasePath(importedVuforiaDeviceDatabasePath);
            AddConfiguredImportedVuforiaTargetNames();
            if (string.IsNullOrWhiteSpace(cachedImportedVuforiaDatabasePath))
            {
                return;
            }

            string databaseXmlAbsolutePath = ResolveImportedVuforiaDatabaseAbsolutePath(cachedImportedVuforiaDatabasePath);
            if (!File.Exists(databaseXmlAbsolutePath))
            {
                Debug.LogWarning(
                    "[ARtiGraf] Metadata device database tidak bisa dibaca dari path file: " +
                    databaseXmlAbsolutePath + ". Menggunakan target konfigurasi: " +
                    string.Join(", ", new List<string>(importedVuforiaDatabaseTargetNames).ToArray()));
                return;
            }

            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(databaseXmlAbsolutePath);
                XmlNodeList imageTargetNodes = xmlDocument.SelectNodes("//ImageTarget");
                if (imageTargetNodes == null)
                {
                    return;
                }

                foreach (XmlNode imageTargetNode in imageTargetNodes)
                {
                    string targetName = imageTargetNode?.Attributes?["name"]?.Value;
                    if (!string.IsNullOrWhiteSpace(targetName))
                    {
                        importedVuforiaDatabaseTargetNames.Add(targetName);
                    }
                }

                Debug.Log("[ARtiGraf] Device database siap: " + cachedImportedVuforiaDatabasePath +
                          " (" + importedVuforiaDatabaseTargetNames.Count + " target)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[ARtiGraf] Gagal membaca metadata device database " +
                    databaseXmlAbsolutePath + ". " + ex.Message);
            }
        }

        void AddConfiguredImportedVuforiaTargetNames()
        {
            if (string.IsNullOrWhiteSpace(importedVuforiaDeviceDatabaseTargets))
            {
                return;
            }

            string[] configuredNames = importedVuforiaDeviceDatabaseTargets.Split(
                new[] { ',', ';', '|', '\n', '\r', '\t', ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < configuredNames.Length; i++)
            {
                string targetName = configuredNames[i].Trim();
                if (!string.IsNullOrWhiteSpace(targetName))
                {
                    importedVuforiaDatabaseTargetNames.Add(targetName);
                }
            }
        }

        string GetImportedVuforiaDatabasePath()
        {
            EnsureImportedVuforiaDatabaseMetadata();
            return cachedImportedVuforiaDatabasePath;
        }

        static string NormalizeImportedVuforiaDatabasePath(string configuredPath)
        {
            string normalizedPath = string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultVuforiaDeviceDatabasePath
                : configuredPath.Trim();
            normalizedPath = normalizedPath.Replace('\\', '/');

            const string streamingAssetsPrefix = "Assets/StreamingAssets/";
            if (normalizedPath.StartsWith(streamingAssetsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(streamingAssetsPrefix.Length);
            }

            return normalizedPath;
        }

        static string ResolveImportedVuforiaDatabaseAbsolutePath(string databasePath)
        {
            if (Path.IsPathRooted(databasePath))
            {
                return databasePath;
            }

            return Path.Combine(Application.streamingAssetsPath, databasePath);
        }

        bool ShouldAnchorBarcodeToMarker(ObserverBehaviour observer)
        {
            return anchorBarcodeContentToMarker && observer is BarcodeBehaviour;
        }

        void ConfigureRuntimeCameraFocus()
        {
            if (!configureRuntimeCameraFocus || !Application.isMobilePlatform)
            {
                return;
            }

            CameraDevice cameraDevice = vuforiaBehaviour != null ? vuforiaBehaviour.CameraDevice : null;
            if (cameraDevice == null || !cameraDevice.IsActive)
            {
                return;
            }

            continuousAutoFocusSupported = cameraDevice.IsFocusModeSupported(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
            if (continuousAutoFocusSupported && cameraDevice.SetFocusMode(FocusMode.FOCUS_MODE_CONTINUOUSAUTO))
            {
                Debug.Log("[ARtiGraf] Continuous autofocus aktif.");
                return;
            }

            if (cameraDevice.IsFocusModeSupported(FocusMode.FOCUS_MODE_TRIGGERAUTO) &&
                cameraDevice.SetFocusMode(FocusMode.FOCUS_MODE_TRIGGERAUTO))
            {
                Debug.Log("[ARtiGraf] Trigger autofocus aktif sebagai fallback.");
            }
        }

        void RequestAutoFocusIfNeeded()
        {
            if (!configureRuntimeCameraFocus || !Application.isMobilePlatform || Time.unscaledTime < nextAutoFocusRequestTime)
            {
                return;
            }

            CameraDevice cameraDevice = vuforiaBehaviour != null ? vuforiaBehaviour.CameraDevice : null;
            if (cameraDevice == null || !cameraDevice.IsActive)
            {
                return;
            }

            continuousAutoFocusSupported = continuousAutoFocusSupported ||
                                           cameraDevice.IsFocusModeSupported(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
            bool focusRequested = false;

            if (cameraDevice.IsFocusModeSupported(FocusMode.FOCUS_MODE_TRIGGERAUTO))
            {
                focusRequested = cameraDevice.SetFocusMode(FocusMode.FOCUS_MODE_TRIGGERAUTO);
                if (focusRequested && continuousAutoFocusSupported)
                {
                    restoreContinuousAutoFocusAt = Time.unscaledTime + autofocusRestoreDelaySeconds;
                }
            }
            else if (continuousAutoFocusSupported)
            {
                focusRequested = cameraDevice.SetFocusMode(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
            }

            if (focusRequested)
            {
                nextAutoFocusRequestTime = Time.unscaledTime + autofocusCooldownSeconds;
            }
        }

        void UpdatePendingCameraFocusRestore()
        {
            if (restoreContinuousAutoFocusAt < 0f || Time.unscaledTime < restoreContinuousAutoFocusAt)
            {
                return;
            }

            restoreContinuousAutoFocusAt = -1f;
            if (!configureRuntimeCameraFocus || !Application.isMobilePlatform)
            {
                return;
            }

            CameraDevice cameraDevice = vuforiaBehaviour != null ? vuforiaBehaviour.CameraDevice : null;
            if (cameraDevice == null || !cameraDevice.IsActive)
            {
                return;
            }

            continuousAutoFocusSupported = continuousAutoFocusSupported ||
                                           cameraDevice.IsFocusModeSupported(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
            if (continuousAutoFocusSupported)
            {
                cameraDevice.SetFocusMode(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
            }
        }

#if ENABLE_INPUT_SYSTEM
#endif

        void ApplyTouchRotation(ObserverBehaviour observer, Transform targetTransform, Vector2 deltaPosition, bool isPreviewTarget)
        {
            if (targetTransform == null)
            {
                return;
            }

            Vector2 rotationAngles;
            if (isPreviewTarget)
            {
                rotationAngles = previewUserRotationAngles;
            }
            else
            {
                if (observer == null)
                {
                    return;
                }

                rotationAngles = spawnedUserRotationAngles.TryGetValue(observer, out Vector2 storedAngles)
                    ? storedAngles
                    : Vector2.zero;
            }

            if (allowTouchPitchRotation)
            {
                rotationAngles.x += deltaPosition.y * touchPitchDegreesPerPixel;
                if (allowFullTouchRotation360)
                {
                    rotationAngles.x = NormalizeInteractionAngle(rotationAngles.x);
                }
                else
                {
                    rotationAngles.x = Mathf.Clamp(
                        rotationAngles.x,
                        -interactionPitchClampDegrees,
                        interactionPitchClampDegrees);
                }
            }
            else
            {
                rotationAngles.x = 0f;
            }

            rotationAngles.y -= deltaPosition.x * touchYawDegreesPerPixel;
            if (allowFullTouchRotation360)
            {
                rotationAngles.y = NormalizeInteractionAngle(rotationAngles.y);
            }

            if (isPreviewTarget)
            {
                previewUserRotationAngles = rotationAngles;
            }
            else
            {
                spawnedUserRotationAngles[observer] = rotationAngles;
            }

            targetTransform.localRotation = Quaternion.Euler(rotationAngles.x, rotationAngles.y, 0f);
        }

        static float NormalizeInteractionAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        void ApplyTouchPan(ObserverBehaviour observer, Transform targetTransform, Vector2 screenDelta, bool isPreviewTarget)
        {
            if (targetTransform == null)
            {
                return;
            }

            Vector3 baseLocalPosition;
            Vector3 nextLocalPosition = targetTransform.localPosition;
            if (isPreviewTarget)
            {
                baseLocalPosition = previewBaseLocalPosition;
                nextLocalPosition.x += screenDelta.x * touchPanMetersPerPixel;
                nextLocalPosition.y += screenDelta.y * touchPanMetersPerPixel;
                nextLocalPosition.x = Mathf.Clamp(nextLocalPosition.x, baseLocalPosition.x - previewPanRange.x, baseLocalPosition.x + previewPanRange.x);
                nextLocalPosition.y = Mathf.Clamp(nextLocalPosition.y, baseLocalPosition.y - previewPanRange.y, baseLocalPosition.y + previewPanRange.y);
            }
            else
            {
                if (observer == null)
                {
                    return;
                }

                baseLocalPosition = spawnedBaseLocalPositions.TryGetValue(observer, out Vector3 storedBaseLocalPosition)
                    ? storedBaseLocalPosition
                    : targetTransform.localPosition;
                nextLocalPosition.x += screenDelta.x * touchPanMetersPerPixel;
                nextLocalPosition.z -= screenDelta.y * touchPanMetersPerPixel;
                nextLocalPosition.x = Mathf.Clamp(nextLocalPosition.x, baseLocalPosition.x - interactionPanRange.x, baseLocalPosition.x + interactionPanRange.x);
                nextLocalPosition.z = Mathf.Clamp(nextLocalPosition.z, baseLocalPosition.z - interactionPanRange.y, baseLocalPosition.z + interactionPanRange.y);
            }

            targetTransform.localPosition = nextLocalPosition;
        }

        void ApplyTouchScale(ObserverBehaviour observer, Transform targetTransform, float pinchDelta, bool isPreviewTarget)
        {
            if (targetTransform == null)
            {
                return;
            }

            Vector3 baseScale;
            if (isPreviewTarget)
            {
                baseScale = previewBaseScale;
            }
            else
            {
                if (observer == null)
                {
                    return;
                }

                baseScale = spawnedBaseScales.TryGetValue(observer, out Vector3 storedBaseScale)
                    ? storedBaseScale
                    : targetTransform.localScale;
            }

            float baseX = Mathf.Max(0.0001f, baseScale.x);
            float currentRatio = targetTransform.localScale.x / baseX;
            float nextRatio = currentRatio * (1f + (pinchDelta * pinchScaleSensitivity));
            nextRatio = Mathf.Clamp(nextRatio, interactionScaleRange.x, interactionScaleRange.y);
            targetTransform.localScale = baseScale * nextRatio;
        }



        static string BuildExampleHint()
        {
            if (!ARtiGraf.Core.AppSession.SelectedCategory.HasValue)
            {
                return "kartu marker buah/hewan seperti apel, jeruk, kucing, dan kupu-kupu";
            }

            return ARtiGraf.Core.AppSession.SelectedCategory.Value == LearningCategory.Typography
                ? "buah seperti apel, jeruk, pisang, atau anggur"
                : "hewan seperti kucing, kupu-kupu, burung, atau ikan";
        }

        string BuildScannerReadyStatus(int imageTargetCount, bool barcodeReady, List<string> createdTargetLabels)
        {
            string targetSummary = imageTargetCount > 0
                ? JoinLimited(createdTargetLabels, 92)
                : "belum ada image target";
            string barcodeSummary = barcodeReady ? "QR aktif" : "marker-only";

            if (useImportedVuforiaDeviceDatabase && importedVuforiaDatabaseTargetNames.Count > 0)
            {
                return "Scanner siap. DB target: " +
                       JoinLimited(new List<string>(importedVuforiaDatabaseTargetNames), 72) +
                       " | image: " + imageTargetCount +
                       " | " + barcodeSummary +
                       ". Print kartu dan isi frame kamera.";
            }

            return "Scanner siap. Target: " + targetSummary +
                   " | " + barcodeSummary +
                   ". Arahkan kamera ke " + BuildExampleHint() + ".";
        }

        static string JoinLimited(List<string> values, int maxCharacters)
        {
            if (values == null || values.Count == 0)
            {
                return "-";
            }

            string joined = string.Join(", ", values.ToArray());
            if (maxCharacters <= 0 || joined.Length <= maxCharacters)
            {
                return joined;
            }

            return joined.Substring(0, Mathf.Max(0, maxCharacters - 3)) + "...";
        }

        static string GetTrackingDisplayLabel(MaterialContentData content, string trackingName)
        {
            string contentLabel = GetContentDisplayName(content);
            if (string.IsNullOrWhiteSpace(trackingName) ||
                string.Equals(contentLabel, trackingName, StringComparison.OrdinalIgnoreCase))
            {
                return contentLabel;
            }

            return contentLabel + "=" + trackingName;
        }

        string GetObserverTrackingLabel(ObserverBehaviour behaviour)
        {
            if (behaviour != null &&
                observerTrackingNames.TryGetValue(behaviour, out string trackingName) &&
                !string.IsNullOrWhiteSpace(trackingName))
            {
                return trackingName;
            }

            return behaviour != null ? behaviour.name : "unknown";
        }

        string BuildInitialStatusMessage()
        {
            string categoryLabel = ARtiGraf.Core.AppSession.GetSelectedCategoryLabel();
            if (disableScannerForExperiment)
            {
                return ARtiGraf.Core.AppSession.SelectedCategory.HasValue
                    ? "Mode eksperimen " + categoryLabel + " aktif tanpa scanner. Object preview dimuat langsung untuk uji interaksi."
                    : "Mode eksperimen tanpa scanner aktif. Object preview dimuat langsung untuk uji interaksi.";
            }

            if (forceBarcodeOnlyScanning)
            {
                return ARtiGraf.Core.AppSession.SelectedCategory.HasValue
                    ? "Mode barcode " + categoryLabel + " aktif. Scan QR atau barcode " + BuildExampleHint() + "."
                    : "Mode barcode aktif. Scan QR atau barcode " + BuildExampleHint() + ".";
            }

            if (!ARtiGraf.Core.AppSession.SelectedCategory.HasValue)
            {
                return "Arahkan kamera ke kartu marker buah/hewan untuk mulai scan.";
            }

            return "Mode " + categoryLabel + " aktif. Scan kartu marker " + BuildExampleHint() + ".";
        }

        string BuildPreviewStatusMessage(MaterialContentData content)
        {
            string baseMessage = disableScannerForExperiment
                ? "Mode eksperimen tanpa scanner aktif."
                : ShouldUsePreviewOnlyMode()
                ? (editorWebcamReady
                    ? "Webcam editor aktif."
                    : "Preview editor aktif tanpa webcam.")
                : "Mode preview aktif.";
            string contentLabel = content != null ? " Konten: " + GetContentDisplayName(content) + "." : string.Empty;
            string controlLabel = ShouldUsePreviewOnlyMode()
                ? " Drag kiri putar 360 derajat, drag kanan/tengah geser, wheel zoom, panah kiri/kanan ganti, R reset."
                : " Putar 1 jari 360 derajat, geser 2 jari, cubit untuk zoom.";
            return baseMessage + contentLabel + controlLabel;
        }
    }
}
