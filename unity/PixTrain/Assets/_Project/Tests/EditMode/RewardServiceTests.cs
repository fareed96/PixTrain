using System.Linq;
using NUnit.Framework;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Services;

namespace PixTrain.Core.Tests
{
    public class RewardServiceTests
    {
        private static RunState StalledRun(TestGame t)
        {
            var run = t.CleanRun();
            run.Fuel = 0.5f;
            run.Y = 100f;
            run.Speed = 50f;
            for (int i = 0; i < 400 && !run.IsStalled; i++) t.G.Runs.Tick(run, 0.05f);
            Assert.That(run.IsStalled, Is.True);
            return run;
        }

        [Test]
        public void ContinueRun_RefuelsHalfTank_OncePerRun()
        {
            var t = new TestGame();
            var run = StalledRun(t);
            var rewards = t.G.Rewards;

            Assert.That(rewards.CanOffer(RewardType.ContinueRun, run), Is.True);
            Assert.That(rewards.Amount(RewardType.ContinueRun, run), Is.EqualTo(50));
            Assert.That(rewards.Grant(RewardType.ContinueRun, run), Is.EqualTo(50));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Running));
            Assert.That(run.Fuel, Is.EqualTo(50f));
            Assert.That(run.Events.Any(e => e.Type == RunEventType.Continued), Is.True);

            // İkinci kez yakıt biterse teklif yok, sefer biter.
            run.Fuel = 0.5f;
            run.Speed = 50f;
            for (int i = 0; i < 400 && !run.IsFinished; i++) t.G.Runs.Tick(run, 0.05f);
            Assert.That(run.IsFinished, Is.True);
            Assert.That(rewards.CanOffer(RewardType.ContinueRun, run), Is.False);
        }

        [Test]
        public void ContinueRun_NotOfferedWhileRunning()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            Assert.That(t.G.Rewards.CanOffer(RewardType.ContinueRun, run), Is.False);
            Assert.That(t.G.Rewards.Grant(RewardType.ContinueRun, run), Is.EqualTo(0));
        }

        [Test]
        public void DoubleEarnings_AddsSameAmountAgain_Once()
        {
            var t = new TestGame();
            var run = t.CleanRun();
            t.AddGate(run, GateKind.Coin, 1, 200f);
            t.DriveThrough(run, 200f);
            t.G.Runs.Abandon(run);
            run.Y = run.Length - 0.6f;
            run.Speed = 20f;
            t.G.Runs.Tick(run, 0.1f);
            Assert.That(run.IsFinished, Is.True);

            int coins = t.G.Economy.Coins;
            int earned = run.Result.Earned;
            Assert.That(earned, Is.GreaterThan(0));
            Assert.That(t.G.Rewards.CanOffer(RewardType.DoubleEarnings, run), Is.True);
            Assert.That(t.G.Rewards.Grant(RewardType.DoubleEarnings, run), Is.EqualTo(earned));
            Assert.That(t.G.Economy.Coins, Is.EqualTo(coins + earned));
            Assert.That(run.Result.TotalEarned, Is.EqualTo(earned * 2));
            Assert.That(t.G.Rewards.CanOffer(RewardType.DoubleEarnings, run), Is.False);
            Assert.That(t.G.Rewards.Grant(RewardType.DoubleEarnings, run), Is.EqualTo(0));
        }

        [Test]
        public void DoubleIdle_CollectsTwice_AndResetsTimer()
        {
            var t = new TestGame();
            t.Progress.CompletedRoutes[0] = true; // 2/dk
            t.Time.UtcNow = t.Time.UtcNow.AddMinutes(30);
            int coins = t.G.Economy.Coins;

            Assert.That(t.G.Rewards.Amount(RewardType.DoubleIdle), Is.EqualTo(120));
            Assert.That(t.G.Rewards.Grant(RewardType.DoubleIdle), Is.EqualTo(120));
            Assert.That(t.G.Economy.Coins, Is.EqualTo(coins + 120));
            Assert.That(t.G.Rewards.CanOffer(RewardType.DoubleIdle), Is.False, "toplandıktan sonra biriken yok");
        }

        [Test]
        public void DailyCoins_LimitedPerDay_ResetsNextDay()
        {
            var t = new TestGame();
            t.Progress.UnlockedRoute = 2;
            var rewards = t.G.Rewards;
            int coins = t.G.Economy.Coins;

            Assert.That(rewards.Amount(RewardType.DailyCoins), Is.EqualTo(80 + 2 * 40));
            for (int i = 0; i < 3; i++) Assert.That(rewards.Grant(RewardType.DailyCoins), Is.EqualTo(160));
            Assert.That(t.G.Economy.Coins, Is.EqualTo(coins + 480));
            Assert.That(rewards.DailyRemaining(), Is.EqualTo(0));
            Assert.That(rewards.CanOffer(RewardType.DailyCoins), Is.False);
            Assert.That(rewards.Grant(RewardType.DailyCoins), Is.EqualTo(0));

            t.Time.UtcNow = t.Time.UtcNow.AddDays(1);
            Assert.That(rewards.DailyRemaining(), Is.EqualTo(3));
            Assert.That(rewards.Grant(RewardType.DailyCoins), Is.EqualTo(160));
            Assert.That(rewards.DailyRemaining(), Is.EqualTo(2));
        }

        [Test]
        public void AttractRun_NeverStalls()
        {
            var t = new TestGame();
            var run = t.G.Runs.StartRun(0, true, 5);
            for (int i = 0; i < 600; i++) t.G.Runs.Tick(run, 0.05f);
            Assert.That(run.IsStalled, Is.False);
            Assert.That(t.G.Rewards.CanOffer(RewardType.ContinueRun, run), Is.False);
        }
    }
}
