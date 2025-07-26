using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using static System.Linq.Enumerable;

namespace Nostradamus;

public class Cab {
    private ObjectReader Reader;
    public readonly Dictionary<long, ObjectInfo> Objects;
    private List<SerializedType> Types;
    public readonly List<string> Externals;
    
    public Cab(Stream stream) {
        var reader = new ObjectReader(stream);
        Reader = reader;
        
        var metadataSize = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var fileSize = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var version = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4)); // 21
        var dataOffset = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        var isBigEndian = reader.ReadBoolean();
        var reserved = reader.ReadBytes(3);
        var unityVersion = reader.ReadStringToNull();
        var targetPlatform = reader.ReadInt32(); // 19 (StandaloneWindows64)

        var enableTypeTree = reader.ReadBoolean();
        Types = Range(0, reader.ReadInt32())
            .Select(_ => ReadType(reader, enableTypeTree, false)).ToList();
        var objCount = reader.ReadInt32();
        reader.AlignStream(); // don't ask me, I don't know either
        Objects = Range(0, objCount).ToDictionary(_ => reader.ReadInt64(), _ =>
            new ObjectInfo(dataOffset + reader.ReadUInt32(), reader.ReadUInt32(), Types[reader.ReadInt32()]));
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
        Externals = externals.Select(e => e.PathName.Split("/").Last()).ToList();
        var refTypes = Range(0, reader.ReadInt32())
            .Select(_ => ReadType(reader, enableTypeTree, true)).ToList();
        var userInformation = reader.ReadStringToNull();
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

    public object ReadObject(ObjectInfo o) {
        var reader = Reader;
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
            _ => $"Unknown classId:{o.Type.ClassId}"
        };
    }
}


public record XForm(Vector3 Translate, Quaternion Rotate, Vector3 Scale);
    
public class ObjectReader(Stream input) : BinaryReader(input) {
    public PPtr<T> ReadPointer<T>() => new(ReadInt32(), ReadInt64());
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
    public override string ToString() => $"{PathId:x16}::{FileId}";
}

public class PackedFloatVector {
    public uint m_NumItems;
    public float m_Range;
    public float m_Start;
    public byte[] m_Data;
    public byte m_BitSize;

    public PackedFloatVector(ObjectReader reader) {
        m_NumItems = reader.ReadUInt32();
        m_Range = reader.ReadSingle();
        m_Start = reader.ReadSingle();
        m_Data = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();
        m_BitSize = reader.ReadByte();
        reader.AlignStream();
    }
}

public class PackedIntVector {
    public uint m_NumItems;
    public byte[] m_Data;
    public byte m_BitSize;

    public PackedIntVector(ObjectReader reader) {
        m_NumItems = reader.ReadUInt32();
        m_Data = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();
        m_BitSize = reader.ReadByte();
        reader.AlignStream();
    }
}

public abstract record GameComponent {
    // public PPtr<GameObject> GameObject = new(reader.ReadInt32(), reader.ReadInt64());
}
public interface NamedObject;

public record GameObject(List<PPtr<GameComponent>> Components, int Layer, string Name) {
    public static GameObject Parse(ObjectReader r) =>
        new GameObject(r.ReadList(_ => r.ReadPointer<GameComponent>()), r.ReadInt32(), r.ReadAlignedString());

    public override string ToString() => $"GameObject('{Name}' l={Layer} Components=[{string.Join(",", Components)}])";
}

public record Animator(ObjectReader reader) {
    public PPtr<GameObject> GameObject = reader.ReadPointer<GameObject>();
    public bool Enabled = reader.Align(reader.ReadBoolean);
    
    public PPtr<Avatar> AvatarPtr = new(reader.ReadInt32(), reader.ReadInt64());
    public PPtr<NamedObject> ControllerPtr = new(reader.ReadInt32(), reader.ReadInt64()); // RuntimeAnimatorController
    public int CullingMode = reader.ReadInt32();
    public int UpdateMode = reader.ReadInt32(); // 4.5 and up
    
    public bool ApplyRootMotion = reader.ReadBoolean();
    public bool LinearVelocityBlending = reader.Align(reader.ReadBoolean); // 5.0 and up
    
    public bool HasTransformHierarchy = reader.ReadBoolean(); // 4.3 and up
    public bool AllowConstantClipSamplingOptimization = reader.ReadBoolean(); // 4.5 and up
    public bool KeepAnimatorControllerStateOnDisable = reader.Align(reader.ReadBoolean); // 2018 and up
}

