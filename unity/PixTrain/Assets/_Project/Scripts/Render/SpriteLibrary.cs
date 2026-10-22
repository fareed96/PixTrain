using System;
using System.Collections.Generic;
using PixTrain.Core.Utils;

namespace PixTrain.Render
{
    /// <summary>Pencereleri gece yanan bina/ev sprite'ı.</summary>
    public sealed class BuildingSprite
    {
        public PixelImage Image;
        /// <summary>Pencere dikdörtgenleri: x, y, genişlik, yükseklik (sprite içi).</summary>
        public readonly List<int[]> Windows = new List<int[]>();
    }

    /// <summary>
    /// Oyundaki bütün piksel çizimleri. Açılışta bir kez üretilir (prototipteki buildSprites ile aynı).
    /// </summary>
    public sealed class SpriteLibrary
    {
        private static SpriteLibrary _instance;

        public static SpriteLibrary Instance => _instance ?? (_instance = new SpriteLibrary());

        public readonly Dictionary<string, PixelImage> Locomotives = new Dictionary<string, PixelImage>();
        /// <summary>Lokomotifin ön ucundan bacaya/egzoza/pantografa uzaklık.</summary>
        public readonly Dictionary<string, int> ChimneyOffset = new Dictionary<string, int> { ["steam"] = 6, ["diesel"] = 15, ["electric"] = 12 };
        public readonly PixelImage Tender, WagonPassenger, WagonFreight;
        public readonly PixelImage Tree, TreeAutumn, Pine, PineSnow, PineSnow2, Palm, Bush, Flowers, Rock, RockBig, RockSnow, Logs;
        public readonly PixelImage[] Cow, CowLeft;
        public readonly PixelImage Sheep, Lamp, Barrier, BarrierLeft, Boat, Lighthouse, Mill, Warn;
        public readonly PixelImage[] Cars, CarsLeft, Coin, Puff, DieselPuff, Clouds;
        public readonly PixelImage[] Oncoming;
        public readonly Dictionary<string, PixelImage> Icons = new Dictionary<string, PixelImage>();
        public readonly Dictionary<int, PixelImage> Arrows = new Dictionary<int, PixelImage>();
        public readonly BuildingSprite Station;
        public readonly List<BuildingSprite> Houses = new List<BuildingSprite>();
        public readonly List<BuildingSprite> Buildings = new List<BuildingSprite>();
        public readonly List<BuildingSprite> Towers = new List<BuildingSprite>();

        private readonly Dictionary<PixelImage, PixelImage> _shadows = new Dictionary<PixelImage, PixelImage>();
        private readonly Dictionary<int, PixelImage> _lights = new Dictionary<int, PixelImage>();
        private readonly Dictionary<int, PixelImage> _glows = new Dictionary<int, PixelImage>();
        private readonly Dictionary<long, PixelImage> _cones = new Dictionary<long, PixelImage>();

