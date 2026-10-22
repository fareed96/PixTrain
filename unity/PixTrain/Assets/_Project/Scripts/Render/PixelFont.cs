using System.Collections.Generic;

namespace PixTrain.Render
{
    /// <summary>
    /// Sahne içi etiketler için 3x5 bitmap yazı ("+54", "DOLU", "KAZA!").
    /// Türkçe harfler en yakın ASCII karşılığına çevrilir; uzun metinler arayüzde gerçek fontla gösterilir.
    /// </summary>
    public static class PixelFont
    {
        private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "111001111100111", ['3'] = "111001111001111",
            ['4'] = "101101111001001", ['5'] = "111100111001111", ['6'] = "111100111101111", ['7'] = "111001010010010",
            ['8'] = "111101111101111", ['9'] = "111101111001111", ['+'] = "000010111010000", ['-'] = "000000111000000",
            ['X'] = "000101010101000", ['!'] = "010010010000010", ['.'] = "000000000000010", ['%'] = "101001010100101",
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
            ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
            ['I'] = "111010010010111", ['K'] = "101101110101101", ['L'] = "100100100100111", ['M'] = "101111111101101",
            ['N'] = "110101101101101", ['O'] = "010101101101010", ['P'] = "110101110100100", ['R'] = "110101110101101",
            ['S'] = "011100010001110", ['T'] = "111010010010010", ['U'] = "101101101101111", ['V'] = "101101101101010",
            ['Y'] = "101101010010010", ['Z'] = "111001010100111", [' '] = "000000000000000",
        };

        private static readonly Dictionary<char, char> Fold = new Dictionary<char, char>
        {
            ['ç'] = 'C', ['Ç'] = 'C', ['ğ'] = 'G', ['Ğ'] = 'G', ['ı'] = 'I', ['İ'] = 'I', ['i'] = 'I',
            ['ö'] = 'O', ['Ö'] = 'O', ['ş'] = 'S', ['Ş'] = 'S', ['ü'] = 'U', ['Ü'] = 'U', ['×'] = 'X', ['x'] = 'X',
        };

        public static int Measure(string text) => text.Length * 4 - 1;

        /// <summary>Metni (x merkezli) çizer; isteğe bağlı koyu kontur ile.</summary>
        public static void DrawCentered(PixelImage dst, string text, int cx, int y, uint color, bool outline = true)
        {
            text = Normalize(text);
            int x0 = cx - Measure(text) / 2;
            if (outline)
            {
                uint k = Palette.Get('k');
                int[,] offsets = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 1 } };
                for (int o = 0; o < offsets.GetLength(0); o++) DrawRaw(dst, text, x0 + offsets[o, 0], y + offsets[o, 1], k);
            }
            DrawRaw(dst, text, x0, y, color);
        }

        private static void DrawRaw(PixelImage dst, string text, int x0, int y, uint color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!Glyphs.TryGetValue(text[i], out var g)) g = Glyphs[' '];
                for (int b = 0; b < 15; b++)
                    if (g[b] == '1') dst.FillRect(x0 + i * 4 + b % 3, y + b / 3, 1, 1, color);
            }
        }

        private static string Normalize(string text)
        {
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Fold.TryGetValue(chars[i], out var f)) chars[i] = f;
                else chars[i] = char.ToUpperInvariant(chars[i]);
            }
            return new string(chars);
        }
    }
}
