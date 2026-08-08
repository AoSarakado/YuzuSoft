# GARbro 密钥完整 dump 配方（柚子社系，2026-08 实测）

目标：从 GARbro 的 Formats.dat（GARbroDB 数据库）dump 出某游戏的 xp3 解密密钥，
生成 GARbro 密钥 txt 文件（格式同社区分享的 `游戏名.txt`）。

## 背景

- 密钥数据在 **fork/mod 的 GameData/Formats.dat**（GARbroDB 二进制），**不在源码**
  （YuzCrypt.cs 等源码 grep 游戏名必空）。
- `Xp3Opener.KnownSchemes` 运行时从 Formats.dat 反序列化出所有加密方案，
  用反射工具 dump 字段即得密钥参数（m_mask/m_offset/PrologOrder/OddBranchOrder/
  EvenBranchOrder/ControlBlock[1024]/YuzKey/m_key1/m_key2 等）。
- GARbro-Mod（crskycode）的 Formats.dat 已收录柚子社多作：
  Cafe Stella、Riddle Joker、Dracu-Riot!、Senren★Banka（千恋万花）、
  Limelight Lemonade Jam（2025 新作，中文圈常叫"柠檬工厂"）、Sanoba Witch 等。

## 步骤

### 1. 准备 dump 工具源码

直接用模板 `scripts/dump_garbro_keys.cs`（2026-08 已修正两处历史坑，无需再改）：
- ControlBlock 已完整输出 1024 个值（旧模板 `Take(40)` 截断会丢密钥）
- 空关键字过滤已含 `Dracu`/`Limelight`（旧版缺这两个，Dracu-Riot! 和 Limelight 不显示）

若要自定义过滤/输出：反射 dump 逻辑参考以下要点——
- ControlBlock 必须完整 join（`string.Join(", ", ua.Select(x => "0x" + x.ToString("X8")))`）
- 过滤关键词按需补 `Dracu`/`Limelight`，或干脆不过滤全部 dump。

### 2. 编译（csc，.NET Framework）

```bash
cd <garbro-mod 目录>
"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe" /nologo \
  /out:DumpAllYuzuFull.exe \
  /r:GameRes.dll /r:ArcFormats.dll /r:System.Text.Encoding.CodePages.dll \
  DumpAllYuzuFull.cs
```

- 坑：`/r:ArcFormats.dll.config` 会报 CS0009（config 不是程序集），别引用 .config。
- 编译产物 exe 必须和 GameData/Formats.dat 同目录运行（`DeserializeScheme` 读
  `AppDomain.CurrentDomain.BaseDirectory/GameData/Formats.dat`）。

### 3. 运行

```bash
./DumpAllYuzuFull.exe > dump.txt 2>&1
grep -a "^===" dump.txt        # 方案列表（文件含非 UTF-8 标题，grep 需 -a）
```

输出每段：`=== 游戏名 -> GameRes.Formats.KiriKiri.xxxCrypt ===` + 字段行
（`  m_mask = 308`、`  (base)ControlBlock[1024] = [0x..., ...]`）。

### 4. 生成密钥 txt

格式（同社区分享格式 / 已有 Cafe Stella 密钥文件）：
- 每行 `字段=值`，去掉 dump 的缩进与方括号：
  `m_mask=308`、`ControlBlock[1024]=0x9C5725A9,0x6F5AB197,...`（完整 1024 个）
- 基类字段加 `base_` 前缀：`base_ControlBlock[1024]=...`
- 数组值 `[0x.., 0x..]` → 去括号、逗号分隔无空格

解析注意：
- **CRLF 坑**：re.split 按 `^=== (.+?) -> (.+?) ===$` 切段时，CRLF 的 `\r`
  会挡在 `===` 和行尾 `$` 之间导致 split 失败（段数=1）——先
  `data = data.replace('\r\n', '\n')`。
- **★ 乱码坑**：ISO-8859-1 解码下 Senren★Banka 的 ★ 变乱码（`Senren£ªBanka`），
  按标题精确匹配 TARGET 字典会漏 —— 按类型（`SenrenCxCrypt`）或标题前缀匹配。
- dump 字段行解析正则：
  `^(\(base\))?\s*(.+?)\s*=\s*(.+)$`，`base` 前缀保留为 `base_字段名`。

### 5. 验证

- 密钥文件里 `ControlBlock` 逗号分割数 == 1024（CxEncryption 等简单加密方案
  没有 ControlBlock，只有 mask/order 几行，属正常）。
- 实测产物：Riddle_Joker、Riddle_Joker_[Steam]、Dracu_Riot、Dracu_Riot_[Steam]、\n  Senren_Banka、Limelight_Lemonade_Jam、Sanoba_Witch 共 7 个新文件 + Cafe Stella 2 个。\n- Sanoba Witch 是 YuzuCrypt 通用加密，**无独立参数**（dump 只有 base 的\n  StartupTjsNotEncrypted/ObfuscatedIndex 两行）——密钥文件只需标题注释说明\n  \"通用加密，用 GARbro 内置支持即可\"，不必追求参数。
- 上传仓库 keys/ 时同步更新 README 支持游戏列表。

## 关联

- 社区密钥检索路径：references/yuzu-community-key-hunting.md
- 解包主流程：阶段 3.2 GARbro.Console.exe headless
