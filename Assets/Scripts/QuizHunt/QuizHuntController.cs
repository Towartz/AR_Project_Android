using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ARtiGraf.Collection;
using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.QuizHunt
{
    /// <summary>
    /// Mode misi: anak membaca ciri, mencari flashcard yang cocok, lalu scan di AR.
    /// State sesi disimpan di AppSession supaya tetap aman ketika pindah scene ke scanner.
    /// </summary>
    public class QuizHuntController : MonoBehaviour
    {
        [Header("Library")]
        [SerializeField] MaterialContentLibrary library;
        const string DefaultLibraryResource = "ARtiGrafContentLibrary";

        [Header("Hunt UI")]
        [SerializeField] Text clueText;
        [SerializeField] Text clueSubText;
        [SerializeField] Text progressText;
        [SerializeField] Text timerText;
        [SerializeField] Text scoreText;
        [SerializeField] GameObject hintPanel;
        [SerializeField] Text hintText;
        [SerializeField] Image clueImage;
        [SerializeField] GameObject successPanel;
        [SerializeField] Text successTitleText;
        [SerializeField] Text successDescText;
        [SerializeField] GameObject failPanel;
        [SerializeField] Text failText;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Text resultScoreText;
        [SerializeField] Text resultStarsText;

        [Header("Mode")]
        [SerializeField] bool useTimed = false;
        [SerializeField] float timeLimitSeconds = 30f;
        [SerializeField] bool onlyFromDiscovered = false;
        [Tooltip("Jumlah soal per sesi")]
        [SerializeField] int questionsPerSession = 5;
        [SerializeField] LearningCategory? filterCategory = null;

        [Header("Game Feel")]
        [SerializeField] bool hideTargetNameInClue = true;
        [SerializeField] bool typewriterClues = true;
        [SerializeField] float clueTypeDelaySeconds = 0.012f;
        [SerializeField] int revealImageHintLevel = 3;
        [SerializeField] int pointsPerCorrect = 10;
        [SerializeField] int streakBonusPoints = 2;
        [SerializeField] float correctAdvanceDelaySeconds = 2.6f;
        [SerializeField] float timeoutAdvanceDelaySeconds = 3f;

        [Header("SFX")]
        [SerializeField] AudioSource sfxAudioSource;
        [SerializeField] AudioClip correctSfx;
        [SerializeField, Range(0f, 1f)] float correctSfxVolume = 0.95f;
        [SerializeField] AudioClip wrongSfx;
        [SerializeField, Range(0f, 1f)] float wrongSfxVolume = 0.9f;

        [Header("Navigator")]
        [SerializeField] Core.SceneNavigationController navigator;

        struct HuntQuestion
        {
            public MaterialContentData Target;
            public string ClueText;
            public string ClueSubText;
        }

        readonly List<HuntQuestion> questions = new List<HuntQuestion>();
        int currentIndex;
        int correctCount;
        int starsEarned;
        int score;
        int streak;
        int hintLevel;
        int wrongAttempts;
        float timeLeft;
        bool timerRunning;
        bool sessionActive;
        Coroutine timerRoutine;
        Coroutine clueRoutine;

        void Start()
        {
            EnsureSfxAudioSource();
            LoadDefaultSfx();
            EnsureLibrary();
            EnsureRuntimeClueImage();
            ApplyResponsiveLayout();
            HideTransientPanels();

            if (!RestoreSession())
                BuildSession();

            if (AppSession.TryConsumeQuizHuntScanResult(out string scannedContentId))
            {
                ShowCurrentQuestion(animateClue: false);
                OnScanResult(scannedContentId);
                return;
            }

            ShowCurrentQuestion();
        }

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyResponsiveLayout();
        }

        void EnsureLibrary()
        {
            if (library == null)
                library = Resources.Load<MaterialContentLibrary>(DefaultLibraryResource);
        }

        void EnsureRuntimeClueImage()
        {
            if (clueImage != null || transform == null) return;

            var go = new GameObject("RuntimeClueImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            clueImage = go.GetComponent<Image>();
            clueImage.preserveAspect = true;
            clueImage.raycastTarget = false;
            clueImage.color = new Color(1f, 1f, 1f, 0.92f);

            RectTransform rect = clueImage.rectTransform;
            rect.anchorMin = new Vector2(0.36f, 0.43f);
            rect.anchorMax = new Vector2(0.64f, 0.62f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.SetActive(false);
        }

        void HideTransientPanels()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
            if (hintPanel != null) hintPanel.SetActive(false);
            if (clueImage != null) clueImage.gameObject.SetActive(false);
        }

        void BuildSession()
        {
            questions.Clear();
            currentIndex = 0;
            correctCount = 0;
            starsEarned = 0;
            score = 0;
            streak = 0;

            var pool = new List<MaterialContentData>();
            if (library != null)
            {
                for (int i = 0; i < library.Items.Count; i++)
                {
                    MaterialContentData item = library.Items[i];
                    if (item == null || item.IsDemoContent) continue;
                    if (filterCategory.HasValue && item.Category != filterCategory.Value) continue;
                    if (onlyFromDiscovered && !AppSession.HasStarForContent(item.Id)) continue;
                    pool.Add(item);
                }
            }

            Shuffle(pool);
            int count = Mathf.Min(Mathf.Max(1, questionsPerSession), pool.Count);
            var sessionIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                questions.Add(BuildQuestion(pool[i]));
                sessionIds.Add(pool[i].Id);
            }

            AppSession.StartQuizHuntSession(sessionIds);
            sessionActive = questions.Count > 0;
            PersistSessionState();
        }

        bool RestoreSession()
        {
            if (!AppSession.HasQuizHuntSession || library == null) return false;

            questions.Clear();
            IReadOnlyList<string> ids = AppSession.QuizHuntSessionQuestionIds;
            for (int i = 0; i < ids.Count; i++)
            {
                if (library.TryGetById(ids[i], out MaterialContentData content) && content != null && !content.IsDemoContent)
                    questions.Add(BuildQuestion(content));
            }

            if (questions.Count == 0) return false;

            currentIndex = Mathf.Clamp(AppSession.QuizHuntSessionCurrentIndex, 0, questions.Count);
            correctCount = Mathf.Clamp(AppSession.QuizHuntSessionCorrectCount, 0, questions.Count);
            starsEarned = Mathf.Max(0, AppSession.QuizHuntSessionStarsEarned);
            score = Mathf.Max(0, AppSession.QuizHuntSessionScore);
            streak = Mathf.Max(0, AppSession.QuizHuntSessionStreak);
            sessionActive = currentIndex < questions.Count;
            return true;
        }

        void PersistSessionState()
        {
            AppSession.StoreQuizHuntProgress(currentIndex, correctCount, starsEarned, score, streak);
        }

        HuntQuestion BuildQuestion(MaterialContentData content)
        {
            return new HuntQuestion
            {
                Target = content,
                ClueText = BuildMysteryClue(content),
                ClueSubText = BuildSubClue(content)
            };
        }

        string BuildMysteryClue(MaterialContentData c)
        {
            string description = FirstUsefulSentence(c.Description);
            if (string.IsNullOrWhiteSpace(description))
                description = FirstUsefulSentence(c.Subtitle);

            if (hideTargetNameInClue)
                description = HideTargetName(description, c.Title);

            if (string.IsNullOrWhiteSpace(description))
                description = "Aku ada di salah satu kartu " + AppSession.GetCategoryLabel(c.Category).ToLowerInvariant() + ".";

            return "MISI RAHASIA\n\n"
                 + description + "\n\n"
                 + "Cari dan scan kartu yang cocok.";
        }

        static string BuildSubClue(MaterialContentData c)
        {
            string category = AppSession.GetCategoryLabel(c.Category);
            string firstLetter = !string.IsNullOrWhiteSpace(c.Title) ? c.Title.Substring(0, 1).ToUpperInvariant() : "?";
            int letterCount = CountLetters(c.Title);
            return "Kategori: " + category + " | Awal: " + firstLetter + " | " + letterCount + " huruf";
        }

        static string FirstUsefulSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string cleaned = Regex.Replace(text.Trim(), @"\s+", " ");
            int end = cleaned.IndexOf('.');
            if (end >= 40) cleaned = cleaned.Substring(0, end + 1);
            if (cleaned.Length > 118) cleaned = TrimToReadableLength(cleaned, 118);
            return cleaned;
        }

        static string TrimToReadableLength(string value, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxChars) return value;

            int cut = Mathf.Clamp(maxChars - 1, 24, value.Length - 1);
            for (int i = cut; i > Mathf.Max(0, cut - 30); i--)
            {
                if (char.IsWhiteSpace(value[i]) || value[i] == ',' || value[i] == '.')
                {
                    cut = i;
                    break;
                }
            }

            return value.Substring(0, cut).TrimEnd(' ', ',', '.') + "...";
        }

        static string HideTargetName(string text, string title)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(title)) return text;
            return Regex.Replace(text, Regex.Escape(title), "Aku", RegexOptions.IgnoreCase);
        }

        static int CountLetters(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
                if (char.IsLetter(value[i])) count++;
            return count;
        }

        void ShowCurrentQuestion(bool animateClue = true)
        {
            if (currentIndex >= questions.Count) { ShowResult(); return; }
            HuntQuestion q = questions[currentIndex];

            hintLevel = 0;
            wrongAttempts = 0;
            HideTransientPanels();

            if (clueRoutine != null) StopCoroutine(clueRoutine);
            if (clueText != null)
            {
                if (typewriterClues && animateClue)
                    clueRoutine = StartCoroutine(TypeText(clueText, q.ClueText));
                else
                    clueText.text = q.ClueText;
            }

            if (clueSubText != null) clueSubText.text = q.ClueSubText;
            ApplyResponsiveLayout();
            RefreshProgressText();
            RefreshScoreText();

            if (useTimed)
            {
                if (timerRoutine != null) StopCoroutine(timerRoutine);
                timerRoutine = StartCoroutine(TimerRoutine());
            }
            else if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }

            AppSession.SetQuizHuntTarget(q.Target.Id);
            sessionActive = true;
            PersistSessionState();
        }

        IEnumerator TypeText(Text target, string value)
        {
            target.text = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                target.text += value[i];
                if (!char.IsWhiteSpace(value[i]))
                    yield return new WaitForSecondsRealtime(clueTypeDelaySeconds);
            }
        }

        void RefreshProgressText()
        {
            if (progressText != null)
                progressText.text = "Misi " + (currentIndex + 1) + " / " + questions.Count;
        }

        void RefreshScoreText()
        {
            if (scoreText != null)
                scoreText.text = "Poin: " + score + "   Benar: " + correctCount + "   Streak: " + streak;
        }

        IEnumerator TimerRoutine()
        {
            timeLeft = timeLimitSeconds;
            if (timerText != null) timerText.gameObject.SetActive(true);
            timerRunning = true;

            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;
                if (timerText != null) timerText.text = "Waktu: " + Mathf.CeilToInt(timeLeft) + "s";
                yield return null;
            }

            timerRunning = false;
            OnTimeUp();
        }

        void OnTimeUp()
        {
            if (currentIndex >= questions.Count) return;
            streak = 0;
            AppSession.ClearQuizHuntTarget();
            PlaySfx(wrongSfx, wrongSfxVolume);
            if (failPanel != null)
            {
                failPanel.SetActive(true);
                if (failText != null)
                    failText.text = "Waktu habis. Jawabannya: " + questions[currentIndex].Target.Title
                                  + ". Lanjut ke misi berikutnya.";
            }
            PersistSessionState();
            StartCoroutine(AutoAdvanceAfterDelay(timeoutAdvanceDelaySeconds));
        }

        public void OnScanResult(string scannedContentId)
        {
            if (!sessionActive || currentIndex >= questions.Count) return;

            if (timerRoutine != null) { StopCoroutine(timerRoutine); timerRoutine = null; }

            HuntQuestion q = questions[currentIndex];
            bool correct = MaterialContentKeyUtility.Normalize(scannedContentId) ==
                           MaterialContentKeyUtility.Normalize(q.Target.Id);

            if (correct)
            {
                int earned = pointsPerCorrect + streak * streakBonusPoints;
                score += earned;
                correctCount++;
                streak++;
                AppSession.UpdateStreak(streak);

                bool newStar = !AppSession.HasStarForContent(q.Target.Id);
                if (newStar)
                {
                    AppSession.AwardStarForContent(q.Target.Id);
                    starsEarned++;
                }

                CollectionManager.Instance?.RecordDiscovered(q.Target);
                AppSession.ClearQuizHuntTarget();
                PersistSessionState();
                PlaySfx(correctSfx, correctSfxVolume);

                if (successPanel != null)
                {
                    successPanel.SetActive(true);
                    if (successTitleText != null)
                        successTitleText.text = newStar ? "Kartu baru terbuka!" : "Benar lagi!";
                    if (successDescText != null)
                        successDescText.text = "Jawabannya " + q.Target.Title + ". +" + earned + " poin."
                                             + (streak > 1 ? "\nStreak " + streak + " misi!" : "");
                }

                RefreshScoreText();
                StartCoroutine(AutoAdvanceAfterDelay(correctAdvanceDelaySeconds));
                return;
            }

            wrongAttempts++;
            streak = 0;
            AppSession.SetQuizHuntTarget(q.Target.Id);
            PersistSessionState();
            PlaySfx(wrongSfx, wrongSfxVolume);

            if (failPanel != null)
            {
                failPanel.SetActive(true);
                if (failText != null)
                    failText.text = BuildWrongFeedback(scannedContentId, q.Target);
            }

            RevealHint(Mathf.Min(revealImageHintLevel, wrongAttempts + 1), autoOpened: true);
            RefreshScoreText();
        }

        void LoadDefaultSfx()
        {
            if (correctSfx == null)
                correctSfx = Resources.Load<AudioClip>("SFX/correct");
            if (wrongSfx == null)
                wrongSfx = Resources.Load<AudioClip>("SFX/wrong");
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

        string BuildWrongFeedback(string scannedContentId, MaterialContentData target)
        {
            string scannedTitle = scannedContentId;
            if (library != null && library.TryGetById(scannedContentId, out MaterialContentData scanned) && scanned != null)
                scannedTitle = scanned.Title;

            string initial = !string.IsNullOrWhiteSpace(target.Title) ? target.Title.Substring(0, 1).ToUpperInvariant() : "?";
            return "Belum cocok.\n"
                 + "Kamu scan: " + scannedTitle + ".\n"
                 + "Target diawali huruf " + initial + ".";
        }

        IEnumerator AutoAdvanceAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            currentIndex++;
            PersistSessionState();
            ShowCurrentQuestion();
        }

        public void OnShowHintPressed()
        {
            RevealHint(hintLevel + 1, autoOpened: false);
        }

        void RevealHint(int requestedLevel, bool autoOpened)
        {
            if (currentIndex >= questions.Count || hintPanel == null) return;

            hintLevel = Mathf.Clamp(requestedLevel, 1, revealImageHintLevel);
            HuntQuestion q = questions[currentIndex];
            hintPanel.SetActive(true);
            if (hintText != null)
                hintText.text = BuildHintText(q.Target, hintLevel, autoOpened);

            bool showImage = hintLevel >= revealImageHintLevel;
            UpdateHintImage(q.Target, showImage);
            ApplyResponsiveLayout();
        }

        string BuildHintText(MaterialContentData c, int level, bool autoOpened)
        {
            string prefix = autoOpened ? "Petunjuk terbuka.\n" : "";
            string firstLetter = !string.IsNullOrWhiteSpace(c.Title) ? c.Title.Substring(0, 1).ToUpperInvariant() : "?";
            int letterCount = CountLetters(c.Title);

            if (level <= 1)
                return prefix + "Clue 1: huruf awalnya " + firstLetter + " dan namanya punya " + letterCount + " huruf.";

            if (level == 2)
            {
                string extra = !string.IsNullOrWhiteSpace(c.FunFact)
                    ? HideTargetName(FirstUsefulSentence(c.FunFact), c.Title)
                    : FirstUsefulSentence(c.Description);
                if (string.IsNullOrWhiteSpace(extra))
                    extra = "Perhatikan warna, bentuk, dan gambar utama di flashcard.";
                return prefix + "Clue 2: " + TrimToReadableLength(extra, 88);
            }

            return prefix + "Clue terakhir: lihat gambar kartu, lalu scan kartu yang sama.";
        }

        void UpdateHintImage(MaterialContentData content, bool visible)
        {
            if (clueImage == null) return;
            if (!visible)
            {
                clueImage.gameObject.SetActive(false);
                return;
            }

            if (!RuntimeSpriteCache.ApplyContentSprite(clueImage, content, new Color(1f, 1f, 1f, 0.96f)))
            {
                clueImage.gameObject.SetActive(false);
                return;
            }

            clueImage.gameObject.SetActive(true);
        }

        void ApplyResponsiveLayout()
        {
            Vector2 size = ResolveCanvasSize();
            bool portrait = size.y >= size.x;
            bool compact = Mathf.Min(size.x, size.y) < 760f;

            RectTransform cluePanel = clueText != null && clueText.transform.parent != null
                ? clueText.transform.parent.GetComponent<RectTransform>()
                : null;

            SetAnchors(GetRect(clueText),
                portrait ? new Vector2(0.055f, 0.30f) : new Vector2(0.06f, 0.31f),
                portrait ? new Vector2(0.945f, 0.94f) : new Vector2(0.94f, 0.94f));
            SetAnchors(GetRect(clueSubText),
                portrait ? new Vector2(0.065f, 0.055f) : new Vector2(0.07f, 0.055f),
                portrait ? new Vector2(0.935f, 0.29f) : new Vector2(0.93f, 0.29f));
            SetAnchors(cluePanel,
                portrait ? new Vector2(compact ? 0.045f : 0.06f, 0.53f) : new Vector2(0.16f, 0.48f),
                portrait ? new Vector2(compact ? 0.955f : 0.94f, 0.805f) : new Vector2(0.84f, 0.80f));
            SetAnchors(GetRect(progressText),
                portrait ? new Vector2(0.18f, 0.83f) : new Vector2(0.25f, 0.83f),
                portrait ? new Vector2(0.82f, 0.88f) : new Vector2(0.75f, 0.88f));
            SetAnchors(GetRect(scoreText),
                portrait ? new Vector2(0.08f, 0.458f) : new Vector2(0.20f, 0.402f),
                portrait ? new Vector2(0.92f, 0.512f) : new Vector2(0.80f, 0.464f));
            SetAnchors(GetRect(timerText),
                portrait ? new Vector2(0.08f, 0.402f) : new Vector2(0.20f, 0.354f),
                portrait ? new Vector2(0.92f, 0.456f) : new Vector2(0.80f, 0.408f));
            SetAnchors(GetRect(clueImage),
                portrait ? new Vector2(compact ? 0.32f : 0.35f, 0.315f) : new Vector2(0.39f, 0.27f),
                portrait ? new Vector2(compact ? 0.68f : 0.65f, 0.455f) : new Vector2(0.61f, 0.45f));

            RectTransform hintRect = hintPanel != null ? hintPanel.GetComponent<RectTransform>() : null;
            SetAnchors(hintRect,
                portrait ? new Vector2(0.07f, 0.245f) : new Vector2(0.20f, 0.22f),
                portrait ? new Vector2(0.93f, 0.325f) : new Vector2(0.80f, 0.32f));

            SetAnchors(GetRect(hintText), new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.90f));

            Button scanButton = FindNamedButton("ScanButton");
            Button hintButton = FindNamedButton("HintButton");
            Button skipButton = FindNamedButton("SkipButton");
            Button menuButton = FindNamedButton("MenuButton");

            SetAnchors(GetRect(scanButton),
                portrait ? new Vector2(compact ? 0.10f : 0.14f, 0.155f) : new Vector2(0.26f, 0.12f),
                portrait ? new Vector2(compact ? 0.90f : 0.86f, 0.23f) : new Vector2(0.74f, 0.20f));
            SetAnchors(GetRect(hintButton),
                portrait ? new Vector2(0.07f, 0.055f) : new Vector2(0.18f, 0.045f),
                portrait ? new Vector2(0.34f, 0.125f) : new Vector2(0.38f, 0.105f));
            SetAnchors(GetRect(skipButton),
                portrait ? new Vector2(0.365f, 0.055f) : new Vector2(0.40f, 0.045f),
                portrait ? new Vector2(0.635f, 0.125f) : new Vector2(0.60f, 0.105f));
            SetAnchors(GetRect(menuButton),
                portrait ? new Vector2(0.66f, 0.055f) : new Vector2(0.62f, 0.045f),
                portrait ? new Vector2(0.93f, 0.125f) : new Vector2(0.82f, 0.105f));

            ConfigureText(clueText, portrait ? (compact ? 36 : 40) : 42, compact ? 25 : 26, TextAnchor.MiddleCenter);
            ConfigureText(clueSubText, portrait ? (compact ? 26 : 28) : 30, 21, TextAnchor.MiddleCenter);
            ConfigureText(progressText, portrait ? 31 : 32, compact ? 20 : 21, TextAnchor.MiddleCenter);
            ConfigureText(scoreText, portrait ? 27 : 29, 19, TextAnchor.MiddleCenter);
            ConfigureText(timerText, portrait ? 27 : 29, 19, TextAnchor.MiddleCenter);
            ConfigureText(hintText, portrait ? (compact ? 25 : 27) : 29, 21, TextAnchor.MiddleCenter);
            ConfigureText(successTitleText, portrait ? 34 : 36, 22, TextAnchor.MiddleCenter);
            ConfigureText(successDescText, portrait ? 27 : 29, 21, TextAnchor.MiddleCenter);
            ConfigureText(failText, portrait ? 28 : 30, 21, TextAnchor.MiddleCenter);
            ConfigureText(resultScoreText, portrait ? 34 : 36, 22, TextAnchor.MiddleCenter);
            ConfigureText(resultStarsText, portrait ? 27 : 29, 19, TextAnchor.MiddleCenter);
        }

        Vector2 ResolveCanvasSize()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
                return new Vector2(canvasRect.rect.width, canvasRect.rect.height);
            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        Button FindNamedButton(string name)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == name)
                    return buttons[i];
            }

            return null;
        }

        static RectTransform GetRect(Graphic graphic) => graphic != null ? graphic.rectTransform : null;
        static RectTransform GetRect(Selectable selectable) => selectable != null ? selectable.GetComponent<RectTransform>() : null;

        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ConfigureText(Text text, int maxSize, int minSize, TextAnchor anchor)
        {
            BuhenARTextStyle.Configure(text, maxSize, minSize, anchor, VerticalWrapMode.Truncate, 1.14f);
        }

        public void OnScanButtonPressed()
        {
            if (currentIndex < questions.Count)
            {
                AppSession.SetQuizHuntTarget(questions[currentIndex].Target.Id);
                PersistSessionState();
            }
            if (navigator != null) navigator.OpenARForAllContent();
        }

        public void OnSkipPressed()
        {
            if (timerRoutine != null) { StopCoroutine(timerRoutine); timerRoutine = null; }
            streak = 0;
            AppSession.ClearQuizHuntTarget();
            currentIndex++;
            PersistSessionState();
            ShowCurrentQuestion();
        }

        public void OnPlayAgainPressed()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            AppSession.ClearQuizHuntSession();
            BuildSession();
            ShowCurrentQuestion();
        }

        public void OnMainMenuPressed()
        {
            AppSession.ClearQuizHuntSession();
            navigator?.OpenMaterialSelect();
        }

        void ShowResult()
        {
            sessionActive = false;
            AppSession.ClearQuizHuntTarget();
            AppSession.StoreQuizResult(correctCount, questions.Count);
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultScoreText != null)
                resultScoreText.text = correctCount + " / " + questions.Count + " benar\nTotal poin: " + score;
            if (resultStarsText != null)
                resultStarsText.text = starsEarned > 0
                    ? "+" + starsEarned + " bintang baru. Koleksi makin lengkap!"
                    : "Belum ada bintang baru. Cari kartu yang belum pernah kamu buka.";
            AppSession.ClearQuizHuntSession();
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
