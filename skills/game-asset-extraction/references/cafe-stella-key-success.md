# Cafe Stella 社区密钥成功路径（2026-08-08 实测全量解包）

补完 cafe-stella-case.md（v1 失败复盘）的成功分支。目标：从社区渠道拿到加密密钥 → 静态全量解包 → 资源转换。

## 关键事实
- 官方 morkt/GARbro 没收录 Cafe Stella（issue #396 自 2019 一直 open）
- **crskycode/GARbro（fork）的 Formats.dat 里有**：`Café Stella to Shinigami no Chou -> RiddleCxCrypt`
- 密钥在 release 的 `GameData/Formats.dat`，**不在源码**（YuzCrypt.cs 只有算法类）

## 步骤

### 1. 找 fork release
- GitHub 搜 `GARbro` fork：crskycode / nanami5270 / kyororay 都是活跃 fork
- 看 release 说明（"support several games"）或直接下载试跑
- 网络：直连被墙 → 代理（Clash Verge 127.0.0.1:10808），24MB 约 26 秒；
  无代理时 gh-proxy.com 镜像可用但限速 ~10KB/s（24MB 要 2 小时），不值得等

### 2. 下载解压 + 反射 dump 密钥
- 解压 release zip（python zipfile 最稳，UnRAR 对中文路径会误判）
- 用 csc 编译小工具，引用 ArcFormats.dll + GameRes.dll，加载 Formats.dat 后反射目标 scheme 字段：

```csharp
FormatCatalog.Instance.DeserializeScheme(File.OpenRead("GameData/Formats.dat"));
// 遍历 Xp3Opener.KnownSchemes，按 游戏名 过滤（注意 é / [Steam] 后缀）
// 反射 dump 字段：YuzKey / ControlBlock / m_mask / m_offset / PrologOrder / OddBranchOrder / EvenBranchOrder / m_random_seed
```

### 3. 本作密钥存档（对照用）
```
Café Stella to Shinigami no Chou -> RiddleCxCrypt（民间版=日文原版）
  YuzKey[6] = [0x9E8F879A, 0xCCDF9B91, 0x869DD2CD, 0x94DF9A8B, 0x27B9123C, 0x80724E1C]
  ControlBlock[1024]（完整在 D:\逆向\out\cafe\garbro-mod\Cafe_Stella_to_Shinigami_no_Chou.txt）
  m_mask=622, m_offset=146, m_random_seed=2463534242
  # 前 4 个 YuzKey 与 Riddle Joker 相同，后 2 个 + ControlBlock 不同 → 印证"同方案异密钥"
Café Stella to Shinigami no Chou [Steam] -> HxCrypt（Steam 版，参数不同）
```

### 4. 静态解包
- `GARbro.Console.exe <xp3>` 列目录；`-x` 提取
- **提取到 cwd**（不是 -o 指定目录），批量时 subprocess cwd 指向目标
- GARbro 按文件名 LookupGame 自动匹配 scheme → 民间版/Steam 版都能解
- 结果：11 个 xp3 → 39870 文件 / 6.8G，全真实文件名

### 5. 资源转换
- tlg → png：arc_unpacker 0.11 kirikiri/tlg 解码器，2097/2097 零失败（输出 cwd 同名 .png）
- pimg（PSB 容器）：GARbro 认识（当容器列出内部 tlg）→ `-x` 提取内嵌 tlg → 再走 arc_unpacker
  （每个 pimg 两次工具调用，108 个约 40 分钟）
- pbd：魔数 `TJS/4s0`，是 TJS 脚本（立绘合成描述 dress/face/base），不是图片，不用转
- .sinfo：立绘图层配置（UTF-16），同理保留不转

## 游戏路径（本机）
- 民间版：E:\galgame\星光咖啡馆与死神之蝶（2019 原版 + 汉化补丁 chsgrp.pck/chsvid.pck）
- Steam 版：E:\SteamLibrary\steamapps\common\CafeStella

## 工具与脚本（D:\逆向\out\cafe\）
- garbro-mod/（GARbro.Console.exe + Formats.dat + 密钥 txt）
- batch_extract.py / convert_tlg.py / convert_pimg.py / stella_unpack.py
