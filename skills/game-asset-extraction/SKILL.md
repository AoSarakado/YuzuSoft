---
name: game-asset-extraction
description: "Use when 解包游戏/提取资源/拆包找 shader 贴图音频。引擎识别→解包→转换五阶段工作流，TA 向。"
version: 1.0.0
author: 四季夏目
license: MIT
platforms: [windows, linux, macos]
metadata:
  hermes:
    tags: [game, unpack, extraction, reverse, ta, shader, assets]
---

# 游戏资源解包工作流

解包游戏资源（CG/BGM/特效/shader/模型）用于 TA 学习研究。源自柚子社 Cafe Stella 实战复盘 + 通用解包四步法。

## 使用边界（先读）
- 只解自己拥有的正版游戏；提取资源仅个人学习研究，不传播、不商用
- 涉及汉化补丁的部分不碰、不公开

## 工作流（5 阶段）

### 阶段 0：边界确认
确认拥有正版；明确目标资源类型（CG/BGM/特效/shader/脚本）。产出目标清单。

### 阶段 1：引擎识别（10 分钟）
- 目录结构特征：`*.pak` → UE；`*_Data` → Unity；`*.xp3`/`*.nsa` → Kirikiri；`data.win` → GameMaker
- Detect It Easy / ExifTool 扫主 exe 确认引擎与版本
- `file` 识别可疑文件；熵估算判断是否加密（熵 ~8.0 且无魔数 → 大概率加密）
- 决策点：加密 → 阶段 2；明文 → 直接阶段 3

### 阶段 2：封包分析（仅加密时）
- 读魔数/头结构；解压 index 区（zlib 常见）
- 找文件名表/映射表/加密段
- 数据段特征：单字节周期 → 流加密；块重复 → ECB
- 决策点：密钥可得 → 阶段 3；不可得 → 旁路

### 阶段 3：工具解包（失败按序降级）
- Kirikiri（krkr/krkrz）：GARbro → KrkrExtract（新版优先）→ arc_unpacker（0.11，覆盖面最广，含 krkrz）→ 专用工具补刀（xp3viewer 加密 krkr2 / xp3dumper / KrkrDump 内存 dump / KirikiriDescrambler 解扰 / ExtractData+Susie 插件 / crass）→ 社区密钥注入 → 内存 dump
- Unity：AssetStudio → UABEA → UnityPy（脚本化批量）
- UE4/5：FModel（含 shader 导出）→ CUE4Parse
- 其他：按 DIE 结果（UndertaleModTool / RPG Maker 明文等）
- 旁路（工具全失败）：搜社区教程/密钥（贴吧/B站/汉化组）→ **动态拦截（KrkrDump，见下）** → 内存 dump 找密钥 → RenderDoc 抓帧兜底

### 阶段 3.2：GARbro.Console.exe headless 解包（拿到密钥后的全量解包利器，2026-08 实测）

GARbro release 自带命令行版 `GARbro.Console.exe`，拿到密钥（mod 的 Formats.dat）后可全量解包：
- `GARbro.Console.exe <file.xp3>` —— 列出内容（能列出真实文件名 = scheme 匹配成功）
- `GARbro.Console.exe -x <file.xp3>` —— 提取全部文件，**输出到 cwd（不是 -o 指定目录）**
- `-c FORMAT` —— 转换图像格式
- 按文件名自动 LookupGame 匹配 scheme，同一 mod 可解多版本（民间版 RiddleCxCrypt / Steam 版 HxCrypt）
- 批量解包：subprocess 的 cwd 参数切到目标目录（见 references/cafe-stella-static-unpack.md）
- 密钥 dump 工具模板：scripts/dump_garbro_keys.cs

### 阶段 3.5：动态拦截（加密 galgame 的王牌，2026-08 实测成功）

静态工具解不开加密封包时，**不要急着逆向找密钥**——先试 KrkrDump 运行时拦截：
把 dll 挂进游戏进程，游戏自己解密 xp3 时把数据导出来，拿到的还是真实文件名。
柚子社 Cafe Stella 就是这条路一次成功的：GARbro/arc_unpacker/KrkrExtract 全灭后，
KrkrDump 直接出 224+ 文件（立绘/BGM/脚本全真名）。完整配方见
references/krkdump-runtime-extraction.md。

