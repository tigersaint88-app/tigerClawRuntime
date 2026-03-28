# TigerClaw Runtime V1

[English](#english) · [中文](#中文)

**Repository:** [https://github.com/tigersaint88-app/tigerClawRuntime](https://github.com/tigersaint88-app/tigerClawRuntime)

---

## English

TigerClaw Runtime V1 is a **local-first, low-token, pluggable-model, workflow-reusable agent runtime** with **long-term memory** (SQLite), **CLI + REST API**, and **Capability Resolution v1** (preflight checks, policy blocks, `bins` / `anybins` manifests).

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Quick start

The SQLite database (`data/tigerclaw.db`) and schema are created on first run.

```powershell
dotnet build
```

### Optional LLM intent routing

By default, **rule-based routing** (keywords) is used. To classify intents with an LLM first (e.g. Taobao search in natural language):

- Set `TIGERCLAW_USE_LLM_INTENT=true`
- Set `TIGERCLAW_MODEL_API_KEY` (OpenAI-compatible API, same as `ModelRouting`)
- Or configure `TigerClaw:ModelRouting` in `appsettings.json`

On failure or when disabled, routing falls back to rules (no extra API cost).

### CLI

```powershell
# Playwright (first time for browser automation)
pwsh src/TigerClaw.Skills/bin/Debug/net8.0/playwright.ps1 install

dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- run "Summarize today's unread mail"
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow run daily_mail_digest
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- skills list
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow list
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- taobao search 空调
```

### API

```powershell
dotnet run --project src/TigerClaw.Api/TigerClaw.Api.csproj
```

Default URLs: `http://localhost:5000` or `http://localhost:5263` (see `launchSettings`).

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| POST | `/tasks/run` | Run a task |
| POST | `/workflows/{id}/run` | Run a workflow |
| GET | `/skills` | List skills |
| GET | `/workflows` | List workflows |
| GET | `/memory/preferences` | List preferences |
| POST | `/memory/preferences` | Upsert preference |

### Capabilities & policy

- Skill manifests in `skills/skills.json` support **prerequisites** (config, resources) and **capability expressions** (`allOf` / `anyOf` / `noneOf` / `prefer`) under `prerequisites.capabilities`.
- Workspace **`bins/anybins.json`** defines **anybin** candidates (first-class capability providers).
- **`TigerClaw:CapabilityPolicy`** in `appsettings.json`: `BlockedCapabilities`, `UserBlockedCapabilities` override observed availability.

### Repository layout

```
src/
  TigerClaw.Api/           # REST API
  TigerClaw.Cli/           # CLI entry
  TigerClaw.Core/        # Core abstractions
  TigerClaw.Runtime/     # Router, planner, workflow engine
  TigerClaw.Capabilities/# Capability snapshot, probes, preflight, evaluator
  TigerClaw.Memory/      # Memory / preferences
  TigerClaw.Models/      # Shared models
  TigerClaw.Skills/      # Built-in skills
  TigerClaw.Workflows/   # Workflow loading & templates
  TigerClaw.Infrastructure/
workflows/               # Workflow JSON
skills/                  # skills.json
bins/                    # anybins.json
tests/
```

### Stack

- .NET 8, SQLite, Dapper, ASP.NET Core Minimal API

### License

MIT

---

## 中文

TigerClaw Runtime V1 是一个 **本地优先、低 token、可插拔模型、可复用 workflow、具备长期记忆** 的 Agent 运行时，提供 **CLI 与 REST API**，并内置 **Capability Resolution v1**（能力预检、策略封禁、`bins` / `anybins` 清单注册）。

### 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 快速开始

首次运行会自动初始化 SQLite（`data/tigerclaw.db`）及表结构。

```powershell
dotnet build
```

### 意图路由（可选 LLM）

默认 **规则路由**（关键词）。若要用 **LLM 先分类再路由**（例如自然语言「打开淘宝搜索电冰箱」）：

- 环境变量 `TIGERCLAW_USE_LLM_INTENT=true`
- `TIGERCLAW_MODEL_API_KEY`（与 OpenAI 兼容的 `ModelRouting`）
- 或在 API 的 `appsettings.json` 中配置 `TigerClaw:ModelRouting`

失败或未启用时自动回退规则路由，不产生额外模型费用。

### 命令行（CLI）

```powershell
# 浏览器自动化前安装 Playwright（首次）
pwsh src/TigerClaw.Skills/bin/Debug/net8.0/playwright.ps1 install

dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- run "读取今天未读邮件并生成摘要"
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow run daily_mail_digest
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- skills list
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- workflow list
dotnet run --project src/TigerClaw.Cli/TigerClaw.Cli.csproj -- taobao search 空调
```

### 启动 API

```powershell
dotnet run --project src/TigerClaw.Api/TigerClaw.Api.csproj
```

默认监听 `http://localhost:5000` 或 `http://localhost:5263`（见 `launchSettings`）。

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/health` | 健康检查 |
| POST | `/tasks/run` | 运行任务 |
| POST | `/workflows/{id}/run` | 运行 workflow |
| GET | `/skills` | 列出 skills |
| GET | `/workflows` | 列出 workflows |
| GET | `/memory/preferences` | 获取 preferences |
| POST | `/memory/preferences` | 保存 preference |

### 能力与策略

- `skills/skills.json` 中 skill 可配置 **prerequisites**（配置项、资源等）及 **capabilities**（`allOf` / `anyOf` / `noneOf` / `prefer`）。
- 工作区 **`bins/anybins.json`** 定义 **anybin** 候选可执行文件，作为能力提供方。
- `appsettings.json` 中 **`TigerClaw:CapabilityPolicy`**：`BlockedCapabilities`、`UserBlockedCapabilities` 可覆盖探测到的可用能力。

### 目录结构

```
src/
  TigerClaw.Api/           # REST API
  TigerClaw.Cli/           # CLI 入口
  TigerClaw.Core/          # 核心抽象
  TigerClaw.Runtime/       # 路由、规划、工作流引擎
  TigerClaw.Capabilities/  # 资源快照、探测、预检、表达式求值
  TigerClaw.Memory/        # 记忆与偏好
  TigerClaw.Models/        # 共享模型
  TigerClaw.Skills/        # 内置 Skill
  TigerClaw.Workflows/     # Workflow 加载与模板
  TigerClaw.Infrastructure/
workflows/                 # workflow JSON
skills/                    # skills.json
bins/                      # anybins.json
tests/
```

### 技术栈

- .NET 8、SQLite、Dapper、ASP.NET Core Minimal API

### 许可证

MIT

### 推送到 GitHub（示例）

```powershell
git remote add origin https://github.com/tigersaint88-app/tigerClawRuntime.git
git branch -M main
git push -u origin main
```

若已存在 `origin`，可改用：

```powershell
git remote set-url origin https://github.com/tigersaint88-app/tigerClawRuntime.git
git push -u origin main
```

---

## Contributing / 贡献

Issues and pull requests are welcome at [tigersaint88-app/tigerClawRuntime](https://github.com/tigersaint88-app/tigerClawRuntime).

欢迎到 [tigersaint88-app/tigerClawRuntime](https://github.com/tigersaint88-app/tigerClawRuntime) 提 Issue 与 PR。
