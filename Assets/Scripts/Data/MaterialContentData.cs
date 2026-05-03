using System;
using UnityEngine;

namespace ARtiGraf.Data
{
    [CreateAssetMenu(fileName = "MaterialContentData", menuName = "ARtiGraf/Material Content")]
    public class MaterialContentData : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] LearningCategory category;
        [SerializeField] string title;
        [SerializeField] string subtitle;
        [SerializeField] string description;
        [SerializeField] Sprite thumbnail;
        [SerializeField] Texture2D referenceImageTexture;
        [SerializeField] GameObject prefab;
        [SerializeField] string referenceImageName;
        [SerializeField] float targetWidthMeters = 0.12f;
        [SerializeField] string objectType;
        [SerializeField] string colorFocus;
        [SerializeField] string fontTypeFocus;
        [SerializeField] AudioClip nameAudioClip;
        [SerializeField] string funFact;
        [Tooltip("Override ejaan TTS. Kosongkan untuk pakai Title secara otomatis. Contoh: 'A-N-G-G-U-R'")]
        [SerializeField] string spellOverride;
        [SerializeField] string[] quizQuestions;
        [SerializeField] string[] quizAnswers;
        [SerializeField] string[] quizWrongOptions;

        public string Id => id;
        public LearningCategory Category => category;
        public string Title => title;
        public string Subtitle => subtitle;
        public string Description => description;
        public Sprite Thumbnail => thumbnail;
        public Texture2D ReferenceImageTexture => referenceImageTexture;
        public GameObject Prefab => prefab;
        public string ReferenceImageName => referenceImageName;
        public float TargetWidthMeters => targetWidthMeters;
        public string ObjectType => objectType;
        public string ColorFocus => colorFocus;
        public string FontTypeFocus => fontTypeFocus;
        public AudioClip NameAudioClip => nameAudioClip;
        public string FunFact => funFact;
        /// <summary>Kata yang dieja TTS. Jika kosong pakai Title.</summary>
        public string SpellWord => string.IsNullOrWhiteSpace(spellOverride) ? title : spellOverride;
        public string[] QuizQuestions => quizQuestions;
        public string[] QuizAnswers => quizAnswers;
        public string[] QuizWrongOptions => quizWrongOptions;
        public string NormalizedId => MaterialContentKeyUtility.Normalize(id);
        public string NormalizedTitle => MaterialContentKeyUtility.Normalize(title);
        public string NormalizedReferenceImageName => MaterialContentKeyUtility.Normalize(referenceImageName);
        public bool HasReferenceImage => referenceImageTexture != null;
        public bool HasPrefab => prefab != null;
        public bool IsBarcodeOnly => !HasReferenceImage;
        public bool IsDemoContent => IsKnownDemoKey(NormalizedId) || IsKnownDemoKey(NormalizedReferenceImageName);

        static bool IsKnownDemoKey(string normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return false;
            }

            return normalizedValue.StartsWith("blend", StringComparison.OrdinalIgnoreCase) ||
                   normalizedValue == "cube" ||
                   normalizedValue == "cubes" ||
                   normalizedValue == "debugcube";
        }
    }
}