        private SpriteLibrary()
        {
            Locomotives["steam"] = SpriteFactory.FromRows(SpriteData.LocoSteam);
            Locomotives["diesel"] = MakeDiesel('o', 'U', 'u');
            Locomotives["electric"] = MakeElectric();
            Tender = SpriteFactory.FromRows(SpriteData.Tender);
            WagonPassenger = SpriteFactory.FromRows(WagonRows(true));
            WagonFreight = SpriteFactory.FromRows(WagonRows(false));

            Tree = SpriteFactory.FromRows(SpriteData.Tree);
            TreeAutumn = SpriteFactory.FromRows(SpriteData.Tree, new Dictionary<char, char> { ['e'] = 'u', ['E'] = 'o', ['l'] = 'y' });
            Pine = SpriteFactory.FromRows(SpriteData.Pine);
            PineSnow = SpriteFactory.FromRows(SpriteData.Pine, new Dictionary<char, char> { ['l'] = 'w' });
            var pine2 = (string[])SpriteData.Pine.Clone();
            for (int i = 0; i < pine2.Length; i++) if (i % 3 == 1) pine2[i] = pine2[i].Replace('E', 'w');
            PineSnow2 = SpriteFactory.FromRows(pine2);
            Palm = SpriteFactory.FromRows(SpriteData.Palm);
            Bush = SpriteFactory.FromRows(SpriteData.Bush);
            Flowers = SpriteFactory.FromRows(SpriteData.Flowers);
            Rock = SpriteFactory.FromRows(SpriteData.Rock);
            RockBig = SpriteFactory.FromRows(SpriteData.Rockb);
            RockSnow = SpriteFactory.FromRows(SpriteData.Rockb, new Dictionary<char, char> { ['S'] = 'w', ['s'] = 'S' });
            Logs = SpriteFactory.FromRows(SpriteData.Logs);
            Cow = new[] { SpriteFactory.FromRows(SpriteData.Cow), SpriteFactory.FromRows(SpriteData.Cow2) };
            CowLeft = new[] { SpriteFactory.FlipX(Cow[0]), SpriteFactory.FlipX(Cow[1]) };
            Sheep = SpriteFactory.FromRows(SpriteData.Sheep);
            Cars = new PixelImage[6];
            CarsLeft = new PixelImage[6];
            char[] carColors = { 'R', 'B', 'Y', 'l', 'w', 'm' };
            for (int i = 0; i < carColors.Length; i++)
            {
                Cars[i] = SpriteFactory.FromRows(SpriteData.Car, new Dictionary<char, char> { ['R'] = carColors[i] });
                CarsLeft[i] = SpriteFactory.FlipX(Cars[i]);
            }
            Lamp = SpriteFactory.FromRows(SpriteData.Lamp);
            Barrier = SpriteFactory.FromRows(SpriteData.Barrier);
            BarrierLeft = SpriteFactory.FlipX(Barrier);
            Boat = SpriteFactory.FromRows(SpriteData.Boat);
            Lighthouse = SpriteFactory.FromRows(SpriteData.Lighthouse);
            Mill = SpriteFactory.FromRows(SpriteData.Mill);
            Warn = SpriteFactory.FromRows(SpriteData.Warn);
            Coin = new[]
            {
                SpriteFactory.FromRows(SpriteData.Coin0), SpriteFactory.FromRows(SpriteData.Coin1),
                SpriteFactory.FromRows(SpriteData.Coin2), SpriteFactory.FromRows(SpriteData.Coin3),
            };
            Icons["coin"] = SpriteFactory.FromRows(SpriteData.IconCoin);
            Icons["fuel"] = SpriteFactory.FromRows(SpriteData.IconFuel);
            Icons["person"] = SpriteFactory.FromRows(SpriteData.IconPerson);
            Icons["coal"] = SpriteFactory.FromRows(SpriteData.IconCoal);
            Icons["wheat"] = SpriteFactory.FromRows(SpriteData.IconWheat);
            Icons["factory"] = SpriteFactory.FromRows(SpriteData.IconFactory);
            Icons["city"] = SpriteFactory.FromRows(SpriteData.IconCity);
            Icons["star"] = SpriteFactory.FromRows(SpriteData.IconStar);
            Icons["starOff"] = SpriteFactory.FromRows(SpriteData.IconStarOff);
            Icons["sun"] = SpriteFactory.FromRows(SpriteData.IconSun);
            Icons["moon"] = SpriteFactory.FromRows(SpriteData.IconMoon);
            Icons["train"] = SpriteFactory.FromRows(SpriteData.IconTrain);
            Icons["lock"] = SpriteFactory.FromRows(SpriteData.IconLock);
            Icons["cargo"] = SpriteFactory.FromRows(SpriteData.IconCargo);
            Arrows[0] = SpriteFactory.FromRows(SpriteData.ArrowStraight);
            Arrows[-1] = SpriteFactory.FromRows(SpriteData.ArrowLeft);
            Arrows[1] = SpriteFactory.FromRows(SpriteData.ArrowRight);
            Puff = new[] { MakePuff(1), MakePuff(2), MakePuff(3), MakePuff(4) };
            DieselPuff = new[] { SpriteFactory.Silhouette(MakePuff(1), Palette.Get('G')), SpriteFactory.Silhouette(MakePuff(2), Palette.Get('G')), SpriteFactory.Silhouette(MakePuff(3), Palette.Get('G')) };
            var cloudRng = new SeededRandom(7);
            Clouds = new PixelImage[4];
            for (int i = 0; i < Clouds.Length; i++) Clouds[i] = MakeCloud(cloudRng);
            Oncoming = new[]
            {
                SpriteFactory.FlipY(MakeDiesel('R', 'r', 'r')), SpriteFactory.FlipY(WagonFreight),
                SpriteFactory.FlipY(WagonFreight), SpriteFactory.FlipY(WagonPassenger),
            };
            Station = MakeStation();

            var rng = new SeededRandom(1234);
            char[][] roofs = { new[] { 'R', 'r' }, new[] { 'U', 'u' }, new[] { 'B', 'b' }, new[] { 'N', 'n' }, new[] { 'G', 'g' }, new[] { 'm', 'p' } };
            char[] walls = { 't', 'S', 'T', 'z', 'w' };
            for (int i = 0; i < 18; i++)
                Houses.Add(MakeHouse(rng, 11 + rng.NextInt(6), roofs[rng.NextInt(roofs.Length)], walls[rng.NextInt(walls.Length)]));
            char[] bWalls = { 'N', 'u', 'G', 'x', 'p', 'n', 'b', 'T' }, bRoofs = { 'G', 's', 'g', 'N' };
            for (int i = 0; i < 24; i++)
                Buildings.Add(MakeBuilding(rng, 12 + rng.NextInt(8), 7 + rng.NextInt(4), 8 + rng.NextInt(10), bWalls[rng.NextInt(bWalls.Length)], bRoofs[rng.NextInt(bRoofs.Length)]));
            for (int i = 0; i < 14; i++)
                Towers.Add(MakeBuilding(rng, 14 + rng.NextInt(7), 8 + rng.NextInt(4), 18 + rng.NextInt(14), bWalls[rng.NextInt(bWalls.Length)], bRoofs[rng.NextInt(bRoofs.Length)]));
        }

