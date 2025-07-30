using Nostradamus;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("** Nostradamus **");
var watch = System.Diagnostics.Stopwatch.StartNew();
var mhy1 = new Mhy1();
// mhy1.LoadCabMap();
Console.WriteLine($"cabMap loaded in {watch.ElapsedMilliseconds}ms.");

watch.Restart();
var blk = mhy1.LoadBlock("3212318446.blk");
var cab0 = blk["CAB-0aa2768ea164a0d7db932b50052974af"];
Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");

watch.Restart();
var blk2 = mhy1.LoadBlock("2299538835.blk"); // SeparateMesh_Avatar_Female_Size01_Jufufu_Model_Jufufu_Body_1
var cab2 = blk2["CAB-cd0a7c1addc386d573a202aad65e4aff"];
Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");


var root = (Transform)cab0.Objects.Values.Single(o => o is Transform { Father.PathId: 0 });
PrintCab(cab0, root);
//
// foreach (var (k, o) in cab0.Objects)
// //     if (o is not Transform && o is not GameObject)
//     Console.WriteLine($"{k:x16}::{o}");

watch.Stop();
Console.WriteLine($"Execution finished in {watch.ElapsedMilliseconds}ms.");

void PrintCab(Cab cab, Transform t, string ident="") {
    Console.WriteLine($"{ident}🎮 {t.GameObject.Val!.Name} {t.X}");
    foreach (var c in t.GameObject.Val!.Components) {
        var o = Point(c, cab);
        if (o is Animator aa) {
            var avatar = Point(aa.AvatarPtr, cab);
            Console.WriteLine($"{ident} ↳ {avatar.Name}");
        } else if (o is SkinnedMeshRenderer smr) {
            Console.WriteLine($"{ident} ↳🎭 {smr} bones={smr.Bones.Count}");
        } else if (o is not Transform)
            Console.WriteLine($"{ident} ↳ {o}");
    }
    if (t.GameObject.Val!.Name == "Bip001") return;
    foreach (var c in t.Children)
        PrintCab(cab, c.Val!, ident + "|");
}

T Point<T>(PPtr<T> pPtr, Cab cab) {
    // if (pPtr.FileId == 0)
        return (T)cab.Objects[pPtr.PathId];
    // var extCabName = cab.Externals[pPtr.FileId - 1];
    // var blk2 = new Blk(cabMap[extCabName]);
    // var cab2 = blk2.Cabs[extCabName];
    // // cache.Add(extCabName, cab2);
    // return (T)cab2.Objects[pPtr.PathId];
}

