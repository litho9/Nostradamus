using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using static System.Linq.Enumerable;
using static Nostradamus.Descrambelhador;

namespace Nostradamus;

public class Mhy1(string dir) {
    public readonly Dictionary<string, Dictionary<string, Cab>> Blocks = new(); // blkName↝cabName↝Cab
    private readonly ConcurrentDictionary<string, string> _cabMap = new(); // cabName↝blkName

    public Dictionary<string, Cab> LoadBlock(string blockName) {
        Dictionary<string, Cab> cabs = new();
        var stream = File.Open(blockName, FileMode.Open, FileAccess.Read);
        var reader = new BinaryReader(stream);
        while (stream.Position < stream.Length) {
            var (nodes, blocks) = ReadMhy1Headers(reader);
            if (nodes.Count == 0) {
                Console.WriteLine($"[MHY1] skipping block {stream.Position:x8}.");
                stream.Position += blocks.Sum(x => x.CompressedSize);
                continue;
            }

            var blocksStream = new MemoryStream(blocks.Sum(x => x.UncompressedSize));
            foreach (var (compressedSize, uncompressedSize) in blocks) {
                var compressed = ArrayPool<byte>.Shared.Rent(compressedSize);
                var uncompressed = ArrayPool<byte>.Shared.Rent(uncompressedSize);
                if (reader.Read(compressed, 0, compressedSize) == 0) throw new Exception("Readn't");
                Descramble(compressed, Math.Min(compressedSize, 128), 8);
                OodleLZ(compressed.AsSpan(28, compressedSize - 28), uncompressed); // offset=28
                blocksStream.Write(uncompressed);
                ArrayPool<byte>.Shared.Return(compressed);
                ArrayPool<byte>.Shared.Return(uncompressed);
            }

            foreach (var node in nodes) {
                var fileStr = $"{blockName.Split("\\").Last()}[{stream.Position:x8}]";
                if (node.Path.EndsWith("resS")) {
                    Console.WriteLine($"[NHY1] Skipping {node.Path} in {fileStr}.");
                    continue;
                }
                Console.WriteLine($"[MHY1] Processing {node.Path} in {fileStr}.");
                blocksStream.Position = node.Offset;
                cabs.Add(node.Path, new Cab(blocksStream));
            }
        }
        Blocks.Add(blockName, cabs);
        return cabs;
    }

    public void LoadCabMap() {
        var d = new DirectoryInfo(dir);
        var files = d.GetFiles("*.blk");
        Parallel.ForEach(files, file => {
            try {
                var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read);
                var reader = new BinaryReader(stream);
                while (stream.Position < stream.Length) {
                    var (nodes, blocks) = ReadMhy1Headers(reader);
                    foreach (var n in nodes) _cabMap.TryAdd(n.Path, file.FullName);
                    stream.Position += blocks.Sum(x => x.CompressedSize);
                }
            } catch (Exception) {
                Console.WriteLine($"Error loading {file.Name}");
            }
        });
    }
    
    public T Point<T>(PPtr<T> pPtr) {
        if (pPtr.Val == null) {
            if (pPtr.ExtPath == null) return default; // TODO
            if (!_cabMap.ContainsKey(pPtr.ExtPath)) LoadCabMap();
            var blockName = _cabMap[pPtr.ExtPath];
            if (!Blocks.ContainsKey(blockName)) LoadBlock(blockName);
            var cab2 = Blocks[blockName][pPtr.ExtPath];
            pPtr.Val = (T)cab2.Objects[pPtr.PathId];
        }
        return (T)pPtr.Val!;
    }

    private static (List<DirNode>, List<StorageBlock>) ReadMhy1Headers(BinaryReader reader) {
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
        return (nodes, blocks);
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
    private static int ReadInt(byte[] buffer) =>
        buffer[2] | (buffer[4] << 8) | (buffer[0] << 0x10) | (buffer[5] << 0x18);
    private static int ReadUInt(BinaryReader r) => ReadUInt(r.ReadBytes(7));
    private static int ReadUInt(byte[] buffer) =>
        buffer[1] | (buffer[6] << 8) | (buffer[3] << 0x10) | (buffer[2] << 0x18);
    private static string ReadString(BinaryReader r) {
        var pos = r.BaseStream.Position;
        var str = ReadStringToNull(r);
        r.BaseStream.Position += 0x105 - r.BaseStream.Position + pos;
        return str;
    }

    private record DirNode(string Path, bool Flags, long Offset, int Size);
    private record StorageBlock(int CompressedSize, int UncompressedSize);
}
