# Cursor 可执行详细指令：TigerClaw Runtime V1

本文档用于指导 Cursor 从 0 开始生成 TigerClaw Runtime V1 的初始代码骨架、核心模块与首批可运行功能。默认技术栈为 **.NET 8 + C# + SQLite**。

---

## 1. 你的角色

你现在扮演资深架构师 + 资深 .NET 工程师。你的任务不是只生成示例代码，而是要创建一个 **可编译、可运行、可扩展** 的 TigerClaw Runtime V1 初始工程。

必须遵守以下要求：

1. 所有代码使用 .NET 8
2. 所有项目和类名清晰、工程化
3. 先搭建骨架，再逐步填充实现
4. 不要把所有逻辑写在 Program.cs
5. 优先接口隔离与模块化设计
6. 输出必须能在 Windows 本地运行
7. 使用 SQLite 做本地存储
8. 使用 JSON 文件保存 workflow 和 skill registry
9. 所有模块必须有基础日志
10. 为后续接入本地 LLM 和远程 LLM 预留标准接口

---

## 2. 最终目标

生成一个可运行的解决方案，包含：
- CLI 入口
- Minimal API 入口
- 核心 Runtime
- Intent Router
- Workflow Engine
- Skill Registry
- Memory Store
- SQLite persistence
- Local/Remote Model Adapter 抽象
- 5~10 个内置 skill
- 2~3 个示例 workflow
- 单元测试基础项目

---

## 3. 解决方案名称与目录

请创建解决方案：

`TigerClaw.Runtime.V1.sln`

目录结构必须如下：

```text
TigerClaw.Runtime.V1/
  src/
    TigerClaw.Api/
    TigerClaw.Cli/
    TigerClaw.Core/
    TigerClaw.Runtime/
    TigerClaw.Memory/
    TigerClaw.Models/
    TigerClaw.Skills/
    TigerClaw.Workflows/
    TigerClaw.Infrastructure/
  tests/
    TigerClaw.Core.Tests/
    TigerClaw.Runtime.Tests/
    TigerClaw.Memory.Tests/
    TigerClaw.Integration.Tests/
  data/
  workflows/
  skills/
  logs/
  docs/
  scripts/
```

请先创建完整 solution 和 projects，并建立正确的项目引用关系。

---

## 4. 项目职责定义

### TigerClaw.Core
放核心抽象与接口：
- IIntentRouter
- ITaskPlanner
- IWorkflowEngine
- ISkill
- ISkillRegistry
- IMemoryStore
- IModelAdapter
- IAuditLogger
- ITaskContextFactory

### TigerClaw.Models
放共享模型：
- TaskRequest
- TaskResponse
- WorkflowDefinition
- WorkflowStepDefinition
- SkillDefinition
- ExecutionContext
- StepExecutionResult
- MemoryRecord
- UserPreference
- AliasRecord
- ProcedureRecord
- ModelRequest
- ModelResponse

### TigerClaw.Runtime
放 orchestration：
- IntentRouter
- TaskPlanner
- WorkflowEngine
- TaskExecutor
- RuntimeFacade

### TigerClaw.Memory
放记忆与检索：
- MemoryStore
- PreferenceService
- AliasService
- ProcedureMemoryService
- SemanticMemoryService（先做接口和 stub）

### TigerClaw.Skills
放 skill 实现：
- FileReadSkill
- FileWriteSkill
- ShellRunSkill
- TextSummarizeSkill
- MemorySavePreferenceSkill
- WorkflowRunNamedSkill
- HumanWaitForContinueSkill
- BrowserOpenUrlSkill（先做 stub）
- EmailFetchUnreadSkill（先做 stub）

### TigerClaw.Workflows
放：
- WorkflowLoader
- WorkflowValidator
- WorkflowTemplateResolver

### TigerClaw.Infrastructure
放：
- SQLite repositories
- Config loader
- Json file loaders
- Logging
- Model adapters
- Audit logger implementation

### TigerClaw.Cli
命令行入口：
- run text task
- run named workflow
- list skills
- list workflows
- show memory aliases
- save preference