        // ---------------------------------------------------------------- önbellekli üretilenler

        public PixelImage ShadowOf(PixelImage image)
        {
            if (!_shadows.TryGetValue(image, out var s)) { s = SpriteFactory.Silhouette(image, Palette.Shadow); _shadows[image] = s; }
            return s;
        }

        /// <summary>Gece maskesinde ışık deliği (kademeli alfa).</summary>
        public PixelImage Light(int r)
        {
            if (!_lights.TryGetValue(r, out var img)) { img = MakeLight(r, 0, 0, 0); _lights[r] = img; }
            return img;
        }

        /// <summary>Sıcak renkli parıltı (eklenerek çizilir).</summary>
        public PixelImage Glow(int r)
        {
            if (!_glows.TryGetValue(r, out var img)) { img = MakeLight(r, 247, 150, 60); _glows[r] = img; }
            return img;
        }

        public PixelImage Cone(int range, int spread)
        {
            long key = (long)range << 32 | (uint)spread;
            if (!_cones.TryGetValue(key, out var img)) { img = MakeCone(range, spread); _cones[key] = img; }
            return img;
        }

        // ---------------------------------------------------------------- çizimler

        private static string[] WagonRows(bool passenger)
        {
            var rows = new List<string> { "....kddk....", ".kkkkkkkkkk." };
            if (passenger)
            {
                for (int r = 2; r <= 18; r++) rows.Add(r >= 4 && r <= 16 && r % 2 == 0 ? ".kbBddddBbk." : ".kbBBccBBbk.");
            }
            else
            {
                rows.Add(".kNNNNNNNNk.");
                for (int r = 3; r <= 17; r++) rows.Add(r % 4 == 1 ? ".kNqnqqnqNk." : ".kNqqqqqqNk.");
                rows.Add(".kNNNNNNNNk.");
            }
            rows.Add(".kkkkkkkkkk.");
            rows.Add("....kddk....");
            return rows.ToArray();
        }

        private static PixelImage MakeDiesel(char body, char vent, char rear) => SpriteFactory.Draw(12, 28, p =>
        {
            p.P(3, 0, 'k', 6, 1); p.P(2, 1, 'k', 8, 27); p.P(1, 4, 'k', 10, 20);
            p.P(3, 1, body, 6, 26); p.P(2, 4, body, 8, 20);
            p.P(3, 1, 'Y', 6, 1); p.P(4, 2, 'k', 4, 1);
            p.P(3, 4, 'c', 6, 1); p.P(3, 5, 'k', 6, 6); p.P(4, 5, 'S', 4, 5); p.P(4, 5, 'w', 4, 1);
            p.P(2, 12, 'Y', 1, 12); p.P(9, 12, 'Y', 1, 12);
            for (int r = 13; r < 24; r += 2) p.P(4, r, vent, 4, 1);
            p.P(5, 15, 'k', 2, 2);
            p.P(3, 25, rear, 6, 2); p.P(5, 27, 'd', 2, 1);
            foreach (int r in new[] { 6, 7, 18, 19 }) { p.P(1, r, 'g'); p.P(10, r, 'g'); }
        });

