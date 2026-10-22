using System.Collections.Generic;
using PixTrain.Core.Data;

namespace PixTrain.Core.Models
{
    public sealed class Wagon
    {
        public WagonType Type;
        public int Capacity;
        public readonly List<CargoType> Items = new List<CargoType>();
        /// <summary>Yük her değiştiğinde artar; görsel katman vagonu yeniden çizmek için kullanır.</summary>
        public int Version;

        public int Free => Capacity - Items.Count;
        public bool IsFull => Items.Count >= Capacity;
    }

    /// <summary>Hat üzerindeki her şey: peron, altın, tehlike, gar.</summary>
    public sealed class Gate
    {
        public GateKind Kind;
        public float Y;
        /// <summary>Şerit (0-2). Gar için -1.</summary>
        public int Lane;
        public bool Done;
        public bool Hit;
        public bool Gone;
        public bool IsOrigin;

        // Yalnızca inek için: yatay konum ve hareket
        public float X;
        public int Direction;
        public float SpeedX;
        public bool Fleeing;
    }

    public sealed class Junction
    {
        public float Y;
        public bool Done;
        public bool AutopilotDecided;
    }

    public sealed class LaneTransition
    {
        public float StartY;
        public int FromLane;
        public int ToLane;
    }

    public sealed class FieldPatch
    {
        public float X0, X1, Y0, Y1;
        public CropType Crop;

        public bool Contains(float x, float y) => x >= X0 && x < X1 && y >= Y0 && y < Y1;
    }

    public sealed class Zone
    {
        public ZoneType Type;
        public float Y0;
        public float Y1;
        public readonly List<float> Roads = new List<float>();
        public readonly List<FieldPatch> Fields = new List<FieldPatch>();
        public bool HasPlatform;
        public float PlatformY0;
        public float PlatformY1;
    }

    public sealed class OncomingTrain
    {
        /// <summary>Lokomotifin ön ucunun dünya konumu; tren aşağı (oyuncuya doğru) ilerler.</summary>
        public float Y;
        public int Lane;
        public float Speed;
        public bool Passed;
        public bool Gone;
        public bool Warned;
    }

    public sealed class ContractProgress
    {
        public ContractDef Def;
        public int Target;

        public string Title => Def.Title(Target);
    }
}
