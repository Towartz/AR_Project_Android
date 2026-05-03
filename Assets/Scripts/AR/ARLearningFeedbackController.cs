using System.Collections;
using ARtiGraf.Core;
using ARtiGraf.Data;
using UnityEngine;
using UnityEngine.UI;
using ARtiGraf.Collection;
using ARtiGraf.UI;

namespace ARtiGraf.AR
{
    /// <summary>
    /// Mengelola fitur belajar interaktif di ARScanScene:
    /// 1. Suara nama objek saat marker terdeteksi.
    /// 2. Animasi tap: objek memantul dan fun fact muncul.
    /// 3. Teks deskripsi muncul seperti mesin tik (typewriter).
    /// 4. Tampilan bintang jika konten ini sudah pernah diselesaikan.
    /// </summary>
    public class ARLearningFeedbackController : MonoBehaviour
    {
        [Header("Suara Nama Objek")]
        [SerializeField] AudioSource audioSource;

        [Header("TTS Ejaan")]
        [Tooltip("Assign TTSController dari scene. Jika kosong, dicari otomatis.")]
        [SerializeField] TTSController ttsController;
        [SerializeField] bool autoSpellOnScan = true;
        [SerializeField] bool speakWordAfterSpell = true;

        [Header("Typewriter")]
        [SerializeField] Text descriptionText;
        [SerializeField] float typewriterSpeed = 0.03f;

        [Header("Fun Fact Popup")]
        [SerializeField] GameObject funFactPanel;
        [SerializeField] Text funFactText;
        [SerializeField] float funFactDuration = 9.5f;

        [Header("First Discovery Effect")]
        [SerializeField] bool showFirstDiscoveryEffect = true;
        [SerializeField] float discoveryEffectDuration = 1.75f;
        [SerializeField] Color discoveryEffectColor = new Color(1f, 0.78f, 0.16f, 0.92f);
        [SerializeField] string discoveryTitle = "Baru ditemukan!";

        [Header("SFX")]
        [SerializeField] AudioSource sfxAudioSource;
        [SerializeField] AudioClip firstScanSfx;
        [SerializeField, Range(0f, 1f)] float firstScanSfxVolume = 0.38f;

        [Header("Tombol Quiz")]
        [SerializeField] GameObject quizButton;
        [SerializeField] Text quizButtonLabel;

        [Header("Tombol Info Panel")]
        [Tooltip("Button Info hanya muncul setelah scan berhasil")]
        [SerializeField] GameObject infoButton;
        [SerializeField] GameObject infoPanel;

        [Header("Tombol Koleksi")]
        [SerializeField] GameObject collectionButton;

        [Header("Bintang")]
        [SerializeField] GameObject starIcon;
        [SerializeField] Text starCountText;

        [Header("Navigator")]
        [SerializeField] SceneNavigationController navigator;

        [Header("Responsive Layout")]
        [SerializeField] RectTransform quizButtonRect;
        [SerializeField] RectTransform funFactPanelRect;
        [SerializeField] RectTransform starIconRect;

        MaterialContentData currentContent;
        Coroutine typewriterCoroutine;
        Coroutine funFactCoroutine;
        Coroutine discoveryEffectCoroutine;
        bool funFactShown;
        int lastLayoutWidth = -1;
        int lastLayoutHeight = -1;

        void Start()
        {
            EnsureLayoutReferences();
            EnsureInfoReferences();
            EnsureTTSController();
            EnsureSfxAudioSource();
            NormalizeSfxVolume();
            NormalizeFunFactDuration();
            LoadDefaultSfx();
            ApplyResponsiveLayout(force: true);
            if (funFactPanel != null) funFactPanel.SetActive(false);
            if (quizButton != null) quizButton.SetActive(false);
            if (starIcon != null) starIcon.SetActive(false);
            // Info button disembunyikan di awal — muncul setelah scan
            if (infoButton != null) infoButton.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
            if (collectionButton != null) collectionButton.SetActive(false);
        }

        void Update()
        {
            ApplyResponsiveLayout();
        }

