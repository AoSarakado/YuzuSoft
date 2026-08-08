# 柚子社 yuzu 系社区密钥检索实战（Cafe Stella，2026-08）

## 结论速查
- Cafe Stella（星光咖啡馆与死神之蝶）密钥在社区**存在**，但不在源码里——封在 GARbro fork 的 Formats.dat
- 算法与 Riddle Joker 相同（RiddleCxCrypt / yuz: 段），密钥不同 —— morkt/GARbro issue #396 评论区确认（"the protection scheme is the same, but key is different"）
- 官方 morkt/GARbro **未支持**（issue #396 至今 open）；**crskycode/GARbro 的 GARbro-Mod release 已支持**（PR #186 明确提到 "Café Stella to Shinigami no Chou" crypt，issue 里有用户实测提取成功）

## 社区密钥检索路径（按优先级）
1. **GARbro GitHub issues 搜索**：`api.github.com/search/issues?q="Cafe Stella"+repo:morkt/GARbro`
   - game request issue 是金矿：评论区常给出"方案相同、密钥不同"的线索，或直接指向可用 mod
2. **GARbro fork/mod 家族**（按活跃度，都要查）：
   - crskycode/GARbro（GARbro-Mod，持续 release，2026 仍在更新）
   - nanami5270/GARbro-Mod（无 release，但常被 crskycode 引用合并）
   - kyororay/GARbro
   - 密钥在其 **Formats.dat**（release zip 内 ArcFormats/Resources/Formats.dat），**不在源码**
3. **本地 GARbro 源码 grep 陷阱**：YuzCrypt.cs / ArcXP3.cs / KiriKiriCx.cs 里搜 Stella 一律为空——scheme 数据是序列化的，源码只有算法类，没有游戏→密钥映射
4. **KrkrExtract 支持某游戏 ≠ 密钥公开**：KrkrExtract 是运行时注入 dump（让游戏自己解密），能解所有 yuzu 游戏但内部没有静态密钥表（搜索其 dll 找不到 Riddle YuzKey 字节为证）

## Formats.dat 格式
```
"GARBRO"(6B magic) + int32 version + zlib(BinaryFormatter(SchemeDataBase))
```
- 序列化/反序列化代码：GameRes/FormatCatalog.cs 的 SerializeScheme / DeserializeScheme
- GitHub contents API 对 >1MB 文件不给 content 字段（只回 download_url），需用 git blobs API 或直接下 release zip

## 枚举 KnownSchemes（本地工具）
```csharp
// ListAllSchemes.cs —— 编译：csc /nologo /r:ArcFormats.dll /r:GameRes.dll /out:X.exe X.cs
using GameRes; using GameRes.Formats.KiriKiri;
foreach (var kv in Xp3Opener.KnownSchemes)
    Console.WriteLine(kv.Key + " -> " + kv.Value.GetType().FullName);
```
- GARbro-new 自带 DumpKeys2.exe 只过滤 "Riddle" 关键词——dump 新游戏要改过滤条件重编译
- 反射 dump 目标字段：`YuzKey (uint[6])` + `ControlBlock (uint[1024])`；用 DumpKeys.cs 的 DumpFields 模式（反射遍历实例字段，含基类）

## 本机网络坑（GitHub 访问，代理未运行时的备选通道）
- 终端 curl 直连 GitHub 被墙；代理 127.0.0.1:10808 可能未运行（Clash Verge 装了但没启动，ProxyEnable=0）
- **browser 工具能通 api.github.com**（GET JSON 可以），但 raw.githubusercontent.com / codeload.github.com DNS 污染
- browser console 对 gzip/非 UTF-8 响应报 `'utf-8' codec can't decode`：用 `fetch().then(r=>r.text())` 拿原始文本，再 JS 里 TextEncoder→btoa 转 base64 绕开
- GitHub contents API 1MB content 限制；大 blob 响应在 browser console 30s 超时（拿不回 2.6MB base64）
- **gh-proxy.com 镜像可达但限速 ~10KB/s**（支持 Range，24MB zip 需 1-2 小时）；ghfast.top 部分路径 403；github.moeyy.xyz / ghproxy.cc 不通
- 大文件下载优先级：让用户启动 Clash Verge 开代理（最快）> gh-proxy 分片后台慢慢拉 > 浏览器

## 拿到 Formats.dat 后
1. 解压 release zip，把 mod 的 Formats.dat 替换 GARbro-new/GameData/Formats.dat（或与 exe 同目录）
2. 跑改好的 DumpKeys（过滤 Stella 关键词）导出 YuzKey/ControlBlock
3. 用 PROGRESS.md 里已实现的 YuzDecryptor（test_yuz_decrypt.py）验证 yuz 段 zlib 头（78 9C/DA/01）
