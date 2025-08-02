using System.Numerics;
using System.Text;
using Nostradamus.Data;
using static System.Linq.Enumerable;

namespace Nostradamus;

public class PackedFloatVector(ObjectReader reader) {
    public uint NumItems = reader.ReadUInt32();
    public float Range = reader.ReadSingle();
    public float Start = reader.ReadSingle();
    public byte[] Data = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));
    public byte BitSize = reader.Align(reader.ReadByte);
}

public class PackedIntVector(ObjectReader reader) {
    public uint NumItems = reader.ReadUInt32();
    public byte[] Data = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));
    public byte BitSize = reader.Align(reader.ReadByte);
}

public class Animator(ObjectReader reader) {
    public readonly PPtr<GameObject> GameObject = reader.ReadPointer<GameObject>();
    public bool Enabled = reader.Align(reader.ReadBoolean);
    public readonly PPtr<Avatar> AvatarPtr = reader.ReadPointer<Avatar>();
    public PPtr<object> ControllerPtr = reader.ReadPointer<object>(); // NamedObject : RuntimeAnimatorController

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
public sealed class Avatar(ObjectReader reader) {
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

public class MonoBehaviour(ObjectReader r) {
    public readonly PPtr<GameObject> GameObject = r.ReadPointer<GameObject>();
    public readonly bool Enabled = r.Align(r.ReadBoolean);
    public readonly PPtr<MonoScript> Script = r.ReadPointer<MonoScript>();
    public readonly string Name = r.Align(() => Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32())));    
}

public class MonoScript(ObjectReader reader) {
    public readonly string Name = reader.ReadAlignedString();
    public readonly int ExecutionOrder = reader.ReadInt32(); // 3.4 and up
    public readonly byte[] PropertiesHash = reader.ReadBytes(16);
    public readonly string ClassName = reader.ReadAlignedString();
    public readonly string Namespace = reader.ReadAlignedString(); // 3.0 and up
    public readonly string AssemblyName = reader.ReadAlignedString();
    public readonly bool IsEditorScript = reader.ReadBoolean();
}

public record AABB(Vector3 Center, Vector3 Extent);
public class SubMesh(ObjectReader reader) {
    public uint FirstByte = reader.ReadUInt32();
    public uint IndexCount = reader.ReadUInt32();
    public int Topology = reader.ReadInt32(); // (GfxPrimitiveType)
    public uint BaseVertex = reader.ReadUInt32();
    public uint FirstVertex = reader.ReadUInt32();
    public uint VertexCount = reader.ReadUInt32();
    public AABB LocalAABB = new(reader.ReadVector3(), reader.ReadVector3());
}
public class BlendShapeVertex(ObjectReader reader) {
    public Vector3 Vertex = reader.ReadVector3();
    public Vector3 Normal = reader.ReadVector3();
    public Vector3 Tangent = reader.ReadVector3();
    public uint Index = reader.ReadUInt32();
}
public class MeshBlendShape(ObjectReader reader) {
    public uint FirstVertex = reader.ReadUInt32();
    public uint VertexCount = reader.ReadUInt32();
    public bool HasNormals = reader.ReadBoolean();
    public bool HasTangents = reader.Align(reader.ReadBoolean);
}
public class MeshBlendShapeChannel(ObjectReader reader) {
    public string Name = reader.ReadAlignedString();
    public uint NameHash = reader.ReadUInt32();
    public int FrameIndex = reader.ReadInt32();
    public int FrameCount = reader.ReadInt32();
}
public class BlendShapeData(ObjectReader r) {
    public List<BlendShapeVertex> Vertices = r.ReadList(_ => new BlendShapeVertex(r));
    public List<MeshBlendShape> Shapes = r.ReadList(_ => new MeshBlendShape(r));
    public List<MeshBlendShapeChannel> Channels = r.ReadList(_ => new MeshBlendShapeChannel(r));
    public float[] FullWeights = r.ReadArray(_ => r.ReadSingle());
}

public record ChannelInfo(byte Stream, byte Offset, byte Format, byte Dimension);

public class StreamInfo(ObjectReader reader) {
    public uint ChannelMask = reader.ReadUInt32();
    public uint Offset = reader.ReadUInt32();
    public uint Stride = reader.ReadByte();
}

