---
name: ida-reversing
description: "Use when 逆向分析二进制/找加密密钥/用 IDA MCP。idalib-mcp 接入 Hermes 配置与用法。"
version: 1.0.0
author: 四季夏目
license: MIT
platforms: [windows]
metadata:
  hermes:
    tags: [ida, reverse, mcp, binary, decompile, key]
---

# IDA Pro 逆向工具链（含 Hermes MCP 接入）

IDA Professional 9.3 已装于 `C:\Program Files\IDA Professional 9.3`。idalib-mcp 是 IDA 的 headless MCP 服务器，已通过 SSE→stdio 桥接接入 Hermes（`mcp_servers.ida`，工具名 `mcp_ida_*`）。

## 架构（为什么有桥接）
- idalib-mcp 是 **SSE-only** 服务器（`http://127.0.0.1:8745/sse`），起 uvicorn HTTP 服务
- Hermes 原生 MCP 客户端只支持 StreamableHTTP（POST /mcp）和 stdio，直连 SSE 报 405
- 桥接脚本把 SSE 转发为 stdio 注册进 Hermes：`D:\逆向\tools\ida_mcp_bridge.py`（副本见 scripts/ida_mcp_bridge.py）

## 使用流程
1. 启动后端：`D:\逆向\tools\start-ida-mcp.cmd <目标文件>`（可选 `--unsafe` 开写操作：改名/注释/补丁），窗口保持开着
   - 目标文件必须在可写目录——idalib 会在文件同目录写 `.i64` 数据库，System32 等无写权限目录直接失败（`Permission denied`）
2. 启动/重启 Hermes → `mcp_ida_*` 工具自动可用（decompile、disasm、xrefs_to、data_read_*、idb_open、list_funcs 等，共 47 个）
3. 换分析目标：关掉窗口重跑，传新文件；或用 `mcp_ida_idb_open` 动态加载（GitHub main 分支支持无参启动，PyPI 2.0.0 的 input_path 为必填）

## 关键坑
- **mcp 2.x 移除了 fastmcp 模块**，ida-pro-mcp 依赖 `mcp.server.fastmcp` → 必须锁 `mcp<2`
- ida-pro-mcp 的 dependencies 只声明 idapro+tomli-w，**mcp 是 dev 依赖**，需显式 `--with mcp<2` 补装
- uvx 不支持重复 `--from`，用 `uv run --with A --with B <entry>` 形式
- idapro 包从 PyPI 装（0.0.10），激活脚本 `py-activate-idalib.py` 写 `ida-config.json` 到 `%APPDATA%\Hex-Rays\IDA Pro\`，idalib 全局激活只需跑一次
- `hermes mcp add` 有交互确认（"Enable all N tools?"），管道喂 `y\n` 即可
- IDA 9.x 起 ida64.exe 合并进 ida.exe，目录里没有 ida64.exe 是正常的

## 完整配置细节与排障
见 references/ida-mcp-setup.md

## 与解包工作流的衔接
工具解包全失败（如柚子社 Cafe Stella 密钥未公开）时，把游戏主程序拖进 IDA，顺加密段解密逻辑找密钥流生成函数——比内存 dump 碰运气靠谱（见 game-asset-extraction 技能）。
