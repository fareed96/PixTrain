using System.Text;
using PixTrain.Core;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Services;
using PixTrain.Render;
using UnityEngine;
using UnityEngine.UI;

namespace PixTrain.Game.UI
{
    /// <summary>Sefer sırasındaki gösterge paneli. Değerler yalnızca değiştiğinde yazılır (çöp üretmemek için).</summary>
    public sealed class HudView
    {
        public RectTransform Root;
        public RectTransform PauseButtonRect;

        private Text _coins, _destination, _km, _clock, _passengers, _coal, _food, _freightCap, _streak, _tip, _bannerTitle, _bannerSub;
        private RectTransform _progressFill, _progressMarker, _fuelFill, _hornFill, _turboFill;
        private Image _fuelImage, _passengerChip, _freightChip;
        private RawImage _clockIcon;
        private Image _hornBox, _turboBox;
        private Text _hornText, _turboText;
        private RectTransform _contracts, _bannerRoot, _tipRoot, _hintRoot, _streakRoot;
        private CanvasGroup _bannerGroup;
        private float _bannerTime = -1f;
        private string _contractKey;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly System.Collections.Generic.Dictionary<Object, string> _textCache = new System.Collections.Generic.Dictionary<Object, string>();

        public HudView(Transform stage, UnityEngine.Events.UnityAction onPause)
        {
            Root = Ui.Node("HUD", stage).Fill();

            // ---- üst bant
            var fade = Ui.FadeDown(Root, "üst-gölge");
            fade.rectTransform.Band(true, 0, 0, 0, 42);

            var row1 = Ui.Row(Root, "satır-1", 2, TextAnchor.MiddleLeft);
            row1.GetComponent<RectTransform>().Band(true, 2, 2, 2, 11);
            var coinRow = Ui.Row(row1.transform, "altın", 1, TextAnchor.MiddleLeft);
            Ui.Icon(coinRow.transform, "coin", 7);
            _coins = Ui.Label(coinRow.transform, "0", 6, Ui.Gold2, TextAnchor.MiddleLeft, bold: true);
            coinRow.Size(34, 11);

            var prog = Ui.Column(row1.transform, "ilerleme", 0.8f);
            prog.Size(-1, 11, flexW: 1);
            var labels = Ui.Row(prog.transform, "etiketler", 1);
            labels.Size(-1, 5);
            _destination = Ui.Label(labels.transform, "", 4.6f, Ui.Ink2);
            _destination.Size(flexW: 1);
            _km = Ui.Label(labels.transform, "", 4.6f, Ui.Ink2, TextAnchor.MiddleRight);
            _km.Size(30);
            var bar = Ui.Box(prog.transform, "çubuk", Ui.Night, Ui.Panel2, 0.6f);
            bar.Size(-1, 2.5f);
            _progressFill = Ui.Box(bar.transform, "dolu", Ui.Green).rectTransform.Anchor(0, 0, 0, 1);
            _progressMarker = Ui.Box(bar.transform, "tren", Ui.Gold2, Ui.Night, 0.6f).rectTransform;
            _progressMarker.anchorMin = _progressMarker.anchorMax = new Vector2(0, 0.5f);
            _progressMarker.sizeDelta = new Vector2(3 * Ui.K, 5.5f * Ui.K);

            var pause = Ui.Button(row1.transform, "II", onPause, ButtonKind.Ghost, 5);
            pause.Size(11, 11);
            PauseButtonRect = (RectTransform)pause.transform;

            var row2 = Ui.Row(Root, "satır-2", 2, TextAnchor.MiddleLeft);
            row2.GetComponent<RectTransform>().Band(true, 2, 2, 15, 7);
            var fuelRow = Ui.Row(row2.transform, "yakıt", 1, TextAnchor.MiddleLeft);
            fuelRow.Size(40, 7);
            Ui.Icon(fuelRow.transform, "fuel", 5);
            var fuelBar = Ui.Box(fuelRow.transform, "yakıt-çubuk", Ui.Night, Ui.Panel2, 0.6f);
            fuelBar.Size(-1, 3.5f, flexW: 1);
            _fuelImage = Ui.Box(fuelBar.transform, "dolu", Ui.Green);
            _fuelFill = _fuelImage.rectTransform.Anchor(0, 0, 1, 1);

            var chips = Ui.Row(row2.transform, "yük", 1, TextAnchor.MiddleCenter);
            chips.Size(-1, 7, flexW: 1);
            var c1 = Ui.Chip(chips.transform, "yolcu", Ui.Panel2);
            _passengerChip = c1.GetComponent<Image>();
            Ui.Icon(c1.transform, "person", 5);
            _passengers = Ui.Label(c1.transform, "0/0", 4.8f, Ui.Ink);
            var c2 = Ui.Chip(chips.transform, "yük", Ui.Panel2);
            _freightChip = c2.GetComponent<Image>();
            Ui.Icon(c2.transform, "coal", 5);
            _coal = Ui.Label(c2.transform, "0", 4.8f, Ui.Ink);
            Ui.Icon(c2.transform, "wheat", 5);
            _food = Ui.Label(c2.transform, "0", 4.8f, Ui.Ink);
            _freightCap = Ui.Label(c2.transform, "/0", 4.8f, Ui.Mute);

            var clockRow = Ui.Row(row2.transform, "saat", 1, TextAnchor.MiddleRight);
            clockRow.Size(32, 7);
            _clockIcon = Ui.Icon(clockRow.transform, "sun", 5);
            _clock = Ui.Label(clockRow.transform, "GÜNDÜZ", 4.6f, Ui.Ink);

            _contracts = Ui.Row(Root, "görevler", 1, TextAnchor.MiddleCenter).GetComponent<RectTransform>().Band(true, 2, 2, 24, 7);

            // ---- alt göstergeler
            _hornBox = CooldownBox("DÜDÜK", -1, out _hornFill, out _hornText);
            _hornBox.rectTransform.Pin(0, 0, 3, 3, 22, 17);
            _turboBox = CooldownBox("TURBO", 1, out _turboFill, out _turboText);
            _turboBox.rectTransform.Pin(1, 0, -3, 3, 22, 17);
            var streak = Ui.Chip(Root, "seri", Ui.Gold);
            _streakRoot = (RectTransform)streak.transform;
            _streakRoot.Pin(0.5f, 0, 0, 5, 44, 8);
            _streak = Ui.Label(streak.transform, "", 5, Ui.Gold2, TextAnchor.MiddleCenter, bold: true);

            // ---- ipucu, duyuru, hareket ipucu
            var tip = Ui.Box(Root, "ipucu", Ui.Night2, Ui.Gold, 0.6f);
            _tipRoot = tip.rectTransform.Band(false, 27, 27, 24, 18);
            _tip = Ui.Label(tip.transform, "", 4.8f, Ui.Ink, TextAnchor.MiddleCenter);
            _tip.rectTransform.Fill(2, 1, 2, 1);
            _tipRoot.gameObject.SetActive(false);

            var banner = Ui.Box(Root, "duyuru", Ui.Night2, Ui.Gold, 1f);
            _bannerRoot = banner.rectTransform.Pin(0.5f, 0.66f, 0, 0, 120, 22);
            _bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            _bannerGroup.blocksRaycasts = false;
            _bannerTitle = Ui.Label(banner.transform, "", 8, Ui.Gold2, TextAnchor.MiddleCenter, bold: true);
            _bannerTitle.rectTransform.Anchor(0, 0.45f, 1, 1);
            _bannerSub = Ui.Label(banner.transform, "", 4.6f, Ui.Ink, TextAnchor.MiddleCenter);
            _bannerSub.rectTransform.Anchor(0, 0, 1, 0.48f, 0, 1.5f, 0, 0);
            _bannerRoot.gameObject.SetActive(false);

            var hint = Ui.Box(Root, "hareketler", Ui.Night2, Ui.Gold, 1f);
            _hintRoot = hint.rectTransform.Pin(0.5f, 0.5f, 0, 0, 104, 52);
            var hintCol = Ui.Column(hint.transform, "liste", 3, TextAnchor.MiddleLeft, 5);
            hintCol.GetComponent<RectTransform>().Fill();
            HintRow(hintCol.transform, new[] { -1, 1 }, "Kaydır: makas");
            HintRow(hintCol.transform, new[] { 2 }, "Aşağı çek: düt düt");
            HintRow(hintCol.transform, new[] { 0 }, "Yukarı it: turbo");
            _hintRoot.gameObject.SetActive(false);
        }

