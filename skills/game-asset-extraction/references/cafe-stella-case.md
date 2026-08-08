# Cafe Stella 解包实战复盘（2026-08-07）

柚子社《星光咖啡馆与死神之蝶》（Kirikiri Z 引擎）解包尝试记录。结果：未解开——加密方案密钥未公开，GARbro 数据库未收录。

## 已确认的技术事实
| 项目 | 结论 |
|---|---|
| 引擎 | 吉里吉里Z（krkrz） |
| 封包头 | 标准 YP3 魔数 |
| index 区 | zlib 压缩，解压后为柚子社魔改 yuz:/File 表 |
| 文件名 | MD5 哈希，真名映射表在额外加密的 yuz: 段 |
| 数据段 | 流加密（0x01 单字节周期特征），非普通压缩 |

## 尝试过的路径（全失败）
1. GARbro 2017 版 → 不支持
2. GARbro 2019 v1.5.44（最新 release）→ 不支持；KnownSchemes 数据库最新只到 Riddle Joker（同引擎但密钥不同）
3. 用 Riddle Joker 的 YuzKey 试 yuz 段 → 密钥流前几字节不符
4. 遍历 Formats.dat 全部 360 个方案密钥 → 无一能解
5. 内存 dump（525MB）搜 YuzKey/ControlBlock → 明文里只有 startup.tjs、yuz: 等字符串，密钥未留在可扫描内存

## 本机留下的工具（D:\逆向\）
- GARbrosdasdwas/          —— 原 GARbro
- tools/GARbro-new/        —— 2019 最新 release
- tools/KrkrExtract/       —— KrkrExtract 5.0.0.2 Lite（已下载，2021 版，当时未试）
- tools/dump_mem.py        —— 内存 dump 脚本（改 PID 即用）

## 下一步方向（未走完的节点）
1. KrkrExtract 5.0.0.2——比 GARbro 新，专为 krkrz 写，可能内置这游戏密钥
2. 社区教程/密钥：贴吧/B站/汉化组发布帖
3. IDA 逆向游戏主程序：顺 yuz: 段解密逻辑找密钥流生成函数（比内存 dump 碰运气靠谱）
4. RenderDoc 抓帧兜底（拿运行时最终资源）

## 关键教训
- 先查工具数据库（GARbro KnownSchemes）再动手
- 密钥是否公开 = 成败分水岭
- 工具选新不选旧
- 静态识别三件套：魔数 / 熵（7.99≈加密）/ 字节周期（流加密）
- 内存 dump 是最后手段且不一定有效

## 版权边界
游戏本身公开、用户有正版安装目录，拆包研究自持资源没问题；拆出来的素材不公开发布（尤其涉及汉化补丁时）。