        private static PixelImage MakeElectric() => SpriteFactory.Draw(12, 30, p =>
        {
            p.P(3, 0, 'k', 6, 1); p.P(2, 1, 'k', 8, 29);
            p.P(3, 1, 'B', 6, 28);
            p.P(3, 1, 'c', 6, 2); p.P(3, 3, 'k', 6, 1);
            p.P(3, 4, 'S', 6, 22); p.P(3, 4, 'b', 1, 22); p.P(8, 4, 'b', 1, 22);
            p.P(4, 9, 'G', 4, 1); p.P(4, 15, 'G', 4, 1); p.P(5, 10, 'k', 2, 5); p.P(4, 12, 'k', 4, 1);
            for (int r = 18; r < 25; r += 2) p.P(5, r, 's', 2, 1);
            p.P(3, 26, 'k', 6, 1); p.P(3, 27, 'c', 6, 2);
            p.P(5, 29, 'd', 2, 1);
            foreach (int r in new[] { 6, 7, 21, 22 }) { p.P(1, r, 'g'); p.P(10, r, 'g'); }
        });

        public static BuildingSprite MakeHouse(IRandom rng, int w, char[] roof, char wall)
        {
            const int roofD = 8, faceH = 6;
            int h = roofD + faceH + 1;
            var b = new BuildingSprite();
            b.Image = SpriteFactory.Draw(w + 2, h, p =>
            {
                p.P(w, 2, '#', 2, h - 2); p.P(1, h - 1, '#', w, 1);
                p.P(0, 0, 'k', w, roofD + faceH);
                p.P(1, 1, roof[0], w - 2, 3); p.P(1, 4, roof[1], w - 2, roofD - 4);
                p.P(1, 4, 'k', w - 2, 1);
                for (int x = 2; x < w - 2; x += 3) p.P(x, 1, roof[1], 1, 2);
                if (rng.NextFloat() < 0.5f) { p.P(w - 5, 1, 'g', 2, 3); p.P(w - 5, 1, 'k', 2, 1); }
                p.P(1, roofD, wall, w - 2, faceH - 1);
                int dx = 2 + rng.NextInt(Math.Max(1, w - 6));
                p.P(dx, roofD + 2, 'n', 2, faceH - 3);
                for (int x = 2; x < w - 2; x += 4)
                {
                    if (Math.Abs(x - dx) < 3) continue;
                    p.P(x, roofD + 2, 'b', 2, 2);
                    b.Windows.Add(new[] { x, roofD + 2, 2, 2 });
                }
            });
            return b;
        }

        public static BuildingSprite MakeBuilding(IRandom rng, int w, int roofD, int faceH, char wall, char roof)
        {
            int h = roofD + faceH + 1;
            var b = new BuildingSprite();
            b.Image = SpriteFactory.Draw(w + 3, h, p =>
            {
                p.P(w, 3, '#', 3, h - 3); p.P(1, h - 1, '#', w, 1);
                p.P(0, 0, 'k', w, roofD + faceH);
                p.P(1, 1, roof, w - 2, roofD - 1);
                p.P(1, 1, 'S', w - 2, 1);
                p.P(1, roofD - 1, 'g', w - 2, 1);
                int ac = rng.NextInt(3);
                for (int i = 0; i < ac; i++)
                {
                    int ax = 2 + rng.NextInt(Math.Max(1, w - 5)), ay = 2 + rng.NextInt(Math.Max(1, roofD - 4));
                    p.P(ax, ay, 'S', 2, 2); p.P(ax, ay + 1, 's', 2, 1);
                }
                if (rng.NextFloat() < 0.3f) { p.P(w - 6, 2, 'N', 4, 3); p.P(w - 6, 2, 'n', 4, 1); }
                p.P(1, roofD, wall, w - 2, faceH - 1);
                int sx = 2 + (w % 3 == 0 ? 1 : 0);
                for (int y = roofD + 2; y < roofD + faceH - 3; y += 3)
                    for (int x = sx; x < w - 3; x += 3)
                    {
                        p.P(x, y, 'd', 2, 2);
                        b.Windows.Add(new[] { x, y, 2, 2 });
                    }
                p.P((w >> 1) - 1, roofD + faceH - 3, 'q', 3, 2);
            });
            return b;
        }

