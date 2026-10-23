using System;
using System.Diagnostics;
using System.IO;
using PixTrain.Core;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Repositories;
using PixTrain.Render;

namespace PixTrain.Snapshots
{
    /// <summary>
    /// Oyunun kurallar ve çizim katmanlarını Unity olmadan çalıştırıp PNG kareler üretir.
    /// Unity'deki görüntüyle aynı kod yolunu kullanır (RunService + RunView + WorldRenderer).
    /// </summary>
    internal static class Program
    {
        private const int ScreenHeight = 320;
        private const int TrainY = ScreenHeight - 92;

        private static int Main(string[] args)
        {
            if (args.Length > 1 && args[0] == "web-assets") return WebAssets(args[1]);
            string outDir = args.Length > 0 ? args[0] : "snapshots";
            Directory.CreateDirectory(outDir);

            Shot(outDir, "01-ova-gunduz", route: 0, seed: 3, seconds: 9, tod: 0.45f);
            Shot(outDir, "02-ova-aksam", route: 0, seed: 3, seconds: 12, tod: 0.76f);
            Shot(outDir, "03-ova-gece", route: 0, seed: 3, seconds: 15, tod: 0.98f);
            Shot(outDir, "04-orman", route: 1, seed: 11, seconds: 10, tod: 0.5f);
            Shot(outDir, "05-nehir-yagmur", route: 2, seed: 4, seconds: 10, tod: 0.5f, rain: true);
            Shot(outDir, "06-dag-tunel", route: 3, seed: 8, seconds: 8, tod: 0.5f, findTunnel: true);
            Shot(outDir, "07-sahil", route: 4, seed: 2, seconds: 10, tod: 0.55f);
            Shot(outDir, "08-kar-gece", route: 5, seed: 6, seconds: 10, tod: 0.02f, findTunnel: true);
            Shot(outDir, "09-varis", route: 0, seed: 3, seconds: 3, tod: 0.45f, nearEnd: true);
            Shot(outDir, "10-dizel-turbo", route: 1, seed: 21, seconds: 10, tod: 0.4f, locomotive: 1, turbo: true);
            Shot(outDir, "11-elektrikli-gece", route: 4, seed: 9, seconds: 10, tod: 0.9f, locomotive: 2);

            var art = new PixelImage(5 * 46 + 102 + 40, 60);
            art.Clear(Palette.Get('d'));
            for (int lv = 1; lv <= 5; lv++) art.Blit(UiArt.CitySkyline(lv, 1), 4 + (lv - 1) * 46, 4);
            art.Blit(UiArt.Logo(), 5 * 46 + 8, 4);
            Png.Write(Path.Combine(outDir, "12-arayuz-cizimleri.png"), art, 4);
            Png.Write(Path.Combine(outDir, "13-garaj-onizleme.png"),
                UiArt.TrainPreview("steam", new[] { WagonType.Passenger, WagonType.Freight, WagonType.Freight }), 4);

            Benchmark();
            Console.WriteLine("Kareler: " + Path.GetFullPath(outDir));
            return 0;
        }

        /// <summary>Web sayfası görselleri: logo, uygulama ikonları, favicon (WebGL şablonu için).</summary>
        private static int WebAssets(string dir)
        {
            Directory.CreateDirectory(dir);
            Png.Write(Path.Combine(dir, "logo.png"), UiArt.Logo(), 4);
            var icon = AppIcon();
            Png.Write(Path.Combine(dir, "favicon.png"), icon, 1);
            Png.Write(Path.Combine(dir, "icon-192.png"), icon, 6);
            Png.Write(Path.Combine(dir, "icon-512.png"), icon, 16);
            Console.WriteLine("Web görselleri: " + Path.GetFullPath(dir));
            return 0;
        }

        /// <summary>32x32 ikon: gece mavisi zemin, ray, buharlı lokomotifin burnu.</summary>
        private static PixelImage AppIcon()
        {
            var lib = SpriteLibrary.Instance;
            var img = new PixelImage(32, 32);
            img.Clear(Palette.Get('d'));
            for (int y = 0; y < 32; y++)
            {
                if (y % 4 == 0) img.FillRect(9, y, 14, 1, Palette.Get('n'));
                img.FillRect(12, y, 1, 1, Palette.Get('S'));
                img.FillRect(19, y, 1, 1, Palette.Get('S'));
            }
            var loco = lib.Locomotives["steam"];
            img.Blit(lib.ShadowOf(loco), 12, 5);
            img.Blit(loco, 10, 3);
            img.FillRect(0, 0, 32, 1, Palette.Get('k'));
            img.FillRect(0, 31, 32, 1, Palette.Get('k'));
            img.FillRect(0, 0, 1, 32, Palette.Get('k'));
            img.FillRect(31, 0, 1, 32, Palette.Get('k'));
            img.FillRect(4, 6, 1, 1, Palette.Get('Y'));
            img.FillRect(26, 4, 1, 1, Palette.Get('Y'));
            img.FillRect(25, 22, 1, 1, Palette.Get('w'));
            return img;
        }