public class VertexData(ObjectReader reader) {
    public uint VertexCount = reader.ReadUInt32();
    public List<ChannelInfo> Channels = reader.ReadList(_ => new ChannelInfo(
        reader.ReadByte(),
        reader.ReadByte(),
        reader.ReadByte(),
        (byte)(reader.ReadByte() & 0xF)
    ));
    public byte[] DataSize = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));
}

public class CompressedMesh(ObjectReader reader) {
    public PackedFloatVector Vertices = new(reader);
    public PackedFloatVector UV = new(reader);
    public PackedFloatVector Normals = new(reader);
    public PackedFloatVector Tangents = new(reader);
    public PackedIntVector Weights = new(reader);
    public PackedIntVector NormalSigns = new(reader);
    public PackedIntVector TangentSigns = new(reader);
    public PackedFloatVector FloatColors = new(reader);
    public PackedIntVector BoneIndices = new(reader);
    public PackedIntVector Triangles = new(reader);
    public uint UVInfo = reader.ReadUInt32();
}

public class StreamingInfo(ObjectReader reader) {
    public uint Offset = reader.ReadUInt32();
    public uint Size = reader.ReadUInt32();
    public string Path = reader.ReadAlignedString();
}

public sealed class Mesh {
    public readonly string Name;
    public List<SubMesh> SubMeshes;
    public BlendShapeData Shapes;
    public Matrix4x4[] BindPose;
    public uint[] BoneNameHashes;
    private bool Use16BitIndices;
    private uint[] IndexBuffer;
    private VertexData VertexData;
    private CompressedMesh CompressedMesh;
    private StreamingInfo StreamData;

    public Mesh(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        SubMeshes = reader.ReadList(_ => new SubMesh(reader));
        Shapes = new BlendShapeData(reader);
        BindPose = reader.ReadArray(_ => reader.ReadMatrix4X4());
        BoneNameHashes = reader.ReadArray(_ => reader.ReadUInt32());
        var rootBoneNameHash = reader.ReadUInt32();
        var bonesAABB = reader.ReadList(_ => (reader.ReadVector3(), reader.ReadVector3()));
        var variableBoneCountWeights = reader.ReadArray(_ => reader.ReadUInt32());

        var meshCompression = reader.ReadBoolean();
        var isReadable = reader.ReadBoolean();
        var keepVertices = reader.ReadBoolean();
        var keepIndices = reader.ReadBoolean();

        Use16BitIndices = reader.ReadInt32() == 0;
        IndexBuffer = Use16BitIndices
            ? reader.Align(() => Range(0, reader.ReadInt32() / 2).Select(_ => (uint)reader.ReadUInt16()).ToArray())
            : Range(0, reader.ReadInt32() / 4).Select(_ => reader.ReadUInt32()).ToArray();
        VertexData = new VertexData(reader);
        CompressedMesh = new CompressedMesh(reader);
        reader.BaseStream.Position += 24; //AABB m_LocalAABB
        var meshUsageFlags = reader.ReadInt32();
        var bakedConvexCollisionMesh = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));
        var bakedTriangleCollisionMesh = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));
        float[] meshMetrics = [reader.ReadSingle(), reader.ReadSingle()];
        
        // if (reader.Game.Type.IsZZZ())
        var metricsDirty = reader.Align(reader.ReadBoolean);
        var closeMeshDynamicCompression = reader.Align(reader.ReadBoolean);
        var compressLevelVertexData = reader.ReadInt32();
        var compressLevelNormalAndTangent = reader.ReadInt32();
        var compressLevelTexCoordinates = reader.ReadInt32();
        var packSkinDataToUV2UV3 = reader.Align(reader.ReadBoolean);
        var bakeBVHData = reader.ReadBoolean();
        var bakeRefittableBVH = reader.Align(reader.ReadBoolean);
        var bVHBakeLevels = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));
        var bakedBVHSize = reader.ReadUInt64();
        var bVHDataBuffer = reader.Align(() => reader.ReadBytes(reader.ReadInt32()));

        StreamData = new StreamingInfo(reader);

        // ProcessData(); TODO
    }
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
    public sbyte Source = reader.ReadSByte();
    public sbyte Target = reader.ReadSByte();
}

