using System;
using PixTrain.Core.Data;
using PixTrain.Core.Repositories;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    public interface IEconomyService
    {
        int Coins { get; }
        float RouteMultiplier(int routeIndex);
        float StreakMultiplier(int streak);
        int CityLevel(int cityIndex);
        string CityLevelName(int cityIndex);
        int TotalStars();
        int MaxStars();
        int IdleRatePerMinute();
        int IdlePending();
        /// <summary>Biriken hat gelirini kasaya ekler, eklenen miktarı döner.</summary>
        int CollectIdle();
        void AddCoins(int amount);
        bool TrySpend(int amount);
    }

    public sealed class EconomyService : IEconomyService
    {
        private readonly GameCatalog _catalog;
        private readonly IProgressRepository _progress;
        private readonly ITimeProvider _time;

        public EconomyService(GameCatalog catalog, IProgressRepository progress, ITimeProvider time)
        {
            _catalog = catalog;
            _progress = progress;
            _time = time;
        }

        private GameBalance B => _catalog.Balance;

        public int Coins => _progress.Get().Coins;

        public float RouteMultiplier(int routeIndex) => 1f + routeIndex * B.RouteMultiplierStep;

        public float StreakMultiplier(int streak) => 1f + Math.Min(B.StreakMaxSteps, Math.Max(0, streak)) * B.StreakStep;

        public int CityLevel(int cityIndex)
        {
            var p = _progress.Get();
            int level = _catalog.Cities[cityIndex].BaseLevel;
            if (cityIndex > 0) level += p.BestStars[cityIndex - 1];
            if (cityIndex < _catalog.Routes.Count && p.CompletedRoutes[cityIndex]) level += 1;
            return Math.Min(_catalog.MaxCityLevel, level);
        }

        public string CityLevelName(int cityIndex) => _catalog.CityLevelNames[CityLevel(cityIndex)];

        public int TotalStars()
        {
            int sum = 0;
            foreach (int s in _progress.Get().BestStars) sum += s;
            return sum;
        }

        public int MaxStars() => _catalog.Routes.Count * 3;

        public int IdleRatePerMinute()
        {
            var p = _progress.Get();
            float rate = 0f;
            for (int i = 0; i < _catalog.Routes.Count; i++)
                if (p.CompletedRoutes[i]) rate += 2 + 2 * p.BestStars[i];
            return MathUtil.RoundHalfUp(rate * _catalog.Locomotives[p.SelectedLocomotive].IdleMultiplier);
        }

        public int IdlePending()
        {
            var p = _progress.Get();
            double minutes = (_time.UtcNow - new DateTime(p.IdleCollectedUtcTicks, DateTimeKind.Utc)).TotalMinutes;
            minutes = Math.Max(0, Math.Min(B.IdleCapMinutes, minutes));
            return (int)Math.Floor(IdleRatePerMinute() * minutes);
        }

        public int CollectIdle()
        {
            int pending = IdlePending();
            if (pending <= 0) return 0;
            var p = _progress.Get();
            p.Coins += pending;
            p.IdleCollectedUtcTicks = _time.UtcNow.Ticks;
            _progress.Save();
            return pending;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            _progress.Get().Coins += amount;
        }

        public bool TrySpend(int amount)
        {
            var p = _progress.Get();
            if (amount < 0 || p.Coins < amount) return false;
            p.Coins -= amount;
            return true;
        }
    }
}
