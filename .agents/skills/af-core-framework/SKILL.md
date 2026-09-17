---
name: af-core-framework
description: "Maintain or evolve AnimusForge core responsibilities, same-DLL team interfaces and the separate versioned sub-MOD API. Use for AF framework work and verified handoffs, not unrelated gameplay rewrites."
---

# AF 主体框架维护与演进

用于 AF 主体实现、接口演进、审查和交接。先从 `git rev-parse --show-toplevel` 定位仓库，再读根 `AGENTS.md`、`HANDOFF.md` 的当前段和本次涉及的实际代码；历史交接、目录名和测试数字不是当前状态或操作授权。

## 与通用维护 Skill 分工

本 Skill 专门约束 AF 主体与内外接口，不取代通用开发维护规则。当前仓库的通用入口是 `.claude/skills/animusforge-maintainer/SKILL.md`；普通功能/Bug/资源开发按对应 workflow，涉及主体框架时再合用本 Skill。完整分工见 [协调说明](../../../.claude/skills/animusforge-maintainer/references/framework-coordination.md)。

“本任务只做主体”是当前主体重构任务的范围，不是永久禁止用户另行授权制作组业务开发；新的业务任务仍由其真实 owner 负责。通用 Skill 中的 Foundation/Module/Bridge 逻辑目标不强制拆 DLL，也不代表现有目录已实现完整生命周期 Host。

## 架构约定

```text
AnimusForge.dll
├─ AF 主体：对话、LLM、Prompt、标签、记忆、调度
├─ internal 制作组接口与薄桥 → 政策 / 宴会 / GCCZ 等同 DLL 模块
└─ public 版本化 API ← 独立子 MOD DLL
```

- 制作组直接使用同 DLL 的 typed internal 契约；子 MOD 使用单独公开的版本化 DTO/能力接口，不要求其依赖所有制作组模块。
- 两层复用同一套真实主体 owner、权威动作执行和记忆/AFEF 提交；不能靠第二条缩水 LLM 链或重复提交模拟功能完成。
- 主体任务只负责其获准的 AF 主体和 AF 侧接缝；制作组业务规则、数值、状态机和业务存档由其 owner 维护，除非用户另行扩大范围。
- 已落地的一套源码、Bannerlord 1.3.x / 1.4.x 双实现、单模块发布及 Bootstrap 唯一版本选择必须保持；保留程序集/存档兼容责任，不得把目录装配 Ready 当成游戏可执行或实机验收。

## 主体功能允许调整，不把实现写死

稳定的是分层与外部承诺，不是当前内部算法、方法名、模块名单或功能上限。

- 用户要求调整对话、Prompt、话题/标签、记忆、调度或失败提示时，可以改主体真实实现；先写清旧行为、批准的新行为、受影响渠道与兼容方式，再更新实现、对应测试和 HANDOFF。没有授权的既有语义仍保持。
- 内部策略可以重构/替换；优先复用已有配置或窄 typed 策略。确实由制作者/玩家调整的规则、文案、参数放在既有合适配置面，明确默认值、校验、读取时机和失败回退。不要把一切改成配置、增加无消费者的接口或为灵活性加入反射/热路径扫描。
- 人物、场景、存档与请求身份来自实际 owner/context，不硬编码 NPC、测试数据、机器绝对路径或固定提交号。英文用于代码标识符和协议 ID，中文可用于解释、提示词定义及玩家提示。
- Api.V1 的已开放能力以实际实现及当前交接为准，不在 Skill 中固定为只读或全部可用。只有完整调用链、线程/生命周期/失败语义、可探测能力与兼容证据都落地后，才按已确认需求增加能力；破坏性公开变化另开版本/迁移，不能把 NotSupported 改成假成功。
- 旧行为对照用于发现非预期回归，不阻止批准的功能改进。区分“有意变化”与“应保持部分”，保留后者的断言及故障反例；更新精确源码校验时提供新差异证据，不能只刷新 hash 消除失败。

## 实施与覆盖边界

- 用户同时要求目录整理与真实职责拆分时，按[联合工作包](../../../.claude/skills/animusforge-maintainer/references/repository-structure.md#joint-module-packages)同包规划、分步验收；只有结构迁移授权时不扩大范围。按该参考的风险对应表判断前置条件，相关分类 HOLD 不豁免，无关 HOLD 不阻塞具名安全工作包。
- 真实算法与状态必须归属新 owner 并接入实际消费者，不能仅拆 partial、转发回旧业务或复制第二套核心就报完成；混合大类保留逐符号过渡责任，必要兼容壳与被替代实现的退出条件随包列明，不借整理改程序集/存档身份或制作组玩法。
- 性能预算要落到实际 job/record 数量或耗时；不能用“每帧最多几个回调”掩盖单回调遍历全部积压。Skill 规则更新不等于相关生产缺口已经修复。
- 沿一条真实输入到输出/提交的路径定位改动。游戏对象读取/写入在所属主线程；后台只运行明确可后台执行的网络/计算。检查 owner、会话和 generation，区分未开始取消、晚结果丢弃与真正的网络取消。
- Native / Scene / Courier 同类行为需要核对，但不要因为一处改完就宣称三渠道都完成；Scene 的多人接力、旁听、玩家输入去重和唯一权威后处理不能被简化掉。
- 未迁移旧代码保留其当前运行责任；同一大文件混合新旧时按符号/调用点标界，不能把整文件标成已重写。仅在替代路径真实接线且无调用、兼容或存档责任时删除旧代码；不复制一整棵旧源码到新编译目录作“隔离”。
- 当前坐标/已接线/未覆盖范围读 [仓库代码范围图](../../../docs/architecture/af-framework-code-scope.md)；这张图要随代码更新，不是固定白名单。详细内部和外部设计按需读根目录下的 `docs/architecture/af-internal-module-guide-v1.md`、`af-public-api-guide-v1.md`。

## 验证和交接

- 验证选择以维护 Skill 的[风险矩阵](../../../.claude/skills/animusforge-maintainer/references/validation.md)为准：说明/Skill 检查格式、链接、受影响坐标及源码未变；生产改动验证行为和双版本编译。Stage 按相关验收和授权执行，不改一键脚本或默认路由凑 PASS。
- 有代码坐标表时，从仓库根目录运行 `python -B .agents/skills/af-core-framework/scripts/verify_code_map.py` 校验记录提交；加 `--working-tree` 校验当前源码。这仅验证定位，不是游戏功能测试。
- 代码证据记录核实的路径、一基行号、符号、源码提交、真实消费者及未覆盖责任，集中在主台账或已有代码范围图；HANDOFF 只链接摘要，不复制全表。行号是定位辅助，符号和提交才是追踪依据。
- 更新根 HANDOFF 当前入口；仅在确有独立交付需要时建立专题说明。格式与授权交付检查按需读[交接与发布](references/handoff-and-delivery.md)。用户要求本地保留的简明版不作为 GitHub 文档依赖。
- 明确代码/契约/fixture/实际 DLL/实机/旧存档分别验证到哪层；列出剩余事项、下一步及可回滚的提交。不承诺零 BUG，不用旧候选的实机反馈替代新版本验收。
- Skill 不授予推送、部署、恢复自动化或全局安装权限；按本轮用户授权执行，未授权时留本地。不要假装仓库 Skill 能改写宿主系统提示或自动安装到其他成员机器。
