using System.Collections;
using System.Collections.Generic;
using ARtiGraf.Core;
using ARtiGraf.Data;
using ARtiGraf.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARtiGraf.Quiz
{
    public class QuizManager : MonoBehaviour
    {
        sealed class RuntimeQuestion
        {
            public string Question;
            public string[] Options;
            public int CorrectIndex;
            public string Explanation;
        }

        [Header("Data")]
        [SerializeField] QuizQuestionBank questionBank;
        [SerializeField] MaterialContentLibrary contentLibrary;

        [Header("UI References")]
        [SerializeField] Text questionText;
        [SerializeField] Text progressText;
        [SerializeField] Text feedbackText;
        [SerializeField] Text streakText;
        [SerializeField] Text timerText;
        [SerializeField] Slider timerSlider;
        [SerializeField] Button[] optionButtons;
        [SerializeField] Text[] optionLabels;

        [Header("Scene")]
        [SerializeField] string resultSceneName = "ResultScene";

        [Header("Timing")]
        [SerializeField] float answerRevealDuration = 1.4f;
        [SerializeField] float questionTimeLimitSeconds = 20f;
        [SerializeField] bool enableTimer = true;

        [Header("Gameplay")]
        [SerializeField] bool shuffleQuestionOrder = true;
        [SerializeField] bool shuffleAnswerOrder   = true;

        [Header("SFX")]
        [SerializeField] AudioSource sfxAudioSource;
        [SerializeField] AudioClip correctSfx;
        [SerializeField, Range(0f, 1f)] float correctSfxVolume = 0.95f;
        [SerializeField] AudioClip wrongSfx;
        [SerializeField, Range(0f, 1f)] float wrongSfxVolume = 0.9f;

        // ── Runtime state ─────────────────────────────────────────────────────
        int  currentQuestionIndex;
        int  score;
        int  currentStreak;
        int  sessionBestStreak;
        bool isAnswerLocked;
        float questionTimeRemaining;
        bool timerRunning;
        Color defaultButtonColor;
        Color defaultTextColor;
        readonly List<RuntimeQuestion> runtimeQuestions = new List<RuntimeQuestion>();
        MaterialContentData activeContentQuiz;

        // Colors
        static readonly Color CorrectButtonColor = new Color32(220, 252, 231, 255);
        static readonly Color WrongButtonColor   = new Color32(254, 226, 226, 255);
        static readonly Color CorrectTextColor   = new Color32(21,  128,  61, 255);
        static readonly Color WrongTextColor     = new Color32(185,  28,  28, 255);
        static readonly Color TimerWarningColor  = new Color32(239, 104,  32, 255);
        static readonly Color TimerNormalColor   = new Color32(59,  130, 246, 255);

        // ── Unity lifecycle ───────────────────────────────────────────────────
        void Start()
        {
            EnsureSfxAudioSource();
            LoadDefaultSfx();
            BuildRuntimeQuestions();
            currentQuestionIndex = 0;
            score          = 0;
            currentStreak  = 0;
            sessionBestStreak = 0;
            CacheDefaultStyles();
            ApplyReadableTextStyles();
            RenderQuestion();
        }

        void Update()
        {
            if (!timerRunning || !enableTimer || isAnswerLocked) return;
            questionTimeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            if (questionTimeRemaining <= 0f)
            {
                questionTimeRemaining = 0f;
                OnTimeExpired();
            }
        }

        // ── Timer ─────────────────────────────────────────────────────────────
        void StartQuestionTimer()
        {
            questionTimeRemaining = questionTimeLimitSeconds;
            timerRunning = true;
            UpdateTimerUI();
        }

        void StopQuestionTimer() => timerRunning = false;

        void UpdateTimerUI()
        {
            float pct = questionTimeLimitSeconds > 0f
                ? questionTimeRemaining / questionTimeLimitSeconds : 0f;

            if (timerSlider != null) timerSlider.value = pct;
            if (timerText  != null)
            {
                timerText.text = Mathf.CeilToInt(questionTimeRemaining) + "s";
                timerText.color = pct < 0.3f ? TimerWarningColor : TimerNormalColor;
            }
        }

        void OnTimeExpired()
        {
            if (isAnswerLocked) return;
            // Treat as wrong answer — highlight correct answer only
            RuntimeQuestion question = runtimeQuestions[currentQuestionIndex];
            isAnswerLocked = true;
            currentStreak  = 0;
            PlaySfx(wrongSfx, wrongSfxVolume);
            ApplyTimeoutState(question);
            SetButtonsInteractable(false);
            UpdateStreakUI();
            if (feedbackText != null)
            {
                string correct = GetOptionText(question, question.CorrectIndex);
                string exp = string.IsNullOrWhiteSpace(question.Explanation)
                    ? string.Empty : " " + question.Explanation.Trim();
                feedbackText.text = "Waktu habis! Jawaban benar: " + correct + "." + exp;
            }
            StartCoroutine(AdvanceAfterDelay());
        }

        void ApplyTimeoutState(RuntimeQuestion question)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null || !optionButtons[i].gameObject.activeSelf) continue;
                bool isCorrect = i == question.CorrectIndex;
                Image img = optionButtons[i].GetComponent<Image>();
                if (img) img.color = isCorrect ? CorrectButtonColor : defaultButtonColor;
                if (i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].color = isCorrect ? CorrectTextColor : defaultTextColor;
            }
        }

        // ── Answer selection ──────────────────────────────────────────────────
        public void SelectAnswer(int answerIndex)
        {
            if (isAnswerLocked || runtimeQuestions.Count == 0) return;

            StopQuestionTimer();
            RuntimeQuestion question = runtimeQuestions[currentQuestionIndex];
            bool isCorrect = answerIndex == question.CorrectIndex;
            isAnswerLocked = true;

            if (isCorrect)
            {
                score++;
                currentStreak++;
                if (currentStreak > sessionBestStreak) sessionBestStreak = currentStreak;
                AppSession.UpdateStreak(sessionBestStreak);
                PlaySfx(correctSfx, correctSfxVolume);
            }
            else
            {
                currentStreak = 0;
                PlaySfx(wrongSfx, wrongSfxVolume);
            }

            ApplyAnswerState(question, answerIndex);
            SetButtonsInteractable(false);
            UpdateFeedback(question, answerIndex, isCorrect);
            UpdateStreakUI();
            StartCoroutine(AdvanceAfterDelay());
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

        IEnumerator AdvanceAfterDelay()
        {
            yield return new WaitForSeconds(answerRevealDuration);
            currentQuestionIndex++;

            if (currentQuestionIndex >= runtimeQuestions.Count)
            {
                AppSession.StoreQuizResult(score, runtimeQuestions.Count);
                AwardContentStarIfPassed();
                yield return LoadSceneAsync(resultSceneName);
                yield break;
            }

            isAnswerLocked = false;
            RenderQuestion();
        }

        IEnumerator LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                yield break;

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = true;
                while (!loadOperation.isDone)
                    yield return null;
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        // ── Render ────────────────────────────────────────────────────────────
        void RenderQuestion()
        {
            ApplyReadableTextStyles();
            if (runtimeQuestions.Count == 0)
            {
                if (questionText  != null) questionText.text  = "Bank soal belum diisi.";
                if (feedbackText  != null) feedbackText.text  = "Tambahkan soal pada Quiz Question Bank.";
                SetButtonsInteractable(false);
                return;
            }

            RuntimeQuestion question = runtimeQuestions[currentQuestionIndex];

            if (questionText != null) questionText.text = question.Question;
            if (progressText != null)
                progressText.text = "Soal " + (currentQuestionIndex + 1) + " / " + runtimeQuestions.Count
                                  + "  •  Skor " + score;
            if (feedbackText != null)
                feedbackText.text = enableTimer
                    ? "Jawab sebelum waktu habis."
                    : "Pilih jawaban yang paling tepat.";

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = question.Options != null && i < question.Options.Length;
                optionButtons[i].gameObject.SetActive(hasOption);
                optionButtons[i].interactable = hasOption;
                if (!hasOption) continue;
                ResetButtonVisual(i);
                if (i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = question.Options[i];
            }

            UpdateStreakUI();
            if (enableTimer) StartQuestionTimer();
        }

        void ApplyReadableTextStyles()
        {
            BuhenARTextStyle.Configure(questionText, 36, 23, TextAnchor.UpperLeft, VerticalWrapMode.Truncate, 1.08f);
            BuhenARTextStyle.Configure(progressText, 26, 19, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(feedbackText, 25, 19, TextAnchor.LowerLeft, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(streakText, 28, 20, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(timerText, 28, 20, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);

            if (optionLabels == null) return;
            for (int i = 0; i < optionLabels.Length; i++)
                BuhenARTextStyle.Configure(optionLabels[i], 30, 21, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.05f);
        }

        void UpdateStreakUI()
        {
            if (streakText == null) return;
            if (currentStreak >= 2)
            {
                streakText.gameObject.SetActive(true);
                streakText.text = currentStreak >= 5 ? "🔥 " + currentStreak + "x Streak!"
                                : currentStreak >= 3 ? "⚡ " + currentStreak + "x Streak"
                                : currentStreak + "x berturut-turut";
            }
            else
            {
                streakText.gameObject.SetActive(false);
            }
        }

        // ── Visual helpers ────────────────────────────────────────────────────
        void CacheDefaultStyles()
        {
            defaultButtonColor = optionButtons != null && optionButtons.Length > 0 && optionButtons[0] != null
                ? (optionButtons[0].GetComponent<Image>()?.color ?? Color.white) : Color.white;
            defaultTextColor   = optionLabels  != null && optionLabels.Length  > 0 && optionLabels[0]  != null
                ? optionLabels[0].color : Color.black;
        }

        void ApplyAnswerState(RuntimeQuestion question, int selectedAnswer)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null || !optionButtons[i].gameObject.activeSelf) continue;
                bool isCorrectAnswer  = i == question.CorrectIndex;
                bool isSelectedWrong  = i == selectedAnswer && !isCorrectAnswer;
                Image img = optionButtons[i].GetComponent<Image>();
                if (img) img.color = isCorrectAnswer ? CorrectButtonColor
                                   : isSelectedWrong ? WrongButtonColor : defaultButtonColor;
                if (i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].color = isCorrectAnswer ? CorrectTextColor
                                          : isSelectedWrong ? WrongTextColor : defaultTextColor;
            }
        }

        void ResetButtonVisual(int index)
        {
            if (index < 0 || index >= optionButtons.Length || optionButtons[index] == null) return;
            Image img = optionButtons[index].GetComponent<Image>();
            if (img) img.color = defaultButtonColor;
            if (index < optionLabels.Length && optionLabels[index] != null)
                optionLabels[index].color = defaultTextColor;
        }

        void SetButtonsInteractable(bool v)
        {
            if (optionButtons == null) return;
            foreach (var btn in optionButtons)
                if (btn != null && btn.gameObject.activeSelf) btn.interactable = v;
        }

        void UpdateFeedback(RuntimeQuestion question, int selectedAnswer, bool isCorrect)
        {
            if (feedbackText == null || question == null) return;
            string exp = string.IsNullOrWhiteSpace(question.Explanation)
                ? "Perhatikan kembali konsep materi ini." : question.Explanation.Trim();
            string correctAnswer = GetOptionText(question, question.CorrectIndex);
            feedbackText.text = isCorrect
                ? "✓ Benar! " + exp
                : "✗ Belum tepat. Jawaban benar: " + correctAnswer + ". " + exp;
        }

        static string GetOptionText(RuntimeQuestion q, int idx)
        {
            return q.Options != null && idx >= 0 && idx < q.Options.Length ? q.Options[idx] : string.Empty;
        }

        // ── Question build ────────────────────────────────────────────────────
        void BuildRuntimeQuestions()
        {
            runtimeQuestions.Clear();
            activeContentQuiz = ResolveLastViewedContentWithQuiz();
            if (activeContentQuiz != null)
            {
                BuildRuntimeQuestionsFromContent(activeContentQuiz);
                if (shuffleQuestionOrder) Shuffle(runtimeQuestions);
                return;
            }

            if (questionBank == null || questionBank.Questions == null) return;
            foreach (var source in questionBank.Questions)
            {
                if (source != null) runtimeQuestions.Add(CloneQuestion(source));
            }
            if (shuffleQuestionOrder) Shuffle(runtimeQuestions);
        }

        MaterialContentData ResolveLastViewedContentWithQuiz()
        {
            if (contentLibrary == null || string.IsNullOrWhiteSpace(AppSession.LastViewedContentId))
            {
                return null;
            }

            string normalizedLastContent = MaterialContentKeyUtility.Normalize(AppSession.LastViewedContentId);
            for (int i = 0; i < contentLibrary.Items.Count; i++)
            {
                MaterialContentData content = contentLibrary.Items[i];
                if (content == null || content.NormalizedId != normalizedLastContent)
                {
                    continue;
                }

                return HasContentQuiz(content) ? content : null;
            }

            return null;
        }

        static bool HasContentQuiz(MaterialContentData content)
        {
            return content != null &&
                   content.QuizQuestions != null &&
                   content.QuizQuestions.Length > 0;
        }

        void BuildRuntimeQuestionsFromContent(MaterialContentData content)
        {
            int count = content.QuizQuestions.Length;
            for (int i = 0; i < count; i++)
            {
                string question = content.QuizQuestions[i];
                if (string.IsNullOrWhiteSpace(question))
                {
                    continue;
                }

                string answer = content.QuizAnswers != null && i < content.QuizAnswers.Length &&
                                !string.IsNullOrWhiteSpace(content.QuizAnswers[i])
                    ? content.QuizAnswers[i]
                    : content.Title;

                string[] options = BuildContentQuizOptions(content, answer, i);
                int correctIndex = 0;
                if (shuffleAnswerOrder && options.Length > 1)
                {
                    ShuffleOptions(options, ref correctIndex);
                }

                runtimeQuestions.Add(new RuntimeQuestion
                {
                    Question = question,
                    Options = options,
                    CorrectIndex = correctIndex,
                    Explanation = "Jawaban ini sesuai dengan materi " + content.Title + "."
                });
            }
        }

        static string[] BuildContentQuizOptions(MaterialContentData content, string answer, int questionIndex)
        {
            var options = new List<string>();
            AddUniqueOption(options, answer);

            AddWrongOptionsForQuestion(content, options, questionIndex);

            AddUniqueOption(options, "Kucing");
            AddUniqueOption(options, "Apel");
            AddUniqueOption(options, "Jeruk");
            AddUniqueOption(options, "Kupu-kupu");

            while (options.Count > 4)
            {
                options.RemoveAt(options.Count - 1);
            }

            return options.ToArray();
        }

        static void AddWrongOptionsForQuestion(MaterialContentData content, List<string> options, int questionIndex)
        {
            if (content.QuizWrongOptions == null) return;

            bool usedQuestionSpecific = false;
            if (questionIndex >= 0 && questionIndex < content.QuizWrongOptions.Length)
            {
                usedQuestionSpecific = AddSplitOptions(options, content.QuizWrongOptions[questionIndex]);
            }

            if (usedQuestionSpecific && options.Count >= 4) return;

            for (int i = 0; i < content.QuizWrongOptions.Length; i++)
            {
                if (usedQuestionSpecific && i == questionIndex) continue;
                AddSplitOptions(options, content.QuizWrongOptions[i]);
                if (options.Count >= 4) break;
            }
        }

        static bool AddSplitOptions(List<string> options, string raw)
        {
            bool added = false;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string[] parts = raw.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                int before = options.Count;
                AddUniqueOption(options, parts[i].Trim());
                added |= options.Count > before;
                if (options.Count >= 4) break;
            }

            return added;
        }

        static void AddUniqueOption(List<string> options, string option)
        {
            if (string.IsNullOrWhiteSpace(option))
            {
                return;
            }

            string normalized = MaterialContentKeyUtility.Normalize(option);
            for (int i = 0; i < options.Count; i++)
            {
                if (MaterialContentKeyUtility.Normalize(options[i]) == normalized)
                {
                    return;
                }
            }

            options.Add(option);
        }

        void AwardContentStarIfPassed()
        {
            if (activeContentQuiz == null || runtimeQuestions.Count == 0)
            {
                return;
            }

            bool passed = score >= Mathf.CeilToInt(runtimeQuestions.Count * 0.5f);
            if (passed)
            {
                AppSession.AwardStarForContent(activeContentQuiz.Id);
            }
        }

        RuntimeQuestion CloneQuestion(QuizQuestionData source)
        {
            string[] options = source.options != null ? (string[])source.options.Clone() : new string[0];
            int correctIndex = Mathf.Clamp(source.correctIndex, 0, Mathf.Max(0, options.Length - 1));
            if (shuffleAnswerOrder && options.Length > 1) ShuffleOptions(options, ref correctIndex);
            return new RuntimeQuestion
            {
                Question    = source.question,
                Options     = options,
                CorrectIndex = correctIndex,
                Explanation  = source.explanation
            };
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        static void ShuffleOptions(string[] options, ref int correctIndex)
        {
            for (int i = options.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                if (j == i) continue;
                string tmp = options[i]; options[i] = options[j]; options[j] = tmp;
                if (correctIndex == i) correctIndex = j;
                else if (correctIndex == j) correctIndex = i;
            }
        }
    }
}
