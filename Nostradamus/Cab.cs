using System.Buffers.Binary;
using static System.Linq.Enumerable;

namespace Nostradamus;

public class Cab {
    public static Dictionary<long, object> ReadCab(ObjectReader reader) {
        /*var metadataSize =*/ BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        /*var fileSize =*/ BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var version = (int)BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)); // 21
        var dataOffset = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        /*var isBigEndian =*/ reader.Align(reader.ReadBoolean);
        /*var unityVersion =*/ reader.ReadStringToNull();
        /*var targetPlatform =*/ reader.ReadInt32(); // 19 (StandaloneWindows64)

        var enableTypeTree = reader.ReadBoolean();
        var types = reader.ReadList(_ => ReadType(reader, enableTypeTree, false, version));
        var objCount = reader.Align(reader.ReadInt32); // don't ask me why align, I don't know either
        var info = Range(0, objCount).ToDictionary(_ => reader.ReadInt64(), _ =>
            new ObjectInfo(dataOffset + reader.ReadUInt32(), reader.ReadUInt32(), types[reader.ReadInt32()]));
        /*var scriptTypes =*/ reader.ReadList(_ => new LocalSerializedObjectIdentifier(
            reader.ReadInt32(), reader.ReadInt64()));
        var externals = reader.ReadList(_ => new FileIdentifier {
            TempEmpty = reader.ReadStringToNull(),
            Guid = new Guid(reader.ReadBytes(16)),
            Type = reader.ReadInt32(),
            PathName = reader.ReadStringToNull()
        }).Select(e => e.PathName.Split("/").Last()).ToList();
        if (version >= 20)
            /*var refTypes =*/ reader.ReadList(_ => ReadType(reader, enableTypeTree, true, version));
        /*var userInformation =*/ reader.ReadStringToNull();

        var objects = info.ToDictionary(i => i.Key, i => ReadObject(i.Value, reader));
        foreach (var pPtr in reader.Pointers)
            pPtr.Resolve1(objects, externals);
        return objects;
    }
    
    private static SerializedType ReadType(ObjectReader reader, bool enableTypeTree, bool isRefType, int version) {
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
        type.Nodes = Range(0, numberOfNodes).Select(_ => new TypeTreeNode(reader.ReadUInt16(),
            reader.ReadByte(), reader.ReadByte(), reader.ReadUInt32(), reader.ReadUInt32(),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), version>=19 ? reader.ReadUInt64() : 0
        )).ToList();
        foreach (var node in type.Nodes) {
            node.Type = ReadString(reader, node.TypeStrOffset);
            node.Name = ReadString(reader, node.NameStrOffset);
        }

        if (version >= 21) {
            if (isRefType) {
                type.KlassName = reader.ReadStringToNull();
                type.NameSpace = reader.ReadStringToNull();
                type.AsmName = reader.ReadStringToNull();
            } else {
                type.TypeDependencies = reader.ReadArray(_ => reader.ReadInt32());
            }
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
        Console.WriteLine($"\e[32m[cab] Reading Obj_{o.ByteStart}..{o.ByteSize}_{o.Type.ClassId}\e[0m");
        return o.Type.ClassId switch {
            1 => GameObject.Parse(reader),
            4 => Transform.Parse(reader),
            21 => new Material(reader),
            23 => new MeshRenderer(reader, o.Type.OldTypeHash),
            28 => new Texture2D(reader),
            33 => MeshFilter.Parse(reader),
            43 => new Mesh(reader),
            48 => new Shader(reader, o.Type.OldTypeHash),
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

public record TypeTreeNode(int Version, int Level, int TypeFlags, //m_IsArray
    uint TypeStrOffset, uint NameStrOffset,
    int ByteSize, int Index, int MetaFlag, ulong RefTypeHash) {
    public string Type = "";
    public string Name = "";
}

public record ObjectInfo(long ByteStart, uint ByteSize, SerializedType Type);

public record LocalSerializedObjectIdentifier(int FileIndex, long IdInFile);

public record FileIdentifier {
    public string TempEmpty;
    public Guid Guid;
    public int Type; // kNonAssetType=0, kDeprecatedCachedAssetType=1, kSerializedAssetType=2, kMetaAssetType=3
    public string PathName;
}

public record DirNode(string Path, bool Flags, long Offset, int Size);
public record StorageBlock(int CompressedSize, int UncompressedSize);
