# 解包资产复用为 ML 数据集（学校项目实战，2026-08-08）

场景：用户做"多源数据清洗"学校小项目，要求 py 生成数据集 + ML 要求不高，想用解包后的真实游戏资源当数据集（替代合成数据）。

## 核心判断
- **解包过程天然产出"脏→净"对照**：输入（加密/乱码/哈希名）→ 输出（真实文件名/格式），就是带标签训练样本
- 但两个数据形态对应不同任务，别混用：
  - 逆向后（full/ 单文件）→ **文件类型分类**（主体，简单，准确率好出）
  - 逆向前（xp3 容器）→ 容器识别/字节流区块识别（加分项；14 个容器样本太少，需切块；密文块全高熵看不出内容类型）

## 特征设计（每行一个文件）
| 特征 | 类型 | 说明 |
|---|---|---|
| size | 数值 | 文件大小 |
| entropy | 数值 | 香农熵（前 4KB），衡量压缩/乱码 |
| printable_ratio | 数值 | 可打印字符占比，文本 vs 二进制 |
| highbyte_ratio | 数值 | 高字节占比，检测 UTF-16/编码 |
| magic | 类别 | 前 4 字节 hex（89504e47=PNG） |
| ext | 类别 | 原始扩展名（弱特征，可能误导） |

## 标签规则（客观优先）
- 魔数判定优先：PNG→`\x89PNG`、OGG→`OggS`、OPUS→`OpusHead`、PSB→`PSB\x00`、pbd→`TJS/4s0`
- 魔数判不出 → 扩展名兜底（ks/tjs/txt/csv/ini/scn→text_script）

## 采样与模型
- **每类限量 300**：否则 2.6 万语音 ogg 淹没其它类（实测抽样 2158 样本、10 类平衡）
- 模型：决策树/随机森林足够（sklearn），数值特征直接喂 + magic/ext one-hot
- 评估：7:3 分割 + 准确率 + 混淆矩阵（报告重点：哪两类易混）

## 环境坑
- 系统 Python 3.14 的 site-packages 权限冲突 → sklearn/pandas/numpy import 全炸（PermissionError）
- 修复：用 uv 建干净环境 `uv venv --python 3.12` + `uv pip install scikit-learn pandas`

## 报告叙事（加分）
"真实数据替代合成数据"作为亮点：解包产生多格式杂乱数据 → 特征工程 + ML 自动识别 → 清洗流水线（容器→类型）。版权注明"正版游戏解包，仅学习研究"，不公开上传。

## 产物位置
- 数据集构建器：D:\逆向\out\cafe\ml\build_dataset.py（stdlib，不依赖 sklearn）
- 行动指南：D:\逆向\out\cafe\ml\DATA-CLEANING-GUIDE.md
