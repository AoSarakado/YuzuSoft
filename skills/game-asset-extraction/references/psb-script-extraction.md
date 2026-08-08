# krkrz PSB 编译脚本字符串提取配方（Cafe Stella 实测，2026-08）

从 .ks.scn（PSB v3）提取台词台本 / BGM 引用 / voice 引用 / 背景引用的完整方法。
实测对象：柚子社《星光咖啡馆与死神之蝶》（Cafe Stella），74 个 scn，产出 55753 条台词。

## 核心事实

1. **.scn 是 PSB v3 容器**：魔数 `50 53 42 00`（"PSB\0"）+ version 0x03000000。不是明文脚本。
2. **字符串表在文件后段，\x00 分隔的 UTF-8 明文**（不是 UTF-16LE！用 UTF-16LE 读全是乱码）。
3. **字符串表按 UTF-8 字节序排序**（あ→い→う 字典序），不是演出顺序。逐句顺序/角色归属需要解析 PSB 指令区（紧凑二进制，工作量大的进阶工作）。

## 提取器（Python）

```python
import re

def extract_strings(data):
    """PSB 字符串表：\x00 分隔 UTF-8。保留含日文或纯 ASCII 的串。"""
    texts = []
    for c in data.split(b'\x00'):
        if not c:
            continue
        try:
            s = c.decode('utf-8')
        except UnicodeDecodeError:
            continue
        has_jp = re.search(r'[ぁ-んァ-ン一-龯]', s)
        is_ascii = all(0x20 <= ord(ch) < 0x7F for ch in s)
        if has_jp or (is_ascii and len(s) >= 3):
            printable = sum(1 for ch in s if ch.isprintable())
            if printable / len(s) > 0.8:
                texts.append(s)
    return texts
```

注意：纯 ASCII 过滤条件必须保留（bgm01、kan108_001 这些纯 ASCII 引用不含日文）。

## 四类提取

| 类型 | 特征 | 正则/方法 |
|---|---|---|
| 台词 | 含「」的串；旁白/选项/演出注释（→歩く／床、▽ドア閉める、★ぶつかる５）也在表里 | 含「」或全角引号 |
| BGM | **小写** `bgm01` / `bgm_BGM03`（大写 BGM 搜不到） | `(?<![A-Za-z])bgm(\d{1,2})` + `bgm_?(BGM\d{1,2})` |
| voice | `角色缩写+场景号4位_序号`（kan108_001） | `^(kan\|nat\|noz\|mei\|suz\|mik\|hir\|kaz\|rok\|miy\|yui)\d{3}_\d+` |
| 背景 | 命中 bgimage 文件名（去扩展名）的串，如 マンション_主人公部屋a | 建小写键集合匹配（见下） |

## 背景引用的坑（本次翻车点）

- scn 里引用**小写**：`マンション_主人公部屋a`；bgimage 文件是**大写**：`マンション_主人公部屋A.png`
- 直接 `s in bg_set` 匹配只有 315 张；忽略大小写后 2220 张（差距巨大）
- 正确做法：
```python
bg_names = {}
for f in os.listdir(BG_DIR):
    if not f.lower().endswith('.png'):
        continue  # base.stage 是配置非图
    bg_names[os.path.splitext(f)[0].lower()] = f
# 命中：
for s in texts:
    key = s.lower()
    if key in bg_names:
        hits.append(bg_names[key])  # 映射回实际文件名
```
- 产出粒度：章节内"用过哪些背景"全集，非精确到播放时刻（字符串表排序限制）

## 章节 → 角色线（Cafe Stella 文件名约定）

- 001-014 共通线；100 栞那；201 ナツメ；300 希（内部再按 ルート001-012）；401 愛衣；501 涼音
- 章节排序键：`(前缀数字, ルート子编号)`，如 `(300, 6)` 在 `(401, 0)` 前

## 产出（assets 组织）

- 台词台本：每章一个 txt（章节名即文件名），文件头标 BGM/台词数/voice 数
- BGM-章节对照：双向 CSV（章节→BGM 带引号含逗号字段，必须 csv.reader 解析；BGM→首次出现按剧情顺序）
- voice 引用清单：每章一个 txt
- 剪辑素材：
  - 按场景：每章文件夹放该章 BGM（硬链接带曲名版本）+ 台本
  - 按 BGM：每首 BGM 文件夹放其出现章节的背景 PNG（合并去重）+ 出现章节说明 txt

## 柚子社 BGM 编号段位（跳号是分类预留，非缺文件）

- BGM01-36：场景曲
- BGM51-55：主题歌 InstVer（伴奏版）
- BGM61-65：同批歌曲 QuietVer（抒情版，与 51-55 成组配对：52↔62、53↔63）
- BGM91：标题画面曲（不走剧情脚本，剧本字符串搜不到）
- SongOP/SongED1-5：主题歌本体
- 角色线专属曲在 61-65 段（Cafe Stella：61 栞那 / 62 ナツメ / 63 希 / 64 愛衣 / 65 涼音）

## 其他坑

- pip 的 `psb` 包是树莓派 GPIO 库（依赖 gpiod/RPi.GPIO），不是 krkrz PSB 解析器，别装
- 事件图 pimg 拆层计数注意 os.walk 统计生成器坑（见 SKILL.md 阶段 5.2）
