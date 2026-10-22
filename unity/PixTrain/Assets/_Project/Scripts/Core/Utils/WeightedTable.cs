using System.Collections.Generic;

namespace PixTrain.Core.Utils
{
    /// <summary>Ağırlıklı rastgele seçim tablosu (peron, tehlike ve bölge seçimleri için).</summary>
    public sealed class WeightedTable<T>
    {
        private readonly List<KeyValuePair<T, float>> _entries = new List<KeyValuePair<T, float>>();

        public IReadOnlyList<KeyValuePair<T, float>> Entries => _entries;

        public WeightedTable<T> Add(T item, float weight)
        {
            _entries.Add(new KeyValuePair<T, float>(item, weight));
            return this;
        }

        public T Pick(IRandom rng)
        {
            float total = 0f;
            foreach (var e in _entries) total += e.Value;
            float r = rng.NextFloat() * total;
            foreach (var e in _entries)
            {
                r -= e.Value;
                if (r <= 0f) return e.Key;
            }
            return _entries[0].Key;
        }
    }
}