public class Node(ObjectReader reader) {
    public int ParentId = reader.ReadInt32();
    public int AxesId = reader.ReadInt32();
}
public class Axes(ObjectReader r) {
    public Vector4 PreQ = r.ReadVector4();
    public Vector4 PostQ = r.ReadVector4();
    public Vector3 Sgn = r.ReadVector3();
    public Vector3 LimitMin = r.ReadVector3();
    public Vector3 LimitMax = r.ReadVector3();
    public float Length = r.ReadSingle();
    public uint Type = r.ReadUInt32();
}
public class Skeleton(ObjectReader reader) {
    public List<Node> Node = reader.ReadList(_ => new Node(reader));
    public uint[] Id = reader.ReadArray(_ => reader.ReadUInt32());
    public List<Axes> AxesArray = reader.ReadList(_ => new Axes(reader));
    public XForm[] Pose = reader.ReadArray(_ => reader.ReadXForm());
}
public class Human(ObjectReader reader) {
    public XForm RootX = reader.ReadXForm();
    public Skeleton Skeleton = new(reader);
    public int[] LeftHand = reader.ReadArray(_ => reader.ReadInt32());
    public int[] RightHand = reader.ReadArray(_ => reader.ReadInt32());
    public int[] HumanBoneIndex = reader.ReadArray(_ => reader.ReadInt32());
    public float[] HumanBoneMass = reader.ReadArray(_ => reader.ReadSingle());
    public float Scale = reader.ReadSingle();
    public float ArmTwist = reader.ReadSingle();
    public float ForeArmTwist = reader.ReadSingle();
    public float UpperLegTwist = reader.ReadSingle();
    public float LegTwist = reader.ReadSingle();
    public float ArmStretch = reader.ReadSingle();
    public float LegStretch = reader.ReadSingle();
    public float FeetSpacing = reader.ReadSingle();
    public bool HasLeftHand = reader.ReadBoolean();
    public bool HasRightHand = reader.ReadBoolean();
    public bool HasTDoF = reader.Align(reader.ReadBoolean); // 5.2 and up
}
public sealed class Avatar(ObjectReader reader) : NamedObject {
    public readonly string Name = reader.ReadAlignedString();
    public uint Size = reader.ReadUInt32();
    public Skeleton AvatarSkeleton = new(reader);
    public XForm[] DefaultPose = reader.ReadArray(_ => reader.ReadXForm()); // 4.3 and up
    public uint[] SkeletonNameIdArray = reader.ReadArray(_ => reader.ReadUInt32()); // 4.3 and up
    public Human Human = new(reader);
    public int[] HumanSkeletonIndexArray = reader.ReadArray(_ => reader.ReadInt32());
    public int[] HumanSkeletonReverseIndexArray = reader.ReadArray(_ => reader.ReadInt32()); // 4.3 and up
    public int RootMotionBoneIndex = reader.ReadInt32();
    public XForm RootMotionBoneX = reader.ReadXForm();
    public Skeleton RootMotionSkeleton = new(reader); // 4.3 and up
    public int[] RootMotionSkeletonIndexArray = reader.ReadArray(_ => reader.ReadInt32());
    public Dictionary<uint, string> TOS = reader.ReadArray(_ => new KeyValuePair<uint,string>(reader.ReadUInt32(), reader.ReadAlignedString())).ToDictionary();
    //HumanDescription m_HumanDescription 2019 and up
}

public record MonoBehaviour(PPtr<GameObject> GameObject, bool Enabled, PPtr<MonoScript> Script, string Name): GameComponent {
    public static MonoBehaviour Parse(ObjectReader r) => new MonoBehaviour(
        new PPtr<GameObject>(r.ReadInt32(), r.ReadInt64()),
        r.Align(r.ReadBoolean),
        new PPtr<MonoScript>(r.ReadInt32(), r.ReadInt64()),
        r.Align(() => Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32())))
    );
}

public record MonoScript(string Name, int ExecutionOrder, byte[] PropertiesHash, string ClassName, string Namespace, string AssemblyName, bool IsEditorScript) : NamedObject {
    public static MonoScript Parse(ObjectReader reader) =>
        new(reader.ReadAlignedString(),
            reader.ReadInt32(), // 3.4 and up
            reader.ReadBytes(16),
            reader.ReadAlignedString(),
            reader.ReadAlignedString(), // 3.0 and up
            reader.ReadAlignedString(),
            reader.ReadBoolean());
}

public record Transform(PPtr<GameObject> GameObject, XForm X, List<PPtr<Transform>> Children, PPtr<Transform> Father) { // : GameComponent
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

public record AABB(Vector3 Center, Vector3 Extent) {
    public static AABB Parse(ObjectReader r) => new(r.ReadVector3(), r.ReadVector3());
}
public class SubMesh(ObjectReader reader) {
    public uint firstByte = reader.ReadUInt32();
    public uint indexCount = reader.ReadUInt32();
    public int topology = reader.ReadInt32(); // (GfxPrimitiveType)
    public uint baseVertex = reader.ReadUInt32();
    public uint firstVertex = reader.ReadUInt32();
    public uint vertexCount = reader.ReadUInt32();
    public AABB localAABB = AABB.Parse(reader);
}
public class BlendShapeVertex(ObjectReader reader) {
    public Vector3 vertex = reader.ReadVector3();
    public Vector3 normal = reader.ReadVector3();
    public Vector3 tangent = reader.ReadVector3();
    public uint index = reader.ReadUInt32();
}
public class MeshBlendShape(ObjectReader reader) {
    public uint firstVertex = reader.ReadUInt32();
    public uint vertexCount = reader.ReadUInt32();
    public bool hasNormals = reader.ReadBoolean();
    public bool hasTangents = reader.Align(reader.ReadBoolean);
}
public class MeshBlendShapeChannel(ObjectReader reader) {
    public string name = reader.ReadAlignedString();
    public uint nameHash = reader.ReadUInt32();
    public int frameIndex = reader.ReadInt32();
    public int frameCount = reader.ReadInt32();
}
public class BlendShapeData(ObjectReader r) {
    public List<BlendShapeVertex> vertices = Range(0, r.ReadInt32()).Select(_ => new BlendShapeVertex(r)).ToList();
    public List<MeshBlendShape> shapes = Range(0, r.ReadInt32()).Select(_ => new MeshBlendShape(r)).ToList();
    public List<MeshBlendShapeChannel> channels = Range(0, r.ReadInt32()).Select(_ => new MeshBlendShapeChannel(r)).ToList();
    public float[] fullWeights = Range(0, r.ReadInt32()).Select(_ => r.ReadSingle()).ToArray();
}

public record ChannelInfo(byte Stream, byte Offset, byte Format, byte Dimension);

public class StreamInfo(ObjectReader reader) {
    public uint channelMask = reader.ReadUInt32();
    public uint offset = reader.ReadUInt32();
    public uint stride = reader.ReadByte();
}

public class VertexData {
    public uint m_VertexCount;
    public List<ChannelInfo> m_Channels;
    public byte[] m_DataSize;

