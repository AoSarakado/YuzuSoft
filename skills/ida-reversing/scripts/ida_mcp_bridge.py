"""
SSE -> stdio 桥接脚本：让 Hermes 的 stdio MCP 客户端能连上 idalib-mcp 的 SSE 服务。

背景：
- idalib-mcp (ida-pro-mcp 2.x) 只提供 SSE 传输（http://127.0.0.1:8745/sse）
- Hermes 原生 MCP 客户端只支持 StreamableHTTP（POST /mcp）和 stdio
- 本脚本作为 stdio MCP server 注册进 Hermes，内部转发到 idalib-mcp 的 SSE 端点

用法（配合后台运行的 idalib-mcp）：
  uv run --with "mcp<2" python D:\\逆向\\tools\\ida_mcp_bridge.py

依赖：mcp>=1.0,<2（fastmcp/sse_client 结构在 1.x）
"""
import asyncio
import json
import logging

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger("ida-bridge")

SSE_URL = "http://127.0.0.1:8745/sse"


async def main() -> None:
    from mcp import ClientSession
    from mcp.client.sse import sse_client
    from mcp.server import Server
    from mcp.server.stdio import stdio_server
    from mcp.types import TextContent

    log.info("connecting to idalib-mcp SSE: %s", SSE_URL)
    async with sse_client(SSE_URL) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            tools = (await session.list_tools()).tools
            log.info("proxying %d tools from idalib-mcp", len(tools))

            server = Server("ida-bridge")

            @server.list_tools()
            async def list_tools():
                return tools

            @server.call_tool()
            async def call_tool(name: str, arguments: dict):
                result = await session.call_tool(name, arguments=arguments or {})
                texts = []
                for content in result.content:
                    if hasattr(content, "text") and content.text:
                        texts.append(TextContent(type="text", text=content.text))
                    else:
                        try:
                            texts.append(
                                TextContent(
                                    type="text",
                                    text=json.dumps(
                                        content.model_dump(),
                                        ensure_ascii=False,
                                        default=str,
                                    ),
                                )
                            )
                        except Exception:
                            texts.append(TextContent(type="text", text=str(content)))
                return texts

            async with stdio_server() as (r, w):
                log.info("stdio bridge ready")
                await server.run(r, w, server.create_initialization_options())


if __name__ == "__main__":
    asyncio.run(main())
