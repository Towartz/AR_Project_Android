using System.IO;
using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialContentData))]
public class MaterialContentDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MaterialContentData content = (MaterialContentData)target;
        string assetPath = AssetDatabase.GetAssetPath(content);
        string assetName = string.IsNullOrWhiteSpace(assetPath)
            ? content.name
            : Path.GetFileNameWithoutExtension(assetPath);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Maintenance Tools", EditorStyles.boldLabel);

        DrawValidationHints(content, assetName);
        DrawActionButtons(content, assetName);
    }

    static void DrawValidationHints(MaterialContentData content, string assetName)
    {
        if (string.IsNullOrWhiteSpace(content.Id))
        {
            EditorGUILayout.HelpBox(
                "ID masih kosong. Auto configure bisa mengisi ID dari nama asset.",
                MessageType.Warning);
        }

        if (!content.HasPrefab)
        {
            string suggestedPrefabPath = ARtiGrafContentMaintenance.FindMatchingPrefabPath(assetName);
            if (!string.IsNullOrWhiteSpace(suggestedPrefabPath))
            {
                EditorGUILayout.HelpBox(
                    "Prefab belum terpasang. Kandidat prefab ditemukan di " + suggestedPrefabPath,
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Prefab belum terpasang.",
                    MessageType.Warning);
            }
        }

        if (!content.HasReferenceImage)
        {
            string suggestedReferenceImagePath = ARtiGrafContentMaintenance.FindMatchingReferenceImagePath(assetName);
            if (!string.IsNullOrWhiteSpace(suggestedReferenceImagePath))
            {
                EditorGUILayout.HelpBox(
                    "Reference image belum terpasang. Kandidat texture ditemukan di " + suggestedReferenceImagePath,
                    MessageType.Info);
            }
        }

        if (content.TargetWidthMeters <= 0f)
        {
            EditorGUILayout.HelpBox(
                "targetWidthMeters <= 0. Nilai default yang aman untuk marker saat ini adalah 0.12.",
                MessageType.Warning);
        }
    }

    static void DrawActionButtons(MaterialContentData content, string assetName)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Auto Configure"))
        {
            if (ARtiGrafContentMaintenance.AutoConfigureContent(content))
            {
                AssetDatabase.SaveAssets();
                GUIUtility.ExitGUI();
            }
        }

        if (GUILayout.Button("Ping Related Assets"))
        {
            PingRelatedAssets(content, assetName);
        }

        EditorGUILayout.EndHorizontal();
    }

    static void PingRelatedAssets(MaterialContentData content, string assetName)
    {
        if (content == null)
        {
            return;
        }

        if (content.Prefab != null)
        {
            EditorGUIUtility.PingObject(content.Prefab);
            return;
        }

        string prefabPath = ARtiGrafContentMaintenance.FindMatchingPrefabPath(assetName);
        if (!string.IsNullOrWhiteSpace(prefabPath))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                EditorGUIUtility.PingObject(prefab);
                return;
            }
        }

        if (content.ReferenceImageTexture != null)
        {
            EditorGUIUtility.PingObject(content.ReferenceImageTexture);
            return;
        }

        string referenceImagePath = ARtiGrafContentMaintenance.FindMatchingReferenceImagePath(assetName);
        if (!string.IsNullOrWhiteSpace(referenceImagePath))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);
            if (texture != null)
            {
                EditorGUIUtility.PingObject(texture);
            }
        }
    }
}