public class ParserBindChannels(ObjectReader reader) {
    public List<ShaderBindChannel> Channels = reader.Align(() => reader.ReadList(_ => new ShaderBindChannel(reader)));
    public uint SourceMap = reader.ReadUInt32();
}

public class VectorParameter(ObjectReader reader) {
    public int NameIndex = reader.ReadInt32();
    public int Index = reader.ReadInt32();
    public int ArraySize = reader.ReadInt32();
    public sbyte Type = reader.ReadSByte();
    public sbyte Dim = reader.Align(reader.ReadSByte);
}

public class MatrixParameter(ObjectReader reader) {
    public int NameIndex = reader.ReadInt32();
    public int Index = reader.ReadInt32();
    public int ArraySize = reader.ReadInt32();
    public sbyte Type = reader.ReadSByte();
    public sbyte RowCount = reader.Align(reader.ReadSByte);
}

public class TextureParameter {
    public int NameIndex;
    public int Index;
    public int SamplerIndex;
    public sbyte Dim;

    public TextureParameter(ObjectReader reader) {
        NameIndex = reader.ReadInt32();
        Index = reader.ReadInt32();
        SamplerIndex = reader.ReadInt32();
        var multiSampled = reader.ReadBoolean();
        Dim = reader.Align(reader.ReadSByte);
    }
}

public class BufferBinding(ObjectReader reader) {
    public int NameIndex = reader.ReadInt32();
    public int Index = reader.ReadInt32();
    public int ArraySize = reader.ReadInt32();
}

public class StructParameter {
    public List<MatrixParameter> MatrixParams;
    public List<VectorParameter> VectorParams;

    public StructParameter(ObjectReader reader) {
        var nameIndex = reader.ReadInt32();
        var index = reader.ReadInt32();
        var arraySize = reader.ReadInt32();
        var structSize = reader.ReadInt32();

        VectorParams = reader.ReadList(_ => new VectorParameter(reader));
        MatrixParams = reader.ReadList(_ => new MatrixParameter(reader));
    }
}

public class SamplerParameter(ObjectReader reader) {
    public uint Sampler = reader.ReadUInt32();
    public int BindPoint = reader.ReadInt32();
}

public class ConstantBuffer(ObjectReader reader) {
    public int NameIndex = reader.ReadInt32();
    public List<MatrixParameter> MatrixParams = reader.ReadList(_ => new MatrixParameter(reader));
    public List<VectorParameter> VectorParams = reader.ReadList(_ => new VectorParameter(reader));
    public List<StructParameter> StructParams = reader.ReadList(_ => new StructParameter(reader));
    public int Size = reader.ReadInt32();
    public bool IsPartialCB;
}

public class UAVParameter(ObjectReader reader) {
    public int NameIndex = reader.ReadInt32();
    public int Index = reader.ReadInt32();
    public int OriginalIndex = reader.ReadInt32();
}

public class SerializedSubProgram {
    public uint BlobIndex;
    public ParserBindChannels Channels;
    public ushort[] KeywordIndices;
    public sbyte ShaderHardwareTier;
    public sbyte GpuProgramType;
    public List<VectorParameter> VectorParams;
    public List<MatrixParameter> MatrixParams;
    public List<TextureParameter> TextureParams;
    public List<BufferBinding> BufferParams;
    public List<ConstantBuffer> ConstantBuffers;
    public List<BufferBinding> ConstantBufferBindings;
    public List<UAVParameter> UAVParams;
    public List<SamplerParameter> Samplers;

    public static bool HasInstancedStructuredBuffers(SerializedType type) => type.Match("E99740711222CD922E9A6F92FF1EB07A", "B239746E4EC6E4D6D7BA27C84178610A", "3FD560648A91A99210D5DDF2BE320536");
    public static bool HasIsAdditionalBlob(SerializedType type) => type.Match("B239746E4EC6E4D6D7BA27C84178610A");

