# TigerClaw Runtime V1

[English](#english) · [中文](#中文)

**Repository:** [https://github.com/tigersaint88-app/tigerClawRuntime](https://github.com/tigersaint88-app/tigerClawRuntime)

---

## English

TigerClaw Runtime V1 is a **local-first, low-token, pluggable-model, workflow-reusable agent runtime** with **long-term memory** (SQLite), **CLI + REST API**, and **Capability Resolution v1** (preflight checks, policy blocks, `bins` / `anybins` manifests).

### V1 (shipped) vs target (roadmap)

| Topic | V1 (this repo) | Target / later |
|--------|----------------|----------------|
| **Intents per message** | **One** `RoutingResult` per request (rules: first keyword match; optional LLM: one JSON intent). | Multiple intents from a single utterance, ordered or parallel plans. |
| **Orchestration** | **One workflow** per run; step order = that workflow’s `steps` in JSON. Not a chain of multiple intents. | Cross-intent pipelines, dynamic DAG, or planner-generated multi-workflow runs. |
| **Skills** | Registry + workflow `skillId`; prerequisites + **capability preflight** each step. | Richer skill marketplace contracts, optional remote skills. |
| **Human / config gaps** | `TaskResponse.outcome` = `needs_user_input`, `issues`, `suggestedPreferenceKeys`, `remediationHint`; client retries after `POST /memory/preferences`. | Session-scoped wizards, optional server-side retry budgets. |
| **Email / IMAP** | MailKit, SQLite prefs, placeholder hosts rejected before connect; **`TIGERCLAW_EMAIL_DRY_RUN`**; built-in **`EmailProviderLookup`** + `POST /email/provider-hint` (not live web search). | LLM or online autodiscover, OAuth providers, bounded auto-retry (e.g. 3) with policy. |
| **Interrupt** | No first-class “cancel running task” API; user stops by not re-invoking. | Explicit cancel, checkpoints, resume tokens. |
| **CLI parity** | See `doc/TigerClaw_CLI_Compatibility_Spec_v1.md` — several groups are stubs. | Align with OpenClaw-style CLI surface where desired. |

V1 is **intentionally** bounded: predictable, local-first, and easy to host. The table above is the contract for what is **implemented today** versus **direction**, not a commitment schedule.

### OpenClaw compatibility (CLI & skill metadata)

TigerClaw is **not** a drop-in host for arbitrary external OpenClaw skill packages (e.g. ad hoc npm bundles). Compatibility is **layered**:

| Layer | What matches OpenClaw-style expectations | Limits |
|--------|------------------------------------------|--------|
| **CLI surface** | Command groups (`run`, `workflow`, `skills`, `memory`, …), `--key=value` options, text/json output. First token **`openclaw`** is ignored (alias). See **`doc/TigerClaw_CLI_Compatibility_Spec_v1.md`**. | Several groups are still **stubs**; full parity is **not** guaranteed. |
| **`skills` commands** | `skills list`, `skills show <id>`, `skills exec <id> [--inputs='{"k":"v"}']` run against the in-process registry. | Execution always goes to a **built-in** `ISkill` implementation. |
| **Manifest JSON (`skills/skills.json`)** | **`SkillDefinition`** supports optional OpenClaw-oriented fields: **`bins`**, **`anyBins`**, **`env`**, **`config`**, plus **`prerequisites`** / **`prerequisites.capabilities`**. Those feed **capability preflight** (e.g. `bins` → `bin:<name>`, `anyBins` → `anybin:<id>` providers). Optional **`inputSchemaJson`** / **`outputSchemaJson`** exist on the model. | The file format is **TigerClaw’s** schema (single `skills.json` array); it is **not** guaranteed to be byte-for-byte interchangeable with another product’s manifest. |
| **Skill implementations** | Each runnable `skillId` maps to a **C#** `ISkill` registered in DI (`TigerClaw.Skills`). | There is **no** runtime loader for third-party assemblies or separate Node/npm skill processes. Adding a new skill requires **code + registration** in this repo (or a future plugin system—see the spec **Future** section). |

**Summary:** use OpenClaw-like **CLI habits** and **metadata** (`bins` / `anyBins` / env / config) for tooling and preflight; expect **TigerClaw-native** implementations for anything that actually runs.

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

### Email (IMAP)

`email.fetch_unread` uses MailKit against your IMAP server (unread = **not seen**). Preferences (per user): `email.default_account_id`; `email.accounts.{id}.host`, `.port`, `.username`, `.authProfile`; password on `email.accounts.{id}.password` **or** `email.auth_profiles.{profile}.password`; optional `email.accounts.{id}.useSsl` (defaults to TLS except when port is 143). For automation without a real mailbox, set **`TIGERCLAW_EMAIL_DRY_RUN=true`** (skips connecting).

### Install from NuGet (global tool)

Package id: **`TigerClaw.Cli`**. Command on PATH: **`tigerclaw`**.

```powershell
dotnet tool install -g TigerClaw.Cli
tigerclaw workflow list
```

Run commands from a directory that contains **`skills/`** and **`workflows/`** (or clone this repo and `cd` into the repo root). The tool does **not** bundle Playwright browsers; for browser skills, build the repo once and run `pwsh src/TigerClaw.Skills/bin/Release/net8.0/playwright.ps1 install`, or install browsers per [Playwright .NET](https://playwright.dev/dotnet/docs/intro) docs.

**Publish to [nuget.org](https://www.nuget.org)** (one-time: create an account, create an API key with *Push* scope):

```powershell
.\scripts\pack-cli.ps1
.\scripts\publish-nuget.ps1 -ApiKey YOUR_NUGET_API_KEY
```

### API

```powershell
dotnet run --project src/TigerClaw.Api/TigerClaw.Api.csproj
```

Default URLs: `http://localhost:5000` or `http://localhost:5263` (see `launchSettings`).

**Why does CLI hit `imap.example.com`?** The CLI does **not** inject that host. It is read from SQLite (`data/tigerclaw.db`) preferences—often left over from tests or copy-paste. Use `GET /memory/preferences?userId=local-user` (CLI user) or clear/update those keys. **Static demo UI:** after `dotnet run` the API serves **`/email-demo.html`** — TigerClaw chat-style flow (check workflow → show missing issues → email → built-in IMAP hint → password → retry → scroll results). **Clear mail prefs:** `POST /memory/preferences/clear-email` with `{ "userId": "local-user" }`. **IMAP hint (built-in domains):** `POST /email/provider-hint` with `{ "emailOrDomain": "a@sina.com" }`.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| POST | `/tasks/run` | Run a task |
| POST | `/workflows/{id}/run` | Run a workflow |
| GET | `/skills` | List skills |
| GET | `/workflows` | List workflows |
| GET | `/memory/preferences?userId=` | List preferences for that user (CLI uses `local-user`) |
| POST | `/memory/preferences` | Upsert preference |

`POST /workflows/{id}/run` and **`POST /tasks/run`** responses share the same shape. Branch on **`outcome`**: `completed` | `failed` | **`needs_user_input`**. When **`needs_user_input`** (aliases: **`waitingHuman`**, **`requiresUserInput`**), the client must show a form: use **`issues`**, **`suggestedPreferenceKeys`**, **`interactionMessage`**, and **`remediationHint`**, then **`POST /memory/preferences`** (same `userId`) and re-invoke the same workflow or natural-language request. Also returned: **`errorCode`** (e.g. `PREREQUISITE_MISSING`, `CAPABILITY_NOT_MET`, `EMAIL_IMAP_CONNECT_FAILED`).

**Prerequisite issues & sensitive keys:** Each item in **`issues`** may include **`maskKeyInUi`** (boolean). When `true`, interactive clients should **not** show the raw **`key`** by default—display a mask (e.g. `********`) and offer a **reveal** control (the static demo uses an eye button). The runtime sets this flag from heuristics in **`PrerequisiteSensitive`** (e.g. preference keys ending with `.password`, names containing `apikey` / `api_key`, env vars containing `PASSWORD`, `SECRET`, `API_KEY`, or `TOKEN`). Skills may set **`MaskKeyInUi`** explicitly on **`PrerequisiteIssue`** when declaring missing prefs. **`suggestedPreferenceKeys`** remains a flat list for backward compatibility; use **`issues`** for per-key masking behavior.

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

### V1（已实现）与目标（路线图）

| 主题 | V1（本仓库） | 目标 / 后续 |
|------|-------------|------------|
| **单句多意图** | 每次请求 **一个** `RoutingResult`（规则：关键词先匹配；可选 LLM：**一行 JSON、一个 intent**）。 | 单段话解析 **多个 intent** 并排序或并行执行。 |
| **编排** | 一次运行对应 **一个 workflow**；执行顺序 = 该 workflow JSON 里 **`steps` 顺序**，不是「多 intent 流水线」。 | 跨 intent 编排、动态 DAG、或由规划器串联多个 workflow。 |
| **Skill** | 注册表 + workflow 中 `skillId`；每步做 **prerequisites + 能力预检**。 | 更丰富的 skill 契约、可选远程 skill 等。 |
| **人前/配置补全** | `outcome=needs_user_input`，附 `issues`、`suggestedPreferenceKeys`、`remediationHint`；客户端改 preferences 后 **再次调用**。 | 会话级向导、服务端自动重试次数上限等。 |
| **邮件 IMAP** | MailKit + SQLite；占位域名 **连接前拦截**；**`TIGERCLAW_EMAIL_DRY_RUN`**；内置 **`EmailProviderLookup`** + `POST /email/provider-hint`（**非**实时联网检索）。 | LLM/在线 autodiscover、OAuth、连接失败 **有限次**（如 3 次）策略化重试等。 |
| **中断** | 无通用「取消运行中任务」API；用户 **不再调用** 即停止。 | 显式 cancel、checkpoint、resume。 |
| **CLI** | 见 `doc/TigerClaw_CLI_Compatibility_Spec_v1.md`，部分命令组为 stub。 | 按需对齐 OpenClaw 式 CLI 能力面。 |

V1 **有意**保持边界清晰：易部署、行为可预期。上表区分 **当前实现** 与 **方向**，时间表不作承诺。

### OpenClaw 兼容（CLI 与 skill 元数据）

TigerClaw **不能**当作「任意 OpenClaw 外置 skill 包丢进目录即运行」的宿主；兼容是 **分层的**：

| 层次 | 与 OpenClaw 式预期一致之处 | 边界 |
|------|---------------------------|------|
| **CLI 能力面** | 命令分组（`run`、`workflow`、`skills`、`memory` 等）、`--key=value`、文本/JSON 输出；首词 **`openclaw`** 视为别名忽略。详见 **`doc/TigerClaw_CLI_Compatibility_Spec_v1.md`**。 | 部分命令组仍为 **stub**，**不保证**与 OpenClaw 逐项一致。 |
| **`skills` 子命令** | `skills list`、`skills show <id>`、`skills exec <id> [--inputs='{...}']` 走进程内注册表。 | 实际执行只指向 **内置** 的 C# **`ISkill`**。 |
| **清单 JSON（`skills/skills.json`）** | **`SkillDefinition`** 支持类 OpenClaw 的可选字段：**`bins`**、**`anyBins`**、**`env`**、**`config`**，以及 **`prerequisites` / `prerequisites.capabilities`**；用于 **能力预检**（如 `bins` → `bin:<name>`，`anyBins` → `anybin:<id>`）。模型上另有可选 **`inputSchemaJson`** / **`outputSchemaJson`**。 | 文件结构为 **TigerClaw** 约定（单文件 `skills.json` 数组），**不保证**与他站 manifest 逐字节互换。 |
| **Skill 实现** | 每个可运行的 `skillId` 对应仓库内 **C#** `ISkill` 并在 DI 中注册。 | **无**运行时加载第三方程序集或独立 Node/npm skill 进程；新 skill 需 **改代码并注册**（或等 spec 中的 **插件/远程**，尚未落地）。 |

**一句话：** CLI 与元数据（`bins` / `anyBins` / env / config）可按 OpenClaw 习惯对接预检与工具链；**真正执行**的能力集以本仓库 **TigerClaw 内置实现** 为准。

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

### 邮件配置存哪里？为什么会出现 example.com？

- **存储位置**：SQLite **`data/tigerclaw.db`** 表 **`preferences`**，键名如 **`email.default_account_id`**、`email.accounts.{账号id}.host` / `port` / `username` / `authProfile` / `password` 等（与 CLI `memory preference set`、API `POST /memory/preferences` 一致）。
- **不是 CLI 自动写的**：`dotnet run ... -- run "读取今天未读邮件…"` 只把 **用户 Id 固定为 `local-user`** 并路由到 workflow **`daily_mail_digest`**，不会填入 `imap.example.com`。若出现该主机，多半是**之前集成测试、文档示例或手动 `memory preference set`** 写入的同一数据库。
- **演示界面**：启动 API 后打开 **`http://localhost:5263/email-demo.html`**（`wwwroot/`）。**TigerClaw 对话流**：运行检查 → 列出欠缺项 → 输入邮箱 → **内置域名 IMAP 表**（非实时联网）→ 输入密码 → 再次执行 → 底部滚动展示未读与摘要。工具栏可 **清除邮件配置**：`POST /memory/preferences/clear-email`；查域名提示：`POST /email/provider-hint`。

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

### 邮件（IMAP）

`email.fetch_unread` 使用 MailKit 连接 IMAP，未读条件为 **NOT SEEN**。用户级 preferences：`email.default_account_id`；`email.accounts.{id}` 下的 `host`、`port`、`username`、`authProfile`；密码写在 `email.accounts.{id}.password` **或** `email.auth_profiles.{profile}.password`；可选 `email.accounts.{id}.useSsl`（默认启用 TLS，端口 143 除外）。自动化/测试可设 **`TIGERCLAW_EMAIL_DRY_RUN=true`** 跳过真实连接。

### 从 NuGet 安装（全局工具）

包 id：**`TigerClaw.Cli`**。安装后命令行：**`tigerclaw`**。

```powershell
dotnet tool install -g TigerClaw.Cli
tigerclaw workflow list
```

请在包含 **`skills/`** 与 **`workflows/`** 的目录下执行（例如本仓库根目录）。工具包**不含** Playwright 浏览器；若使用浏览器类 skill，可先在本仓库执行 `dotnet build` 后运行 `pwsh src/TigerClaw.Skills/bin/Release/net8.0/playwright.ps1 install`，或按 [Playwright .NET 文档](https://playwright.dev/dotnet/docs/intro) 安装。

**发布到 [nuget.org](https://www.nuget.org)**（需注册账号并创建具有 *Push* 权限的 API Key）：

```powershell
.\scripts\pack-cli.ps1
.\scripts\publish-nuget.ps1 -ApiKey 你的_NUGET_API_KEY
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
| GET | `/memory/preferences?userId=` | 按用户获取 preferences（CLI 为 `local-user`） |
| POST | `/memory/preferences` | 保存 preference |

**`POST /workflows/{id}/run`** 与 **`POST /tasks/run`** 返回结构一致。请以前端分支字段 **`outcome`** 为准：`completed` | `failed` | **`needs_user_input`**。为 **`needs_user_input`** 时（**`waitingHuman` / `requiresUserInput`** 同为 true），必须据 **`issues`**、**`suggestedPreferenceKeys`**、**`interactionMessage`**、**`remediationHint`** 展示表单，**`POST /memory/preferences`**（同一 `userId`）后再**重试同一请求**。另有 **`errorCode`** 等字段。

**前置条件与敏感键名：** **`issues`** 中每一项可含 **`maskKeyInUi`**（布尔）。为 `true` 时，交互式客户端默认应用 **`*` 掩码**展示 **`key`**，并提供 **「眼睛」切换**显示完整键名（静态演示页 **`/email-demo.html`** 已实现；输入邮箱密码步骤下输入框为 `password` 类型，同样有眼睛切换明文）。服务端依据 **`PrerequisiteSensitive`** 启发式设置（例如 preference 键名以 **`.password`** 结尾、含 **`apikey` / `api_key`**；环境变量名含 **PASSWORD / SECRET / API_KEY / TOKEN** 等）。各 skill 也可在构造 **`PrerequisiteIssue`** 时显式设置 **`MaskKeyInUi`**。**`suggestedPreferenceKeys`** 仍为扁平列表；需要按项掩码时请读 **`issues`**。

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
