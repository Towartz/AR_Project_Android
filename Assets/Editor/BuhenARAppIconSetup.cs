using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class BuhenARAppIconSetup
{
    const string IconPath = "Assets/Art/UI_APPS/icon.png";

    [MenuItem("Tools/BuhenAR/Set App Icon")]
    public static void SetAppIcon()
    {
        Texture2D icon = PrepareIconTexture();
        if (icon == null)
        {
            Debug.LogError("[BuhenAR] Icon app tidak ditemukan: " + IconPath);
            return;
        }

        ApplyLegacyIcons(BuildTargetGroup.Unknown, icon);
        ApplyLegacyIcons(BuildTargetGroup.Android, icon);
        TryApplyNamedBuildTargetIcons(icon);

        AssetDatabase.SaveAssets();
        Debug.Log("[BuhenAR] App icon diset dari " + IconPath);
    }

    static Texture2D PrepareIconTexture()
    {
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
    }

    static void ApplyLegacyIcons(BuildTargetGroup group, Texture2D icon)
    {
        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(group);
        int count = sizes != null && sizes.Length > 0 ? sizes.Length : 1;
        PlayerSettings.SetIconsForTargetGroup(group, RepeatIcon(icon, count));
    }

    static void TryApplyNamedBuildTargetIcons(Texture2D icon)
    {
        try
        {
            Type namedBuildTargetType = Type.GetType("UnityEditor.Build.NamedBuildTarget, UnityEditor");
            Type iconKindType = Type.GetType("UnityEditor.IconKind, UnityEditor");
            if (namedBuildTargetType == null || iconKindType == null)
                return;

            object androidTarget = namedBuildTargetType.GetProperty("Android", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object applicationKind = Enum.Parse(iconKindType, "Application");
            if (androidTarget == null || applicationKind == null)
                return;

            MethodInfo getIconSizes = typeof(PlayerSettings).GetMethod(
                "GetIconSizes",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { namedBuildTargetType, iconKindType },
                null);
            MethodInfo setIcons = typeof(PlayerSettings).GetMethod(
                "SetIcons",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { namedBuildTargetType, typeof(Texture2D[]), iconKindType },
                null);

            if (setIcons == null)
                return;

            int count = 1;
            if (getIconSizes != null)
            {
                int[] sizes = getIconSizes.Invoke(null, new[] { androidTarget, applicationKind }) as int[];
                count = sizes != null && sizes.Length > 0 ? sizes.Length : 1;
            }

            setIcons.Invoke(null, new object[] { androidTarget, RepeatIcon(icon, count), applicationKind });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[BuhenAR] NamedBuildTarget icon API dilewati: " + ex.Message);
        }
    }

    static Texture2D[] RepeatIcon(Texture2D icon, int count)
    {
        count = Mathf.Max(1, count);
        Texture2D[] icons = new Texture2D[count];
        for (int i = 0; i < icons.Length; i++)
            icons[i] = icon;
        return icons;
    }
}
