namespace PixTrain.Core.Data
{
    public enum CargoType { Passenger = 0, Coal = 1, Food = 2 }

    public enum WagonType { Passenger, Freight }

    public enum GateKind { Town, Mine, Farm, City, Factory, Fuel, Coin, BrokenRail, Rock, Cow, Terminal }

    public enum GateCategory { Pickup, Delivery, Fuel, Coin, Hazard, Terminal }

    public enum ZoneType { Station, Countryside, Town, City, River, Tunnel, Terminal }

    public enum BiomeId { Plains, Forest, River, Mountain, Coast, Snow }

    public enum UpgradeId { Engine = 0, FuelTank = 1, Capacity = 2, Wagon = 3, Headlight = 4 }

    public enum ContractMetric { Passengers, Coal, Food, Coins, NightCoins, NoCrash }

    public enum CropType { Wheat, Green, Plowed }

    public enum DayPhase { Day, Dawn, Dusk, Night }

    public enum LocomotiveStatus { Selected, Owned, NeedsStars, NeedsCoins, Available }
}