    public SerializedSubProgram(ObjectReader reader) {
        BlobIndex = reader.ReadUInt32();
        // if (HasIsAdditionalBlob(reader.SerializedType)) { TODO
        //     var m_IsAdditionalBlob = reader.ReadBoolean();
        //     reader.AlignStream();
        // }

        Channels = new ParserBindChannels(reader);

        var globalKeywordIndices = reader.Align(() => reader.ReadArray(_ => reader.ReadUInt16()));
        var localKeywordIndices = reader.Align(() => reader.ReadArray(_ => reader.ReadUInt16()));
        ShaderHardwareTier = reader.ReadSByte();
        GpuProgramType = reader.Align(reader.ReadSByte);

        VectorParams = reader.ReadList(_ => new VectorParameter(reader));
        MatrixParams = reader.ReadList(_ => new MatrixParameter(reader));
        TextureParams = reader.ReadList(_ => new TextureParameter(reader));
        BufferParams = reader.ReadList(_ => new BufferBinding(reader));
        ConstantBuffers = reader.ReadList(_ => new ConstantBuffer(reader));
        ConstantBufferBindings = reader.ReadList(_ => new BufferBinding(reader));
        UAVParams = reader.ReadList(_ => new UAVParameter(reader));
        Samplers = reader.ReadList(_ => new SamplerParameter(reader));

        var shaderRequirements = reader.ReadInt32();

        // if (HasInstancedStructuredBuffers(reader.SerializedType)) { TODO
        //     int numInstancedStructuredBuffers = reader.ReadInt32();
        //     var m_InstancedStructuredBuffers = new List<ConstantBuffer>();
        //     for (int i = 0; i < numInstancedStructuredBuffers; i++) {
        //         m_InstancedStructuredBuffers.Add(new ConstantBuffer(reader));
        //     }
        // }
    }
}

public class SerializedProgram(ObjectReader reader) {
    public List<SerializedSubProgram> SubPrograms = reader.ReadList(_ => new SerializedSubProgram(reader));
}

public class SerializedShaderFloatValue(ObjectReader reader) {
    public float Val = reader.ReadSingle();
    public string Name = reader.ReadAlignedString();
}

public class SerializedShaderRTBlendState(ObjectReader reader) {
    public SerializedShaderFloatValue SrcBlend = new(reader);
    public SerializedShaderFloatValue DestBlend = new(reader);
    public SerializedShaderFloatValue SrcBlendAlpha = new(reader);
    public SerializedShaderFloatValue DestBlendAlpha = new(reader);
    public SerializedShaderFloatValue BlendOp = new(reader);
    public SerializedShaderFloatValue BlendOpAlpha = new(reader);
    public SerializedShaderFloatValue ColMask = new(reader);
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

public class SerializedShaderState {
    public string m_Name;
    public List<SerializedShaderRTBlendState> rtBlend;
    public bool RtSeparateBlend;
    public SerializedShaderFloatValue ZClip;
    public SerializedShaderFloatValue ZTest;
    public SerializedShaderFloatValue ZWrite;
    public SerializedShaderFloatValue Culling;
    public SerializedShaderFloatValue OffsetFactor;
    public SerializedShaderFloatValue OffsetUnits;
    public SerializedShaderFloatValue AlphaToMask;
    public SerializedStencilOp StencilOp;
    public SerializedStencilOp StencilOpFront;
    public SerializedStencilOp StencilOpBack;
    public SerializedShaderFloatValue StencilReadMask;
    public SerializedShaderFloatValue StencilWriteMask;
    public SerializedShaderFloatValue StencilRef;
    public SerializedShaderFloatValue FogStart;
    public SerializedShaderFloatValue FogEnd;
    public SerializedShaderFloatValue FogDensity;
    public SerializedShaderVectorValue FogColor;
    public int fogMode; // { Unknown = -1, Disabled = 0, Linear = 1, Exp = 2, Exp2 = 3 }
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
        RtSeparateBlend = reader.Align(reader.ReadBoolean);
        ZClip = new SerializedShaderFloatValue(reader);
        ZTest = new SerializedShaderFloatValue(reader);
        ZWrite = new SerializedShaderFloatValue(reader);
        Culling = new SerializedShaderFloatValue(reader);
        OffsetFactor = new SerializedShaderFloatValue(reader);
        OffsetUnits = new SerializedShaderFloatValue(reader);
        AlphaToMask = new SerializedShaderFloatValue(reader);
        StencilOp = new SerializedStencilOp(reader);
        StencilOpFront = new SerializedStencilOp(reader);
        StencilOpBack = new SerializedStencilOp(reader);
        StencilReadMask = new SerializedShaderFloatValue(reader);
        StencilWriteMask = new SerializedShaderFloatValue(reader);
        StencilRef = new SerializedShaderFloatValue(reader);
        FogStart = new SerializedShaderFloatValue(reader);
        FogEnd = new SerializedShaderFloatValue(reader);
        FogDensity = new SerializedShaderFloatValue(reader);
        FogColor = new SerializedShaderVectorValue(reader);
        fogMode = reader.ReadInt32();
        gpuProgramID = reader.ReadInt32();
        Tags = Range(0, reader.ReadInt32())
            .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
        m_LOD = reader.ReadInt32();
        lighting = reader.Align(reader.ReadBoolean);
    }
}

