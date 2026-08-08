# Cafe Stella 静态解包完整案例（2026-08-08）

柚子社 krkrz 加密 galgame 从"密钥到手 → 全量解包 → 立绘转 PNG"的完整闭环。
前提：GARbro 官方没收录，KrkrDump 运行时拦截已拿到 224+ 文件但静态解包卡在密钥。

## 1. 密钥获取（社区渠道，成败分水岭）

**路径**：morkt/GARbro issue #396（Cafe Stella game request，2019 至今 open）评论区
→ jszhtian 线索："protection scheme is the same, but key is different"（同 Riddle Joker 算法，仅密钥不同）
→ 官方 GARbro 没收录 → 搜 fork → **crskycode/GARbro（GARbro-Mod）release 已支持**（用户实测）

**获取步骤**：
1. 代理直连 GitHub（Clash Verge 10808 启动后 24MB zip 26 秒下完；无代理时 gh-proxy.com 镜像限速 ~10KB/s 不可行）
2. 下载 GARbro-Mod-1.0.2.2.zip → 解压
3. `GARbro.Console.exe <游戏main.xp3>` 若能列出真实文件名 = mod 已支持（自动按文件名 LookupGame 匹配 scheme）
4. 密钥在 `GameData/Formats.dat`（zlib + .NET BinaryFormatter 序列化），不在源码——grep YuzCrypt.cs 必空
5. 用 scripts/dump_garbro_keys.cs 反射 dump（见 scripts/）

**Cafe Stella 密钥结果**：
```
Café Stella to Shinigami no Chou -> RiddleCxCrypt（民间版/日文原版）
  YuzKey[6] = [0x9E8F879A, 0xCCDF9B91, 0x869DD2CD, 0x94DF9A8B, 0x27B9123C, 0x80724E1C]
  ControlBlock[1024]（完整见 D:\逆向\out\cafe\garbro-mod\Cafe_Stella_to_Shinigami_no_Chou.txt）
  m_mask=622, m_offset=146, m_random_seed=2463534242
  （前 4 个 YuzKey 与 Riddle Joker 相同，后 2 个 + ControlBlock 不同——印证"方案相同密钥不同"）
Café Stella to Shinigami no Chou [Steam] -> HxCrypt（Steam 版参数完全不同）
```

## 2. 全量解包（GARbro.Console.exe headless）

**关键点：GARbro.Console.exe 提取输出到 cwd（不是 -o 指定目录）**。
批量解包时用 subprocess 的 cwd 参数切到目标目录：

```python
CONSOLE = r"D:\逆向\out\cafe\garbro-mod\GARbro.Console.exe"
r = subprocess.run([CONSOLE, "-x", xp3_path], cwd=dst_dir, ...)
```

**版本区分**：
- 民间版 = 日文原版 → RiddleCxCrypt（E:\galgame\星光咖啡馆与死神之蝶）
- Steam 版 = HxCrypt（E:\SteamLibrary\steamapps\common\CafeStella，带 .sig 文件 + steam_api.dll）
- 两个版本 mod 都能解，Console 按文件名自动匹配

**结果**：民间版 11 个 xp3 全量解包 39870 文件 / 6.8GB：
main 60、data 3、scn 74（.ks.scn 剧情脚本）、fgimage 1811 tlg、bgimage 118 PNG、
evimage 108 pimg、bgm 100（.opus+.sli）、voice 35185（.ogg，1.6G）、uipsd 28、patch 810、video 12

## 3. krkrz 立绘结构

- `fgimage/<角色>/` 按角色分目录（1_栞那、2_ナツメ、3_希、4_愛衣、5_涼音、6_ミカド、7_宏人、8_和史、9_ケットシー）
- 命名：`角色a_0_编号.tlg`（a=衣装差分，_0=差分维度，编号=资源 ID）
- 标准立绘尺寸约 508x1725 / 843x1769；小图为表情/装饰碎片
- `.pbd`（魔数 TJS/4s0）= 加密/混淆脚本，是立绘合成描述元数据，**不是图片**，无需转换
- `.sinfo` = 图层配置（dress/face/base 引用关系），同样无需转换
- 伴随 `.csv`（emotion/facethumbpos/facezoom）= 表情/立绘映射表

## 4. tlg → PNG 批量转换

arc_unpacker 0.11 的 kirikiri/tlg 解码器，2097 张零失败。单文件转，输出 cwd 同名 .png：

```python
subprocess.run([ARC, tlg_path], cwd=dirpath, ...)  # ARC = arc_unpacker011\arc_unpacker.exe
```

2097 张约 12-15 分钟（每张 ~0.5s），量大用后台任务跑。

## 5. pimg（PSB 容器）事件图

- `.pimg` 魔数 "PSB" = krkrz PSB 二进制序列化容器，内含多张 tlg 图层
- **arc_unpacker 直接喂 pimg 会卡住（超时）**——不要试
- **GARbro.Console.exe 认识 pimg**：`-x` 提取出内嵌 tlg（44.tlg/60.tlg...），再 arc_unpacker 转 png
- 108 个 pimg 约 40+ 分钟（每个要 GARbro 提取 + arc_unpacker 转换两次工具调用）
- 输出按 pimg 名分目录（evimage_png/ev101a/10.png...）

## 6. 本机路径备忘

- 民间版：E:\galgame\星光咖啡馆与死神之蝶（2019 原版 + 汉化补丁 chsgrp.pck/chsvid.pck）
- Steam 版：E:\SteamLibrary\steamapps\common\CafeStella
- GARbro mod：D:\逆向\out\cafe\garbro-mod\（GARbro.Console.exe 可复用）
- 密钥 txt：D:\逆向\out\cafe\garbro-mod\Cafe_Stella_to_Shinigami_no_Chou*.txt
- 解包输出：D:\逆向\out\cafe\full\
- 脚本：convert_tlg.py / convert_pimg.py / batch_extract.py / stella_unpack.py
- 资源清单：D:\逆向\out\cafe\资源清单.md