        private static BuildingSprite MakeStation()
        {
            var b = new BuildingSprite();
            b.Image = SpriteFactory.Draw(30, 24, p =>
            {
                p.P(28, 3, '#', 2, 21); p.P(1, 23, '#', 28, 1);
                p.P(0, 0, 'k', 28, 23);
                p.P(1, 1, 'R', 26, 5); p.P(1, 6, 'r', 26, 6); p.P(1, 6, 'k', 26, 1);
                for (int x = 3; x < 26; x += 4) p.P(x, 1, 'r', 1, 4);
                p.P(12, 2, 'w', 4, 3); p.P(13, 3, 'k'); p.P(14, 3, 'k');
                p.P(1, 12, 't', 26, 10);
                p.P(1, 12, 'T', 26, 1);
                for (int x = 3; x < 26; x += 5)
                {
                    if (x == 13) continue;
                    p.P(x, 14, 'b', 3, 4); p.P(x, 14, 'B', 3, 1);
                    b.Windows.Add(new[] { x, 15, 3, 3 });
                }
                p.P(12, 15, 'n', 4, 7); p.P(13, 16, 'q', 2, 6);
            });
            return b;
        }

        private static PixelImage MakePuff(int r)
        {
            int s = r * 2 + 1;
            return SpriteFactory.Draw(s, s, p =>
            {
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        int dx = x - r, dy = y - r;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d > r + 0.2f) continue;
                        char c = d < r * 0.5f && dx <= 0 && dy <= 0 ? 'w' : (dx + dy > r * 0.6f ? 's' : 'S');
                        p.P(x, y, c);
                    }
            });
        }

        private static PixelImage MakeLight(int r, int cr, int cg, int cb)
        {
            int s = r * 2 + 1;
            var img = new PixelImage(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = (float)Math.Sqrt((x - r) * (x - r) + (y - r) * (y - r)) / r;
                    if (d > 1f) continue;
                    float a = d < 0.4f ? 1f : d < 0.7f ? 0.66f : 0.33f;
                    img.Put(x, y, Rgba.Pack(cr, cg, cb, (int)(a * 255)));
                }
            return img;
        }

        private static PixelImage MakeCone(int range, int spread)
        {
            int w = spread * 2 + 7;
            var img = new PixelImage(w, range);
            float cx = w / 2f;
            for (int py = 0; py < range; py++)
            {
                int d = range - py;
                float f = (float)d / range, hw = 2.5f + spread * f;
                for (int px = 0; px < w; px++)
                {
                    float dx = Math.Abs(px + 0.5f - cx);
                    if (dx > hw) continue;
                    float a = 1f - f * 0.85f;
                    if (dx > hw * 0.7f) a *= 0.55f;
                    a = (float)Math.Ceiling(a * 4f) / 4f;
                    img.Put(px, py, Rgba.Pack(0, 0, 0, (int)(Math.Min(1f, a) * 255)));
                }
            }
            return img;
        }

        private static PixelImage MakeCloud(IRandom rng)
        {
            const int w = 64, h = 34;
            int blobs = 5 + rng.NextInt(4);
            var cs = new float[blobs, 3];
            for (int i = 0; i < blobs; i++)
            {
                cs[i, 0] = 12 + rng.NextFloat() * 40;
                cs[i, 1] = 10 + rng.NextFloat() * 14;
                cs[i, 2] = 6 + rng.NextFloat() * 9;
            }
            return SpriteFactory.Draw(w, h, p =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        for (int i = 0; i < blobs; i++)
                        {
                            float dx = x - cs[i, 0], dy = y - cs[i, 1];
                            if (dx * dx + dy * dy < cs[i, 2] * cs[i, 2]) { p.P(x, y, 'k'); break; }
                        }
            });
        }
    }
}
