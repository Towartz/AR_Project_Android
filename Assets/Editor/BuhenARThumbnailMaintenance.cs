using ARtiGraf.Data;
using UnityEditor;
using UnityEngine;

public static class BuhenARThumbnailMaintenance
{
    [MenuItem("BuhenAR/Maintenance/Fix Flashcard Thumbnails")]
    public static void FixFlashcardThumbnails()
    {
        int fixedCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:MaterialContentData", new[] { "Assets/ScriptableObjects" });

        for (int i = 0; i < guids.Length; i++)
        {
            string contentPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            MaterialContentData content = AssetDatabase.LoadAssetAtPath<MaterialContentData>(contentPath);
            if (content == null || content.ReferenceImageTexture == null) continue;

            string texturePath = AssetDatabase.GetAssetPath(content.ReferenceImageTexture);
            if (string.IsNullOrWhiteSpace(texturePath)) continue;

            Sprite sprite = EnsureSprite(texturePath);
            if (sprite == null) continue;

            SerializedObject serializedContent = new SerializedObject(content);
            serializedContent.FindProperty("thumbnail").objectReferenceValue = sprite;
            serializedContent.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuhenAR] Flashcard thumbnail fixed: " + fixedCount + " content assets.");
    }

    static Sprite EnsureSprite(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 1024);

            if (changed)
                importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }
}
