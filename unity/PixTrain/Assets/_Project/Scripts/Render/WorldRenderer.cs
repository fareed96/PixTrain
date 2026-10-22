using System;
using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Services;
using PixTrain.Core.Utils;

namespace PixTrain.Render
{
    /// <summary>
    /// Sefer sahnesini 160 piksel genişliğinde bir tuvale çizer (prototipteki render() ile aynı sıra ve görünüm).
    /// Dünya y'si yukarı doğru artar; trenin burnu ekranda <c>trainScreenY</c> satırındadır.
    /// </summary>
    public sealed class WorldRenderer
    {
        public const int Width = 160;
        private const int CH = RunView.ChunkHeight;

        private struct LightSource
        {
            public int X, Y, R;
            public bool Warning;
            public LightSource(int x, int y, int r, bool warning = false) { X = x; Y = y; R = r; Warning = warning; }
        }

        private struct LitPixel
        {
            public int X, Y, W, H;
            public char Color;
            public LitPixel(int x, int y, int w, int h, char color) { X = x; Y = y; W = w; H = h; Color = color; }
        }

        private static readonly Dictionary<GateKind, char[]> CanopyColors = new Dictionary<GateKind, char[]>
        {
            [GateKind.Town] = new[] { 'b', 'B' }, [GateKind.Mine] = new[] { 'g', 'G' }, [GateKind.Farm] = new[] { 'e', 'l' },
            [GateKind.City] = new[] { 'p', 'm' }, [GateKind.Factory] = new[] { 'u', 'U' }, [GateKind.Fuel] = new[] { 'r', 'R' },
        };

        private static readonly Dictionary<GateKind, string> CanopyIcons = new Dictionary<GateKind, string>
        {
            [GateKind.Town] = "person", [GateKind.Mine] = "coal", [GateKind.Farm] = "wheat",
            [GateKind.City] = "city", [GateKind.Factory] = "factory", [GateKind.Fuel] = "fuel",
        };

        private readonly SpriteLibrary _lib = SpriteLibrary.Instance;
        private readonly List<LightSource> _lights = new List<LightSource>();
        private readonly List<LitPixel> _lit = new List<LitPixel>();
        private readonly List<PixelImage> _parts = new List<PixelImage>();
        private readonly Random _shake = new Random(5);
        private PixelImage _mask;
        private PixelImage _shift;

        /// <summary>Ekran sarsıntısını kapatır (erişilebilirlik).</summary>
        public bool ReduceMotion;

