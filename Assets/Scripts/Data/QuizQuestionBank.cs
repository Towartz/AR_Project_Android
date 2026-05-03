using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARtiGraf.Data
{
    [Serializable]
    public class QuizQuestionData
    {
        [TextArea(2, 4)]
        public string question;

        public string[] options = new string[4];

        [Min(0)]
        public int correctIndex;

        [TextArea(1, 3)]
        public string explanation;
    }

    [CreateAssetMenu(fileName = "QuizQuestionBank", menuName = "ARtiGraf/Quiz Question Bank")]
    public class QuizQuestionBank : ScriptableObject
    {
        [SerializeField] List<QuizQuestionData> questions = new List<QuizQuestionData>();

        public IReadOnlyList<QuizQuestionData> Questions => questions;
    }
}
