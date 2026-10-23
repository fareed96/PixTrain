using System;
using PixTrain.Core.Data;

namespace PixTrain.Core.Models
{
    /// <summary>
    /// Kalıcı oyuncu ilerlemesi. Alanlar public tutulur; Unity JsonUtility ve sunucu tarafı serileştirme doğrudan kullanabilir.
    /// </summary>
    [Serializable]
    public sealed class PlayerProgress
    {
        public int Coins;
        public int[] BestStars;
        public bool[] CompletedRoutes;
        public int UnlockedRoute;
        public int[] UpgradeLevels;
        public bool[] OwnedLocomotives;
        public int SelectedLocomotive;
        public long IdleCollectedUtcTicks;
        public bool SeenTutorial;
        public bool Muted;
        /// <summary>Günlük ödül sayacı: gün numarası (UTC, 1970'ten beri) ve o gün alınan adet.</summary>
        public int DailyRewardDay;
        public int DailyRewardsClaimed;

        public static PlayerProgress CreateDefault(GameCatalog catalog, DateTime utcNow)
        {
            var p = new PlayerProgress
            {
                Coins = catalog.Balance.StartCoins,
                IdleCollectedUtcTicks = utcNow.Ticks,
            };
            p.Normalize(catalog);
            p.OwnedLocomotives[0] = true;
            return p;
        }

        /// <summary>Eski bir kayıt yeni içerikle (daha fazla hat, lokomotif) yüklendiğinde dizi boyutlarını düzeltir.</summary>
        public void Normalize(GameCatalog catalog)
        {
            BestStars = Resize(BestStars, catalog.Routes.Count);
            CompletedRoutes = Resize(CompletedRoutes, catalog.Routes.Count);
            UpgradeLevels = Resize(UpgradeLevels, catalog.Upgrades.Count);
            OwnedLocomotives = Resize(OwnedLocomotives, catalog.Locomotives.Count);
            if (OwnedLocomotives.Length > 0) OwnedLocomotives[0] = true;
            if (SelectedLocomotive < 0 || SelectedLocomotive >= OwnedLocomotives.Length || !OwnedLocomotives[SelectedLocomotive]) SelectedLocomotive = 0;
            if (UnlockedRoute < 0) UnlockedRoute = 0;
            if (UnlockedRoute > catalog.Routes.Count - 1) UnlockedRoute = catalog.Routes.Count - 1;
        }

        public int UpgradeLevel(UpgradeId id) => UpgradeLevels[(int)id];

        private static T[] Resize<T>(T[] source, int length)
        {
            var result = new T[length];
            if (source != null) Array.Copy(source, result, Math.Min(source.Length, length));
            return result;
        }
    }
}
