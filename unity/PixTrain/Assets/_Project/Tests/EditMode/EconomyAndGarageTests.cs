using NUnit.Framework;
using PixTrain.Core.Data;

namespace PixTrain.Core.Tests
{
    public class EconomyAndGarageTests
    {
        [Test]
        public void CityLevel_GrowsWithStarsAndCompletion_CappedAtFive()
        {
            var t = new TestGame();
            Assert.That(t.G.Economy.CityLevel(1), Is.EqualTo(1));

            t.Progress.BestStars[0] = 2;
            Assert.That(t.G.Economy.CityLevel(1), Is.EqualTo(3), "taban 1 + varış hattının 2 yıldızı");

            t.Progress.CompletedRoutes[1] = true;
            Assert.That(t.G.Economy.CityLevel(1), Is.EqualTo(4), "çıkış hattı tamamlanınca +1");

            t.Progress.BestStars[5] = 3;
            t.Progress.CompletedRoutes[5] = true;
            Assert.That(t.G.Economy.CityLevel(6), Is.EqualTo(5), "Başkent: 3 + 3 → en fazla 5");
            Assert.That(t.G.Economy.CityLevelName(6), Is.EqualTo("Metropol"));
        }

        [Test]
        public void IdleIncome_DependsOnCompletedRoutesAndLocomotive()
        {
            var t = new TestGame();
            Assert.That(t.G.Economy.IdleRatePerMinute(), Is.EqualTo(0));

            t.Progress.CompletedRoutes[0] = true;
            t.Progress.BestStars[0] = 3;
            Assert.That(t.G.Economy.IdleRatePerMinute(), Is.EqualTo(8));

            t.Progress.OwnedLocomotives[2] = true;
            t.Progress.SelectedLocomotive = 2;
            Assert.That(t.G.Economy.IdleRatePerMinute(), Is.EqualTo(16), "elektrikli ×2");
        }

        [Test]
        public void IdlePending_IsCappedAtFourHours_AndCollectResetsTimer()
        {
            var t = new TestGame();
            t.Progress.CompletedRoutes[0] = true; // 2/dk
            t.Time.UtcNow = t.Time.UtcNow.AddMinutes(30);
            Assert.That(t.G.Economy.IdlePending(), Is.EqualTo(60));

            t.Time.UtcNow = t.Time.UtcNow.AddHours(10);
            Assert.That(t.G.Economy.IdlePending(), Is.EqualTo(480), "240 dk × 2");

            int coins = t.G.Economy.Coins;
            int saves = t.Repo.SaveCount;
            Assert.That(t.G.Economy.CollectIdle(), Is.EqualTo(480));
            Assert.That(t.G.Economy.Coins, Is.EqualTo(coins + 480));
            Assert.That(t.G.Economy.IdlePending(), Is.EqualTo(0));
            Assert.That(t.Repo.SaveCount, Is.GreaterThan(saves));
        }

        [Test]
        public void TrySpend_FailsWhenNotEnoughCoins()
        {
            var t = new TestGame();
            Assert.That(t.G.Economy.TrySpend(151), Is.False);
            Assert.That(t.G.Economy.TrySpend(150), Is.True);
            Assert.That(t.G.Economy.Coins, Is.EqualTo(0));
        }

        [Test]
        public void BuyUpgrade_DeductsCoinsAndLevelsUp_UntilMax()
        {
            var t = new TestGame();
            t.Progress.Coins = 10000;

            Assert.That(t.G.Garage.NextUpgradeCost(UpgradeId.Wagon), Is.EqualTo(350));
            Assert.That(t.G.Garage.BuyUpgrade(UpgradeId.Wagon), Is.True);
            Assert.That(t.G.Economy.Coins, Is.EqualTo(9650));
            Assert.That(t.G.Garage.UpgradeLevel(UpgradeId.Wagon), Is.EqualTo(1));

            Assert.That(t.G.Garage.BuyUpgrade(UpgradeId.Wagon), Is.True);
            Assert.That(t.G.Garage.BuyUpgrade(UpgradeId.Wagon), Is.True);
            Assert.That(t.G.Garage.IsMaxed(UpgradeId.Wagon), Is.True);
            Assert.That(t.G.Garage.BuyUpgrade(UpgradeId.Wagon), Is.False);
            Assert.That(t.G.Garage.NextUpgradeCost(UpgradeId.Wagon), Is.EqualTo(-1));
        }