public class SerializedPass {
        public Dictionary<string, int> NameIndices;
        public int Type; // { Normal = 0, Use = 1, Grab = 2 }
        public SerializedShaderState State;
        public uint ProgramMask;
        public SerializedProgram ProgVertex;
        public SerializedProgram ProgFragment;
        public SerializedProgram ProgGeometry;
        public SerializedProgram ProgHull;
        public SerializedProgram ProgDomain;
        public SerializedProgram ProgRayTracing;
        public bool HasInstancingVariant;
        public string UseName;
        public string Name;
        public string TextureName;
        public Dictionary<string, string> Tags;

        public SerializedPass(ObjectReader reader) {
            NameIndices = Range(0, reader.ReadInt32())
                .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadInt32());
            Type = reader.ReadInt32();
            State = new SerializedShaderState(reader);
            ProgramMask = reader.ReadUInt32();
            ProgVertex = new SerializedProgram(reader);
            ProgFragment = new SerializedProgram(reader);
            ProgGeometry = new SerializedProgram(reader);
            ProgHull = new SerializedProgram(reader);
            ProgDomain = new SerializedProgram(reader);
            ProgRayTracing = new SerializedProgram(reader);
            HasInstancingVariant = reader.ReadBoolean();
            var hasProceduralInstancingVariant = reader.ReadInt32() > 0;
            UseName = reader.ReadAlignedString();
            Name = reader.ReadAlignedString();
            TextureName = reader.ReadAlignedString();
            Tags = Range(0, reader.ReadInt32())
                .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
        }
    }

public class SerializedSubShader(ObjectReader reader) {
    public List<SerializedPass> Passes = reader.ReadList(_ => new SerializedPass(reader));
    public Dictionary<string, string> Tags = Range(0, reader.ReadInt32())
        .ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
    public int LOD = reader.ReadInt32();
}

public class SerializedShaderDependency(ObjectReader reader) {
    public string From = reader.ReadAlignedString();
    public string To = reader.ReadAlignedString();
}

public class SerializedShader(ObjectReader reader) {
    public List<SerializedProperty> Props = reader.ReadList(_ => new SerializedProperty(reader));
    public List<SerializedSubShader> SubShaders = reader.ReadList(_ => new SerializedSubShader(reader));
    public string Name = reader.ReadAlignedString();
    public string CustomEditorName = reader.ReadAlignedString();
    public string FallbackName = reader.ReadAlignedString();
    public List<SerializedShaderDependency> Dependencies = reader.ReadList(_ => new SerializedShaderDependency(reader));
    public bool DisableNoSubshadersMessage = reader.Align(reader.ReadBoolean);
}

public class Shader {
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
        var shaderIsBaked = reader.Align(reader.ReadBoolean);
    }
}

public abstract class Texture {
    public readonly string Name;
    
    protected Texture(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        var forcedFallbackFormat = reader.ReadInt32();
        var downscaleFallback = reader.Align(reader.ReadBoolean);
    }
}

