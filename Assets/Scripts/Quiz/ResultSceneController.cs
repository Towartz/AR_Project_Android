using ARtiGraf.Core;
using ARtiGraf.UI;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ARtiGraf.Quiz
{
    public class ResultSceneController : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] Text scoreText;
        [SerializeField] Text percentageText;
        [SerializeField] Text summaryText;
        [SerializeField] Text feedbackText;
        [SerializeField] Text nextStepText;
        [SerializeField] Text newRecordLabel;

        [Header("Stats Panel")]
        [SerializeField] Text highScoreText;
        [SerializeField] Text bestStreakText;
        [SerializeField] Text totalQuizPlayedText;
        [SerializeField] Text totalScansText;

        [Header("Animation")]
        [SerializeField] float scoreCountUpDuration = 1.2f;

        void Start()
        {
            ApplyReadableTextStyles();
            int score        = AppSession.LastQuizScore;
            int total        = AppSession.LastQuizTotal;
            int percentage   = AppSession.LastQuizPercentage;
            int discovered   = AppSession.GetDiscoveredContentCount(AppSession.SelectedCategory);

            PopulateScoreSection(score, total, percentage, discovered);
            PopulateStatsPanel();
            PopulateFeedback(score, total, percentage);
            PopulateNextStep(percentage);

            if (scoreCountUpDuration > 0f && total > 0)
                StartCoroutine(AnimateScoreCount(score, total));
        }

        void ApplyReadableTextStyles()
        {
            BuhenARTextStyle.Configure(scoreText, 62, 32, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(percentageText, 36, 24, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(summaryText, 30, 21, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(feedbackText, 27, 20, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(nextStepText, 25, 19, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(newRecordLabel, 27, 20, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.04f);
            BuhenARTextStyle.Configure(highScoreText, 23, 18, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.02f);
            BuhenARTextStyle.Configure(bestStreakText, 23, 18, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.02f);
            BuhenARTextStyle.Configure(totalQuizPlayedText, 23, 18, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.02f);
            BuhenARTextStyle.Configure(totalScansText, 23, 18, TextAnchor.MiddleCenter, VerticalWrapMode.Truncate, 1.02f);
        }

        // ── Score section ─────────────────────────────────────────────────────
        void PopulateScoreSection(int score, int total, int percentage, int discovered)
        {
            if (scoreText != null)
                scoreText.text = score + " / " + total;

            if (percentageText != null)
                percentageText.text = total > 0 ? percentage + "%" : "-";

            if (summaryText != null)
                summaryText.text = total == 0
                    ? discovered + " materi sudah dibuka"
                    : percentage + "% benar  •  " + discovered + " materi dibuka";

            if (newRecordLabel != null)
                newRecordLabel.gameObject.SetActive(AppSession.LastQuizIsNewHighScore && total > 0);
        }

        // ── Stats panel ───────────────────────────────────────────────────────
        void PopulateStatsPanel()
        {
            int   hs      = AppSession.HighScore;
            int   hsTotal = AppSession.HighScoreTotal;
            float hsPct   = hsTotal > 0 ? Mathf.RoundToInt(hs / (float)hsTotal * 100f) : 0f;

            if (highScoreText != null)
                highScoreText.text = hsTotal > 0
                    ? "Rekor: " + hs + "/" + hsTotal + "  (" + hsPct + "%)"
                    : "Rekor: -";

            if (bestStreakText != null)
                bestStreakText.text = "Streak terbaik: " + AppSession.BestStreak + "x";

            if (totalQuizPlayedText != null)
                totalQuizPlayedText.text = "Quiz dimainkan: " + AppSession.TotalQuizPlayed + "x";

            if (totalScansText != null)
                totalScansText.text = "Total scan: " + AppSession.TotalScansAllTime;
        }

        // ── Feedback text ─────────────────────────────────────────────────────
        void PopulateFeedback(int score, int total, int percentage)
        {
            if (feedbackText == null) return;

            if (total == 0)
            {
                feedbackText.text = "Quiz belum dijalankan.";
                return;
            }

            if (AppSession.LastQuizIsNewHighScore)
                feedbackText.text = "Rekor baru! Kamu melampaui skor terbaikmu sebelumnya. Terus pertahankan.";
            else if (percentage >= 90)
                feedbackText.text = "Pemahamanmu sudah sangat kuat. Demo AR siap ditampilkan dengan percaya diri.";
            else if (percentage >= 70)
                feedbackText.text = "Hasil cukup baik. Ulangi sekali lagi agar penjelasan materi lebih mantap.";
            else if (percentage >= 50)
                feedbackText.text = "Setengah jalan. Review materi yang terlewat sebelum demo UJIKOM.";
            else
                feedbackText.text = "Perlu review lebih dalam sebelum demo. Kembali ke scan AR dan pelajari ulang.";
        }

        // ── Next step CTA ─────────────────────────────────────────────────────
        void PopulateNextStep(int percentage)
        {
            if (nextStepText == null) return;

            int   total       = AppSession.LastQuizTotal;
            string category   = AppSession.GetSelectedCategoryLabel().ToLowerInvariant();
            string lastContent = string.IsNullOrWhiteSpace(AppSession.LastViewedContentTitle)
                ? category : AppSession.LastViewedContentTitle;

            if (total == 0)
            {
                nextStepText.text = "Mulai dari menu materi, lalu scan marker untuk membuka pembelajaran.";
                return;
            }

            if (percentage >= 90)
                nextStepText.text = "Coba tantang dirimu dengan kategori lain, atau scan ulang marker "
                                  + lastContent + " untuk review visual.";
            else if (percentage >= 70)
                nextStepText.text = "Review cepat materi " + lastContent
                                  + ", lalu ulangi quiz untuk memastikan skor stabil.";
            else if (percentage >= 50)
                nextStepText.text = "Kembali ke AR scan untuk jalur " + category
                                  + " dan fokus pada materi yang belum dikuasai.";
            else
                nextStepText.text = "Sebaiknya scan semua marker " + category
                                  + " terlebih dahulu sebelum mencoba quiz lagi.";
        }

        // ── Score count-up animation ──────────────────────────────────────────
        IEnumerator AnimateScoreCount(int targetScore, int total)
        {
            if (scoreText == null) yield break;
            float elapsed = 0f;
            while (elapsed < scoreCountUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scoreCountUpDuration);
                // Ease out cubic
                float smooth = 1f - Mathf.Pow(1f - t, 3f);
                int displayed = Mathf.RoundToInt(smooth * targetScore);
                scoreText.text = displayed + " / " + total;
                yield return null;
            }
            scoreText.text = targetScore + " / " + total;
        }
    }
}
