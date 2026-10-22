using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    public interface ICargoService
    {
        int FreeCapacity(RunState run, CargoType cargo);
        /// <summary>Uygun vagonlara en fazla <paramref name="amount"/> yük koyar; yüklenen adedi döner.</summary>
        int Load(RunState run, CargoType cargo, int amount);
        /// <summary>Teslimatın kazancını yük indirmeden hesaplar (peron üstündeki tahmin).</summary>
        DeliveryQuote Quote(RunState run, IReadOnlyList<CargoType> accepts, float multiplier);
        /// <summary>Kabul edilen yükleri indirir; kazancı döner (paranın eklenmesi RunService'tedir).</summary>
        DeliveryQuote Deliver(RunState run, IReadOnlyList<CargoType> accepts, float multiplier);
        /// <summary>Kazada rastgele vagonlardan yük düşürür; düşen adedi döner.</summary>
        int LoseRandom(RunState run, int count);
        CargoSummary Summarize(RunState run);
    }

    public sealed class CargoService : ICargoService
    {
        private static readonly CargoType[] AllCargo = { CargoType.Passenger, CargoType.Coal, CargoType.Food };

        private readonly GameCatalog _catalog;
        private readonly IEconomyService _economy;

        public CargoService(GameCatalog catalog, IEconomyService economy)
        {
            _catalog = catalog;
            _economy = economy;
        }

        public static IReadOnlyList<CargoType> All => AllCargo;

        public static WagonType WagonFor(CargoType cargo) => cargo == CargoType.Passenger ? WagonType.Passenger : WagonType.Freight;

        public int FreeCapacity(RunState run, CargoType cargo)
        {
            var type = WagonFor(cargo);
            int free = 0;
            foreach (var w in run.Wagons) if (w.Type == type) free += w.Free;
            return free;
        }

        public int Load(RunState run, CargoType cargo, int amount)
        {
            var type = WagonFor(cargo);
            int loaded = 0;
            foreach (var w in run.Wagons)
            {
                if (w.Type != type) continue;
                bool changed = false;
                while (!w.IsFull && loaded < amount) { w.Items.Add(cargo); loaded++; changed = true; }
                if (changed) w.Version++;
            }
            return loaded;
        }

        public DeliveryQuote Quote(RunState run, IReadOnlyList<CargoType> accepts, float multiplier)
        {
            var b = _catalog.Balance;
            float routeMult = _economy.RouteMultiplier(run.RouteIndex);
            bool night = DayCycle.IsNight(run.TimeOfDay);
            float pay = 0f;
            int count = 0;
            foreach (var w in run.Wagons)
            {
                foreach (var item in w.Items)
                {
                    if (!Contains(accepts, item)) continue;
                    float p = b.CargoPrices[(int)item] * routeMult;
                    if (item == CargoType.Passenger && night) p *= b.NightPassengerMultiplier;
                    pay += p;
                    count++;
                }
            }
            if (count >= b.ComboThreshold) pay *= b.ComboMultiplier;
            return new DeliveryQuote(MathUtil.RoundHalfUp(pay * multiplier), count);
        }

        public DeliveryQuote Deliver(RunState run, IReadOnlyList<CargoType> accepts, float multiplier)
        {
            var quote = Quote(run, accepts, multiplier);
            if (quote.Count == 0) return quote;
            foreach (var w in run.Wagons)
            {
                int removed = w.Items.RemoveAll(item =>
                {
                    if (!Contains(accepts, item)) return false;
                    run.Delivered[(int)item]++;
                    return true;
                });
                if (removed > 0) w.Version++;
            }
            return quote;
        }

        public int LoseRandom(RunState run, int count)
        {
            int lost = 0;
            var loaded = new List<Wagon>();
            for (int k = 0; k < count; k++)
            {
                loaded.Clear();
                foreach (var w in run.Wagons) if (w.Items.Count > 0) loaded.Add(w);
                if (loaded.Count == 0) break;
                var wagon = loaded[run.FxRng.NextInt(loaded.Count)];
                wagon.Items.RemoveAt(wagon.Items.Count - 1);
                wagon.Version++;
                lost++;
            }
            return lost;
        }

        public CargoSummary Summarize(RunState run)
        {
            var s = new CargoSummary();
            foreach (var w in run.Wagons)
            {
                if (w.Type == WagonType.Passenger) s.PassengerCapacity += w.Capacity; else s.FreightCapacity += w.Capacity;
                foreach (var item in w.Items)
                {
                    if (item == CargoType.Passenger) s.Passengers++;
                    else if (item == CargoType.Coal) s.Coal++;
                    else s.Food++;
                }
            }
            return s;
        }

        private static bool Contains(IReadOnlyList<CargoType> list, CargoType c)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == c) return true;
            return false;
        }
    }
}
