using NUnit.Framework;
using PixTrain.Core.Data;

namespace PixTrain.Core.Tests
{
    public class CargoServiceTests
    {
        private static readonly CargoType[] CityAccepts = { CargoType.Passenger, CargoType.Food };

        [Test]
        public void Load_FillsOnlyMatchingWagons_UpToCapacity()
        {
            var t = new TestGame();
            var run = t.CleanRun();

            int loaded = t.G.Cargo.Load(run, CargoType.Coal, 10);

            Assert.That(loaded, Is.EqualTo(3), "başlangıçta tek yük vagonu, kapasite 3");
            var s = t.G.Cargo.Summarize(run);
            Assert.That(s.Coal, Is.EqualTo(3));
            Assert.That(s.Passengers, Is.EqualTo(0));
            Assert.That(t.G.Cargo.FreeCapacity(run, CargoType.Food), Is.EqualTo(0), "kömür ve gıda aynı yük vagonunu paylaşır");
            Assert.That(t.G.Cargo.FreeCapacity(run, CargoType.Passenger), Is.EqualTo(3));
        }

        [Test]
        public void Load_WhenFull_ReturnsZero()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.Fill(run, CargoType.Passenger, 3);

            Assert.That(t.G.Cargo.Load(run, CargoType.Passenger, 3), Is.EqualTo(0));
        }

        [Test]
        public void Quote_PassengersPayMoreAtNight()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.Fill(run, CargoType.Passenger, 2);

            run.TimeOfDay = 0.5f;
            int day = t.G.Cargo.Quote(run, CityAccepts, 1f).Pay;
            run.TimeOfDay = 0f;
            int night = t.G.Cargo.Quote(run, CityAccepts, 1f).Pay;

            Assert.That(day, Is.EqualTo(18));
            Assert.That(night, Is.EqualTo(27));
        }

        [Test]
        public void Deliver_RemovesOnlyAcceptedCargo_AndCountsIt()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            run.TimeOfDay = 0.5f;
            t.Fill(run, CargoType.Passenger, 3);
            t.Fill(run, CargoType.Coal, 1);
            t.Fill(run, CargoType.Food, 2);

            var q = t.G.Cargo.Deliver(run, CityAccepts, 1f);

            Assert.That(q.Count, Is.EqualTo(5));
            Assert.That(q.Pay, Is.EqualTo(3 * 9 + 2 * 6));
            var s = t.G.Cargo.Summarize(run);
            Assert.That(s.Coal, Is.EqualTo(1), "şehir kömür almaz");
            Assert.That(s.Passengers + s.Food, Is.EqualTo(0));
            Assert.That(run.DeliveredCount(CargoType.Passenger), Is.EqualTo(3));
            Assert.That(run.DeliveredCount(CargoType.Food), Is.EqualTo(2));
        }

        [Test]
        public void Quote_AppliesComboFromSixUnits()
        {
            var t = new TestGame();
            t.Progress.UpgradeLevels[(int)UpgradeId.Capacity] = 3; // vagon başına 6
            var run = t.CleanRun();
            run.TimeOfDay = 0.5f;
            t.Fill(run, CargoType.Passenger, 6);

            Assert.That(t.G.Cargo.Quote(run, CityAccepts, 1f).Pay, Is.EqualTo(68), "6 × 9 × 1.25 = 67.5 → 68");
        }

        [Test]
        public void Quote_ScalesWithRouteMultiplier()
        {
            var t = new TestGame();
            var run = t.CleanRun(route: 2);
            run.TimeOfDay = 0.5f;
            t.Fill(run, CargoType.Coal, 2);

            Assert.That(t.G.Cargo.Quote(run, new[] { CargoType.Coal }, 1f).Pay, Is.EqualTo(21), "2 × 8 × 1.3 = 20.8");
        }

        [Test]
        public void LoseRandom_NeverLosesMoreThanLoaded()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.Fill(run, CargoType.Food, 1);

            Assert.That(t.G.Cargo.LoseRandom(run, 2), Is.EqualTo(1));
            Assert.That(t.G.Cargo.LoseRandom(run, 2), Is.EqualTo(0));
        }
    }
}
