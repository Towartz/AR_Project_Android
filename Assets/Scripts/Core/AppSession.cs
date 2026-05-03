using System.Collections.Generic;
using ARtiGraf.Data;
using UnityEngine;

namespace ARtiGraf.Core
{
    /// <summary>
    /// Menyimpan state runtime sesi aplikasi dan data progress persisten via PlayerPrefs.
    /// </summary>
    public static class AppSession
    {
        // ── Runtime-only state ────────────────────────────────────────────────
        static readonly HashSet<string> ViewedContentIds  = new HashSet<string>();
        static readonly HashSet<string> ViewedFruitIds    = new HashSet<string>();
        static readonly HashSet<string> ViewedAnimalIds   = new HashSet<string>();

        // ── PlayerPrefs keys ──────────────────────────────────────────────────
        const string KeyHighScore        = "ARtiGraf_HighScore";
        const string KeyHighScoreTotal   = "ARtiGraf_HighScoreTotal";
        const string KeyTotalQuizPlayed  = "ARtiGraf_TotalQuizPlayed";
        const string KeyTotalCorrect     = "ARtiGraf_TotalCorrect";
        const string KeyBestStreak       = "ARtiGraf_BestStreak";
        const string KeyTotalScansAll    = "ARtiGraf_TotalScansAll";
        const string KeyStarPrefix       = "ARtiGraf_Star_";
        const string KeyDiscoveredPrefix = "ARtiGraf_Discovered_";

        // ── Public accessors ──────────────────────────────────────────────────
        public static LearningCategory? SelectedCategory       { get; private set; }
        public static int LastQuizScore                        { get; private set; }
        public static int LastQuizTotal                        { get; private set; }
        public static int TotalScansThisRun                    { get; private set; }
        public static string LastViewedContentId               { get; private set; }
        public static string LastViewedContentTitle            { get; private set; }
        public static int UniqueViewedContentCount             => ViewedContentIds.Count;

        // Quiz Hunt state
        public static string QuizHuntTargetContentId           { get; private set; }
        public static bool IsQuizHuntActive                    => !string.IsNullOrEmpty(QuizHuntTargetContentId);
        public static string QuizHuntPendingScanContentId      { get; private set; }
        public static bool HasQuizHuntPendingScanResult        => !string.IsNullOrEmpty(QuizHuntPendingScanContentId);
        public static IReadOnlyList<string> QuizHuntSessionQuestionIds => QuizHuntSessionIds;
        public static int QuizHuntSessionCurrentIndex          { get; private set; }
        public static int QuizHuntSessionCorrectCount          { get; private set; }
        public static int QuizHuntSessionStarsEarned           { get; private set; }
        public static int QuizHuntSessionScore                 { get; private set; }
        public static int QuizHuntSessionStreak                { get; private set; }
        public static bool HasQuizHuntSession                  => QuizHuntSessionIds.Count > 0;

        static readonly List<string> QuizHuntSessionIds = new List<string>();

        // Persisted stats
        public static int HighScore          => PlayerPrefs.GetInt(KeyHighScore, 0);
        public static int HighScoreTotal     => PlayerPrefs.GetInt(KeyHighScoreTotal, 0);
        public static int TotalQuizPlayed    => PlayerPrefs.GetInt(KeyTotalQuizPlayed, 0);
        public static int TotalCorrectAllTime => PlayerPrefs.GetInt(KeyTotalCorrect, 0);
        public static int BestStreak         => PlayerPrefs.GetInt(KeyBestStreak, 0);
        public static int TotalScansAllTime  => PlayerPrefs.GetInt(KeyTotalScansAll, 0);

        // Derived
        public static int LastQuizPercentage =>
            LastQuizTotal > 0 ? Mathf.RoundToInt((LastQuizScore / (float)LastQuizTotal) * 100f) : 0;
        public static bool LastQuizIsNewHighScore { get; private set; }