        public void Render(PixelImage screen, RunView view, int trainScreenY)
        {
            var run = view.Run;
            var services = view.Services;
            var runs = services.Runs;
            var B = services.Catalog.Balance;
            int ty = trainScreenY, H = screen.Height;
            int cam = MathUtil.RoundHalfUp(run.Y);
            float t = run.Time;
            float dk = DayCycle.Darkness(run.TimeOfDay);
            bool night = DayCycle.IsNight(run.TimeOfDay);
            int yLo = cam - (H - ty) - 10, yHi = cam + ty + 10;
            Func<float, int> sy = y => ty - MathUtil.RoundHalfUp(y - cam);
            _lights.Clear();
            _lit.Clear();

            screen.Clear(Palette.Get('k'));

            // ---- zemin parçaları
            for (int k = FloorDiv(yLo, CH); k <= FloorDiv(yHi, CH); k++)
            {
                if (!view.Chunks.TryGetValue(k, out var chunk)) { chunk = RenderChunk(view, k); view.Chunks[k] = chunk; }
                screen.CopyOpaque(chunk, 0, ty - ((k + 1) * CH - cam));
            }

            // ---- sıradaki makasın yolu (sarı ray) ve ok
            var junction = runs.NextJunction(run);
            if (junction != null && junction.Y < yHi && run.Phase == RunPhase.Running)
            {
                int from = run.Lane, to = MathUtil.Clamp(run.Lane + run.SwitchDir, 0, 2);
                uint yellow = Palette.Get('Y');
                float startY = Math.Max(junction.Y - 18, cam);
                for (float wy = startY; wy <= junction.Y + B.JunctionLength + 10; wy++)
                {
                    float x = B.LaneX[from];
                    if (wy >= junction.Y)
                        x = wy > junction.Y + B.JunctionLength ? B.LaneX[to] : MathUtil.Lerp(B.LaneX[from], B.LaneX[to], MathUtil.Smooth((wy - junction.Y) / B.JunctionLength));
                    int rx = MathUtil.RoundHalfUp(x), s = sy(wy);
                    screen.FillRect(rx - 3, s, 1, 1, yellow);
                    screen.FillRect(rx + 3, s, 1, 1, yellow);
                }
                bool blink = (int)Math.Floor(t * 6) % 2 == 0;
                if (!run.IsAttract && (blink || junction.Y - run.Y > 60))
                    screen.Blit(_lib.Arrows[run.SwitchDir], (int)B.LaneX[from] - 3, sy(junction.Y) - 4);
            }

            // ---- peron zeminleri, altınlar, tehlikeler
            foreach (var g in run.Gates)
            {
                if (g.Y < yLo - 30) continue;
                if (g.Y > yHi + 30) break;
                if (g.Gone || g.Kind == GateKind.Cow) continue;
                int s = sy(g.Y), x = g.Lane >= 0 ? (int)B.LaneX[g.Lane] : 80;
                var cat = services.Catalog.Station(g.Kind).Category;
                if (cat == GateCategory.Pickup || cat == GateCategory.Delivery || cat == GateCategory.Fuel)
                {
                    screen.FillRect(x - 13, s - 9, 5, 18, Palette.Get('k')); screen.FillRect(x + 8, s - 9, 5, 18, Palette.Get('k'));
                    screen.FillRect(x - 12, s - 8, 3, 16, Palette.Get('S')); screen.FillRect(x + 9, s - 8, 3, 16, Palette.Get('S'));
                    screen.FillRect(x - 9, s - 8, 1, 16, Palette.Get('Y')); screen.FillRect(x + 8, s - 8, 1, 16, Palette.Get('Y'));
                }
                else if (g.Kind == GateKind.Coin)
                {
                    var coin = _lib.Coin[(int)Math.Floor(t * 8 + g.Y) & 3];
                    screen.Blit(coin, x - (coin.Width >> 1), s - 3);
                }
                else if (g.Kind == GateKind.Rock)
                {
                    if (g.Hit) { screen.Blit(_lib.Rock, x - 14, s - 2); screen.Blit(_lib.Rock, x + 7, s - 4); }
                    else screen.Blit(_lib.RockBig, x - 5, s - 6);
                }
                else if (g.Kind == GateKind.BrokenRail)
                {
                    uint ballast = Palette.Get(view.Style.Ballast[0]);
                    screen.FillRect(x - 4, s - 6, 3, 12, ballast); screen.FillRect(x + 2, s - 6, 3, 12, ballast);
                    screen.FillRect(x - 6, s - 7, 1, 3, Palette.Get('S')); screen.FillRect(x + 5, s + 3, 1, 3, Palette.Get('S'));
                    screen.FillRect(x - 5, s - 1, 2, 1, Palette.Get('n')); screen.FillRect(x + 3, s + 2, 2, 1, Palette.Get('n'));
                    if (!g.Hit)
                    {
                        screen.Blit(_lib.Barrier, x - 5, s - 12);
                        if ((int)Math.Floor(t * 3) % 2 == 0)
                        {
                            _lit.Add(new LitPixel(x - 1, s - 15, 2, 2, 'h'));
                            _lights.Add(new LightSource(x, s - 14, 6, true));
                        }
                    }
                }
            }
            foreach (var g in run.Gates)
            {
                if (g.Kind != GateKind.Cow || g.Gone) continue;
                if (g.Y < yLo) continue;
                if (g.Y > yHi + 20) break;
                int frame = (int)Math.Floor(t * (g.Fleeing ? 10 : 3)) & 1;
                screen.Blit(g.Direction > 0 ? _lib.Cow[frame] : _lib.CowLeft[frame], MathUtil.RoundHalfUp(g.X) - 5, sy(g.Y) - 5);
            }

            // ---- süs objeleri (uzaktan yakına)
            var props = view.Scenery.Props;
            int last = props.Count - 1;
            while (last >= 0 && props[last].Y > yHi + 50) last--;
            for (int i = last; i >= 0; i--)
            {
                var p = props[i];
                if (p.Y < yLo - 4) break;
                int s = sy(p.Y);
                float px = p.X;
                if (p.IsBoat) px += MathUtil.RoundHalfUp((float)Math.Sin(t * 0.6f + p.BoatPhase) * 3f);
                int dx = MathUtil.RoundHalfUp(px - p.Sprite.Width / 2f), dy = s - p.Sprite.Height;
                screen.Blit(p.Sprite, dx, dy);
                if (p.IsMill)
                {
                    float a = t * 1.6f + p.MillPhase;
                    int hx = dx + 4, hy = dy + 2;
                    for (int q = 0; q < 4; q++)
                    {
                        float aa = a + q * (float)Math.PI / 2f;
                        for (int r = 2; r < 9; r++)
                            screen.FillRect(MathUtil.RoundHalfUp(hx + (float)Math.Cos(aa) * r), MathUtil.RoundHalfUp(hy + (float)Math.Sin(aa) * r * 0.8f), 1, 1, Palette.Get('t'));
                    }
                    screen.FillRect(hx, hy, 1, 1, Palette.Get('k'));
                }
                if (p.Light > 0)
                {
                    int lx = MathUtil.RoundHalfUp(px);
                    _lights.Add(new LightSource(lx, dy + 1, p.Light));
                    _lit.Add(p.Lighthouse ? new LitPixel(lx - 1, dy, 3, 3, 'Y') : new LitPixel(lx, dy, 1, 2, 'Y'));
                }
                if (p.Windows != null && dk > 0.05f)
                {
                    for (int w = 0; w < p.Windows.Count; w++)
                    {
                        if (MathUtil.Hash(p.Seed, w) > 0.62f) continue;
                        var win = p.Windows[w];
                        _lit.Add(new LitPixel(dx + win[0], dy + win[1], win[2], win[3], MathUtil.Hash(p.Seed + 1, w) < 0.2f ? 'c' : 'Y'));
                        if (w % 2 == 0) _lights.Add(new LightSource(dx + win[0] + 1, dy + win[1] + 1, 3));
                    }
                }
                if (p.Car != 0 && dk > 0.05f)
                {
                    int fx = p.Car > 0 ? dx + p.Sprite.Width - 1 : dx;
                    _lit.Add(new LitPixel(fx, dy + 1, 1, 1, 'Y'));
                    _lit.Add(new LitPixel(fx, dy + 3, 1, 1, 'Y'));
                    _lights.Add(new LightSource(fx + p.Car * 5, dy + 2, 5));
                }
            }

            // ---- tren (gölge, sonra kendisi)
            _parts.Clear();
            string key = view.LocomotiveKey;
            _parts.Add(_lib.Locomotives[key]);
            if (key == "steam") _parts.Add(_lib.Tender);
            foreach (var w in run.Wagons) _parts.Add(view.WagonImage(w, night));
            int off = 0;
            foreach (var im in _parts) { DrawBent(screen, _lib.ShadowOf(im), run, runs, cam, cam - off, ty, 2); off += im.Height; }
            off = 0;
            int firstWagon = key == "steam" ? 2 : 1;
            for (int pi = 0; pi < _parts.Count; pi++)
            {
                var im = _parts[pi];
                DrawBent(screen, im, run, runs, cam, cam - off, ty, 0);
                if (pi >= firstWagon && dk > 0.05f)
                {
                    var w = run.Wagons[pi - firstWagon];
                    if (w.Type == WagonType.Passenger)
                        for (int q = 0; q < Math.Min(7, w.Items.Count); q++)
                        {
                            float wy = cam - off - 4 - q * 2;
                            _lights.Add(new LightSource(MathUtil.RoundHalfUp(runs.TrainXAt(run, wy)), sy(wy), 3));
                        }
                }
                off += im.Height;
            }
            int headX = MathUtil.RoundHalfUp(runs.TrainXAt(run, cam));
            if (run.HornTimer > 0f)
            {
                int r = MathUtil.RoundHalfUp((0.8f - run.HornTimer) * 22f) + 6;
                for (float a = -2.4f; a <= -0.7f; a += 0.12f)
                    screen.FillRect(MathUtil.RoundHalfUp(headX + (float)Math.Cos(a) * r), MathUtil.RoundHalfUp(ty + 6 + (float)Math.Sin(a) * r), 1, 1, Palette.Get('w'));
            }

            // ---- karşıdan gelen trenler
            foreach (var o in run.Oncoming)
            {
                if (o.Gone) continue;
                int x = (int)B.LaneX[o.Lane], fs = sy(o.Y);
                if (fs < -120 || fs > H + 40) continue;
                float yy = o.Y;
                foreach (var im in _lib.Oncoming)
                {
                    int top = sy(yy) - im.Height + 1;
                    screen.Blit(_lib.ShadowOf(im), x - 4, top + 2);
                    screen.Blit(im, x - 6, top);
                    yy += im.Height;
                }
                _lights.Add(new LightSource(x, fs + 12, 13));
                _lit.Add(new LitPixel(x - 2, fs, 1, 1, 'Y'));
                _lit.Add(new LitPixel(x + 1, fs, 1, 1, 'Y'));
            }

            // ---- peron kanopileri ve garlar (trenin üstünde)
            foreach (var g in run.Gates)
            {
                if (g.Y < yLo - 30) continue;
                if (g.Y > yHi + 30) break;
                var cat = services.Catalog.Station(g.Kind).Category;
                int s = sy(g.Y);
                if (cat == GateCategory.Terminal)
                {
                    screen.FillRect(42, s + 22, 78, 3, Palette.Shadow);
                    screen.FillRect(40, s - 10, 80, 33, Palette.Get('k'));
                    screen.FillRect(41, s - 9, 78, 31, Palette.Get('r'));
                    for (int yy = -9; yy < 22; yy += 4) screen.FillRect(41, s + yy, 78, 2, Palette.Get('R'));
                    for (int xx = 46; xx < 116; xx += 12) screen.FillRect(xx, s + 4, 6, 3, Palette.Get('c'));
                    screen.FillRect(72, s - 14, 17, 11, Palette.Get('k'));
                    screen.FillRect(73, s - 13, 15, 9, Palette.Get('t'));
                    screen.Blit(_lib.Icons[g.IsOrigin ? "train" : "city"], 77, s - 12);
                    _lights.Add(new LightSource(60, s + 5, 14));
                    _lights.Add(new LightSource(100, s + 5, 14));
                    _lights.Add(new LightSource(80, s - 8, 10));
                    continue;
                }
                if (cat != GateCategory.Pickup && cat != GateCategory.Delivery && cat != GateCategory.Fuel) continue;
                int x = (int)B.LaneX[g.Lane];
                var cols = CanopyColors[g.Kind];
                screen.FillRect(x - 10, s + 6, 23, 2, Palette.Shadow);
                screen.FillRect(x - 12, s - 6, 24, 12, Palette.Get('k'));
                screen.FillRect(x - 11, s - 5, 22, 6, Palette.Get(cols[1]));
                screen.FillRect(x - 11, s + 1, 22, 4, Palette.Get(cols[0]));
                screen.FillRect(x - 5, s - 5, 10, 9, Palette.Get('k'));
                screen.FillRect(x - 4, s - 4, 8, 7, Palette.Get('t'));
                screen.Blit(_lib.Icons[CanopyIcons[g.Kind]], x - 4, s - 4);
                if (g.Hit && run.Y - g.Y < 40) screen.FillRect(x - 11, s - 5, 22, 1, Palette.Get('Y'));
                _lights.Add(new LightSource(x, s, 15));
                _lit.Add(new LitPixel(x - 11, s + 4, 1, 1, 'Y'));
                _lit.Add(new LitPixel(x + 10, s + 4, 1, 1, 'Y'));
            }

            // ---- parçacıklar
            foreach (var p in view.Particles)
            {
                int s = sy(p.Y);
                float f = p.T / p.Life;
                switch (p.Kind)
                {
                    case ParticleKind.Puff:
                    {
                        var im = _lib.Puff[Math.Min(3, (int)Math.Floor(f * 4.2f))];
                        screen.Blit(im, MathUtil.RoundHalfUp(p.X - im.Width / 2f), s - im.Height / 2, 1f - f * 0.85f);
                        break;
                    }
                    case ParticleKind.DieselPuff:
                    {
                        var im = _lib.DieselPuff[Math.Min(2, (int)Math.Floor(f * 3f))];
                        screen.Blit(im, MathUtil.RoundHalfUp(p.X - im.Width / 2f), s - im.Height / 2, 0.8f - f * 0.7f);
                        break;
                    }
                    case ParticleKind.Spark:
                    {
                        int x = MathUtil.RoundHalfUp(p.X);
                        _lit.Add(new LitPixel(x, s, 1, 1, 'c'));
                        _lit.Add(new LitPixel(x + 1, s - 1, 1, 1, 'w'));
                        _lights.Add(new LightSource(x, s, 5));
                        break;
                    }
                    case ParticleKind.Debris:
                        screen.FillRect(MathUtil.RoundHalfUp(p.X), s, 1, 1, Palette.Get(p.Color));
                        break;
                }
            }

            // ---- tünel dağları
            for (int k = FloorDiv(yLo, CH); k <= FloorDiv(yHi, CH); k++)
            {
                if (!view.Overlays.TryGetValue(k, out var overlay)) { overlay = RenderTunnelOverlay(view, k); view.Overlays[k] = overlay; }
                if (overlay != null) screen.Blit(overlay, 0, ty - ((k + 1) * CH - cam));
            }

            // ---- turbo hız çizgileri
            if (run.IsTurbo)
            {
                for (int i = 0; i < 18; i++)
                {
                    int x = (int)Math.Floor(MathUtil.Hash(i, 7) * Width);
                    float y = (MathUtil.Hash(i, 9) * (H + 20) + t * 420f) % (H + 20) - 10;
                    screen.FillRect(x, MathUtil.RoundHalfUp(y), 1, 7, Palette.Get('w'), 0.55f);
                }
            }

            // ---- bulut gölgeleri (gündüz)
            if (dk < 0.9f)
            {
                float alpha = 0.13f * (1f - dk);
                for (int c = 0; c < 4; c++)
                {
                    int baseY = FloorDiv(cam, 260) * 260 + c * 130 - 130;
                    for (int rep = 0; rep < 3; rep++)
                    {
                        int wy = baseY + rep * 520, ci = Math.Abs(wy / 130) % 4;
                        float cx = (MathUtil.Hash(ci, wy) * 220f + t * 4f) % 220f - 60f;
                        screen.Blit(_lib.Clouds[ci], MathUtil.RoundHalfUp(cx), sy(wy), alpha);
                    }
                }
            }

            // ---- kar
            if (run.Biome.HasSnow)
            {
                for (int i = 0; i < 70; i++)
                {
                    float x = (MathUtil.Hash(i, 1) * 200f + t * (8f + MathUtil.Hash(i, 3) * 10f)) % 170f - 5f;
                    float y = (MathUtil.Hash(i, 2) * H + t * (22f + MathUtil.Hash(i, 4) * 16f)) % H;
                    screen.FillRect(MathUtil.RoundHalfUp(x), MathUtil.RoundHalfUp(y), 1, 1, Palette.Get('w'));
                }
            }

            // ---- yağmur
            if (run.Raining)
            {
                screen.FillRect(0, 0, Width, H, Rgba.Pack(18, 40, 90, 41));
                for (int i = 0; i < 90; i++)
                {
                    float x = ((MathUtil.Hash(i, 11) * 190f - t * 36f) % 190f + 190f) % 190f - 15f;
                    float y = (MathUtil.Hash(i, 12) * H + t * (230f + MathUtil.Hash(i, 13) * 60f)) % H;
                    int rx = MathUtil.RoundHalfUp(x), ry = MathUtil.RoundHalfUp(y);
                    screen.FillRect(rx, ry, 1, 2, Palette.Get('c'), 0.5f);
                    screen.FillRect(rx - 1, ry + 2, 1, 2, Palette.Get('c'), 0.5f);
                }
            }

            // ---- şafak / akşam tonu
            float tw = DayCycle.Twilight(run.TimeOfDay);
            if (tw > 0.02f)
            {
                bool dawn = run.TimeOfDay < 0.5f;
                screen.MultiplyAll(247, dawn ? 170 : 140, dawn ? 150 : 90, 0.55f * tw);
            }

            // ---- gece ışıkları
            if (dk > 0.02f)
            {
                if (_mask == null || _mask.Width != Width || _mask.Height != H) _mask = new PixelImage(Width, H);
                _mask.Clear(Rgba.Pack(10, 12, 40, (int)(0.84f * dk * 255f)));
                if (services.World.ZoneAt(run, cam).Type != ZoneType.Tunnel)
                {
                    int range = 70 + run.HeadlightLevel * 28, spread = 16 + run.HeadlightLevel * 4;
                    var cone = _lib.Cone(range, spread);
                    _mask.Erase(cone, headX - (cone.Width >> 1), ty - range);
                }
                foreach (var l in _lights) _mask.Erase(_lib.Light(l.R), l.X - l.R, l.Y - l.R);
                screen.Blit(_mask, 0, 0);
                foreach (var l in _lights)
                    if (l.R >= 10 || l.Warning) screen.Add(_lib.Glow(l.R), l.X - l.R, l.Y - l.R, 0.18f * dk);
                float litAlpha = Math.Min(1f, dk * 1.6f);
                foreach (var p in _lit) screen.FillRect(p.X, p.Y, p.W, p.H, Palette.Get(p.Color), litAlpha);
                screen.FillRect(headX - 2, ty, 1, 1, Palette.Get('Y'), litAlpha);
                screen.FillRect(headX + 1, ty, 1, 1, Palette.Get('Y'), litAlpha);
            }
            else
            {
                foreach (var p in _lit) if (p.Color == 'h') screen.FillRect(p.X, p.Y, p.W, p.H, Palette.Get('h'));
            }

            // ---- peron tahminleri
            if (!run.IsAttract && run.Phase == RunPhase.Running)
            {
                foreach (var g in run.Gates)
                {
                    if (g.Done || g.Y < cam + 14) continue;
                    if (g.Y > cam + ty - 40) break;
                    var preview = runs.Preview(run, g);
                    if (preview.Text == null) continue;
                    PixelFont.DrawCentered(screen, preview.Text, (int)B.LaneX[g.Lane], sy(g.Y) - 13, Palette.Get(ToneColor(preview.Tone)));
                }
                foreach (var o in run.Oncoming)
                {
                    if (o.Gone || o.Passed || o.Y - run.Y > 560) continue;
                    if (sy(o.Y) > 48 || (int)Math.Floor(t * 5) % 2 == 1) continue;
                    int x = (int)B.LaneX[o.Lane];
                    screen.Blit(_lib.Warn, x - 4, 40);
                    PixelFont.DrawCentered(screen, "TREN", x, 50, Palette.Get('R'));
                }
            }

            // ---- uçan yazılar
            foreach (var f in view.Floaters)
            {
                if ((int)Math.Floor(f.T * 20) % 2 != 0 && f.T >= 0.6f) continue;
                PixelFont.DrawCentered(screen, f.Text, MathUtil.RoundHalfUp(f.X), MathUtil.RoundHalfUp(ty - 12 - f.T * 22), Palette.Get(f.Color));
            }

            // ---- sarsıntı
            if (run.Shake > 0f && !ReduceMotion)
            {
                float amp = run.Shake * 6f;
                int ox = MathUtil.RoundHalfUp(((float)_shake.NextDouble() - 0.5f) * amp);
                int oy = MathUtil.RoundHalfUp(((float)_shake.NextDouble() - 0.5f) * amp);
                if (ox != 0 || oy != 0) Shift(screen, ox, oy);
            }
        }

