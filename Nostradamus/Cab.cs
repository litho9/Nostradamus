using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using static System.Linq.Enumerable;

namespace Nostradamus;

public class Cab {
    public readonly Dictionary<long, object> Objects;
    
    public Cab(Stream stream) {
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

        Objects = info.ToDictionary(i => i.Key, i => ReadObject(i.Value, reader));
        foreach (var pPtr in reader.Pointers)
            pPtr.Resolve1(Objects, externals);
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
            48 => new Shader(reader),
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


public record XForm(Vector3 Translate, Quaternion Rotate, Vector3 Scale) {
    public override string ToString() => string.Join(" ",
        Translate is { X: 0, Y: 0, Z: 0 } ? "⊕" : "",
        Rotate is { W: 1, X: 0, Y: 0, Z: 0 } ? "↺" : "",
        Scale is { X: 0, Y: 0, Z: 0 } ? "⇲" : "");
};
    
public class ObjectReader(Stream input) : BinaryReader(input) {
    public readonly List<PPtr0> Pointers = [];

    public PPtr<T> ReadPointer<T>() {
        var ptr = new PPtr<T>(ReadInt32(), ReadInt64());
        Pointers.Add(ptr);
        return ptr;
    }

    public T[] ReadArray<T>(Func<int,T> fn) => Range(0, ReadInt32()).Select(fn).ToArray();
    public List<T> ReadList<T>(Func<int,T> fn) => Range(0, ReadInt32()).Select(fn).ToList();
    public Vector3 ReadVector3() => new(ReadSingle(), ReadSingle(), ReadSingle());
    public Vector4 ReadVector4() => new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
    public Quaternion ReadQuaternion() => new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
    public Vector2 ReadVector2() => new(ReadSingle(), ReadSingle());
    public Matrix4x4 ReadMatrix4X4() => new(
        ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
        ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
        ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
        ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle()
    );
    public XForm ReadXForm() => new(ReadVector3(), ReadQuaternion(), ReadVector3());

    private IEnumerable<byte> BytesToNull(byte b = 0) { while ((b = ReadByte()) != 0) yield return b; }
    public string ReadStringToNull() => Encoding.UTF8.GetString(BytesToNull().ToArray());
    public string ReadAlignedString() => Align(() => Encoding.UTF8.GetString(ReadBytes(ReadInt32())));

    public T Align<T>(Func<T> fn, int alignment = 4) {
        var ret = fn();
        var mod = BaseStream.Position % alignment;
        if (mod != 0) BaseStream.Position += alignment - mod;
        return ret;
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
    public int Type; // enum { kNonAssetType = 0, kDeprecatedCachedAssetType = 1, kSerializedAssetType = 2, kMetaAssetType = 3 };
    public string PathName;
}

public abstract record PPtr0(int FileId, long PathId) { // c# is dumb too
    public abstract void Resolve1(Dictionary<long, object> objects, List<string> externals);
}

public record PPtr<T>(int FileId, long PathId) : PPtr0(FileId, PathId) { 
    public T? Val;
    public string? ExtPath;

    public override void Resolve1(Dictionary<long, object> objects, List<string> externals) {
        if (FileId != 0) ExtPath = externals[FileId - 1];
        else if (objects.TryGetValue(PathId, out var o)) Val = (T)o;
        else Console.WriteLine($"[CAB] '{this}' points to nothing.");
    }

    public override string ToString() => $"{PathId:x16}::{Val?.ToString() ?? ExtPath ?? ""}";
}

public record GameObject(List<PPtr<object>> Components, int Layer, string Name) {
    public static GameObject Parse(ObjectReader r) =>
        new(r.ReadList(_ => r.ReadPointer<object>()), r.ReadInt32(), r.ReadAlignedString());

    public override string ToString() => $"GameObject('{Name}' l={Layer} Components=[{string.Join(",", Components)}])";
}

public record Transform(PPtr<GameObject> GameObject, XForm X, List<PPtr<Transform>> Children, PPtr<Transform> Father) {
    public static Transform Parse(ObjectReader r) {
        var gameObject = r.ReadPointer<GameObject>();
        var localRotation = r.ReadQuaternion();
        var localPosition = r.ReadVector3();
        var localScale = r.ReadVector3();
        var children = r.ReadList(_ => r.ReadPointer<Transform>());
        var father = r.ReadPointer<Transform>();
        var x = new XForm(localPosition, localRotation, localScale);
        return new Transform(gameObject, x, children, father);
    }

    public override string ToString() => $"Transform(X={X}, Children=[{string.Join(",", Children)}])";
}