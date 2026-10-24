using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace F1.UI
{
    /// <summary>
    /// Round 104 (판정 1): where the top of a figure's art is, as a share of its canvas from the floor — the head the stagger's stars turn
    /// over. Every figure shares one canvas and one floor line, but a rat's head is far lower than a knight's, and no data says where it is.
    /// The art is not readable on the CPU, so a small copy is drawn on the GPU and read back, once per sprite. Without a graphics device
    /// (the tests' batch mode) the share is <see cref="Unknown"/>.
    /// </summary>
    public static class ArtTop
    {
        /// <summary>The share used when the art cannot be read: about a standing figure's head.</summary>
        public const float Unknown = 0.85f;

        /// <summary>A pixel with more alpha than this is the art's.</summary>
        const byte Opaque = 128;

        const int ReadWidth = 48;
        const int ReadHeight = 64;

        static readonly Dictionary<Sprite, float> Tops = new Dictionary<Sprite, float>();

        public static float Of(Sprite sprite)
        {
            if (sprite == null)
            {
                return Unknown;
            }

            if (!Tops.TryGetValue(sprite, out float top))
            {
                top = Measure(sprite);
                Tops[sprite] = top;
            }

            return top;
        }

        static float Measure(Sprite sprite)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return Unknown;
            }

            Texture texture = sprite.texture;
            Rect part = sprite.textureRect;
            RenderTexture target = RenderTexture.GetTemporary(ReadWidth, ReadHeight, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(texture, target, new Vector2(part.width / texture.width, part.height / texture.height),
                new Vector2(part.x / texture.width, part.y / texture.height));

            RenderTexture before = RenderTexture.active;
            RenderTexture.active = target;
            var read = new Texture2D(ReadWidth, ReadHeight, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, ReadWidth, ReadHeight), 0, 0, false);
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(target);

            Color32[] pixels = read.GetPixels32();
            Object.Destroy(read);
            for (int y = ReadHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < ReadWidth; x++)
                {
                    if (pixels[y * ReadWidth + x].a > Opaque)
                    {
                        return (y + 1) / (float)ReadHeight;
                    }
                }
            }

            return Unknown;
        }
    }
}