        public bool Visible { set => Root.gameObject.SetActive(value); }

        private Image CooldownBox(string label, int side, out RectTransform fill, out Text text)
        {
            var box = Ui.Box(Root, label, Ui.Night2, Ui.Gold, 0.6f);
            var arrow = Ui.Picture(box.transform, "ok", GestureArt.Arrow(label == "DÜDÜK" ? 2 : 0), 1f);
            arrow.rectTransform.Pin(0.5f, 1, 0, -2, 7, 7);
            text = Ui.Label(box.transform, label, 4f, Ui.Gold2, TextAnchor.MiddleCenter, bold: true);
            text.rectTransform.Band(false, 0, 0, 2.5f, 6);
            var bar = Ui.Box(box.transform, "dolum", Ui.Gold);
            fill = bar.rectTransform.Band(false, 0, 0, 0, 1);
            return box;
        }

        private static void HintRow(Transform parent, int[] arrows, string text)
        {
            var row = Ui.Row(parent, "satır", 3, TextAnchor.MiddleLeft);
            row.Size(-1, 11);
            var icons = Ui.Row(row.transform, "oklar", 0, TextAnchor.MiddleCenter);
            icons.Size(18, 11);
            foreach (int a in arrows) Ui.Picture(icons.transform, "ok", GestureArt.Arrow(a), 1f).Size(8, 8);
            Ui.Label(row.transform, text, 5.4f, Ui.Ink).Size(flexW: 1);
        }

