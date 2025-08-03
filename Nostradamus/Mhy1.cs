using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using static System.Linq.Enumerable;
using static Nostradamus.Descrambelhador;

namespace Nostradamus;

public class Mhy1(string dir) {
    public readonly Dictionary<string, Dictionary<string, Dictionary<long, object>>> Blocks = new(); // blkName↝cabName↝pathId↝gameComponent
    private readonly ConcurrentDictionary<string, string> _cabMap = new(); // cabName↝blkName

    public Dictionary<string, Dictionary<long, object>> LoadBlock(string blockName) {
        Dictionary<string, Dictionary<long, object>> cabs = new();
        var stream = File.Open(blockName, FileMode.Open, FileAccess.Read);
        var reader = new ObjectReader(stream);
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
                var objects = ReadCab(blocksStream);
                cabs.Add(node.Path, objects);
            }
        }
        Blocks.Add(blockName, cabs);
        return cabs;
    }

    public ConcurrentDictionary<string, string> LoadCabMap() {
        var d = new DirectoryInfo(dir);
        var files = d.GetFiles("*.blk");
        Parallel.ForEach(files, file => {
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
        return _cabMap;
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
    
    public static Dictionary<long, object> ReadCab(Stream stream) {
        var reader = new ObjectReader(stream);
        
        /*var metadataSize =*/ BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        /*var fileSize =*/ BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        /*var version =*/ BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)); // 21
        var dataOffset = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        /*var isBigEndian =*/ reader.ReadBoolean();
        /*var reserved =*/ reader.ReadBytes(3);
        /*var unityVersion =*/ reader.ReadStringToNull();
        /*var targetPlatform =*/ reader.ReadInt32(); // 19 (StandaloneWindows64)

        var enableTypeTree = reader.ReadBoolean();
        var types = reader.ReadList(_ => ReadType(reader, enableTypeTree, false));
        var objCount = reader.Align(reader.ReadInt32); // don't ask me why align, I don't know either
        var info = Range(0, objCount).ToDictionary(_ => reader.ReadInt64(), _ =>
            new ObjectInfo(dataOffset + reader.ReadUInt32(), reader.ReadUInt32(), types[reader.ReadInt32()]));
        /*var scriptTypes =*/ reader.ReadList(_ => new LocalSerializedObjectIdentifier {
            LocalSerializedFileIndex = reader.ReadInt32(),
            LocalIdentifierInFile = reader.ReadInt64()
        });
        var externals = reader.ReadList(_ => new FileIdentifier {
            TempEmpty = reader.ReadStringToNull(),
            Guid = new Guid(reader.ReadBytes(16)),
            Type = reader.ReadInt32(),
            PathName = reader.ReadStringToNull()
        }).Select(e => e.PathName.Split("/").Last()).ToList();
        /*var refTypes =*/ reader.ReadList(_ => ReadType(reader, enableTypeTree, true));
        /*var userInformation =*/ reader.ReadStringToNull();

        var objects = info.ToDictionary(i => i.Key, i => ReadObject(i.Value, reader));
        foreach (var pPtr in reader.Pointers)
            pPtr.Resolve1(objects, externals);
        return objects;
    }
    
    private static SerializedType ReadType(ObjectReader reader, bool enableTypeTree, bool isRefType) {
        var type = new SerializedType();
        type.ClassId = reader.ReadInt32();
        type.IsStrippedType = reader.ReadBoolean();
        type.ScriptTypeIndex = reader.ReadInt16();
        if (isRefType && type.ScriptTypeIndex >= 0 || type.ClassId == 114)
            type.ScriptId = reader.ReadBytes(16);
        type.OldTypeHash = reader.ReadBytes(16);
        if (!enableTypeTree) return type;
                
        var numberOfNodes = reader.ReadInt32();
        /*var stringBufferSize =*/ reader.ReadInt32();
        type.Nodes = Range(0, numberOfNodes).Select(_ => new TypeTreeNode {
            Version = reader.ReadUInt16(),
            Level = reader.ReadByte(),
            TypeFlags = reader.ReadByte(),
            TypeStrOffset = reader.ReadUInt32(),
            NameStrOffset = reader.ReadUInt32(),
            ByteSize = reader.ReadInt32(),
            Index = reader.ReadInt32(),
            MetaFlag = reader.ReadInt32(),
            RefTypeHash = reader.ReadUInt64() // >=19
        }).ToList();
        foreach (var node in type.Nodes) {
            node.Type = ReadString(reader, node.TypeStrOffset);
            node.Name = ReadString(reader, node.NameStrOffset);
        }

        if (isRefType) {
            type.KlassName = reader.ReadStringToNull();
            type.NameSpace = reader.ReadStringToNull();
            type.AsmName = reader.ReadStringToNull();
        } else {
            type.TypeDependencies = reader.ReadArray(_ => reader.ReadInt32());
        }
        return type;
        
        string ReadString(ObjectReader r, uint value) {
            if ((value & 0x80000000) != 0) // isOffset
                return value.ToString();
            r.BaseStream.Position = value;
            return r.ReadStringToNull();
        }
    }
    
    private static object ReadObject(ObjectInfo o, ObjectReader reader) {
        reader.BaseStream.Position = o.ByteStart;
        // Console.WriteLine($"Reading {o}");
        return o.Type.ClassId switch {
            1 => GameObject.Parse(reader),
            4 => Transform.Parse(reader),
            21 => new Material(reader),
            23 => new MeshRenderer(reader, o.Type.OldTypeHash),
            28 => new Texture2D(reader),
            33 => MeshFilter.Parse(reader),
            43 => new Mesh(reader),
            // 48 => new Shader(reader),
            90 => new Avatar(reader),
            95 => new Animator(reader),
            // 111 => new Animation(reader),
            114 => new MonoBehaviour(reader),
            115 => new MonoScript(reader),
            137 => new SkinnedMeshRenderer(reader, o.Type.OldTypeHash),
            142 => AssetBundle.Parse(reader),
            _ => $"Unknown classId:{o.Type.ClassId}"
        };
    }

    private record DirNode(string Path, bool Flags, long Offset, int Size);
    private record StorageBlock(int CompressedSize, int UncompressedSize);
}

public record SerializedType {
    public int ClassId;
    public bool IsStrippedType;
    public short ScriptTypeIndex = -1;
    public byte[] ScriptId; // Hash128
    public byte[] OldTypeHash; // Hash128
    public List<TypeTreeNode> Nodes;
    public int[] TypeDependencies;
    public string KlassName;
    public string NameSpace;
    public string AsmName;

    public bool Match(params string[] hashes) => hashes.Any(x => x == Convert.ToHexString(OldTypeHash));
}

public class TypeTreeNode {
    public string Type;
    public string Name;
    public int ByteSize;
    public int Index;
    public int TypeFlags; //m_IsArray
    public int Version;
    public int MetaFlag;
    public int Level;
    public uint TypeStrOffset;
    public uint NameStrOffset;
    public ulong RefTypeHash;
}

public record ObjectInfo(long ByteStart, uint ByteSize, SerializedType Type);

public class LocalSerializedObjectIdentifier {
    public int LocalSerializedFileIndex;
    public long LocalIdentifierInFile;
}

public record FileIdentifier {
    public string TempEmpty;
    public Guid Guid;
    public int Type; // kNonAssetType=0, kDeprecatedCachedAssetType=1, kSerializedAssetType=2, kMetaAssetType=3
    public string PathName;
}
