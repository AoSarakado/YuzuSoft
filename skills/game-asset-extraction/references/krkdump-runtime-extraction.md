# KrkrDump 运行时拦截解包（完整配方）

2026-08 实测：柚子社《星光咖啡馆与死神之蝶》（krkrz，yuzuex 加密）一次成功。
静态工具（GARbro / arc_unpacker 0.11 / KrkrExtract 5.0.0.2 / KirikiriDescrambler）
全部失败后，KrkrDump 直接导出 224+ 个解密文件，含真实文件名。

## 原理

KrkrDump.dll 被注入游戏进程后挂钩文件系统 API：游戏运行时自己解密 xp3，
KrkrDump 拦截解密后的数据流并落盘。**不需要破解密钥，不碰加密本身。**
产出的是游戏运行时实际读取过的资源（+真实文件名，MD5 问题不存在）。

## 工具位置

krkr 解包封包工具合集里（本机 D:\逆向\krkr解包封包工具合集\...\7.krkrdump\）：
- KrkrDump.dll —— 注入用的 dll
- KrkrDumpLoader.exe —— 加载器（启动游戏 + 注入）
- KrkrDump.json —— 配置

注意：这套工具是 rar 解压的，无 MOTW，Defender 不拦。
KrkrDumpLoader.exe 无参运行输出 "Usage: KrkrDumpLoader <path>"。

## 配置（KrkrDump.json）

```json
{
    "loglevel": 2,
    "enableExtract": true,
    "outputDirectory": "D:\\逆向\\out\\cafe\\dump",
    "rules": [
        "file://\\/.+?\\.xp3>(.+?\\..+$)",
        "archive://./(.+)",
        "arc://./(.+)",
        "bres://./(.+)"
    ],
    "includeExtensions": [],
    "excludeExtensions": [".ogg"],
    "decryptSimpleCrypt": true
}
```
必改项：outputDirectory（默认是别人的路径 E:\games\game\dump）。
excludeExtensions 里有 .ogg 时语音 ogg 不 dump（会 dump .ogg.sli 索引）。

## 启动

```python
# python subprocess（MSYS bash 直接跑 exe 可能有权限问题，用 subprocess 最稳）
subprocess.run([r'D:\逆向\tools\krkrdump\KrkrDumpLoader.exe',
                r'E:\galgame\游戏目录\Game.exe'],
               cwd=r'D:\逆向\tools\krkrdump', timeout=120)
```
- cwd 必须在 KrkrDump 三件套所在目录（json 和 dll 要能被 loader 找到）
- loader 会启动游戏并注入 dll；loader 进程退出不影响已注入的 dll
- 游戏窗口出现、内存上涨（几百 MB）说明注入成功

## 验证与收集

- dump 目录持续增长即生效。游戏要**实际加载资源**才 dump：
  - 标题画面：脚本（.ks/.tjs）、标题 BGM（.opus）、片头视频（.amv）
  - 进剧情：角色立绘差分（.tlg + .sinfo）、语音索引（.ogg.sli）、场景 BGM
  - 背景图要切换场景才加载；全 CG 靠画廊/回想模式
- krkrz 自动模式：向游戏窗口发 A 键开启自动播放，剧情自动推进、资源自动加载

## 全量触发：文件损坏检查工具（实测有效）

游戏自带的完整性校验工具会**遍历所有 xp3**（解密+校验），把 KrkrDump 注入它，
一次检查 = 全量 dump，比推剧情逐张加载快一个数量级：

```python
subprocess.run([r'D:\逆向\tools\krkrdump\KrkrDumpLoader.exe',
                r'E:\galgame\游戏目录\ファイル破損チェックツール.exe'],   # 检查工具 exe
               cwd=r'D:\逆向\tools\krkrdump', timeout=600)
```

- 检查工具窗口可能最小化到屏幕外：GetWindowRect 返回 -32000 时
  ShowWindow(hwnd, 9)（SW_RESTORE）+ SetForegroundWindow 拉回
- 部分检查工具要点击"开始检查"才遍历：ctypes mouse_event 点窗口下部
  （按钮区），或键盘 Tab/Enter
- 若窗口按钮位置盲点不中，优先改用 IDA 逆向拿密钥（静态全量解包），
  别在 GUI 自动化上耗太久

## 产物特征

- 文件名是真名（如 希a_949.tlg、蝶1.amv、custom.ks），UTF-16 脚本带 fffe BOM
- 立绘 .tlg（TLG5.0）→ arc_unpacker 转 PNG（见 SKILL.md 阶段 5）
- BGM .opus 直接可播；.sli 是同名音频索引
- 部分资源在明文目录（voice\ 6153 个 MD5 文件）不走 xp3，游戏直接读

## 局限

- 只导出游戏运行中实际读取的资源，不读的包/文件不出现
- 需要游戏能跑起来（正版安装目录、能过启动校验）