        private static GameServices NewGame(int locomotive)
        {
            var catalog = DefaultCatalog.Create();
            var progress = PlayerProgress.CreateDefault(catalog, DateTime.UtcNow);
            for (int i = 0; i <= locomotive; i++) progress.OwnedLocomotives[i] = true;
            progress.SelectedLocomotive = locomotive;
            return GameServices.Create(new InMemoryProgressRepository(progress), null, catalog);
        }

        private static void Shot(string dir, string name, int route, int seed, float seconds, float tod,
            bool rain = false, bool findTunnel = false, bool nearEnd = false, int locomotive = 0, bool turbo = false)
        {
            var game = NewGame(locomotive);
            var run = game.Runs.StartRun(route, false, seed);
            run.Raining = rain;
            var view = new RunView(run, game);
            var screen = new PixelImage(WorldRenderer.Width, ScreenHeight);
            var renderer = new WorldRenderer();

            if (findTunnel || nearEnd)
            {
                float target = run.Length - 260f;
                if (findTunnel)
                {
                    // Tünel bulana kadar hattı hızlıca ileri sar.
                    for (int i = 0; i < 400; i++)
                    {
                        var tunnel = run.Zones.Find(z => z.Type == ZoneType.Tunnel && z.Y0 > run.Y + 150);
                        if (tunnel != null) { target = tunnel.Y0 - 120f; break; }
                        Advance(game, view, run, 2f, steerEvery: 0);
                    }
                }
                while (run.Y < target - 400f && !run.IsFinished) { run.Y += 300f; run.Fuel = run.FuelMax; Advance(game, view, run, 0.05f, 0); }
                run.Y = Math.Max(run.Y, target - 200f);
            }

            float dt = 1f / 30f;
            for (float t = 0; t < seconds && !run.IsFinished; t += dt)
            {
                run.TimeOfDay = tod;
                if (turbo && t > seconds - 1.2f) game.Runs.Turbo(run);
                Step(game, view, run, dt, t);
            }
            run.TimeOfDay = tod;
            renderer.Render(screen, view, TrainY);
            Png.Write(Path.Combine(dir, name + ".png"), screen, 3);
        }

        private static void Advance(GameServices game, RunView view, RunState run, float seconds, int steerEvery)
        {
            float dt = 1f / 30f;
            for (float t = 0; t < seconds && !run.IsFinished; t += dt) Step(game, view, run, dt, t);
        }

        private static int _frame;

        private static void Step(GameServices game, RunView view, RunState run, float dt, float t)
        {
            // Basit oyuncu: her makasta peronlara doğru yönelir, tehlikeden kaçar.
            var j = game.Runs.NextJunction(run);
            if (j != null && j.Y - run.Y < 120 && run.SwitchDir == 0 && (_frame++ % 7 == 0))
            {
                int best = 0; float bestScore = float.MinValue;
                for (int d = -1; d <= 1; d++)
                {
                    int lane = run.Lane + d;
                    if (lane < 0 || lane > 2) continue;
                    float score = d == 0 ? 0.5f : 0f;
                    foreach (var g in run.Gates)
                    {
                        if (g.Done || g.Y < j.Y || g.Y > j.Y + 230 || g.Lane != lane) continue;
                        var cat = game.Catalog.Station(g.Kind).Category;
                        score += cat == GateCategory.Hazard ? -10 : cat == GateCategory.Coin ? 1 : 3;
                    }
                    if (score > bestScore) { bestScore = score; best = d; }
                }
                if (best != 0) game.Runs.Steer(run, best);
            }
            foreach (var g in run.Gates)
                if (g.Kind == GateKind.Cow && !g.Done && g.Y > run.Y && g.Y - run.Y < 80) game.Runs.Horn(run);
            game.Runs.Tick(run, dt);
            view.HandleEvents();
            view.Update(dt);
        }

        private static void Benchmark()
        {
            var game = NewGame(0);
            var run = game.Runs.StartRun(0, true, 99);
            var view = new RunView(run, game);
            var screen = new PixelImage(WorldRenderer.Width, 356);
            var renderer = new WorldRenderer();
            for (int i = 0; i < 60; i++) { game.Runs.Tick(run, 1f / 60f); view.Update(1f / 60f); renderer.Render(screen, view, 356 - 92); }
            var sw = Stopwatch.StartNew();
            const int frames = 600;
            for (int i = 0; i < frames; i++)
            {
                run.TimeOfDay = i < frames / 2 ? 0.5f : 0f;
                game.Runs.Tick(run, 1f / 60f);
                view.Update(1f / 60f);
                renderer.Render(screen, view, 356 - 92);
            }
            Console.WriteLine($"Ortalama kare süresi (kurallar + çizim, 160x356): {sw.Elapsed.TotalMilliseconds / frames:0.00} ms");
        }
    }
}
