using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using static System.Linq.Enumerable;

namespace Nostradamus;

public class Cab {
    public readonly Dictionary<long, object> Objects;
    public readonly List<string> Externals;
    
    public Cab(Stream stream) {
        var reader = new ObjectReader(stream);
        
        var metadataSize = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var fileSize = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var version = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)); // 21
        var dataOffset = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var isBigEndian = reader.ReadBoolean();
        var reserved = reader.ReadBytes(3);
        var unityVersion = reader.ReadStringToNull();
        var targetPlatform = reader.ReadInt32(); // 19 (StandaloneWindows64)

        var enableTypeTree = reader.ReadBoolean();
        var types = Range(0, reader.ReadInt32())
            .Select(_ => ReadType(reader, enableTypeTree, false)).ToList();
        var objCount = reader.ReadInt32();
        reader.AlignStream(); // don't ask me, I don't know either
        var info = Range(0, objCount).ToDictionary(_ => reader.ReadInt64(), _ =>
            new ObjectInfo(dataOffset + reader.ReadUInt32(), reader.ReadUInt32(), types[reader.ReadInt32()]));
        var scriptTypes = Range(0, reader.ReadInt32()).Select(_ => new LocalSerializedObjectIdentifier {
            LocalSerializedFileIndex = reader.ReadInt32(),
            LocalIdentifierInFile = reader.ReadInt64()
        }).ToList();
        var externals = reader.ReadList(_ => new FileIdentifier {
            TempEmpty = reader.ReadStringToNull(),
            Guid = new Guid(reader.ReadBytes(16)),
            Type = reader.ReadInt32(),
            PathName = reader.ReadStringToNull()
        });
        var refTypes = Range(0, reader.ReadInt32())
            .Select(_ => ReadType(reader, enableTypeTree, true)).ToList();
        var userInformation = reader.ReadStringToNull();

        Objects = info.ToDictionary(i => i.Key, i => ReadObject(i.Value, reader));
        Externals = externals.Select(e => e.PathName.Split("/").Last()).ToList();
        ResolveInternalPointers();
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
        var stringBufferSize = reader.ReadInt32();
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

        using var memoryStream = new MemoryStream(reader.ReadBytes(stringBufferSize));
        using (var stringBufferReader = new ObjectReader(memoryStream)) {
            foreach (var node in type.Nodes) {
                node.Type = ReadString(stringBufferReader, node.TypeStrOffset);
                node.Name = ReadString(stringBufferReader, node.NameStrOffset);
            }
        }
                
        if (isRefType) {
            type.KlassName = reader.ReadStringToNull();
            type.NameSpace = reader.ReadStringToNull();
            type.AsmName = reader.ReadStringToNull();
        } else {
            type.TypeDependencies = Range(0, reader.ReadInt32()).Select(_ => reader.ReadInt32()).ToArray();
        }
        return type;
        
        string ReadString(ObjectReader r, uint value) {
            if ((value & 0x80000000) == 0) { // isOffset
                r.BaseStream.Position = value;
                return r.ReadStringToNull();
            }
            var offset = value & 0x7FFFFFFF;
            return StringBuffer.TryGetValue(offset, out var str) ? str : offset.ToString();
        }
    }

    private static readonly Dictionary<uint, string> StringBuffer = new() {
        {0, "AABB"},
        {5, "AnimationClip"},
        {19, "AnimationCurve"},
        {34, "AnimationState"},
        {49, "Array"},
        {55, "Base"},
        {60, "BitField"},
        {69, "bitset"},
        {76, "bool"},
        {81, "char"},
        {86, "ColorRGBA"},
        {96, "Component"},
        {106, "data"},
        {111, "deque"},
        {117, "double"},
        {124, "dynamic_array"},
        {138, "FastPropertyName"},
        {155, "first"},
        {161, "float"},
        {167, "Font"},
        {172, "GameObject"},
        {183, "Generic Mono"},
        {196, "GradientNEW"},
        {208, "GUID"},
        {213, "GUIStyle"},
        {222, "int"},
        {226, "list"},
        {231, "long long"},
        {241, "map"},
        {245, "Matrix4x4f"},
        {256, "MdFour"},
        {263, "MonoBehaviour"},
        {277, "MonoScript"},
        {288, "m_ByteSize"},
        {299, "m_Curve"},
        {307, "m_EditorClassIdentifier"},
        {331, "m_EditorHideFlags"},
        {349, "m_Enabled"},
        {359, "m_ExtensionPtr"},
        {374, "m_GameObject"},
        {387, "m_Index"},
        {395, "m_IsArray"},
        {405, "m_IsStatic"},
        {416, "m_MetaFlag"},
        {427, "m_Name"},
        {434, "m_ObjectHideFlags"},
        {452, "m_PrefabInternal"},
        {469, "m_PrefabParentObject"},
        {490, "m_Script"},
        {499, "m_StaticEditorFlags"},
        {519, "m_Type"},
        {526, "m_Version"},
        {536, "Object"},
        {543, "pair"},
        {548, "PPtr<Component>"},
        {564, "PPtr<GameObject>"},
        {581, "PPtr<Material>"},
        {596, "PPtr<MonoBehaviour>"},
        {616, "PPtr<MonoScript>"},
        {633, "PPtr<Object>"},
        {646, "PPtr<Prefab>"},
        {659, "PPtr<Sprite>"},
        {672, "PPtr<TextAsset>"},
        {688, "PPtr<Texture>"},
        {702, "PPtr<Texture2D>"},
        {718, "PPtr<Transform>"},
        {734, "Prefab"},
        {741, "Quaternionf"},
        {753, "Rectf"},
        {759, "RectInt"},
        {767, "RectOffset"},
        {778, "second"},
        {785, "set"},
        {789, "short"},
        {795, "size"},
        {800, "SInt16"},
        {807, "SInt32"},
        {814, "SInt64"},
        {821, "SInt8"},
        {827, "staticvector"},
        {840, "string"},
        {847, "TextAsset"},
        {857, "TextMesh"},
        {866, "Texture"},
        {874, "Texture2D"},
        {884, "Transform"},
        {894, "TypelessData"},
        {907, "UInt16"},
        {914, "UInt32"},
        {921, "UInt64"},
        {928, "UInt8"},
        {934, "unsigned int"},
        {947, "unsigned long long"},
        {966, "unsigned short"},
        {981, "vector"},
        {988, "Vector2f"},
        {997, "Vector3f"},
        {1006, "Vector4f"},
        {1015, "m_ScriptingClassIdentifier"},
        {1042, "Gradient"},
        {1051, "Type*"},
        {1057, "int2_storage"},
        {1070, "int3_storage"},
        {1083, "BoundsInt"},
        {1093, "m_CorrespondingSourceObject"},
        {1121, "m_PrefabInstance"},
        {1138, "m_PrefabAsset"},
        {1152, "FileSize"},
        {1161, "Hash128"}
    };

    private static object ReadObject(ObjectInfo o, ObjectReader reader) {
        reader.BaseStream.Position = o.ByteStart;
        // Console.WriteLine($"Reading {o}");
        return o.Type.ClassId switch {
            1 => GameObject.Parse(reader),
            4 => Transform.Parse(reader),
            23 => new MeshRenderer(reader, o.Type.OldTypeHash),
            33 => MeshFilter.Parse(reader),
            43 => new Mesh(reader),
            90 => new Avatar(reader),
            95 => new Animator(reader),
            // 111 => new Animation(reader),
            114 => MonoBehaviour.Parse(reader),
            115 => MonoScript.Parse(reader),
            137 => new SkinnedMeshRenderer(reader, o.Type.OldTypeHash),
            142 => AssetBundle.Parse(reader),
            _ => $"Unknown classId:{o.Type.ClassId}"
        };
    }

    private void ResolveInternalPointers() {
        foreach (var o in Objects.Values) {
            if (o is Transform tt) {
                tt.GameObject.Resolve(Objects, Externals);
                tt.Children.ForEach(c => c.Resolve(Objects, Externals));
            // } else if (o is GameObject g) {
            //     g.Components.ForEach(c => c.Resolve(Objects, Externals));
            } else if (o is SkinnedMeshRenderer smr) {
                smr.Materials.ForEach(m => m.Resolve(Objects, Externals));
            } else if (o is AssetBundle ab) {
                ab.PreloadTable.ForEach(c => c.Resolve(Objects, Externals));
            }
        }
    }
}