    public VertexData(ObjectReader reader) {
        m_VertexCount = reader.ReadUInt32();
        m_Channels = Range(0, reader.ReadInt32()).Select(_ => new ChannelInfo(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            (byte)(reader.ReadByte() & 0xF)
        )).ToList();
        m_DataSize = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();
    }
}

public class CompressedMesh(ObjectReader reader) {
    public PackedFloatVector m_Vertices = new(reader);
    public PackedFloatVector m_UV = new(reader);
    public PackedFloatVector m_Normals = new(reader);
    public PackedFloatVector m_Tangents = new(reader);
    public PackedIntVector m_Weights = new(reader);
    public PackedIntVector m_NormalSigns = new(reader);
    public PackedIntVector m_TangentSigns = new(reader);
    public PackedFloatVector m_FloatColors = new(reader);
    public PackedIntVector m_BoneIndices = new(reader);
    public PackedIntVector m_Triangles = new(reader);
    public uint m_UVInfo = reader.ReadUInt32();
}

public class StreamingInfo(ObjectReader reader) {
    public long offset = reader.ReadInt64(); // ulong
    public uint size = reader.ReadUInt32();
    public string path = reader.ReadAlignedString();
}

public sealed class Mesh : NamedObject {
    public readonly string Name;
    public List<SubMesh> SubMeshes;
    public BlendShapeData Shapes;
    public Matrix4x4[] BindPose;
    public uint[] BoneNameHashes;
    private bool Use16BitIndices = true;
    private uint[] IndexBuffer;
    private VertexData VertexData;
    private CompressedMesh CompressedMesh;
    private StreamingInfo m_StreamData;

    // public int m_VertexCount;
    // public float[] m_Vertices;
    // public List<BoneWeights4> m_Skin;
    // public float[] m_Normals;
    // public float[] m_Colors;
    // public float[] m_UV0;
    // public float[] m_UV1;
    // public float[] m_UV2;
    // public float[] m_UV3;
    // public float[] m_UV4;
    // public float[] m_UV5;
    // public float[] m_UV6;
    // public float[] m_UV7;
    // public float[] m_Tangents;
    // private bool m_CollisionMeshBaked = false;
    // public List<uint> m_Indices = new List<uint>();

    public Mesh(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        SubMeshes = Range(0, reader.ReadInt32()).Select(_ => new SubMesh(reader)).ToList();
        Shapes = new BlendShapeData(reader);
        BindPose = Range(0, reader.ReadInt32()).Select(_ => new Matrix4x4(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
        )).ToArray();
        BoneNameHashes = Range(0, reader.ReadInt32()).Select(_ => reader.ReadUInt32()).ToArray();
        var RootBoneNameHash = reader.ReadUInt32();
        var BonesAABB = reader.ReadList(_ => new KeyValuePair<Vector3,Vector3>(reader.ReadVector3(), reader.ReadVector3()));
        var VariableBoneCountWeights = Range(0, reader.ReadInt32()).Select(_ => reader.ReadUInt32()).ToArray();

        var meshCompression = reader.ReadBoolean();
        var isReadable = reader.ReadBoolean();
        var keepVertices = reader.ReadBoolean();
        var keepIndices = reader.ReadBoolean();

        Use16BitIndices = reader.ReadInt32() == 0;
        var ibSize = reader.ReadInt32();
        if (Use16BitIndices) {
            IndexBuffer = Range(0, ibSize / 2).Select(_ => (uint)reader.ReadUInt16()).ToArray();
            reader.AlignStream();
        } else {
            IndexBuffer = Range(0, ibSize / 4).Select(_ => reader.ReadUInt32()).ToArray();
        }

        VertexData = new VertexData(reader);
        CompressedMesh = new CompressedMesh(reader);
        reader.BaseStream.Position += 24; //AABB m_LocalAABB
        int m_MeshUsageFlags = reader.ReadInt32();
        var m_BakedConvexCollisionMesh = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();
        var m_BakedTriangleCollisionMesh = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();

        var m_MeshMetrics = new float[2];
        m_MeshMetrics[0] = reader.ReadSingle();
        m_MeshMetrics[1] = reader.ReadSingle();
        
        // if (reader.Game.Type.IsZZZ())
        var m_MetricsDirty = reader.ReadBoolean();
        reader.AlignStream();
        var m_CloseMeshDynamicCompression = reader.ReadBoolean();
        reader.AlignStream();
        var m_CompressLevelVertexData = reader.ReadInt32();
        var m_CompressLevelNormalAndTangent = reader.ReadInt32();
        var m_CompressLevelTexCoordinates = reader.ReadInt32();
        var m_PackSkinDataToUV2UV3 = reader.ReadBoolean();
        reader.AlignStream();
        var m_BakeBVHData = reader.ReadBoolean();
        var m_BakeRefittableBVH = reader.ReadBoolean();
        reader.AlignStream();
        var m_BVHBakeLevels = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();
        var m_BakedBVHSize = reader.ReadUInt64();
        var m_BVHDataBuffer = reader.ReadBytes(reader.ReadInt32());
        reader.AlignStream();

        reader.AlignStream();
        m_StreamData = new StreamingInfo(reader);

        // ProcessData(); TODO
    }
    
