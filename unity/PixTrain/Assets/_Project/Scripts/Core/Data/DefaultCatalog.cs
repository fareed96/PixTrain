namespace PixTrain.Core.Data
{
    /// <summary>Prototipteki içeriğin aynısı. İleride ScriptableObject ya da sunucu ayarından doldurulabilir.</summary>
    public static class DefaultCatalog
    {
        public static GameCatalog Create()
        {
            var c = new GameCatalog();

            c.Cities.Add(new CityDef { Name = "Gölbaşı", BaseLevel = 2 });
            c.Cities.Add(new CityDef { Name = "Çamlıca", BaseLevel = 1 });
            c.Cities.Add(new CityDef { Name = "Taşköprü", BaseLevel = 1 });
            c.Cities.Add(new CityDef { Name = "Kayalar", BaseLevel = 1 });
            c.Cities.Add(new CityDef { Name = "Mavikent", BaseLevel = 2 });
            c.Cities.Add(new CityDef { Name = "Karlıdağ", BaseLevel = 1 });
            c.Cities.Add(new CityDef { Name = "Başkent", BaseLevel = 3 });

            c.Routes.Add(new RouteDef { Biome = BiomeId.Plains, Length = 4200, StartTimeOfDay = 0.36f, Description = "Buğday tarlaları, çiftlikler, ilk yolcuların." });
            c.Routes.Add(new RouteDef { Biome = BiomeId.Forest, Length = 5000, StartTimeOfDay = 0.55f, Description = "Çam ormanları ve kereste köyleri. Geceye girer." });
            c.Routes.Add(new RouteDef { Biome = BiomeId.River, Length = 5600, StartTimeOfDay = 0.70f, Description = "Köprüler, nehir kasabaları, uzun bir gece." });
            c.Routes.Add(new RouteDef { Biome = BiomeId.Mountain, Length = 6200, StartTimeOfDay = 0.30f, Description = "Madenler ve kayalar. Kömür burada kazandırır." });
            c.Routes.Add(new RouteDef { Biome = BiomeId.Coast, Length = 6800, StartTimeOfDay = 0.62f, Description = "Deniz kıyısı, deniz feneri, liman fabrikaları." });
            c.Routes.Add(new RouteDef { Biome = BiomeId.Snow, Length = 7400, StartTimeOfDay = 0.80f, Description = "Karlı geçit. Gece uzun, far şart." });

            AddBiome(c, BiomeId.Plains, "Ova Hattı", river: 0.12f, tunnel: 0f, rain: 0.3f, fields: true, sea: false, snow: false,
                farm: 5, town: 4, mine: 2, city: 6, factory: 3, cow: 5, broken: 4, rock: 1);
            AddBiome(c, BiomeId.Forest, "Orman Hattı", river: 0.12f, tunnel: 0.1f, rain: 0.35f, fields: false, sea: false, snow: false,
                farm: 2, town: 4, mine: 3, city: 5, factory: 4, cow: 2, broken: 4, rock: 3);
            AddBiome(c, BiomeId.River, "Nehir Hattı", river: 0.42f, tunnel: 0f, rain: 0.45f, fields: true, sea: false, snow: false,
                farm: 4, town: 5, mine: 2, city: 6, factory: 3, cow: 4, broken: 4, rock: 1);
            AddBiome(c, BiomeId.Mountain, "Dağ Hattı", river: 0.08f, tunnel: 0.28f, rain: 0f, fields: false, sea: false, snow: false,
                farm: 1, town: 3, mine: 6, city: 4, factory: 5, cow: 1, broken: 4, rock: 6);
            AddBiome(c, BiomeId.Coast, "Sahil Hattı", river: 0.06f, tunnel: 0f, rain: 0.4f, fields: false, sea: true, snow: false,
                farm: 3, town: 5, mine: 2, city: 6, factory: 3, cow: 2, broken: 4, rock: 3);
            AddBiome(c, BiomeId.Snow, "Kar Geçidi", river: 0.05f, tunnel: 0.22f, rain: 0f, fields: false, sea: false, snow: true,
                farm: 2, town: 4, mine: 4, city: 5, factory: 4, cow: 1, broken: 5, rock: 4);

            AddStation(c, GateKind.Town, GateCategory.Pickup, "Kasaba", CargoType.Passenger);
            AddStation(c, GateKind.Mine, GateCategory.Pickup, "Maden", CargoType.Coal);
            AddStation(c, GateKind.Farm, GateCategory.Pickup, "Çiftlik", CargoType.Food);
            AddStation(c, GateKind.City, GateCategory.Delivery, "Şehir", CargoType.Passenger, CargoType.Passenger, CargoType.Food);
            AddStation(c, GateKind.Factory, GateCategory.Delivery, "Fabrika", CargoType.Coal, CargoType.Coal);
            AddStation(c, GateKind.Fuel, GateCategory.Fuel, "Yakıt", CargoType.Passenger);
            AddStation(c, GateKind.Coin, GateCategory.Coin, "Altın", CargoType.Passenger);
            AddStation(c, GateKind.BrokenRail, GateCategory.Hazard, "Kırık ray", CargoType.Passenger);
            AddStation(c, GateKind.Rock, GateCategory.Hazard, "Kaya", CargoType.Passenger);
            AddStation(c, GateKind.Cow, GateCategory.Hazard, "İnek", CargoType.Passenger);
            AddStation(c, GateKind.Terminal, GateCategory.Terminal, "Gar", CargoType.Passenger);

            c.Upgrades.Add(new UpgradeDef { Id = UpgradeId.Engine, Name = "Motor Gücü", Description = "Tren %8 hızlanır, varış daha erken.", Costs = new[] { 150, 300, 550, 900, 1400 } });
            c.Upgrades.Add(new UpgradeDef { Id = UpgradeId.FuelTank, Name = "Yakıt Deposu", Description = "+25 yakıt. Daha az durak, daha cesur rota.", Costs = new[] { 120, 250, 450, 750, 1200 } });
            c.Upgrades.Add(new UpgradeDef { Id = UpgradeId.Capacity, Name = "Geniş Vagon", Description = "Her vagon +1 yük alır. Büyük teslimat = kombo.", Costs = new[] { 200, 400, 750, 1200 } });
            c.Upgrades.Add(new UpgradeDef { Id = UpgradeId.Wagon, Name = "Yeni Vagon", Description = "Trene bir vagon daha (yük, yolcu, yük).", Costs = new[] { 350, 800, 1600 } });
            c.Upgrades.Add(new UpgradeDef { Id = UpgradeId.Headlight, Name = "Güçlü Far", Description = "Gece daha uzağı görürsün.", Costs = new[] { 180, 420, 800 } });

            c.Locomotives.Add(new LocomotiveDef { Name = "Kara Duman", TypeName = "Buharlı", SpriteKey = "steam", Cost = 0, StarsRequired = 0,
                SpeedMultiplier = 1f, FuelEfficiency = 1f, IdleMultiplier = 1f, Description = "Ağır ve sadık. Kömür yakar, duman bırakır." });
            c.Locomotives.Add(new LocomotiveDef { Name = "Turuncu Kaplan", TypeName = "Dizel", SpriteKey = "diesel", Cost = 1500, StarsRequired = 6,
                SpeedMultiplier = 1.15f, FuelEfficiency = 0.8f, IdleMultiplier = 1.5f, Description = "%15 hızlı, %20 az yakıt, hat geliri ×1.5." });
            c.Locomotives.Add(new LocomotiveDef { Name = "Mavi Şimşek", TypeName = "Elektrikli", SpriteKey = "electric", Cost = 4000, StarsRequired = 12,
                SpeedMultiplier = 1.3f, FuelEfficiency = 0.6f, IdleMultiplier = 2f, Description = "%30 hızlı, %40 az yakıt, hat geliri ×2." });

            c.Contracts.Add(new ContractDef { Metric = ContractMetric.Passengers, Title = n => n + " yolcu taşı", TargetForRoute = r => 8 + r * 3 });
            c.Contracts.Add(new ContractDef { Metric = ContractMetric.Coal, Title = n => n + " kömür taşı", TargetForRoute = r => 6 + r * 3 });
            c.Contracts.Add(new ContractDef { Metric = ContractMetric.Food, Title = n => n + " gıda taşı", TargetForRoute = r => 6 + r * 3 });
            c.Contracts.Add(new ContractDef { Metric = ContractMetric.Coins, Title = n => n + " altın kazan", TargetForRoute = r => 160 + r * 90 });
            c.Contracts.Add(new ContractDef { Metric = ContractMetric.NightCoins, Title = n => "Gece " + n + " altın", TargetForRoute = r => 50 + r * 30 });
            c.Contracts.Add(new ContractDef { Metric = ContractMetric.NoCrash, Title = n => "Kazasız varış", TargetForRoute = r => 1, EndOnly = true });

            return c;
        }

        private static void AddBiome(GameCatalog c, BiomeId id, string name, float river, float tunnel, float rain, bool fields, bool sea, bool snow,
            float farm, float town, float mine, float city, float factory, float cow, float broken, float rock)
        {
            var b = new BiomeDef
            {
                Id = id, Name = name, RiverChance = river, TunnelChance = tunnel, RainChance = rain,
                HasFields = fields, HasSea = sea, HasSnow = snow,
            };
            b.Pickups.Add(GateKind.Farm, farm).Add(GateKind.Town, town).Add(GateKind.Mine, mine);
            b.Deliveries.Add(GateKind.City, city).Add(GateKind.Factory, factory);
            b.Hazards.Add(GateKind.Cow, cow).Add(GateKind.BrokenRail, broken).Add(GateKind.Rock, rock);
            c.Biomes[id] = b;
        }

        private static void AddStation(GameCatalog c, GateKind kind, GateCategory category, string name, CargoType cargo, params CargoType[] accepts)
        {
            c.Stations[kind] = new StationDef { Kind = kind, Category = category, Name = name, Cargo = cargo, Accepts = accepts };
        }
    }
}
