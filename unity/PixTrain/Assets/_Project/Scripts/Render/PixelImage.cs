using System;

namespace PixTrain.Render
{
    /// <summary>Renk: bellekte R,G,B,A bayt sırası (Unity RGBA32 / Color32 ile aynı).</summary>
    public static class Rgba
    {
        public const uint Transparent = 0u;

        public static uint Pack(int r, int g, int b, int a = 255) =>
            (uint)(r & 255) | ((uint)(g & 255) << 8) | ((uint)(b & 255) << 16) | ((uint)(a & 255) << 24);

        public static uint FromHex(string hex, int a = 255)
        {
            int v = Convert.ToInt32(hex.TrimStart('#'), 16);
            return Pack(v >> 16 & 255, v >> 8 & 255, v & 255, a);
        }

        public static int R(uint c) => (int)(c & 255);
        public static int G(uint c) => (int)(c >> 8 & 255);
        public static int B(uint c) => (int)(c >> 16 & 255);
        public static int A(uint c) => (int)(c >> 24);

        public static uint WithAlpha(uint c, int a) => (c & 0x00FFFFFFu) | ((uint)(a & 255) << 24);

        /// <summary>src'yi dst üzerine <paramref name="alpha"/> (0-255) opaklıkla karıştırır.</summary>
        public static uint Blend(uint dst, uint src, int alpha)
        {
            if (alpha <= 0) return dst;
            if (alpha >= 255) return src | 0xFF000000u;
            int ia = 255 - alpha;
            int da = A(dst);
            int r = (R(src) * alpha + R(dst) * ia + 127) / 255;
            int g = (G(src) * alpha + G(dst) * ia + 127) / 255;
            int b = (B(src) * alpha + B(dst) * ia + 127) / 255;
            int a = alpha + (da * ia + 127) / 255;
            return Pack(r, g, b, a);
        }
    }

    /// <summary>
    /// Basit yazılım tuvali. Sol üst köşe (0,0). Hem sprite'lar hem ekran bu sınıftır.
    /// Unity tarafında piksel dizisi doğrudan Texture2D'ye yüklenir.
    /// </summary>
    public sealed class PixelImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly uint[] Pixels;

        public PixelImage(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new uint[width * height];
        }

        public uint Get(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height ? Pixels[y * Width + x] : 0u;

        /// <summary>Pikseli doğrudan yazar (karıştırmadan).</summary>
        public void Put(int x, int y, uint c)
        {
            if ((uint)x < (uint)Width && (uint)y < (uint)Height) Pixels[y * Width + x] = c;
        }

        public void Clear(uint c)
        {
            for (int i = 0; i < Pixels.Length; i++) Pixels[i] = c;
        }

        /// <summary>Dikdörtgen doldurur; renk yarı saydamsa karıştırır. <paramref name="alpha"/> ek opaklıktır.</summary>
        public void FillRect(int x, int y, int w, int h, uint c, float alpha = 1f)
        {
            int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
            int x1 = Math.Min(Width, x + w), y1 = Math.Min(Height, y + h);
            if (x0 >= x1 || y0 >= y1) return;
            int a = (int)(Rgba.A(c) * alpha + 0.5f);
            if (a <= 0) return;
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * Width;
                if (a >= 255)
                {
                    uint opaque = c | 0xFF000000u;
                    for (int xx = x0; xx < x1; xx++) Pixels[row + xx] = opaque;
                }
                else
                {
                    for (int xx = x0; xx < x1; xx++) Pixels[row + xx] = Rgba.Blend(Pixels[row + xx], c, a);
                }
            }
        }

        public void Blit(PixelImage src, int dx, int dy, float alpha = 1f) =>
            BlitRegion(src, 0, 0, src.Width, src.Height, dx, dy, alpha);