    // private void ProcessData()
    // {
    //     if (!string.IsNullOrEmpty(m_StreamData?.path))
    //     {
    //         if (m_VertexData.m_VertexCount > 0)
    //         {
    //             var resourceReader = new ResourceReader(m_StreamData.path, assetsFile, m_StreamData.offset, m_StreamData.size);
    //             m_VertexData.m_DataSize = resourceReader.GetData();
    //         }
    //     }
    //     ReadVertexData(); // 3.5 and up
    //     if (m_CollisionMeshBaked)
    //         return;
    //     DecompressCompressedMesh(); // 2.6.0 and later
    //     GetTriangles();
    // }
}

public class SerializedProperty(ObjectReader reader) {
    public string Name = reader.ReadAlignedString();
    public string Description = reader.ReadAlignedString();
    public string[] Attributes = reader.ReadArray(_ => reader.ReadAlignedString());
    public int Type = reader.ReadInt32(); // SerializedPropertyType
    public uint Flags = reader.ReadUInt32(); // SerializedPropertyFlag
    public Matrix4x4 DefValue = reader.ReadMatrix4X4();
    public string DefaultName = reader.ReadAlignedString();
    public int TexDim = reader.ReadInt32(); // TextureDimension
}

public class ShaderBindChannel(ObjectReader reader) {
    public sbyte source = reader.ReadSByte();
    public sbyte target = reader.ReadSByte();
}

public class ParserBindChannels {
    public List<ShaderBindChannel> Channels;
    public uint SourceMap;

    public ParserBindChannels(ObjectReader reader) {
        Channels = reader.ReadList(_ => new ShaderBindChannel(reader));
        reader.AlignStream();
        SourceMap = reader.ReadUInt32();
    }
}

public class VectorParameter
{
    public int m_NameIndex;
    public int m_Index;
    public int m_ArraySize;
    public sbyte m_Type;
    public sbyte m_Dim;

    public VectorParameter(ObjectReader reader)
    {
        m_NameIndex = reader.ReadInt32();
        m_Index = reader.ReadInt32();
        m_ArraySize = reader.ReadInt32();
        m_Type = reader.ReadSByte();
        m_Dim = reader.ReadSByte();
        reader.AlignStream();
    }
}

public class MatrixParameter
{
    public int m_NameIndex;
    public int m_Index;
    public int m_ArraySize;
    public sbyte m_Type;
    public sbyte m_RowCount;

    public MatrixParameter(ObjectReader reader)
    {
        m_NameIndex = reader.ReadInt32();
        m_Index = reader.ReadInt32();
        m_ArraySize = reader.ReadInt32();
        m_Type = reader.ReadSByte();
        m_RowCount = reader.ReadSByte();
        reader.AlignStream();
    }
}

public class TextureParameter
{
    public int m_NameIndex;
    public int m_Index;
    public int m_SamplerIndex;
    public sbyte m_Dim;

    public TextureParameter(ObjectReader reader)
    {
        m_NameIndex = reader.ReadInt32();
        m_Index = reader.ReadInt32();
        m_SamplerIndex = reader.ReadInt32();
        var m_MultiSampled = reader.ReadBoolean();
        m_Dim = reader.ReadSByte();
        reader.AlignStream();
    }
}

public class BufferBinding
{
    public int m_NameIndex;
    public int m_Index;
    public int m_ArraySize;

    public BufferBinding(ObjectReader reader)
    {
        m_NameIndex = reader.ReadInt32();
        m_Index = reader.ReadInt32();
        m_ArraySize = reader.ReadInt32();
    }
}

public class StructParameter
{
    public List<MatrixParameter> m_MatrixParams;
    public List<VectorParameter> m_VectorParams;

    public StructParameter(ObjectReader reader)
    {
        var m_NameIndex = reader.ReadInt32();
        var m_Index = reader.ReadInt32();
        var m_ArraySize = reader.ReadInt32();
        var m_StructSize = reader.ReadInt32();

        int numVectorParams = reader.ReadInt32();
        m_VectorParams = new List<VectorParameter>();
        for (int i = 0; i < numVectorParams; i++)
        {
            m_VectorParams.Add(new VectorParameter(reader));
        }

        int numMatrixParams = reader.ReadInt32();
        m_MatrixParams = new List<MatrixParameter>();
        for (int i = 0; i < numMatrixParams; i++)
        {
            m_MatrixParams.Add(new MatrixParameter(reader));
        }
    }
}

public class SamplerParameter(ObjectReader reader) {
    public uint sampler = reader.ReadUInt32();
    public int bindPoint = reader.ReadInt32();
}

public class ConstantBuffer
{
    public int m_NameIndex;
    public List<MatrixParameter> m_MatrixParams;
    public List<VectorParameter> m_VectorParams;
    public List<StructParameter> m_StructParams;
    public int m_Size;
    public bool m_IsPartialCB;

