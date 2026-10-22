using System.Collections.Generic;

namespace PixTrain.Core.Data
{
    /// <summary>Oyunun sabit içeriği: şehirler, hatlar, biyomlar, peronlar, geliştirmeler, lokomotifler, sözleşmeler.</summary>
    public sealed class GameCatalog
    {
        public GameBalance Balance = new GameBalance();
        public List<CityDef> Cities = new List<CityDef>();
        public List<RouteDef> Routes = new List<RouteDef>();
        public Dictionary<BiomeId, BiomeDef> Biomes = new Dictionary<BiomeId, BiomeDef>();
        public Dictionary<GateKind, StationDef> Stations = new Dictionary<GateKind, StationDef>();
        public List<UpgradeDef> Upgrades = new List<UpgradeDef>();
        public List<LocomotiveDef> Locomotives = new List<LocomotiveDef>();
        public List<ContractDef> Contracts = new List<ContractDef>();
        /// <summary>Vagon geliştirmesiyle eklenen vagonların sırası (ilk ikisi başlangıçta vardır).</summary>
        public WagonType[] WagonOrder = { WagonType.Passenger, WagonType.Freight, WagonType.Freight, WagonType.Passenger, WagonType.Freight };
        public string[] CityLevelNames = { "", "Köy", "Kasaba", "İlçe", "Şehir", "Metropol" };
        public int MaxCityLevel = 5;

        public BiomeDef Biome(BiomeId id) => Biomes[id];

        public StationDef Station(GateKind kind) => Stations[kind];

        public UpgradeDef Upgrade(UpgradeId id) => Upgrades[(int)id];

        public ContractDef Contract(ContractMetric metric) => Contracts.Find(c => c.Metric == metric);
    }
}