**全量触发技巧（实测）**：游戏自带的"文件损坏检查工具"（如
ファイル破損チェックツール.exe）会遍历校验所有 xp3——把 KrkrDump 注入它，
一次检查 = 全量 dump，比推剧情加载快得多（Cafe Stella 从推剧情逐张加载
升级到检查工具直接批量出）。注意该工具窗口可能最小化到屏幕外
（GetWindowRect 返回 -32000），ShowWindow(SW_RESTORE=9) 拉回再操作。

### 阶段 4：资源转换与清洗
- 贴图：texconv（DDS→PNG）、BC/ASTC 解码
- 音频：vgmstream（万能）、wwiser（Wwise .bnk）、fmod2unity（FMOD .bank）
- 模型/动画：AssetStudio→FBX；FModel→glTF/FBX
- shader：FModel 导字节码 / RenderDoc 抓帧 → spirv-cross / dxil-spirv 反编译
- 脚本（Gal 演出）：.ks/.tjs 直接读

### 阶段 4.2：krkrz 立绘/事件图（柚子社系，2026-08 实测）

**立绘结构**（fgimage/）：
- 按角色分目录（1_栞那、2_ナツメ...），命名 `角色a_0_编号.tlg`（a=衣装差分，编号=资源 ID）
- **`角色a_0_编号`（508x1725/843x1769）= 身体层，脸区是空的（无五官）**——2026-08 用户目测纠正：像素级深色特征启发式（眼睛/眉毛/嘴 `(r+g+b)/3<110` 或 `(r>160&g<110&b<130)` 占比 ≥1%）会把刘海/轮廓线误判成五官（实测 141 张"带五官"全是空脸），**像素五官检测不可靠，以用户目测为准**；带五官的完整立绘 = 身体层 + 表情碎片 alpha_composite 合成，但**自动贴脸合成已被用户否决**（身体层是空脸，从图本身无法确定眼睛坐标，试过多个变体位置全不对；表情碎片是 149x120 左右的透明底细条——内部只有 2 条深色横线（y≈10% 和 y≈50%），是眉毛/眼睛线条局部，不是完整脸贴图；face id → 碎片映射又在加密 pbd 里）——**用户选择在图像软件里手动贴合**，未来同类任务直接交付素材（身体层 + 表情碎片）并说明"碎片是 1x 尺寸，贴 2x 大图（1686x3538）需放大 2 倍"即可，别花时间做自动合成实验变体（_确认图 实验目录已删）
- **表情碎片 = 角色目录里的小图**（149x120 等，用户确认是表情）；完整立绘 = 身体层 + 表情碎片 alpha_composite 合成；face id → 碎片文件映射在加密 pbd（TJS/4s0 字节码）里，静态拿不到，需运行时提取（KrkrDump）
- **`角色a_编号`（无 _0_，1686x3538）= 同姿态 2x 高清版**（2 倍尺寸成对出现），可作高清源
- 小图（149x120 等）= 表情/装饰碎片；腕/肢体差分图肤色占比 100%（识别 H 立绘时需排除）
- `.pbd`（魔数 TJS/4s0）= **加密 TJS 字节码**（内容高熵乱码），是立绘合成描述元数据（衣装→tlg 映射在此），**不是图片**，静态无法解析，需运行时提取（KrkrDump）
- `.sinfo` = 图层配置（dress/face/base 引用），**UTF-16LE**（用 utf-8 读会因 \u0000 丢失 face 行）；dress 衣装名含「裸」「裸私服」= R18 衣装标记；face 行给表情 id 但无文件映射

**tlg → PNG**：arc_unpacker 0.11 的 kirikiri/tlg 解码器批量零失败
（单文件转，输出 cwd 同名 .png；2097 张约 12-15 分钟，量大用后台任务）

**pimg（事件图，魔数 "PSB"）**：
- PSB 容器内含多张 tlg 图层
- **arc_unpacker 直接喂 pimg 会卡住（超时）——别试**
- **GARbro.Console.exe 认识 pimg**：`-x` 提取出内嵌 tlg → 再 arc_unpacker 转 png
- 108 个 pimg 约 40+ 分钟（每个两次工具调用），输出按 pimg 名分目录

完整配方：references/cafe-stella-static-unpack.md

