using System.Collections.Generic;
using PixTrain.Core;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Services;
using PixTrain.Core.Utils;

namespace PixTrain.Render
{
    public enum ParticleKind { Puff, DieselPuff, Spark, Debris }

    public sealed class Particle
    {
        public ParticleKind Kind;
        public float X, Y, Vx, Vy, Life, T;
        public char Color;
    }

    /// <summary>Trenin üstünden yükselen kısa yazı ("+54", "DOLU", "KAZA!").</summary>
    public sealed class Floater
    {
        public string Text;
        public char Color;
        public float X;
        public float T;
    }

    /// <summary>
    /// Bir seferin yalnızca görsel durumu: manzara, zemin önbelleği, parçacıklar, uçan yazılar.
    /// Oyun kurallarını değiştirmez; RunState'i okur.
    /// </summary>
    public sealed class RunView
    {
        public const int ChunkHeight = 128;
        public const float FloaterLife = 1.1f;

        public readonly RunState Run;
        public readonly GameServices Services;
        public readonly Scenery Scenery;
        public readonly BiomeStyle Style;
        public readonly Dictionary<int, PixelImage> Chunks = new Dictionary<int, PixelImage>();
        /// <summary>Tünel dağı katmanı; parçada tünel yoksa null.</summary>
        public readonly Dictionary<int, PixelImage> Overlays = new Dictionary<int, PixelImage>();
        public readonly List<Particle> Particles = new List<Particle>();
        public readonly List<Floater> Floaters = new List<Floater>();

        private readonly Dictionary<Wagon, WagonCache> _wagons = new Dictionary<Wagon, WagonCache>();
        private readonly IRandom _fx;
        private float _smokeTimer;

        private sealed class WagonCache
        {
            public int Version = -1;
            public bool Night;
            public PixelImage Image;
        }

        public RunView(RunState run, GameServices services)
        {
            Run = run;
            Services = services;
            Style = BiomeStyle.For(run.Biome.Id);
            Scenery = new Scenery(run, services.World, services.Economy);
            _fx = new SeededRandom(run.Seed ^ 0x1B873593);
            Scenery.Update();
        }

        public string LocomotiveKey => Services.Catalog.Locomotives[Run.LocomotiveIndex].SpriteKey;

        /// <summary>RunService.Tick'ten hemen sonra çağrılır: olayları yazıya ve efekte çevirir.</summary>
        public void HandleEvents()
        {
            if (Run.IsAttract) return;
            foreach (var e in Run.Events)
            {
                switch (e.Type)
                {
                    case RunEventType.Refuel: Float("+YAKIT", 'R', e.X); break;
                    case RunEventType.Pickup: Float("+" + e.Count, CargoColor(e.Cargo), e.X); break;
                    case RunEventType.WagonsFull: Float("DOLU", 'R', e.X); break;
                    case RunEventType.Delivery: Float("+" + e.Value + (e.Count >= Services.Catalog.Balance.ComboThreshold ? " KOMBO" : ""), 'Y', e.X); break;
                    case RunEventType.NothingToDeliver: Float("BOS", 's', e.X); break;
                    case RunEventType.Horn: Float("DUUT!", 'Y', e.X); break;
                    case RunEventType.Turbo: Float("TURBO!", 'o', e.X); break;
                    case RunEventType.StreakUp: Float("SERI X" + Services.Economy.StreakMultiplier(e.Value).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), 'Y', e.X); break;
                    case RunEventType.Crash:
                    case RunEventType.OncomingCrash:
                        Float(e.Count > 0 ? "KAZA! -" + e.Count : "KAZA!", 'R', e.X);
                        Debris(e.X, e.Type == RunEventType.OncomingCrash ? 24 : 14);
                        break;
                }
            }
        }

