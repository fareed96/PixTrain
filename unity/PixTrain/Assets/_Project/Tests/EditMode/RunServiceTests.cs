using System.Linq;
using NUnit.Framework;
using PixTrain.Core.Data;
using PixTrain.Core.Models;

namespace PixTrain.Core.Tests
{
    public class RunServiceTests
    {
        private static bool Has(RunState run, RunEventType type) => run.Events.Any(e => e.Type == type);

        [Test]
        public void StartRun_BuildsTrainFromGarage_AndTutorialContracts()
        {
            var t = new TestGame();
            var run = t.G.Runs.StartRun(0, false, 3);

            Assert.That(run.Wagons.Select(w => w.Type), Is.EqualTo(new[] { WagonType.Passenger, WagonType.Freight }));
            Assert.That(run.Fuel, Is.EqualTo(run.FuelMax));
            Assert.That(run.Lane, Is.EqualTo(1));
            Assert.That(run.Contracts.Select(c => c.Def.Metric), Is.EqualTo(new[] { ContractMetric.Passengers, ContractMetric.Coins }));
            Assert.That(run.Gates[0].IsOrigin, Is.True);
            Assert.That(run.Junctions.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Steer_MovesOneStepAtATime_AndStopsAtTheEdge()
        {
            var t = new TestGame();
            var run = t.CleanRun();

            Assert.That(t.G.Runs.Steer(run, 1), Is.True);
            Assert.That(run.SwitchDir, Is.EqualTo(1));
            Assert.That(t.G.Runs.Steer(run, 1), Is.False, "orta şeritten iki adım sağa gidilemez");
            Assert.That(Has(run, RunEventType.SwitchBlocked), Is.True);

            Assert.That(t.G.Runs.Steer(run, -1), Is.True);
            Assert.That(t.G.Runs.Steer(run, -1), Is.True);
            Assert.That(run.SwitchDir, Is.EqualTo(-1));

            run.Lane = 0;
            run.SwitchDir = 0;
            Assert.That(t.G.Runs.Steer(run, -1), Is.False, "en sol şeritte sola makas yok");
        }

        [Test]
        public void PassingJunction_ChangesLane_AndTrainFollowsTheCurve()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            run.Junctions.Add(new Junction { Y = 300f });
            t.G.Runs.Steer(run, 1);

            t.DriveThrough(run, 300f);

            Assert.That(run.Lane, Is.EqualTo(2));
            Assert.That(run.SwitchDir, Is.EqualTo(0), "makas geçince düze döner");
            Assert.That(t.G.Runs.TrainXAt(run, 299f), Is.EqualTo(t.B.LaneX[1]));
            Assert.That(t.G.Runs.TrainXAt(run, 300f + t.B.JunctionLength / 2), Is.EqualTo(92f).Within(0.01f));
            Assert.That(t.G.Runs.TrainXAt(run, 300f + t.B.JunctionLength), Is.EqualTo(t.B.LaneX[2]));
        }

        [Test]
        public void Coin_OnlyCollectedInTheSameLane()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            var mine = t.AddGate(run, GateKind.Coin, 1, 200f);
            var other = t.AddGate(run, GateKind.Coin, 0, 200f);
            int coins = t.G.Economy.Coins;

            t.DriveThrough(run, 200f);

            Assert.That(mine.Hit, Is.True);
            Assert.That(other.Hit, Is.False);
            Assert.That(t.G.Economy.Coins, Is.EqualTo(coins + 3));
            Assert.That(run.Earned, Is.EqualTo(3f));
        }

        [Test]
        public void Pickup_ThenDelivery_PaysAndBuildsStreak()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            run.TimeOfDay = 0.45f;
            t.AddGate(run, GateKind.Town, 1, 200f);
            t.AddGate(run, GateKind.City, 1, 400f);

            t.DriveThrough(run, 200f);
            Assert.That(t.G.Cargo.Summarize(run).Passengers, Is.EqualTo(3));
            Assert.That(run.Streak, Is.EqualTo(1));
            Assert.That(Has(run, RunEventType.Pickup), Is.True);

            var preview = t.G.Runs.Preview(run, run.Gates.First(g => g.Kind == GateKind.City));
            t.DriveThrough(run, 400f);
            var delivery = run.Events.First(e => e.Type == RunEventType.Delivery);