### 阶段 5：归档与学习（TA 向）
- 目录规范：`<游戏>/<类型>/`（CG、BGM、SFX、Textures、Shaders、Models、Scripts）
- 学习路径：拆特效 → 材质网络 → 抓帧对照 → 笔记沉淀
- **解包资产可复用为 ML 数据集**（学校项目/数据清洗实验）：逆向后单文件做类型分类（魔数/熵/字节分布特征，每类限量 300），逆向前容器做识别加分项——完整配方见 references/ml-dataset-from-assets.md

### 阶段 5.2：资产整理归档（Gal 系，2026-08 实测）
- **硬链接归档省空间**：`os.link(src, dst)` 同卷 NTFS 零拷贝建链，几万文件秒级完成；assets 与 full 互为镜像，删任何一边不影响另一边（README 里写清楚"移动用复制别用剪切"）
- **语音按说话人分组**：柚子社 voice 命名 = `角色缩写3字母+场景号4位_序号.ogg`（kan/nat/noz/mei/suz/mik/hir/kaz → 栞那/ナツメ/希/愛衣/涼音/ミカド/宏人/和史），charvoice.csv 的匹配正则 `^([^\d]+)\d\d\d\d[^\d]*$` 就是解析规则；前 3 字母不在角色表 = 路人/未使用（1510 个）
- **BGM 曲名映射在 soundlist.csv**（UTF-16LE）：`BGMxx,曲名` 可直接重命名 opus 为 `编号_曲名.opus`；.sli 是同步/歌词数据，保持原名即可
- **统计坑**：`sum(len(fs) for dp,_,fs in os.walk(d) for f in fs if ...)` 生成器会按匹配文件数重复 yield len(fs)，把目录文件数放大（185635 虚数 → 实际 2693）。正确写法：先 walk 数 len(fs)，再单独数匹配文件
- **脚本 .scn 是 PSB 编译容器**（魔数 50 53 42 00 = "PSB\0"），不是明文；明文脚本是 main/ 的 .tjs/.ks（UTF-16LE + BOM）
- **资产公开仓库推送（GitHub，2026-08 实测）**：素材仓库（AoSarakado/YuziSoft）推送前**先与用户确认排除项**——视频（单文件可能 >100MB 超 GitHub 限制）和语音（几万个小 ogg 严重拖慢 git，25974 个推不动）通常不传；.gitignore 排除后剩 ~2900 文件 1.4GB 可正常推。Windows 中文路径仓库需 `git config core.longpaths true`；本机 GitHub 需 `git config http.proxy http://127.0.0.1:10808`（+https.proxy）。**坑：kill 后台 git 任务（push 中途被杀）会留 `.git/index.lock`**，后续 commit 报 "may have crashed in this repository earlier: remove the file manually to continue"——`rm -f .git/index.lock` 后重试即可；分支 `git branch -M main` + `git push -u origin main`
- **仓库改名（GitHub API，2026-08 实测）**：从 Windows 凭据管理器取 token（`echo "url=https://github.com" | git credential fill` 的 password 行就是 PAT）→ `curl -s -x http://127.0.0.1:10808 -X PATCH "https://api.github.com/repos/OWNER/OLDNAME" -H "Authorization: token $TOKEN" -d '{"name":"NEWNAME"}'`（token 读进变量不外泄）→ 本地 `git remote set-url origin https://github.com/OWNER/NEWNAME.git`；旧 URL 自动重定向
- **素材目录改名**（如 full/ → "Cafe Stella/"）：`mv` 后 `git add -A`，git 自动识别 100% rename（内容 hash 不变只记 tree 变化），推送量极小；同步改 .gitignore 排除路径与 README 引用

