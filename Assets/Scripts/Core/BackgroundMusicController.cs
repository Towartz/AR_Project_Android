using System;
using UnityEngine;

namespace ARtiGraf.Core
{
    public class BackgroundMusicController : MonoBehaviour
    {
        const string MutedPreferenceKey = "BuhenAR_MusicMuted";

        static BackgroundMusicController instance;

        [SerializeField] AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] float volume = 0.35f;
        [SerializeField] bool playOnAwake = true;

        AudioSource audioSource;

        public static event Action StateChanged;

        public static bool IsMuted => PlayerPrefs.GetInt(MutedPreferenceKey, 0) == 1;
        public static bool HasInstance => instance != null;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                if (instance.musicClip == null && musicClip != null)
                    instance.SetMusicClip(musicClip);

                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            ConfigureAudioSource();
        }

        void Start()
        {
            ApplyState(playIfNeeded: true);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void ToggleMute()
        {
            SetMuted(!IsMuted);
        }

        public static void ToggleGlobalMute()
        {
            if (instance != null)
            {
                instance.ToggleMute();
                return;
            }

            SetMutedPreference(!IsMuted);
            StateChanged?.Invoke();
        }

        public void SetMuted(bool muted)
        {
            SetMutedPreference(muted);
            ApplyState(playIfNeeded: true);
            StateChanged?.Invoke();
        }

        void SetMusicClip(AudioClip clip)
        {
            musicClip = clip;
            ConfigureAudioSource();
            ApplyState(playIfNeeded: true);
        }

        void ConfigureAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.clip = musicClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;
        }

        void ApplyState(bool playIfNeeded)
        {
            if (audioSource == null)
                ConfigureAudioSource();

            audioSource.mute = IsMuted;
            audioSource.volume = volume;

            if (playOnAwake && playIfNeeded && musicClip != null && !audioSource.isPlaying)
                audioSource.Play();
        }

        static void SetMutedPreference(bool muted)
        {
            PlayerPrefs.SetInt(MutedPreferenceKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (audioSource != null)
                ApplyState(playIfNeeded: false);
        }
#endif
    }
}
