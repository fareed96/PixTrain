using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    public interface IContractService
    {
        /// <summary>Sefer için iki sözleşme seçer. İlk hat her zaman öğretici ikiliyi verir.</summary>
        List<ContractProgress> Pick(int routeIndex, IRandom rng);
        int CurrentValue(RunState run, ContractMetric metric);
        /// <summary>Sefer içinde gösterim için: hedefe ulaşıldı mı (varış koşulu hariç).</summary>
        bool IsReached(RunState run, ContractProgress contract);
        /// <summary>Sefer sonunda yıldız için: hedef ve varış koşulu birlikte.</summary>
        bool IsCompleted(RunState run, ContractProgress contract, bool arrived);
    }

    public sealed class ContractService : IContractService
    {
        private readonly GameCatalog _catalog;

        public ContractService(GameCatalog catalog)
        {
            _catalog = catalog;
        }

        public List<ContractProgress> Pick(int routeIndex, IRandom rng)
        {
            var chosen = new List<ContractDef>();
            if (routeIndex == 0)
            {
                chosen.Add(_catalog.Contract(ContractMetric.Passengers));
                chosen.Add(_catalog.Contract(ContractMetric.Coins));
            }
            else
            {
                var pool = new List<ContractDef>(_catalog.Contracts);
                while (chosen.Count < 2 && pool.Count > 0)
                {
                    int i = rng.NextInt(pool.Count);
                    chosen.Add(pool[i]);
                    pool.RemoveAt(i);
                }
            }
            var result = new List<ContractProgress>();
            foreach (var def in chosen) result.Add(new ContractProgress { Def = def, Target = def.TargetForRoute(routeIndex) });
            return result;
        }

        public int CurrentValue(RunState run, ContractMetric metric)
        {
            switch (metric)
            {
                case ContractMetric.Passengers: return run.DeliveredCount(CargoType.Passenger);
                case ContractMetric.Coal: return run.DeliveredCount(CargoType.Coal);
                case ContractMetric.Food: return run.DeliveredCount(CargoType.Food);
                case ContractMetric.Coins: return (int)run.Earned;
                case ContractMetric.NightCoins: return (int)run.NightEarned;
                case ContractMetric.NoCrash: return run.Crashes == 0 ? 1 : 0;
                default: return 0;
            }
        }

        public bool IsReached(RunState run, ContractProgress contract) =>
            !contract.Def.EndOnly && CurrentValue(run, contract.Def.Metric) >= contract.Target;

        public bool IsCompleted(RunState run, ContractProgress contract, bool arrived) =>
            CurrentValue(run, contract.Def.Metric) >= contract.Target && (!contract.Def.EndOnly || arrived);
    }
}
