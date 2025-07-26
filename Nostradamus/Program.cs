using System.Numerics;
using System.Text;
using Nostradamus;

var watch = System.Diagnostics.Stopwatch.StartNew();
Console.WriteLine("** Nostradamus **");
// var cabMap0 = new Dictionary<string,string>();
// var path1 = @"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\Persistent\Blocks";
// ExtractCabNames(path1, "./persistent.txt", cabMap0);
// var path2 = @"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks";
// ExtractCabNames(path2, "./streaming.txt", cabMap0);
// Console.WriteLine($"CAB map built in {watch.ElapsedMilliseconds}ms.");

var path0 = @"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks\";
// var cache = new Dictionary<string, Cab>();
// var cab = Importer.LoadCab(path0 + "2991565378.blk", "CAB-670bbd3e00b8581a11ca5e0833cc562b");

// var path0 = @"C:\mod\downloads\assetStudioDumps\sampleBlks\1661020214.blk";
// var cab = FindCabName(@"C:\mod\downloads\assetStudioDumps\sampleBlks\1116910043.blk", 6257557361031075141);
// var cab = FindCabName(@"C:\mod\downloads\assetStudioDumps\sampleBlks\1116910043.blk", -145404673416549181);
// var cab = FindCabName(@"C:\mod\downloads\assetStudioDumps\sampleBlks\1116910043.blk", -4099066726693155252);
// var cab = FindCabName(@"C:\mod\downloads\assetStudioDumps\sampleBlks\725256930.blk", 151464532536747268);
// var cab = FindCabName(@"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks\2762031060.blk", -4684785624729543907); // Neko face
// var cab = FindCabName(@"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks\2762031060.blk", 4782166793769566738); // Neko hair
// var cab = FindCabName(@"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks\2762031060.blk", -7689337673308927675); // Neko weapon
// var cab = Importer.FindCabName(@"C:\Program Files\HoYoPlay\games\ZenlessZoneZero Game\ZenlessZoneZero_Data\StreamingAssets\Blocks\1443711241.blk", -3698059659076524130); // Neko body

// var cab = Importer.FindCabName(path0 + "1135071822.blk", -8189108865086251604); // Ellen face
// var cab = Importer.FindCabName(path0 + "1135071822.blk", 6572189710928896315); // Ellen body2 (hair)
// var cab = Importer.FindCabName(path0 + "3859696171.blk", [3417397351254354728]); // Ellen body1 (body)
// var cab = Importer.FindCabName(path0 + "1135071822.blk", 6051348117482440328); // Ellen weapon
// var cab = Importer.FindCabName(path0 + "1135071822.blk", [3495824013808196430]); // Ellen weapon1

// var cab = Importer.FindCabName(path0 + "1603015026.blk", [
//     2517273183572762172,
//     8537208255025032758,
//     -8793944780888160997,
//     -6144800201502013285,
//     6079899271867383927]); // koleda
// cache.Add(cabName, cab);

// var cab = Importer.FindCabName(path0 + "1984883942.blk", [-1993847924726774199]); // astra_body_1 (body)
// var cab = Importer.FindCabName(path0 + "3267960099.blk", [834371889628805990, -1651881802340334825,
//     -5644320311604663443, 5130587602689091552, -1854470823460582422, 486398751512964195, -8932569558258624725, 2439723831746476756]); // Grace
// var cab = Importer.FindCabName(path0 + "4236428796.blk", [-8334226123215613041, 6475116723146550135, -7853853869695093574, 5093981658556521906]); // Aokaku1
// var cab = Importer.FindCabName(path0 + "1777173822.blk", [-38056927830103713, 218089248927113965]); // Aokaku2

// var cab = Importer.FindCabName(path0 + "2913790002.blk", [614966783691556233, -3687289149166102235, -1904561940581043374, -7147812988480311680]); // Clara
// var cab = Importer.FindCabName(path0 + "1934887133.blk", [5365255431446343955, -7320360154012607135]); // Clara2
// var cab = Importer.FindCabName(path0 + "4236428796.blk", [-6500448696333484520]); // Clara3

// var cab = Importer.FindCabName(path0 + "2590200712.blk", [-9141530740745037958, -6822309956914440082]); // Unagi face & body_2
// var cab = Importer.FindCabName(path0 + "4075972532.blk", [-3190184381172704112, -2140911132297955029]); // Unagi hair
// var cab = Importer.FindCabName(path0 + "2562570398.blk", [-4675795542627727652]); // Unagi body_1
// var cab = Importer.FindCabName(path0 + "4075972532.blk", [-5574769458488961755, 8578745282876133531, -820515199674430258, 4474883144546551925, 5217364547154771440, 1420859704488569472, -3931734050289967829, -1947291744230173974, 2518905700658081685, 6257599974096207303]); // Unagi weapon & hairpin

// var cab = Importer.FindCabName(path0 + "129575367.blk", [1712335660054917389, -8377461652780390947]); // Yanagi

Mhy1.FindCabName(path0 + "3212318446.blk", [1712335660054917389]);

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
















