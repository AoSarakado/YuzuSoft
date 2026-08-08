---
name: ida-mcp-setup
description: "Use when configuring IDA Pro MCP for Hermes."
version: 1.0.0
author: Natsume
license: MIT
platforms: [windows]
metadata:
  hermes:
    tags: [ida, mcp, reverse-engineering, idalib, hermes-config]
---

# IDA Pro MCP 接入 Hermes

让 Hermes 通过 MCP 直接指挥 IDA Pro（vibe reversing）：`mcp_ida_*` 工具
（decompile / disasm / xrefs_to / data_read_* / idb_open / idb_list 等 47 个）。

## 触发条件
- 配置/修复 IDA Pro MCP（换机、重装、Hermes 重装后）
- 要开始一次 IDA 逆向分析会话（如找游戏加密密钥）
- 排查 mcp_ida_* 工具不可用

## 前提
- IDA Professional 已装（本机：C:\Program Files\IDA Professional 9.3）
- uv 已装；Hermes venv 里有 mcp 包（hermes-agent/venv/Scripts/python.exe -c "import mcp"）
- 注意 IDA 9.x 起 ida64.exe 合并进 ida.exe，目录里没有 ida64.exe 是正常的

## 架构（为什么有桥接层）
```
Hermes (stdio MCP client)
   │  stdin/stdout
   ▼
ida_mcp_bridge.py  ← SSE→stdio 转发（D:\逆向\tools\ida_mcp_bridge.py）
   │  http://127.0.0.1:8745/sse (SSE, GET /sse 200, POST 405)
   ▼
idalib-mcp  ← uv run --with "mcp<2" --with idapro --with ida-pro-mcp idalib-mcp <目标>
   │  通过 idapro 包调用 IDA idalib（headless，无需打开 IDA GUI）
   ▼
IDA idalib（分析 .exe/.dll 等，产出 .i64 数据库）
```
关键：idalib-mcp 只提供 SSE 传输，而 Hermes 原生 MCP 客户端只支持
StreamableHTTP 和 stdio——所以必须桥接。直连会 405 Method Not Allowed。

## 安装步骤（换机/重装时按序执行）

### 1. 激活 idalib
```bash
uv run "C:\Program Files\IDA Professional 9.3\idalib\python\py-activate-idalib.py"
```
产出：C:\Users\<user>\AppData\Roaming\Hex-Rays\IDA Pro\ida-config.json
（记录 ida-install-dir，idapro 包靠它找 IDA）

### 2. 验证后端能启动（先手工试一次）
```bash
mkdir -p /c/Users/<user>/tmp_idatest && cp "/c/Windows/System32/notepad.exe" /c/Users/<user>/tmp_idatest/
cd /c/Users/<user> && uv run --with "mcp<2" --with idapro --with ida-pro-mcp idalib-mcp "C:\Users\<user>\tmp_idatest\notepad.exe"
```
看到 `MCP Server available at: http://127.0.0.1:8745/sse` 即成功。
注意：目标文件必须在可写目录（idalib 在目标同目录写 .i64，System32 会
Permission denied）。

### 3. 写桥接脚本（SSE→stdio）
脚本内容见本技能 scripts/ida_mcp_bridge.py（已部署到 D:\逆向\tools\ida_mcp_bridge.py）。
核心：mcp.client.sse.sse_client 连 8745 → ClientSession → 枚举 tools →
本地 mcp.server.Server(list_tools/call_tool) 转发 → stdio_server.run()。

### 4. 注册进 Hermes
```bash
printf 'y\n' | hermes mcp add ida --connect-timeout 120 --command uv \
  --args run --with "mcp<2" "D:\逆向\tools\ida_mcp_bridge.py"
```
- 交互提示（auth Y/n、Enable all N tools Y/n/select）必须管道喂输入，否则卡住/取消
- `--args` 必须是 add 的最后一个选项
- 输出 "Saved 'ida' ... (47/47 tools enabled)" 即成功

### 5. 验证
```bash
hermes mcp test ida   # 列出工具即通过
```
MCP 工具只在新会话注入——当前会话看不到 mcp_ida_* 是正常的，重启 hermes。

## 日常使用流程
1. 启动后端（窗口保持运行）：
   `D:\逆向\tools\start-ida-mcp.cmd <目标文件> [--unsafe]`
   --unsafe = 启用写操作（改名/注释/补丁），默认只读
2. 启动 Hermes 新会话 → mcp_ida_* 可用
3. 换分析目标：关掉后端窗口重跑（8745 端口固定，不能同时跑两个）
4. 不预加载文件也行（GitHub main 支持 idb_open 动态加载；但 PyPI 2.0.0 的
   input_path 参数必填，所以启动器总是带一个目标）

## 坑（全部实测）
1. **SSE-only**：idalib-mcp 无 /mcp 端点，POST /sse 405。必须桥接，不能 url 直连
2. **mcp<2 必须锁版本**：mcp 2.x 移除了 mcp.server.fastmcp，ida-pro-mcp import 会炸
3. **uvx 不能重复 --from**：`uvx --from ida-pro-mcp --from mcp` 报错，用
   `uv run --with A --with B`（支持多个 --with）
4. **--with ida-pro-mcp 不会自动装 idapro**（依赖解析有坑），必须显式 --with idapro
5. **.i64 写权限**：目标必须在可写目录，否则 "Database initialization failed"
6. **hermes mcp add 交互**：两个 Y/n 提示都要管道喂输入
7. **PyPI 2.0.0 与 GitHub main 有差异**：PyPI 版 input_path 必填、动态 idb_open
   不可用；GitHub main 版 input_path 可选（nargs='?'）、支持 idb_open 动态加载。
   要动态加载可用 git+https 安装，但走代理且复杂，日常用启动器带目标即可

## 验证清单
- [ ] ida-config.json 存在且指向 IDA 安装目录
- [ ] idalib-mcp 单独启动输出 "MCP Server available at: http://127.0.0.1:8745/sse"
- [ ] hermes mcp list 里有 ida
- [ ] hermes mcp test ida 能列出工具
- [ ] 新会话里出现 mcp_ida_* 工具
