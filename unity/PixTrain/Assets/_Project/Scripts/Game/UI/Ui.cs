using System.Collections.Generic;
using PixTrain.Render;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixTrain.Game.UI
{
    public enum ButtonKind { Gold, Ghost }

    /// <summary>
    /// Arayüzü koddan kurmak için küçük yardımcılar. Ölçü birimi: 1 oyun pikseli = <see cref="K"/> tuval birimi.
    /// Renkler prototipteki CSS değişkenleriyle aynıdır.
    /// </summary>
    public static class Ui
    {
        public const float K = 2f;

        public static Font Font;
        public static Font Bold;

        public static readonly Color Night = Hex("181425");
        public static readonly Color Ink = Hex("ead4aa");
        public static readonly Color Ink2 = Hex("c0cbdc");
        public static readonly Color Mute = Hex("8b9bb4");
        public static readonly Color Panel = Hex("262b44");
        public static readonly Color Panel2 = Hex("3a4466");
        public static readonly Color Line = Hex("5a6988");
        public static readonly Color Gold = Hex("feae34");
        public static readonly Color Gold2 = Hex("fee761");
        public static readonly Color Orange = Hex("f77622");
        public static readonly Color Red = Hex("e43b44");
        public static readonly Color Green = Hex("63c74d");
        public static readonly Color Night2 = new Color(0.094f, 0.078f, 0.145f, 0.86f);

        private static readonly Dictionary<PixelImage, Texture2D> Textures = new Dictionary<PixelImage, Texture2D>();
        private static Texture2D _fadeDown;

        public static void LoadFonts()
        {
            Font = Resources.Load<Font>("Fonts/PixelifySans-Regular");
            Bold = Resources.Load<Font>("Fonts/PixelifySans-Bold");
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Bold == null) Bold = Font;
        }

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }

        // ---------------------------------------------------------------- düğüm ve yerleşim

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Çapalarla yerleştirir. Kenar boşlukları oyun pikseli cinsindendir.</summary>
        public static RectTransform Anchor(this RectTransform rt, float minX, float minY, float maxX, float maxY,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(left * K, bottom * K);
            rt.offsetMax = new Vector2(-right * K, -top * K);
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0) =>
            rt.Anchor(0, 0, 1, 1, left, bottom, right, top);

        /// <summary>Yatayda uzanan, üst ya da alt kenara bağlı sabit yükseklikte şerit.</summary>
        public static RectTransform Band(this RectTransform rt, bool top, float left, float right, float y, float height)
        {
            float a = top ? 1f : 0f;
            rt.anchorMin = new Vector2(0, a);
            rt.anchorMax = new Vector2(1, a);
            rt.pivot = new Vector2(0.5f, a);
            rt.offsetMin = new Vector2(left * K, top ? -(y + height) * K : y * K);
            rt.offsetMax = new Vector2(-right * K, top ? -y * K : (y + height) * K);
            return rt;
        }

        /// <summary>Sabit boyutlu, bir çapa noktasına bağlı (pivot aynı nokta).</summary>
        public static RectTransform Pin(this RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, ay);
            rt.anchoredPosition = new Vector2(x * K, y * K);
            rt.sizeDelta = new Vector2(w * K, h * K);
            return rt;
        }

        public static LayoutElement Size(this Component c, float width = -1, float height = -1, float flexW = -1, float flexH = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) { le.minWidth = width * K; le.preferredWidth = width * K; }
            if (height >= 0) { le.minHeight = height * K; le.preferredHeight = height * K; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static HorizontalLayoutGroup Row(Transform parent, string name, float spacing = 2, TextAnchor align = TextAnchor.MiddleLeft, float pad = 0)
        {
            var rt = Node(name, parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing * K;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            int p = Mathf.RoundToInt(pad * K);
            h.padding = new RectOffset(p, p, p, p);
            return h;
        }

        public static VerticalLayoutGroup Column(Transform parent, string name, float spacing = 2, TextAnchor align = TextAnchor.UpperLeft, float pad = 0)
        {
            var rt = Node(name, parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing * K;
            v.childAlignment = align;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            int p = Mathf.RoundToInt(pad * K);
            v.padding = new RectOffset(p, p, p, p);
            return v;
        }

        public static void FitHeight(Component c)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ---------------------------------------------------------------- görseller

        /// <summary>Düz renk kutusu; isteğe bağlı piksel kontur.</summary>
        public static Image Box(Transform parent, string name, Color fill, Color? border = null, float borderPx = 0.6f)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = fill;
            img.raycastTarget = false;
            if (border.HasValue) AddBorder(img, border.Value, borderPx);
            return img;
        }

        public static void AddBorder(Graphic g, Color color, float px = 0.6f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(px * K, -px * K);
            o.useGraphicAlpha = false;
        }

        /// <summary>Yukarıdan aşağı koyulaşıp saydamlaşan şerit (HUD arka planı).</summary>
        public static RawImage FadeDown(Transform parent, string name)
        {
            if (_fadeDown == null)
            {
                _fadeDown = new Texture2D(1, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 16; y++)
                {
                    float t = y / 15f; // 0 = alt
                    float a = t < 0.3f ? Mathf.Lerp(0f, 0.7f, t / 0.3f) : Mathf.Lerp(0.7f, 0.9f, (t - 0.3f) / 0.7f);
                    _fadeDown.SetPixel(0, y, new Color(Night.r, Night.g, Night.b, a));
                }
                _fadeDown.Apply();
            }
            var raw = Node(name, parent).gameObject.AddComponent<RawImage>();
            raw.texture = _fadeDown;
            raw.raycastTarget = false;
            return raw;
        }

        /// <summary>Piksel çizimi gösterir; boyut oyun pikseli × <paramref name="scale"/>.</summary>
        public static RawImage Picture(Transform parent, string name, PixelImage image, float scale = 1f)
        {
            var raw = Node(name, parent).gameObject.AddComponent<RawImage>();
            raw.texture = TextureOf(image);
            raw.uvRect = PixelTexture.FlippedUv;
            raw.raycastTarget = false;
            raw.Size(image.Width * scale, image.Height * scale);
            raw.rectTransform.sizeDelta = new Vector2(image.Width * scale * K, image.Height * scale * K);
            return raw;
        }

        public static void SetPicture(RawImage raw, PixelImage image) => raw.texture = TextureOf(image);

        /// <summary>Aynı çizim için tek doku (ikonlar sık kullanılır).</summary>
        public static Texture2D TextureOf(PixelImage image)
        {
            if (!Textures.TryGetValue(image, out var tex)) { tex = PixelTexture.FromImage(image); Textures[image] = tex; }
            return tex;
        }

        public static RawImage Icon(Transform parent, string icon, float size = 7)
        {
            var raw = Picture(parent, "ikon-" + icon, SpriteLibrary.Instance.Icons[icon], size / 7f);
            return raw;
        }

        // ---------------------------------------------------------------- yazı ve düğme

        /// <summary>Metin; boyut oyun pikseli cinsinden (6 = gövde, 4.5 = küçük, 9 = başlık).</summary>
        public static Text Label(Transform parent, string text, float size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var t = Node("yazı", parent).gameObject.AddComponent<Text>();
            t.font = bold ? Bold : Font;
            t.fontSize = Mathf.RoundToInt(size * K);
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1f;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, ButtonKind kind = ButtonKind.Gold, float size = 6)
        {
            var rt = Node("düğme-" + label, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = kind == ButtonKind.Gold ? Gold : Panel2;
            AddBorder(img, Night, 0.6f);
            var shade = rt.gameObject.AddComponent<Shadow>();
            shade.effectColor = kind == ButtonKind.Gold ? Orange : Panel;
            shade.effectDistance = new Vector2(0, -1.2f * K);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.9f);
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Label(rt, label, size, kind == ButtonKind.Gold ? Night : Ink, TextAnchor.MiddleCenter, bold: true);
            t.rectTransform.Fill(2, 0, 2, 0);
            return b;
        }

        public static void SetButton(UnityEngine.UI.Button b, string label, bool interactable)
        {
            b.interactable = interactable;
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = label;
        }

        /// <summary>İkon + yazı içeren küçük etiket (HUD çipleri).</summary>
        public static HorizontalLayoutGroup Chip(Transform parent, string name, Color border)
        {
            var row = Row(parent, name, 0.8f, TextAnchor.MiddleCenter);
            row.padding = new RectOffset((int)(1.4f * K), (int)(1.4f * K), (int)(0.6f * K), (int)(0.6f * K));
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = new Color(Panel.r, Panel.g, Panel.b, 0.92f);
            bg.raycastTarget = false;
            AddBorder(bg, border, 0.5f);
            return row;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
