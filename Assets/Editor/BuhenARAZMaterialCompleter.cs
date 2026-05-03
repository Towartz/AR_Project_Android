using System.Collections.Generic;
using System.Text;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

public static class BuhenARAZMaterialCompleter
{
    const string ScriptableObjectsFolder = "Assets/ScriptableObjects";
    const string QuizBankPath = "Assets/ScriptableObjects/ARtiGrafQuizBank.asset";
    const string SummaryPath = "UJIKOM_AZ_Materi.md";

    sealed class Entry
    {
        public string Asset;
        public string Letter;
        public string Title;
        public LearningCategory Category;
        public string Type;
        public string Color;
        public string Feature;
        public string Description;
        public string FunFact;
    }

    static readonly Entry[] Entries =
    {
        Fruit("apel", "A", "Apel", "Merah, hijau, atau kuning", "Bulat, bertangkai, kulit halus",
            "Apel adalah buah yang tumbuh di pohon. Kulitnya halus, dagingnya renyah, dan rasanya manis atau sedikit asam.",
            "Apel punya biji kecil di bagian tengah dan sering dimakan sebagai camilan sehat."),
        Animal("buaya", "B", "Buaya", "Hijau tua atau cokelat", "Bersisik, tubuh panjang, gigi tajam",
            "Buaya adalah reptil besar yang hidup di air dan darat. Tubuhnya bersisik, mulutnya panjang, dan ekornya kuat.",
            "Buaya bisa menunggu dengan sangat tenang di air sebelum bergerak cepat."),
        Fruit("ceri", "C", "Ceri", "Merah", "Kecil, bulat, bertangkai",
            "Ceri adalah buah kecil berwarna merah yang biasanya memiliki tangkai. Rasanya manis dan sedikit asam.",
            "Ceri sering dipakai sebagai hiasan kue karena warnanya cerah."),
        Animal("domba", "D", "Domba", "Putih atau krem", "Bulu tebal, berkaki empat, makan rumput",
            "Domba adalah hewan ternak berbulu tebal. Bulunya dapat dimanfaatkan untuk membuat wol.",
            "Anak domba disebut cempe dan biasanya terlihat lucu saat melompat."),
        Animal("elang", "E", "Elang", "Cokelat, putih, atau hitam", "Sayap lebar, paruh bengkok, cakar tajam",
            "Elang adalah burung pemangsa yang dapat terbang tinggi. Matanya tajam untuk melihat mangsa dari jauh.",
            "Penglihatan elang sangat kuat sehingga ia bisa melihat gerakan kecil dari ketinggian."),
        Animal("flamingo", "F", "Flamingo", "Merah muda", "Leher panjang, kaki panjang, berdiri di air",
            "Flamingo adalah burung berkaki panjang yang sering hidup di dekat air. Bulunya dikenal berwarna merah muda.",
            "Warna merah muda flamingo berasal dari makanan yang dimakannya."),
        Animal("gajah", "G", "Gajah", "Abu-abu", "Badan besar, belalai panjang, telinga lebar",
            "Gajah adalah hewan darat yang sangat besar. Ia memakai belalai untuk mengambil makanan dan minum.",
            "Belalai gajah bisa dipakai untuk mencium, mengambil benda, dan menyemprot air."),
        Animal("harimau", "H", "Harimau", "Oranye dengan belang hitam", "Belang hitam, cakar tajam, tubuh kuat",
            "Harimau adalah kucing besar dengan belang hitam. Ia hidup sebagai pemburu di hutan.",
            "Setiap harimau punya pola belang yang berbeda seperti sidik jari."),
        Animal("ikan", "I", "Ikan", "Beragam warna", "Bersirip, bersisik, hidup di air",
            "Ikan adalah hewan air yang bernapas dengan insang. Ikan bergerak memakai sirip dan ekor.",
            "Ikan memakai insang untuk mengambil oksigen dari air."),
        Fruit("jagung", "J", "Jagung", "Kuning", "Biji berjajar, tongkol panjang, berdaun hijau",
            "Jagung adalah tanaman pangan dengan biji kuning yang tersusun di tongkol. Jagung bisa dimakan rebus atau bakar.",
            "Satu tongkol jagung berisi banyak biji yang tersusun rapi."),
        Animal("kelinci", "K", "Kelinci", "Putih, cokelat, atau abu-abu", "Telinga panjang, melompat, bulu lembut",
            "Kelinci adalah hewan kecil bertelinga panjang. Ia suka melompat dan makan sayuran.",
            "Kelinci menggerakkan hidungnya cepat saat mencium bau di sekitar."),
        Fruit("lemon", "L", "Lemon", "Kuning", "Oval, kulit kuning, rasa asam",
            "Lemon adalah buah sitrus berwarna kuning. Rasanya asam dan sering dibuat minuman segar.",
            "Aroma lemon sering dipakai karena terasa segar."),
        Fruit("mangga", "M", "Mangga", "Hijau, kuning, atau oranye", "Lonjong, berdaging tebal, berbiji besar",
            "Mangga adalah buah tropis yang rasanya manis saat matang. Daging buahnya tebal dan berwarna kuning oranye.",
            "Mangga muda biasanya terasa lebih asam daripada mangga matang."),
        Fruit("nanas", "N", "Nanas", "Kuning dan hijau", "Kulit bersisik tajam, mahkota daun, rasa manis asam",
            "Nanas adalah buah tropis dengan mahkota daun di atas. Kulitnya bersisik dan rasanya manis asam.",
            "Nanas tampak seperti memakai mahkota daun di bagian atas."),
        Animal("orang_utan", "O", "Orang Utan", "Cokelat kemerahan", "Lengan panjang, berbulu merah kecokelatan, pintar memanjat",
            "Orang utan adalah primata besar yang hidup di hutan. Lengannya panjang dan kuat untuk bergelantungan.",
            "Nama orang utan berarti manusia hutan."),
        Fruit("pepaya", "P", "Pepaya", "Hijau atau oranye", "Lonjong, daging oranye, banyak biji kecil",
            "Pepaya adalah buah lonjong yang dagingnya oranye saat matang. Di bagian tengahnya ada banyak biji kecil.",
            "Pepaya matang terasa manis dan lembut."),
        Animal("quail", "Q", "Quail", "Cokelat bercorak", "Burung kecil, tubuh berbintik, kaki pendek",
            "Quail atau burung puyuh adalah burung kecil bercorak cokelat. Burung ini sering berjalan di tanah.",
            "Quail dikenal juga sebagai burung puyuh."),
        Fruit("rambutan", "R", "Rambutan", "Merah atau kuning", "Kulit berambut, daging putih, rasa manis",
            "Rambutan adalah buah tropis yang kulitnya terlihat berambut. Daging buahnya putih dan rasanya manis.",
            "Nama rambutan berasal dari bentuk kulitnya yang seperti rambut."),
        Animal("sapi", "S", "Sapi", "Putih, cokelat, atau hitam", "Badan besar, bertanduk, makan rumput",
            "Sapi adalah hewan ternak besar yang makan rumput. Sapi dapat menghasilkan susu.",
            "Susu sapi sering diolah menjadi yogurt, keju, dan mentega."),
        Fruit("timun", "T", "Timun", "Hijau", "Panjang, hijau, banyak air",
            "Timun adalah sayur buah yang terasa segar karena banyak mengandung air. Bentuknya panjang dan kulitnya hijau.",
            "Timun sering dimakan sebagai lalapan karena rasanya segar."),
        Animal("ular", "U", "Ular", "Beragam warna", "Tubuh panjang, tidak berkaki, melata",
            "Ular adalah reptil bertubuh panjang tanpa kaki. Ular bergerak dengan cara melata.",
            "Ular menjulurkan lidah untuk membantu mengenali bau di sekitarnya."),
        Animal("vampire_bat", "V", "Vampire Bat", "Cokelat gelap", "Bersayap, aktif malam, tidur menggantung",
            "Vampire bat atau kelelawar vampir adalah hewan malam yang bisa terbang. Ia tidur dengan posisi menggantung.",
            "Kelelawar memakai pendengaran tajam untuk bergerak di tempat gelap."),
        Fruit("wortel", "W", "Wortel", "Oranye", "Akar panjang, warna oranye, daun hijau",
            "Wortel adalah sayuran akar berwarna oranye. Bagian yang dimakan tumbuh di dalam tanah.",
            "Wortel dikenal baik untuk mengenalkan warna oranye kepada anak-anak."),
        Animal("xenops", "X", "Xenops", "Cokelat dan krem", "Burung kecil, paruh runcing, hidup di pohon",
            "Xenops adalah burung kecil dari hutan tropis. Burung ini mencari serangga di batang pohon.",
            "Xenops membantu menjaga keseimbangan alam dengan memakan serangga kecil."),
        Animal("yak", "Y", "Yak", "Cokelat tua atau hitam", "Bulu tebal, tanduk panjang, kuat di udara dingin",
            "Yak adalah hewan berbulu tebal yang hidup di daerah dingin dan pegunungan. Tubuhnya kuat dan besar.",
            "Bulu yak yang tebal membantu tubuhnya tetap hangat."),
        Animal("zebra", "Z", "Zebra", "Hitam dan putih", "Belang hitam putih, mirip kuda, hidup berkelompok",
            "Zebra adalah hewan mirip kuda dengan belang hitam putih. Zebra biasanya hidup berkelompok.",
            "Pola belang setiap zebra berbeda-beda.")
    };

