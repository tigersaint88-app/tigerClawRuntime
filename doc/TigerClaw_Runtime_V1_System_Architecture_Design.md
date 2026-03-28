# TigerClaw Runtime V1 系统架构设计文档

## 1. 文档目标

本文档定义 TigerClaw Runtime V1 的系统边界、核心模块、数据流、部署方式、长期记忆方案、模型调度策略、工程目录建议与 V1 交付范围。目标不是做一个“聊天机器人”，而是做一个 **本地优先、低 token、可插拔模型、可复用 workflow、具备长期记忆的 Agent Runtime**。

---

## 2. 设计目标

### 2.1 核心目标
- 支持本地 LLM 与远程 LLM 混合使用
- 显著降低单次任务 token 消耗
- 提供长期记忆，而不是仅依赖聊天上下文
- 支持 skill 注册、workflow 编排、任务执行与人工介入节点
- 支持桌面入口、CLI、Web/API 等多种入口
- 支持未来扩展到 Email、Browser、File、Excel、Desktop Automation、Test Automation

### 2.2 非目标
V1 暂不覆盖：
- 大规模分布式调度
- 多租户 SaaS 商业计费系统
- 完整 skill marketplace
- 全行业复杂权限治理
- 完整可视化 workflow designer

---

## 3. 产品定位

TigerClaw Runtime V1 是一个：

**Hybrid Agent Runtime**
- Local-first
- Token-efficient
- Workflow-centric
- Memory-enabled
- Skill-executable

它不是把所有问题都交给大模型，而是用以下原则驱动：
1. 能不用 LLM 就不用
2. 能用规则就不用模型
3. 能用小模型就不用大模型
4. 能复用 workflow 就不重复规划
5. 能命中记忆就不重复生成

---

## 4. 总体架构

```text
+-------------------------------------------------------------+
|                     TigerClaw Entry Layer                   |
|   Desktop Entry / CLI / Web UI / API / Cursor Extension     |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                    Session & Request Gateway                |
|  session, auth context, request normalization, tracing id   |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                        Intent Router                        |
| rules -> classifier -> planner escalation                   |
+------------------+-------------------+----------------------+
                   |                   |
                   v                   v
         +----------------+   +-----------------------------+
         | Memory Retrieve |   | Model Adapter              |
         | alias/profile   |   | local / remote model route |
         +----------------+   +-----------------------------+
                   \                   /
                    \                 /
                     v               v
+-------------------------------------------------------------+
|                        Task Planner                         |
| template workflow / dynamic planning / parameter filling    |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                       Workflow Engine                       |
| steps, branching, retry, human checkpoint, rollback hooks   |
+------------------+-------------------+----------------------+
                   |                   |
                   v                   v
        +-------------------+   +----------------------------+
        | Skill Registry    |   | Execution Context Store    |
        | schema/capability |   | state/result/artifacts     |
        +-------------------+   +----------------------------+
                   |
                   v
+-------------------------------------------------------------+
|                        Skill Runtime                        |
| Email / Browser / File / Excel / Shell / Desktop / Test    |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                         Memory Layer                        |
| profile / alias / procedure / semantic / task history       |
+-------------------------------------------------------------+
```

---

## 5. 核心模块设计

## 5.1 Entry Layer
### 职责
- 接收用户请求
- 保持会话标识
- 输出执行结果与状态
- 支持未来 GUI 和桌面悬浮入口

### V1 入口建议
- CLI
- REST API
- 简单 Web UI
- 预留桌面入口接口

### 输入输出
输入：
- text command
- task request
- optional attachments
- user/session metadata

输出：
- task accepted
- current step
- structured result
- artifact path
- error summary

---

## 5.2 Session & Request Gateway
### 职责
- 统一请求格式
- 生成 trace id / task id / session id
- 处理用户配置上下文
- 记录审计日志

### 核心对象
```json
{
  "requestId": "uuid",
  "sessionId": "uuid",
  "userId": "local-user",
  "inputText": "读取今天未读邮件并生成摘要",
  "channel": "cli",
  "attachments": [],
  "preferencesSnapshot": {}
}
```

---

## 5.3 Intent Router
### 职责
负责低成本决定任务走哪条路径：

#### 第 1 层：规则路由
适合固定命令：
- /run daily_mail_digest
- /open sina_mail
- /workflow list

#### 第 2 层：轻量分类
用小模型或关键词分类：
- email
- browser
- file
- excel
- test
- generic_chat

#### 第 3 层：升级到 planner
只有复杂需求才进入动态规划。

### 设计目标
- 降低 token 消耗
- 缩短响应时间
- 避免所有任务都走大模型

---

## 5.4 Memory Retrieval
### 职责
在规划前按需取回相关记忆，不把全部历史塞进 prompt。

### 记忆类型
1. Profile Memory
2. Alias Memory
3. Procedure Memory
4. Semantic Memory
5. Recent Task Summary

### 检索策略
- exact key match
- tag match
- embedding similarity
- recent recency bonus