### TigerClaw.Api
ASP.NET Core Minimal API：
- POST /tasks/run
- POST /workflows/{name}/run
- GET /skills
- GET /workflows
- GET /memory/preferences
- POST /memory/preferences

---

## 5. 技术选型要求

请使用以下依赖：
- .NET 8
- Microsoft.Data.Sqlite
- Dapper 或 EF Core（二选一，优先 Dapper，轻量）
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging
- System.Text.Json
- xUnit

不要引入过重依赖。不要默认引入复杂 Agent framework。

---

## 6. 代码风格要求

1. 每个 public 类必须有清晰职责
2. 不允许 God Class
3. async/await 规范
4. 所有外部 IO 代码要有异常处理
5. 所有模型尽量使用 record
6. 所有 JSON schema 读取使用强类型模型
7. 所有 service 通过 DI 注册
8. 所有项目编译无 warning 为目标
9. 所有核心方法有 XML 注释或清晰注释
10. 所有日志包含 taskId / workflowId / stepId 上下文

---

## 7. 第一阶段：生成解决方案骨架

请按顺序完成：

### 第一步
创建 solution 与全部 projects。

### 第二步
建立项目引用关系，确保依赖方向正确：
- Api/Cli -> Runtime/Core/Models/Infrastructure/Skills/Workflows/Memory
- Runtime -> Core/Models
- Memory -> Core/Models
- Skills -> Core/Models
- Workflows -> Core/Models
- Infrastructure -> Core/Models
- Tests -> 引用各自目标项目

### 第三步
创建每个项目的基础 README 或说明注释。

### 第四步
确保解决方案能空编译通过。

---

## 8. 第二阶段：定义核心模型

请完整生成以下模型，放在 TigerClaw.Models 中。

### TaskRequest
字段建议：
- RequestId
- SessionId
- UserId
- InputText
- Channel
- Attachments
- Metadata
- CreatedAtUtc

### TaskResponse
字段建议：
- RequestId
- Success
- Message
- FinalText
- Artifacts
- WorkflowId
- Steps
- ErrorCode

### WorkflowDefinition
字段建议：
- Id
- Name
- Description
- Tags
- Parameters
- Steps

### WorkflowStepDefinition
字段建议：
- Id
- Name
- Type
- SkillId
- Inputs
- NextStepId
- OnFailureStepId
- RetryPolicy
- HumanInstruction

### SkillDefinition
字段建议：
- Id
- Name
- Description
- Tags
- RiskLevel
- InputSchemaJson
- OutputSchemaJson
- ExecutionMode

### ExecutionContext
字段建议：
- TaskId
- WorkflowId
- CurrentStepId
- Variables
- Artifacts
- UserId
- StartedAtUtc

### 其他模型
请一并补齐。

要求：
- 使用 record 或 class + init
- 命名一致
- 便于序列化

---

## 9. 第三阶段：定义接口

请在 TigerClaw.Core 中完整生成：

- IIntentRouter
- ITaskPlanner
- IWorkflowEngine
- ISkill
- ISkillRegistry
- IMemoryStore
- IPreferenceService
- IAliasService
- IProcedureMemoryService
- IModelAdapter
- IAuditLogger
- IWorkflowLoader
- IWorkflowTemplateResolver
- IRuntimeFacade

每个接口都要有合理的方法，不要空壳。

---

## 10. 第四阶段：实现 SQLite 与配置基础设施

请在 TigerClaw.Infrastructure 中生成：

### 10.1 配置类
- TigerClawOptions
- DatabaseOptions
- ModelRoutingOptions
- WorkspaceOptions

### 10.2 SQLite 初始化
创建：
- DatabaseInitializer
- 建表 SQL
- migrations/bootstrap.sql

至少创建这些表：
- preferences
- aliases
- procedures
- tasks
- task_steps
- audit_logs

### 10.3 Repository
优先使用 Dapper 实现：
- PreferenceRepository
- AliasRepository
- ProcedureRepository
- TaskRepository
- AuditLogRepository

### 10.4 Json Loader
- SkillDefinitionLoader
- WorkflowDefinitionLoader

---

## 11. 第五阶段：实现 Memory 模块

请在 TigerClaw.Memory 中实现：

