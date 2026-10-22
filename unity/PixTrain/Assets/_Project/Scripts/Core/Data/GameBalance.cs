namespace PixTrain.Core.Data
{
    /// <summary>
    /// Oyunun tüm sayısal ayarları tek yerde. Birimler: dünya pikseli (1 px = 10 m), saniye.
    /// Değerler oynanabilir HTML prototipiyle (prototype/index.html) birebir aynıdır.
    /// </summary>
    public sealed class GameBalance
    {
        // Hat geometrisi
        public float[] LaneX = { 56f, 80f, 104f };
        public float CenterX = 80f;
        public float JunctionLength = 36f;
        public float LaneHitTolerance = 6f;
        public float CowHitTolerance = 9f;
        public float OncomingHitTolerance = 8f;

        // İnekler
        public float CowMinX = 50f;
        public float CowMaxX = 110f;
        public float CowDespawnMinX = 20f;
        public float CowDespawnMaxX = 140f;
        public float CowFleeSpeed = 55f;
        public float CowCrashFleeSpeed = 70f;

        // Hız
        public float BaseSpeed = 48f;
        public float SpeedPerEngineLevel = 0.08f;
        public float ProgressSpeedBonus = 0.25f;
        public float AttractSpeedBonus = 0.1f;
        public float SlowSpeedMultiplier = 0.45f;
        public float ArrivalBrakeDistance = 120f;
        public float MinArrivalSpeed = 10f;
        public float OutOfFuelStopSpeed = 1.2f;

        // Yakıt
        public float BaseFuel = 100f;
        public float FuelPerTankLevel = 25f;
        public float PixelsPerFuel = 38f;
        public float FuelGateAmount = 45f;
        public float FuelLowWarning = 0.22f;
        public float FuelMercyThreshold = 0.4f;

        // Yük ve para
        public int BaseCapacity = 3;
        public int BasePickupAmount = 3;
        public int CoinValue = 3;
        public float RouteMultiplierStep = 0.15f;
        public int[] CargoPrices = { 9, 8, 6 }; // CargoType sırasıyla: yolcu, kömür, gıda
        public float NightPassengerMultiplier = 1.5f;
        public int ComboThreshold = 6;
        public float ComboMultiplier = 1.25f;
        public float FinalDeliveryMultiplier = 1.5f;
        public float StreakStep = 0.05f;
        public int StreakMaxSteps = 10;

        // Turbo, düdük
        public float TurboDuration = 2.6f;
        public float TurboCooldown = 10f;
        public float TurboSpeedMultiplier = 1.6f;
        public float TurboFuelMultiplier = 2f;
        public float TurboCoinMultiplier = 2f;
        public float TurboDeliveryMultiplier = 1.25f;
        public float HornCooldown = 6f;
        public float AttractHornCooldown = 3f;
        public float HornRange = 170f;
        public float HornDuration = 0.8f;

        // Kazalar
        public int CrashCargoLoss = 2;
        public float CrashFuelLoss = 8f;
        public float CrashSlowDuration = 1.4f;
        public int OncomingExtraCargoLoss = 2;
        public float OncomingExtraFuelLoss = 10f;
        public float OncomingSlowDuration = 2.2f;
        public float OncomingWarningDistance = 480f;

        // Zaman
        public float DayLengthSeconds = 120f;
        public float AttractDayLengthSeconds = 60f;
        public float IdleCapMinutes = 240f;
        public int StartCoins = 150;

        // Dünya üretimi
        public float StationZoneStart = -420f;
        public float StationZoneLength = 620f;
        public float FirstSegmentY = 170f;
        public float TerminalZoneOffset = 520f;
        public float FinalSegmentOffset = 560f;
        public float NearDestinationDistance = 900f;
        public float GenerateAhead = 620f;
        public float ZonesAhead = 1000f;
        public float CleanupBehind = 220f;
    }
}