        private static char ToneColor(PreviewTone tone)
        {
            switch (tone)
            {
                case PreviewTone.Passenger: return 'B';
                case PreviewTone.Coal: return 'S';
                case PreviewTone.Food: return 'Y';
                case PreviewTone.Money: return 'Y';
                case PreviewTone.Fuel: return 'R';
                case PreviewTone.Warning: return 'R';
                default: return 's';
            }
        }

        /// <summary>Vagonu rayın eğrisine göre satır satır kaydırarak çizer (makasta tren kıvrılır).</summary>
        private static void DrawBent(PixelImage screen, PixelImage img, RunState run, IRunService runs, int cam, int yFront, int ty, int offset)
        {
            int w = img.Width;
            for (int r = 0; r < img.Height; r++)
            {
                int wy = yFront - r;
                int x = MathUtil.RoundHalfUp(runs.TrainXAt(run, wy)) - (w >> 1) + offset;
                int s = ty - (wy - cam) + offset;
                screen.BlitRegion(img, 0, r, w, 1, x, s);
            }
        }

        private void Shift(PixelImage screen, int ox, int oy)
        {
            if (_shift == null || _shift.Width != screen.Width || _shift.Height != screen.Height) _shift = new PixelImage(screen.Width, screen.Height);
            _shift.Clear(Palette.Get('k'));
            _shift.CopyOpaque(screen, ox, oy);
            Array.Copy(_shift.Pixels, screen.Pixels, screen.Pixels.Length);
        }

