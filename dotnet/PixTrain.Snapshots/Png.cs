using System.IO;
using System.IO.Compression;
using System.Text;
using PixTrain.Render;

namespace PixTrain.Snapshots
{
    /// <summary>Bağımlılıksız küçük PNG yazıcı (RGBA, en yakın komşu büyütmeli).</summary>
    internal static class Png
    {
        private static readonly uint[] CrcTable = BuildCrcTable();

        public static void Write(string path, PixelImage image, int scale = 1)
        {
            int w = image.Width * scale, h = image.Height * scale;
            using var raw = new MemoryStream();
            for (int y = 0; y < h; y++)
            {
                raw.WriteByte(0);
                for (int x = 0; x < w; x++)
                {
                    uint c = image.Pixels[(y / scale) * image.Width + x / scale];
                    raw.WriteByte((byte)Rgba.R(c));
                    raw.WriteByte((byte)Rgba.G(c));
                    raw.WriteByte((byte)Rgba.B(c));
                    raw.WriteByte((byte)Rgba.A(c));
                }
            }
            using var compressed = new MemoryStream();
            using (var z = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) raw.WriteTo(z);

            using var file = File.Create(path);
            file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var header = new MemoryStream();
            WriteInt(header, w); WriteInt(header, h);
            header.Write(new byte[] { 8, 6, 0, 0, 0 });
            Chunk(file, "IHDR", header.ToArray());
            Chunk(file, "IDAT", compressed.ToArray());
            Chunk(file, "IEND", new byte[0]);
        }

        private static void Chunk(Stream s, string type, byte[] data)
        {
            WriteInt(s, data.Length);
            var typeBytes = Encoding.ASCII.GetBytes(type);
            s.Write(typeBytes);
            s.Write(data);
            uint crc = 0xFFFFFFFFu;
            foreach (var b in typeBytes) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            WriteInt(s, (int)(crc ^ 0xFFFFFFFFu));
        }

        private static void WriteInt(Stream s, int v)
        {
            s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v);
        }

        private static uint[] BuildCrcTable()
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                t[n] = c;
            }
            return t;
        }
    }
}
