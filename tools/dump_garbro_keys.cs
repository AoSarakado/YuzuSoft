// GARbro Formats.dat 密钥 dump 工具模板
// 用途：从 GARbro（或 fork mod）的 GameData/Formats.dat 里导出指定游戏的加密 scheme 密钥
// 背景：GARbro 密钥不在源码，在 release 的 Formats.dat（zlib + .NET BinaryFormatter 序列化）
// 编译：csc /nologo /r:ArcFormats.dll /r:GameRes.dll /out:DumpGarbroKeys.exe DumpGarbroKeys.cs
// 用法：把本文件放到解压后的 GARbro mod 目录（与 ArcFormats.dll/GameRes.dll 同级），
//       编译后运行：DumpGarbroKeys.exe "Stella"     （参数为游戏名关键字，不区分大小写）
// 运行前提：同目录有 GameData\Formats.dat（mod release 自带）
// 实测：Cafe Stella 密钥导出成功（2026-08-08，crskycode/GARbro-Mod-1.0.2.2）

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GameRes;
using GameRes.Formats.KiriKiri;

class DumpGarbroKeys
{
    static void Main(string[] args)
    {
        string keyword = args.Length > 0 ? args[0] : "";
        string gd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData");
        using (var fs = File.OpenRead(Path.Combine(gd, "Formats.dat")))
            FormatCatalog.Instance.DeserializeScheme(fs);
        foreach (var kv in Xp3Opener.KnownSchemes)
        {
            // 关键字过滤；空关键字 = dump 全部 yuzu 系（Riddle/Nana/Cabbage/Senren/Yuzu/Dracu/Limelight）
            bool hit = string.IsNullOrEmpty(keyword)
                ? kv.Key.IndexOf("Riddle", StringComparison.OrdinalIgnoreCase) >= 0
                    || kv.Key.IndexOf("Stella", StringComparison.OrdinalIgnoreCase) >= 0
                    || kv.Key.IndexOf("Senren", StringComparison.OrdinalIgnoreCase) >= 0
                    || kv.Key.IndexOf("Cabbage", StringComparison.OrdinalIgnoreCase) >= 0
                    || kv.Key.IndexOf("Nana", StringComparison.OrdinalIgnoreCase) >= 0
                    || kv.Key.IndexOf("Dracu", StringComparison.OrdinalIgnoreCase) >= 0
                    || kv.Key.IndexOf("Limelight", StringComparison.OrdinalIgnoreCase) >= 0
                : kv.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!hit) continue;
            Console.WriteLine("=== " + kv.Key + " -> " + kv.Value.GetType().FullName + " ===");
            DumpFields(kv.Value);
            Console.WriteLine();
        }
    }

    static void DumpFields(object obj)
    {
        Type t = obj.GetType();
        while (t != null && t != typeof(object))
        {
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    object v = f.GetValue(obj);
                    string prefix = t == obj.GetType() ? "" : "  (base)";
                    if (v is uint[])
                    {
                        var ua = (uint[])v;
                        if (ua.Length <= 16 || f.Name.Contains("Key"))
                            Console.WriteLine(prefix + f.Name + "[" + ua.Length + "] = [" + string.Join(", ", ua.Select(x => "0x" + x.ToString("X8"))) + "]");
                        else if (f.Name.Contains("ControlBlock"))
                            // 完整输出 1024 个值：密钥文件需要完整 ControlBlock，Take(40) 截断会丢密钥
                            Console.WriteLine(prefix + f.Name + "[" + ua.Length + "] = [" + string.Join(", ", ua.Select(x => "0x" + x.ToString("X8"))) + "]");
                    }
                    else if (v is byte[] && ((byte[])v).Length <= 64)
                    {
                        var ba = (byte[])v;
                        Console.WriteLine(prefix + f.Name + " = [" + string.Join(", ", ba.Select(x => "0x" + x.ToString("X2"))) + "]");
                    }
                    else if (v is uint || v is int || v is string || v is bool)
                        Console.WriteLine(prefix + f.Name + " = " + v);
                }
                catch { }
            }
            t = t.BaseType;
        }
    }
}
