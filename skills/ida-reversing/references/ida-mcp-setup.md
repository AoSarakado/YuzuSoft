# IDA Pro MCP 接入 Hermes 完整配置与排障

## 环境
- IDA Professional 9.3：`C:\Program Files\IDA Professional 9.3`
- idalib 全局激活（只跑一次）：
  ```bash
  cd "/c/Program Files/IDA Professional 9.3/idalib/python"
  uv run py-activate-idalib.py
  ```
  产出：`%APPDATA%\Hex-Rays\IDA Pro\ida-config.json`，内容为 `{"Paths": {"ida-install-dir": "C:\\Program Files\\IDA Professional 9.3"}}`

## 后端启动（idalib-mcp）
```bash
uv run --with "mcp<2" --with idapro --with ida-pro-mcp idalib-mcp "C:\path\to\target.exe"
```
- 启动成功标志：`MCP Server available at: http://127.0.0.1:8745/sse` + `Uvicorn running`
- 本机封装：`D:\逆向\tools\start-ida-mcp.cmd`（cd 到用户目录再 uv run，传 %* 透传参数）

## 桥接（SSE → stdio）
Hermes 原生 MCP 客户端只支持 StreamableHTTP 和 stdio；idalib-mcp 只有 SSE（GET /sse 200，POST /sse 405，/mcp 404）。
桥接脚本 `D:\逆向\tools\ida_mcp_bridge.py`（技能内副本 scripts/ida_mcp_bridge.py）：
- `mcp.client.sse.sse_client` 连 `http://127.0.0.1:8745/sse`
- `mcp.server.Server` + `stdio_server()` 起 stdio server
- list_tools 原样转发远端工具；call_tool 转发调用并序列化结果

## Hermes 注册
```bash
printf 'y\n' | hermes mcp add ida --connect-timeout 120 --command uv --args run --with "mcp<2" "D:\逆向\tools\ida_mcp_bridge.py"
```
- 交互提示 "Enable all 47 tools?" 需喂 `y\n`（管道或 TTY）
- 验证：`hermes mcp test ida`；配置在 `~/.hermes/config.yaml` 的 `mcp_servers.ida`
- MCP 工具在 Hermes 启动时发现，**添加后需重启会话**才出现 mcp_ida_* 工具

## 排障清单
| 症状 | 原因 | 解决 |
|---|---|---|
| `failed to analyze input file` + `Permission denied` | 目标文件目录不可写（.i64 数据库写不进去） | 把文件复制到可写目录（如用户目录）再分析 |
| `ModuleNotFoundError: mcp.server.fastmcp` | mcp 2.x 移除了 fastmcp | 锁 `mcp<2` |
| `ModuleNotFoundError: idapro` | ida-pro-mcp 依赖解析没带 idapro | 显式 `--with idapro` |
| `uvx: argument '--from' cannot be used multiple times` | uvx 不支持多个 --from | 改用 `uv run --with A --with B <entry>` |
| `405 Method Not Allowed` on /sse | 客户端用 POST 连 SSE-only server | 走桥接脚本（SSE 客户端），不要直连 |
| input_path required 报错 | PyPI 2.0.0 的 input_path 是必填 positional | 传一个存在的文件；GitHub main 分支支持无参启动+idb_open 动态加载 |
| `ida64.exe` 不存在 | IDA 9.x 起 64 位合并进 ida.exe | 正常现象，用 ida.exe |

## 工具名（mcp_ida_*，47 个）
list_funcs / list_globals / imports / lookup_funcs / decompile / disasm / xrefs_to / callees / get_string / data_read_dword / data_read_qword / data_read_string / idb_open / idb_list / int_convert / find_regex / func_query / entity_query / analyze_batch 等。--unsafe 开启后才有改名/注释/补丁类写操作。
