using System;
using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Utils;

namespace PixTrain.Render
{
    /// <summary>Hareket ipuçlarındaki oklar: -1 sol, 1 sağ, 0 yukarı, 2 aşağı.</summary>
    public static class GestureArt
    {
        private static PixelImage _down;

        public static PixelImage Arrow(int direction)
        {
            var arrows = SpriteLibrary.Instance.Arrows;
            if (direction == 2) return _down ?? (_down = SpriteFactory.FlipY(arrows[0]));
            return arrows[direction];
        }
    }

    /// <summary>Arayüzde kullanılan piksel çizimler: logo, şehir silüeti, garaj önizlemesi.</summary>
    public static class UiArt
    {
        private static readonly Dictionary<char, string[]> LogoGlyphs = new Dictionary<char, string[]>
        {
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
        };

        /// <summary>"PIXTRAIN" logosu: 5x7 harfler, 2x büyütülmüş, konturlu ve gölgeli (102x26).</summary>
        public static PixelImage Logo()
        {
            const string text = "PIXTRAIN";
            const int scale = 2;
            var img = new PixelImage(102, 26);
            char[] rowColors = { 'Y', 'Y', 'y', 'y', 'o', 'o', 'u' };
            void Put(int ox, int oy, Func<int, uint> color)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var g = LogoGlyphs[text[i]];
                    for (int r = 0; r < 7; r++)
                        for (int q = 0; q < 5; q++)
                            if (g[r][q] == '#') img.FillRect((1 + i * 6 + q) * scale + ox, (1 + r) * scale + oy, scale, scale, color(r));
                }
            }
            uint k = Palette.Get('k');
            int[,] outline = { { -2, 0 }, { 2, 0 }, { 0, -2 }, { 0, 2 }, { 2, 2 }, { -2, 2 }, { 2, -2 }, { -2, -2 } };
            for (int o = 0; o < outline.GetLength(0); o++) Put(outline[o, 0] + 2, outline[o, 1] + 2, r => k);
            Put(4, 6, r => k);
            Put(2, 4, r => Palette.Get('r'));
            Put(2, 2, r => Palette.Get(rowColors[r]));
            return img;
        }

        private static readonly Dictionary<int, PixelImage> Skylines = new Dictionary<int, PixelImage>();

        /// <summary>Hat haritasındaki şehir silüeti (42x22). Seviye 1 köy, 5 metropol. Aynı girdi aynı görüntüyü döner.</summary>
        public static PixelImage CitySkyline(int level, int seed)
        {
            int key = seed * 16 + level;
            if (Skylines.TryGetValue(key, out var cached)) return cached;
            var image = DrawSkyline(level, seed);
            Skylines[key] = image;
            return image;
        }

        private static PixelImage DrawSkyline(int level, int seed)
        {
            const int w = 42, h = 22;
            var rng = new SeededRandom(seed * 97 + level * 13);
            var img = new PixelImage(w, h);
            for (int y = 0; y < h; y++)
                img.FillRect(0, y, w, 1, y < 8 ? Rgba.FromHex("2b3a5c") : y < 14 ? Rgba.FromHex("3a4a70") : Rgba.FromHex("4a5a80"));
            img.FillRect(30, 3, 2, 2, Palette.Get('Y'));
            img.FillRect(0, h - 3, w, 3, Palette.Get('E'));
            img.FillRect(0, h - 3, w, 1, Palette.Get('l'));

            int lv = Math.Max(1, Math.Min(5, level));
            int[] buildings = { 0, 0, 1, 3, 5, 7 }, houses = { 0, 2, 4, 3, 2, 1 };
            char[] walls = { 'N', 'u', 'G', 'x', 'p' }, roofs = { 'G', 's', 'g' };
            char[][] housePairs = { new[] { 'R', 'r' }, new[] { 'U', 'u' }, new[] { 'B', 'b' } };
            var items = new List<KeyValuePair<float, KeyValuePair<float, PixelImage>>>();
            for (int i = 0; i < buildings[lv]; i++)
            {
                int bw = 6 + rng.NextInt(4), fh = 4 + rng.NextInt(lv * 3 + 2);
                float x = 2 + rng.NextFloat() * (w - bw - 4), y = h - 2 - rng.NextFloat() * 2;
                var b = SpriteLibrary.MakeBuilding(rng, bw, 3, fh, walls[rng.NextInt(walls.Length)], roofs[rng.NextInt(roofs.Length)]);
                items.Add(new KeyValuePair<float, KeyValuePair<float, PixelImage>>(y, new KeyValuePair<float, PixelImage>(x, b.Image)));
            }
            for (int i = 0; i < houses[lv]; i++)
            {
                float x = 2 + rng.NextFloat() * (w - 10), y = h - 1 - rng.NextFloat() * 2;
                var house = SpriteLibrary.MakeHouse(rng, 7, housePairs[rng.NextInt(housePairs.Length)], 't');
                items.Add(new KeyValuePair<float, KeyValuePair<float, PixelImage>>(y, new KeyValuePair<float, PixelImage>(x, house.Image)));
            }
            items.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var it in items)
                img.Blit(it.Value.Value, MathUtil.RoundHalfUp(it.Value.Key), MathUtil.RoundHalfUp(it.Key - it.Value.Value.Height));
            if (lv >= 5)
            {
                img.FillRect(36, 2, 1, 16, Palette.Get('y'));
                img.FillRect(28, 2, 9, 1, Palette.Get('y'));
                img.FillRect(29, 3, 1, 3, Palette.Get('k'));
            }
            if (lv <= 2)
            {
                var tree = SpriteLibrary.Instance.Tree;
                img.Blit(tree, 30, h - 13);
                img.Blit(tree, 2, h - 12);
            }
            return img;
        }

        /// <summary>Garajdaki dikey tren önizlemesi (raylar üzerinde lokomotif ve vagonlar).</summary>
        public static PixelImage TrainPreview(string locomotiveKey, IReadOnlyList<WagonType> wagons)
        {
            var lib = SpriteLibrary.Instance;
            var parts = new List<PixelImage> { lib.Locomotives[locomotiveKey] };
            if (locomotiveKey == "steam") parts.Add(lib.Tender);
            foreach (var w in wagons) parts.Add(w == WagonType.Passenger ? lib.WagonPassenger : lib.WagonFreight);
            int h = 6;
            foreach (var p in parts) h += p.Height;
            var img = new PixelImage(24, h);
            img.Clear(Palette.Get('E'));
            for (int y = 0; y < h; y++)
            {
                img.FillRect(6, y, 12, 1, Palette.Get('G'));
                if (y % 4 == 0) img.FillRect(7, y, 11, 1, Palette.Get('n'));
                img.FillRect(9, y, 1, 1, Palette.Get('S'));
                img.FillRect(15, y, 1, 1, Palette.Get('S'));
            }
            int yy = 3;
            foreach (var p in parts)
            {
                img.Blit(lib.ShadowOf(p), 8, yy + 2);
                img.Blit(p, 6, yy);
                yy += p.Height;
            }
            return img;
        }
    }
}