public class GLTextureSettings(ObjectReader reader) {
    public readonly int FilterMode = reader.ReadInt32();
    public readonly int Aniso = reader.ReadInt32();
    public readonly float MipBias = reader.ReadSingle();
    public readonly int WrapMode = reader.ReadInt32(); //m_WrapU
    public readonly int WrapV = reader.ReadInt32();
    public readonly int WrapW = reader.ReadInt32();
}

public class Texture2D : Texture {
    public readonly int Width;
    public readonly int Height;
    public readonly TextureFormat Format;
    public bool m_MipMap;
    public int m_MipCount;
    public GLTextureSettings Settings;
    public StreamingInfo m_StreamData;
    public byte[] Data;

    public Texture2D(ObjectReader reader) : base(reader) {
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        var m_CompleteImageSize = reader.ReadInt32();
        Format = (TextureFormat)reader.ReadInt32();
        m_MipCount = reader.ReadInt32();
        var m_IsReadable = reader.ReadBoolean(); //2.6.0 and up
        var m_IsPreProcessed = reader.ReadBoolean(); // 2020.1 and up OR ZZZ
        var m_IgnoreMasterTextureLimit = reader.ReadBoolean(); // 2019.3 and up
        var m_StreamingMipmaps = reader.Align(reader.ReadBoolean); // 2018.2 and up
        var m_StreamingMipmapsPriority = reader.ReadInt32(); //2018.2 and up
        var m_IsCompressed = reader.Align(reader.ReadBoolean); // is ZZZ
        var m_ImageCount = reader.ReadInt32();
        var m_TextureDimension = reader.ReadInt32();
        Settings = new GLTextureSettings(reader);
        var m_LightmapFormat = reader.ReadInt32(); //3.0 and up
        var m_ColorSpace = reader.ReadInt32(); //3.5.0 and up
        var image_data_size = reader.ReadInt32();
        if (image_data_size == 0) { // && 5.3.0 and up
            var m_ExternalMipRelativeIndex = reader.ReadUInt32(); // is ZZZ
            m_StreamData = new StreamingInfo(reader);
        } else {
            Data = reader.ReadBytes(image_data_size);
        }
    }
    
    public override string ToString() => $"{Name} [{Width}x{Height}][{Format}]";
}

public class UnityTexEnv(ObjectReader reader) {
    public readonly PPtr<Texture> Texture = reader.ReadPointer<Texture>();
    public readonly Vector2 Scale = reader.ReadVector2();
    public readonly Vector2 Offset = reader.ReadVector2();
    
    public override string ToString() => Texture.ToString();
}

public record Color4(float R, float G, float B, float A);

public record UnityPropertySheet {
    public readonly Dictionary<string, UnityTexEnv> TexEnvs;
    public readonly Dictionary<string, float> Floats;
    public readonly List<(string, Color4)> Colors;

    public UnityPropertySheet(ObjectReader r) {
        TexEnvs = Range(0, r.ReadInt32()).ToDictionary(_ => r.ReadAlignedString(), _ => new UnityTexEnv(r));
        Floats = Range(0, r.ReadInt32()).ToDictionary(_ => r.ReadAlignedString(), _ => r.ReadSingle());
        Colors = r.ReadList(_ => (r.ReadAlignedString(), 
            new Color4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle())));
    }
}

public class Material {
    public readonly string Name;
    public readonly PPtr<Shader> Shader;
    public readonly UnityPropertySheet SavedProperties;

    public Material(ObjectReader reader) {
        Name = reader.ReadAlignedString();
        Shader = reader.ReadPointer<Shader>();
        var shaderKeywords = reader.ReadAlignedString();
        var lightmapFlags = reader.ReadUInt32();
        var enableInstancingVariants = reader.ReadBoolean();
        var doubleSidedGI = reader.ReadBoolean(); //2017 and up
        var highShadingRate = reader.Align(reader.ReadBoolean); //ZZZ

        var customRenderQueue = reader.ReadInt32();
        var tags = Range(0, reader.ReadInt32()).ToDictionary(_ => reader.ReadAlignedString(), _ => reader.ReadAlignedString());
        var disabledShaderPasses = reader.ReadArray(_ => reader.ReadAlignedString());
        var enabledPassMask = reader.ReadUInt32(); // reader.Game.Type.IsZZZ() && HasEnabledPassMask(reader.SerializedType)
        SavedProperties = new UnityPropertySheet(reader);
    }
    
