using System;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Repositories;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Tests
{
    internal sealed class FixedTime : ITimeProvider
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>Testler için hazır oyun: bellekte ilerleme, sabit saat.</summary>
    internal sealed class TestGame
    {
        public readonly FixedTime Time = new FixedTime();
        public readonly InMemoryProgressRepository Repo;
        public readonly GameServices G;

        public TestGame()
        {
            var catalog = DefaultCatalog.Create();
            Repo = new InMemoryProgressRepository(PlayerProgress.CreateDefault(catalog, Time.UtcNow));
            G = GameServices.Create(Repo, Time, catalog);
        }

        public PlayerProgress Progress => Repo.Get();
        public GameBalance B => G.Catalog.Balance;

        /// <summary>Üretilmiş içeriği temizlenmiş sefer: test kendi peronlarını koyar.</summary>
        public RunState CleanRun(int route = 0, int seed = 1)
        {
            var run = G.Runs.StartRun(route, false, seed);
            run.Gates.Clear();
            run.Junctions.Clear();
            run.Oncoming.Clear();
            run.GenY = float.MaxValue;
            return run;
        }

        /// <summary>Treni verilen y'nin hemen önüne koyar ve sabit hızla bir adım ilerletir.</summary>
        public void DriveThrough(RunState run, float y, float speed = 50f, float dt = 0.1f)
        {
            run.Y = y - 1f;
            run.Speed = speed;
            G.Runs.Tick(run, dt);
        }

        public Gate AddGate(RunState run, GateKind kind, int lane, float y)
        {
            var g = new Gate { Kind = kind, Lane = lane, Y = y };
            if (kind == GateKind.Cow) { g.X = B.LaneX[lane]; g.Direction = 1; g.SpeedX = 0f; }
            run.Gates.Add(g);
            run.Gates.Sort((a, b) => a.Y.CompareTo(b.Y));
            return g;
        }

        public void Fill(RunState run, CargoType cargo, int count) => G.Cargo.Load(run, cargo, count);
    }
}