        /// <summary>Kaynağın bir bölgesini saydamlığa göre karıştırarak kopyalar.</summary>
        public void BlitRegion(PixelImage src, int sx, int sy, int w, int h, int dx, int dy, float alpha = 1f)
        {
            if (dx < 0) { sx -= dx; w += dx; dx = 0; }
            if (dy < 0) { sy -= dy; h += dy; dy = 0; }
            if (dx + w > Width) w = Width - dx;
            if (dy + h > Height) h = Height - dy;
            if (sx + w > src.Width) w = src.Width - sx;
            if (sy + h > src.Height) h = src.Height - sy;
            if (w <= 0 || h <= 0) return;
            int alphaScale = (int)(alpha * 256f + 0.5f);
            for (int j = 0; j < h; j++)
            {
                int si = (sy + j) * src.Width + sx;
                int di = (dy + j) * Width + dx;
                for (int i = 0; i < w; i++)
                {
                    uint c = src.Pixels[si + i];
                    int a = Rgba.A(c);
                    if (a == 0) continue;
                    if (alphaScale < 256) a = a * alphaScale >> 8;
                    Pixels[di + i] = a >= 255 ? c : Rgba.Blend(Pixels[di + i], c, a);
                }
            }
        }

        /// <summary>Opak bir görüntüyü hızlıca kopyalar (zemin parçaları).</summary>
        public void CopyOpaque(PixelImage src, int dx, int dy)
        {
            int sx = 0, sy = 0, w = src.Width, h = src.Height;
            if (dx < 0) { sx -= dx; w += dx; dx = 0; }
            if (dy < 0) { sy -= dy; h += dy; dy = 0; }
            if (dx + w > Width) w = Width - dx;
            if (dy + h > Height) h = Height - dy;
            if (w <= 0 || h <= 0) return;
            for (int j = 0; j < h; j++) Array.Copy(src.Pixels, (sy + j) * src.Width + sx, Pixels, (dy + j) * Width + dx, w);
        }

        /// <summary>"destination-out": maskenin opak yerlerinde bu görüntünün alfasını siler (gece ışık delikleri).</summary>
        public void Erase(PixelImage mask, int dx, int dy, float alpha = 1f)
        {
            int scale = (int)(alpha * 256f + 0.5f);
            for (int j = 0; j < mask.Height; j++)
            {
                int y = dy + j;
                if ((uint)y >= (uint)Height) continue;
                for (int i = 0; i < mask.Width; i++)
                {
                    int x = dx + i;
                    if ((uint)x >= (uint)Width) continue;
                    int m = Rgba.A(mask.Pixels[j * mask.Width + i]) * scale >> 8;
                    if (m <= 0) continue;
                    int idx = y * Width + x;
                    uint d = Pixels[idx];
                    int na = Rgba.A(d) * (255 - Math.Min(255, m)) / 255;
                    Pixels[idx] = Rgba.WithAlpha(d, na);
                }
            }
        }

        /// <summary>"lighter": kaynağın rengini alfasıyla orantılı ekler (sıcak ışık parıltısı).</summary>
        public void Add(PixelImage src, int dx, int dy, float alpha)
        {
            for (int j = 0; j < src.Height; j++)
            {
                int y = dy + j;
                if ((uint)y >= (uint)Height) continue;
                for (int i = 0; i < src.Width; i++)
                {
                    int x = dx + i;
                    if ((uint)x >= (uint)Width) continue;
                    uint s = src.Pixels[j * src.Width + i];
                    float k = Rgba.A(s) / 255f * alpha;
                    if (k <= 0f) continue;
                    int idx = y * Width + x;
                    uint d = Pixels[idx];
                    Pixels[idx] = Rgba.Pack(
                        Math.Min(255, Rgba.R(d) + (int)(Rgba.R(s) * k)),
                        Math.Min(255, Rgba.G(d) + (int)(Rgba.G(s) * k)),
                        Math.Min(255, Rgba.B(d) + (int)(Rgba.B(s) * k)),
                        Rgba.A(d));
                }
            }
        }

        /// <summary>"multiply": bütün görüntüyü bir renkle çarpar (şafak/akşam tonu).</summary>
        public void MultiplyAll(int r, int g, int b, float amount)
        {
            if (amount <= 0f) return;
            int fr = (int)(256 * (1f - amount + amount * r / 255f));
            int fg = (int)(256 * (1f - amount + amount * g / 255f));
            int fb = (int)(256 * (1f - amount + amount * b / 255f));
            for (int i = 0; i < Pixels.Length; i++)
            {
                uint d = Pixels[i];
                Pixels[i] = Rgba.Pack(Rgba.R(d) * fr >> 8, Rgba.G(d) * fg >> 8, Rgba.B(d) * fb >> 8, Rgba.A(d));
            }
        }

        public PixelImage Clone()
        {
            var c = new PixelImage(Width, Height);
            Array.Copy(Pixels, c.Pixels, Pixels.Length);
            return c;
        }
    }
}
