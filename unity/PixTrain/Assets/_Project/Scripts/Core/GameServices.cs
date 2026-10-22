using PixTrain.Core.Data;
using PixTrain.Core.Repositories;
using PixTrain.Core.Services;
using PixTrain.Core.Utils;

namespace PixTrain.Core
{
    /// <summary>
    /// Kompozisyon kökü: servisleri bağımlılıklarıyla birlikte oluşturur.
    /// Unity tarafı oyun açılışında bir kez çağırır ve servisleri arayüzleri üzerinden kullanır.
    /// </summary>
    public sealed class GameServices
    {
        public GameCatalog Catalog { get; private set; }
        public IProgressRepository Progress { get; private set; }
        public IEconomyService Economy { get; private set; }
        public IGarageService Garage { get; private set; }
        public ICargoService Cargo { get; private set; }
        public IContractService Contracts { get; private set; }
        public IWorldGenService World { get; private set; }
        public IRunService Runs { get; private set; }

        public static GameServices Create(IProgressRepository progress, ITimeProvider time = null, GameCatalog catalog = null)
        {
            catalog = catalog ?? DefaultCatalog.Create();
            time = time ?? new SystemTimeProvider();
            progress.Get().Normalize(catalog);

            var economy = new EconomyService(catalog, progress, time);
            var garage = new GarageService(catalog, progress, economy);
            var cargo = new CargoService(catalog, economy);
            var contracts = new ContractService(catalog);
            var world = new WorldGenService(catalog);
            var runs = new RunService(catalog, world, cargo, economy, garage, contracts, progress);

            return new GameServices
            {
                Catalog = catalog,
                Progress = progress,
                Economy = economy,
                Garage = garage,
                Cargo = cargo,
                Contracts = contracts,
                World = world,
                Runs = runs,
            };
        }
    }
}
