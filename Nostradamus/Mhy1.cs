using System.Buffers;
using System.Text;
using static System.Linq.Enumerable;
using static Nostradamus.Descrambelhador;

namespace Nostradamus;

public class Blk {
    public readonly Dictionary<string, Cab> Cabs = new();

    public Blk(string fileName) {
        var stream = File.Open(fileName, FileMode.Open, FileAccess.Read);
        var reader = new BinaryReader(stream);
        while (stream.Position < stream.Length) {
            if (!reader.ReadBytes(4).SequenceEqual("mhy1"u8.ToArray()))
                throw new Exception("File does not begin with 'mhy1'.");
            var compressed = reader.ReadBytes((int)reader.ReadUInt32());
            Descramble(compressed, Math.Min(compressed.Length, 128), 28);
            var size = ReadUInt(compressed[48..(48+7)]); // offset=48 signature=7 
            var block = ArrayPool<byte>.Shared.Rent(size);
            OodleLZ(compressed.AsSpan(48+7), block.AsSpan(0, size));

            using var r = new BinaryReader(new MemoryStream(block, 0, size));
            var nodes = Range(0, ReadInt(r)).Select(_ =>
                new DirNode(ReadString(r), r.ReadBoolean(), ReadInt(r), ReadUInt(r))).ToList();
            var blocks = Range(0, ReadInt(r)).Select(_ =>
                new StorageBlock(ReadInt(r), ReadUInt(r))).ToList();
            ArrayPool<byte>.Shared.Return(block);
            if (nodes.Count == 0) {
                Console.WriteLine($"[MHY1] skipping block {stream.Position:x8}.");
                stream.Position += blocks.Sum(x => x.CompressedSize);
                continue;
            }

            var blocksStream = new MemoryStream(blocks.Sum(x => x.UncompressedSize));
            foreach (var (compressedSize, uncompressedSize) in blocks) {
                var compressedBytes = ArrayPool<byte>.Shared.Rent(compressedSize);
                var uncompressedBytes = ArrayPool<byte>.Shared.Rent(uncompressedSize);
                if (reader.Read(compressedBytes, 0, compressedSize) == 0) throw new Exception("Readn't");
                Descramble(compressedBytes, Math.Min(compressedSize, 128), 8);
                OodleLZ(compressedBytes.AsSpan(28, compressedSize - 28), uncompressedBytes); // offset=28
                blocksStream.Write(uncompressedBytes);
                ArrayPool<byte>.Shared.Return(compressedBytes);
                ArrayPool<byte>.Shared.Return(uncompressedBytes);
            }

            foreach (var node in nodes) {
                var fileStr = $"{fileName.Split("\\").Last()}[{stream.Position:x8}]";
                if (node.Path.EndsWith("resS")) {
                    Console.WriteLine($"[NHY1] Skipping {node.Path} in {fileStr}.");
                    continue;
                }
                Console.WriteLine($"[MHY1] Processing {node.Path} in {fileStr}.");
                blocksStream.Position = node.Offset;
                Cabs.Add(node.Path, new Cab(blocksStream));
            }
        }
    }
    
    private static string ReadStringToNull(BinaryReader reader, int maxLength = 32767) {
        var bytes = new List<byte>();
        var count = 0;
        while (count < maxLength) {
            var b = reader.ReadByte();
            if (b == 0) break;
            bytes.Add(b);
            count++;
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }
    private static int ReadInt(BinaryReader r) => ReadInt(r.ReadBytes(6));
    private static int ReadInt(byte[] buffer) => buffer[2] | (buffer[4] << 8) | (buffer[0] << 0x10) | (buffer[5] << 0x18);
    private static int ReadUInt(BinaryReader r) => ReadUInt(r.ReadBytes(7));
    private static int ReadUInt(byte[] buffer) => buffer[1] | (buffer[6] << 8) | (buffer[3] << 0x10) | (buffer[2] << 0x18);
    private static string ReadString(BinaryReader r) {
        var pos = r.BaseStream.Position;
        var str = ReadStringToNull(r);
        r.BaseStream.Position += 0x105 - r.BaseStream.Position + pos;
        return str;
    }

    private record DirNode(string Path, bool Flags, long Offset, int Size);
    private record StorageBlock(int CompressedSize, int UncompressedSize);
}