### V1 建议
- Profile/Alias 用 SQLite 表
- Semantic 用 SQLite + vector extension 或单独向量索引
- 先实现简单 top-k 检索

---

## 5.5 Model Adapter
### 职责
统一封装多模型调用能力。

### 必须支持
- OpenAI-compatible API
- Ollama
- 可扩展到 llama.cpp / LM Studio / vLLM

### 任务分流建议
- Intent classify -> local small model
- Parameter extraction -> local small model
- Short summary -> local medium model
- Complex plan -> remote strong model
- Sensitive tasks -> forced local

### 接口示意
```ts
interface IModelAdapter {
  complete(request: ModelRequest): Promise<ModelResponse>;
  embed(request: EmbeddingRequest): Promise<EmbeddingResponse>;
}
```

---

## 5.6 Task Planner
### 职责
根据 intent、记忆、模板和可用 skill 生成可执行计划。

### 两种模式
#### A. Template-first
优先命中预定义 workflow 模板，例如：
- daily mail digest
- export excel report
- login browser and download file

#### B. Dynamic planning
复杂任务由模型生成步骤，但仍需转换为标准 workflow step。

### 输出格式
```json
{
  "planType": "workflow",
  "workflowId": "daily_mail_digest",
  "steps": [
    { "id": "s1", "skill": "email.fetch_unread", "inputs": {} },
    { "id": "s2", "skill": "text.summarize", "inputs": {} }
  ]
}
```

---

## 5.7 Workflow Engine
### 职责
执行标准化步骤流。

### V1 必须支持
- 顺序执行
- 条件分支
- 重试
- timeout
- 人工介入节点
- step result 记录
- resumable execution

### Step 状态
- pending
- running
- success
- failed
- waiting_human
- skipped
- cancelled

### Human Checkpoint 示例
```json
{
  "type": "human_checkpoint",
  "reason": "captcha required",
  "instruction": "请用户完成验证码后点击继续"
}
```

---

## 5.8 Skill Registry
### 职责
统一管理 skill 元数据。

### Skill 元数据字段
- id
- name
- description
- tags
- input schema
- output schema
- auth requirement
- execution mode
- risk level
- supported channels

### 示例
```json
{
  "id": "email.fetch_unread",
  "name": "Fetch unread emails",
  "tags": ["email", "mailbox", "summary"],
  "riskLevel": "medium",
  "executionMode": "local",
  "inputSchema": {
    "type": "object",
    "properties": {
      "accountId": { "type": "string" },
      "folder": { "type": "string" }
    }
  }
}
```

---

## 5.9 Skill Runtime
### 职责
真正执行动作，而不是让模型模拟执行。

### V1 建议内置 Skill
1. file.read_text
2. file.write_text
3. shell.run
4. browser.open_url
5. browser.extract_text
6. email.fetch_unread
7. text.summarize
8. memory.save_preference
9. workflow.run_named
10. human.wait_for_continue

### 未来扩展
- desktop automation
- excel automation
- test automation
- doc parser
- OCR + image object recognition

---

## 5.10 Memory Layer
### 设计原则
记忆不是聊天记录堆积，而是结构化、可检索、可解释、可更新的数据层。

### 记忆分类

#### A. Profile Memory
长期偏好
```json
{
  "language": "zh-CN",
  "summaryStyle": "brief",
  "preferredModel": "qwen-local-7b"
}
```

#### B. Alias Memory
别名映射
```json
{
  "老板邮箱": "boss@company.com",
  "1号服务器": "TestServer01"
}
```

#### C. Procedure Memory
任务成功路径
```json
{
  "taskType": "sina_mail_login",
  "stepsSummary": [
    "open login page",
    "fill username",
    "fill password",
    "wait human captcha"
  ]
}
```

#### D. Semantic Memory
文档、说明书、环境知识、用户上传资料

#### E. Task History Summary
历史执行摘要，不保留全部原始上下文进入模型

---

## 5.11 Observability & Audit
### 目标
- 可调试
- 可解释
- 可审计

### 记录内容
- task id
- step logs
- model used
- prompt token / completion token
- execution duration
- skill call result
- memory hits

### 输出方式
- console log
- json log
- SQLite audit table

---

## 6. 数据流

## 6.1 用户任务执行主流程
```text
User Request
  -> Gateway
  -> Intent Router
  -> Retrieve Memory
  -> Planner
  -> Workflow Engine
  -> Skill Runtime
  -> Save Results & Memory
  -> Return Final Response
```

## 6.2 复杂任务流程
```text
Input Text
  -> classify as complex
  -> retrieve relevant skills + memory
  -> remote model planning
  -> compile plan to workflow steps
  -> execute
  -> summarize
```

## 6.3 低 token 任务流程
```text
Input Text
  -> exact rule / template match
  -> fill parameters from memory
  -> execute workflow directly
```

---

## 7. 低 token 策略

## 7.1 Prompt 分层
- Core prompt：固定最小系统指令
- Task prompt：场景级
- Skill prompt：仅加载相关 skill
- Memory prompt：只注入 top-k 相关记忆