### 阶段 5.3：PSB 编译脚本字符串提取（台词/BGM/voice，2026-08 实测）
krkrz .ks.scn（PSB v3）的**字符串表在文件后段，\x00 分隔的 UTF-8 明文**（不是 UTF-16LE！）：
- 提取：`data.split(b'\x00')` 每段 UTF-8 解码，过滤含日文（假名/汉字）或纯 ASCII（len>=3）的串
- **台词**：含「」的串；旁白/选项/演出注释（→歩く／床、▽ドア閉める、★ぶつかる５）也在表里
- **BGM 引用是小写**：`bgm01` / `bgm_BGM03` 形式（大写 BGM 搜不到），正则 `(?<![A-Za-z])bgm(\d{1,2})` + `bgm_?(BGM\d{1,2})`
- **voice 引用**：`角色缩写+场景号_序号`（kan108_001），正则 `^(kan|nat|noz|mei|suz|mik|hir|kaz|rok|miy|yui)\d{3}_\d+`
- **坑：字符串表按 UTF-8 字节序排序**，不是演出顺序——台词/voice 的"顺序"是字典序，逐句顺序与角色归属需要解析 PSB 指令区（进阶工作，产出"全集"足够用时不必做）
- 产出范例：台词台本每章 txt（章节名自带角色线：100 栞那/201 ナツメ/300 希/401 愛衣/501 涼音）+ BGM-章节双向对照 CSV（BGM62 只在ナツメ线、BGM61 只在栞那线这类角色专属曲一目了然）
- PSB v3 头：magic(4)+version(4)+0x2c 起偏移数组；pip 的 `psb` 包是树莓派 GPIO 库，别装

### 阶段 5.4：背景引用提取 + BGM 配背景视频批量生成（2026-08 实测）
- **背景引用**：scn 字符串表里直接命中 bgimage 文件名（去扩展名），但**大小写不敏感**——scn 引用小写 `マンション_主人公部屋a`、文件是大写 `主人公部屋A.png`，匹配必须 lower() 化
- **BGM-背景链路**：章节到BGM CSV（csv 模块解析，字段内逗号会坑 split）+ 每章背景引用 => BGM→背景集合；角色专属曲的背景与角色线吻合（BGM62→ナツメの部屋）
- **批量视频**（背景轮播 + BGM 音频）：ffmpeg concat demuxer（`file 'path'` + `duration 6`）→ scale+pad 到统一分辨率 → libx264+aac，`-shortest` 对齐音频时长
- **坑：concat demuxer 的 file 路径含单引号会截断**（BGM11_I'm Busy! 失败于 "BGM11_Im"）——文件名带 `'` 的必须先把图复制/硬链接到临时目录的序号安全名（bg_001.png）再写列表
- **大坑：concat demuxer 图片轮播的时间戳跳变**（2026-08 实测 41/42 视频中招）：`file 'x.png' + duration 6` 的列表会让 demuxer 把多个 6s 块合并成一个大 packet（duration 12.03/17.97/24/30/42 ≈ 6 的倍数），PTS 显示时跳 6-18s，播放/剪辑软件卡顿。检测：`ffprobe -show_packets -show_entries packet=pts_time,duration_time` 找 duration>1s 的包；显示顺序连续性要把 PTS 排序后检查（h264 B 帧会让存储序 PTS 看似乱跳，那是正常的）
- **正确做法：concat filter 替代 demuxer**——每张图一个输入 `-framerate 30 -loop 1 -t 6 -i img.png`，filter_complex 各自 scale+pad 后 `concat=n=N:v=1:a=0`，再 map 音频。时间戳精确（实测 3834 帧 0 跳变），路径带 `'` 也没问题（list 参数不走 shell）
- 参考样例参数可用 ffprobe 反推（1788x1080 = 背景 2120x1280 等比例缩放，30fps）；BGM 时长匹配样例视频时长可反推用户做了哪些
- **柚子社 BGM 编号段位体系**（跳号是分类预留，非缺文件）：01-36 场景曲、51-55 = 主题歌 InstVer（伴奏）、61-65 = 同曲 QuietVer（抒情，与 51-55 成组配对）、91 = 标题画面曲（不走剧情脚本，剧本字符串里搜不到）、SongOP/SongED = 歌曲本体；角色线专属曲看 61-65 段（Cafe Stella：BGM61 栞那/62 ナツメ/63 希/64 愛衣/65 涼音）
- **剪辑向组织**：按章节建文件夹放该章 BGM（从 06_BGM 硬链接带曲名版本）+ 台词台本，CSV 读带引号含逗号字段必须用 csv 模块（split(',') 会把 "BGM01,BGM03..." 拆碎只留第一个）
- **背景引用提取**：字符串表里命中 bgimage 文件名（去扩展名）的串（如 マンション_主人公部屋a）；**大小写坑**：scn 引用小写（部屋a）而 bgimage 文件是大写（部屋A.png），必须建小写键集合匹配后映射回实际文件名；只收 PNG（base.stage 是配置非图）；章节内为"用过哪些背景"全集，非精确到播放时刻
- **按 BGM 配背景（剪辑反向组织）**：BGM → 章节（反推章节到BGM CSV）→ 章节背景合并去重 → 每首 BGM 一个文件夹放背景 PNG + 出现章节 txt；角色专属曲（61-65 段）背景与角色线吻合（BGM62 含 ナツメの部屋）
- 完整提取配方（台词/BGM/voice/背景 脚本逻辑与正则）：references/psb-script-extraction.md