    [MenuItem("Tools/BuhenAR/Content/Complete A-Z Materials")]
    public static void CompleteDefaultMaterials()
    {
        int updated = 0;
        for (int i = 0; i < Entries.Length; i++)
        {
            Entry entry = Entries[i];
            string path = ScriptableObjectsFolder + "/" + entry.Asset + ".asset";
            MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(path);
            if (content == null)
            {
                Debug.LogWarning("[BuhenAR] Materi tidak ditemukan: " + path);
                continue;
            }

            SerializedObject serialized = new SerializedObject(content);
            Set(serialized, "category", (int)entry.Category);
            Set(serialized, "title", entry.Title);
            Set(serialized, "subtitle", "Huruf " + entry.Letter + " - " + entry.Title);
            Set(serialized, "description", entry.Description);
            Set(serialized, "objectType", entry.Type);
            Set(serialized, "colorFocus", entry.Color);
            Set(serialized, "fontTypeFocus", entry.Feature);
            Set(serialized, "funFact", entry.FunFact);
            Set(serialized, "spellOverride", entry.Title);
            SetArray(serialized.FindProperty("quizQuestions"), BuildQuestions(entry));
            SetArray(serialized.FindProperty("quizAnswers"), BuildAnswers(entry));
            SetArray(serialized.FindProperty("quizWrongOptions"), BuildWrongOptions(i, entry));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);
            updated++;
        }

