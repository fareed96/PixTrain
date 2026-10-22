using System;
using System.Collections.Generic;

namespace PixTrain.Render
{
    /// <summary>Piksel çizerken kullanılan küçük yardımcı: palet kodu ya da renk ile dikdörtgen.</summary>
    public sealed class Painter
    {
        public readonly PixelImage Image;

        public Painter(PixelImage image)
        {
            Image = image;
        }

        /// <summary>Palet koduyla doldurur (karıştırmadan yazar; '#' gibi yarı saydamlar da öyle yazılır).</summary>
        public void P(int x, int y, char code, int w = 1, int h = 1) => Raw(x, y, Palette.Get(code), w, h);

        public void Raw(int x, int y, uint color, int w = 1, int h = 1)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Image.Put(xx, yy, color);
        }
    }

    /// <summary>Metin tablosundan ya da çizim fonksiyonundan sprite üretir.</summary>
    public static class SpriteFactory
    {
        /// <summary>Her karakter bir piksel; '.' ve ' ' saydam. <paramref name="recolor"/> kodları değiştirir.</summary>
        public static PixelImage FromRows(string[] rows, IDictionary<char, char> recolor = null)
        {
            int w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            var img = new PixelImage(w, rows.Length);
            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (ch == '.' || ch == ' ') continue;
                    if (recolor != null && recolor.TryGetValue(ch, out var mapped)) ch = mapped;
                    img.Put(x, y, Palette.Get(ch));
                }
            }
            return img;
        }

        public static PixelImage Draw(int w, int h, Action<Painter> paint)
        {
            var img = new PixelImage(w, h);
            paint(new Painter(img));
            return img;
        }

        public static PixelImage FlipX(PixelImage src)
        {
            var img = new PixelImage(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                    img.Pixels[y * src.Width + (src.Width - 1 - x)] = src.Pixels[y * src.Width + x];
            return img;
        }

        public static PixelImage FlipY(PixelImage src)
        {
            var img = new PixelImage(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                Array.Copy(src.Pixels, y * src.Width, img.Pixels, (src.Height - 1 - y) * src.Width, src.Width);
            return img;
        }

        /// <summary>Opak pikselleri tek renge boyar (gölge).</summary>
        public static PixelImage Silhouette(PixelImage src, uint color)
        {
            var img = new PixelImage(src.Width, src.Height);
            for (int i = 0; i < src.Pixels.Length; i++)
                if (Rgba.A(src.Pixels[i]) > 0) img.Pixels[i] = color;
            return img;
        }
    }
}