    public ConstantBuffer(ObjectReader reader)
    {
        m_NameIndex = reader.ReadInt32();

        int numMatrixParams = reader.ReadInt32();
        m_MatrixParams = new List<MatrixParameter>();
        for (int i = 0; i < numMatrixParams; i++)
        {
            m_MatrixParams.Add(new MatrixParameter(reader));
        }

        int numVectorParams = reader.ReadInt32();
        m_VectorParams = new List<VectorParameter>();
        for (int i = 0; i < numVectorParams; i++)
        {
            m_VectorParams.Add(new VectorParameter(reader));
        }
        int numStructParams = reader.ReadInt32();
        m_StructParams = new List<StructParameter>();
        for (int i = 0; i < numStructParams; i++)
        {
            m_StructParams.Add(new StructParameter(reader));
        }
        m_Size = reader.ReadInt32();
    }
}

public class UAVParameter(ObjectReader reader) {
    public int m_NameIndex = reader.ReadInt32();
    public int m_Index = reader.ReadInt32();
    public int m_OriginalIndex = reader.ReadInt32();
}

public class SerializedProgramParameters
    {
        public List<VectorParameter> m_VectorParams;
        public List<MatrixParameter> m_MatrixParams;
        public List<TextureParameter> m_TextureParams;
        public List<BufferBinding> m_BufferParams;
        public List<ConstantBuffer> m_ConstantBuffers;
        public List<BufferBinding> m_ConstantBufferBindings;
        public List<UAVParameter> m_UAVParams;
        public List<SamplerParameter> m_Samplers;

        public SerializedProgramParameters(ObjectReader reader)
        {
            int numVectorParams = reader.ReadInt32();
            m_VectorParams = new List<VectorParameter>();
            for (int i = 0; i < numVectorParams; i++)
            {
                m_VectorParams.Add(new VectorParameter(reader));
            }

            int numMatrixParams = reader.ReadInt32();
            m_MatrixParams = new List<MatrixParameter>();
            for (int i = 0; i < numMatrixParams; i++)
            {
                m_MatrixParams.Add(new MatrixParameter(reader));
            }

            int numTextureParams = reader.ReadInt32();
            m_TextureParams = new List<TextureParameter>();
            for (int i = 0; i < numTextureParams; i++)
            {
                m_TextureParams.Add(new TextureParameter(reader));
            }

            int numBufferParams = reader.ReadInt32();
            m_BufferParams = new List<BufferBinding>();
            for (int i = 0; i < numBufferParams; i++)
            {
                m_BufferParams.Add(new BufferBinding(reader));
            }

            int numConstantBuffers = reader.ReadInt32();
            m_ConstantBuffers = new List<ConstantBuffer>();
            for (int i = 0; i < numConstantBuffers; i++)
            {
                m_ConstantBuffers.Add(new ConstantBuffer(reader));
            }

            int numConstantBufferBindings = reader.ReadInt32();
            m_ConstantBufferBindings = new List<BufferBinding>();
            for (int i = 0; i < numConstantBufferBindings; i++)
            {
                m_ConstantBufferBindings.Add(new BufferBinding(reader));
            }

            int numUAVParams = reader.ReadInt32();
            m_UAVParams = new List<UAVParameter>();
            for (int i = 0; i < numUAVParams; i++)
            {
                m_UAVParams.Add(new UAVParameter(reader));
            }

            int numSamplers = reader.ReadInt32();
            m_Samplers = new List<SamplerParameter>();
            for (int i = 0; i < numSamplers; i++)
            {
                m_Samplers.Add(new SamplerParameter(reader));
            }
        }
    }

public class SerializedSubProgram {
    public uint m_BlobIndex;
    public ParserBindChannels m_Channels;
    public ushort[] m_KeywordIndices;
    public sbyte m_ShaderHardwareTier;
    public sbyte m_GpuProgramType;
    public SerializedProgramParameters m_Parameters;
    public List<VectorParameter> m_VectorParams;
    public List<MatrixParameter> m_MatrixParams;
    public List<TextureParameter> m_TextureParams;
    public List<BufferBinding> m_BufferParams;
    public List<ConstantBuffer> m_ConstantBuffers;
    public List<BufferBinding> m_ConstantBufferBindings;
    public List<UAVParameter> m_UAVParams;
    public List<SamplerParameter> m_Samplers;

    public static bool HasInstancedStructuredBuffers(SerializedType type) => type.Match( "E99740711222CD922E9A6F92FF1EB07A", "B239746E4EC6E4D6D7BA27C84178610A", "3FD560648A91A99210D5DDF2BE320536");
    public static bool HasIsAdditionalBlob(SerializedType type) => type.Match("B239746E4EC6E4D6D7BA27C84178610A");

