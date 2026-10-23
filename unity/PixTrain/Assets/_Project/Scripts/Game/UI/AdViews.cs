using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixTrain.Game.UI
{
    /// <summary>Yakıt bitip tren durunca çıkan pencere: reklam izleyip devam et ya da seferi bitir.</summary>
    public sealed class ContinueDialog
    {
        public readonly RectTransform Root;
        private readonly Text _body, _message;
        private readonly Button _watch;

        public ContinueDialog(Transform stage, UnityAction onWatch, UnityAction onGiveUp)
        {
            Root = Ui.Node("Devam", stage).Fill();
            var dim = Root.gameObject.AddComponent<Image>();
            dim.color = new Color(Ui.Night.r, Ui.Night.g, Ui.Night.b, 0.55f);
            var card = Ui.Column(Root, "kart", 3, TextAnchor.UpperCenter, 5);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(136 * Ui.K, 0);
            Ui.FitHeight(card);
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = Ui.Panel;
            Ui.AddBorder(bg, Ui.Gold, 1f);

            var head = Ui.Row(card.transform, "başlık", 2, TextAnchor.MiddleCenter);
            head.Size(-1, 12);
            Ui.Icon(head.transform, "fuel", 7);
            Ui.Label(head.transform, "YAKIT BİTTİ", 8, Ui.Gold2, TextAnchor.MiddleCenter, bold: true);
            _body = Ui.Label(card.transform, "", 5, Ui.Ink, TextAnchor.MiddleCenter);
            _message = Ui.Label(card.transform, "", 4.6f, Ui.Red, TextAnchor.MiddleCenter);
            _watch = Ui.Button(card.transform, "REKLAM İZLE · DEVAM ET", onWatch, ButtonKind.Gold, 5.5f);
            _watch.Size(-1, 14);
            Ui.Button(card.transform, "SEFERİ BİTİR", onGiveUp, ButtonKind.Ghost, 5f).Size(-1, 12);
            Root.gameObject.SetActive(false);
        }

        public bool Visible => Root.gameObject.activeSelf;

        public void Show(int fuel)
        {
            _body.text = "Tren yolda kaldı. Kısa bir reklam izle, " + fuel + " yakıtla yola devam et. Kazandığın her şey seninle kalır.";
            _message.text = "";
            _message.gameObject.SetActive(false);
            _watch.interactable = true;
            Root.gameObject.SetActive(true);
        }

        public void SetBusy(bool busy) => _watch.interactable = !busy;

        public void SetMessage(string text)
        {
            _message.text = text;
            _message.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void Hide() => Root.gameObject.SetActive(false);
    }

    /// <summary>Test reklamı ekranı (editör ve reklam ağı bağlanmamış sürümler).</summary>
    public sealed class TestAdOverlay
    {
        public readonly RectTransform Root;
        private readonly Text _countdown;

        public TestAdOverlay(Transform stage, UnityAction onClose)
        {
            Root = Ui.Node("TestReklam", stage).Fill();
            var bg = Root.gameObject.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.06f, 0.96f);
            var col = Ui.Column(Root, "içerik", 4, TextAnchor.MiddleCenter, 8);
            col.GetComponent<RectTransform>().Fill();
            Ui.Label(col.transform, "TEST REKLAMI", 10, Ui.Gold2, TextAnchor.MiddleCenter, bold: true).Size(-1, 16);
            Ui.Label(col.transform, "Yayında burada gerçek bir ödüllü reklam gösterilir. Sonuna kadar izlenince ödül verilir.", 5, Ui.Ink2, TextAnchor.MiddleCenter);
            _countdown = Ui.Label(col.transform, "", 14, Ui.Ink, TextAnchor.MiddleCenter, bold: true);
            _countdown.Size(-1, 20);
            Ui.Button(col.transform, "KAPAT (ödülsüz)", onClose, ButtonKind.Ghost, 5).Size(60, 12);
            Root.gameObject.SetActive(false);
        }

        public void Set(bool visible, float remaining)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
            if (visible) _countdown.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
            if (visible) Root.SetAsLastSibling();
        }
    }
}