        [Test]
        public void BuyUpgrade_FailsWithoutCoins()
        {
            var t = new TestGame();
            t.Progress.Coins = 100;
            Assert.That(t.G.Garage.BuyUpgrade(UpgradeId.Engine), Is.False);
            Assert.That(t.G.Garage.UpgradeLevel(UpgradeId.Engine), Is.EqualTo(0));
            Assert.That(t.G.Economy.Coins, Is.EqualTo(100));
        }

        [Test]
        public void Locomotive_RequiresStarsThenCoins_ThenCanBeSelected()
        {
            var t = new TestGame();
            t.Progress.Coins = 5000;
            Assert.That(t.G.Garage.GetLocomotiveStatus(0), Is.EqualTo(LocomotiveStatus.Selected));
            Assert.That(t.G.Garage.GetLocomotiveStatus(1), Is.EqualTo(LocomotiveStatus.NeedsStars));
            Assert.That(t.G.Garage.BuyLocomotive(1), Is.False);

            t.Progress.BestStars[0] = 3;
            t.Progress.BestStars[1] = 3;
            Assert.That(t.G.Garage.GetLocomotiveStatus(1), Is.EqualTo(LocomotiveStatus.Available));
            Assert.That(t.G.Garage.BuyLocomotive(1), Is.True);
            Assert.That(t.G.Economy.Coins, Is.EqualTo(3500));
            Assert.That(t.G.Garage.GetLocomotiveStatus(1), Is.EqualTo(LocomotiveStatus.Selected));

            Assert.That(t.G.Garage.SelectLocomotive(0), Is.True);
            Assert.That(t.G.Garage.GetLocomotiveStatus(1), Is.EqualTo(LocomotiveStatus.Owned));
            Assert.That(t.G.Garage.SelectLocomotive(2), Is.False, "sahip olunmayan lokomotif seçilemez");
        }

        [Test]
        public void TrainSpec_ReflectsUpgradesAndLocomotive()
        {
            var t = new TestGame();
            var basic = t.G.Garage.BuildTrainSpec();
            Assert.That(basic.Wagons, Is.EqualTo(new[] { WagonType.Passenger, WagonType.Freight }));
            Assert.That(basic.Capacity, Is.EqualTo(3));
            Assert.That(basic.FuelMax, Is.EqualTo(100f));
            Assert.That(basic.CruiseSpeed, Is.EqualTo(48f).Within(1e-4f));

            t.Progress.UpgradeLevels[(int)UpgradeId.Wagon] = 2;
            t.Progress.UpgradeLevels[(int)UpgradeId.Capacity] = 1;
            t.Progress.UpgradeLevels[(int)UpgradeId.FuelTank] = 2;
            t.Progress.UpgradeLevels[(int)UpgradeId.Engine] = 5;
            t.Progress.OwnedLocomotives[1] = true;
            t.Progress.SelectedLocomotive = 1;
            var spec = t.G.Garage.BuildTrainSpec();

            Assert.That(spec.Wagons, Is.EqualTo(new[] { WagonType.Passenger, WagonType.Freight, WagonType.Freight, WagonType.Passenger }));
            Assert.That(spec.Capacity, Is.EqualTo(4));
            Assert.That(spec.FuelMax, Is.EqualTo(150f));
            Assert.That(spec.CruiseSpeed, Is.EqualTo(48f * 1.15f * 1.4f).Within(1e-3f));
            Assert.That(spec.FuelEfficiency, Is.EqualTo(0.8f));
        }

        [Test]
        public void Progress_NormalizeFixesOldSaves()
        {
            var t = new TestGame();
            var p = t.Progress;
            p.BestStars = new[] { 3 };
            p.UpgradeLevels = null;
            p.SelectedLocomotive = 2;
            p.Normalize(t.G.Catalog);

            Assert.That(p.BestStars.Length, Is.EqualTo(6));
            Assert.That(p.BestStars[0], Is.EqualTo(3));
            Assert.That(p.UpgradeLevels.Length, Is.EqualTo(5));
            Assert.That(p.SelectedLocomotive, Is.EqualTo(0), "sahip olunmayan seçim sıfırlanır");
        }
    }
}