        // ---------------------------------------------------------------- güncelleme

        public void ShowHint(bool on) => _hintRoot.gameObject.SetActive(on);

        public void ShowTip(string text)
        {
            _tipRoot.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) _tip.text = text;
        }

        public void Banner(string title, string sub)
        {
            _bannerTitle.text = title;
            _bannerSub.text = sub ?? "";
            _bannerTime = 0f;
            _bannerRoot.gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            if (_bannerTime < 0f) return;
            _bannerTime += dt;
            const float life = 2.4f;
            float a = _bannerTime < 0.25f ? _bannerTime / 0.25f : _bannerTime > life * 0.8f ? Mathf.Max(0f, (life - _bannerTime) / (life * 0.2f)) : 1f;
            _bannerGroup.alpha = Mathf.Round(a * 6f) / 6f;
            float rise = _bannerTime < 0.25f ? (1f - _bannerTime / 0.25f) * 6f : 0f;
            _bannerRoot.anchoredPosition = new Vector2(0, -rise * Ui.K);
            if (_bannerTime >= life) { _bannerTime = -1f; _bannerRoot.gameObject.SetActive(false); }
        }

        public void Refresh(RunState run, GameServices s)
        {
            Set(_coins, Texts.Number(s.Economy.Coins));
            float progress = Mathf.Clamp01(run.Y / run.Length);
            Set(_destination, "Varış: " + s.Catalog.Cities[run.RouteIndex + 1].Name);
            Set(_km, Texts.Decimal1(Mathf.Max(0f, run.Length - run.Y) / 100f) + " km");
            _progressFill.anchorMax = new Vector2(progress, 1f);
            _progressMarker.anchorMin = _progressMarker.anchorMax = new Vector2(progress, 0.5f);

            float fuel = run.Fuel / run.FuelMax;
            _fuelFill.anchorMax = new Vector2(fuel, 1f);
            bool low = fuel < 0.25f;
            _fuelImage.color = low ? (Mathf.FloorToInt(Time.unscaledTime * 4f) % 2 == 0 ? Ui.Red : new Color(Ui.Red.r, Ui.Red.g, Ui.Red.b, 0.35f)) : fuel < 0.5f ? Ui.Gold : Ui.Green;

            var cargo = s.Cargo.Summarize(run);
            Set(_passengers, cargo.Passengers + "/" + cargo.PassengerCapacity);
            Set(_coal, cargo.Coal.ToString());
            Set(_food, cargo.Food.ToString());
            Set(_freightCap, "/" + cargo.FreightCapacity);
            SetBorder(_passengerChip, cargo.Passengers >= cargo.PassengerCapacity ? Ui.Red : Ui.Panel2);
            SetBorder(_freightChip, cargo.Coal + cargo.Food >= cargo.FreightCapacity ? Ui.Red : Ui.Panel2);

            var phase = DayCycle.Phase(run.TimeOfDay);
            string clock = phase == DayPhase.Night ? "GECE ×1,5" : phase == DayPhase.Dawn ? "ŞAFAK" : phase == DayPhase.Dusk ? "AKŞAM" : "GÜNDÜZ";
            if (Set(_clock, clock))
            {
                _clock.color = phase == DayPhase.Night ? Ui.Hex("9fd8ff") : Ui.Ink;
                Ui.SetPicture(_clockIcon, SpriteLibrary.Instance.Icons[DayCycle.Darkness(run.TimeOfDay) >= 0.5f ? "moon" : "sun"]);
            }

            RefreshContracts(run, s);

            bool hornWait = run.HornCooldown > 0f;
            _hornFill.anchorMax = new Vector2(hornWait ? 1f - run.HornCooldown / s.Catalog.Balance.HornCooldown : 1f, 0f);
            _hornBox.color = Ui.Night2;
            _hornText.color = hornWait ? Ui.Mute : Ui.Gold2;
            bool turboLive = run.IsTurbo, turboWait = run.TurboCooldown > 0f && !turboLive;
            float turboAmount = turboLive ? run.TurboTimer / s.Catalog.Balance.TurboDuration : turboWait ? 1f - run.TurboCooldown / s.Catalog.Balance.TurboCooldown : 1f;
            _turboFill.anchorMax = new Vector2(turboAmount, 0f);
            _turboBox.color = turboLive ? Ui.Gold : Ui.Night2;
            _turboText.color = turboLive ? Ui.Night : turboWait ? Ui.Mute : Ui.Gold2;

            bool showStreak = run.Streak >= 2;
            _streakRoot.gameObject.SetActive(showStreak);
            if (showStreak) Set(_streak, "SERİ ×" + Texts.Decimal2(s.Economy.StreakMultiplier(run.Streak)));
        }

