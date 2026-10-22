using System;
using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Services;
using PixTrain.Core.Utils;

namespace PixTrain.Render
{
    /// <summary>Hattın kenarındaki süs: ağaç, ev, bina, lamba, araba, tekne.</summary>
    public sealed class Prop
    {
        public float X;
        public float Y;
        public PixelImage Sprite;
        public List<int[]> Windows;
        public int Seed;
        public int Light;
        public bool Lighthouse;
        public bool IsBoat;
        public float BoatPhase;
        public bool IsMill;
        public float MillPhase;
        /// <summary>Araba: 1 sağa, -1 sola bakar; 0 araba değil.</summary>
        public int Car;
    }

    /// <summary>
    /// Bölgelerden manzara üretir (prototipteki genZoneProps). Sadece görseldir; tohumdan deterministik üretilir.
    /// </summary>
    public sealed class Scenery
    {
        private const float LeftMin = 4f, LeftMax = 44f, RightMin = 116f, RightMax = 156f;

        private readonly RunState _run;
        private readonly IWorldGenService _world;
        private readonly IEconomyService _economy;
        private readonly BiomeStyle _style;
        private readonly IRandom _rng;
        private float _processedUpTo = float.NegativeInfinity;

        /// <summary>Y'ye göre sıralı.</summary>
        public readonly List<Prop> Props = new List<Prop>();

        public Scenery(RunState run, IWorldGenService world, IEconomyService economy)
        {
            _run = run;
            _world = world;
            _economy = economy;
            _style = BiomeStyle.For(run.Biome.Id);
            _rng = new SeededRandom(run.Seed ^ 0x2545F491);
        }

        /// <summary>Yeni bölgeler için manzara üretir, çok geride kalanları siler.</summary>
        public void Update()
        {
            var fresh = new List<Prop>();
            foreach (var z in _run.Zones)
            {
                if (z.Y0 <= _processedUpTo) continue;
                Generate(z, fresh);
                _processedUpTo = z.Y0;
            }
            if (fresh.Count > 0)
            {
                Props.AddRange(fresh);
                Props.Sort((a, b) => a.Y.CompareTo(b.Y));
            }
            float lo = _run.Y - 160f;
            int remove = 0;
            while (remove < Props.Count && Props[remove].Y < lo) remove++;
            if (remove > 0) Props.RemoveRange(0, remove);
        }

        private float SideX(int side) => side < 0 ? LeftMin + _rng.NextFloat() * (LeftMax - LeftMin) : RightMin + _rng.NextFloat() * (RightMax - RightMin);

