# 本机逆向工具盘点（D:\逆向，2026-08-07 整理后）

## 目录结构
```
D:\逆向\
├── cafe/                        # 加密资源目录（当前空，目标文件已移走）
├── CG提取工具/                  # 用户下载：krkrExtract GUI 版、ExtractData 1.20 汉化、
│                                #   CG 合成 perl 脚本（pimg 合并）、krkr立绘合成工具、要安装的依赖
├── die_win64_portable_3.21_x64/ # Detect It Easy 便携版（引擎识别）
├── GAL-TOOLS/                    # 已完整解压（密码 baiyu 验证有效）：UE4专区(umodel)、Unity专区(AssetStudio v0.16)、啥都有专区(arc_unpacker 0.11 等)
├── GARbrosdasdwas/              # GARbro 用户版
├── IDA Professional 9.3/        # IDA 便携副本（与 C:\Program Files 正式安装同内容，可删）
├── krkr解包封包工具合集/        # 2023 年收集的 krkr 工具全家桶（最值钱）
├── symbols-snapshot_2026-05-27/ # x64dbg 的 PDB 符号快照（x32/x64/x96dbg.pdb）——本体已补下载到 tools\x64dbg
├── tools/                       # 主工具目录（见下）
└── archive/                     # 归档：压缩包 + game_dump.bin(525MB) + 测试产物
```

## tools/ 现状
- dump_mem.py —— 手写内存 dump 脚本（改 PID 即用；有专业版 KrkrDump 可替代）
- DumpKeys.cs / GarbroExtract.cs —— 上次分析的 C# 思路记录（保留）
- GARbro-new/ —— GARbro 2019 v1.5.44（最新 release）
- garbro-src/ —— GARbro 源码（要改格式支持时用）
- KrkrExtract/ —— KrkrExtract 5.0.0.2 Lite 命令行版（最新；**被 Defender 拦，本机跑不了**）
- arc_unpacker011/ —— arc_unpacker 官方 0.11 release（2019 最终版，vn-tools/arc_unpacker；tlg→png 转换实测可用，解柚子社 xp3 加密无效）
- ida_mcp_bridge.py + start-ida-mcp.cmd —— IDA MCP 桥接与启动器（见 ida-mcp-setup 技能）
- texconv/ —— DirectXTex 2026.5.8.1（微软官方，DDS/贴图转换，已验证）
- vgmstream/ —— r2117 CLI（万能音频解码，已验证）
- x64dbg/ —— x64dbg 本体 snapshot 2026.05.27（与 symbols-snapshot 同版本，release\x64\x64dbg.exe）
- RenderDoc 1.45 —— 已装 C:\Program Files\RenderDoc\qrenderdoc.exe（winget 装，官网被墙走 winget 通道）

## krkr 工具合集速查（krkr解包封包工具合集/）
- 1.xp3_upk（krkr2 无密码解包）
- 2.GARbro（通用首选）
- 3.xp3viewer（加密 krkr2 专用 viewer）
- 3H3arcconv（arc 文件转换）
- 4.krkrextract（krkrz 有密码！内含 krkrextract1.0.3.1 + KrkrExtract4.0.1.4 两个版本）
- 5.kirikiritools（KirikiriDescrambler 解扰器、Xp3Pack 封包器）
- 6.xp3dumper（xp3dumper_gui.exe）
- 7.krkrdump（KrkrDump.dll + KrkrDumpLoader.exe——专业内存 dump，比自写脚本强）
- 8.xp3封包工具
- AssetStudioGUI（ress 文件，Unity 系）
- crass（强大但难用）、date（通用但老旧）、PAK提取软件、paz（中二社专用）

## GAL-TOOLS 啥都有专区速查（D:\逆向\GAL-TOOLS\GAL-TOOLS 2022 A08-password\GAL-TOOLS 2022 A08-password\啥都有专区\）
- arc_unpacker/ —— arc_unpacker 0.11 万能解包器（vn-tools 系，支持海量引擎含 krkrz）+ 2016 旧 exe；AssetStudio.x64.v0.14.38 也在
- arc_conv、arc_unpacker、ExtractData、crass.rar —— krkr/arc 系
- GALGAME引擎识别工具 —— 引擎识别补充
- xp3_dumper_gui_0.2、krkr2comptools_120410、krkr2plugin_3 —— krkr2 专用
- 其余：ALMA/atuworks/AXL/Constructor 等各家厂商专用打包解包工具

## 使用注意
- **加密 krkrz 优先 KrkrDump 动态拦截**（2026-08 实测：柚子社 Cafe Stella 一次成功，详见 references/krkdump-runtime-extraction.md）；静态工具对柚子社类加密基本无解：arc_unpacker 0.11 报 "No plugin was selected"、KrkrExtract 被 Defender 拦、KirikiriDescrambler 报 not scrambled
- 静态优先顺序参考：arc_unpacker 0.11 > KrkrExtract 5.0.0.2 > 合集 4.0.1.4/1.0.3.1 > GARbro（明文/普通加密包仍可用）
- 解包产出示例：D:\逆向\out\cafe\（dump 解密资源 + tlg 原始 + png 转换成品）
- GAL-TOOLS rar 密码：baiyu（已验证），已完整解压到 D:\逆向\GAL-TOOLS\
- x64dbg 本体 + 符号齐全（tools\x64dbg + symbols-snapshot）——动态调试就绪
- RenderDoc 1.45 已装（winget），抓帧可用
- IDA 两份副本（C:\Program Files 正式 + D:\逆向 便携），MCP 用 C 盘那份
- game_dump.bin（525MB 旧内存 dump）已归档到 archive/，确认无用可删
- **Windows 原生工具不吃 MSYS /d/ 路径**：UnRAR 等要传 D:\... Windows 路径；bash 里末尾反斜杠+引号会转义炸掉，去掉末尾反斜杠
