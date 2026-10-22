namespace PixTrain.Game
{
    /// <summary>Oyuncunun gördüğü Türkçe metinler.</summary>
    public static class Texts
    {
        public const string Tagline = "Makas senin elinde.";
        public const string TitleHint = "Şehirler arası sefere çık. Parmağını sola ya da sağa kaydırarak makası çevir, aşağı çekerek düdük çal. Yolcu, kömür ve gıda taşı, gece gündüz para kazan.";
        public const string Play = "OYNA";

        public static readonly string[] Tips =
        {
            "Parmağını SOLA ya da SAĞA kaydır: sıradaki makas döner.",
            "Sarı ray, trenin gideceği yolu gösterir.",
            "Mavi peron yolcu verir. Pembe şehir yolcu ve gıda satın alır.",
            "Gri maden kömür verir. Turuncu fabrika kömürü alır.",
            "Kırmızı peron yakıt. Yakıt biterse sefer biter!",
            "Rayda inek mi var? Parmağını AŞAĞI çek: düt düt!",
            "YUKARI it: turbo. Altınlar ×2, ama yakıt iki kat hızlı biter.",
            "Peronları kazasız art arda tuttur: SERİ çarpanı ×1,5'e kadar çıkar.",
            "Karşıdan tren gelirse üstte kırmızı uyarı yanar. Şerit değiştir!",
        };

        public static readonly string[][] Legend =
        {
            new[] { "0099db", "Kasaba peronu: yolcu al" },
            new[] { "5a6988", "Maden: kömür al" },
            new[] { "63c74d", "Çiftlik: gıda al" },
            new[] { "b55088", "Şehir: yolcu ve gıda sat" },
            new[] { "d77643", "Fabrika: kömür sat" },
            new[] { "e43b44", "Yakıt istasyonu" },
        };

        /// <summary>Türkçe büyük harf (i → İ, ı → I). Kültür verisine güvenmez; WebGL ve IL2CPP'de de aynı çalışır.</summary>
        public static string Upper(string s)
        {
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                chars[i] = chars[i] == 'i' ? 'İ' : chars[i] == 'ı' ? 'I' : char.ToUpperInvariant(chars[i]);
            return new string(chars);
        }

        /// <summary>Binlik ayraçlı sayı: 12450 → "12.450".</summary>
        public static string Number(int n)
        {
            string digits = System.Math.Abs((long)n).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0) sb.Append('.');
                sb.Append(digits[i]);
            }
            return (n < 0 ? "-" : "") + sb;
        }

        /// <summary>Tek ondalıklı sayı, virgülle: 3.25 → "3,3".</summary>
        public static string Decimal1(float v) => v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

        public static string Decimal2(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