### 阶段 5.5：画廊 CG 结构与角色专属曲（柚子社系，2026-08 实测）
- **cglist.csv = 画廊（CGモード）完整清单**：每行 `thum_evXXX, 变体名1, 变体名2, ...`，`:角色` 行分段（kan/nat/noz/mei/suz/etc/sd），thum 缩略图在 data/thum/（127 张 JPG：thum_evXXX + thum_SDXXX + thum_ED_xxx）
- **变体名不是文件名**，是合成表达式 `EV111DF|*ev111_aa`（基础CG | *差分图层名）——画廊完整 CG = 基础图 + 差分图层叠加，121 事件 × 差分 ≈ 3843 变体全靠合成；evimage_png 的拆层 PNG 是图层碎片不是合成图
- **SD 图（小剧场插画）在 evimage/sd/ 子目录**（344 张成品 PNG + 3 .asd 配置，直接可用）——整理时 os.listdir 只列一层会漏掉子目录，必须递归！画廊 29 个 SD 事件（sd001-501 按角色线分段）
- **角色专属 BGM 成对编号**（Cafe Stella 全验证）：BGM51-55 = 歌曲 InstVer（伴奏），BGM61-65 = QuietVer（抒情），只在对应角色线出现（BGM→章节统计验证）：61/51=Only you!(栞那)、62/52=Sweetest Betterness(ナツメ)、63/53=心地いい日常(希)、64/54=Happy Sunshine(愛衣)、65/55=Cold&Sweet(涼音)
- **主题曲视频**：代表图 + Ken Burns 推拉比静态轮播更合适——zoompan `z='min(zoom+step,1.2)':d=1:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':s=WxH:fps=30`，step = 0.2/(时长秒×30) 全片平滑放大；输入 `-loop 1 -i 图` + `-t 时长`，1788x1080/30fps/h264+aac

### 阶段 5.6：R18 内容识别与提取（柚子社 Gal 系，2026-08 实测）
不要靠猜/视觉识别，用游戏自己的清单做金标准：
- **scenelist.csv**（回想モード一覧）：`thum_EVxxx,,replay*角色H场景,@replay_...` 行 = 该角色 H 回想，`thum_EV111|thum_EV112` 列出 H 事件图 ev 编号段（Cafe Stella：栞那 EV111-120、ナツメ EV211-220、希 EV311-320、愛衣 EV411-420、涼音 EV503-509；同角色 SFW 段是低位编号 101-110 等——低位 SFW / 高位 H 是柚子社惯例）
- **data/scenario/replay.ks**（重放脚本，UTF-16LE）：`*kanH1|` + `[シーン回想開始 storage="108.栞那08（同棲）ver1.00.ks" target=*chapter_kanH1]` = H 回想 → 剧情章节精确映射
- 由章节号推 H 语音场景号（voice 命名 `角色缩写+场景号_序号`），按场景号集合提取 ogg 即得 H 语音，按角色分目录；输出全部硬链接（源在 full/，删 R18 目录不影响源）
- Cafe Stella 完整映射表（25 个 H 回想 → ev 编号/章节/场景号/角色）：references/cafe-stella-r18-mapping.md
- 不做的事：SD 图（Q 版）默认不按 R18 处理；立绘 H 差分命名无标记，但**可以用肤色占比分析识别**（见下"H 立绘识别"），不必纯靠猜
- **H 立绘识别（肤色启发式，实测 67 张候选）**：裸体/大面积暴露立绘的肤色像素占比显著高（>60% 高置信，45-60% 疑似泳装/浅色衣装）。方法：HSV/通道启发式皮肤检测（R>90,G>35,B>15,R>G≥B,R-B>20,max-min>20），只统计**完整立绘尺寸**（高度≥1400）排除腕部差分（100% 皮肤但尺寸小的碎片）误报；标准版与大图成对出现（同编号 _0_ 与无 _0_）。需人工抽查确认（浅色衣服可能误判）。配方：references/standee-skin-analysis.md
- **批量转换后必须做编号全集对比**：cglist/scenelist 引用的编号全集 vs 实际产物集合，缺号立刻暴露（Cafe Stella 因 pimg 转图层不完整，12 个 H ev 编号的拆层 PNG 缺失直到 R18 提取才查出；恢复路径：游戏源 xp3 + 归档工具重解包——完整配方 references/cafe-stella-ev-restore.md：定位 xp3 → GARbro 列内容确认 → 全量提取 → 拆 pimg→tlg → arc_unpacker 转 png → 归位 + R18 硬链接，实测 14 编号 695 png 0 失败）
- **用户偏好（整理为二创素材时）**：素材目录只留可视化资源（PNG/opus/ogg/视频/台词），工程内容（脚本/工具/密钥/配置表/进度文档/中间格式）移归档目录——移动不删除，素材目录保持纯净；不要过度构建分类库（用户可能自己删掉 assets 分类只留源数据——硬链接镜像删除安全，源不丢）
- **用户偏好（R18 与 SFW 分离）**：R18 立绘/CG/语音单独放 R18 目录，`full/` 素材目录保持 SFW——用户会主动删除 full 里的 H 立绘（**H 立绘全部是 R18 内容，任何情况都不要"还原"回素材目录**；用户删除的 H 立绘全是不带五官的 = 合成素材/差分层）；像素五官检测不可靠，不要用算法自行判断"带五官可留"放回 full，先问用户

