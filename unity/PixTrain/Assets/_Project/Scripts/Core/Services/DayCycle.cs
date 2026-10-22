using System;
using PixTrain.Core.Data;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    /// <summary>Gün saati (0 = gece yarısı, 0.5 = öğle) üzerinden ışık ve gece hesapları.</summary>
    public static class DayCycle
    {
        /// <summary>Güneş yüksekliği: öğlen 1, gece yarısı -1.</summary>
        public static float Sun(float timeOfDay) => (float)Math.Cos((timeOfDay - 0.5f) * Math.PI * 2.0);

        /// <summary>0 tam gündüz, 1 tam gece.</summary>
        public static float Darkness(float timeOfDay) => MathUtil.Clamp((0.15f - Sun(timeOfDay)) / 0.55f, 0f, 1f);

        /// <summary>Şafak/akşam renk tonunun gücü (0-1).</summary>
        public static float Twilight(float timeOfDay) => MathUtil.Clamp(1f - Math.Abs(Sun(timeOfDay) - 0.08f) / 0.32f, 0f, 1f);

        public static bool IsNight(float timeOfDay) => Darkness(timeOfDay) > 0.5f;

        public static DayPhase Phase(float timeOfDay)
        {
            if (Darkness(timeOfDay) >= 0.6f) return DayPhase.Night;
            if (Sun(timeOfDay) < 0.45f) return timeOfDay < 0.5f ? DayPhase.Dawn : DayPhase.Dusk;
            return DayPhase.Day;
        }
    }
}