        private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);

        // ================================================================ zemin

        private static float VNoise(float x, float y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float u = MathUtil.Smooth(x - xi), v = MathUtil.Smooth(y - yi);
            return MathUtil.Lerp(MathUtil.Lerp(MathUtil.Hash(xi, yi), MathUtil.Hash(xi + 1, yi), u),
                MathUtil.Lerp(MathUtil.Hash(xi, yi + 1), MathUtil.Hash(xi + 1, yi + 1), u), v);
        }

        private static char GroundCode(RunView view, Zone z, int x, int wy)
        {
            var biome = view.Run.Biome;
            var world = view.Services.World;
            float h = MathUtil.Hash(x, wy), hc = VNoise(x / 6f, wy / 6f);
            if (biome.HasSea)
            {
                int edge = 124 + MathUtil.RoundHalfUp(5f * (float)Math.Sin(wy / 37f) + 3f * (float)Math.Sin(wy / 13f));
                if (x > edge + 4) return h < 0.05f ? 'B' : (MathUtil.Hash(x >> 1, wy) > 0.985f ? 'c' : 'b');
                if (x > edge + 2) return MathUtil.Hash(x, wy >> 1) < 0.5f ? 'c' : 'B';
                if (x > edge - 3) return h < 0.5f ? 't' : 'T';
            }
            if (z.Type == ZoneType.River)
            {
                float a = z.Y0 + 5, b = z.Y1 - 5;
                if (wy > a && wy < b) return (wy - a < 2 || b - wy < 2) ? 'B' : (h < 0.05f ? 'B' : ((x + wy * 3) % 13 == 0 ? 'c' : 'b'));
                if (wy >= z.Y0 && wy <= z.Y1) return h < 0.5f ? 't' : 'T';
            }
            foreach (float ry in z.Roads)
            {
                if (wy >= ry && wy < ry + 9)
                {
                    int k = wy - (int)ry;
                    if (k == 0 || k == 8) return 's';
                    if (k == 4 && (x >> 2) % 2 == 0) return 'S';
                    return h < 0.08f ? 'G' : 'g';
                }
            }
            if (z.Type == ZoneType.City || z.Type == ZoneType.Terminal || z.Type == ZoneType.Station)
            {
                if (biome.HasSnow && hc < 0.5f) return h < 0.5f ? 'w' : 'S';
                return (x % 9 == 0 || wy % 9 == 0) ? 'g' : (h < 0.06f ? 's' : 'G');
            }
            if (z.Type == ZoneType.Town && !biome.HasSnow)
            {
                float dn = VNoise(x / 9f + 40f, wy / 9f);
                if (dn < 0.3f || (dn < 0.33f && ((x + wy) & 1) == 1)) return h < 0.7f ? 'T' : 'x';
            }
            var field = world.FieldAt(z, x, wy);
            if (field != null)
            {
                if (x == (int)field.X0 || x == (int)field.X1 - 1 || wy == (int)field.Y0 || wy == (int)field.Y1 - 1) return 'n';
                switch (field.Crop)
                {
                    case CropType.Wheat: return wy % 3 == 0 ? 'x' : (h < 0.3f ? 'y' : 'T');
                    case CropType.Green: return wy % 3 == 0 ? 'e' : (h < 0.2f ? 'E' : 'l');
                    default: return wy % 2 == 0 ? 'n' : 'N';
                }
            }
            var g = view.Style.Ground;
            float n = VNoise(x / 16f, wy / 16f) * 0.7f + VNoise(x / 5f + 9f, wy / 5f) * 0.3f;
            bool dith = ((x + wy) & 1) == 1;
            if (n > 0.64f || (n > 0.6f && dith)) return h > 0.96f ? g[0] : g[1];
            if (n < 0.3f || (n < 0.33f && dith)) return h > 0.95f ? g[0] : g[2];
            if (h > 0.975f) return g[1];
            if (h < 0.012f) return g[2];
            return g[0];
        }

