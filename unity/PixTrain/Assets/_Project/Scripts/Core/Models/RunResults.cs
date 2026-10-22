using PixTrain.Core.Data;

namespace PixTrain.Core.Models
{
    public enum RunPhase { Running, OutOfFuel, Finished }

    /// <summary>Sefer sırasında olan şeyler. Görsel katman bunları ses, yazı ve efekte çevirir.</summary>
    public enum RunEventType
    {
        SwitchChanged, SwitchBlocked, JunctionPassed,
        Coin, Pickup, WagonsFull, Delivery, NothingToDeliver, Refuel,
        Crash, OncomingCrash, Horn, Turbo, StreakUp,
        FuelLow, OutOfFuel, NearDestination, NightFell, OncomingWarning, Finished,
    }

    public struct RunEvent
    {
        public RunEventType Type;
        /// <summary>Olayın dünya x konumu (yazının çıkacağı yer).</summary>
        public float X;
        /// <summary>Para, adet ya da yakıt miktarı.</summary>
        public int Value;
        public int Count;
        public CargoType Cargo;

        public RunEvent(RunEventType type, float x, int value = 0, int count = 0, CargoType cargo = CargoType.Passenger)
        {
            Type = type; X = x; Value = value; Count = count; Cargo = cargo;
        }
    }

    public struct DeliveryQuote
    {
        public int Pay;
        public int Count;

        public DeliveryQuote(int pay, int count) { Pay = pay; Count = count; }
    }

    public struct CargoSummary
    {
        public int Passengers;
        public int Coal;
        public int Food;
        public int PassengerCapacity;
        public int FreightCapacity;
    }

    public enum PreviewTone { Passenger, Coal, Food, Money, Fuel, Warning, Neutral }

    /// <summary>Peronun üstünde gösterilen tahmini değer ("+3", "DOLU", "+54").</summary>
    public struct GatePreview
    {
        public string Text;
        public PreviewTone Tone;

        public GatePreview(string text, PreviewTone tone) { Text = text; Tone = tone; }
    }

    public sealed class TrainSpec
    {
        public int LocomotiveIndex;
        public float CruiseSpeed;
        public float FuelEfficiency;
        public float FuelMax;
        public int Capacity;
        public WagonType[] Wagons;
        public int HeadlightLevel;
    }

    public sealed class ContractResult
    {
        public string Title;
        public bool Completed;
    }

    public sealed class RunResult
    {
        public bool Arrived;
        public int Stars;
        public int Earned;
        public int FinalPay;
        public int Crashes;
        public ContractResult[] Contracts;
        public int CityIndex;
        public int CityLevelBefore;
        public int CityLevelAfter;
        public int PreviousBestStars;
        public int IdleRateAfter;

        public int RouteEarnings => Earned - FinalPay;
        public bool CityGrew => CityLevelAfter > CityLevelBefore;
    }
}