        /// <summary>Her karede: duman, parçacıklar, yazılar, manzara ve önbellek temizliği.</summary>
        public void Update(float dt)
        {
            var runs = Services.Runs;
            string key = LocomotiveKey;
            int chimney = SpriteLibrary.Instance.ChimneyOffset[key];
            _smokeTimer -= dt;
            if (_smokeTimer <= 0f && !Run.IsFinished)
            {
                float y = Run.Y - chimney, x = runs.TrainXAt(Run, y);
                if (key == "steam")
                {
                    _smokeTimer = (Run.IsTurbo ? 0.035f : 0.07f) + (1f - System.Math.Min(1f, Run.Speed / 60f)) * 0.12f;
                    Particles.Add(new Particle { Kind = ParticleKind.Puff, X = x + (_fx.NextFloat() - 0.5f) * 2f, Y = y, Vx = (_fx.NextFloat() - 0.5f) * 6f + 4f, Vy = 6f, Life = 1.3f });
                }
                else if (key == "diesel")
                {
                    _smokeTimer = 0.16f;
                    Particles.Add(new Particle { Kind = ParticleKind.DieselPuff, X = x, Y = y, Vx = (_fx.NextFloat() - 0.5f) * 4f + 3f, Vy = 4f, Life = 0.8f });
                }
                else
                {
                    _smokeTimer = 0.3f + _fx.NextFloat() * 0.8f;
                    if (_fx.NextFloat() < 0.4f) Particles.Add(new Particle { Kind = ParticleKind.Spark, X = x + (_fx.NextFloat() - 0.5f) * 4f, Y = y, Life = 0.12f });
                }
            }
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.T += dt;
                p.X += p.Vx * dt;
                p.Y += p.Vy * dt;
                if (p.Kind == ParticleKind.Debris) p.Vy -= 60f * dt;
                if (p.T >= p.Life) Particles.RemoveAt(i);
            }
            for (int i = Floaters.Count - 1; i >= 0; i--)
            {
                Floaters[i].T += dt;
                if (Floaters[i].T >= FloaterLife) Floaters.RemoveAt(i);
            }

            Scenery.Update();
            int lowChunk = (int)System.Math.Floor((Run.Y - 260f) / ChunkHeight);
            Prune(Chunks, lowChunk);
            Prune(Overlays, lowChunk);
        }

        public PixelImage WagonImage(Wagon w, bool night)
        {
            if (!_wagons.TryGetValue(w, out var cache)) { cache = new WagonCache(); _wagons[w] = cache; }
            if (cache.Image != null && cache.Version == w.Version && cache.Night == night) return cache.Image;
            cache.Version = w.Version;
            cache.Night = night;
            var lib = SpriteLibrary.Instance;
            var img = (w.Type == WagonType.Passenger ? lib.WagonPassenger : lib.WagonFreight).Clone();
            if (w.Type == WagonType.Passenger)
            {
                for (int i = 0; i < System.Math.Min(7, w.Items.Count); i++)
                {
                    img.FillRect(4, 4 + i * 2, 4, 1, Palette.Get(night ? 'Y' : 'z'));
                    if (!night) img.Put(5, 4 + i * 2, Palette.Get('n'));
                }
            }
            else
            {
                const int rows = 15;
                for (int i = 0; i < w.Items.Count; i++)
                {
                    int r0 = 3 + i * rows / w.Capacity, r1 = 3 + (i + 1) * rows / w.Capacity;
                    var item = w.Items[i];
                    for (int r = r0; r < r1; r++)
                        for (int x = 3; x < 9; x++)
                        {
                            float hh = MathUtil.Hash(x + i * 7, r * 3);
                            char c;
                            if (item == CargoType.Coal) c = hh < 0.55f ? 'k' : hh < 0.85f ? 'g' : 'G';
                            else if (item == CargoType.Food) c = (r == r0 || x == 3 || x == 8) ? 'N' : (hh < 0.6f ? 'y' : 'Y');
                            else c = 'z';
                            img.Put(x, r, Palette.Get(c));
                        }
                }
            }
            cache.Image = img;
            return img;
        }

        private void Float(string text, char color, float x) => Floaters.Add(new Floater { Text = text, Color = color, X = x });

        private void Debris(float x, int count)
        {
            char[] cols = { 'n', 'N', 'S', 'Y' };
            for (int i = 0; i < count; i++)
                Particles.Add(new Particle
                {
                    Kind = ParticleKind.Debris, X = x, Y = Run.Y + 4, Vx = (_fx.NextFloat() - 0.5f) * 70f, Vy = _fx.NextFloat() * 40f + 10f,
                    Life = 0.6f + _fx.NextFloat() * 0.4f, Color = cols[i % cols.Length],
                });
        }

        private static char CargoColor(CargoType c) => c == CargoType.Passenger ? 'B' : c == CargoType.Coal ? 'S' : 'Y';

        private static void Prune(Dictionary<int, PixelImage> cache, int lowChunk)
        {
            List<int> old = null;
            foreach (var k in cache.Keys) if (k < lowChunk) (old ?? (old = new List<int>())).Add(k);
            if (old != null) foreach (var k in old) cache.Remove(k);
        }
    }
}
