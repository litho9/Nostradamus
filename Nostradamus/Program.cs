using Nostradamus;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var watch = System.Diagnostics.Stopwatch.StartNew();
Console.WriteLine("** Nostradamus **");

var path0 = @"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks\";
var blk = new Blk(path0 + "3212318446.blk");
Console.WriteLine($"blk processed in {watch.ElapsedMilliseconds}ms.");
var cab0 = blk.Cabs["CAB-0aa2768ea164a0d7db932b50052974af"];
var root = (Transform)cab0.Objects.Values.Single(o => o is Transform { Father.PathId: 0 });
PrintCab(cab0, root);
// var rootPathId = 0x6d0996f1b0cd237d;
// Write(rootPathId);
// foreach (var objInfoMap in cab.Objects) {
//     Console.WriteLine($"{objInfoMap.Key:x8}: {cab.ReadObject(objInfoMap.Value)}");
// }

// var obj = cab.Objects[-6568161147572652636];
// var g = (GameObject)cab.ReadObject(obj);
// Console.WriteLine(g);
// foreach (var c in g.Components) {
//     var component = cab.ReadObject(cab.Objects[c.PathId]);
//     // Console.WriteLine(component);
//     if (component is Animator a) {
//         var avatar = Point(a.AvatarPtr, cab);
//         Console.WriteLine(avatar);
//     }
//     if (component is Transform t) {
//         Console.WriteLine(t);
//         foreach (var child in t.Children.Select(ch => Point(ch, cab))) {
//             Console.WriteLine($"  {child}");
//         }
//     }
// }

watch.Stop();
Console.WriteLine($"Execution finished in {watch.ElapsedMilliseconds}ms.");

void PrintCab(Cab cab, Transform t, string a="") {
    var g = Point(t.GameObject, cab);
    Console.WriteLine($"{a}🎮 {g.Name} {t.X}");
    foreach (var c in g.Components) {
        var o = Point(c, cab);
        if (o is Animator aa) {
            var avatar = Point(aa.AvatarPtr, cab);
            Console.WriteLine($"{a}↳ {avatar}");
        } else if (o is not Transform)
            Console.WriteLine($"{a}↳ {o}");
    }
    if (g.Name == "Bip001") return;
    foreach (var c in t.Children)
        PrintCab(cab, Point(c, cab), a + "|");
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
















