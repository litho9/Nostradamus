using System.Buffers;
using System.Collections.Concurrent;
using static System.Linq.Enumerable;
using static Nostradamus.Descrambelhador;

namespace Nostradamus;

public class Mhy1(string dir) {
    private readonly Dictionary<string, Dictionary<string, Dictionary<long, object>>> Blocks = new(); // blkName↝cabName↝pathId↝gameComponent
    private readonly ConcurrentDictionary<string, string> _cabMap = new(); // cabName↝blkName

    public Dictionary<string, Dictionary<long, object>> LoadBlock(string blockName) {
        Dictionary<string, Dictionary<long, object>> cabs = new();
        var stream = File.Open(blockName, FileMode.Open, FileAccess.Read);
        var reader = new ObjectReader(stream);
        while (stream.Position < stream.Length) {
            var (nodes, blocks) = ReadMhy1Headers(reader);

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
            var blocksReader = new ObjectReader(blocksStream);
            foreach (var node in nodes.Where(node => !node.Path.EndsWith("resS"))) {
                blocksStream.Position = node.Offset;
                cabs.Add(node.Path, Cab.ReadCab(blocksReader));
            }
        }
        Blocks.Add(blockName, cabs);
        return cabs;
    }

    public void LoadCabMap() {
        Parallel.ForEach(new DirectoryInfo(dir).GetFiles("*.blk"), file => {
            try {
                var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read);
                var reader = new ObjectReader(stream);
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
            pPtr.Val = (T)Blocks[blockName][pPtr.ExtPath][pPtr.PathId];
        }
        return (T)pPtr.Val!;
    }

    private static (List<DirNode>, List<StorageBlock>) ReadMhy1Headers(ObjectReader reader) {
        if (!reader.ReadBytes(4).SequenceEqual("mhy1"u8.ToArray()))
            throw new Exception("File does not begin with 'mhy1'.");
        var compressed = reader.ReadBytes((int)reader.ReadUInt32());
        Descramble(compressed, Math.Min(compressed.Length, 128), 28);
        var size = ObjectReader.ReadMhyUInt(compressed[48..(48+7)]); // offset=48 signature=7 
        var block = ArrayPool<byte>.Shared.Rent(size);
        OodleLZ(compressed.AsSpan(48+7), block.AsSpan(0, size));

        using var r = new ObjectReader(new MemoryStream(block, 0, size));
        var nodes = Range(0, r.ReadMhyInt()).Select(_ =>
            new DirNode(r.ReadMhyString(), r.ReadBoolean(), r.ReadMhyInt(), r.ReadMhyUInt())).ToList();
        var blocks = Range(0, r.ReadMhyInt()).Select(_ =>
            new StorageBlock(r.ReadMhyInt(), r.ReadMhyUInt())).ToList();
        ArrayPool<byte>.Shared.Return(block);
        return (nodes, blocks);
    }
}
