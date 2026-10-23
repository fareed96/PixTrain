using System;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Repositories;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Services
{
    public interface IRunService
    {
        /// <summary>Yeni sefer başlatır. <paramref name="attract"/> = menü arkasındaki otomatik vitrin seferi.</summary>
        RunState StartRun(int routeIndex, bool attract, int? seed = null);
        /// <summary>Seferi dt saniye ilerletir; olaylar <see cref="RunState.Events"/> listesine yazılır.</summary>
        void Tick(RunState run, float dt);
        /// <summary>Sıradaki makası bir adım sola (-1) ya da sağa (+1) çevirir.</summary>
        bool Steer(RunState run, int direction);
        bool Horn(RunState run);
        bool Turbo(RunState run);
        /// <summary>Trenin verilen dünya y'sindeki yatay konumu (vagonlar rayı satır satır izler).</summary>
        float TrainXAt(RunState run, float y);
        int LaneOfX(float x);
        Junction NextJunction(RunState run);
        GatePreview Preview(RunState run, Gate gate);
        float DeliveryMultiplier(RunState run);
        /// <summary>Oyuncu seferi yarıda bıraktı: kazanılan para korunur, ilerleme kaydedilir.</summary>
        void Abandon(RunState run);
        /// <summary>Yakıtı biten ve duran trene yakıt verip sefere devam ettirir (ödüllü reklam sonrası).</summary>
        bool ContinueRun(RunState run, float fuel);
        /// <summary>Durmuş seferi bitirir (oyuncu devam etmek istemedi).</summary>
        void GiveUp(RunState run);
    }

    public sealed class RunService : IRunService
    {
        private readonly GameCatalog _catalog;
        private readonly IWorldGenService _world;
        private readonly ICargoService _cargo;
        private readonly IEconomyService _economy;
        private readonly IGarageService _garage;
        private readonly IContractService _contracts;
        private readonly IProgressRepository _progress;
        private readonly Random _seedSource = new Random();

        public RunService(GameCatalog catalog, IWorldGenService world, ICargoService cargo, IEconomyService economy,
            IGarageService garage, IContractService contracts, IProgressRepository progress)
        {
            _catalog = catalog;
            _world = world;
            _cargo = cargo;
            _economy = economy;
            _garage = garage;
            _contracts = contracts;
            _progress = progress;
        }

        private GameBalance B => _catalog.Balance;

        // ------------------------------------------------------------------ başlatma

        public RunState StartRun(int routeIndex, bool attract, int? seed = null)
        {
            int s = seed ?? _seedSource.Next();
            var route = _catalog.Routes[routeIndex];
            var spec = _garage.BuildTrainSpec();
            var run = new RunState
            {
                RouteIndex = routeIndex,
                Route = route,
                Biome = _catalog.Biome(route.Biome),
                IsAttract = attract,
                Seed = s,
                Rng = new SeededRandom(s),
                ZoneRng = new SeededRandom(s ^ 0x5bd1e995),
                FxRng = new SeededRandom(unchecked(s * 31 + 7)),
                Length = attract ? float.PositiveInfinity : route.Length,
                CruiseSpeed = spec.CruiseSpeed,
                FuelEfficiency = spec.FuelEfficiency,
                FuelMax = spec.FuelMax,
                Fuel = spec.FuelMax,
                HeadlightLevel = spec.HeadlightLevel,
                LocomotiveIndex = spec.LocomotiveIndex,
                ZoneCursorY = B.StationZoneStart,
                GenY = B.FirstSegmentY,
            };
            run.TimeOfDay = attract ? 0.3f + run.FxRng.NextFloat() * 0.4f : route.StartTimeOfDay;
            run.Raining = run.FxRng.NextFloat() < run.Biome.RainChance;
            foreach (var type in spec.Wagons) run.Wagons.Add(new Wagon { Type = type, Capacity = spec.Capacity });
            if (!attract) run.Contracts.AddRange(_contracts.Pick(routeIndex, run.FxRng));
            run.Gates.Add(new Gate { Kind = GateKind.Terminal, Y = 0f, Lane = -1, Done = true, IsOrigin = true });
            _world.EnsureGenerated(run);
            return run;
        }

        // ------------------------------------------------------------------ oyuncu eylemleri

        public bool Steer(RunState run, int direction)
        {
            if (run.IsAttract || run.Phase != RunPhase.Running) return false;
            int next = MathUtil.Clamp(run.SwitchDir + Math.Sign(direction), -1, 1);
            if (run.Lane + next < 0 || run.Lane + next > 2) next = run.SwitchDir;
            if (next == run.SwitchDir)
            {
                Emit(run, RunEventType.SwitchBlocked);
                return false;
            }
            run.SwitchDir = next;
            Emit(run, RunEventType.SwitchChanged, next);
            return true;
        }

        public bool Horn(RunState run)
        {
            if (run.HornCooldown > 0f || run.Phase != RunPhase.Running) return false;
            run.HornCooldown = run.IsAttract ? B.AttractHornCooldown : B.HornCooldown;
            run.HornTimer = B.HornDuration;
            foreach (var g in run.Gates)
            {
                if (g.Kind != GateKind.Cow || g.Done || g.Fleeing) continue;
                if (g.Y > run.Y && g.Y < run.Y + B.HornRange) Flee(g, B.CowFleeSpeed);
            }
            Emit(run, RunEventType.Horn);
            return true;
        }

        public bool Turbo(RunState run)
        {
            if (run.IsAttract || run.TurboCooldown > 0f || run.Phase != RunPhase.Running) return false;
            run.TurboTimer = B.TurboDuration;
            run.TurboCooldown = B.TurboCooldown;
            Emit(run, RunEventType.Turbo);
            return true;
        }

        public bool ContinueRun(RunState run, float fuel)
        {
            if (!run.IsStalled || run.ContinueUsed) return false;
            run.ContinueUsed = true;
            run.Fuel = Math.Min(run.FuelMax, fuel);
            run.WarnedFuelLow = false;
            run.Phase = RunPhase.Running;
            Emit(run, RunEventType.Continued, (int)run.Fuel);
            return true;
        }

        public void GiveUp(RunState run)
        {
            if (run.IsStalled || run.Phase == RunPhase.OutOfFuel) Finish(run, false);
        }

        public void Abandon(RunState run)
        {
            if (!run.IsAttract) _progress.Save();
        }

        // ------------------------------------------------------------------ sorgular

        public float TrainXAt(RunState run, float y)
        {
            var list = run.Transitions;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var tr = list[i];
                if (tr.StartY > y) continue;
                float t = (y - tr.StartY) / B.JunctionLength;
                return t >= 1f ? B.LaneX[tr.ToLane] : MathUtil.Lerp(B.LaneX[tr.FromLane], B.LaneX[tr.ToLane], MathUtil.Smooth(t));
            }
            return B.LaneX[run.StartLane];
        }

        public int LaneOfX(float x)
        {
            if (x < (B.LaneX[0] + B.LaneX[1]) * 0.5f) return 0;
            if (x < (B.LaneX[1] + B.LaneX[2]) * 0.5f) return 1;
            return 2;
        }

        public Junction NextJunction(RunState run)
        {
            foreach (var j in run.Junctions) if (!j.Done) return j;
            return null;
        }

        public float DeliveryMultiplier(RunState run) =>
            _economy.StreakMultiplier(run.Streak) * (run.IsTurbo ? B.TurboDeliveryMultiplier : 1f);

        public GatePreview Preview(RunState run, Gate gate)
        {
            var station = _catalog.Station(gate.Kind);
            switch (station.Category)
            {
                case GateCategory.Pickup:
                {
                    int n = Math.Min(_cargo.FreeCapacity(run, station.Cargo), PickupAmount(run));
                    return n > 0 ? new GatePreview("+" + n, ToneOf(station.Cargo)) : new GatePreview("DOLU", PreviewTone.Warning);
                }
                case GateCategory.Delivery:
                {
                    var q = _cargo.Quote(run, station.Accepts, DeliveryMultiplier(run));
                    return q.Count > 0 ? new GatePreview("+" + q.Pay, PreviewTone.Money) : new GatePreview("0", PreviewTone.Neutral);
                }
                case GateCategory.Fuel:
                    return new GatePreview("+" + (int)B.FuelGateAmount, PreviewTone.Fuel);
                default:
                    return new GatePreview(null, PreviewTone.Neutral);
            }
        }

        // ------------------------------------------------------------------ zaman adımı

        public void Tick(RunState run, float dt)
        {
            run.Events.Clear();
            if (dt <= 0f) return;
            run.Time += dt;
            float dayLength = run.IsAttract ? B.AttractDayLengthSeconds : B.DayLengthSeconds;
            if (!run.IsFinished && !run.IsStalled) run.TimeOfDay = (run.TimeOfDay + dt / dayLength) % 1f;
            run.HornCooldown = Math.Max(0f, run.HornCooldown - dt);
            run.HornTimer = Math.Max(0f, run.HornTimer - dt);
            run.TurboTimer = Math.Max(0f, run.TurboTimer - dt);
            run.TurboCooldown = Math.Max(0f, run.TurboCooldown - dt);
            run.Shake = Math.Max(0f, run.Shake - dt);

            if (!run.IsFinished && !run.IsStalled)
            {
                Move(run, dt);
                if (run.IsFinished) return;
            }

            bool night = DayCycle.IsNight(run.TimeOfDay);
            if (!run.IsAttract && night && !run.WasNight && run.Time > 2f) Emit(run, RunEventType.NightFell);
            run.WasNight = night;

            run.CleanupTimer += dt;
            if (run.CleanupTimer > 1f)
            {
                run.CleanupTimer = 0f;
                Cleanup(run);
            }
        }

        private void Move(RunState run, float dt)
        {
            float target = run.CruiseSpeed * (1f + (run.IsAttract ? B.AttractSpeedBonus : B.ProgressSpeedBonus * run.Y / run.Length));
            if (run.SlowTimer > 0f) { run.SlowTimer -= dt; target *= B.SlowSpeedMultiplier; }
            if (run.IsTurbo) target *= B.TurboSpeedMultiplier;
            if (run.Phase == RunPhase.OutOfFuel) target = 0f;
            if (!run.IsAttract && run.Y > run.Length - B.ArrivalBrakeDistance)
                target = Math.Max(B.MinArrivalSpeed, run.CruiseSpeed * (float)Math.Sqrt(Math.Max(0f, run.Length - run.Y) / B.ArrivalBrakeDistance));
            float rate = run.Speed < target ? (run.IsTurbo ? 3f : 0.8f) : 2.2f;
            run.Speed += (target - run.Speed) * Math.Min(1f, dt * rate);
            float dy = run.Speed * dt;
            run.Y += dy;

            if (!run.IsAttract && run.Phase == RunPhase.Running)
            {
                run.Fuel -= dy / B.PixelsPerFuel * run.FuelEfficiency * (run.IsTurbo ? B.TurboFuelMultiplier : 1f);
                if (run.Fuel <= run.FuelMax * B.FuelLowWarning && !run.WarnedFuelLow)
                {
                    run.WarnedFuelLow = true;
                    Emit(run, RunEventType.FuelLow);
                }
                if (run.Fuel <= 0f)
                {
                    run.Fuel = 0f;
                    run.Phase = RunPhase.OutOfFuel;
                    Emit(run, RunEventType.OutOfFuel);
                }
            }
            if (run.Phase == RunPhase.OutOfFuel && run.Speed < B.OutOfFuelStopSpeed)
            {
                run.Speed = 0f;
                if (run.IsAttract || run.ContinueUsed) { Finish(run, false); return; }
                run.Phase = RunPhase.Stalled;
                Emit(run, RunEventType.ContinueOffered);
                return;
            }
            if (!run.IsAttract && run.Y >= run.Length - 0.5f) { run.Y = run.Length; Finish(run, true); return; }
            if (!run.IsAttract && run.Y > run.Length - B.NearDestinationDistance && !run.WarnedNear)
            {
                run.WarnedNear = true;
                Emit(run, RunEventType.NearDestination);
            }

            _world.EnsureGenerated(run);
            PassJunctions(run);
            UpdateGates(run, dt);
            UpdateOncoming(run, dt);
            if (run.IsAttract) Autopilot(run);
        }

        private void PassJunctions(RunState run)
        {
            foreach (var j in run.Junctions)
            {
                if (j.Done) continue;
                if (run.Y < j.Y) break;
                j.Done = true;
                int next = run.Lane + run.SwitchDir;
                if (run.SwitchDir != 0 && next >= 0 && next <= 2)
                {
                    run.Transitions.Add(new LaneTransition { StartY = j.Y, FromLane = run.Lane, ToLane = next });
                    run.Lane = next;
                }
                run.SwitchDir = 0;
                Emit(run, RunEventType.JunctionPassed);
            }
        }

        private void UpdateGates(RunState run, float dt)
        {
            foreach (var g in run.Gates)
            {
                if (g.Y > run.Y + 420f) break;
                if (g.Kind == GateKind.Cow && !g.Gone) MoveCow(g, dt);
                if (!g.Done && run.Y >= g.Y) HitGate(run, g);
            }
        }

        private void MoveCow(Gate g, float dt)
        {
            g.X += g.Direction * g.SpeedX * dt;
            if (!g.Fleeing)
            {
                if (g.X < B.CowMinX) { g.X = B.CowMinX; g.Direction = 1; }
                if (g.X > B.CowMaxX) { g.X = B.CowMaxX; g.Direction = -1; }
            }
            else if (g.X < B.CowDespawnMinX || g.X > B.CowDespawnMaxX)
            {
                g.Gone = true;
            }
        }

        private void HitGate(RunState run, Gate g)
        {
            g.Done = true;
            var station = _catalog.Station(g.Kind);
            if (station.Category == GateCategory.Terminal) return;
            float tx = TrainXAt(run, g.Y);
            if (g.Kind == GateKind.Cow)
            {
                if (!g.Fleeing && Math.Abs(tx - g.X) < B.CowHitTolerance) Crash(run, g);
                return;
            }
            if (Math.Abs(tx - B.LaneX[g.Lane]) > B.LaneHitTolerance) return;
            g.Hit = true;

            switch (station.Category)
            {
                case GateCategory.Coin:
                {
                    float mult = _economy.RouteMultiplier(run.RouteIndex) * (run.IsTurbo ? B.TurboCoinMultiplier : 1f) * _economy.StreakMultiplier(run.Streak);
                    int value = MathUtil.RoundHalfUp(B.CoinValue * mult);
                    Earn(run, value);
                    g.Gone = true;
                    Emit(run, RunEventType.Coin, value, 0, CargoType.Passenger, tx);
                    break;
                }
                case GateCategory.Fuel:
                    run.Fuel = Math.Min(run.FuelMax, run.Fuel + B.FuelGateAmount);
                    BumpStreak(run);
                    Emit(run, RunEventType.Refuel, (int)B.FuelGateAmount, 0, CargoType.Passenger, tx);
                    break;
                case GateCategory.Pickup:
                {
                    int loaded = _cargo.Load(run, station.Cargo, PickupAmount(run));
                    if (loaded == 0)
                    {
                        Emit(run, RunEventType.WagonsFull, 0, 0, station.Cargo, tx);
                    }
                    else
                    {
                        BumpStreak(run);
                        Emit(run, RunEventType.Pickup, 0, loaded, station.Cargo, tx);
                    }
                    break;
                }
                case GateCategory.Delivery:
                {
                    var q = _cargo.Deliver(run, station.Accepts, DeliveryMultiplier(run));
                    if (q.Count == 0)
                    {
                        Emit(run, RunEventType.NothingToDeliver, 0, 0, CargoType.Passenger, tx);
                    }
                    else
                    {
                        Earn(run, q.Pay);
                        BumpStreak(run);
                        Emit(run, RunEventType.Delivery, q.Pay, q.Count, CargoType.Passenger, tx);
                    }
                    break;
                }
                case GateCategory.Hazard:
                    Crash(run, g);
                    break;
            }
        }

        private void UpdateOncoming(RunState run, float dt)
        {
            foreach (var o in run.Oncoming)
            {
                if (o.Gone) continue;
                o.Y -= o.Speed * dt;
                if (!o.Passed && o.Y <= run.Y + 1f)
                {
                    o.Passed = true;
                    if (run.Phase == RunPhase.Running && Math.Abs(TrainXAt(run, run.Y) - B.LaneX[o.Lane]) < B.OncomingHitTolerance)
                        OncomingCrash(run, o);
                }
                if (!run.IsAttract && !o.Warned && !o.Passed && o.Y - run.Y < B.OncomingWarningDistance && o.Lane == run.Lane)
                {
                    o.Warned = true;
                    Emit(run, RunEventType.OncomingWarning, o.Lane, 0, CargoType.Passenger, B.LaneX[o.Lane]);
                }
            }
        }

        // ------------------------------------------------------------------ kurallar

        private int PickupAmount(RunState run) => B.BasePickupAmount + run.RouteIndex / 2;

        private void Earn(RunState run, int amount)
        {
            run.Earned += amount;
            if (DayCycle.IsNight(run.TimeOfDay)) run.NightEarned += amount;
            if (!run.IsAttract) _economy.AddCoins(amount);
        }

        private void BumpStreak(RunState run)
        {
            run.Streak++;
            if (!run.IsAttract && (run.Streak == 5 || run.Streak == 10)) Emit(run, RunEventType.StreakUp, run.Streak);
        }

        private int CrashCore(RunState run)
        {
            run.Streak = 0;
            run.Crashes++;
            run.Shake = 0.5f;
            run.SlowTimer = B.CrashSlowDuration;
            run.Fuel = Math.Max(0f, run.Fuel - B.CrashFuelLoss);
            return _cargo.LoseRandom(run, B.CrashCargoLoss);
        }

        private void Crash(RunState run, Gate g)
        {
            g.Hit = true;
            int lost = CrashCore(run);
            if (g.Kind == GateKind.Cow) Flee(g, B.CowCrashFleeSpeed);
            Emit(run, RunEventType.Crash, 0, lost);
        }

        private void OncomingCrash(RunState run, OncomingTrain o)
        {
            o.Gone = true;
            int lost = CrashCore(run);
            run.Shake = 0.9f;
            run.SlowTimer = B.OncomingSlowDuration;
            run.Fuel = Math.Max(0f, run.Fuel - B.OncomingExtraFuelLoss);
            lost += _cargo.LoseRandom(run, B.OncomingExtraCargoLoss);
            Emit(run, RunEventType.OncomingCrash, 0, lost);
        }

        private void Flee(Gate cow, float speed)
        {
            cow.Fleeing = true;
            cow.Direction = cow.X < B.CenterX ? -1 : 1;
            cow.SpeedX = speed;
        }

        private void Finish(RunState run, bool arrived)
        {
            if (run.IsFinished) return;
            run.Phase = RunPhase.Finished;
            if (run.IsAttract) return;

            if (arrived)
            {
                var q = _cargo.Deliver(run, CargoService.All, B.FinalDeliveryMultiplier);
                run.FinalPay = q.Pay;
                if (q.Count > 0) Earn(run, q.Pay);
            }

            var contractResults = new ContractResult[run.Contracts.Count];
            int completed = 0;
            for (int i = 0; i < run.Contracts.Count; i++)
            {
                bool ok = _contracts.IsCompleted(run, run.Contracts[i], arrived);
                if (ok) completed++;
                contractResults[i] = new ContractResult { Title = run.Contracts[i].Title, Completed = ok };
            }
            int stars = arrived ? 1 + completed : 0;
            int cityIndex = run.RouteIndex + 1;
            int levelBefore = _economy.CityLevel(cityIndex);
            var p = _progress.Get();
            int previousBest = p.BestStars[run.RouteIndex];
            if (arrived)
            {
                p.CompletedRoutes[run.RouteIndex] = true;
                p.BestStars[run.RouteIndex] = Math.Max(previousBest, stars);
                p.UnlockedRoute = Math.Max(p.UnlockedRoute, Math.Min(_catalog.Routes.Count - 1, run.RouteIndex + 1));
            }
            p.SeenTutorial = true;
            _progress.Save();

            run.Result = new RunResult
            {
                Arrived = arrived,
                Stars = stars,
                Earned = MathUtil.RoundHalfUp(run.Earned),
                FinalPay = run.FinalPay,
                Crashes = run.Crashes,
                Contracts = contractResults,
                CityIndex = cityIndex,
                CityLevelBefore = levelBefore,
                CityLevelAfter = _economy.CityLevel(cityIndex),
                PreviousBestStars = previousBest,
                IdleRateAfter = _economy.IdleRatePerMinute(),
            };
            Emit(run, RunEventType.Finished, stars);
        }

        private void Cleanup(RunState run)
        {
            float lo = run.Y - B.CleanupBehind;
            run.Gates.RemoveAll(g => g.Y < lo && g.Done);
            run.Junctions.RemoveAll(j => j.Y + B.JunctionLength < lo);
            while (run.Transitions.Count > 1 && run.Transitions[1].StartY + B.JunctionLength < lo)
            {
                run.StartLane = run.Transitions[0].ToLane;
                run.Transitions.RemoveAt(0);
            }
            while (run.Zones.Count > 2 && run.Zones[1].Y1 < lo)
            {
                run.Zones.RemoveAt(0);
                run.ZoneCursorIndex = 0;
            }
            run.Oncoming.RemoveAll(o => o.Gone || o.Y + 100f < lo);
        }

        // ------------------------------------------------------------------ vitrin otopilotu

        private void Autopilot(RunState run)
        {
            var j = NextJunction(run);
            if (j != null && !j.AutopilotDecided && j.Y - run.Y < 130f)
            {
                j.AutopilotDecided = true;
                int ji = run.Junctions.IndexOf(j);
                float yEnd = ji + 1 < run.Junctions.Count ? run.Junctions[ji + 1].Y : j.Y + 260f;
                float best = float.MinValue;
                int bestDir = 0;
                for (int d = -1; d <= 1; d++)
                {
                    int lane = run.Lane + d;
                    if (lane < 0 || lane > 2) continue;
                    float score = (d == 0 ? 0.6f : 0f) + run.FxRng.NextFloat() * 1.2f;
                    foreach (var g in run.Gates)
                    {
                        if (g.Done || g.Y < j.Y) continue;
                        if (g.Y > yEnd) break;
                        int gateLane = g.Kind == GateKind.Cow ? LaneOfX(g.X) : g.Lane;
                        if (gateLane != lane) continue;
                        var station = _catalog.Station(g.Kind);
                        switch (station.Category)
                        {
                            case GateCategory.Hazard: score -= g.Kind == GateKind.Cow ? 2f : 20f; break;
                            case GateCategory.Coin: score += 1.5f; break;
                            case GateCategory.Fuel: score += run.Fuel < run.FuelMax * 0.5f ? 8f : 0.5f; break;
                            case GateCategory.Pickup: score += _cargo.FreeCapacity(run, station.Cargo) > 0 ? 4f : 0f; break;
                            case GateCategory.Delivery: score += _cargo.Quote(run, station.Accepts, 1f).Pay / 8f; break;
                        }
                    }
                    foreach (var o in run.Oncoming)
                        if (!o.Passed && !o.Gone && o.Lane == lane && o.Y > j.Y - 220f) score -= 25f;
                    if (score > best) { best = score; bestDir = d; }
                }
                run.SwitchDir = bestDir;
            }

            foreach (var g in run.Gates)
            {
                if (g.Y > run.Y + 100f) break;
                if (g.Kind == GateKind.Cow && !g.Done && !g.Fleeing && g.Y > run.Y && Math.Abs(g.X - TrainXAt(run, g.Y)) < 12f)
                {
                    Horn(run);
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ yardımcılar

        private void Emit(RunState run, RunEventType type, int value = 0, int count = 0, CargoType cargo = CargoType.Passenger, float? x = null)
        {
            run.Events.Add(new RunEvent(type, x ?? TrainXAt(run, run.Y), value, count, cargo));
        }

        private static PreviewTone ToneOf(CargoType cargo)
        {
            switch (cargo)
            {
                case CargoType.Passenger: return PreviewTone.Passenger;
                case CargoType.Coal: return PreviewTone.Coal;
                default: return PreviewTone.Food;
            }
        }
    }
}
