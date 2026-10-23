using System;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Repositories;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    /// <summary>Ödüllü reklamla alınabilen ödüller. Hepsi oyuncunun seçimiyle sunulur, hiçbiri zorunlu değildir.</summary>
    public enum RewardType
    {
        /// <summary>Yakıt bitip tren durunca: yakıt deposunun yarısıyla sefere devam (sefer başına bir kez).</summary>
        ContinueRun,
        /// <summary>Sefer sonunda: kazancı ikiye katla (sefer başına bir kez).</summary>
        DoubleEarnings,
        /// <summary>Hat geliri: biriken altını iki kat topla.</summary>
        DoubleIdle,
        /// <summary>Garaj: günlük altın paketi (günde sınırlı).</summary>
        DailyCoins,
    }

    public interface IRewardService
    {
        /// <summary>Bu ödül şu an sunulabilir mi (reklamın hazır olup olmadığı sunum katmanının işidir).</summary>
        bool CanOffer(RewardType type, RunState run = null);
        /// <summary>Ödülün büyüklüğü: yakıt ya da altın miktarı.</summary>
        int Amount(RewardType type, RunState run = null);
        /// <summary>Reklam sonuna kadar izlendikten sonra ödülü verir; verilen miktarı döner (0 = verilemedi).</summary>
        int Grant(RewardType type, RunState run = null);
        /// <summary>Bugün kalan günlük altın paketi.</summary>
        int DailyRemaining();
    }

    public sealed class RewardService : IRewardService
    {
        private readonly GameCatalog _catalog;
        private readonly IProgressRepository _progress;
        private readonly IEconomyService _economy;
        private readonly IRunService _runs;
        private readonly ITimeProvider _time;

        public RewardService(GameCatalog catalog, IProgressRepository progress, IEconomyService economy, IRunService runs, ITimeProvider time)
        {
            _catalog = catalog;
            _progress = progress;
            _economy = economy;
            _runs = runs;
            _time = time;
        }

        private GameBalance B => _catalog.Balance;

        private int Today => (int)(_time.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;

        public int DailyRemaining()
        {
            var p = _progress.Get();
            int claimed = p.DailyRewardDay == Today ? p.DailyRewardsClaimed : 0;
            return Math.Max(0, B.DailyRewardLimit - claimed);
        }

        public bool CanOffer(RewardType type, RunState run = null)
        {
            switch (type)
            {
                case RewardType.ContinueRun:
                    return run != null && !run.IsAttract && run.IsStalled && !run.ContinueUsed;
                case RewardType.DoubleEarnings:
                    return run != null && run.Result != null && !run.Result.EarningsDoubled && run.Result.Earned > 0;
                case RewardType.DoubleIdle:
                    return _economy.IdlePending() > 0;
                case RewardType.DailyCoins:
                    return DailyRemaining() > 0;
                default:
                    return false;
            }
        }

        public int Amount(RewardType type, RunState run = null)
        {
            switch (type)
            {
                case RewardType.ContinueRun:
                    return run == null ? 0 : MathUtil.RoundHalfUp(run.FuelMax * B.ContinueFuelFraction);
                case RewardType.DoubleEarnings:
                    return run?.Result?.Earned ?? 0;
                case RewardType.DoubleIdle:
                    return _economy.IdlePending() * 2;
                case RewardType.DailyCoins:
                    return B.DailyCoinsBase + B.DailyCoinsPerRoute * _progress.Get().UnlockedRoute;
                default:
                    return 0;
            }
        }

        public int Grant(RewardType type, RunState run = null)
        {
            if (!CanOffer(type, run)) return 0;
            int amount = Amount(type, run);
            var p = _progress.Get();
            switch (type)
            {
                case RewardType.ContinueRun:
                    return _runs.ContinueRun(run, amount) ? amount : 0;
                case RewardType.DoubleEarnings:
                    _economy.AddCoins(amount);
                    run.Result.EarningsDoubled = true;
                    run.Result.BonusEarned = amount;
                    break;
                case RewardType.DoubleIdle:
                    int collected = _economy.CollectIdle();
                    _economy.AddCoins(collected);
                    amount = collected * 2;
                    break;
                case RewardType.DailyCoins:
                    if (p.DailyRewardDay != Today) { p.DailyRewardDay = Today; p.DailyRewardsClaimed = 0; }
                    p.DailyRewardsClaimed++;
                    _economy.AddCoins(amount);
                    break;
            }
            _progress.Save();
            return amount;
        }
    }
}
