using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Models
{
    /// <summary>
    /// Bir seferin tüm durumu. Dünya ekseni: y ileri (ekranda yukarı), x yatay; birim dünya pikseli.
    /// Kuralları RunService işletir; görsel katman yalnızca okur.
    /// </summary>
    public sealed class RunState
    {
        public int RouteIndex;
        public RouteDef Route;
        public BiomeDef Biome;
        public bool IsAttract;
        public int Seed;

        /// <summary>Dünya üretimi (peron, makas). Aynı tohum aynı hattı verir.</summary>
        public IRandom Rng;
        /// <summary>Bölge üretimi (kır, kasaba, tünel).</summary>
        public IRandom ZoneRng;
        /// <summary>Dünyayı etkilemeyen rastgelelik: kaza kaybı, otopilot, hava.</summary>
        public IRandom FxRng;

        /// <summary>Hat uzunluğu. Vitrin (attract) modunda sonsuz.</summary>
        public float Length;
        public float Y;
        public float Speed;
        public float CruiseSpeed;
        public float FuelEfficiency;
        public float FuelMax;
        public float Fuel;
        public int HeadlightLevel;
        public int LocomotiveIndex;

        public int Lane = 1;
        public int StartLane = 1;
        public int SwitchDir;
        public readonly List<LaneTransition> Transitions = new List<LaneTransition>();

        public readonly List<Junction> Junctions = new List<Junction>();
        public readonly List<Gate> Gates = new List<Gate>();
        public readonly List<Zone> Zones = new List<Zone>();
        public readonly List<OncomingTrain> Oncoming = new List<OncomingTrain>();
        public float ZoneCursorY;
        public int ZoneCursorIndex;
        public float GenY;

        public readonly List<Wagon> Wagons = new List<Wagon>();
        public readonly List<ContractProgress> Contracts = new List<ContractProgress>();

        public float Earned;
        public float NightEarned;
        public int Crashes;
        public readonly int[] Delivered = new int[3];
        public int FinalPay;
        public int Streak;

        public float TimeOfDay;
        public float Time;
        public RunPhase Phase;
        public bool Raining;

        public float Shake;
        public float SlowTimer;
        public float HornCooldown;
        public float HornTimer;
        public float TurboTimer;
        public float TurboCooldown;

        public bool WarnedFuelLow;
        public bool WarnedNear;
        public bool WasNight;
        public float CleanupTimer;

        /// <summary>Son Tick'te oluşan olaylar.</summary>
        public readonly List<RunEvent> Events = new List<RunEvent>();
        public RunResult Result;

        public bool IsTurbo => TurboTimer > 0f;
        public bool IsFinished => Phase == RunPhase.Finished;
        public float Progress => IsAttract ? 0f : Y / Length;
        public int DeliveredCount(CargoType c) => Delivered[(int)c];
    }
}
