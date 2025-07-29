using Nostradamus;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var watch = System.Diagnostics.Stopwatch.StartNew();
Console.WriteLine("** Nostradamus **");

var path0 = Environment.GetEnvironmentVariable("GAME_PATH");
var blk = new Blk(path0 + "3212318446.blk");
Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");

var blk2 = new Blk(path0 + "2299538835.blk"); // SeparateMesh_Avatar_Female_Size01_Jufufu_Model_Jufufu_Body_1
var cab2 = blk2.Cabs["CAB-cd0a7c1addc386d573a202aad65e4aff"];


var cab0 = blk.Cabs["CAB-0aa2768ea164a0d7db932b50052974af"];
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


// void Write(long pathId, string ident="") {
//     var t = (Transform)cab.ReadObject(cab.Objects[pathId]);
//     var o = (GameObject)cab.ReadObject(cab.Objects[t.GameObject.PathId]);
//     Console.WriteLine($"{ident}{o.Name}");
//     foreach (var readObject in o.Components
//                  .Select(c => cab.ReadObject(cab.Objects[c.PathId]))
//                  .Where(readObject => readObject is not Transform)) {
//         Console.WriteLine($"{ident}- {readObject}");
//         // if (o.Name == "maoyou_body" && readObject is MonoBehaviour mb) {
//         //     var script = cab.ReadObject(cab.Objects[mb.Script.PathId]);
//         //     Console.WriteLine($"{ident}-- {mb.Name}::{script}");
//         // }
//     }
//     foreach (var child in t.Children) {
//         Write(child.PathId, ident+"  ");
//     }
// }
