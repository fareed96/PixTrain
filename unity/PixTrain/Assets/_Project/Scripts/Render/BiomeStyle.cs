using System.Collections.Generic;
using PixTrain.Core.Data;
using PixTrain.Core.Utils;

namespace PixTrain.Render
{
    public enum PropKind { None, Tree, TreeAutumn, Pine, PineSnow, PineSnow2, Palm, Bush, Flowers, Rock, RockBig, RockSnow, Logs }

    public enum SpecialProp { Mill, Farm, Lighthouse }

    /// <summary>Biyomun görünümü: zemin ve balast renkleri, tünel dağı renkleri, kırsal manzara dağılımı.</summary>
    public sealed class BiomeStyle
    {
        /// <summary>Zemin: ana renk, açık leke, koyu leke (palet kodları).</summary>
        public char[] Ground;
        public char[] Ballast;
        /// <summary>Tünel dağı: ana, ışık alan, gölge, kontur, ön yüz, ön yüz koyu.</summary>
        public char[] Mountain = { 's', 'S', 'G', 'g', 'n', 'q' };
        public float Density = 1f;
        public WeightedTable<PropKind> Countryside = new WeightedTable<PropKind>();
        public SpecialProp[] Specials = new SpecialProp[0];
        /// <summary>Kasaba içindeki ağaç türü.</summary>
        public PropKind TownTree = PropKind.Tree;

        private static Dictionary<BiomeId, BiomeStyle> _all;

        public static BiomeStyle For(BiomeId id)
        {
            if (_all == null) _all = Build();
            return _all[id];
        }

        private static Dictionary<BiomeId, BiomeStyle> Build()
        {
            var d = new Dictionary<BiomeId, BiomeStyle>();
            d[BiomeId.Plains] = new BiomeStyle
            {
                Ground = new[] { 'E', 'l', 'e' }, Ballast = new[] { 'G', 's', 'g' }, Density = 1f,
                Countryside = new WeightedTable<PropKind>().Add(PropKind.Tree, 6).Add(PropKind.TreeAutumn, 1).Add(PropKind.Bush, 3)
                    .Add(PropKind.Flowers, 2).Add(PropKind.Rock, 0.6f).Add(PropKind.None, 3),
                Specials = new[] { SpecialProp.Mill, SpecialProp.Farm },
            };
            d[BiomeId.Forest] = new BiomeStyle
            {
                Ground = new[] { 'e', 'E', 'v' }, Ballast = new[] { 'G', 's', 'g' }, Density = 1.7f,
                Mountain = new[] { 'E', 'l', 'e', 'v', 'n', 'q' },
                Countryside = new WeightedTable<PropKind>().Add(PropKind.Pine, 9).Add(PropKind.Tree, 3).Add(PropKind.Bush, 2)
                    .Add(PropKind.Logs, 1).Add(PropKind.Rock, 1),
                Specials = new[] { SpecialProp.Farm }, TownTree = PropKind.Pine,
            };
            d[BiomeId.River] = new BiomeStyle
            {
                Ground = new[] { 'E', 'l', 'e' }, Ballast = new[] { 'G', 's', 'g' }, Density = 1f,
                Countryside = new WeightedTable<PropKind>().Add(PropKind.Tree, 6).Add(PropKind.Bush, 4).Add(PropKind.Flowers, 3).Add(PropKind.None, 2),
                Specials = new[] { SpecialProp.Mill, SpecialProp.Farm },
            };
            d[BiomeId.Mountain] = new BiomeStyle
            {
                Ground = new[] { 'x', 'N', 'T' }, Ballast = new[] { 'G', 's', 'g' }, Density = 1.2f,
                Mountain = new[] { 's', 'S', 'G', 'g', 'n', 'q' },
                Countryside = new WeightedTable<PropKind>().Add(PropKind.Rock, 5).Add(PropKind.RockBig, 3).Add(PropKind.Pine, 4).Add(PropKind.Bush, 1),
                TownTree = PropKind.Pine,
            };
            d[BiomeId.Coast] = new BiomeStyle
            {
                Ground = new[] { 'E', 'l', 'e' }, Ballast = new[] { 'G', 's', 'g' }, Density = 1f,
                Countryside = new WeightedTable<PropKind>().Add(PropKind.Palm, 4).Add(PropKind.Bush, 3).Add(PropKind.Rock, 2)
                    .Add(PropKind.Flowers, 2).Add(PropKind.Tree, 1),
                Specials = new[] { SpecialProp.Lighthouse },
            };
            d[BiomeId.Snow] = new BiomeStyle
            {
                Ground = new[] { 'S', 'w', 's' }, Ballast = new[] { 's', 'S', 'G' }, Density = 1.3f,
                Mountain = new[] { 'w', 'w', 'S', 's', 'G', 'g' },
                Countryside = new WeightedTable<PropKind>().Add(PropKind.PineSnow, 6).Add(PropKind.PineSnow2, 3).Add(PropKind.RockSnow, 3).Add(PropKind.None, 2),
                TownTree = PropKind.PineSnow,
            };
            return d;
        }

        public static PixelImage SpriteOf(PropKind kind)
        {
            var s = SpriteLibrary.Instance;
            switch (kind)
            {
                case PropKind.Tree: return s.Tree;
                case PropKind.TreeAutumn: return s.TreeAutumn;
                case PropKind.Pine: return s.Pine;
                case PropKind.PineSnow: return s.PineSnow;
                case PropKind.PineSnow2: return s.PineSnow2;
                case PropKind.Palm: return s.Palm;
                case PropKind.Bush: return s.Bush;
                case PropKind.Flowers: return s.Flowers;
                case PropKind.Rock: return s.Rock;
                case PropKind.RockBig: return s.RockBig;
                case PropKind.RockSnow: return s.RockSnow;
                case PropKind.Logs: return s.Logs;
                default: return null;
            }
        }
    }
}