        private void Generate(Zone z, List<Prop> list)
        {
            var lib = SpriteLibrary.Instance;
            bool sea = _run.Biome.HasSea;
            float yEnd = z.Type == ZoneType.Terminal ? _run.Length + 420f : z.Y1;

            switch (z.Type)
            {
                case ZoneType.Countryside:
                {
                    float step = 9f / _style.Density;
                    for (float y = z.Y0 + 4; y < z.Y1 - 4; y += step * (0.5f + _rng.NextFloat()))
                    {
                        int side = _rng.NextFloat() < 0.5f ? -1 : 1;
                        float x = SideX(side);
                        if (sea && side > 0)
                        {
                            if (_rng.NextFloat() < 0.05f) list.Add(Boat(136 + _rng.NextFloat() * 16, y));
                            continue;
                        }
                        var field = _world.FieldAt(z, x, y);
                        if (field != null)
                        {
                            if (field.Crop == CropType.Green && _rng.NextFloat() < 0.12f)
                                list.Add(new Prop { X = x, Y = y, Sprite = _rng.NextFloat() < 0.5f ? lib.Cow[0] : lib.Sheep });
                            continue;
                        }
                        if (_world.InRoad(z, y, 4)) continue;
                        var kind = _style.Countryside.Pick(_rng);
                        var sprite = BiomeStyle.SpriteOf(kind);
                        if (sprite != null) list.Add(new Prop { X = x, Y = y, Sprite = sprite });
                    }
                    foreach (var sp in _style.Specials)
                    {
                        if (_rng.NextFloat() > 0.55f) continue;
                        float y = z.Y0 + 30 + _rng.NextFloat() * Math.Max(10f, z.Y1 - z.Y0 - 60);
                        if (sp == SpecialProp.Mill) list.Add(new Prop { X = _rng.NextFloat() < 0.5f ? 24 : 136, Y = y, Sprite = lib.Mill, IsMill = true, MillPhase = _rng.NextFloat() * 6 });
                        else if (sp == SpecialProp.Farm) list.Add(House(_rng.NextFloat() < 0.5f ? 22 : 138, y, lib.Houses[_rng.NextInt(lib.Houses.Count)]));
                        else list.Add(new Prop { X = 146, Y = y, Sprite = lib.Lighthouse, Light = 18, Lighthouse = true });
                    }
                    break;
                }
                case ZoneType.River:
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (sea && side > 0) continue;
                        list.Add(new Prop { X = side < 0 ? 8 + _rng.NextFloat() * 30 : 122 + _rng.NextFloat() * 30, Y = z.Y1 + 2, Sprite = _rng.NextFloat() < 0.5f ? lib.Bush : lib.Tree });
                        list.Add(new Prop { X = side < 0 ? 8 + _rng.NextFloat() * 30 : 122 + _rng.NextFloat() * 30, Y = z.Y0 - 1, Sprite = lib.Bush });
                    }
                    if (_rng.NextFloat() < 0.6f) list.Add(Boat(14 + _rng.NextFloat() * 20, (z.Y0 + z.Y1) / 2 + 3));
                    break;
                case ZoneType.Town:
                    RoadDecor(z, list, sea);
                    Lamps(z, yEnd, 44f, list, sea);
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (sea && side > 0)
                        {
                            for (float y = z.Y0; y < z.Y1; y += 70) if (_rng.NextFloat() < 0.5f) list.Add(Boat(136 + _rng.NextFloat() * 16, y));
                            continue;
                        }
                        float a = side < 0 ? LeftMin : RightMin, b = side < 0 ? LeftMax : RightMax;
                        for (float y = z.Y0 + 14; y < z.Y1 - 4; y += 15 + _rng.NextFloat() * 6)
                        {
                            if (_world.InRoad(z, y, 2) || _world.InRoad(z, y - 14)) continue;
                            float x = a + 8;
                            while (x < b - 8)
                            {
                                if (_rng.NextFloat() < 0.72f)
                                {
                                    var h = lib.Houses[_rng.NextInt(lib.Houses.Count)];
                                    int hw = h.Image.Width;
                                    if (x + hw > b + 1) break;
                                    list.Add(House(x + hw / 2f, y + _rng.NextFloat() * 3, h));
                                    x += hw + 2 + _rng.NextFloat() * 4;
                                }
                                else
                                {
                                    list.Add(new Prop { X = x + 5, Y = y + _rng.NextFloat() * 6, Sprite = BiomeStyle.SpriteOf(_style.TownTree) });
                                    x += 11;
                                }
                            }
                        }
                    }
                    break;
                case ZoneType.City:
                case ZoneType.Terminal:
                case ZoneType.Station:
                {
                    RoadDecor(z, list, sea);
                    Lamps(z, yEnd, z.Type == ZoneType.City ? 34f : 40f, list, sea);
                    bool big = z.Type != ZoneType.Station;
                    if (z.Type == ZoneType.Station) list.Add(House(26, -24, lib.Station));
                    if (z.Type == ZoneType.Terminal) list.Add(House(26, _run.Length - 18, lib.Station));
                    int level = z.Type == ZoneType.Terminal && !_run.IsAttract ? _economy.CityLevel(_run.RouteIndex + 1) : 3;
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (sea && side > 0)
                        {
                            for (float y = z.Y0; y < yEnd; y += 50) if (_rng.NextFloat() < 0.6f) list.Add(Boat(134 + _rng.NextFloat() * 18, y));
                            continue;
                        }
                        float a = side < 0 ? LeftMin : RightMin, b = side < 0 ? LeftMax : RightMax;
                        for (float y = z.Y0 + 12; y < yEnd; y += 10 + _rng.NextFloat() * 5)
                        {
                            if (_world.InRoad(z, y, 2) || _world.InRoad(z, y - 12)) continue;
                            if (side < 0 && ((z.Type == ZoneType.Station && y > -50 && y < 4) || (z.Type == ZoneType.Terminal && y > _run.Length - 46 && y < _run.Length + 10))) continue;
                            float x = a + 1;
                            while (x < b - 6)
                            {
                                bool tall = big && _rng.NextFloat() < (z.Type == ZoneType.Terminal ? 0.15f + level * 0.1f : 0.35f);
                                var pool = tall ? lib.Towers : (_rng.NextFloat() < 0.3f && !big ? lib.Houses : lib.Buildings);
                                var o = pool[_rng.NextInt(pool.Count)];
                                int bw = o.Image.Width - 3;
                                if (x + bw > b)
                                {
                                    if (b - x > 9 && _rng.NextFloat() < 0.6f) list.Add(new Prop { X = x + 4, Y = y + 2, Sprite = lib.Tree });
                                    break;
                                }
                                list.Add(House(x + o.Image.Width / 2f, y + _rng.NextFloat() * 2, o));
                                x += bw + 1;
                            }
                        }
                    }
                    break;
                }
            }
        }

        private Prop House(float x, float y, BuildingSprite b) =>
            new Prop { X = x, Y = y, Sprite = b.Image, Windows = b.Windows, Seed = _rng.NextInt(1000000) };

        private Prop Boat(float x, float y) =>
            new Prop { X = x, Y = y, Sprite = SpriteLibrary.Instance.Boat, IsBoat = true, BoatPhase = _rng.NextFloat() * 6 };

        private void Lamps(Zone z, float yEnd, float step, List<Prop> list, bool sea)
        {
            var lamp = SpriteLibrary.Instance.Lamp;
            for (float y = z.Y0 + 10; y < yEnd; y += step)
            {
                if (_world.InRoad(z, y, 3)) continue;
                list.Add(new Prop { X = 45, Y = y, Sprite = lamp, Light = 11 });
                if (!sea) list.Add(new Prop { X = 115, Y = y + step / 2, Sprite = lamp, Light = 11 });
            }
        }

        private void RoadDecor(Zone z, List<Prop> list, bool sea)
        {
            var lib = SpriteLibrary.Instance;
            foreach (float ry in z.Roads)
            {
                list.Add(new Prop { X = 40, Y = ry - 1, Sprite = lib.Barrier });
                if (!sea) list.Add(new Prop { X = 120, Y = ry + 10, Sprite = lib.BarrierLeft });
                if (_rng.NextFloat() < 0.7f) list.Add(new Prop { X = 20 + _rng.NextFloat() * 16, Y = ry + 6, Sprite = lib.Cars[_rng.NextInt(lib.Cars.Length)], Car = 1 });
                if (!sea && _rng.NextFloat() < 0.6f) list.Add(new Prop { X = 126 + _rng.NextFloat() * 22, Y = ry + 2, Sprite = lib.CarsLeft[_rng.NextInt(lib.CarsLeft.Length)], Car = -1 });
            }
        }
    }
}
