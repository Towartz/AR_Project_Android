using System.Collections.Generic;
using ARtiGraf.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ARtiGraf.UI
{
    /// <summary>
    /// Converts content reference textures to UI sprites at runtime.
    /// This keeps Collection and Quiz Hunt thumbnails visible even when the
    /// ScriptableObject thumbnail field is intentionally left empty.
    /// </summary>
    public static class RuntimeSpriteCache
    {
        static readonly Dictionary<Texture2D, Sprite> TextureSprites = new Dictionary<Texture2D, Sprite>();

        public static Sprite ResolveContentSprite(MaterialContentData content)
        {
            if (content == null) return null;
            if (content.Thumbnail != null) return content.Thumbnail;

            Texture2D texture = content.ReferenceImageTexture;
            if (texture == null) return null;

            if (!TextureSprites.TryGetValue(texture, out Sprite sprite) || sprite == null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                TextureSprites[texture] = sprite;
            }

            return sprite;
        }

        public static bool ApplyContentSprite(Image image, MaterialContentData content, Color color)
        {
            if (image == null) return false;

            Sprite sprite = ResolveContentSprite(content);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = color;
            image.enabled = sprite != null;
            image.raycastTarget = false;
            return sprite != null;
        }
    }
}