## 实战教训（Cafe Stella 案例：失败复盘 references/cafe-stella-case.md；成功路径 references/cafe-stella-key-success.md）
1. **先查工具数据库收录情况再动手**：GARbro 的 KnownSchemes 没有的游戏，别浪费时间盲试
2. **密钥是否公开 = 成败分水岭**：动手前先搜社区（贴吧/B站/汉化组），商业 Gal 密钥基本都在讨论帖。GitHub 搜 fork 的 release（GARbro 官方没收录的，crskycode/nanami5270 等 fork mod 的 Formats.dat 里往往有）
   - **GARbro GitHub issues + fork/mod 家族是最高效渠道**：morkt/GARbro 的 game request issue 评论区常有人给出"保护方案相同、仅密钥不同"的线索；crskycode/GARbro（GARbro-Mod）等 fork 常已收录新游戏密钥
   - **密钥在 fork 的 Formats.dat 里，不在源码**——YuzCrypt.cs 等源码 grep 游戏名必空；需下载 fork 的 release zip，用反射工具 dump KnownSchemes（模板见 scripts/dump_garbro_keys.cs）
   - **完整 dump 配方见 references/yuzu-key-dump-full.md**（2026-08 实测）：反射模板两处必改——① ControlBlock 默认 `Take(40)` 截断，密钥文件要完整 1024 个值；② isYuzu 过滤关键词要补 `Dracu`/`Limelight`，否则 Dracu-Riot! 和 Limelight Lemonade Jam（"柠檬工厂"，柚子社 2025 新作）不显示。GARbro-Mod 的 Formats.dat 已收录：Cafe Stella / Riddle Joker / Dracu-Riot! / Senren★Banka / Limelight Lemonade Jam / Sanoba Witch。生成密钥 txt 的解析坑：CRLF 的 `\r` 挡 re.split 的 `$`（先 replace('\r\n','\n')）；Senren★Banka 的 ★ 在 ISO-8859-1 解码下变乱码（按类型 SenrenCxCrypt 匹配而非标题）；csc 编译 `C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /r:GameRes.dll /r:ArcFormats.dll`（.config 不是程序集，别引用）
   - **KrkrExtract 支持某游戏 ≠ 密钥公开**：它靠运行时注入让游戏自己解密，dll 内无静态密钥表
   - 完整检索路径见 references/yuzu-community-key-hunting.md
