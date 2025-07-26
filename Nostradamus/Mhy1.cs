using System.Buffers;
using System.Text;
using static System.Linq.Enumerable;
using static Nostradamus.Descrambelhador;

namespace Nostradamus;

public static class Mhy1 {
    static void ExtractCabNames(string dir, string outName, Dictionary<string, string> cabMap) {
        // using var writer = new StreamWriter(outName);
        // writer.WriteLine("{");
        var d = new DirectoryInfo(dir);
        var files = d.GetFiles("*.blk");
        foreach (var file in files) {
            var reader = new BinaryReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read));
            while (reader.BaseStream.Position < reader.BaseStream.Length) {
                var (nodes, blocks) = CabsFromBlocks(reader);
                foreach (var n in nodes) {
                    // if (n.Path == "CAB-40587ae403cb2c9f487be1d7a74f3c21") Console.WriteLine("Found it");
                    cabMap.TryAdd(n.Path, file.FullName);
                    // writer.WriteLine($"\t\"{n.Path}\":\"{file.Name}\",");
                }
                reader.BaseStream.Position += blocks.Sum(x => x.CompressedSize);
            }
        }
        // writer.WriteLine("}");
    }

    public static Cab LoadCab(string file, string cabName) {
        var reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read));
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            var (nodes, blocks) = CabsFromBlocks(reader);
            if (nodes.Count == 0) {
                Console.WriteLine($"[MHY1] skipping block {reader.BaseStream.Position:x8}.");
                reader.BaseStream.Position += blocks.Sum(x => x.CompressedSize);
                continue;
            }
            var node = nodes.Find(n => n.Path == cabName);
            if (node == null) {
                reader.BaseStream.Position += blocks.Sum(x => x.CompressedSize);
                continue;
            }

            var stream = GetStream(reader, blocks);
            Console.WriteLine($"[LoadCab] Processing {node.Path} in {file.Split("\\").Last()}[{reader.BaseStream.Position:x8}].");
            stream.Position = node.Offset;
            return new Cab(stream);
        }
        throw new InvalidDataException();
    }

    public static void FindCabName(string file, long[] pathIds) {
        var reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read));
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            var (nodes, blocks) = CabsFromBlocks(reader);
            if (nodes.Count == 0) {
                Console.WriteLine($"[MHY1] skipping block {reader.BaseStream.Position:x8}.");
                reader.BaseStream.Position += blocks.Sum(x => x.CompressedSize);
                continue;
            }

            var stream = GetStream(reader, blocks);
            foreach (var node in nodes) {
                // if (node.Path != "CAB-aa6d8d733f6e9eb4ed21e616760746f8") continue; // TODO TEST!!!
                Console.WriteLine($"[MHY1] Processing {node.Path} in {file.Split("\\").Last()}[{reader.BaseStream.Position:x8}].");
                stream.Position = node.Offset;
                if (node.Path.EndsWith("resS")) {
                    Console.WriteLine($"[NHY1] skipping {node.Path}");
                    continue;
                }
                var cab = new Cab(stream);
                foreach (var (pathId, objInfo) in cab.Objects) {
                    var obj = cab.ReadObject(cab.Objects[pathId]);
                    Console.WriteLine($"{pathId:x16} {obj}");
                    if (obj is Transform tt && tt.Father.PathId == 0) {
                        Console.WriteLine("I am root!");
                        break;
                    }
                }
                // foreach (var pathId in pathIds)
                //     if (cab.Objects.ContainsKey(pathId)) {
                //         Console.WriteLine($"{pathId} found in {node.Path}");
                //         var readObject = cab.ReadObject(cab.Objects[pathId]);
                //         Console.WriteLine($"{readObject}");
                //     }
            }
        }
    }

    static T Point<T>(PPtr<T> pPtr, Cab cab, Dictionary<string, string> cabMap) {
        if (pPtr.FileId == 0) return (T)cab.ReadObject(cab.Objects[pPtr.PathId]);
        var extCabName = cab.Externals[pPtr.FileId - 1];
        var blkName = cabMap[extCabName];
        var cab2 = LoadCab(blkName, extCabName);
        // cache.Add(extCabName, cab2);
        return (T)cab2.ReadObject(cab2.Objects[pPtr.PathId]);
    }

    private static (List<DirNode>, List<StorageBlock>) CabsFromBlocks(BinaryReader reader) {
        if (!reader.ReadBytes(4).SequenceEqual("mhy1"u8.ToArray())) {
            Console.WriteLine("File does not begin with 'mhy1'.");
            return ([], [new StorageBlock((int)reader.BaseStream.Length, 0)]);
        }
        var compressed = reader.ReadBytes((int)reader.ReadUInt32());
        Descramble(compressed, Math.Min(compressed.Length, 128), 28);
        var size = ReadUInt(compressed[48..(48+7)]); // offset=48 signature=7 
        var block = ArrayPool<byte>.Shared.Rent(size);
        OodleLZ(compressed.AsSpan(48+7), block.AsSpan(0, size));

        using var r = new BinaryReader(new MemoryStream(block, 0, size));
        var dirInfo = Range(0, ReadInt(r)).Select(_ => new DirNode(
            ReadString(r),
            r.ReadBoolean(),
            ReadInt(r),
            ReadUInt(r))
        ).ToList();
        var blockInfo = Range(0, ReadInt(r)).Select(_ => new StorageBlock(
            ReadInt(r),
            ReadUInt(r)
        )).ToList();
        ArrayPool<byte>.Shared.Return(block);
        return (dirInfo, blockInfo);
    }

    private static MemoryStream GetStream(BinaryReader reader, List<StorageBlock> blockInfo) {
        var blocksStream = new MemoryStream(blockInfo.Sum(x => x.UncompressedSize));
        foreach (var (compressedSize, uncompressedSize) in blockInfo) {
            var compressedBytes = ArrayPool<byte>.Shared.Rent(compressedSize);
            var uncompressedBytes = ArrayPool<byte>.Shared.Rent(uncompressedSize);
            if (reader.Read(compressedBytes, 0, compressedSize) == 0) throw new Exception("Readn't");
            try {
                var cSpan = compressedBytes.AsSpan(0, compressedSize);
                Descramble(cSpan, Math.Min(compressedSize, 128), 8);
                var offset = 28;
                OodleLZ(cSpan.Slice(offset, compressedSize - offset), uncompressedBytes);
                blocksStream.Write(uncompressedBytes);
            } finally {
                ArrayPool<byte>.Shared.Return(compressedBytes);
                ArrayPool<byte>.Shared.Return(uncompressedBytes);
            }
        }
        return blocksStream;
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
    public static int ReadInt(BinaryReader r) => ReadInt(r.ReadBytes(6));
    public static int ReadInt(byte[] buffer) => buffer[2] | (buffer[4] << 8) | (buffer[0] << 0x10) | (buffer[5] << 0x18);
    public static int ReadUInt(BinaryReader r) => ReadUInt(r.ReadBytes(7));
    public static int ReadUInt(byte[] buffer) => buffer[1] | (buffer[6] << 8) | (buffer[3] << 0x10) | (buffer[2] << 0x18);
    public static string ReadString(BinaryReader r) {
        var pos = r.BaseStream.Position;
        var str = ReadStringToNull(r);
        r.BaseStream.Position += 0x105 - r.BaseStream.Position + pos;
        return str;
    }

    private record DirNode(string Path, bool Flags, long Offset, int Size);
    private record StorageBlock(int CompressedSize, int UncompressedSize);
}

