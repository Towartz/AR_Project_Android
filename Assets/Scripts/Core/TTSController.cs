using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARtiGraf.Core
{
    /// <summary>
    /// Text-To-Speech controller untuk BuhenAR.
    /// Mendukung dua mode:
    /// 1. Ejaan huruf: A - P - E - L (satu huruf per AudioClip dari Resources).
    /// 2. Cara baca: memutar AudioClip nama lengkap dari MaterialContentData.NameAudioClip.
    ///
    /// Fallback: jika tidak ada AudioClip, gunakan Android TTS native via AndroidJavaObject.
    /// </summary>
    public class TTSController : MonoBehaviour
    {
        static TTSController instance;
        public static TTSController Instance => instance;

        [Header("Audio Source")]
        [SerializeField] AudioSource ttsAudioSource;

        [Header("Ejaan Huruf")]
        [Tooltip("Folder di Resources/ yang berisi AudioClip A.mp3, B.mp3, ... Z.mp3 dan 0.mp3-9.mp3")]
        [SerializeField] string letterClipsFolder = "TTS/Letters";

        [Header("Jeda antar huruf (detik)")]
        [SerializeField] float letterPauseSeconds = 0.25f;

        [Header("Jeda antara ejaan dan cara baca (detik)")]
        [SerializeField] float spellToWordPauseSeconds = 0.5f;

        [Header("Gunakan Android TTS Native sebagai fallback")]
        [SerializeField] bool useAndroidTTSFallback = false;
        [SerializeField] bool preferNativeSingleUtteranceWhenClipsMissing = true;
        [Range(0.1f, 2f)]
        [SerializeField] float nativeSpeechRate = 0.88f;
        [Range(0.1f, 2f)]
        [SerializeField] float nativePitch = 1.08f;

        [Header("Fallback AudioClip Lokal")]
        [Tooltip("Aktifkan hanya kalau folder Resources/TTS sudah berisi rekaman suara manusia. Default mati agar fallback sintetis tidak terdengar aneh saat demo.")]
        [SerializeField] bool useResourceAudioClipFallback = true;
        [Tooltip("Folder di Resources/ untuk clip kata lengkap. Contoh: TTS/Words/apel.mp3")]
        [SerializeField] string wordClipsFolder = "TTS/Words";
        [Tooltip("Fallback nada sintetis darurat. Default mati karena tidak cocok untuk demo anak-anak.")]
        [SerializeField] bool useSyntheticAudioClipFallback = false;
        [SerializeField, Range(0f, 1f)] float ttsVolume = 1f;

        readonly Dictionary<char, AudioClip> letterCache = new Dictionary<char, AudioClip>();
        readonly Dictionary<string, AudioClip> wordCache = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> syntheticCache = new Dictionary<string, AudioClip>();
        Coroutine activeRoutine;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass androidTTSBridge;
        bool androidTTSReady;
#endif

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            NormalizeRuntimeSettings();

            if (ttsAudioSource == null)
                ttsAudioSource = gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource();
            PreloadLetterClips();
            InitAndroidTTS();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            ShutdownAndroidTTS();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Eja huruf kata lalu baca nama lengkap. Contoh: A-P-E-L ... Apel</summary>
        public void SpellThenSpeak(string word, AudioClip wordClip)
        {
            SpellThenSpeak(word, word, wordClip);
        }

        /// <summary>Eja dari spellWord, lalu baca spokenWord. Berguna jika spellOverride berisi A-P-E-L.</summary>
        public void SpellThenSpeak(string spellWord, string spokenWord, AudioClip wordClip)
        {
            StopSpeaking();
            activeRoutine = StartCoroutine(SpellThenSpeakRoutine(spellWord, spokenWord, wordClip));
        }

        /// <summary>Hanya eja huruf saja. Contoh: A-P-E-L</summary>
        public void SpellOnly(string word)
        {
            StopSpeaking();
            activeRoutine = StartCoroutine(SpellRoutine(word, null));
        }

        /// <summary>Hanya putar AudioClip nama lengkap.</summary>
        public void SpeakWord(AudioClip wordClip, string fallbackText = null)
        {
            StopSpeaking();
            activeRoutine = StartCoroutine(SpeakWordRoutine(wordClip, fallbackText));
        }

        public void StopSpeaking()
        {
            if (activeRoutine != null) { StopCoroutine(activeRoutine); activeRoutine = null; }
            if (ttsAudioSource != null) ttsAudioSource.Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                androidTTSBridge?.CallStatic("stop");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TTS] Stop native gagal: " + ex.Message);
            }
#endif
        }

        public bool IsSpeaking => activeRoutine != null;

        // ── Coroutines ────────────────────────────────────────────────────────

        IEnumerator SpellThenSpeakRoutine(string spellWord, string spokenWord, AudioClip wordClip)
        {
            if (string.IsNullOrWhiteSpace(spellWord))
                spellWord = spokenWord;
            if (string.IsNullOrWhiteSpace(spokenWord))
                spokenWord = StripSpellSeparators(spellWord);

            // Demo memakai rekaman suara manusia per kata. Kalau belum ada clip huruf A-Z,
            // jangan tunggu ejaan kosong; langsung putar nama objek agar feedback terasa responsif.
            if (!useAndroidTTSFallback && !useSyntheticAudioClipFallback && !HasAnyLetterClip(spellWord))
            {
                yield return SpeakWordRoutine(wordClip, spokenWord);
                activeRoutine = null;
                yield break;
            }

            bool useNativeForSpelling = ShouldUseNativeForSpelling(spellWord);
            if (useNativeForSpelling && wordClip == null && preferNativeSingleUtteranceWhenClipsMissing)
            {
                SpeakNative(BuildNativeSpellThenSpeakPhrase(spellWord, spokenWord));
                yield return new WaitForSecondsRealtime(EstimateNativeDuration(spellWord, spokenWord));
                activeRoutine = null;
                yield break;
            }

            if (useNativeForSpelling)
            {
                SpeakNative(BuildNativeSpellPhrase(spellWord));
                yield return new WaitForSecondsRealtime(EstimateNativeSpellDuration(spellWord));
            }
            else
            {
                yield return SpellRoutine(spellWord, null);
                yield return new WaitForSecondsRealtime(spellToWordPauseSeconds);
            }

            yield return new WaitForSecondsRealtime(spellToWordPauseSeconds);
            yield return SpeakWordRoutine(wordClip, spokenWord);
            activeRoutine = null;
        }

        IEnumerator SpellRoutine(string word, AudioClip _)
        {
            if (string.IsNullOrWhiteSpace(word)) yield break;
            if (ShouldUseNativeForSpelling(word))
            {
                SpeakNative(BuildNativeSpellPhrase(word));
                yield return new WaitForSecondsRealtime(EstimateNativeSpellDuration(word));
                activeRoutine = null;
                yield break;
            }
            if (!useSyntheticAudioClipFallback && !useAndroidTTSFallback && !HasAnyLetterClip(word))
            {
                yield break;
            }

            string upper = word.ToUpper().Trim();
            foreach (char c in upper)
            {
                if (c == ' ' || c == '-')
                {
                    yield return new WaitForSecondsRealtime(letterPauseSeconds * 2f);
                    continue;
                }

                AudioClip clip = GetLetterClip(c);
                if (clip != null)
                {
                    ttsAudioSource.PlayOneShot(clip);
                    yield return new WaitForSecondsRealtime(clip.length + letterPauseSeconds);
                }
                else if (useSyntheticAudioClipFallback)
                {
                    AudioClip syntheticClip = GetSyntheticLetterClip(c);
                    ttsAudioSource.PlayOneShot(syntheticClip);
                    yield return new WaitForSecondsRealtime(syntheticClip.length + letterPauseSeconds);
                }
                else if (useAndroidTTSFallback)
                {
                    SpeakNative(c.ToString());
                    yield return new WaitForSecondsRealtime(0.45f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(letterPauseSeconds);
                }
            }
        }

        IEnumerator SpeakWordRoutine(AudioClip wordClip, string fallbackText)
        {
            AudioClip clip = wordClip != null ? wordClip : GetWordClip(fallbackText);
            if (clip != null)
            {
                ttsAudioSource.PlayOneShot(clip);
                yield return new WaitForSecondsRealtime(clip.length + 0.1f);
            }
            else if (useSyntheticAudioClipFallback && !string.IsNullOrWhiteSpace(fallbackText))
            {
                yield return PlaySyntheticWordRoutine(fallbackText);
            }
            else if (useAndroidTTSFallback && !string.IsNullOrWhiteSpace(fallbackText))
            {
                SpeakNative(fallbackText);
                yield return new WaitForSecondsRealtime(Mathf.Max(1f, fallbackText.Length * 0.1f));
            }

            activeRoutine = null;
        }

        // ── Letter clip cache ─────────────────────────────────────────────────

        void ConfigureAudioSource()
        {
            if (ttsAudioSource == null) return;
            ttsAudioSource.playOnAwake = false;
            ttsAudioSource.loop = false;
            ttsAudioSource.mute = false;
            ttsAudioSource.volume = ttsVolume;
            ttsAudioSource.spatialBlend = 0f;
            ttsAudioSource.ignoreListenerPause = true;
        }

        void NormalizeRuntimeSettings()
        {
            if (string.IsNullOrWhiteSpace(letterClipsFolder))
                letterClipsFolder = "TTS/Letters";
            if (string.IsNullOrWhiteSpace(wordClipsFolder))
                wordClipsFolder = "TTS/Words";

            // Paksa mode aman untuk presentasi: pakai rekaman AudioClip manusia,
            // bukan Android TTS/sintetis yang sebelumnya terdengar aneh atau silent.
            useResourceAudioClipFallback = true;
            useAndroidTTSFallback = false;
            preferNativeSingleUtteranceWhenClipsMissing = false;
            useSyntheticAudioClipFallback = false;
            if (ttsVolume <= 0f) ttsVolume = 1f;
        }

        void PreloadLetterClips()
        {
            if (!useResourceAudioClipFallback) return;

            for (char c = 'A'; c <= 'Z'; c++)
                TryLoadLetter(c);
            for (char c = '0'; c <= '9'; c++)
                TryLoadLetter(c);
        }

        void TryLoadLetter(char c)
        {
            if (letterCache.ContainsKey(c)) return;
            AudioClip clip = Resources.Load<AudioClip>(letterClipsFolder + "/" + c);
            if (clip != null) letterCache[c] = clip;
        }

        AudioClip GetLetterClip(char c)
        {
            if (!useResourceAudioClipFallback) return null;

            char upper = char.ToUpper(c);
            if (letterCache.TryGetValue(upper, out AudioClip clip)) return clip;
            TryLoadLetter(upper);
            return letterCache.TryGetValue(upper, out clip) ? clip : null;
        }

        AudioClip GetWordClip(string word)
        {
            if (!useResourceAudioClipFallback) return null;
            if (string.IsNullOrWhiteSpace(word)) return null;

            string key = NormalizeResourceKey(word);
            if (wordCache.TryGetValue(key, out AudioClip cached)) return cached;

            string[] candidates =
            {
                word.Trim(),
                key,
                key.ToUpperInvariant(),
                char.ToUpperInvariant(key[0]) + (key.Length > 1 ? key.Substring(1) : string.Empty)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(candidates[i])) continue;
                AudioClip clip = Resources.Load<AudioClip>(wordClipsFolder + "/" + candidates[i]);
                if (clip != null)
                {
                    wordCache[key] = clip;
                    return clip;
                }
            }

            wordCache[key] = null;
            return null;
        }

        AudioClip GetSyntheticLetterClip(char c)
        {
            char upper = char.ToUpperInvariant(c);
            string key = "letter_" + upper;
            if (syntheticCache.TryGetValue(key, out AudioClip clip)) return clip;

            float frequency = 360f + (Mathf.Abs(upper) % 18) * 38f;
            clip = CreateToneClip(key, frequency, 0.22f, 0.82f);
            syntheticCache[key] = clip;
            return clip;
        }

        IEnumerator PlaySyntheticWordRoutine(string word)
        {
            string key = NormalizeResourceKey(word);
            if (string.IsNullOrEmpty(key)) yield break;

            // Fallback ini sengaja berupa AudioClip sintetis, bukan native TTS,
            // agar demo tetap terdengar di HP yang tidak punya engine TTS.
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (!char.IsLetterOrDigit(c)) continue;
                AudioClip clip = GetSyntheticLetterClip(c);
                ttsAudioSource.PlayOneShot(clip, 0.72f);
                yield return new WaitForSecondsRealtime(0.085f);
            }

            AudioClip endClip = GetSyntheticWordEndClip(key);
            ttsAudioSource.PlayOneShot(endClip, 0.95f);
            yield return new WaitForSecondsRealtime(endClip.length + 0.1f);
        }

        AudioClip GetSyntheticWordEndClip(string key)
        {
            string cacheKey = "word_" + key;
            if (syntheticCache.TryGetValue(cacheKey, out AudioClip clip)) return clip;

            float frequency = 520f + (Mathf.Abs(key.GetHashCode()) % 220);
            clip = CreateToneClip(cacheKey, frequency, 0.42f, 0.72f);
            syntheticCache[cacheKey] = clip;
            return clip;
        }

        static AudioClip CreateToneClip(string name, float frequency, float durationSeconds, float volume)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
            float[] data = new float[sampleCount];
            float phase = 0f;
            float increment = 2f * Mathf.PI * frequency / sampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                float envelope = Mathf.Sin(Mathf.PI * t);
                float vibrato = Mathf.Sin(2f * Mathf.PI * 5f * t) * 0.018f;
                data[i] = Mathf.Sin(phase + vibrato) * envelope * volume;
                phase += increment;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        bool ShouldUseNativeForSpelling(string word)
        {
            return useAndroidTTSFallback &&
                   preferNativeSingleUtteranceWhenClipsMissing &&
                   !string.IsNullOrWhiteSpace(word) &&
                   !HasAllLetterClips(word);
        }

        bool HasAllLetterClips(string word)
        {
            bool hasSpokenCharacter = false;
            foreach (char raw in word)
            {
                char c = char.ToUpper(raw);
                if (!char.IsLetterOrDigit(c)) continue;

                hasSpokenCharacter = true;
                if (GetLetterClip(c) == null) return false;
            }

            return hasSpokenCharacter;
        }

        bool HasAnyLetterClip(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return false;
            foreach (char raw in word)
            {
                char c = char.ToUpper(raw);
                if (!char.IsLetterOrDigit(c)) continue;
                if (GetLetterClip(c) != null) return true;
            }

            return false;
        }

        static string StripSpellSeparators(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
        }

        static string NormalizeResourceKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string trimmed = StripSpellSeparators(value).Trim().ToLowerInvariant();
            var chars = new List<char>(trimmed.Length);
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (char.IsLetterOrDigit(c)) chars.Add(c);
                else if (char.IsWhiteSpace(c) || c == '-' || c == '_') chars.Add('_');
            }

            return new string(chars.ToArray()).Trim('_');
        }

        static string BuildNativeSpellThenSpeakPhrase(string spellWord, string spokenWord)
        {
            string spellPhrase = BuildNativeSpellPhrase(spellWord);
            string spoken = string.IsNullOrWhiteSpace(spokenWord) ? StripSpellSeparators(spellWord) : spokenWord.Trim();
            return string.IsNullOrWhiteSpace(spoken) ? spellPhrase : spellPhrase + " " + spoken + ".";
        }

        static string BuildNativeSpellPhrase(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return string.Empty;

            var parts = new List<string>();
            foreach (char raw in word.ToUpperInvariant())
            {
                if (raw == ' ' || raw == '-')
                {
                    continue;
                }

                parts.Add(GetIndonesianCharacterName(raw));
            }

            return string.Join(". ", parts) + ".";
        }

        static string GetIndonesianCharacterName(char c)
        {
            switch (c)
            {
                case 'A': return "A";
                case 'B': return "Be";
                case 'C': return "Ce";
                case 'D': return "De";
                case 'E': return "E";
                case 'F': return "Ef";
                case 'G': return "Ge";
                case 'H': return "Ha";
                case 'I': return "I";
                case 'J': return "Je";
                case 'K': return "Ka";
                case 'L': return "El";
                case 'M': return "Em";
                case 'N': return "En";
                case 'O': return "O";
                case 'P': return "Pe";
                case 'Q': return "Ki";
                case 'R': return "Er";
                case 'S': return "Es";
                case 'T': return "Te";
                case 'U': return "U";
                case 'V': return "Ve";
                case 'W': return "We";
                case 'X': return "Eks";
                case 'Y': return "Ye";
                case 'Z': return "Zet";
                case '0': return "Nol";
                case '1': return "Satu";
                case '2': return "Dua";
                case '3': return "Tiga";
                case '4': return "Empat";
                case '5': return "Lima";
                case '6': return "Enam";
                case '7': return "Tujuh";
                case '8': return "Delapan";
                case '9': return "Sembilan";
                default: return c.ToString();
            }
        }

        static int CountSpokenCharacters(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return 0;
            int count = 0;
            foreach (char c in word)
            {
                if (char.IsLetterOrDigit(c)) count++;
            }
            return count;
        }

        static float EstimateNativeSpellDuration(string spellWord)
        {
            return Mathf.Clamp(CountSpokenCharacters(spellWord) * 0.42f, 0.65f, 6f);
        }

        static float EstimateNativeDuration(string spellWord, string spokenWord)
        {
            float spellDuration = EstimateNativeSpellDuration(spellWord);
            float wordDuration = string.IsNullOrWhiteSpace(spokenWord) ? 0.8f : Mathf.Clamp(spokenWord.Length * 0.09f, 0.8f, 2.4f);
            return Mathf.Clamp(spellDuration + wordDuration + 0.4f, 1.2f, 7.5f);
        }

        // ── Android TTS Native ────────────────────────────────────────────────

        void InitAndroidTTS()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!useAndroidTTSFallback) return;
            try
            {
                if (androidTTSBridge == null)
                    androidTTSBridge = new AndroidJavaClass("com.buhenar.tts.BuhenARTTS");

                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                androidTTSBridge.CallStatic("setVoice", nativeSpeechRate, nativePitch);
                androidTTSBridge.CallStatic("init", activity);
                androidTTSReady = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TTS] Android TTS init gagal: " + ex.Message);
                androidTTSBridge = null;
                androidTTSReady = false;
            }
#endif
        }

        void ShutdownAndroidTTS()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (androidTTSBridge != null)
            {
                androidTTSBridge.CallStatic("shutdown");
                androidTTSBridge = null;
                androidTTSReady = false;
            }
#endif
        }

        void SpeakNative(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(text)) return;
            if (androidTTSBridge == null || !androidTTSReady)
            {
                InitAndroidTTS();
                if (androidTTSBridge == null) return;
            }
            try
            {
                androidTTSBridge.CallStatic("speak", text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TTS] SpeakNative gagal: " + ex.Message);
            }
#endif
        }
    }
}