        // ── Dipanggil dari ARImageTrackingController saat konten ditemukan ───
        public void OnContentDetected(MaterialContentData content, Transform arObject)
        {
            if (content == null) return;
            currentContent = content;
            funFactShown = false;
            bool isNewDiscovery = !AppSession.IsContentDiscovered(content.Id);

            // TTS: eja huruf dulu, lalu baca nama
            if (autoSpellOnScan)
                TriggerTTS(content);
            else
                PlayNameAudio(content);

            StartTypewriter(content.Description);
            RefreshQuizButton(content);
            RefreshStarDisplay(content);

            // Info button dan collection button muncul setelah scan pertama
            if (infoButton != null) infoButton.SetActive(true);
            if (collectionButton != null) collectionButton.SetActive(true);

            // Catat ke koleksi
            bool recordedNewDiscovery = AppSession.RecordDiscoveredContent(content);
            CollectionManager.Instance?.RecordDiscovered(content);

            if (showFirstDiscoveryEffect && (isNewDiscovery || recordedNewDiscovery))
            {
                PlayFirstDiscoveryEffect(content, arObject);
            }
        }

        public void OnContentLost()
        {
            currentContent = null;
            StopTypewriter();
            HideFunFact();
            if (quizButton != null) quizButton.SetActive(false);
            if (infoButton != null) infoButton.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
        }

        // ── TTS ───────────────────────────────────────────────────────────────
        void EnsureTTSController()
        {
            if (ttsController == null)
                ttsController = TTSController.Instance ?? FindAnyObjectByType<TTSController>();

            if (ttsController == null)
            {
                GameObject ttsObject = new GameObject("BuhenAR_RuntimeTTS");
                DontDestroyOnLoad(ttsObject);
                ttsObject.AddComponent<AudioSource>();
                ttsController = ttsObject.AddComponent<TTSController>();
            }
        }

        void TriggerTTS(MaterialContentData content)
        {
            EnsureTTSController();
            if (ttsController == null)
            {
                PlayNameAudio(content);
                return;
            }

            // SpellWord: bisa override di Inspector. Default = Title konten.
            // Contoh: Title "Apel" -> SpellWord "Apel" -> eja A-P-E-L lalu baca "Apel"
            if (speakWordAfterSpell)
                ttsController.SpellThenSpeak(content.SpellWord, content.Title, content.NameAudioClip);
            else
                ttsController.SpellOnly(content.SpellWord);
        }

        // ── Suara nama objek (fallback tanpa TTS) ─────────────────────────────
        void PlayNameAudio(MaterialContentData content)
        {
            if (audioSource == null || content.NameAudioClip == null) return;
            audioSource.Stop();
            audioSource.clip = content.NameAudioClip;
            audioSource.Play();
        }

        // ── Toggle Info Panel ─────────────────────────────────────────────────
        public void OnInfoButtonPressed()
        {
            if (infoPanel == null) return;
            infoPanel.SetActive(!infoPanel.activeSelf);
        }

        void PlayFirstDiscoveryEffect(MaterialContentData content, Transform arObject)
        {
            if (discoveryEffectCoroutine != null)
            {
                StopCoroutine(discoveryEffectCoroutine);
            }

            PlaySfx(firstScanSfx, firstScanSfxVolume);
            SpawnObjectSparkles(arObject);
            discoveryEffectCoroutine = StartCoroutine(DiscoveryOverlayRoutine(content));
        }

        void LoadDefaultSfx()
        {
            if (firstScanSfx == null)
                firstScanSfx = Resources.Load<AudioClip>("SFX/first-scan_reward");
        }

        void NormalizeSfxVolume()
        {
            // Scene lama bisa masih menyimpan 0.95; turunkan supaya TTS tetap jelas.
            if (firstScanSfxVolume > 0.45f)
                firstScanSfxVolume = 0.38f;
        }

        void NormalizeFunFactDuration()
        {
            // Scene lama bisa masih menyimpan 3.5 detik; durasi ini terlalu cepat untuk anak membaca.
            if (funFactDuration < 9.5f)
                funFactDuration = 9.5f;
        }

        void EnsureSfxAudioSource()
        {
            if (sfxAudioSource != null) return;
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.ignoreListenerPause = true;
        }