        // ── Category ──────────────────────────────────────────────────────────
        public static void SelectCategory(LearningCategory category) => SelectedCategory = category;
        public static void ClearCategory()                            => SelectedCategory = null;

        // ── Quiz Hunt target ──────────────────────────────────────────────────
        public static void SetQuizHuntTarget(string contentId) => QuizHuntTargetContentId = contentId;
        public static void ClearQuizHuntTarget()               => QuizHuntTargetContentId = string.Empty;
        public static void SetQuizHuntScanResult(string contentId) => QuizHuntPendingScanContentId = contentId;
        public static bool TryConsumeQuizHuntScanResult(out string contentId)
        {
            contentId = QuizHuntPendingScanContentId;
            QuizHuntPendingScanContentId = string.Empty;
            return !string.IsNullOrEmpty(contentId);
        }

        public static void StartQuizHuntSession(IReadOnlyList<string> questionIds)
        {
            QuizHuntSessionIds.Clear();
            if (questionIds != null)
            {
                for (int i = 0; i < questionIds.Count; i++)
                {
                    string normalized = MaterialContentKeyUtility.Normalize(questionIds[i]);
                    if (!string.IsNullOrEmpty(normalized))
                        QuizHuntSessionIds.Add(normalized);
                }
            }

            QuizHuntSessionCurrentIndex = 0;
            QuizHuntSessionCorrectCount = 0;
            QuizHuntSessionStarsEarned = 0;
            QuizHuntSessionScore = 0;
            QuizHuntSessionStreak = 0;
        }

        public static void StoreQuizHuntProgress(int currentIndex, int correctCount, int starsEarned, int score, int streak)
        {
            QuizHuntSessionCurrentIndex = Mathf.Max(0, currentIndex);
            QuizHuntSessionCorrectCount = Mathf.Max(0, correctCount);
            QuizHuntSessionStarsEarned = Mathf.Max(0, starsEarned);
            QuizHuntSessionScore = Mathf.Max(0, score);
            QuizHuntSessionStreak = Mathf.Max(0, streak);
            UpdateStreak(QuizHuntSessionStreak);
        }

        public static void ClearQuizHuntSession()
        {
            QuizHuntSessionIds.Clear();
            QuizHuntSessionCurrentIndex = 0;
            QuizHuntSessionCorrectCount = 0;
            QuizHuntSessionStarsEarned = 0;
            QuizHuntSessionScore = 0;
            QuizHuntSessionStreak = 0;
            QuizHuntPendingScanContentId = string.Empty;
            ClearQuizHuntTarget();
        }

        // ── Quiz result ───────────────────────────────────────────────────────
        public static void StoreQuizResult(int score, int total)
        {
            LastQuizScore = score;
            LastQuizTotal = total;
            LastQuizIsNewHighScore = false;

            // Update persistent counters
            PlayerPrefs.SetInt(KeyTotalQuizPlayed, TotalQuizPlayed + 1);
            PlayerPrefs.SetInt(KeyTotalCorrect,    TotalCorrectAllTime + score);

            // High score: compare percentage to be fair across question-count changes
            int prevHigh  = HighScore;
            int prevTotal = HighScoreTotal;
            float prevPct = prevTotal > 0 ? prevHigh / (float)prevTotal : -1f;
            float newPct  = total   > 0 ? score   / (float)total       :  0f;

            if (total > 0 && newPct >= prevPct)
            {
                PlayerPrefs.SetInt(KeyHighScore,      score);
                PlayerPrefs.SetInt(KeyHighScoreTotal, total);
                if (newPct > prevPct || (newPct == prevPct && score > prevHigh))
                {
                    LastQuizIsNewHighScore = true;
                }
            }

            PlayerPrefs.Save();
        }

        // ── Streak ────────────────────────────────────────────────────────────
        public static void UpdateStreak(int streak)
        {
            if (streak > BestStreak)
            {
                PlayerPrefs.SetInt(KeyBestStreak, streak);
                PlayerPrefs.Save();
            }
        }