    public SerializedSubProgram(ObjectReader reader) {
        m_BlobIndex = reader.ReadUInt32();
        // if (HasIsAdditionalBlob(reader.SerializedType)) { TODO
        //     var m_IsAdditionalBlob = reader.ReadBoolean();
        //     reader.AlignStream();
        // }

        m_Channels = new ParserBindChannels(reader);

        var m_GlobalKeywordIndices = reader.ReadArray(_ => reader.ReadUInt16());
        reader.AlignStream();
        var m_LocalKeywordIndices = reader.ReadArray(_ => reader.ReadUInt16());
        reader.AlignStream();

        m_ShaderHardwareTier = reader.ReadSByte();
        m_GpuProgramType = reader.ReadSByte();
        reader.AlignStream();

        int numVectorParams = reader.ReadInt32();
        m_VectorParams = new List<VectorParameter>();
        for (int i = 0; i < numVectorParams; i++) {
            m_VectorParams.Add(new VectorParameter(reader));
        }

        int numMatrixParams = reader.ReadInt32();
        m_MatrixParams = new List<MatrixParameter>();
        for (int i = 0; i < numMatrixParams; i++) {
            m_MatrixParams.Add(new MatrixParameter(reader));
        }

        int numTextureParams = reader.ReadInt32();
        m_TextureParams = new List<TextureParameter>();
        for (int i = 0; i < numTextureParams; i++) {
            m_TextureParams.Add(new TextureParameter(reader));
        }

        int numBufferParams = reader.ReadInt32();
        m_BufferParams = new List<BufferBinding>();
        for (int i = 0; i < numBufferParams; i++) {
            m_BufferParams.Add(new BufferBinding(reader));
        }

        int numConstantBuffers = reader.ReadInt32();
        m_ConstantBuffers = new List<ConstantBuffer>();
        for (int i = 0; i < numConstantBuffers; i++) {
            m_ConstantBuffers.Add(new ConstantBuffer(reader));
        }

        int numConstantBufferBindings = reader.ReadInt32();
        m_ConstantBufferBindings = new List<BufferBinding>();
        for (int i = 0; i < numConstantBufferBindings; i++) {
            m_ConstantBufferBindings.Add(new BufferBinding(reader));
        }

        int numUAVParams = reader.ReadInt32();
        m_UAVParams = new List<UAVParameter>();
        for (int i = 0; i < numUAVParams; i++) {
            m_UAVParams.Add(new UAVParameter(reader));
        }

        int numSamplers = reader.ReadInt32();
        m_Samplers = new List<SamplerParameter>();
        for (int i = 0; i < numSamplers; i++) {
            m_Samplers.Add(new SamplerParameter(reader));
        }

        var m_ShaderRequirements = reader.ReadInt32();

        // if (HasInstancedStructuredBuffers(reader.SerializedType)) { TODO
        //     int numInstancedStructuredBuffers = reader.ReadInt32();
        //     var m_InstancedStructuredBuffers = new List<ConstantBuffer>();
        //     for (int i = 0; i < numInstancedStructuredBuffers; i++) {
        //         m_InstancedStructuredBuffers.Add(new ConstantBuffer(reader));
        //     }
        // }
    }
}

public class SerializedProgram {
    public List<SerializedSubProgram> m_SubPrograms;

    public SerializedProgram(ObjectReader reader) {
        int numSubPrograms = reader.ReadInt32();
        m_SubPrograms = new List<SerializedSubProgram>();
        for (int i = 0; i < numSubPrograms; i++) {
            m_SubPrograms.Add(new SerializedSubProgram(reader));
        }
    }
}

public class SerializedShaderFloatValue(ObjectReader reader) {
    public float val = reader.ReadSingle();
    public string name = reader.ReadAlignedString();
}

public class SerializedShaderRTBlendState(ObjectReader reader) {
    public SerializedShaderFloatValue srcBlend = new(reader);
    public SerializedShaderFloatValue destBlend = new(reader);
    public SerializedShaderFloatValue srcBlendAlpha = new(reader);
    public SerializedShaderFloatValue destBlendAlpha = new(reader);
    public SerializedShaderFloatValue blendOp = new(reader);
    public SerializedShaderFloatValue blendOpAlpha = new(reader);
    public SerializedShaderFloatValue colMask = new(reader);
}

public class SerializedStencilOp(ObjectReader reader) {
    public SerializedShaderFloatValue pass = new(reader);
    public SerializedShaderFloatValue fail = new(reader);
    public SerializedShaderFloatValue zFail = new(reader);
    public SerializedShaderFloatValue comp = new(reader);
}

public class SerializedShaderVectorValue(ObjectReader reader) {
    public SerializedShaderFloatValue x = new(reader);
    public SerializedShaderFloatValue y = new(reader);
    public SerializedShaderFloatValue z = new(reader);
    public SerializedShaderFloatValue w = new(reader);
    public string name = reader.ReadAlignedString();
}

public enum FogMode { Unknown = -1, Disabled = 0, Linear = 1, Exp = 2, Exp2 = 3 }

public class SerializedShaderState
{
    public string m_Name;
    public List<SerializedShaderRTBlendState> rtBlend;
    public bool rtSeparateBlend;
    public SerializedShaderFloatValue zClip;
    public SerializedShaderFloatValue zTest;
    public SerializedShaderFloatValue zWrite;
    public SerializedShaderFloatValue culling;
    public SerializedShaderFloatValue offsetFactor;
    public SerializedShaderFloatValue offsetUnits;
    public SerializedShaderFloatValue alphaToMask;
    public SerializedStencilOp stencilOp;
    public SerializedStencilOp stencilOpFront;
    public SerializedStencilOp stencilOpBack;
    public SerializedShaderFloatValue stencilReadMask;
    public SerializedShaderFloatValue stencilWriteMask;
    public SerializedShaderFloatValue stencilRef;
    public SerializedShaderFloatValue fogStart;
    public SerializedShaderFloatValue fogEnd;
    public SerializedShaderFloatValue fogDensity;
    public SerializedShaderVectorValue fogColor;
    public FogMode fogMode;
    public int gpuProgramID;
    public Dictionary<string, string> Tags;
    public int m_LOD;
    public bool lighting;