        private static PixelImage RenderChunk(RunView view, int k)
        {
            var run = view.Run;
            var world = view.Services.World;
            var B = view.Services.Catalog.Balance;
            var img = new PixelImage(Width, CH);
            int yTop = (k + 1) * CH - 1;
            for (int r = 0; r < CH; r++)
            {
                int wy = yTop - r;
                var z = world.ZoneAt(run, wy);
                for (int x = 0; x < Width; x++) img.Pixels[r * Width + x] = Palette.Get(GroundCode(view, z, x, wy));
            }

            var junctions = new List<Junction>();
            foreach (var j in run.Junctions) if (j.Y <= yTop && j.Y + B.JunctionLength >= yTop - CH) junctions.Add(j);
            var p = new Painter(img);
            var ballast = view.Style.Ballast;
            int[][] platforms = { new[] { 38, 49 }, new[] { 63, 73 }, new[] { 87, 97 }, new[] { 111, 122 } };
            int[] platformEdges = { 49, 63, 73, 87, 97, 111 };
            var xs = new List<float>(8);
            for (int r = 0; r < CH; r++)
            {
                int wy = yTop - r;
                var z = world.ZoneAt(run, wy);
                bool bridge = z.Type == ZoneType.River && wy > z.Y0 + 3 && wy < z.Y1 - 3;
                bool platform = z.HasPlatform && wy >= z.PlatformY0 && wy <= z.PlatformY1;
                bool road = world.InRoad(z, wy);
                Junction jn = null;
                foreach (var j in junctions) if (wy >= j.Y && wy <= j.Y + B.JunctionLength) { jn = j; break; }

                if (bridge)
                {
                    p.P(46, r, wy % 3 == 0 ? 'N' : 'n', 68); p.P(45, r, 'k'); p.P(114, r, 'k');
                    if (wy % 6 == 0) { p.P(43, r, 'g', 2); p.P(115, r, 'g', 2); }
                }
                else if (!road)
                {
                    if (jn != null) BallastSpan(p, ballast, 48, 112, r, wy);
                    else foreach (float l in B.LaneX) BallastSpan(p, ballast, (int)l - 6, (int)l + 6, r, wy);
                }
                if (platform)
                {
                    foreach (var span in platforms) p.P(span[0], r, 'S', span[1] - span[0] + 1);
                    foreach (int e in platformEdges) p.P(e, r, 'Y');
                }
                if (wy % 4 == 0 && !road)
                {
                    if (jn != null) p.P(49, r, 'n', 63);
                    else foreach (float l in B.LaneX) p.P((int)l - 5, r, bridge ? 'q' : 'n', 11);
                }
                xs.Clear();
                xs.AddRange(B.LaneX);
                if (jn != null)
                {
                    float tt = MathUtil.Smooth((wy - jn.Y) / B.JunctionLength);
                    xs.Add(MathUtil.Lerp(B.LaneX[0], B.LaneX[1], tt));
                    xs.Add(MathUtil.Lerp(B.LaneX[1], B.LaneX[0], tt));
                    xs.Add(MathUtil.Lerp(B.LaneX[1], B.LaneX[2], tt));
                    xs.Add(MathUtil.Lerp(B.LaneX[2], B.LaneX[1], tt));
                }
                foreach (float cx in xs)
                {
                    int rx = MathUtil.RoundHalfUp(cx);
                    p.P(rx - 3, r, road ? 's' : 'S');
                    p.P(rx + 3, r, road ? 's' : 'S');
                }
            }
            return img;
        }