public record XForm(Vector3 Translate, Quaternion Rotate, Vector3 Scale) {
    public override string ToString() => string.Join(" ",
        Translate is { X: 0, Y: 0, Z: 0 } ? "⊕" : "",
        Rotate is { W: 1, X: 0, Y: 0, Z: 0 } ? "↺" : "",
        Scale is { X: 0, Y: 0, Z: 0 } ? "⇲" : "");
};
    
public class ObjectReader(Stream input) : BinaryReader(input) {
    public PPtr<T> ReadPointer<T>() {
        var fileId = ReadInt32();
        return new PPtr<T>(fileId, ReadInt64());
    }

    public T[] ReadArray<T>(Func<int,T> fn) => Enumerable.Range(0, ReadInt32()).Select(fn).ToArray();
    public List<T> ReadList<T>(Func<int,T> fn) => Enumerable.Range(0, ReadInt32()).Select(fn).ToList();
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

    public string ReadStringToNull(int maxLength = 32767) {
        var bytes = new List<byte>();
        var count = 0;
        while (BaseStream.Length - BaseStream.Position > 0 && count < maxLength) {
            var b = ReadByte();
            if (b == 0) break;
            bytes.Add(b);
            count++;
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }
    
    public string ReadAlignedString() => Align(() => Encoding.UTF8.GetString(ReadBytes(ReadInt32())));

    public void AlignStream(int alignment = 4) {
        var mod = BaseStream.Position % alignment;
        if (mod != 0) BaseStream.Position += alignment - mod;
    }
    public T Align<T>(Func<T> fn, int alignment = 4) {
        var ret = fn();
        AlignStream(alignment);
        return ret;
    }

    public List<string> Externals { get; set; }
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

public record PPtr<T>(int FileId, long PathId) {
    public T? Val;
    public string? ExtPath;

    public void Resolve(Dictionary<long, object> objects, List<string> externals) {
        if (FileId == 0) Val = (T)objects[PathId];
        else ExtPath = externals[FileId - 1];
    }
    
    public override string ToString() => $"{PathId:x16}::{Val?.ToString() ?? ExtPath ?? ""}";
}

public record GameObject(List<PPtr<GameComponent>> Components, int Layer, string Name) {
    public static GameObject Parse(ObjectReader r) =>
        new GameObject(r.ReadList(_ => r.ReadPointer<GameComponent>()), r.ReadInt32(), r.ReadAlignedString());

    public override string ToString() => $"GameObject('{Name}' l={Layer} Components=[{string.Join(",", Components)}])";
}

public record Transform(PPtr<GameObject> GameObject, XForm X, List<PPtr<Transform>> Children, PPtr<Transform> Father) : GameComponent {
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

    public override string ToString() => $"Transform(g={GameObject} Father={Father} Children=[{string.Join(",", Children)}]";
}