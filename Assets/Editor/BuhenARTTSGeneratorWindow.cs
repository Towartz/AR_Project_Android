using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

public class BuhenARTTSGeneratorWindow : EditorWindow
{
    const string TTSRootFolder = "Assets/Resources/TTS";
    const string LettersFolder = TTSRootFolder + "/Letters";
    const string WordsFolder = TTSRootFolder + "/Words";
    const string ManifestPath = TTSRootFolder + "/tts_word_list.csv";
    const string ScriptableObjectsFolder = "Assets/ScriptableObjects";

    readonly List<MaterialContentData> contents = new List<MaterialContentData>();
    Vector2 scrollPosition;
    string statusText = string.Empty;

    [MenuItem("Tools/BuhenAR/TTS/Open TTS Generator")]
    [MenuItem("Tools/ARtiGraf/TTS Generator")]
    public static void Open()
    {
        BuhenARTTSGeneratorWindow window = GetWindow<BuhenARTTSGeneratorWindow>("BuhenAR TTS");
        window.minSize = new Vector2(760f, 520f);
        window.RefreshData();
    }

    void OnEnable()
    {
        RefreshData();
    }

    void OnFocus()
    {
        RefreshData();
    }

    void OnGUI()
    {
        DrawHeader();
        DrawActions();
        DrawStatus();
        DrawContentList();
    }

    void DrawHeader()
    {
        EditorGUILayout.HelpBox(
            "Fast path: build Android langsung bisa pakai native Android TTS tanpa file MP3. " +
            "Generator ini dipakai kalau ingin versi suara custom: buat folder, export CSV nama konten, lalu auto-assign MP3 yang sudah kamu taruh di Resources/TTS/Words.",
            MessageType.Info);
    }

    void DrawActions()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Setup Folders", GUILayout.Height(34f)))
        {
            SetupFolders();
        }

        if (GUILayout.Button("2. Generate CSV", GUILayout.Height(34f)))
        {
            GenerateManifestCsv();
        }

