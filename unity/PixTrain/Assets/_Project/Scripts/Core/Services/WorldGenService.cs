using System;
using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    public interface IWorldGenService
    {
        /// <summary>Trenin önünde yeterli bölge ve hat parçası olmasını sağlar.</summary>
        void EnsureGenerated(RunState run);
        void ExtendZones(RunState run, float upto);
        void GenerateSegment(RunState run);
        Zone ZoneAt(RunState run, float y);
        bool InRoad(Zone zone, float y, float pad = 0f);
        FieldPatch FieldAt(Zone zone, float x, float y);
    }

    /// <summary>
    /// Prosedürel hat üretimi. Bölgeler (kır, kasaba, şehir, nehir, tünel) manzarayı belirler;
    /// her ~200 px'lik parça bir makas ve şerit başına bir içerik (peron, altın, tehlike) taşır.
    /// Manzara objeleri (ağaç, bina) görsel katmanda bölgelerden üretilir.
    /// </summary>
    public sealed class WorldGenService : IWorldGenService
    {
        private enum Slot { Pickup, Delivery, Coin, Hazard, Fuel, Empty }

        private static readonly CropType[] Crops = { CropType.Wheat, CropType.Green, CropType.Plowed, CropType.Wheat };

        private readonly GameCatalog _catalog;

        public WorldGenService(GameCatalog catalog)
        {
            _catalog = catalog;
        }

        private GameBalance B => _catalog.Balance;

        public void EnsureGenerated(RunState run)
        {
            ExtendZones(run, run.Y + B.ZonesAhead);
            while (run.GenY < run.Y + B.GenerateAhead) GenerateSegment(run);
        }

        public Zone ZoneAt(RunState run, float y)
        {
            var zones = run.Zones;
            int i = Math.Min(run.ZoneCursorIndex, zones.Count - 1);
            if (i < 0) i = 0;
            while (i > 0 && y < zones[i].Y0) i--;
            while (i < zones.Count - 1 && y >= zones[i].Y1) i++;
            run.ZoneCursorIndex = i;
            return zones[i];
        }

        public bool InRoad(Zone zone, float y, float pad = 0f)
        {
            foreach (float r in zone.Roads) if (y >= r - pad && y < r + 9f + pad) return true;
            return false;
        }

        public FieldPatch FieldAt(Zone zone, float x, float y)
        {
            foreach (var f in zone.Fields) if (f.Contains(x, y)) return f;
            return null;
        }

        public void ExtendZones(RunState run, float upto)
        {
            var rng = run.ZoneRng;
            var biome = run.Biome;
            while (run.ZoneCursorY < upto)
            {
                float y0 = run.ZoneCursorY;
                bool first = run.Zones.Count == 0;
                ZoneType prev = first ? ZoneType.Station : run.Zones[run.Zones.Count - 1].Type;
                ZoneType type;
                float length;
                if (first) { type = ZoneType.Station; length = B.StationZoneLength; }
                else if (!run.IsAttract && y0 >= run.Length - B.TerminalZoneOffset) { type = ZoneType.Terminal; length = 1e7f; }
                else if (prev != ZoneType.Countryside) { type = ZoneType.Countryside; length = 260f + rng.NextFloat() * 360f; }
                else
                {
                    float r = rng.NextFloat();
                    float river = biome.RiverChance, tunnel = biome.TunnelChance;
                    if (r < river) { type = ZoneType.River; length = 56f + rng.NextFloat() * 26f; }
                    else if (r < river + tunnel) { type = ZoneType.Tunnel; length = 130f + rng.NextFloat() * 90f; }
                    else if (r < river + tunnel + 0.34f) { type = ZoneType.Town; length = 220f + rng.NextFloat() * 160f; }
                    else if (r < river + tunnel + 0.5f) { type = ZoneType.City; length = 220f + rng.NextFloat() * 140f; }
                    else { type = ZoneType.Countryside; length = 200f + rng.NextFloat() * 300f; }
                }
                length = (float)Math.Round(length);

                // Varış bölgesine taşan bölgeyi kırp; kırpılınca çok kısalan nehir/tünel kır olur.
                if (!run.IsAttract && type != ZoneType.Terminal && type != ZoneType.Station && y0 + length > run.Length - B.TerminalZoneOffset)
                {
                    length = run.Length - B.TerminalZoneOffset - y0;
                    if ((type == ZoneType.River && length < 56f) || (type == ZoneType.Tunnel && length < 100f)) type = ZoneType.Countryside;
                }

                var zone = new Zone { Type = type, Y0 = y0, Y1 = y0 + length };
                if (type == ZoneType.Town || type == ZoneType.City || type == ZoneType.Terminal || type == ZoneType.Station)
                    AddRoads(run, zone, rng);
                if (type == ZoneType.Station) { zone.HasPlatform = true; zone.PlatformY0 = -110f; zone.PlatformY1 = 30f; }
                if (type == ZoneType.Terminal) { zone.HasPlatform = true; zone.PlatformY0 = run.Length - 120f; zone.PlatformY1 = run.Length + 40f; }
                if (type == ZoneType.Countryside && biome.HasFields) AddFields(zone, rng);

                run.Zones.Add(zone);
                run.ZoneCursorY = zone.Y1;
            }
        }

        private void AddRoads(RunState run, Zone zone, IRandom rng)
        {
            float end = zone.Type == ZoneType.Terminal ? run.Length + 380f : zone.Y1 - 30f;
            for (float y = zone.Y0 + 34f + rng.NextFloat() * 30f; y < end; y += 74f + rng.NextFloat() * 40f)
            {
                if (zone.Type == ZoneType.Station && y > -110f && y < 110f) continue;
                if (zone.Type == ZoneType.Terminal && y > run.Length - 120f && y < run.Length + 90f) continue;
                zone.Roads.Add((float)Math.Round(y));
            }
        }

        private static void AddFields(Zone zone, IRandom rng)
        {
            foreach (int side in new[] { -1, 1 })
            {
                for (float y = zone.Y0 + 10f; y < zone.Y1 - 40f; y += 60f + rng.NextFloat() * 50f)
                {
                    if (rng.NextFloat() < 0.4f) continue;
                    float y1 = Math.Min(zone.Y1 - 6f, y + 34f + rng.NextFloat() * 40f);
                    zone.Fields.Add(new FieldPatch
                    {
                        X0 = side < 0 ? 2 + (int)(rng.NextFloat() * 8f) : 117f,
                        X1 = side < 0 ? 44f : 158 - (int)(rng.NextFloat() * 8f),
                        Y0 = (float)Math.Round(y),
                        Y1 = (float)Math.Round(y1),
                        Crop = Crops[rng.NextInt(Crops.Length)],
                    });
                }
            }
        }

        public void GenerateSegment(RunState run)
        {
            var rng = run.Rng;
            float y0 = run.GenY;
            if (!run.IsAttract && y0 > run.Length - B.FinalSegmentOffset)
            {
                run.Gates.Add(new Gate { Kind = GateKind.Terminal, Y = run.Length, Lane = -1 });
                run.GenY = float.PositiveInfinity;
                return;
            }

            float progress = run.IsAttract ? 0.25f : y0 / run.Length;
            float length = (float)Math.Round(MathUtil.Lerp(215f, 165f, progress) + rng.NextFloat() * 40f);
            var zoneA = ZoneAt(run, y0);
            var zoneB = ZoneAt(run, y0 + B.JunctionLength);
            bool canSwitch = !BlocksSwitch(zoneA.Type) && !BlocksSwitch(zoneB.Type)
                             && !InRoad(zoneA, y0, 2f) && !InRoad(zoneB, y0 + B.JunctionLength, 2f);
            if (canSwitch) run.Junctions.Add(new Junction { Y = y0 });

            // Şerit başına içerik; aynı parçada en fazla iki tehlike, yani her zaman güvenli bir şerit var.
            var slotTable = new WeightedTable<Slot>()
                .Add(Slot.Pickup, 28f).Add(Slot.Delivery, 22f).Add(Slot.Coin, 17f)
                .Add(Slot.Hazard, 15f + progress * 14f).Add(Slot.Fuel, 7f).Add(Slot.Empty, 8f);
            var slots = new Slot[3];
            int hazards = 0;
            for (int i = 0; i < 3; i++)
            {
                var s = slotTable.Pick(rng);
                if (s == Slot.Hazard) { if (hazards >= 2) s = Slot.Coin; else hazards++; }
                slots[i] = s;
            }
            // Görünmez merhamet: yakıt azsa bu parçaya bir yakıt peronu konur.
            if (!run.IsAttract && run.Fuel < run.FuelMax * B.FuelMercyThreshold && Array.IndexOf(slots, Slot.Fuel) < 0)
            {
                int hazardIndex = Array.IndexOf(slots, Slot.Hazard);
                int i = hazardIndex >= 0 && hazards == 2 ? hazardIndex : rng.NextInt(3);
                slots[i] = Slot.Fuel;
            }

            float a = y0 + B.JunctionLength + 16f, b = y0 + length - 14f;
            var batch = new List<Gate>();
            for (int lane = 0; lane < 3; lane++)
            {
                float y = (float)Math.Round(a + rng.NextFloat() * Math.Max(4f, b - a - 34f));
                switch (slots[lane])
                {
                    case Slot.Pickup: batch.Add(new Gate { Kind = run.Biome.Pickups.Pick(rng), Lane = lane, Y = y }); break;
                    case Slot.Delivery: batch.Add(new Gate { Kind = run.Biome.Deliveries.Pick(rng), Lane = lane, Y = y }); break;
                    case Slot.Fuel: batch.Add(new Gate { Kind = GateKind.Fuel, Lane = lane, Y = y }); break;
                    case Slot.Coin:
                        for (int k = 0; k < 4; k++) batch.Add(new Gate { Kind = GateKind.Coin, Lane = lane, Y = y + k * 9f });
                        break;
                    case Slot.Hazard:
                        var hazard = run.Biome.Hazards.Pick(rng);
                        var gate = new Gate { Kind = hazard, Lane = lane, Y = y };
                        if (hazard == GateKind.Cow)
                        {
                            gate.X = B.LaneX[lane];
                            gate.Direction = rng.NextFloat() < 0.5f ? -1 : 1;
                            gate.SpeedX = 5f + rng.NextFloat() * 7f;
                        }
                        batch.Add(gate);
                        break;
                }
            }
            batch.Sort((p, q) => p.Y.CompareTo(q.Y));
            foreach (var g in batch)
                if (ZoneAt(run, g.Y).Type != ZoneType.Tunnel) run.Gates.Add(g);

            // Karşıdan gelen tren: tünelde karşılaşma olmasın diye yakın tünel varsa çıkmaz.
            if (progress > 0.12f && rng.NextFloat() < 0.1f + progress * 0.12f)
            {
                float oy = (float)Math.Round(y0 + length * 0.6f);
                int lane = rng.NextInt(3);
                float speed = 26f + rng.NextFloat() * 10f;
                bool nearTunnel = false;
                foreach (float d in new[] { 0f, 150f, 300f }) if (ZoneAt(run, oy - d).Type == ZoneType.Tunnel) nearTunnel = true;
                if (!nearTunnel) run.Oncoming.Add(new OncomingTrain { Y = oy, Lane = lane, Speed = speed });
            }

            run.GenY = y0 + length;
        }

        private static bool BlocksSwitch(ZoneType type) => type == ZoneType.River || type == ZoneType.Tunnel;
    }
}
