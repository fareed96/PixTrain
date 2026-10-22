using PixTrain.Render;
using UnityEngine;

namespace PixTrain.Game
{
    /// <summary>Yazılım tuvalini (PixelImage) Unity dokusuna aktarır. Satır sırası RawImage.uvRect ile çevrilir.</summary>
    public static class PixelTexture
    {
        /// <summary>Tuval satırları yukarıdan aşağı, Unity dokusu aşağıdan yukarı: dikey çevirme.</summary>
        public static readonly Rect FlippedUv = new Rect(0f, 1f, 1f, -1f);

        public static Texture2D Create(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "PixTrain " + width + "x" + height,
            };
        }

        public static Texture2D FromImage(PixelImage image)
        {
            var tex = Create(image.Width, image.Height);
            Upload(tex, image);
            return tex;
        }

        public static void Upload(Texture2D texture, PixelImage image)
        {
            texture.SetPixelData(image.Pixels, 0);
            texture.Apply(false, false);
        }
    }
}
