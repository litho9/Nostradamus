using Nostradamus;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("** Nostradamus **");

var watch = System.Diagnostics.Stopwatch.StartNew();
var path0 = @"D:\games\HoYoPlay\games\Genshin Impact game\GenshinImpact_Data\StreamingAssets\AssetBundles\blocks\00\";

var stream = File.Open(path0 + "00000890.blk", FileMode.Open, FileAccess.Read);
var cabs = Blb3.LoadBlock(stream);
Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");
var cab0 = cabs["CAB-0aa2768ea164a0d7db932b50052974af"];
var root = (Transform)cab0.Values.Single(o => o is Transform { Father.PathId: 0 });
// PrintCab(root, cab0)

void runZ() {
    var path0 = Environment.GetEnvironmentVariable("GAME_PATH")!;

    var mhy1 = new Mhy1(path0);
    mhy1.LoadCabMap();
    Console.WriteLine($"cabMap loaded in {watch.ElapsedMilliseconds}ms.");

    watch.Restart();
    var blk = mhy1.LoadBlock(path0 + "3212318446.blk");
    var cab0 = blk["CAB-0aa2768ea164a0d7db932b50052974af"];
    Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");

    watch.Restart();
    var blk2 = mhy1.LoadBlock(path0 + "2299538835.blk"); // SeparateMesh_Avatar_Female_Size01_Jufufu_Model_Jufufu_Body_1
    var cab2 = blk2["CAB-cd0a7c1addc386d573a202aad65e4aff"];
    Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");

    var root = (Transform)cab0.Values.Single(o => o is Transform { Father.PathId: 0 });
    PrintCab(root, mhy1);
    watch.Stop();
    Console.WriteLine($"Execution finished in {watch.ElapsedMilliseconds}ms.");
}

void PrintCab(Transform t, Mhy1 mhy1, string ident = "") {
    Console.WriteLine($"{ident}🎮 {t.GameObject.Val!.Name} {t.X}");
    foreach (var c in t.GameObject.Val!.Components) {
        var o = mhy1.Point(c);
        if (o is null) {
            Console.WriteLine($"{ident} ↳ {c} NOT FOUND!!");
        // } else if (o is Animator aa) {
        //     var avatar = mhy1.Point(aa.AvatarPtr);
        //     Console.WriteLine($"{ident} ↳ {avatar.Name}");
        // } else if (o is SkinnedMeshRenderer smr) {
        //     var mats = smr.Materials.Select(mhy1.Point).ToList();
        //     // var shaders = mhy1.Point(mats[0].Shader);
        //     foreach (var texEnv in mats.SelectMany(mat => mat.TexEnvs))
        //         mhy1.Point(texEnv.Value.Texture);
        //     Console.WriteLine($"{ident} ↳ 🎭 materials={string.Join(", ", mats.Select(m => m.Name))} bones={smr.Bones.Count}");
        } else if (o is not Transform)
            Console.WriteLine($"{ident} ↳ {o}");
    }
    if (t.GameObject.Val!.Name == "Bip001") return;
    foreach (var c in t.Children)
        PrintCab(c.Val!, mhy1, ident: ident + "|");
}
