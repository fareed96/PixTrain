using System;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Repositories;

namespace PixTrain.Core.Services
{
    public interface IGarageService
    {
        int UpgradeLevel(UpgradeId id);
        bool IsMaxed(UpgradeId id);
        /// <summary>Sıradaki seviyenin fiyatı; en üst seviyedeyse -1.</summary>
        int NextUpgradeCost(UpgradeId id);
        bool CanBuyUpgrade(UpgradeId id);
        bool BuyUpgrade(UpgradeId id);
        LocomotiveStatus GetLocomotiveStatus(int index);
        bool BuyLocomotive(int index);
        bool SelectLocomotive(int index);
        /// <summary>Geliştirmelerden ve seçili lokomotiften seferde kullanılacak tren özelliklerini hesaplar.</summary>
        TrainSpec BuildTrainSpec();
    }

    public sealed class GarageService : IGarageService
    {
        private readonly GameCatalog _catalog;
        private readonly IProgressRepository _progress;
        private readonly IEconomyService _economy;

        public GarageService(GameCatalog catalog, IProgressRepository progress, IEconomyService economy)
        {
            _catalog = catalog;
            _progress = progress;
            _economy = economy;
        }

        public int UpgradeLevel(UpgradeId id) => _progress.Get().UpgradeLevel(id);

        public bool IsMaxed(UpgradeId id) => UpgradeLevel(id) >= _catalog.Upgrade(id).MaxLevel;

        public int NextUpgradeCost(UpgradeId id) => IsMaxed(id) ? -1 : _catalog.Upgrade(id).Costs[UpgradeLevel(id)];

        public bool CanBuyUpgrade(UpgradeId id) => !IsMaxed(id) && _economy.Coins >= NextUpgradeCost(id);

        public bool BuyUpgrade(UpgradeId id)
        {
            if (!CanBuyUpgrade(id) || !_economy.TrySpend(NextUpgradeCost(id))) return false;
            _progress.Get().UpgradeLevels[(int)id]++;
            _progress.Save();
            return true;
        }

        public LocomotiveStatus GetLocomotiveStatus(int index)
        {
            var p = _progress.Get();
            if (p.SelectedLocomotive == index) return LocomotiveStatus.Selected;
            if (p.OwnedLocomotives[index]) return LocomotiveStatus.Owned;
            var def = _catalog.Locomotives[index];
            if (_economy.TotalStars() < def.StarsRequired) return LocomotiveStatus.NeedsStars;
            if (_economy.Coins < def.Cost) return LocomotiveStatus.NeedsCoins;
            return LocomotiveStatus.Available;
        }

        public bool BuyLocomotive(int index)
        {
            if (GetLocomotiveStatus(index) != LocomotiveStatus.Available) return false;
            if (!_economy.TrySpend(_catalog.Locomotives[index].Cost)) return false;
            var p = _progress.Get();
            p.OwnedLocomotives[index] = true;
            p.SelectedLocomotive = index;
            _progress.Save();
            return true;
        }

        public bool SelectLocomotive(int index)
        {
            var p = _progress.Get();
            if (index < 0 || index >= p.OwnedLocomotives.Length || !p.OwnedLocomotives[index]) return false;
            p.SelectedLocomotive = index;
            _progress.Save();
            return true;
        }

        public TrainSpec BuildTrainSpec()
        {
            var p = _progress.Get();
            var b = _catalog.Balance;
            var loco = _catalog.Locomotives[p.SelectedLocomotive];
            int wagonCount = Math.Min(_catalog.WagonOrder.Length, 2 + p.UpgradeLevel(UpgradeId.Wagon));
            var wagons = new WagonType[wagonCount];
            Array.Copy(_catalog.WagonOrder, wagons, wagonCount);
            return new TrainSpec
            {
                LocomotiveIndex = p.SelectedLocomotive,
                CruiseSpeed = b.BaseSpeed * loco.SpeedMultiplier * (1f + b.SpeedPerEngineLevel * p.UpgradeLevel(UpgradeId.Engine)),
                FuelEfficiency = loco.FuelEfficiency,
                FuelMax = b.BaseFuel + b.FuelPerTankLevel * p.UpgradeLevel(UpgradeId.FuelTank),
                Capacity = b.BaseCapacity + p.UpgradeLevel(UpgradeId.Capacity),
                Wagons = wagons,
                HeadlightLevel = p.UpgradeLevel(UpgradeId.Headlight),
            };
        }
    }
}