            Assert.That(delivery.Count, Is.EqualTo(3));
            Assert.That(delivery.Value, Is.EqualTo(28), "3 × 9 = 27, seri ×1.05 → 28");
            Assert.That(preview.Text, Is.EqualTo("+28"), "peron üstündeki tahmin gerçek kazançla aynı");
            Assert.That(run.Streak, Is.EqualTo(2));
            Assert.That(run.DeliveredCount(CargoType.Passenger), Is.EqualTo(3));
        }

        [Test]
        public void Pickup_WhenFull_ShowsDoluAndKeepsStreak()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.Fill(run, CargoType.Coal, 3);
            run.Streak = 4;
            var mine = t.AddGate(run, GateKind.Mine, 1, 200f);

            Assert.That(t.G.Runs.Preview(run, mine).Text, Is.EqualTo("DOLU"));
            t.DriveThrough(run, 200f);
            Assert.That(Has(run, RunEventType.WagonsFull), Is.True);
            Assert.That(run.Streak, Is.EqualTo(4));
        }

        [Test]
        public void Hazard_CrashesLosesCargoAndResetsStreak()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.Fill(run, CargoType.Passenger, 3);
            run.Streak = 7;
            t.AddGate(run, GateKind.Rock, 1, 200f);
            float fuel = run.Fuel;

            t.DriveThrough(run, 200f);

            Assert.That(run.Crashes, Is.EqualTo(1));
            Assert.That(run.Streak, Is.EqualTo(0));
            Assert.That(t.G.Cargo.Summarize(run).Passengers, Is.EqualTo(1));
            Assert.That(run.Fuel, Is.LessThan(fuel - 7.9f));
            Assert.That(run.SlowTimer, Is.GreaterThan(0f));
            Assert.That(Has(run, RunEventType.Crash), Is.True);
        }

        [Test]
        public void Horn_ScaresCowOffTheTrack()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            var cow = t.AddGate(run, GateKind.Cow, 1, 150f);
            run.Y = 100f;

            Assert.That(t.G.Runs.Horn(run), Is.True);
            Assert.That(cow.Fleeing, Is.True);
            Assert.That(t.G.Runs.Horn(run), Is.False, "düdük bekleme süresinde");

            t.DriveThrough(run, 150f);
            Assert.That(run.Crashes, Is.EqualTo(0));
        }

        [Test]
        public void Cow_WithoutHorn_CausesCrash()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.AddGate(run, GateKind.Cow, 1, 150f);

            t.DriveThrough(run, 150f);
            Assert.That(run.Crashes, Is.EqualTo(1));
        }

        [Test]
        public void Turbo_DoublesCoins_AndBurnsFuelTwiceAsFast()
        {
            var t = new TestGame();
            var normal = t.CleanRun();
            normal.Y = 100f; normal.Speed = 50f;
            t.G.Runs.Tick(normal, 0.1f);
            float normalBurn = normal.FuelMax - normal.Fuel;

            var run = t.CleanRun();
            Assert.That(t.G.Runs.Turbo(run), Is.True);
            Assert.That(t.G.Runs.Turbo(run), Is.False, "turbo bekleme süresinde");
            t.AddGate(run, GateKind.Coin, 1, 200f);
            run.Y = 199f; run.Speed = 50f;
            float fuel = run.Fuel;
            float y = run.Y;
            t.G.Runs.Tick(run, 0.1f);

            Assert.That(run.Events.First(e => e.Type == RunEventType.Coin).Value, Is.EqualTo(6));
            float burnPerPixel = (fuel - run.Fuel) / (run.Y - y);
            Assert.That(burnPerPixel, Is.EqualTo(2f * normalBurn / (normal.Y - 100f)).Within(1e-4f));
        }

        [Test]
        public void OncomingTrain_InSameLaneCrashes_OtherLanePasses()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            run.Oncoming.Add(new OncomingTrain { Y = 210f, Lane = 1, Speed = 30f });
            run.Oncoming.Add(new OncomingTrain { Y = 210f, Lane = 0, Speed = 30f });
            t.Fill(run, CargoType.Passenger, 3);
            t.Fill(run, CargoType.Coal, 3);

            run.Y = 100f; run.Speed = 50f;
            t.G.Runs.Tick(run, 0.1f);
            Assert.That(Has(run, RunEventType.OncomingWarning), Is.True, "aynı şeritteki tren için uyarı");

            for (int i = 0; i < 30 && !run.Oncoming.All(o => o.Passed); i++) t.G.Runs.Tick(run, 0.1f);

            Assert.That(run.Crashes, Is.EqualTo(1));
            var s = t.G.Cargo.Summarize(run);
            Assert.That(s.Passengers + s.Coal, Is.EqualTo(2), "karşı tren kazası 4 yük düşürür");
        }

        [Test]
        public void RunningOutOfFuel_EndsRunWithoutStars_ButKeepsCoins()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.AddGate(run, GateKind.Coin, 1, 120f);
            run.Fuel = 1f;
            run.Y = 100f;
            run.Speed = 50f;

            bool offered = false;
            for (int i = 0; i < 400 && !run.IsStalled; i++)
            {
                t.G.Runs.Tick(run, 0.05f);
                offered |= Has(run, RunEventType.ContinueOffered);
            }

            Assert.That(run.IsStalled, Is.True, "tren durur ve devam teklifi bekler");
            Assert.That(offered, Is.True);
            float y = run.Y;
            t.G.Runs.Tick(run, 1f);
            Assert.That(run.Y, Is.EqualTo(y), "karar beklenirken tren kıpırdamaz");

            t.G.Runs.GiveUp(run);
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.Result.Arrived, Is.False);
            Assert.That(run.Result.Stars, Is.EqualTo(0));
            Assert.That(t.Progress.CompletedRoutes[0], Is.False);
            Assert.That(t.G.Economy.Coins, Is.EqualTo(153), "yolda kazanılan altın kalır");
        }

        [Test]
        public void Arrival_UnloadsAtOneAndAHalf_AwardsStars_UnlocksNextRoute_GrowsCity()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            run.TimeOfDay = 0.45f;
            t.Fill(run, CargoType.Passenger, 3);
            t.Fill(run, CargoType.Food, 2);
            run.Delivered[(int)CargoType.Passenger] = 8; // yolcu sözleşmesi hedefi
            run.Y = run.Length - 0.6f;
            run.Speed = 20f;

            t.G.Runs.Tick(run, 0.1f);

            Assert.That(run.IsFinished, Is.True);
            var r = run.Result;
            Assert.That(r.Arrived, Is.True);
            Assert.That(r.FinalPay, Is.EqualTo(MathUtilRound((3 * 9 + 2 * 6) * 1.5f)));
            Assert.That(r.Contracts[0].Completed, Is.True, "8 yolcu");
            Assert.That(r.Contracts[1].Completed, Is.False, "160 altın kazanılmadı");
            Assert.That(r.Stars, Is.EqualTo(2));
            Assert.That(r.CityLevelBefore, Is.EqualTo(1));
            Assert.That(r.CityLevelAfter, Is.EqualTo(3));
            Assert.That(r.CityGrew, Is.True);
            Assert.That(t.Progress.CompletedRoutes[0], Is.True);
            Assert.That(t.Progress.BestStars[0], Is.EqualTo(2));
            Assert.That(t.Progress.UnlockedRoute, Is.EqualTo(1));
            Assert.That(t.Progress.SeenTutorial, Is.True);
            Assert.That(Has(run, RunEventType.Finished), Is.True);
        }

        [Test]
        public void AttractRun_NeverEnds_AndNeverPays()
        {
            var t = new TestGame();
            var run = t.G.Runs.StartRun(0, true, 11);
            int coins = t.G.Economy.Coins;

            for (int i = 0; i < 3000; i++) t.G.Runs.Tick(run, 0.05f);

            Assert.That(run.IsFinished, Is.False);
            Assert.That(run.Y, Is.GreaterThan(5000f));
            Assert.That(t.G.Economy.Coins, Is.EqualTo(coins));
            Assert.That(run.Gates.Count, Is.LessThan(200), "geride kalan içerik temizlenir");
        }

        [Test]
        public void FullRuns_AlwaysTerminate([Values(0, 1, 2, 3, 4, 5)] int route)
        {
            var t = new TestGame();
            for (int seed = 0; seed < 3; seed++)
            {
                var run = t.G.Runs.StartRun(route, false, seed * 101 + route);
                int ticks = 0;
                while (!run.IsFinished && ticks < 20000)
                {
                    if (run.IsStalled) t.G.Runs.GiveUp(run);
                    if (ticks % 40 == 0) t.G.Runs.Steer(run, (ticks / 40) % 3 - 1);
                    t.G.Runs.Tick(run, 1f / 30f);
                    ticks++;
                }
                Assert.That(run.IsFinished, Is.True, "sefer bitmeli (hat " + route + ", tohum " + seed + ")");
                Assert.That(run.Result, Is.Not.Null);
            }
        }

        private static int MathUtilRound(float v) => Utils.MathUtil.RoundHalfUp(v);
    }
}