4. **工具选新不选旧**：KrkrExtract（2021，专为 krkrz）比 GARbro（2019 停更）更可能内置新游戏密钥
5. **静态识别三件套**：魔数 → 熵（~7.99 ≈ 加密）→ 字节周期（0x01 周期 = 流加密）
6. **内存 dump 是最后手段**，且密钥可能不在明文内存（混淆/仅存句柄）
7. **动态拦截优先于逆向**：工具全失败时先试 KrkrDump 运行时拦截（绕开一切加密），IDA 逆向找密钥是最后手段（见 ida-mcp-setup 技能）
8. **TLG5/6 → PNG**：arc_unpacker 的 kirikiri/tlg 解码器批量转换零失败（把 tlg 喂给 arc_unpacker，输出 cwd 下同名 .png，再手动移动）
9. **脚本 .ks/.tjs 是 UTF-16LE**：用 python decode('utf-16-le') 读，xxd 能看到 fffe BOM
10. **执行准则（用户偏好，实测被纠正两次）**：用户指定操作范围时（"只修改我提出的几批"），**严格按范围执行，不擅自扩大**——即使发现"所有 42 个视频都有同样问题"，也先完成指定范围 + 说明情况，让用户决定是否扩大。批量删除/重做前先确认影响范围：曾因 rm 全部旧视频 + 中途被杀留下残缺文件（1-3MB 的不完整 mp4，正常 10-20MB），需要事后识别清理

## 工具清单速查
- 认引擎：Detect It Easy + ExifTool
- 解包：GARbro/KrkrExtract（Gal）、AssetStudio/UnityPy（Unity）、FModel（UE）
- 转格式：texconv（贴图）、vgmstream（音频）、wwiser（Wwise）
- 拆 shader：RenderDoc 抓帧 + spirv-cross 反编译（TA 最值钱的一环）

本机工具完整盘点（含 krkr 专用工具合集、GAL-TOOLS 密码、archive 归档）：references/toolkit-inventory.md

## 本机坑（Windows 实测）
1. **Windows 原生工具不吃 MSYS /d/ 路径**：UnRAR、ov CLI 等要传 `D:\...` Windows 路径；bash 里路径末尾反斜杠+引号会转义炸（`\"`），去掉末尾反斜杠
2. **rar 套娃解压**：用 `"/c/Program Files/WinRAR/UnRAR.exe" x -p"密码" "D:\路径.rar" "D:\目标"`
3. **官网被墙的 Windows 工具用 winget**（独立通道）：如 RenderDoc `winget install --id BaldurKarlsson.RenderDoc -e`
4. **内存 dump 大文件**（500MB+）归档不删（archive\），供后续分析
5. **Defender 拦逆向工具**（WinError 225 "包含病毒或潜在的垃圾软件"）：下载的 exe 带 MOTW 被实时保护直接删除，Unblock-File 无效（按内容判定）；绕过：用 rar/zip 解压出来的旧工具（无 MOTW 不被拦），或用户手动加 Defender 排除（Add-MpPreference 需管理员 UAC，agent 触发会卡在确认被拦）
6. **python subprocess 的 cwd 必须 Windows 路径**（D:\...），MSYS 路径（/d/...）报 NotADirectoryError
7. **arc_unpacker 0.11 的 -o 输出参数不生效**，输出落在 cwd，转换后手动移动
8. **游戏窗口控制**：computer_use 不可用时用 Python ctypes keybd_event 模拟按键（Space/Enter 交替推进对话）；WScript.Shell AppActivate 接受 PID 不接受窗口 handle；krkrz 按 A 键开自动模式，剧情自动推进、资源持续加载，比手动按键高效得多
9. **IDA 安装位置可能被移动**（2026-08 实测：C:\Program Files → D:\逆向\IDA Professional 9.3）：idalib 报 "Cannot load IDA library file" 时先 `os.path.isdir()` 验证实际位置，再用 python 更新 %APPDATA%\Hex-Rays\IDA Pro\ida-config.json 的 Paths.ida-install-dir；ida-mcp-setup 技能里的 C 盘路径已过时
10. **idapro 裸脚本（非 idalib-mcp）**：`uv run --with idapro python script.py` 需要 IDADIR 环境变量指向 IDA 目录；uv run 管道下 print 可能被吞（exit 255 / 空输出），分析结果写文件更可靠；ida-config.json 必须指向有效位置否则 import idapro 直接 ImportError
11. **Python f-string 表达式不能含反斜杠转义**（<3.12）：`f"{os.path.join(r'E:\\BUG', f)}"` 直接 SyntaxError（本会话踩 3 次）——路径先存变量（`p = os.path.join(...)`）再放进 f-string，或避免 f-string 内嵌含反斜杠的表达式

完整工作流文档存档：`C:\Users\Furina\Documents\ChatGPT\RAG\outputs\game-asset-extraction-workflow.md`
