using System;

namespace PixTrain.Core.Utils
{
    /// <summary>Oyun kurallarında kullanılan küçük matematik yardımcıları (Unity'den bağımsız).</summary>
    public static class MathUtil
    {
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;

        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Smoothstep: makas geçişinde trenin raya oturduğu eğri.</summary>
        public static float Smooth(float t) => t * t * (3f - 2f * t);

        /// <summary>JavaScript Math.round ile aynı: .5 yukarı yuvarlanır.</summary>
        public static int RoundHalfUp(float v) => (int)Math.Floor(v + 0.5f);

        /// <summary>32 bitlik değeri [0, 1) aralığına çevirir; float yuvarlaması 1'e taşmaz.</summary>
        public static float ToUnit(uint v)
        {
            float f = (float)(v / 4294967296.0);
            return f < 1f ? f : 0.99999994f;
        }

        /// <summary>Konumdan deterministik 0..1 değer (zemin dokusu, pencere ışıkları vb.).</summary>
        public static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (int)((uint)h >> 13)) * 1274126177;
                h ^= (int)((uint)h >> 16);
                return ToUnit((uint)h);
            }
        }
    }
}