### PreferenceService
支持：
- get by key
- upsert
- list all

### AliasService
支持：
- get by alias
- upsert
- list all

### ProcedureMemoryService
支持：
- save procedure summary
- search by task type
- list all

### SemanticMemoryService
先建立接口和 stub，返回 NotImplemented 风格的安全结果，不抛崩溃级异常。

### MemoryStore
作为聚合门面，统一协调上述服务。

---

## 12. 第六阶段：实现 Skill Registry 与基础 skills

### Skill Registry
在 TigerClaw.Runtime 或 Skills 中实现：
- JsonSkillRegistry
- 支持从 `/skills/skills.json` 读取
- 支持按 id 获取
- 支持按 tag 搜索
- 支持列出全部 skill

### 必做 skills
请实现以下技能，全部实现标准接口 ISkill：

1. `file.read_text`
2. `file.write_text`
3. `shell.run`
4. `text.summarize`
5. `memory.save_preference`
6. `workflow.run_named`
7. `human.wait_for_continue`

### Stub skills
先实现接口与基础结果：
8. `browser.open_url`
9. `email.fetch_unread`

### Skill 实现要求
- 输入参数使用 Dictionary 或强类型解析
- 返回统一 SkillExecutionResult
- 有日志
- 对错误有安全处理

---

## 13. 第七阶段：实现 Model Adapter

请在 Infrastructure 中实现：

### IModelAdapter 的两个实现
1. `LocalModelAdapter`
2. `RemoteOpenAiCompatibleModelAdapter`

### 当前阶段要求
- 先实现接口和可替换 stub
- TextSummarizeSkill 可优先调用本地 fake summarizer
- 配置文件中预留 API base url、model name、api key 字段

### 路由器
再实现一个：
- `ModelRouter`

它根据任务类型选择 local / remote。

规则先简化为：
- classify / extract -> local
- summarize_short -> local
- complex_plan -> remote
- sensitive -> local

---

## 14. 第八阶段：实现 Intent Router 与 Planner

### IntentRouter
支持三层：
1. 规则匹配
2. 关键词分类
3. fallback to model classifier

至少支持识别这些 intent：
- run_named_workflow
- email_digest
- file_read
- file_write
- save_preference
- list_skills
- generic_task

### TaskPlanner
优先 template-first：
- 若命中 workflow 模板，直接返回 workflow plan
- 否则尝试 skill-based plan
- 否则 fallback 为 generic plan

### 输出
必须输出标准化的可执行计划，而不是自然语言段落。

---

## 15. 第九阶段：实现 Workflow Engine

必须支持：

1. 顺序执行
2. step result 保存
3. 失败停止
4. retry policy
5. human checkpoint
6. named workflow 执行
7. context variables 传递

### 关键类
- WorkflowEngine
- WorkflowExecutor
- StepInputResolver
- ExecutionContextFactory

### 规则
- step 的 inputs 支持变量替换，例如 `{{input.accountId}}`、`{{context.previous.summary}}`
- human.wait_for_continue 先在 CLI 模式中提示用户按键继续
- 失败时写 audit log

---

## 16. 第十阶段：实现 Runtime Facade

请实现一个上层入口：
- `RuntimeFacade`

方法建议：
- RunTaskAsync(TaskRequest request)
- RunWorkflowAsync(string workflowId, Dictionary<string, object?> inputs)
- ListSkillsAsync()
- ListWorkflowsAsync()

它作为 CLI 和 API 共同调用的统一入口。

---

## 17. 第十一阶段：实现 CLI

请在 TigerClaw.Cli 中实现命令：

### 命令 1
```bash
tigerclaw run "读取今天未读邮件并生成摘要"
```

### 命令 2
```bash
tigerclaw workflow run daily_mail_digest
```

### 命令 3
```bash
tigerclaw skills list
```

### 命令 4
```bash
tigerclaw workflows list
```

### 命令 5
```bash
tigerclaw memory aliases list
```

### 命令 6
```bash
tigerclaw memory preference set language zh-CN
```

要求：
- 输出整洁
- 出错信息明确
- human checkpoint 可交互

---

## 18. 第十二阶段：实现 API