## 7.2 Skill 检索而不是全量注入
不要将全部 skill 描述发送给模型，仅检索可能相关的 3-5 个。

## 7.3 Session Summary
- 最近 4-8 轮对话保留原文
- 更早对话仅保留摘要

## 7.4 Workflow Reuse
命中模板时不重新规划。

## 7.5 Small Model First
简单分类与抽取使用本地模型。

## 7.6 Structured Result Compression
skill 执行结果只回灌关键字段，而不是全量日志。

---

## 8. 存储设计

## 8.1 V1 建议技术选型
- SQLite：配置、任务、审计、profile、alias、procedure
- 文件系统：artifacts、logs、workflow definitions
- JSON/YAML：skill registry 与 workflow template

## 8.2 目录建议
```text
workspace/
  config/
  data/
    tigerclaw.db
  memory/
    embeddings/
  workflows/
  skills/
  artifacts/
  logs/
```

## 8.3 核心表
- users
- sessions
- tasks
- task_steps
- preferences
- aliases
- procedures
- memory_documents
- audit_logs

---

## 9. 安全设计

### V1 安全要求
- 本地密钥与配置分离
- 高风险 skill 需要显式确认
- human checkpoint 支持
- 审计日志可追踪
- 敏感任务可强制本地模型
- 记忆数据支持删除与更新

### 风险分级
- low：文件读取、摘要
- medium：浏览器登录、邮件读取
- high：桌面控制、批量发送、删除数据

---

## 10. 部署模式

## 10.1 单机本地模式
适合个人用户
- runtime
- sqlite
- local model
- optional remote API

## 10.2 本地 + 远程混合模式
- 本地做分类、记忆、执行
- 远程做复杂推理

## 10.3 企业内网模式
- 私有模型网关
- 内网 skill services
- 本地部署 memory store

---

## 11. 推荐技术栈

### 若以 .NET 为主
- .NET 8
- ASP.NET Core Minimal API
- SQLite + Dapper/EF Core
- Spectre.Console for CLI
- Semantic Kernel 可选，但建议轻量自研 orchestration

### 若以 TypeScript 为主
- Node.js
- Fastify/Express
- better-sqlite3
- zod
- local LLM adapters

### 结合你的背景建议
优先用 **.NET 8 + C#** 做核心 runtime：
- 更适合桌面、Windows automation、企业集成
- 后续接 Avalonia、MARS、自动化引擎更顺

---

## 12. 建议工程目录（C# 版本）

```text
TigerClaw/
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
  docs/
  samples/
  scripts/
```

### 各项目职责
- TigerClaw.Api：REST API
- TigerClaw.Cli：命令行入口
- TigerClaw.Core：核心抽象、接口、模型
- TigerClaw.Runtime：router/planner/executor
- TigerClaw.Memory：memory store、retrieval、embedding service
- TigerClaw.Skills：内置 skill 实现
- TigerClaw.Workflows：workflow parser 与 templates
- TigerClaw.Infrastructure：sqlite、config、logging、adapters

---

## 13. V1 功能范围

### 必做
- CLI + API
- skill registry
- workflow engine
- local/remote model adapter
- profile/alias/procedure memory
- sqlite persistence
- audit log
- named workflow run
- email/browser/file/shell/text summary 基础能力

### 可选
- web UI
- desktop entry
- embeddings
- semantic memory document upload

### 不做
- 完整 market
- 多人权限系统
- 分布式 agent cluster

---

## 14. V1 典型场景

### 场景 A：邮件摘要
输入：
“读取今天未读邮件，过滤广告，生成中文摘要”

执行：
- intent route -> mail digest template
- fetch unread
- filter
- summarize
- save task history

### 场景 B：测试助手
输入：
“根据安装文档生成测试 checklist”

执行：
- file/doc read
- semantic retrieve
- structured generate
- save checklist artifact

### 场景 C：浏览器工作流
输入：
“打开后台系统，登录并导出日报”

执行：
- named workflow or dynamic plan
- browser steps
- human checkpoint if captcha
- export artifact

---

## 15. 未来演进路线

## V1
低 token runtime + memory + basic workflow + skill execution

## V1.5
桌面入口 + better UI + semantic memory + visual execution panel

## V2
workflow designer + plugin SDK + desktop automation + skill market prototype

## V3
enterprise governance + team memory + marketplace + billing

---

## 16. 成功标准

### 技术指标
- 平均简单任务 token 降低 50% 以上
- 模板命中任务响应时间 < 2 秒（不含外部执行）
- workflow 可恢复执行
- 本地/远程模型切换稳定

### 产品指标
- 邮件摘要、文件处理、浏览器任务 3 个场景可用
- 用户偏好和 alias 生效
- 日志与执行步骤可追踪

---

## 17. 结论

TigerClaw Runtime V1 的重点不是“更会聊天”，而是：
- 更少 token
- 更强执行
- 更可控
- 更可记忆
- 更适合真实工作流

架构核心思想是：

**LLM 负责理解与推理，Runtime 负责控制与执行，Memory 负责持续积累。**

这三者分离后，系统才能真正工程化。
