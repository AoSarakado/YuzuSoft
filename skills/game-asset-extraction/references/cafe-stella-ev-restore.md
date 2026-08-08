# 缺失事件图从游戏源 xp3 恢复（Cafe Stella 实测，2026-08）

## 触发场景
批量转换后做编号全集对比（cglist/scenelist 引用编号 vs 实际产物）发现缺号，
如 evimage_png 缺 ev416-420、ev501-509（当年 pimg→图层转换不完整，不是删除造成）。

## 完整恢复流程（14 编号 / 17 pimg / 695 png / 0 失败 / 795s 实测）

### 1. 定位游戏源 xp3
游戏安装目录（E:\galgame\<游戏名>\）下按资源类型分 xp3：
`evimage.xp3`（事件图）、`fgimage.xp3`（立绘）、`bgimage.xp3`、`voice.xp3`、`scn.xp3`、`bgm.xp3` 等。
每个 xp3 旁边有 .sig（验签文件，可忽略）。

### 2. 确认工具链
- GARbro.Console.exe（含密钥的 mod 版，归档 garbro-mod/）
- arc_unpacker 0.11（tlg→png）
- Windows 路径传参（不吃 MSYS /d/ 路径）

### 3. 列出 xp3 内容确认识别
```
GARbro.Console.exe "E:\galgame\...\evimage.xp3" | grep -iE "ev41[6-9]|ev420|ev50[1-9]"
```
能列出真实文件名 = scheme 匹配成功。注意**同一 ev 编号可能有多个 pimg**（差分/变体）：
ev416a.pimg、ev416mm.pimg、ev416_a.pimg、ev416_mm.pimg 都是 ev416。

### 4. 全量提取（GARbro -x 输出到 cwd）
```
mkdir _restore && cd _restore
GARbro.Console.exe -x "E:\galgame\...\evimage.xp3"
```
GARbro 不支持按文件过滤提取，只能全量（evimage 1895MB 几分钟，后台跑）。
提取物含 sd/ 子目录等全部内容，恢复完整体删除。

### 5. 拆 pimg → tlg（每个 pimg 一个工作子目录）
```
mkdir _ev416a && cd _ev416a
GARbro.Console.exe -x "..\ev416a.pimg"    # 输出到 cwd
```
子目录里出现 .tlg 文件。

### 6. tlg → png（arc_unpacker，输出 cwd 同名）
```
arc_unpacker.exe "路径\_ev416a\xxx.tlg"
```
零失败（与立绘 tlg 转换同）。

### 7. 归位 + R18 硬链接
- png 移入 `full/evimage_png/ev416a/`（目录名统一 `ev<编号>a`）
- R18 编号（H 段）再硬链接到 `R18_成人内容/01_CG事件图/ev<编号>a/`（os.link，删 R18 不影响 full）
- 顺手补 SFW 低位编号（如 ev501/502）保证完整性

### 8. 清理临时目录
```
cd <上一级> && rm -rf _restore
```
**坑：bash cwd 在 _restore 里时 rm -rf 报 "Device or resource busy"——先 cd 出去再删**（Python 句柄可能延迟释放，sleep 2 后可重试）。

## 恢复脚本要点（restore_ev.py 存归档）
- TARGET 编号列表驱动；每个编号匹配 `ev<编号>(a|_a|mm|_mm).pimg` 正则
- 已存在目录跳过（可重入）
- GARbro/arc_unpacker 均用 subprocess cwd 参数传工作目录
- 全部步骤子进程化 + 后台跑 + notify_on_complete

## 教训
- 转换批量任务完成后必须做编号全集对比，缺号立刻暴露（本会话 R18 提取时才查出，晚了）
- 源 xp3 别删——游戏安装目录是恢复的最终保障；归档工具链（GARbro+密钥）也保留
