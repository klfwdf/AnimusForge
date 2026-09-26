---
name: animusforge-maintainer
description: "Develop, fix, review and refactor the AnimusForge Bannerlord mod while preserving dual 1.3/1.4 support, runtime and save contracts. Use for verified AF work, not other mods or generic plugin architecture."
metadata:
  version: "0.2.0"
  local-adaptation: "af-core-framework-coordination-v2"
  short-description: "AF development and responsibility-based refactoring"
---

# AnimusForge Maintainer

Skill version: `0.2.0`

仓库采用本版主源；主体/内部接口/公开 API 工作另读[框架协调](references/framework-coordination.md)。该定制不改变双版本或日常任务边界。

用于 AF 日常开发、维护和按需重构。只处理当前任务，不把普通修复变成全项目重构、完整插件平台建设或历史债务审计。

## 确认任务与工作区

用用户明确指定的 AF 项目，或 `AnimusForge.csproj`、模块 XML 与 Bootstrap 的组合确认身份；单独的 AF/Forge 名称不够。先核实当前 Git 根、分支、HEAD 和未提交改动，再读当前交接摘要及相关实现。只有存在副本歧义时才扩大调查，见[身份与路径](references/routing-and-identity.md)。显式调用本 Skill 也不授权修改未知副本。

## 不可破坏的 AF 约束

- **现状就是双版本**：一套源码分别构建 Bannerlord 1.3.x / 1.4.x；一个 AnimusForge 模块，XML 只加载 Bootstrap，运行时只选择一个对应实现。保持既有程序集、序列化类型和存档键身份。
- 保留项目一键构建方式；构建、Stage、部署、打包、发布分别授权。保护玩家数据、凭据、许可边界与其他作者改动。
- 游戏对象读取和修改留在所属主线程；后台使用脱离游戏对象的不可变输入，回写重验 owner、generation、目标及来源。
- 三渠道保持同类规则、历史角色、后处理、动作和 AFEF 事实语义；渠道特例须有明确依据。LLM 输出不是执行事实，内部标签不进入可见回复。
- 性能按真实频率、工作项与耗时判断；避免热路径全量扫描、反射、重复计算、无界队列和无效分配，不靠削减功能换取通过。

## 选择需要的参考

| 当前任务 | 阅读入口 |
| --- | --- |
| 普通功能、Bug、UI、配置、资源 | [日常开发](references/mod-development.md) |
| 职责抽取、内部模块、跨域协作 | [架构边界](references/plugin-architecture.md)、[工作包](references/module-and-bridge-workflow.md) |
| 目录整理、资源归属、数据或产物清理 | [仓库结构](references/repository-structure.md) |
| TaleWorlds / Harmony / 双版本 / Bootstrap | [兼容性](references/bannerlord-compatibility.md) |
| 信使、自由对话、喊话、Prompt、标签 | [交互链路](references/interaction-pipeline.md) |
| LLM HTTP、SSE、取消/超时、模型目录、TTS 传输 | [LLM 传输边界](references/llm-transport.md) |
| 存档、玩家数据、配置持久化 | [持久化](references/persistence-and-user-data.md) |
| Tick、异步、线程、生命周期、诊断 | [运行安全](references/runtime-safety.md) |
| 测试与验收 | [风险与证据](references/validation.md) |
| 原计划对照或重构复核 | [复核方法](references/refactor-review-checklist.md) |
| 明确要求债务盘点 | [债务识别](references/known-debt.md) |
| 进度记录与接续 | [台账与交接](references/ledger-and-handoff.md) |
| Skill 安装、同步或发现问题 | [维护与宿主](references/host-compatibility.md) |

## 完成当前任务

读真实入口、消费者和状态归属，确定应保持行为、获准变化及验收条件；修改一个完整职责单元，验证受影响边界，再记录结果与未验证项。职责抽取要实际迁移算法/状态并接通消费者，不能用目录、转发壳或测试数量代替完成。

以**有限责任包**推进连续重构：开工时给当前包定义少量可执行退出门（真实 owner、实际消费者、关键风险证据、最终构建/契约门禁）。退出门全部通过就记录保留项并进入下一包，不因行数、文件数、注释、历史猜想或“还能再补一个测试”继续滞留。只在新的运行证据、相关失败或本包源码变化暴露具体风险时追加门槛；优先复用有效测试，避免为每个小方法复制 harness、源码逆变换或整套构建。快速不等于降级断言，完整不等于无限扩张范围。

无关历史 HOLD 不阻塞当前已确认安全的工作，相关数据/兼容/发布条件也不能被局部成功豁免。遵守现有授权连续完成同一工作包，不在每个机械步骤后重开审批；新的破坏性、外部写入或范围变化仍需确认。

Skill 只保存稳定方法，当前代码与修订绑定证据说明实际实现。历史记录不自动成为待办；文档更新不等于产品缺口已修复。