        FillGlobalQuizBank();
        WriteSummary();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuhenAR] Materi A-Z dan bank soal dilengkapi: " + updated + " item.");
    }

    public static void CompleteAndSave()
    {
        CompleteDefaultMaterials();
    }

    static Entry Fruit(string asset, string letter, string title, string color, string feature, string description, string fact)
    {
        return new Entry
        {
            Asset = asset,
            Letter = letter,
            Title = title,
            Category = LearningCategory.Typography,
            Type = "Buah/Sayur",
            Color = color,
            Feature = feature,
            Description = description,
            FunFact = fact
        };
    }

    static Entry Animal(string asset, string letter, string title, string color, string feature, string description, string fact)
    {
        return new Entry
        {
            Asset = asset,
            Letter = letter,
            Title = title,
            Category = LearningCategory.Color,
            Type = "Hewan",
            Color = color,
            Feature = feature,
            Description = description,
            FunFact = fact
        };
    }

    static string[] BuildQuestions(Entry entry)
    {
        return new[]
        {
            "Apa nama objek pada kartu huruf " + entry.Letter + "?",
            "Huruf awal dari " + entry.Title + " adalah?",
            entry.Title + " termasuk kelompok apa?",
            "Warna yang cocok dengan " + entry.Title + " adalah?",
            "Ciri utama " + entry.Title + " adalah?",
            "Jika petunjuknya \"" + entry.Feature + "\", kartu apa yang harus discan?"
        };
    }

    static string[] BuildAnswers(Entry entry)
    {
        return new[]
        {
            entry.Title,
            entry.Letter,
            entry.Type,
            entry.Color,
            entry.Feature,
            entry.Title
        };
    }

    static string[] BuildWrongOptions(int index, Entry entry)
    {
        return new[]
        {
            JoinNext(index, e => e.Title),
            JoinNext(index, e => e.Letter),
            entry.Type == "Hewan" ? "Buah/Sayur|Benda Mati|Kendaraan" : "Hewan|Benda Mati|Kendaraan",
            JoinNext(index, e => e.Color),
            JoinNext(index, e => e.Feature),
            JoinNext(index, e => e.Title)
        };
    }

    static void FillGlobalQuizBank()
    {
        QuizQuestionBank bank = AssetDatabase.LoadAssetAtPath<QuizQuestionBank>(QuizBankPath);
        if (bank == null)
        {
            bank = ScriptableObject.CreateInstance<QuizQuestionBank>();
            AssetDatabase.CreateAsset(bank, QuizBankPath);
        }

        SerializedObject bankObject = new SerializedObject(bank);
        SerializedProperty questions = bankObject.FindProperty("questions");
        questions.ClearArray();

        for (int i = 0; i < Entries.Length; i++)
        {
            Entry entry = Entries[i];
            AddBankQuestion(
                questions,
                "Kartu huruf " + entry.Letter + " berisi objek apa?",
                BuildOptions(i, e => e.Title),
                "Kartu huruf " + entry.Letter + " adalah " + entry.Title + ".");

            AddBankQuestion(
                questions,
                "Huruf awal dari " + entry.Title + " adalah?",
                BuildOptions(i, e => e.Letter),
                entry.Title + " dimulai dari huruf " + entry.Letter + ".");

            AddBankQuestion(
                questions,
                entry.Title + " termasuk kelompok apa?",
                BuildGroupOptions(entry),
                entry.Title + " termasuk kelompok " + entry.Type + ".");

            AddBankQuestion(
                questions,
                "Ciri yang cocok dengan " + entry.Title + " adalah?",
                BuildOptions(i, e => e.Feature),
                "Ciri utama " + entry.Title + ": " + entry.Feature + ".");

            AddBankQuestion(
                questions,
                "Warna yang cocok dengan " + entry.Title + " adalah?",
                BuildOptions(i, e => e.Color),
                "Warna/pengenal " + entry.Title + ": " + entry.Color + ".");
        }

        bankObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bank);
        Debug.Log("[BuhenAR] Global quiz bank diisi: " + questions.arraySize + " soal.");
    }

    static string[] BuildOptions(int index, System.Func<Entry, string> selector)
    {
        var options = new List<string>();
        AddUnique(options, selector(Entries[index]));
        for (int offset = 1; offset < Entries.Length && options.Count < 4; offset++)
            AddUnique(options, selector(Entries[(index + offset) % Entries.Length]));

        while (options.Count < 4)
            options.Add("-");

        return options.ToArray();
    }

    static string[] BuildGroupOptions(Entry entry)
    {
        return entry.Type == "Hewan"
            ? new[] { "Hewan", "Buah/Sayur", "Benda Mati", "Kendaraan" }
            : new[] { "Buah/Sayur", "Hewan", "Benda Mati", "Kendaraan" };
    }

    static void AddBankQuestion(SerializedProperty questions, string question, string[] options, string explanation)
    {
        int index = questions.arraySize;
        questions.InsertArrayElementAtIndex(index);

        SerializedProperty item = questions.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("question").stringValue = question;
        item.FindPropertyRelative("correctIndex").intValue = 0;
        item.FindPropertyRelative("explanation").stringValue = explanation;

        SerializedProperty optionProperty = item.FindPropertyRelative("options");
        optionProperty.arraySize = options.Length;
        for (int i = 0; i < options.Length; i++)
            optionProperty.GetArrayElementAtIndex(i).stringValue = options[i];
    }

    static void AddUnique(List<string> values, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
                return;
        }

        values.Add(value);
    }

    static string JoinNext(int index, System.Func<Entry, string> selector)
    {
        var values = new List<string>();
        for (int offset = 1; offset < Entries.Length && values.Count < 3; offset++)
        {
            Entry candidate = Entries[(index + offset) % Entries.Length];
            string value = selector(candidate);
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
                values.Add(value);
        }

        return string.Join("|", values);
    }

    static void Set(SerializedObject serialized, string propertyName, string value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.stringValue = value ?? string.Empty;
    }

    static void Set(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.intValue = value;
    }

    static void SetArray(SerializedProperty property, string[] values)
    {
        if (property == null || !property.isArray) return;
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i] ?? string.Empty;
    }

    static void WriteSummary()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Materi A-Z BuhenAR");
        builder.AppendLine();
        builder.AppendLine("File ini dibuat otomatis oleh `Tools/BuhenAR/Content/Complete A-Z Materials`.");
        builder.AppendLine("Materi aktif runtime tetap memakai 26 flashcard A-Z dari database BuhenAR.");
        builder.AppendLine();
        builder.AppendLine("| Huruf | Nama | Kelompok | Ciri Utama | Warna |");
        builder.AppendLine("|---|---|---|---|---|");
        foreach (Entry entry in Entries)
            builder.AppendLine("| " + entry.Letter + " | " + entry.Title + " | " + entry.Type + " | " + entry.Feature + " | " + entry.Color + " |");

        System.IO.File.WriteAllText(SummaryPath, builder.ToString());
    }
}