        private void RefreshContracts(RunState run, GameServices s)
        {
            _sb.Clear();
            foreach (var c in run.Contracts) _sb.Append(s.Contracts.CurrentValue(run, c.Def.Metric)).Append('|').Append(run.Crashes).Append(';');
            string key = _sb.ToString();
            if (key == _contractKey) return;
            _contractKey = key;
            Ui.Clear(_contracts);
            foreach (var c in run.Contracts)
            {
                bool ok = s.Contracts.IsReached(run, c);
                int value = s.Contracts.CurrentValue(run, c.Def.Metric);
                string progress = c.Def.EndOnly ? (run.Crashes > 0 ? "kaza var" : "şimdilik temiz") : Mathf.Min(value, c.Target) + "/" + c.Target;
                var chip = Ui.Chip(_contracts, "görev", ok ? Ui.Green : Ui.Panel2);
                Ui.Icon(chip.transform, ok ? "star" : "starOff", 5);
                Ui.Label(chip.transform, c.Title + " · " + progress, 4.4f, ok ? Ui.Green : Ui.Ink);
            }
        }

        private bool Set(Text t, string value)
        {
            if (_textCache.TryGetValue(t, out var old) && old == value) return false;
            _textCache[t] = value;
            t.text = value;
            return true;
        }

        private static void SetBorder(Graphic g, Color c)
        {
            var o = g.GetComponent<Outline>();
            if (o != null && o.effectColor != c) o.effectColor = c;
        }

        public void ResetCache()
        {
            _textCache.Clear();
            _contractKey = null;
        }
    }
}