        void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null) return;
            EnsureSfxAudioSource();
            if (sfxAudioSource != null)
                sfxAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        void SpawnObjectSparkles(Transform arObject)
        {
            if (arObject == null)
            {
                return;
            }

            GameObject fx = new GameObject("FirstDiscoverySparkles");
            fx.transform.SetParent(arObject, false);
            fx.transform.localPosition = Vector3.up * 0.08f;
            fx.transform.localRotation = Quaternion.identity;
            fx.transform.localScale = Vector3.one;

            ParticleSystem particles = fx.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.9f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.32f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.052f);
            main.startColor = discoveryEffectColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 34),
                new ParticleSystem.Burst(0.18f, 18)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.09f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            Light glow = fx.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = discoveryEffectColor;
            glow.range = 0.55f;
            glow.intensity = 1.8f;

            particles.Play();
            Destroy(fx, discoveryEffectDuration + 0.5f);
        }

        IEnumerator DiscoveryOverlayRoutine(MaterialContentData content)
        {
            GameObject root = CreateDiscoveryOverlay(content);
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            RectTransform card = root.transform.Find("Card") as RectTransform;

            float duration = Mathf.Max(0.5f, discoveryEffectDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float fadeIn = Mathf.Clamp01(t / 0.2f);
                float fadeOut = Mathf.Clamp01((1f - t) / 0.28f);
                if (group != null)
                {
                    group.alpha = Mathf.Min(fadeIn, fadeOut);
                }

                if (card != null)
                {
                    float pulse = 1f + Mathf.Sin(t * Mathf.PI * 3f) * 0.055f;
                    card.localScale = Vector3.one * pulse;
                }

                yield return null;
            }

            Destroy(root);
            discoveryEffectCoroutine = null;
        }

        GameObject CreateDiscoveryOverlay(MaterialContentData content)
        {
            GameObject root = new GameObject("DiscoveryRewardOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            Image flash = CreateUiImage("Flash", root.transform, new Color(discoveryEffectColor.r, discoveryEffectColor.g, discoveryEffectColor.b, 0.20f));
            Stretch(flash.rectTransform, Vector2.zero, Vector2.zero);

            GameObject cardObject = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(root.transform, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.08f, 0.58f);
            cardRect.anchorMax = new Vector2(0.92f, 0.76f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = new Color(0.05f, 0.09f, 0.18f, 0.92f);

            Text title = CreateUiText("Title", cardObject.transform, 42, FontStyle.Bold, TextAnchor.MiddleCenter, discoveryTitle, Color.white);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.48f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            string contentName = content != null ? content.Title : "Item";
            Text subtitle = CreateUiText("Subtitle", cardObject.transform, 30, FontStyle.Bold, TextAnchor.MiddleCenter,
                contentName + " masuk koleksi", discoveryEffectColor);
            subtitle.rectTransform.anchorMin = new Vector2(0.05f, 0.08f);
            subtitle.rectTransform.anchorMax = new Vector2(0.95f, 0.52f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;

            return root;
        }

        static Image CreateUiImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text CreateUiText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment, string text, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.fontSize = BuhenARTextStyle.ScaleSize(fontSize, 1.12f);
            label.resizeTextMinSize = Mathf.Max(14, BuhenARTextStyle.ScaleSize(fontSize - 14, 1.08f));
            label.resizeTextMaxSize = label.fontSize;
            label.font = BuhenARTextStyle.ResolveFont(fontStyle);
            return label;
        }

        static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // ── Typewriter deskripsi ──────────────────────────────────────────────
        void StartTypewriter(string text)
        {
            StopTypewriter();
            if (descriptionText == null || string.IsNullOrEmpty(text)) return;
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(LimitReadableText(text, 145)));
        }

        void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
        }

        IEnumerator TypewriterRoutine(string fullText)
        {
            descriptionText.text = string.Empty;
            for (int i = 0; i < fullText.Length; i++)
            {
                descriptionText.text += fullText[i];
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }
        }

        static string LimitReadableText(string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string clean = value.Trim();
            if (clean.Length <= maxChars) return clean;

            int cut = Mathf.Clamp(maxChars - 1, 24, clean.Length - 1);
            for (int i = cut; i > Mathf.Max(0, cut - 32); i--)
            {
                if (char.IsWhiteSpace(clean[i]) || clean[i] == ',' || clean[i] == '.')
                {
                    cut = i;
                    break;
                }
            }

            return clean.Substring(0, cut).TrimEnd(' ', ',', '.') + "...";
        }

        // ── Efek tap pada objek AR ────────────────────────────────────────────
        /// <summary>
        /// Panggil ini dari ARImageTrackingController saat OnTap mengenai objek aktif.
        /// </summary>
        public void OnARObjectTapped(Transform arObject)
        {
            if (arObject != null)
                StartCoroutine(BounceRoutine(arObject));

            if (currentContent != null && !funFactShown && !string.IsNullOrEmpty(currentContent.FunFact))
                ShowFunFact(currentContent.FunFact);
        }

        IEnumerator BounceRoutine(Transform target)
        {
            Vector3 originalScale = target.localScale;
            Vector3 bigScale = originalScale * 1.18f;
            float duration = 0.08f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(originalScale, bigScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(bigScale, originalScale, elapsed / duration);
                yield return null;
            }

            target.localScale = originalScale;
        }

        // ── Fun fact popup ────────────────────────────────────────────────────
        void ShowFunFact(string fact)
        {
            funFactShown = true;
            if (funFactPanel == null) return;
            if (funFactText != null)
            {
                funFactText.text = fact;
                BuhenARTextStyle.Configure(funFactText, 32, 22, TextAnchor.MiddleCenter, VerticalWrapMode.Overflow, 1.08f);
            }
            funFactPanel.SetActive(true);

            if (funFactCoroutine != null) StopCoroutine(funFactCoroutine);
            funFactCoroutine = StartCoroutine(HideFunFactAfterDelay());
        }

        IEnumerator HideFunFactAfterDelay()
        {
            yield return new WaitForSecondsRealtime(funFactDuration);
            HideFunFact();
        }

        void HideFunFact()
        {
            if (funFactCoroutine != null)
            {
                StopCoroutine(funFactCoroutine);
                funFactCoroutine = null;
            }
            if (funFactPanel != null) funFactPanel.SetActive(false);
        }

        // ── Tombol latihan: Quiz biasa dipensiunkan, AR hanya membuka Quiz Hunt.
        void RefreshQuizButton(MaterialContentData content)
        {
            if (quizButton == null) return;
            quizButton.SetActive(content != null && !AppSession.IsQuizHuntActive);
            if (quizButtonLabel != null)
            {
                quizButtonLabel.text = "Quiz Hunt";
                BuhenARTextStyle.Configure(quizButtonLabel, 28, 22, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.08f);
            }
        }

        public void OnQuizButtonPressed()
        {
            if (navigator == null || currentContent == null) return;
            navigator.OpenQuizHunt();
        }

        // ── Bintang ───────────────────────────────────────────────────────────
        void RefreshStarDisplay(MaterialContentData content)
        {
            bool hasStar = AppSession.HasStarForContent(content.Id);
            if (starIcon != null) starIcon.SetActive(hasStar);
            if (starCountText != null)
            {
                starCountText.text = hasStar ? "Bintang didapat" : string.Empty;
                BuhenARTextStyle.Configure(starCountText, 25, 21, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.06f);
            }
        }

        // ── Dipanggil dari ResultScene / QuizScene setelah quiz selesai ───────
        public static void AwardStarIfPassed(string contentId, bool passed)
        {
            if (passed) AppSession.AwardStarForContent(contentId);
        }

        void EnsureLayoutReferences()
        {
            EnsureInfoReferences();
            if (quizButtonRect == null && quizButton != null)
                quizButtonRect = quizButton.GetComponent<RectTransform>();
            if (funFactPanelRect == null && funFactPanel != null)
                funFactPanelRect = funFactPanel.GetComponent<RectTransform>();
            if (starIconRect == null && starIcon != null)
                starIconRect = starIcon.GetComponent<RectTransform>();
        }

        void EnsureInfoReferences()
        {
            if (infoButton == null)
            {
                GameObject foundButton = GameObject.Find("InfoToggleButton");
                if (foundButton != null) infoButton = foundButton;
            }

            if (infoPanel == null)
            {
                GameObject foundPanel = GameObject.Find("InfoPanel");
                if (foundPanel != null) infoPanel = foundPanel;
            }
        }

        void ApplyResponsiveLayout(bool force = false)
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (!force && width == lastLayoutWidth && height == lastLayoutHeight)
            {
                return;
            }

            lastLayoutWidth = width;
            lastLayoutHeight = height;
            bool landscape = width > height;

            EnsureLayoutReferences();
            if (landscape)
            {
                SetAnchors(quizButtonRect, new Vector2(0.02f, 0.35f), new Vector2(0.18f, 0.43f));
                SetAnchors(funFactPanelRect, new Vector2(0.18f, 0.57f), new Vector2(0.58f, 0.73f));
                SetAnchors(starIconRect, new Vector2(0.40f, 0.75f), new Vector2(0.58f, 0.84f));
            }
            else
            {
                SetAnchors(quizButtonRect, new Vector2(0.04f, 0.50f), new Vector2(0.34f, 0.58f));
                SetAnchors(funFactPanelRect, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.75f));
                SetAnchors(starIconRect, new Vector2(0.08f, 0.59f), new Vector2(0.50f, 0.665f));
            }
        }

        static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