请在 TigerClaw.Api 中实现 Minimal API：

- `POST /tasks/run`
- `POST /workflows/{id}/run`
- `GET /skills`
- `GET /workflows`
- `GET /memory/preferences`
- `POST /memory/preferences`

加上：
- Swagger
- 健康检查 `/health`
- 基础异常中间件
- 请求日志

---

## 19. 第十三阶段：生成示例 JSON 文件

### 19.1 skills.json
请生成一个完整的 `skills/skills.json`，包含全部内置 skill 定义。

### 19.2 workflows
请生成以下 workflow：

#### workflow 1: daily_mail_digest.json
步骤：
1. email.fetch_unread
2. text.summarize
3. file.write_text

#### workflow 2: save_user_language_pref.json
步骤：
1. memory.save_preference

#### workflow 3: open_url_with_human_checkpoint.json
步骤：
1. browser.open_url
2. human.wait_for_continue

要求：
- JSON 结构规范
- 可被 loader 正确解析

---

## 20. 第十四阶段：测试

请至少创建以下测试：

### Core.Tests
- model serialization tests
- step variable resolution tests

### Runtime.Tests
- intent routing tests
- planner template resolution tests
- workflow execution tests

### Memory.Tests
- preference upsert/get tests
- alias lookup tests

### Integration.Tests
- CLI run workflow smoke test
- API health test
- end-to-end workflow execution test

---

## 21. 第十五阶段：工程完善

请补充：

1. `appsettings.json`
2. `appsettings.Development.json`
3. `.gitignore`
4. `README.md`
5. `scripts/bootstrap.ps1`
6. `scripts/run-cli.ps1`
7. `scripts/run-api.ps1`

README 中必须包括：
- 如何初始化
- 如何运行 CLI
- 如何启动 API
- 如何查看 SQLite 数据
- 如何添加新 skill
- 如何新增 workflow

---

## 22. 代码生成顺序要求

请严格分阶段执行，不要一次性胡乱生成。

### 第 1 轮
先生成 solution、projects、引用关系、基础 Program、空编译通过。

### 第 2 轮
生成 Models 与 Core 接口。

### 第 3 轮
生成 Infrastructure + SQLite 初始化。

### 第 4 轮
生成 Memory 与 Skill Registry。

### 第 5 轮
生成 Skills。

### 第 6 轮
生成 Runtime：Router、Planner、Workflow Engine、Facade。

### 第 7 轮
生成 CLI 与 API。

### 第 8 轮
生成 JSON 配置、样例 workflow、测试、README、脚本。

每完成一轮，请先修复编译错误，再进入下一轮。

---

## 23. 关键实现细节要求

### 23.1 关于 `text.summarize`
V1 不要强依赖真实远程模型。先这样实现：
- 若配置启用 fake summarizer，则返回前 N 行摘要
- 若配置启用 real adapter，则调用 model router

### 23.2 关于 `email.fetch_unread`
先做 stub，返回模拟邮件列表：
- subject
- sender
- date
- body snippet

后续替换成真实邮箱 skill。

### 23.3 关于 `browser.open_url`
先做 stub，返回“已请求打开 URL”，并记录参数。

### 23.4 关于 human checkpoint
CLI 下使用：
- 输出说明
- 等待用户按 Enter
API 下返回 waiting_human 状态

---

## 24. 成果验收标准

当你完成后，工程必须满足：

1. `dotnet build` 成功
2. CLI 可以列出 skills/workflows
3. CLI 可以运行 named workflow
4. preference 能写入 SQLite
5. alias 能查询
6. API 能启动并访问 `/health`
7. workflow 执行日志可追踪
8. 至少 1 个端到端 workflow 可执行成功

---

## 25. 最后执行要求

请你立即开始工作，按以下方式输出：

1. 先创建 solution 与项目结构
2. 然后逐轮输出新增文件清单
3. 对每轮生成的关键文件给出完整代码
4. 明确说明要执行的命令
5. 每轮结束时给出“下一轮继续”的建议
6. 任何时候都不要偷懒用伪代码替代核心实现

如果某个模块暂时只能 stub，请明确标注 TODO，并确保系统仍然可编译、可运行、可扩展。
