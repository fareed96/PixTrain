using System;
using PixTrain.Core;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Render;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixTrain.Game.UI
{
    /// <summary>Menülerde ortak yardımcılar.</summary>
    internal static class MenuKit
    {
        public static Image Card(Transform parent, string name, bool highlight = false)
        {
            if (!parent.TryGetComponent<Image>(out var img)) img = parent.gameObject.AddComponent<Image>();
            img.color = Ui.Night;
            img.raycastTarget = false;
            Ui.AddBorder(img, highlight ? Ui.Gold : Ui.Panel2, 0.6f);
            return img;
        }

        /// <summary>İkon + yazılı düğme (fiyat, yıldız şartı).</summary>
        public static Button IconButton(Transform parent, string icon, string label, UnityAction onClick, ButtonKind kind = ButtonKind.Gold, float size = 4.6f)
        {
            var b = Ui.Button(parent, "", onClick, kind, size);
            var oldText = b.GetComponentInChildren<Text>();
            UnityEngine.Object.Destroy(oldText.gameObject);
            var row = Ui.Row(b.transform, "içerik", 1, TextAnchor.MiddleCenter);
            row.GetComponent<RectTransform>().Fill(1, 0, 1, 0);
            if (icon != null) Ui.Icon(row.transform, icon, 5);
            Ui.Label(row.transform, label, size, kind == ButtonKind.Gold ? Ui.Night : Ui.Ink, TextAnchor.MiddleCenter, bold: true);
            return b;
        }

        public static RectTransform Scroll(Transform parent, out RectTransform content)
        {
            var root = Ui.Node("kaydırma", parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            var viewport = Ui.Node("görünüm", root).Fill();
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var column = Ui.Column(viewport, "içerik", 1.5f, TextAnchor.UpperLeft, 3);
            content = column.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            Ui.FitHeight(column);
            scroll.viewport = viewport;
            scroll.content = content;
            return root;
        }
    }

    // ==================================================================== başlık

    public sealed class TitleView
    {
        public readonly RectTransform Root;

        public TitleView(Transform stage, UnityAction onPlay)
        {
            Root = Ui.Node("Başlık", stage).Fill();
            var scrim = Ui.FadeDown(Root, "karartma");
            scrim.rectTransform.Fill();
            scrim.uvRect = new Rect(0, 1, 1, -1);
            scrim.color = new Color(1, 1, 1, 0.9f);

            var logo = Ui.Picture(Root, "logo", UiArt.Logo(), 1f);
            logo.rectTransform.Pin(0.5f, 1f, 0, -24, 102, 26);
            var tag = Ui.Label(Root, Texts.Tagline, 6.5f, Ui.Ink, TextAnchor.MiddleCenter);
            tag.rectTransform.Band(true, 8, 8, 54, 10);
            var shadow = tag.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Ui.Night;
            shadow.effectDistance = new Vector2(Ui.K, -Ui.K);

            var play = Ui.Button(Root, Texts.Play, onPlay, ButtonKind.Gold, 8);
            ((RectTransform)play.transform).Pin(0.5f, 0f, 0, 48, 70, 16);
            var hint = Ui.Label(Root, Texts.TitleHint, 4.6f, Ui.Ink2, TextAnchor.UpperCenter);
            hint.rectTransform.Band(false, 14, 14, 10, 32);
            var hs = hint.gameObject.AddComponent<Shadow>();
            hs.effectColor = Ui.Night;
            hs.effectDistance = new Vector2(0.6f * Ui.K, -0.6f * Ui.K);
        }

        public bool Visible { set => Root.gameObject.SetActive(value); }
    }

    // ==================================================================== hat + garaj

    public sealed class MapView
    {
        public readonly RectTransform Root;
        private readonly GameServices _s;
        private readonly Action<int> _onStartRoute;
        private readonly Action _onChanged;
        private readonly Text _coins, _stars, _rate, _pending;
        private readonly Button _collect, _mute;
        private readonly Button _tabLine, _tabGarage;
        private readonly RectTransform _lineScroll, _lineContent, _garageScroll, _garageContent;
        private bool _garage;

        public MapView(Transform stage, GameServices services, Action<int> onStartRoute, Action onCollect, Action onMute, Action onChanged)
        {
            _s = services;
            _onStartRoute = onStartRoute;
            _onChanged = onChanged;
            Root = Ui.Node("Hat", stage).Fill();

            var head = Ui.Row(Root, "başlık", 2, TextAnchor.MiddleLeft, 0);
            head.GetComponent<RectTransform>().Band(true, 3, 3, 3, 11);
            var coinPill = Ui.Chip(head.transform, "altın", Ui.Panel2);
            Ui.Icon(coinPill.transform, "coin", 5);
            _coins = Ui.Label(coinPill.transform, "0", 5.5f, Ui.Gold2, TextAnchor.MiddleLeft, bold: true);
            var starPill = Ui.Chip(head.transform, "yıldız", Ui.Panel2);
            Ui.Icon(starPill.transform, "star", 5);
            _stars = Ui.Label(starPill.transform, "0", 5.5f, Ui.Ink, TextAnchor.MiddleLeft, bold: true);
            Ui.Node("boşluk", head.transform).Size(flexW: 1);
            _mute = Ui.Button(head.transform, "SES: AÇIK", () => onMute(), ButtonKind.Ghost, 4.4f);
            _mute.Size(40, 10);

            var sheet = Ui.Box(Root, "panel", Ui.Panel, Ui.Night, 0.6f);
            sheet.rectTransform.Anchor(0, 0, 1, 0.58f);
            var top = Ui.Box(sheet.transform, "çizgi", Ui.Line);
            top.rectTransform.Band(true, 0, 0, 0, 0.6f);

            var idle = Ui.Row(sheet.transform, "hat-geliri", 2.5f, TextAnchor.MiddleLeft, 2.5f);
            idle.GetComponent<RectTransform>().Band(true, 3, 3, 3, 20);
            MenuKit.Card(idle.transform, "kart");
            Ui.Icon(idle.transform, "train", 7);
            var idleText = Ui.Column(idle.transform, "yazı", 0.6f, TextAnchor.MiddleLeft);
            idleText.Size(flexW: 1);
            Ui.Label(idleText.transform, "Hat geliri · seferdeyken bile işler", 4.4f, Ui.Mute);
            var rateRow = Ui.Row(idleText.transform, "değer", 0, TextAnchor.MiddleLeft);
            _rate = Ui.Label(rateRow.transform, "0/dk", 5.5f, Ui.Ink, TextAnchor.MiddleLeft, bold: true);
            Ui.Label(rateRow.transform, " · biriken ", 5.5f, Ui.Ink);
            _pending = Ui.Label(rateRow.transform, "0", 5.5f, Ui.Gold2, TextAnchor.MiddleLeft, bold: true);
            _collect = Ui.Button(idle.transform, "TOPLA", () => onCollect(), ButtonKind.Gold, 5);
            _collect.Size(30, 12);

            var tabs = Ui.Row(sheet.transform, "sekmeler", 2, TextAnchor.MiddleCenter);
            tabs.GetComponent<RectTransform>().Band(true, 3, 3, 26, 11);
            tabs.childForceExpandWidth = true;
            _tabLine = Ui.Button(tabs.transform, "HAT", () => SelectTab(false), ButtonKind.Ghost, 5.5f);
            _tabLine.Size(-1, 11, flexW: 1);
            _tabGarage = Ui.Button(tabs.transform, "GARAJ", () => SelectTab(true), ButtonKind.Ghost, 5.5f);
            _tabGarage.Size(-1, 11, flexW: 1);

            _lineScroll = MenuKit.Scroll(sheet.transform, out _lineContent);
            _lineScroll.Fill(0, 0, 0, 39);
            _garageScroll = MenuKit.Scroll(sheet.transform, out _garageContent);
            _garageScroll.Fill(0, 0, 0, 39);
            SelectTab(false);
        }

        public bool Visible { set => Root.gameObject.SetActive(value); }

        public void SelectTab(bool garage)
        {
            _garage = garage;
            _lineScroll.gameObject.SetActive(!garage);
            _garageScroll.gameObject.SetActive(garage);
            StyleTab(_tabLine, !garage);
            StyleTab(_tabGarage, garage);
        }

        private static void StyleTab(Button b, bool selected)
        {
            b.GetComponent<Image>().color = selected ? Ui.Ink : Ui.Night;
            b.GetComponentInChildren<Text>().color = selected ? Ui.Night : Ui.Mute;
        }

        /// <summary>Saniyede bir: altın, hat geliri.</summary>
        public void RefreshCounters()
        {
            var p = _s.Progress.Get();
            _coins.text = Texts.Number(_s.Economy.Coins);
            _stars.text = _s.Economy.TotalStars() + "/" + _s.Economy.MaxStars();
            _rate.text = _s.Economy.IdleRatePerMinute() + "/dk";
            int pending = _s.Economy.IdlePending();
            _pending.text = Texts.Number(pending);
            _collect.interactable = pending > 0;
            Ui.SetButton(_mute, p.Muted ? "SES: KAPALI" : "SES: AÇIK", true);
        }

        /// <summary>Listeleri baştan kurar (sefer, satın alma sonrası).</summary>
        public void Rebuild()
        {
            RefreshCounters();
            BuildLine();
            BuildGarage();
        }

        private void BuildLine()
        {
            Ui.Clear(_lineContent);
            var p = _s.Progress.Get();
            var catalog = _s.Catalog;
            for (int i = catalog.Cities.Count - 1; i >= 0; i--)
            {
                bool reached = i == 0 || p.CompletedRoutes[i - 1];
                var stop = Ui.Row(_lineContent, "durak", 3, TextAnchor.MiddleLeft);
                stop.Size(-1, 24);
                Ui.Box(stop.transform, "işaret", reached ? Ui.Gold : Ui.Panel2, Ui.Night, 0.6f).Size(5, 5);
                Ui.Picture(stop.transform, "silüet", UiArt.CitySkyline(_s.Economy.CityLevel(i), i), 1f);
                var names = Ui.Column(stop.transform, "ad", 0.6f, TextAnchor.MiddleLeft);
                names.Size(flexW: 1);
                Ui.Label(names.transform, catalog.Cities[i].Name, 6.5f, reached ? Ui.Ink : Ui.Mute, TextAnchor.MiddleLeft, bold: true);
                Ui.Label(names.transform, _s.Economy.CityLevelName(i) + " · " + (reached ? "bağlı" : "hat bekliyor"), 4.4f, Ui.Mute);

                if (i == 0) continue;
                int route = i - 1;
                var def = catalog.Routes[route];
                bool open = route <= p.UnlockedRoute;
                var leg = Ui.Row(_lineContent, "hat", 2.5f, TextAnchor.MiddleLeft, 2.5f);
                MenuKit.Card(leg.transform, "kart", route == p.UnlockedRoute);
                var col = Ui.Column(leg.transform, "bilgi", 0.8f, TextAnchor.MiddleLeft);
                col.Size(flexW: 1);
                Ui.Label(col.transform, catalog.Biome(def.Biome).Name + " · " + def.Kilometers + " km", 5.4f, open ? Ui.Ink : Ui.Mute, TextAnchor.MiddleLeft, bold: true);
                Ui.Label(col.transform, def.Description, 4.3f, Ui.Mute);
                var stars = Ui.Row(col.transform, "yıldızlar", 0.6f);
                for (int k = 0; k < 3; k++) Ui.Icon(stars.transform, k < p.BestStars[route] ? "star" : "starOff", 5);
                if (open)
                {
                    int r = route;
                    Ui.Button(leg.transform, "SEFER", () => _onStartRoute(r), ButtonKind.Gold, 4.8f).Size(28, 12);
                }
                else
                {
                    Ui.Icon(leg.transform, "lock", 7);
                }
            }
        }

        private void BuildGarage()
        {
            Ui.Clear(_garageContent);
            var p = _s.Progress.Get();
            var catalog = _s.Catalog;
            var garage = _s.Garage;

            var top = Ui.Row(_garageContent, "üst", 3, TextAnchor.UpperLeft);
            var spec = garage.BuildTrainSpec();
            Ui.Picture(top.transform, "tren", UiArt.TrainPreview(catalog.Locomotives[p.SelectedLocomotive].SpriteKey, spec.Wagons), 1f);
            var locos = Ui.Column(top.transform, "lokomotifler", 2);
            locos.Size(flexW: 1);
            Ui.Label(locos.transform, "LOKOMOTİF", 4.6f, Ui.Mute);
            for (int i = 0; i < catalog.Locomotives.Count; i++)
            {
                var def = catalog.Locomotives[i];
                var status = garage.GetLocomotiveStatus(i);
                var card = Ui.Row(locos.transform, "loko", 2, TextAnchor.MiddleLeft, 2);
                MenuKit.Card(card.transform, "kart", status == LocomotiveStatus.Selected);
                var info = Ui.Column(card.transform, "bilgi", 0.6f);
                info.Size(flexW: 1);
                Ui.Label(info.transform, def.Name, 5, Ui.Ink, TextAnchor.MiddleLeft, bold: true);
                Ui.Label(info.transform, def.TypeName + " · " + def.Description, 4.2f, Ui.Mute);
                int index = i;
                Button action;
                switch (status)
                {
                    case LocomotiveStatus.Selected:
                        action = Ui.Button(card.transform, "SEÇİLİ", null, ButtonKind.Ghost, 4.4f);
                        action.interactable = false;
                        break;
                    case LocomotiveStatus.Owned:
                        action = Ui.Button(card.transform, "SEÇ", () => { garage.SelectLocomotive(index); _onChanged(); }, ButtonKind.Ghost, 4.4f);
                        break;
                    case LocomotiveStatus.NeedsStars:
                        action = MenuKit.IconButton(card.transform, "star", def.StarsRequired.ToString(), null);
                        action.interactable = false;
                        break;
                    default:
                        action = MenuKit.IconButton(card.transform, "coin", Texts.Number(def.Cost), () => { if (garage.BuyLocomotive(index)) _onChanged(); });
                        action.interactable = status == LocomotiveStatus.Available;
                        break;
                }
                action.Size(30, 11);
            }

            Ui.Label(_garageContent, "GELİŞTİRMELER", 4.6f, Ui.Mute).Size(-1, 8);
            foreach (var up in catalog.Upgrades)
            {
                var id = up.Id;
                int level = garage.UpgradeLevel(id);
                var row = Ui.Row(_garageContent, "geliştirme", 2, TextAnchor.MiddleLeft, 2);
                MenuKit.Card(row.transform, "kart");
                var info = Ui.Column(row.transform, "bilgi", 0.6f);
                info.Size(flexW: 1);
                Ui.Label(info.transform, up.Name, 5, Ui.Ink, TextAnchor.MiddleLeft, bold: true);
                Ui.Label(info.transform, up.Description, 4.2f, Ui.Mute);
                var pips = Ui.Row(info.transform, "seviye", 0.8f);
                for (int k = 0; k < up.MaxLevel; k++) Ui.Box(pips.transform, "pip", k < level ? Ui.Green : Ui.Panel2).Size(3, 3);
                Button buy;
                if (garage.IsMaxed(id))
                {
                    buy = Ui.Button(row.transform, "MAKS", null, ButtonKind.Ghost, 4.4f);
                    buy.interactable = false;
                }
                else
                {
                    buy = MenuKit.IconButton(row.transform, "coin", Texts.Number(garage.NextUpgradeCost(id)), () => { if (garage.BuyUpgrade(id)) _onChanged(); });
                    buy.interactable = garage.CanBuyUpgrade(id);
                }
                buy.Size(30, 11);
            }
        }
    }

    // ==================================================================== mola

    public sealed class PauseView
    {
        public readonly RectTransform Root;

        public PauseView(Transform stage, UnityAction onResume, UnityAction onQuit)
        {
            Root = Ui.Node("Mola", stage).Fill();
            var dim = Root.gameObject.AddComponent<Image>();
            dim.color = new Color(Ui.Night.r, Ui.Night.g, Ui.Night.b, 0.6f);
            var card = Ui.Column(Root, "kart", 3, TextAnchor.UpperCenter, 5);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(148 * Ui.K, 0);
            Ui.FitHeight(card);
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = Ui.Panel;
            Ui.AddBorder(bg, Ui.Line, 1f);

            Ui.Label(card.transform, "Mola", 9, Ui.Gold2, TextAnchor.MiddleCenter, bold: true).Size(-1, 12);
            GestureRow(card.transform, new[] { -1, 1 }, "Kaydır: sıradaki makas");
            GestureRow(card.transform, new[] { 2 }, "Aşağı çek: düdük (inekler kaçar)");
            GestureRow(card.transform, new[] { 0 }, "Yukarı it: turbo (altın ×2, yakıt ×2)");
            foreach (var item in Texts.Legend)
            {
                var row = Ui.Row(card.transform, "renk", 2.5f, TextAnchor.MiddleLeft);
                row.Size(-1, 6);
                Ui.Box(row.transform, "renk", Ui.Hex(item[0]), Ui.Night, 0.6f).Size(8, 5);
                Ui.Label(row.transform, item[1], 4.6f, Ui.Ink).Size(flexW: 1);
            }
            var acts = Ui.Row(card.transform, "düğmeler", 3, TextAnchor.MiddleCenter);
            acts.Size(-1, 14);
            Ui.Button(acts.transform, "DEVAM", onResume, ButtonKind.Gold, 5.5f).Size(40, 13);
            Ui.Button(acts.transform, "SEFERİ BIRAK", onQuit, ButtonKind.Ghost, 5.5f).Size(60, 13);
        }

        private static void GestureRow(Transform parent, int[] arrows, string text)
        {
            var row = Ui.Row(parent, "hareket", 2.5f, TextAnchor.MiddleLeft);
            row.Size(-1, 8);
            var icons = Ui.Row(row.transform, "oklar", 0, TextAnchor.MiddleCenter);
            icons.Size(16, 8);
            foreach (int a in arrows) Ui.Picture(icons.transform, "ok", GestureArt.Arrow(a), 1f).Size(7, 7);
            Ui.Label(row.transform, text, 4.6f, Ui.Ink).Size(flexW: 1);
        }

        public bool Visible { set => Root.gameObject.SetActive(value); }
    }

    // ==================================================================== sonuç

    public sealed class ResultView
    {
        public readonly RectTransform Root;
        private readonly RectTransform _card;
        private readonly GameServices _s;
        private readonly RawImage[] _stars = new RawImage[3];
        private float _time;
        private int _starCount;

        public ResultView(Transform stage, GameServices services)
        {
            _s = services;
            Root = Ui.Node("Sonuç", stage).Fill();
            var dim = Root.gameObject.AddComponent<Image>();
            dim.color = new Color(Ui.Night.r, Ui.Night.g, Ui.Night.b, 0.6f);
            var card = Ui.Column(Root, "kart", 3, TextAnchor.UpperCenter, 5);
            _card = card.GetComponent<RectTransform>();
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(148 * Ui.K, 0);
            Ui.FitHeight(card);
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = Ui.Panel;
            Ui.AddBorder(bg, Ui.Line, 1f);
        }

        public bool Visible { set => Root.gameObject.SetActive(value); }

        public void Show(RunState run, UnityAction onBack, UnityAction onAgain)
        {
            var r = run.Result;
            Ui.Clear(_card);
            var catalog = _s.Catalog;
            string city = catalog.Cities[r.CityIndex].Name;
            Ui.Label(_card, r.Arrived ? city + " garı" : "Yolda kaldın", 9, Ui.Gold2, TextAnchor.MiddleCenter, bold: true).Size(-1, 12);
            Ui.Label(_card, r.Arrived ? "Sefer tamamlandı" : "Yakıt bitti. Kazandığın para kasada.", 4.8f, Ui.Ink2, TextAnchor.MiddleCenter);

            var starRow = Ui.Row(_card, "yıldızlar", 3, TextAnchor.MiddleCenter);
            starRow.Size(-1, 16);
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = Ui.Picture(starRow.transform, "yıldız", SpriteLibrary.Instance.Icons[i < r.Stars ? "star" : "starOff"], 16f / 7f);
                if (i < r.Stars) _stars[i].rectTransform.localScale = Vector3.zero;
            }
            _starCount = r.Stars;
            _time = 0f;

            Line("Yol boyu teslimat ve altın", Texts.Number(r.RouteEarnings), Ui.Ink);
            if (r.Arrived) Line("Varış teslimatı ×1,5", Texts.Number(r.FinalPay), Ui.Ink);
            Line("Kaza", r.Crashes.ToString(), Ui.Ink);
            foreach (var c in r.Contracts) Line(c.Title, c.Completed ? "tamam" : "olmadı", c.Completed ? Ui.Green : Ui.Red);
            Ui.Box(_card, "çizgi", Ui.Line).Size(-1, 0.6f);
            Line("Toplam kazanç", Texts.Number(r.Earned), Ui.Gold2, true);

            if (r.Arrived && r.CityGrew)
            {
                var grow = Ui.Column(_card, "büyüme", 2, TextAnchor.MiddleCenter, 2.5f);
                var gbg = grow.gameObject.AddComponent<Image>();
                gbg.color = Ui.Night;
                var pics = Ui.Row(grow.transform, "silüetler", 3, TextAnchor.MiddleCenter);
                Ui.Picture(pics.transform, "önce", UiArt.CitySkyline(r.CityLevelBefore, r.CityIndex), 1f);
                Ui.Picture(pics.transform, "ok", GestureArt.Arrow(1), 1f);
                Ui.Picture(pics.transform, "sonra", UiArt.CitySkyline(r.CityLevelAfter, r.CityIndex), 1f);
                Ui.Label(grow.transform, city + " büyüdü: " + catalog.CityLevelNames[r.CityLevelBefore] + " → <color=#fee761><b>" + catalog.CityLevelNames[r.CityLevelAfter] + "</b></color>",
                    4.6f, Ui.Ink, TextAnchor.MiddleCenter);
            }
            else if (r.Arrived && r.Stars > r.PreviousBestStars)
            {
                Ui.Label(_card, "Hat geliri arttı: <color=#fee761><b>" + r.IdleRateAfter + "/dk</b></color>", 4.6f, Ui.Ink, TextAnchor.MiddleCenter);
            }

            var acts = Ui.Row(_card, "düğmeler", 3, TextAnchor.MiddleCenter);
            acts.Size(-1, 14);
            Ui.Button(acts.transform, "HATTA DÖN", onBack, ButtonKind.Gold, 5.5f).Size(52, 13);
            Ui.Button(acts.transform, "TEKRAR", onAgain, ButtonKind.Ghost, 5.5f).Size(40, 13);
        }

        /// <summary>Yıldızları sırayla büyüterek gösterir; çalan yıldızın sırasını döner (-1 yok).</summary>
        public int Tick(float dt)
        {
            float before = _time;
            _time += dt;
            int popped = -1;
            for (int i = 0; i < _starCount; i++)
            {
                float start = 0.3f + i * 0.25f;
                float t = Mathf.Clamp01((_time - start) / 0.5f);
                float s = t < 0.7f ? t / 0.7f * 1.3f : Mathf.Lerp(1.3f, 1f, (t - 0.7f) / 0.3f);
                if (_stars[i] != null) _stars[i].rectTransform.localScale = new Vector3(s, s, 1f);
                if (before < start && _time >= start) popped = i;
            }
            return popped;
        }

        private void Line(string label, string value, Color valueColor, bool bold = false)
        {
            var row = Ui.Row(_card, "satır", 2, TextAnchor.MiddleLeft);
            row.Size(-1, 6.5f);
            Ui.Label(row.transform, label, 5, bold ? Ui.Gold2 : Ui.Ink, TextAnchor.MiddleLeft, bold).Size(flexW: 1);
            Ui.Label(row.transform, value, 5, valueColor, TextAnchor.MiddleRight, bold);
        }
    }
}
