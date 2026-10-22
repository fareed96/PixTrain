using System.Collections.Generic;
using NUnit.Framework;
using PixTrain.Core.Data;
using PixTrain.Core.Models;

namespace PixTrain.Core.Tests
{
    public class WorldGenServiceTests
    {
        /// <summary>Hattın tamamını üretir; gerekirse temizlik yapılmaması için doğrudan dünya servisini kullanır.</summary>
        private static RunState GenerateWholeRoute(TestGame t, int route, int seed, List<Gate> gates, List<Junction> junctions, List<Zone> zones)
        {
            var run = t.G.Runs.StartRun(route, false, seed);
            run.Fuel = run.FuelMax;
            for (float y = 0; y <= run.Length; y += 50f)
            {
                run.Y = y;
                t.G.World.EnsureGenerated(run);
            }
            gates.AddRange(run.Gates);
            junctions.AddRange(run.Junctions);
            zones.AddRange(run.Zones);
            return run;
        }

        [Test]
        public void Zones_AreContiguous_AndEndWithTerminal([Values(0, 1, 2, 3, 4, 5)] int route)
        {
            var t = new TestGame();
            var gates = new List<Gate>(); var junctions = new List<Junction>(); var zones = new List<Zone>();
            var run = GenerateWholeRoute(t, route, 42 + route, gates, junctions, zones);

            Assert.That(zones[0].Type, Is.EqualTo(ZoneType.Station));
            for (int i = 1; i < zones.Count; i++)
                Assert.That(zones[i].Y0, Is.EqualTo(zones[i - 1].Y1), "bölgeler arasında boşluk olmamalı");
            Assert.That(zones[zones.Count - 1].Type, Is.EqualTo(ZoneType.Terminal));
            Assert.That(zones[zones.Count - 1].Y0, Is.EqualTo(run.Length - t.B.TerminalZoneOffset));
            var last = gates[gates.Count - 1];
            Assert.That(last.Kind, Is.EqualTo(GateKind.Terminal));
            Assert.That(last.Y, Is.EqualTo(run.Length));
        }

        [Test]
        public void GatesAreSorted_AndNoneInsideTunnels_NoJunctionInRiverOrTunnel([Values(1, 3, 5)] int route)
        {
            var t = new TestGame();
            for (int seed = 0; seed < 8; seed++)
            {
                var gates = new List<Gate>(); var junctions = new List<Junction>(); var zones = new List<Zone>();
                var run = GenerateWholeRoute(t, route, seed, gates, junctions, zones);

                for (int i = 1; i < gates.Count; i++)
                    Assert.That(gates[i].Y, Is.GreaterThanOrEqualTo(gates[i - 1].Y));
                foreach (var g in gates)
                    Assert.That(t.G.World.ZoneAt(run, g.Y).Type, Is.Not.EqualTo(ZoneType.Tunnel), "tünelde peron olmamalı");
                foreach (var j in junctions)
                {
                    var a = t.G.World.ZoneAt(run, j.Y).Type;
                    var b = t.G.World.ZoneAt(run, j.Y + t.B.JunctionLength).Type;
                    Assert.That(a != ZoneType.River && a != ZoneType.Tunnel && b != ZoneType.River && b != ZoneType.Tunnel,
                        "makas köprüde ya da tünelde olmamalı (y=" + j.Y + ")");
                }
            }
        }

        [Test]
        public void SameSeed_ProducesSameRoute()
        {
            var t = new TestGame();
            var g1 = new List<Gate>(); var g2 = new List<Gate>();
            GenerateWholeRoute(t, 0, 777, g1, new List<Junction>(), new List<Zone>());
            GenerateWholeRoute(t, 0, 777, g2, new List<Junction>(), new List<Zone>());

            Assert.That(g2.Count, Is.EqualTo(g1.Count));
            for (int i = 0; i < g1.Count; i++)
            {
                Assert.That(g2[i].Kind, Is.EqualTo(g1[i].Kind));
                Assert.That(g2[i].Y, Is.EqualTo(g1[i].Y));
                Assert.That(g2[i].Lane, Is.EqualTo(g1[i].Lane));
            }
        }

        [Test]
        public void EverySegmentLeavesASafeLane()
        {
            var t = new TestGame();
            for (int seed = 0; seed < 20; seed++)
            {
                var gates = new List<Gate>(); var junctions = new List<Junction>();
                GenerateWholeRoute(t, 3, seed, gates, junctions, new List<Zone>());
                for (int i = 0; i + 1 < junctions.Count; i++)
                {
                    float y0 = junctions[i].Y, y1 = junctions[i + 1].Y;
                    if (y1 - y0 > 260f) continue; // arada makassız parça var; tek parça değil
                    var hazardLanes = new HashSet<int>();
                    foreach (var g in gates)
                        if (g.Y >= y0 && g.Y < y1 && t.G.Catalog.Station(g.Kind).Category == GateCategory.Hazard) hazardLanes.Add(g.Lane);
                    Assert.That(hazardLanes.Count, Is.LessThan(3), "üç şerit birden kapalı olmamalı");
                }
            }
        }

        [Test]
        public void LowFuel_AddsFuelStationToNextSegment()
        {
            var t = new TestGame();
            var run = t.G.Runs.StartRun(0, false, 5);
            int before = run.Gates.Count;
            run.Fuel = run.FuelMax * 0.2f;
            t.G.World.GenerateSegment(run);

            bool hasFuel = false;
            for (int i = before; i < run.Gates.Count; i++) if (run.Gates[i].Kind == GateKind.Fuel) hasFuel = true;
            Assert.That(hasFuel, Is.True);
        }
    }
}
