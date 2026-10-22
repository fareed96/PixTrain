using PixTrain.Core;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Utils;
using PixTrain.Game.UI;
using PixTrain.Render;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixTrain.Game
{
    /// <summary>
    /// Oyunun Unity tarafı (sunum katmanı): ekranları yönetir, her karede kuralları ilerletir
    /// (IRunService.Tick), sahneyi yazılım tuvaline çizer ve dokuya yükler, olayları ses ve yazıya çevirir.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private enum Mode { Title, Map, Run, Pause, Result }

        private const int Width = WorldRenderer.Width;
        private const int MinHeight = 270;
        private const int MaxHeight = 356;
        private const float MaxDelta = 0.05f;

        private GameServices _s;
        private PlayerPrefsProgressRepository _repo;
        private RunState _run;
        private RunView _view;
        private readonly WorldRenderer _renderer = new WorldRenderer();
        private PixelImage _screen;
        private Texture2D _texture;

        private CanvasScaler _scaler;
        private RectTransform _stage;
        private RawImage _display;
        private HudView _hud;
        private TitleView _title;
        private MapView _map;
        private PauseView _pause;
        private ResultView _result;
        private SynthAudio _audio;
        private readonly GestureInput _input = new GestureInput();

        private Mode _mode;
        private int _height;
        private float _pixelScale = 2f;
        private float _trainY;
        private Rect _safeArea;
        private float _counterTimer;
        private float _resultDelay = -1f;
        private int _tipIndex;
        private float _tipTimer;
        private float _hintTimer;
        private float _chuffDistance;
        private float _lastY;

        // ================================================================ kurulum

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            Ui.LoadFonts();

            var catalog = DefaultCatalog.Create();
            _repo = new PlayerPrefsProgressRepository(catalog);
            _s = GameServices.Create(_repo, new SystemTimeProvider(), catalog);
            _renderer.ReduceMotion = false;

            BuildCamera();
            BuildEventSystem();
            BuildCanvas();
            _audio = gameObject.AddComponent<SynthAudio>();
            _audio.Muted = _repo.Get().Muted;
            _input.IsBlocked = pos => _mode == Mode.Run && RectTransformUtility.RectangleContainsScreenPoint(_hud.PauseButtonRect, pos, null);

            Layout(force: true);
            ToTitle();
            _trainY = _height - 92;
        }

        private void BuildCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Kamera");
            go.transform.SetParent(transform, false);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ui.Night;
            cam.orthographic = true;
            cam.cullingMask = 0;
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        private void BuildEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildCanvas()
        {
            var go = new GameObject("Arayüz");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            _scaler = go.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            go.AddComponent<GraphicRaycaster>();

            _stage = Ui.Node("Sahne", go.transform);
            _stage.anchorMin = _stage.anchorMax = Vector2.zero;
            _stage.pivot = new Vector2(0.5f, 0.5f);
            var mask = _stage.gameObject.AddComponent<RectMask2D>();
            mask.enabled = true;

            _display = Ui.Node("Oyun", _stage).Fill().gameObject.AddComponent<RawImage>();
            _display.uvRect = PixelTexture.FlippedUv;
            _display.raycastTarget = false;

            _hud = new HudView(_stage, Pause);
            _title = new TitleView(_stage, () => { _audio.Play(Sfx.Click); ToMap(); });
            _map = new MapView(_stage, _s, StartRun, CollectIdle, ToggleMute, OnGarageChanged);
            _pause = new PauseView(_stage, Resume, Quit);
            _result = new ResultView(_stage, _s);
        }

        /// <summary>Ekranı güvenli alana sığdırır; oyun genişliği 160 piksel, yükseklik telefona göre 270-356.</summary>
        private void Layout(bool force)
        {
            var safe = Screen.safeArea;
            if (!force && safe == _safeArea) return;
            _safeArea = safe;
            float u = safe.width / Width;
            int h = Mathf.RoundToInt(safe.height / u);
            if (h < MinHeight) { h = MinHeight; u = safe.height / h; }
            if (h > MaxHeight) { h = MaxHeight; u = Mathf.Min(safe.width / Width, safe.height / h); }
            _pixelScale = u;
            _input.PixelScale = u;
            _scaler.scaleFactor = u / Ui.K;
            _stage.sizeDelta = new Vector2(Width * Ui.K, h * Ui.K);
            _stage.anchoredPosition = safe.center / _scaler.scaleFactor;

            if (h != _height || _screen == null)
            {
                _height = h;
                _screen = new PixelImage(Width, h);
                if (_texture != null) Destroy(_texture);
                _texture = PixelTexture.Create(Width, h);
                _display.texture = _texture;
            }
        }

        // ================================================================ ekranlar

        private void Show(Mode mode)
        {
            _mode = mode;
            _hud.Visible = mode == Mode.Run || mode == Mode.Pause;
            _title.Visible = mode == Mode.Title;
            _map.Visible = mode == Mode.Map;
            _pause.Visible = mode == Mode.Pause;
            _result.Visible = mode == Mode.Result;
            _input.Reset();
        }

        private void EnsureAttract()
        {
            if (_run != null && _run.IsAttract) return;
            int route = Mathf.Min(_repo.Get().UnlockedRoute, _s.Catalog.Routes.Count - 1);
            _run = _s.Runs.StartRun(route, true);
            _view = new RunView(_run, _s);
        }

        private void ToTitle()
        {
            EnsureAttract();
            Show(Mode.Title);
        }

        private void ToMap()
        {
            EnsureAttract();
            _map.Rebuild();
            _map.SelectTab(false);
            Show(Mode.Map);
        }

        private void StartRun(int route)
        {
            _audio.Play(Sfx.Click);
            _run = _s.Runs.StartRun(route, false);
            _view = new RunView(_run, _s);
            _lastY = _run.Y;
            _chuffDistance = 0f;
            _resultDelay = -1f;
            _hud.ResetCache();
            Show(Mode.Run);
            var c = _s.Catalog;
            _hud.Banner(Texts.Upper(c.Biome(_run.Route.Biome).Name), c.Cities[route].Name + " → " + c.Cities[route + 1].Name + (_run.Raining ? " · yağmurlu" : ""));
            bool tutorial = !_repo.Get().SeenTutorial;
            _tipIndex = tutorial ? 0 : Texts.Tips.Length;
            _tipTimer = 0f;
            _hud.ShowTip(null);
            _hintTimer = tutorial ? 4.5f : 0f;
            _hud.ShowHint(tutorial);
        }

        private void Pause()
        {
            if (_mode != Mode.Run) return;
            _audio.Play(Sfx.Click);
            Show(Mode.Pause);
        }

        private void Resume()
        {
            _audio.Play(Sfx.Click);
            Show(Mode.Run);
        }

        private void Quit()
        {
            _audio.Play(Sfx.Click);
            _s.Runs.Abandon(_run);
            _run = null;
            ToMap();
        }

        private void ShowResult()
        {
            _resultDelay = -1f;
            _hud.ShowTip(null);
            _hud.ShowHint(false);
            var finished = _run;
            _result.Show(finished,
                () => { _audio.Play(Sfx.Click); _run = null; ToMap(); },
                () => StartRun(finished.RouteIndex));
            Show(Mode.Result);
        }

        private void CollectIdle()
        {
            if (_s.Economy.CollectIdle() > 0) _audio.Play(Sfx.Cash);
            _map.RefreshCounters();
        }

        private void ToggleMute()
        {
            var p = _repo.Get();
            p.Muted = !p.Muted;
            _repo.Save();
            _audio.Muted = p.Muted;
            _audio.Play(Sfx.Click);
            _map.RefreshCounters();
        }

        private void OnGarageChanged()
        {
            _audio.Play(Sfx.Cash);
            _run = null;
            EnsureAttract(); // yeni lokomotif vitrinde görünsün
            _map.Rebuild();
            _map.SelectTab(true);
        }

        // ================================================================ kare döngüsü

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxDelta);
            Layout(force: false);

            switch (_mode)
            {
                case Mode.Title:
                case Mode.Map:
                    Step(dt);
                    if (_mode == Mode.Map && (_counterTimer += dt) >= 1f) { _counterTimer = 0f; _map.RefreshCounters(); }
                    break;
                case Mode.Run:
                    HandleGesture(_input.Poll());
                    Step(dt);
                    UpdateTutorial(dt);
                    if (_resultDelay >= 0f && (_resultDelay -= dt) < 0f) ShowResult();
                    break;
                case Mode.Pause:
                    if (_input.Poll() == Gesture.Pause) Resume();
                    break;
                case Mode.Result:
                    _view.Update(dt);
                    int star = _result.Tick(dt);
                    if (star >= 0) _audio.Play(star == 0 ? Sfx.Star0 : star == 1 ? Sfx.Star1 : Sfx.Star2);
                    break;
            }

            _hud.Tick(dt);
            if (_mode == Mode.Run || _mode == Mode.Pause) _hud.Refresh(_run, _s);

            float target = _mode == Mode.Map ? Mathf.Round(_height * 0.34f) : _height - 92;
            _trainY += (target - _trainY) * Mathf.Min(1f, dt * 4f);
            if (_view != null)
            {
                _renderer.Render(_screen, _view, Mathf.RoundToInt(_trainY));
                PixelTexture.Upload(_texture, _screen);
            }
        }

        private void Step(float dt)
        {
            _s.Runs.Tick(_run, dt);
            ProcessEvents();
            _view.Update(dt);
        }

        private void HandleGesture(Gesture g)
        {
            if (g == Gesture.None) return;
            _hud.ShowHint(false);
            _hintTimer = 0f;
            switch (g)
            {
                case Gesture.Left: _s.Runs.Steer(_run, -1); break;
                case Gesture.Right: _s.Runs.Steer(_run, 1); break;
                case Gesture.Down: _s.Runs.Horn(_run); break;
                case Gesture.Up: _s.Runs.Turbo(_run); break;
                case Gesture.Pause: Pause(); return;
            }
            ProcessEvents();
        }

        /// <summary>Kuralların ürettiği olayları sese, yazıya ve duyuruya çevirir; sonra listeyi boşaltır.</summary>
        private void ProcessEvents()
        {
            if (_run == null) return;
            _view.HandleEvents();
            if (!_run.IsAttract)
            {
                foreach (var e in _run.Events) OnRunEvent(e);
                if (_s.Catalog.Locomotives[_run.LocomotiveIndex].SpriteKey == "steam")
                {
                    _chuffDistance += _run.Y - _lastY;
                    if (_chuffDistance > 26f) { _chuffDistance = 0f; _audio.Play(Sfx.Chuff); }
                }
                _lastY = _run.Y;
            }
            _run.Events.Clear();
        }

        private void OnRunEvent(RunEvent e)
        {
            var c = _s.Catalog;
            switch (e.Type)
            {
                case RunEventType.SwitchChanged: _audio.Play(Sfx.Click); break;
                case RunEventType.SwitchBlocked: _audio.Play(Sfx.Full); break;
                case RunEventType.JunctionPassed: _audio.Play(Sfx.Clack); break;
                case RunEventType.Coin: _audio.Play(Sfx.Coin); break;
                case RunEventType.Pickup: _audio.Play(Sfx.Pickup); break;
                case RunEventType.WagonsFull:
                case RunEventType.NothingToDeliver: _audio.Play(Sfx.Full); break;
                case RunEventType.Delivery: _audio.Play(Sfx.Cash); break;
                case RunEventType.Refuel: _audio.Play(Sfx.Fuel); break;
                case RunEventType.Horn: _audio.Play(Sfx.Horn); break;
                case RunEventType.Turbo: _audio.Play(Sfx.Turbo); break;
                case RunEventType.StreakUp: _audio.Play(Sfx.Star1); break;
                case RunEventType.OncomingWarning: _audio.Play(Sfx.Warn); break;
                case RunEventType.Crash:
                case RunEventType.OncomingCrash:
                    _audio.Play(Sfx.Crash);
#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
#endif
                    break;
                case RunEventType.FuelLow: _hud.Banner("YAKIT AZ!", "Kırmızı perona yönel"); break;
                case RunEventType.OutOfFuel: _hud.Banner("YAKIT BİTTİ", "Tren duruyor…"); break;
                case RunEventType.NearDestination:
                    _hud.Banner(Texts.Upper(c.Cities[_run.RouteIndex + 1].Name) + " GÖRÜNDÜ", "Varışta tüm yük ×1,5 değerinde");
                    break;
                case RunEventType.NightFell: _hud.Banner("GECE ÇÖKTÜ", "Yolcu ücreti ×1,5 · farın tek ışık"); break;
                case RunEventType.Finished:
                    if (_run.Result != null && _run.Result.Arrived && _run.FinalPay > 0) _audio.Play(Sfx.Cash);
                    _resultDelay = _run.Result != null && _run.Result.Arrived ? 0.9f : 0.3f;
                    break;
            }
        }

        private void UpdateTutorial(float dt)
        {
            if (_hintTimer > 0f && (_hintTimer -= dt) <= 0f) _hud.ShowHint(false);
            if (_tipIndex > Texts.Tips.Length) return;
            _tipTimer -= dt;
            if (_tipTimer > 0f) return;
            if (_tipIndex < Texts.Tips.Length)
            {
                _hud.ShowTip(Texts.Tips[_tipIndex++]);
                _tipTimer = 5.5f;
            }
            else
            {
                _hud.ShowTip(null);
                _tipIndex++;
            }
        }

        // ================================================================ yaşam döngüsü

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            _repo.Save();
            if (_mode == Mode.Run) Show(Mode.Pause);
        }

        private void OnApplicationQuit() => _repo.Save();
    }
}