    public SerializedShaderState(ObjectReader reader) {
        m_Name = reader.ReadAlignedString();
        rtBlend = new List<SerializedShaderRTBlendState>();
        for (int i = 0; i < 8; i++) {
            rtBlend.Add(new SerializedShaderRTBlendState(reader));
        }
        rtSeparateBlend = reader.ReadBoolean();
        reader.AlignStream();
        zClip = new SerializedShaderFloatValue(reader);
        zTest = new SerializedShaderFloatValue(reader);
        zWrite = new SerializedShaderFloatValue(reader);
        culling = new SerializedShaderFloatValue(reader);
        offsetFactor = new SerializedShaderFloatValue(reader);
        offsetUnits = new SerializedShaderFloatValue(reader);
        alphaToMask = new SerializedShaderFloatValue(reader);
        stencilOp = new SerializedStencilOp(reader);
        stencilOpFront = new SerializedStencilOp(reader);
        stencilOpBack = new SerializedStencilOp(reader);
        stencilReadMask = new SerializedShaderFloatValue(reader);
        stencilWriteMask = new SerializedShaderFloatValue(reader);
        stencilRef = new SerializedShaderFloatValue(reader);
        fogStart = new SerializedShaderFloatValue(reader);
        fogEnd = new SerializedShaderFloatValue(reader);
        fogDensity = new SerializedShaderFloatValue(reader);
        fogColor = new SerializedShaderVectorValue(reader);
        fogMode = (FogMode)reader.ReadInt32();
        gpuProgramID = reader.ReadInt32();
        Tags = Range(0, reader.ReadInt32())
            .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
        m_LOD = reader.ReadInt32();
        lighting = reader.ReadBoolean();
        reader.AlignStream();
    }
}

public class SerializedPass {
        public Dictionary<string, int> NameIndices;
        public int Type; // { Normal = 0, Use = 1, Grab = 2 }
        public SerializedShaderState m_State;
        public uint m_ProgramMask;
        public SerializedProgram progVertex;
        public SerializedProgram progFragment;
        public SerializedProgram progGeometry;
        public SerializedProgram progHull;
        public SerializedProgram progDomain;
        public SerializedProgram progRayTracing;
        public bool m_HasInstancingVariant;
        public string m_UseName;
        public string m_Name;
        public string m_TextureName;
        public Dictionary<string, string> Tags;

        public SerializedPass(ObjectReader reader) {
            NameIndices = Range(0, reader.ReadInt32())
                .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadInt32());
            Type = reader.ReadInt32();
            m_State = new SerializedShaderState(reader);
            m_ProgramMask = reader.ReadUInt32();
            progVertex = new SerializedProgram(reader);
            progFragment = new SerializedProgram(reader);
            progGeometry = new SerializedProgram(reader);
            progHull = new SerializedProgram(reader);
            progDomain = new SerializedProgram(reader);
            progRayTracing = new SerializedProgram(reader);
            m_HasInstancingVariant = reader.ReadBoolean();
            var m_HasProceduralInstancingVariant = reader.ReadInt32() > 0;
            m_UseName = reader.ReadAlignedString();
            m_Name = reader.ReadAlignedString();
            m_TextureName = reader.ReadAlignedString();
            Tags = Range(0, reader.ReadInt32())
                .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
        }
    }

public class SerializedSubShader(ObjectReader reader) {
    public List<SerializedPass> m_Passes = reader.ReadList(_ => new SerializedPass(reader));
    public Dictionary<string, string> Tags = Range(0, reader.ReadInt32())
        .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
    public int LOD = reader.ReadInt32();
}

public class SerializedShaderDependency(ObjectReader reader) {
    public string from = reader.ReadAlignedString();
    public string to = reader.ReadAlignedString();
}

public class SerializedShader {
    public List<SerializedProperty> Props;
    public List<SerializedSubShader> SubShaders;
    public string Name;
    public string CustomEditorName;
    public string FallbackName;
    public List<SerializedShaderDependency> Dependencies;
    public bool DisableNoSubshadersMessage;

    public SerializedShader(ObjectReader reader) {
        Props = reader.ReadList(_ => new SerializedProperty(reader));
        SubShaders = reader.ReadList(_ => new SerializedSubShader(reader));
        Name = reader.ReadAlignedString();
        CustomEditorName = reader.ReadAlignedString();
        FallbackName = reader.ReadAlignedString();
        Dependencies = reader.ReadList(_ => new SerializedShaderDependency(reader));
        DisableNoSubshadersMessage = reader.ReadBoolean();
        reader.AlignStream();
    }
}

public class Shader : NamedObject {
    public readonly string Name;
    
    public SerializedShader ParsedForm;
    public uint[] Platforms;
    public uint[][] Offsets;
    public uint[][] CompressedLengths;
    public uint[][] DecompressedLengths;
    public byte[] CompressedBlob;

    public Shader(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        
        ParsedForm = new SerializedShader(reader);
        Platforms = reader.ReadArray(_ => reader.ReadUInt32());
        Offsets = reader.ReadArray(_ => reader.ReadArray(_ => reader.ReadUInt32()));
        CompressedLengths = reader.ReadArray(_ => reader.ReadArray(_ => reader.ReadUInt32()));
        DecompressedLengths = reader.ReadArray(_ => reader.ReadArray(_ => reader.ReadUInt32()));
        CompressedBlob = reader.ReadBytes(reader.ReadInt32());
        var dependencies = reader.ReadArray(_ => reader.ReadPointer<Shader>());
        var nonModifiableTextures = Range(0, reader.ReadInt32())
            .ToDictionary(_=> reader.ReadAlignedString(), _ => reader.ReadPointer<Texture>());
        var shaderIsBaked = reader.ReadBoolean();
        reader.AlignStream();
    }
}

public abstract class Texture : NamedObject {
    public readonly string Name;
    
    protected Texture(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        var forcedFallbackFormat = reader.ReadInt32();
        var downscaleFallback = reader.ReadBoolean();
        reader.AlignStream();
    }
}

public class UnityTexEnv(ObjectReader reader) {
    public PPtr<Texture> Texture = reader.ReadPointer<Texture>();
    public Vector2 Scale = reader.ReadVector2();
    public Vector2 Offset = reader.ReadVector2();
}

public record Color4(float R, float G, float B, float A);

public record UnityPropertySheet {
    public Dictionary<string, UnityTexEnv> TexEnvs;
    public Dictionary<string, float> Floats;
    public Dictionary<string, Color4> Colors;