    public override string ToString() => Name;
}

public abstract record Renderer {
    private static bool HasCullingDistance(byte[] hash) => "BFA28DBFE9993C2ABE21B3408666CFD3" == Convert.ToHexString(hash);
    
    public readonly PPtr<GameObject> GameObject;
    public readonly List<PPtr<Material>> Materials;

    protected Renderer(ObjectReader reader, byte[] oldTypeHash) {
        GameObject = reader.ReadPointer<GameObject>();
        /*var flags1 =*/ reader.ReadInt32(); // enabled castShadows receiveShadows dynamicOccludee
        /*var flags2 =*/ reader.ReadInt32(); // motionVectors lightProbeUsage reflectionProbeUsage rayTracingMode
        /*var rayTraceProcedural =*/ reader.Align(reader.ReadByte);
        /*var renderingLayerMask =*/ reader.ReadUInt32();
        /*var rendererPriority =*/ reader.ReadInt32();

        /*var lightmapIndex =*/ reader.ReadUInt16();
        /*var lightmapIndexDynamic =*/ reader.ReadUInt16();
        /*var lightmapTilingOffset =*/ reader.ReadVector4();
        /*var lightmapTilingOffsetDynamic =*/ reader.ReadVector4();

        Materials = reader.ReadList(_ => reader.ReadPointer<Material>());
        /*var staticBatchFirstSubMesh =*/ reader.ReadUInt16();
        /*var staticBatchSubMeshCount =*/ reader.ReadUInt16();
        /*var staticBatchRoot =*/ reader.ReadPointer<Transform>();

        /*var probeAnchor =*/ reader.ReadPointer<Transform>();
        /*var lightProbeVolumeOverride =*/ reader.ReadPointer<GameObject>();

        /*var sortingLayerId =*/ reader.ReadUInt32();
        /*var sortingOrder =*/ reader.Align(reader.ReadInt16);

        // if (reader.Game.Type.IsZZZ())
        /*var needHizCulling =*/ reader.ReadBoolean();
        /*var highShadingRate =*/ reader.ReadBoolean();
        /*var rayTracingLayerMask =*/ reader.Align(reader.ReadBoolean);
        if (HasCullingDistance(oldTypeHash)) {
            /*var cullingDistance =*/ reader.ReadSingle();
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
        var mSkinNormals = reader.Align(reader.ReadBoolean); //3.1.0 and below
        Mesh = reader.ReadPointer<Mesh>();
        Bones = reader.ReadList(_ => reader.ReadPointer<Transform>());
        var mSortingFudge = reader.ReadSingle(); // ZZZ
        BlendShapeWeights = reader.ReadArray(_ => reader.ReadSingle());

        // if (reader.Game.Type.IsGIGroup() || reader.Game.Type.IsZZZ()) {
        RootBone = reader.ReadPointer<Transform>();
        MAabb = new AABB(reader.ReadVector3(), reader.ReadVector3());
        MDirtyAabb = reader.Align(reader.ReadBoolean);
    }
}

public record MeshRenderer : Renderer {
    public PPtr<Mesh> AdditionalVertexStreams;
    public MeshRenderer(ObjectReader reader, byte[] oldTypeHash) : base(reader, oldTypeHash) {
        AdditionalVertexStreams = reader.ReadPointer<Mesh>();
    }
}

public record MeshFilter(PPtr<GameObject> GameObject, PPtr<Mesh> Mesh) {
    public static MeshFilter Parse(ObjectReader r) =>
        new MeshFilter(r.ReadPointer<GameObject>(), r.ReadPointer<Mesh>());
}

public record AssetInfo(string Id, int Idx, int Size, PPtr<object> Asset);

public record AssetBundle(string Name, List<PPtr<object>> PreloadTable, List<AssetInfo> Container) {
    public static AssetBundle Parse(ObjectReader r) => new(
        r.ReadAlignedString(),
        r.ReadList(_ => r.ReadPointer<object>()),
        r.ReadList(_ => new AssetInfo(r.ReadAlignedString(),
            r.ReadInt32(), r.ReadInt32(), r.ReadPointer<object>())));
}
