namespace PixTrain.Core.Utils
{
    public interface IRandom
    {
        /// <summary>[0, 1) aralığında sayı.</summary>
        float NextFloat();

        /// <summary>[0, maxExclusive) aralığında tamsayı.</summary>
        int NextInt(int maxExclusive);
    }

    /// <summary>
    /// Mulberry32: prototiple aynı, hızlı ve tohumlanabilir rastgele sayı üreteci.
    /// Aynı tohum her platformda aynı dünyayı üretir.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        public float NextFloat()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint t = _state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t = (t + (t ^ (t >> 7)) * (t | 61u)) ^ t;
                return MathUtil.ToUnit(t ^ (t >> 14));
            }
        }

        public int NextInt(int maxExclusive)
        {
            int v = (int)(NextFloat() * maxExclusive);
            return v >= maxExclusive ? maxExclusive - 1 : v;
        }
    }
}