        private static void BallastSpan(Painter p, char[] ballast, int a, int b, int r, int wy)
        {
            for (int x = a; x <= b; x++)
            {
                float hh = MathUtil.Hash(x * 3, wy * 7);
                p.P(x, r, hh < 0.62f ? ballast[0] : hh < 0.85f ? ballast[1] : ballast[2]);
            }
        }

        private static PixelImage RenderTunnelOverlay(RunView view, int k)
        {
            var run = view.Run;
            var B = view.Services.Catalog.Balance;
            int yTop = (k + 1) * CH - 1, yBot = k * CH;
            var tunnels = new List<Zone>();
            foreach (var z in run.Zones)
                if (z.Type == ZoneType.Tunnel && z.Y1 + 14 >= yBot && z.Y0 - 22 <= yTop) tunnels.Add(z);
            if (tunnels.Count == 0) return null;

            var img = new PixelImage(Width, CH);
            var M = view.Style.Mountain;
            var lo = new int[Width];
            var hi = new int[Width];
            foreach (var z in tunnels)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool side = x < 46 || x > 114;
                    lo[x] = side ? (int)z.Y0 - 3 + MathUtil.RoundHalfUp((VNoise(x / 7f, z.Y0 / 7f) - 0.5f) * 18f) : (int)z.Y0;
                    hi[x] = side ? (int)z.Y1 + 3 + MathUtil.RoundHalfUp((VNoise(x / 7f + 50f, z.Y1 / 7f) - 0.5f) * 18f) : (int)z.Y1;
                }
                for (int r = 0; r < CH; r++)
                {
                    int wy = yTop - r;
                    for (int x = 0; x < Width; x++)
                    {
                        if (wy < lo[x] - 6 || wy > hi[x]) continue;
                        bool track = x >= 46 && x <= 114;
                        char col;
                        if (wy < lo[x])
                        {
                            if (track) continue;
                            col = wy == lo[x] - 6 ? 'k' : (x % 3 == 0 || MathUtil.Hash(x, wy) > 0.8f ? M[5] : M[4]);
                        }
                        else
                        {
                            float n = VNoise(x / 9f, wy / 9f), slope = n - VNoise((x + 1) / 9f, (wy - 1) / 9f);
                            col = slope > 0.025f ? M[1] : slope < -0.025f ? M[2] : M[0];
                            if (MathUtil.Hash(x, wy) > 0.985f || wy - lo[x] < 1 || hi[x] - wy < 1) col = M[3];
                            if (track)
                            {
                                int k2 = wy - (int)z.Y0;
                                if (k2 < 7)
                                {
                                    float lane = -1;
                                    foreach (float l in B.LaneX) if (Math.Abs(x - l) <= 6) lane = l;
                                    if (lane >= 0 && Math.Abs(x - lane) <= 6 - Math.Max(0, k2 - 3) * 2) col = 'k';
                                    else col = k2 == 6 ? 'g' : ((k2 % 2 == 0 || (x + (k2 >> 1) * 3) % 6 == 0) ? 'G' : 's');
                                }
                                if (z.Y1 - wy < 3) foreach (float l in B.LaneX) if (Math.Abs(x - l) <= 6) col = 'k';
                            }
                        }
                        img.Pixels[r * Width + x] = Palette.Get(col);
                    }
                }
            }
            return img;
        }
    }
}
