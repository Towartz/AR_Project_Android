using System.Collections.Generic;
using ARtiGraf.AR;
using ARtiGraf.Core;
using ARtiGraf.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    /// <summary>
    /// QuizController berbasis konten ScriptableObject MaterialContentData.
    /// Ambil konten dari AppSession.LastViewedContentId, tampilkan soal dari
    /// QuizQuestions + QuizAnswers + QuizWrongOptions yang ada di data.
    /// Jika data quiz kosong, fallback ke soal umum.
    /// </summary>
    public class QuizController : MonoBehaviour
    {
        [Header("UI Referensi")]
        [SerializeField] Text questionText;
        [SerializeField] Button[] answerButtons;
        [SerializeField] Text[] answerLabels;
        [SerializeField] Text scoreText;
        [SerializeField] Text progressText;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Text resultTitleText;
        [SerializeField] Text resultScoreText;
        [SerializeField] Text starAwardText;

        [Header("Navigator")]
        [SerializeField] SceneNavigationController navigator;

        [Header("Konten Library")]
        [SerializeField] MaterialContentLibrary library;
        const string DefaultLibraryResourceName = "ARtiGrafContentLibrary";

        struct QuizItem
        {
            public string Question;
            public string CorrectAnswer;
            public string[] Options;
        }

        List<QuizItem> quizItems = new List<QuizItem>();
        int currentIndex;
        int correctCount;
        MaterialContentData currentContent;

        void Start()
        {
            EnsureLibrary();
            BuildQuizFromLastContent();
            if (resultPanel != null) resultPanel.SetActive(false);
            ApplyReadableTextStyles();
            ShowQuestion();
        }

        void EnsureLibrary()
        {
            if (library == null)
                library = Resources.Load<MaterialContentLibrary>(DefaultLibraryResourceName);
        }

        void BuildQuizFromLastContent()
        {
            quizItems.Clear();
            currentContent = null;

            if (library != null && !string.IsNullOrEmpty(AppSession.LastViewedContentId))
            {
                for (int i = 0; i < library.Items.Count; i++)
                {
                    if (library.Items[i] != null &&
                        library.Items[i].NormalizedId == MaterialContentKeyUtility.Normalize(AppSession.LastViewedContentId))
                    {
                        currentContent = library.Items[i];
                        break;
                    }
                }
            }

            if (currentContent != null &&
                currentContent.QuizQuestions != null &&
                currentContent.QuizQuestions.Length > 0)
            {
                BuildQuizFromContentData(currentContent);
            }
            else
            {
                BuildFallbackQuiz();
            }

            Shuffle(quizItems);
        }

        void BuildQuizFromContentData(MaterialContentData content)
        {
            int count = content.QuizQuestions.Length;
            for (int i = 0; i < count; i++)
            {
                string question = content.QuizQuestions[i];
                string answer = content.QuizAnswers != null && i < content.QuizAnswers.Length
                    ? content.QuizAnswers[i]
                    : content.Title;
                var options = new List<string> { answer };
                AddWrongOptionsForQuestion(content, options, answer, i);

                while (options.Count < 2) options.Add("?");
                Shuffle(options);

                quizItems.Add(new QuizItem
                {
                    Question = question,
                    CorrectAnswer = answer,
                    Options = options.ToArray()
                });
            }
        }

        static void AddWrongOptionsForQuestion(MaterialContentData content, List<string> options, string answer, int questionIndex)
        {
            if (content.QuizWrongOptions == null) return;

            bool usedQuestionSpecific = false;
            if (questionIndex >= 0 && questionIndex < content.QuizWrongOptions.Length)
            {
                usedQuestionSpecific = AddSplitOptions(options, answer, content.QuizWrongOptions[questionIndex]);
            }

            if (usedQuestionSpecific && options.Count >= 4) return;

            for (int i = 0; i < content.QuizWrongOptions.Length; i++)
            {
                if (usedQuestionSpecific && i == questionIndex) continue;
                AddSplitOptions(options, answer, content.QuizWrongOptions[i]);
                if (options.Count >= 4) break;
            }
        }

        static bool AddSplitOptions(List<string> options, string answer, string raw)
        {
            bool added = false;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string[] parts = raw.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string option = parts[i].Trim();
                if (string.IsNullOrWhiteSpace(option) || option == answer || options.Contains(option))
                    continue;

                options.Add(option);
                added = true;
                if (options.Count >= 4) break;
            }

            return added;
        }

        void BuildFallbackQuiz()
        {
            string title = string.IsNullOrEmpty(AppSession.LastViewedContentTitle)
                ? "objek tadi" : AppSession.LastViewedContentTitle;

            quizItems.Add(new QuizItem
            {
                Question = "Apa nama objek yang baru kamu lihat?",
                CorrectAnswer = title,
                Options = new[] { title, "Mangga", "Kucing", "Kelinci" }
            });

            quizItems.Add(new QuizItem
            {
                Question = "Apakah kamu tadi melihat " + title + " di kamera AR?",
                CorrectAnswer = "Ya",
                Options = new[] { "Ya", "Tidak" }
            });
        }

        void ShowQuestion()
        {
            ApplyReadableTextStyles();
            if (quizItems.Count == 0)
            {
                ShowResult();
                return;
            }

            if (currentIndex >= quizItems.Count)
            {
                ShowResult();
                return;
            }

            QuizItem item = quizItems[currentIndex];
            if (questionText != null) questionText.text = item.Question;
            if (progressText != null)
                progressText.text = "Soal " + (currentIndex + 1) + " / " + quizItems.Count;
            if (scoreText != null) scoreText.text = "Benar: " + correctCount;

            if (answerButtons == null)
            {
                return;
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null)
                    continue;

                bool hasOption = i < item.Options.Length;
                answerButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption) continue;
                string option = item.Options[i];
                if (answerLabels != null && i < answerLabels.Length && answerLabels[i] != null)
                    answerLabels[i].text = option;

                // Capture untuk closure
                string capturedOption = option;
                string capturedAnswer = item.CorrectAnswer;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(capturedOption, capturedAnswer));
            }
        }

        void ApplyReadableTextStyles()
        {
            BuhenARTextStyle.Configure(questionText, 36, 23, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.08f);
            BuhenARTextStyle.Configure(scoreText, 26, 19, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(progressText, 26, 19, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(resultTitleText, 42, 26, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(resultScoreText, 32, 22, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(starAwardText, 26, 19, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);

            if (answerLabels == null) return;
            for (int i = 0; i < answerLabels.Length; i++)
                BuhenARTextStyle.Configure(answerLabels[i], 30, 21, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.05f);
        }

        void OnAnswerSelected(string selected, string correct)
        {
            bool isCorrect = selected == correct;
            if (isCorrect) correctCount++;
            currentIndex++;
            ShowQuestion();
        }

        void ShowResult()
        {
            if (resultPanel != null) resultPanel.SetActive(true);

            AppSession.StoreQuizResult(correctCount, quizItems.Count);
            bool passed = quizItems.Count > 0 && correctCount >= Mathf.CeilToInt(quizItems.Count * 0.5f);

            if (currentContent != null)
                ARLearningFeedbackController.AwardStarIfPassed(currentContent.Id, passed);

            if (resultTitleText != null)
                resultTitleText.text = passed ? "Bagus sekali!" : "Coba lagi yuk!";

            if (resultScoreText != null)
                resultScoreText.text = correctCount + " / " + quizItems.Count + " benar";

            if (starAwardText != null)
                starAwardText.text = passed && currentContent != null
                    ? "Kamu dapat bintang untuk " + currentContent.Title + "!"
                    : string.Empty;
        }

        public void OnPlayAgainPressed()
        {
            if (navigator != null) navigator.RestartQuiz();
        }

        public void OnBackToARPressed()
        {
            if (navigator != null) navigator.GoBack();
        }

        public void OnMainMenuPressed()
        {
            if (navigator != null) navigator.OpenMaterialSelect();
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

        static void Shuffle(List<QuizItem> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                QuizItem tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
