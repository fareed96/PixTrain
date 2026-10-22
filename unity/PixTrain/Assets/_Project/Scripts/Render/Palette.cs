using System.Collections.Generic;

namespace PixTrain.Render
{
    /// <summary>
    /// Endesga 32 paleti, tek harfli kodlarla (prototiple aynı). '#' yarı saydam gölgedir.
    /// </summary>
    public static class Palette
    {
        private static readonly Dictionary<char, uint> Map = new Dictionary<char, uint>
        {
            ['k'] = Rgba.FromHex("181425"), ['d'] = Rgba.FromHex("262b44"), ['g'] = Rgba.FromHex("3a4466"),
            ['G'] = Rgba.FromHex("5a6988"), ['s'] = Rgba.FromHex("8b9bb4"), ['S'] = Rgba.FromHex("c0cbdc"),
            ['w'] = Rgba.FromHex("ffffff"), ['r'] = Rgba.FromHex("a22633"), ['R'] = Rgba.FromHex("e43b44"),
            ['o'] = Rgba.FromHex("f77622"), ['y'] = Rgba.FromHex("feae34"), ['Y'] = Rgba.FromHex("fee761"),
            ['n'] = Rgba.FromHex("733e39"), ['N'] = Rgba.FromHex("b86f50"), ['q'] = Rgba.FromHex("3e2731"),
            ['u'] = Rgba.FromHex("be4a2f"), ['U'] = Rgba.FromHex("d77643"), ['t'] = Rgba.FromHex("ead4aa"),
            ['T'] = Rgba.FromHex("e4a672"), ['x'] = Rgba.FromHex("c28569"), ['z'] = Rgba.FromHex("e8b796"),
            ['e'] = Rgba.FromHex("265c42"), ['E'] = Rgba.FromHex("3e8948"), ['l'] = Rgba.FromHex("63c74d"),
            ['v'] = Rgba.FromHex("193c3e"), ['b'] = Rgba.FromHex("124e89"), ['B'] = Rgba.FromHex("0099db"),
            ['c'] = Rgba.FromHex("2ce8f5"), ['p'] = Rgba.FromHex("68386c"), ['m'] = Rgba.FromHex("b55088"),
            ['f'] = Rgba.FromHex("f6757a"), ['h'] = Rgba.FromHex("ff0044"),
            ['#'] = Rgba.FromHex("181425", 97),
        };

        public static uint Get(char code) => Map.TryGetValue(code, out var c) ? c : Rgba.Transparent;

        public static bool Has(char code) => Map.ContainsKey(code);

        public static readonly uint Night = Get('k');
        public static readonly uint Shadow = Get('#');
    }
}