    public UnityPropertySheet(ObjectReader reader) {
        TexEnvs = Range(0, reader.ReadInt32()).ToDictionary(_ => reader.ReadAlignedString(), _ => new UnityTexEnv(reader));
        Floats = Range(0, reader.ReadInt32()).ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadSingle());
        Colors = Range(0, reader.ReadInt32()).ToDictionary(_ => reader.ReadAlignedString(), _ =>
            new Color4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
    }
}

public sealed class Material : NamedObject {
    public readonly string Name;
    public PPtr<Shader> Shader;
    public UnityPropertySheet SavedProperties;

    public Material(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        Shader = reader.ReadPointer<Shader>();
        var shaderKeywords = reader.ReadAlignedString();
        var lightmapFlags = reader.ReadUInt32();
        var enableInstancingVariants = reader.ReadBoolean();
        var doubleSidedGI = reader.ReadBoolean(); //2017 and up
        var highShadingRate = reader.ReadBoolean(); //ZZZ
        reader.AlignStream();

        var customRenderQueue = reader.ReadInt32();
        var tags = Range(0, reader.ReadInt32()).ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
        var disabledShaderPasses = reader.ReadArray(_ => reader.ReadAlignedString());
        var enabledPassMask = reader.ReadUInt32(); // reader.Game.Type.IsZZZ() && HasEnabledPassMask(reader.SerializedType)
        SavedProperties = new UnityPropertySheet(reader);
    }
}

public record StaticBatchInfo(ushort FirstSubMesh, ushort SubMeshCount);

public abstract record Renderer : GameComponent {
    private static bool HasCullingDistance(byte[] hash) => "BFA28DBFE9993C2ABE21B3408666CFD3" == Convert.ToHexString(hash);
    
    public readonly PPtr<GameObject> GameObject;
    public List<PPtr<Material>> Materials;
    public StaticBatchInfo StaticBatchInfo;

    protected Renderer(ObjectReader reader, byte[] oldTypeHash) {
        GameObject = reader.ReadPointer<GameObject>();
        var flags1 = reader.ReadInt32(); // enabled castShadows receiveShadows dynamicOccludee
        var flags2 = reader.ReadInt32(); // motionVectors lightProbeUsage reflectionProbeUsage rayTracingMode
        var rayTraceProcedural = reader.ReadByte();
        reader.AlignStream();
        var renderingLayerMask = reader.ReadUInt32();
        var rendererPriority = reader.ReadInt32();

        var lightmapIndex = reader.ReadUInt16();
        var lightmapIndexDynamic = reader.ReadUInt16();
        var lightmapTilingOffset = reader.ReadVector4();
        var lightmapTilingOffsetDynamic = reader.ReadVector4();

        Materials = reader.ReadList(_ => reader.ReadPointer<Material>());
        StaticBatchInfo = new StaticBatchInfo(reader.ReadUInt16(), reader.ReadUInt16());
        var m_StaticBatchRoot = new PPtr<Transform>(reader.ReadInt32(), reader.ReadInt64());

        var m_ProbeAnchor = new PPtr<Transform>(reader.ReadInt32(), reader.ReadInt64());
        var m_LightProbeVolumeOverride = new PPtr<GameObject>(reader.ReadInt32(), reader.ReadInt64());

        var m_SortingLayerID = reader.ReadUInt32();
        var m_SortingOrder = reader.ReadInt16();
        reader.AlignStream();

        // if (reader.Game.Type.IsZZZ())
        var m_NeedHizCulling = reader.ReadBoolean();
        var m_HighShadingRate = reader.ReadBoolean();
        var m_RayTracingLayerMask = reader.ReadBoolean();
        reader.AlignStream();
        if (HasCullingDistance(oldTypeHash)) {
            var m_CullingDistance = reader.ReadSingle();
        }
    }
}

public record SkinnedMeshRenderer : Renderer {
    public readonly PPtr<Mesh> Mesh;
    public readonly List<PPtr<Transform>> Bones;
    public float[] BlendShapeWeights;
    public PPtr<Transform> RootBone;
    public AABB MAabb;
    public bool MDirtyAabb;

    public SkinnedMeshRenderer(ObjectReader reader, byte[] oldTypeHash) : base(reader, oldTypeHash) {
        var mQuality = reader.ReadInt32();
        var mUpdateWhenOffscreen = reader.ReadBoolean();
        var mSkinNormals = reader.ReadBoolean(); //3.1.0 and below
        reader.AlignStream();
        Mesh = reader.ReadPointer<Mesh>();
        Bones = reader.ReadList(_ => reader.ReadPointer<Transform>());
        var mSortingFudge = reader.ReadSingle(); // ZZZ
        BlendShapeWeights = reader.ReadArray(_ => reader.ReadSingle());

        // if (reader.Game.Type.IsGIGroup() || reader.Game.Type.IsZZZ()) {
        RootBone = reader.ReadPointer<Transform>();
        MAabb = AABB.Parse(reader);
        MDirtyAabb = reader.ReadBoolean();
        reader.AlignStream();
    }
}

public record MeshRenderer : Renderer {
    public PPtr<Mesh> AdditionalVertexStreams;
    public MeshRenderer(ObjectReader reader, byte[] oldTypeHash) : base(reader, oldTypeHash) {
        AdditionalVertexStreams = reader.ReadPointer<Mesh>();
    }
}

public record MeshFilter(PPtr<GameObject> GameObject, PPtr<Mesh> Mesh) : GameComponent {
    public static MeshFilter Parse(ObjectReader r) =>
        new MeshFilter(r.ReadPointer<GameObject>(), r.ReadPointer<Mesh>());
}