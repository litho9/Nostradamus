using System.Numerics;
using System.Text;
using static System.Linq.Enumerable;

namespace Nostradamus;

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
    
    public int ReadMhyInt() => ReadMhyInt(ReadBytes(6));
    private static int ReadMhyInt(byte[] buffer) =>
        buffer[2] | (buffer[4] << 8) | (buffer[0] << 0x10) | (buffer[5] << 0x18);
    public int ReadMhyUInt() => ReadMhyUInt(ReadBytes(7));
    public static int ReadMhyUInt(byte[] buffer) =>
        buffer[1] | (buffer[6] << 8) | (buffer[3] << 0x10) | (buffer[2] << 0x18);
    public string ReadMhyString() {
        var pos = BaseStream.Position;
        var str = ReadStringToNull();
        BaseStream.Position += 0x105 - BaseStream.Position + pos;
        return str;
    }
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
        // else Console.WriteLine($"[CAB] '{this}' points to nothing.");
    }

    public override string ToString() => $"{PathId:x16}:{ExtPath ?? ""}:{Val?.ToString() ?? ""}";
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