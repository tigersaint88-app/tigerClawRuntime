# TigerClaw Runtime V1

TigerClaw Runtime V1 是一个 **本地优先、低 token、可插拔模型、可复用 workflow、具备长期记忆的 Agent Runtime**。

## 快速开始

### 初始化

首次运行会自动初始化 SQLite 数据库（`data/tigerclaw.db`）和表结构。

```powershell
# 无需额外初始化，直接运行
dotnet build
```

### 意图路由（可选 LLM）

默认使用 **规则路由**（关键词）。若希望自然语言里识别「打开淘宝搜索电冰箱」等组合意图，可启用 **LLM 先分类再路由**：

- 设置环境变量 `TIGERCLAW_USE_LLM_INTENT=true`
- 设置 `TIGERCLAW_MODEL_API_KEY`（与 `ModelRouting` 中远程模型一致，如 OpenAI 兼容 API）
- 或在 API 的 `appsettings.json` 中设置 `TigerClaw:ModelRouting:UseLlmIntentRouting` 与 `ApiKey`

失败或关闭时自动回退到规则路由（无额外费用）。

### 运行 CLI

```powershell
# 安装 Playwright 浏览器（首次使用浏览器自动化前）
pwsh src/TigerClaw.Skills/bin/Debug/net8.0/playwright.ps1 install

# 运行文本任务
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- run "读取今天未读邮件并生成摘要"

# 运行命名 workflow
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow run daily_mail_digest

# 列出 skills
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- skills list

# 列出 workflows（OpenClaw 风格：workflow list；兼容 workflows list）
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow list

# JSON 输出 / 帮助 / OpenClaw 兼容前缀
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow list --json
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- run --help
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- openclaw skills list

# 列出 memory aliases
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- memory aliases list

# 设置 preference
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- memory preference set language zh-CN

# 浏览器自动化示例：淘宝搜索（关键词可省略，默认「空调」）
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- taobao search 空调
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow run taobao_search 手机
```

### 启动 API

```powershell
dotnet run --project src/TigerClaw.Api/TigerClaw.Api.csproj
```

API 默认监听 `http://localhost:5000` 或 `http://localhost:5263`（取决于 launchSettings）。

- `GET /health` - 健康检查
- `POST /tasks/run` - 运行任务
- `POST /workflows/{id}/run` - 运行 workflow
- `GET /skills` - 列出 skills
- `GET /workflows` - 列出 workflows
- `GET /memory/preferences` - 获取 preferences
- `POST /memory/preferences` - 保存 preference

### 查看 SQLite 数据

```powershell
sqlite3 data/tigerclaw.db ".tables"
```

## 目录结构

```
TigerClaw.Runtime.V1/
  src/
    TigerClaw.Api/      # REST API
    TigerClaw.Cli/      # 命令行入口
    TigerClaw.Core/     # 核心抽象与接口
    TigerClaw.Runtime/  # Router、Planner、Workflow Engine
    TigerClaw.Memory/   # 记忆存储
    TigerClaw.Models/   # 共享模型
    TigerClaw.Skills/   # 内置 skill 实现
    TigerClaw.Workflows/# workflow 解析与模板
    TigerClaw.Infrastructure/  # SQLite、配置、适配器
  tests/
  data/                 # SQLite 数据目录
  workflows/            # workflow JSON 定义
  skills/               # skill 定义 (skills.json)
  logs/
  scripts/
```

## 添加新 Skill

1. 在 `skills/skills.json` 中添加 skill 定义
2. 在 `TigerClaw.Skills` 中实现 `ISkill` 接口
3. 在 `ServiceCollectionExtensions.cs` 中注册

## 新增 Workflow

在 `workflows/` 目录下创建 `{workflowId}.json`，例如：

```json
{
  "id": "my_workflow",
  "name": "My Workflow",
  "steps": [
    {
      "id": "s1",
      "skillId": "file.read_text",
      "inputs": { "path": "input.txt" },
      "nextStepId": "s2"
    }
  ]
}
```

## 技术栈

- .NET 8
- SQLite (Microsoft.Data.Sqlite)
- Dapper
- ASP.NET Core Minimal API

## 许可证

MIT
