using System.Linq;
using NUnit.Framework;
using PixTrain.Core.Data;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Tests
{
    public class ContractServiceTests
    {
        [Test]
        public void LaterRoutes_GetTwoDifferentContracts_ScaledByRoute()
        {
            var t = new TestGame();
            for (int seed = 0; seed < 50; seed++)
            {
                var picked = t.G.Contracts.Pick(4, new SeededRandom(seed));
                Assert.That(picked.Count, Is.EqualTo(2));
                Assert.That(picked[0].Def.Metric, Is.Not.EqualTo(picked[1].Def.Metric));
                foreach (var c in picked) Assert.That(c.Target, Is.EqualTo(c.Def.TargetForRoute(4)));
            }
            var passengers = t.G.Catalog.Contract(ContractMetric.Passengers);
            Assert.That(passengers.TargetForRoute(4), Is.EqualTo(20));
            Assert.That(passengers.Title(20), Is.EqualTo("20 yolcu taşı"));
        }

        [Test]
        public void NoCrash_CountsOnlyOnArrival()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            var noCrash = new Models.ContractProgress { Def = t.G.Catalog.Contract(ContractMetric.NoCrash), Target = 1 };

            Assert.That(t.G.Contracts.IsReached(run, noCrash), Is.False, "yol boyunca tamamlanmış sayılmaz");
            Assert.That(t.G.Contracts.IsCompleted(run, noCrash, arrived: false), Is.False);
            Assert.That(t.G.Contracts.IsCompleted(run, noCrash, arrived: true), Is.True);

            run.Crashes = 1;
            Assert.That(t.G.Contracts.IsCompleted(run, noCrash, arrived: true), Is.False);
        }

        [Test]
        public void NightCoins_TrackOnlyNightEarnings()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            run.TimeOfDay = 0.5f;
            t.AddGate(run, GateKind.Coin, 1, 200f);
            t.DriveThrough(run, 200f);
            run.TimeOfDay = 0f;
            t.AddGate(run, GateKind.Coin, 1, 300f);
            t.DriveThrough(run, 300f);

            Assert.That(t.G.Contracts.CurrentValue(run, ContractMetric.Coins), Is.EqualTo(6));
            Assert.That(t.G.Contracts.CurrentValue(run, ContractMetric.NightCoins), Is.EqualTo(3));
            Assert.That(run.Contracts.Select(c => c.Def.Metric).Contains(ContractMetric.Coins), Is.True);
        }
    }
}