        if (GUILayout.Button("3. Auto Assign Word Clips", GUILayout.Height(34f)))
        {
            AutoAssignWordClips();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Height(26f)))
        {
            RefreshData();
        }

        if (GUILayout.Button("Select TTS Folder", GUILayout.Height(26f)))
        {
            SetupFolders();
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(TTSRootFolder);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        if (GUILayout.Button("Validate", GUILayout.Height(26f)))
        {
            ValidateSetup();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Folder huruf", LettersFolder);
        EditorGUILayout.LabelField("Folder kata", WordsFolder);
        EditorGUILayout.LabelField("CSV", ManifestPath);
        EditorGUILayout.EndVertical();
    }

    void DrawStatus()
    {
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            EditorGUILayout.HelpBox(statusText, MessageType.None);
        }
    }

    void DrawContentList()
    {
        EditorGUILayout.LabelField("Konten TTS", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (contents.Count == 0)
        {
            EditorGUILayout.LabelField("Belum ada MaterialContentData.");
        }

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null) continue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(content, typeof(MaterialContentData), false);
            EditorGUILayout.LabelField("ID: " + Safe(content.Id), GUILayout.Width(160f));
            EditorGUILayout.LabelField("Title: " + Safe(content.Title), GUILayout.Width(180f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spell: " + Safe(content.SpellWord), GUILayout.Width(260f));
            EditorGUILayout.ObjectField("Word Clip", content.NameAudioClip, typeof(AudioClip), false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    void SetupFolders()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "TTS");
        EnsureFolder(TTSRootFolder, "Letters");
        EnsureFolder(TTSRootFolder, "Words");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        statusText = "Folder TTS siap. Masukkan A.mp3-Z.mp3 ke Letters jika mau suara huruf custom, dan nama item ke Words jika mau suara kata custom.";
        RefreshData();
    }

    void GenerateManifestCsv()
    {
        SetupFolders();
        RefreshData();

        var builder = new StringBuilder();
        builder.AppendLine("id,title,spell_word,recommended_word_clip,spoken_text");

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null || content.IsDemoContent) continue;

            string id = Safe(content.Id);
            string title = Safe(content.Title);
            string spell = Safe(content.SpellWord);
            string recommended = WordsFolder + "/" + MaterialContentKeyUtility.Normalize(id) + ".mp3";
            string spoken = title;
            builder.AppendLine(
                Csv(id) + "," +
                Csv(title) + "," +
                Csv(spell) + "," +
                Csv(recommended) + "," +
                Csv(spoken));
        }

        File.WriteAllText(ManifestPath, builder.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(ManifestPath);
        statusText = "CSV dibuat: " + ManifestPath + ". Pakai ini sebagai daftar batch TTS external kalau ingin MP3 custom.";
    }

    void AutoAssignWordClips()
    {
        SetupFolders();
        RefreshData();

        Dictionary<string, AudioClip> wordClips = LoadWordClipMap();
        int assigned = 0;
        int missing = 0;

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null || content.IsDemoContent) continue;

            AudioClip clip = FindClipForContent(wordClips, content);
            if (clip == null)
            {
                missing++;
                continue;
            }

            SerializedObject serialized = new SerializedObject(content);
            SerializedProperty property = serialized.FindProperty("nameAudioClip");
            if (property != null && property.objectReferenceValue != clip)
            {
                property.objectReferenceValue = clip;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(content);
                assigned++;
            }
        }

        AssetDatabase.SaveAssets();
        statusText = "Auto assign selesai. Updated: " + assigned + ", belum ada MP3: " + missing + ".";
        RefreshData();
    }

    void ValidateSetup()
    {
        SetupFolders();
        RefreshData();

        int letterCount = CountLetterClips();
        int customWordCount = 0;
        int missingWordCount = 0;

        for (int i = 0; i < contents.Count; i++)
        {
            MaterialContentData content = contents[i];
            if (content == null || content.IsDemoContent) continue;
            if (content.NameAudioClip != null) customWordCount++;
            else missingWordCount++;
        }

        statusText =
            "Huruf custom ditemukan: " + letterCount + " / 26. " +
            "Kata custom assigned: " + customWordCount + ". " +
            "Tanpa kata custom: " + missingWordCount + ". " +
            "Catatan: tanpa MP3 pun tetap bisa bicara di Android karena TTSController memakai native Android TTS fallback.";
    }

    void RefreshData()
    {
        contents.Clear();
        string[] guids = AssetDatabase.FindAssets("t:MaterialContentData", new[] { ScriptableObjectsFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(path);
            if (content != null) contents.Add(content);
        }

        contents.Sort((left, right) => string.Compare(Safe(left?.Title), Safe(right?.Title), StringComparison.OrdinalIgnoreCase));
        Repaint();
    }

    static Dictionary<string, AudioClip> LoadWordClipMap()
    {
        var result = new Dictionary<string, AudioClip>();
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { WordsFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            string key = MaterialContentKeyUtility.Normalize(fileName);
            if (!string.IsNullOrEmpty(key) && !result.ContainsKey(key))
            {
                result.Add(key, clip);
            }
        }

        return result;
    }

    static AudioClip FindClipForContent(Dictionary<string, AudioClip> wordClips, MaterialContentData content)
    {
        string[] candidates =
        {
            content.Id,
            content.Title,
            content.SpellWord,
            MaterialContentKeyUtility.Normalize(content.Id),
            MaterialContentKeyUtility.Normalize(content.Title)
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string key = MaterialContentKeyUtility.Normalize(candidates[i]);
            if (!string.IsNullOrEmpty(key) && wordClips.TryGetValue(key, out AudioClip clip))
            {
                return clip;
            }
        }

        return null;
    }

    static int CountLetterClips()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { LettersFolder });
        var available = new HashSet<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            available.Add(Path.GetFileNameWithoutExtension(path).ToUpperInvariant());
        }

        for (char c = 'A'; c <= 'Z'; c++)
        {
            if (available.Contains(c.ToString())) count++;
        }

        return count;
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static string Safe(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    static string Csv(string value)
    {
        value = Safe(value).Replace("\"", "\"\"");
        return "\"" + value + "\"";
    }
}
