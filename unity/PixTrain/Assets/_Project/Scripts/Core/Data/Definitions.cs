using System;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Data
{
    public sealed class CityDef
    {
        public string Name;
        public int BaseLevel;
    }

    public sealed class RouteDef
    {
        public BiomeId Biome;
        public float Length;
        public float StartTimeOfDay;
        public string Description;

        /// <summary>Oyuncuya gösterilen mesafe (1 km = 100 dünya pikseli).</summary>
        public int Kilometers => (int)Math.Round(Length / 100f);
    }

    public sealed class BiomeDef
    {
        public BiomeId Id;
        public string Name;
        public float RiverChance;
        public float TunnelChance;
        public float RainChance;
        public bool HasFields;
        public bool HasSea;
        public bool HasSnow;
        public WeightedTable<GateKind> Pickups = new WeightedTable<GateKind>();
        public WeightedTable<GateKind> Deliveries = new WeightedTable<GateKind>();
        public WeightedTable<GateKind> Hazards = new WeightedTable<GateKind>();
    }

    public sealed class StationDef
    {
        public GateKind Kind;
        public GateCategory Category;
        public string Name;
        /// <summary>Yük alma peronunun verdiği yük.</summary>
        public CargoType Cargo;
        /// <summary>Teslimat peronunun satın aldığı yükler.</summary>
        public CargoType[] Accepts = new CargoType[0];
    }

    public sealed class UpgradeDef
    {
        public UpgradeId Id;
        public string Name;
        public string Description;
        public int[] Costs;

        public int MaxLevel => Costs.Length;
    }

    public sealed class LocomotiveDef
    {
        public string Name;
        public string TypeName;
        public string Description;
        public string SpriteKey;
        public int Cost;
        public int StarsRequired;
        public float SpeedMultiplier;
        public float FuelEfficiency;
        public float IdleMultiplier;
    }

    public sealed class ContractDef
    {
        public ContractMetric Metric;
        /// <summary>Hedef sayıdan oyuncuya gösterilen başlık ("14 yolcu taşı").</summary>
        public Func<int, string> Title;
        /// <summary>Hat sırasından hedef sayı.</summary>
        public Func<int, int> TargetForRoute;
        /// <summary>Yalnızca varışta değerlendirilir (ör. kazasız varış).</summary>
        public bool EndOnly;
    }
}
