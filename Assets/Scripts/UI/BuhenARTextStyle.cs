using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    public static class BuhenARTextStyle
    {
        const float DefaultScale = 1.24f;
        public const int MinimumReadableSize = 20;
        const float ReferenceShortSide = 720f;

        static Font regularFont;
        static Font semiBoldFont;
        static Font boldFont;
        static Font extraBoldFont;

        public static Font ResolveFont(FontStyle style)
        {
            bool bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            if (bold)
                return extraBoldFont ??= LoadFont("Nunito-ExtraBold") ?? LoadFont("Nunito-Bold") ?? FallbackFont();

            return semiBoldFont ??= LoadFont("Nunito-SemiBold") ?? LoadFont("Nunito-Regular") ?? FallbackFont();
        }

        public static void Configure(
            Text text,
            int maxSize,
            int minSize,
            TextAnchor alignment,
            VerticalWrapMode verticalOverflow = VerticalWrapMode.Truncate,
            float scale = DefaultScale)
        {
            if (text == null) return;

            int scaledMax = ScaleSize(maxSize, scale);
            int scaledMin = Mathf.Min(scaledMax, Mathf.Max(MinimumReadableSize, ScaleSize(minSize, scale)));

            text.alignment = alignment;
            text.font = ResolveFont(text.fontStyle);
            text.fontSize = scaledMax;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = scaledMin;
            text.resizeTextMaxSize = scaledMax;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = verticalOverflow;
            text.lineSpacing = 1.06f;
            ApplyReadableShadow(text);
        }

        public static void ApplyRuntimeDefaults(Text text)
        {
            if (text == null) return;

            text.font = ResolveFont(text.fontStyle);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.lineSpacing = Mathf.Max(text.lineSpacing, 1.04f);
            if (text.resizeTextForBestFit)
                text.resizeTextMinSize = Mathf.Max(text.resizeTextMinSize, BuhenARTextStyle.MinimumReadableSize);
            ApplyReadableShadow(text);
        }

        public static int ScaleSize(int size, float scale = DefaultScale)
        {
            float responsiveScale = ResolveResponsiveScale();
            return Mathf.Clamp(Mathf.CeilToInt(size * scale * responsiveScale), MinimumReadableSize, 96);
        }

        static float ResolveResponsiveScale()
        {
            if (!Application.isPlaying)
                return 1f;

            float shortSide = Mathf.Min(Screen.width, Screen.height);
            if (shortSide <= 1f)
                return 1f;

            // 720px short side is the design baseline. Square-root scaling keeps
            // text readable across devices without huge jumps on tablets.
            return Mathf.Clamp(Mathf.Sqrt(shortSide / ReferenceShortSide), 0.94f, 1.16f);
        }

        static void ApplyReadableShadow(Text text)
        {
            if (text == null) return;

            float luminance = (text.color.r * 0.299f) + (text.color.g * 0.587f) + (text.color.b * 0.114f);
            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null)
                shadow = text.gameObject.AddComponent<Shadow>();

            bool needsShadow = luminance > 0.72f;
            shadow.enabled = needsShadow;
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(1.1f, -1.1f);
            shadow.useGraphicAlpha = true;
        }

        static Font LoadFont(string name)
        {
            return Resources.Load<Font>("Fonts/Nunito/" + name);
        }

        static Font FallbackFont()
        {
            return regularFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    public static class BuhenARTextRuntimeBootstrap
    {
        static readonly HashSet<Text> scaledTexts = new HashSet<Text>();
        static bool initialized;
        static int lastSweepFrame = -120;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            SceneManager.sceneLoaded += (_, __) => ApplyToAllText();
            Canvas.willRenderCanvases += ApplyToAllText;
            ApplyToAllText();
        }

        static void ApplyToAllText()
        {
            if (Application.isPlaying && Time.frameCount - lastSweepFrame < 60)
                return;
            lastSweepFrame = Time.frameCount;

            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
                ApplyToText(texts[i]);
        }

        static void ApplyToText(Text text)
        {
            if (text == null) return;

            BuhenARTextStyle.ApplyRuntimeDefaults(text);
            if (scaledTexts.Contains(text)) return;
            scaledTexts.Add(text);
        }
    }
}
