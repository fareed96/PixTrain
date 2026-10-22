using PixTrain.Core.Models;

namespace PixTrain.Core.Repositories
{
    /// <summary>Bellekte tutan depo: testler ve kaydın gerekmediği vitrin modu için.</summary>
    public sealed class InMemoryProgressRepository : IProgressRepository
    {
        private PlayerProgress _progress;

        public InMemoryProgressRepository(PlayerProgress initial)
        {
            _progress = initial;
        }

        public int SaveCount { get; private set; }

        public PlayerProgress Get() => _progress;

        public void Save() => SaveCount++;

        public void Replace(PlayerProgress progress) => _progress = progress;
    }
}