        // ── Content scan ──────────────────────────────────────────────────────
        public static void RecordViewedContent(MaterialContentData content)
        {
            if (content == null || content.IsDemoContent) return;

            if (LastViewedContentId != content.Id) TotalScansThisRun++;

            LastViewedContentId    = content.Id;
            LastViewedContentTitle = content.Title;
            ViewedContentIds.Add(content.NormalizedId);

            // Persist total scan count across sessions
            PlayerPrefs.SetInt(KeyTotalScansAll, TotalScansAllTime + 1);
            PlayerPrefs.Save();

            if (content.Category == LearningCategory.Typography)
            {
                ViewedFruitIds.Add(content.NormalizedId);
                return;
            }

            ViewedAnimalIds.Add(content.NormalizedId);
        }

        public static bool IsContentDiscovered(string contentId)
        {
            string key = BuildDiscoveredKey(contentId);
            return !string.IsNullOrEmpty(key) && PlayerPrefs.GetInt(key, 0) == 1;
        }

        public static bool RecordDiscoveredContent(MaterialContentData content)
        {
            if (content == null || content.IsDemoContent) return false;

            string key = BuildDiscoveredKey(content.Id);
            if (string.IsNullOrEmpty(key)) return false;

            bool isNew = PlayerPrefs.GetInt(key, 0) != 1;
            if (isNew)
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }

            return isNew;
        }

        public static int GetDiscoveredContentCount(LearningCategory? category)
        {
            if (!category.HasValue) return UniqueViewedContentCount;
            return category.Value == LearningCategory.Typography
                ? ViewedFruitIds.Count
                : ViewedAnimalIds.Count;
        }

        // ── Labels ────────────────────────────────────────────────────────────
        public static string GetSelectedCategoryLabel() =>
            SelectedCategory.HasValue ? GetCategoryLabel(SelectedCategory.Value) : "Semua Materi";

        public static string GetCategoryLabel(LearningCategory category) =>
            category == LearningCategory.Typography ? "Buah/Sayur" : "Hewan";

        // ── Reset ─────────────────────────────────────────────────────────────
        /// <summary>Hapus semua data persisten (untuk debug / tombol reset di settings).</summary>
        public static void ResetAllPersistentData()
        {
            PlayerPrefs.DeleteKey(KeyHighScore);
            PlayerPrefs.DeleteKey(KeyHighScoreTotal);
            PlayerPrefs.DeleteKey(KeyTotalQuizPlayed);
            PlayerPrefs.DeleteKey(KeyTotalCorrect);
            PlayerPrefs.DeleteKey(KeyBestStreak);
            PlayerPrefs.DeleteKey(KeyTotalScansAll);
            PlayerPrefs.Save();
        }

        // ── Star / Bintang per konten ─────────────────────────────────────────
        public static bool HasStarForContent(string contentId)
        {
            string key = BuildStarKey(contentId);
            if (string.IsNullOrEmpty(key)) return false;
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        public static void AwardStarForContent(string contentId)
        {
            string key = BuildStarKey(contentId);
            if (string.IsNullOrEmpty(key)) return;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        public static int GetTotalStarsEarned(MaterialContentLibrary library)
        {
            if (library == null) return 0;
            int count = 0;
            for (int i = 0; i < library.Items.Count; i++)
            {
                MaterialContentData item = library.Items[i];
                if (item != null && HasStarForContent(item.Id)) count++;
            }
            return count;
        }

        static string BuildStarKey(string contentId)
        {
            string normalizedId = MaterialContentKeyUtility.Normalize(contentId);
            return string.IsNullOrEmpty(normalizedId) ? string.Empty : KeyStarPrefix + normalizedId;
        }

        static string BuildDiscoveredKey(string contentId)
        {
            string normalizedId = MaterialContentKeyUtility.Normalize(contentId);
            return string.IsNullOrEmpty(normalizedId) ? string.Empty : KeyDiscoveredPrefix + normalizedId;
        }
    }
}
