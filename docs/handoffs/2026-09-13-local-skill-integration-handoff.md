# AF 两套 Skill 本地融合与冲突核对（2026-09-13）

## 本轮范围 / 已完成（仅本地）

用户要求列表介绍 ZIP Skill、与已有框架 Skill 核对冲突并融入本地。本轮只更新仓库内 `.claude/skills/animusforge-maintainer/` 的维护 Skill、`.agents/skills/af-core-framework/` 的协调条款，以及根 AGENTS/HANDOFF 入口；不安装全局、不改生产、不推送、不恢复自动化。

- 本地起点：`e40c92d72524b7ea80a5dc0e36dc996963a62e66`；工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-framework-skill-delivery-20260911`。
- 已抓取远端：`bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8`。前轮只 fetch，未合并这 4 个远端提交；本次不顺带更新运行源码。
- 输入：`D:\qq\af-skill (1).zip`，上游 Skill `animusforge-maintainer` 版本 `0.1.1`，ZIP SHA256 `ac342dc52157bd3c9402dcd0e1967d75174a2a3e30edd20998e19a1b1040d163`。原 ZIP 只读保留，不执行安装脚本。
- 原本地维护 Skill 已受 Git 跟踪；本检查点保存融合前状态。两份 2026-09-06 用户草稿和仅本地简明 HANDOFF 保持原样。
- 验证：Skill YAML/格式、链接、附带脚本语法与只读校验、路由场景、协调条款审读，以及生产源码和用户文件未变化。宿主新会话实际发现不作未执行的保证。

## Skill 功能列表与协调结果

| 项目 | ZIP 维护 Skill | 已有框架 Skill | 结论 / 本次处理 |
| --- | --- | --- | --- |
| 日常功能、Bug、UI、资源、配置 | 通用开发维护 workflow | 专门负责主体框架 | 不冲突；按实际任务加载，不把所有工作强制变成重构 |
| 架构 | Foundation / Module / Bridge 的逻辑职责 | 同一 DLL 内主体、internal 接口、public API | 不冲突；在入口明确逻辑划分不强制拆 DLL，也不代表平台已完成 |
| 制作组业务范围 | 可承接另行授权的模块业务 | 当前主体任务不重写政策/宴会/GCCZ | 范围差异而非矛盾；最新用户授权和真实 owner 决定，不能自动扩权 |
| “公开能力” | provider 的受支持跨 owner 契约 | internal 制作组 / public 子 MOD 分开 | 主文容易误读；已明确同 DLL 可用 C# internal，不要求一律 public |
| 主体功能可调整 | 允许批准的新功能和行为变化 | 不冻结算法、Prompt、功能上限 | 一致；稳定兼容不等于禁止改进，测试保护未授权变化 |
| 清理和模块门禁 | 全面拆分/新模块有专门验收 | 小改动不制造占位接口 | 一致；明确普通修复/Skill-only 不强制加 manifest 或完成全仓清理 |
| 当前状态入口 | 首选原 ledger | 根 HANDOFF 与实际 Git | 存在误用旧入口风险；统一先判当前状态与明确替代关系，不单靠文件名/旧盘符 |
| 代码位置交接 | 原有路径、变更与证据要求 | 一基行号、符号、源码提交、覆盖范围 | 互补；维护 Skill 补齐精确坐标要求，不冻结未来行号 |
| 主线程/性能 | 真实 job/record/耗时预算、输入快照和结果重验 | 原生命周期/快照边界要求 | 一致并补强；框架 Skill 明确“回调数不等于真实工作量” |
| 安装与移植 | 原文曾写死作者 Mac 源目录 | 先用 Git 定位本机工作区 | 实际移植问题；移除固定源目录指令，保留历史路径示例作为示例 |
| 历史审查 R01–R07 | 绑定 `bd2ed35f` 的审查清单 | 按当前源码判断覆盖 | 历史参考，不是当前本地全部缺陷清单，也不是自动继续开发/删除授权 |
| 验证和发布 | 双版本/存档/Host 证据分层 | 不把离线 PASS 写成整个阶段 DONE | 一致；两者都不授予推送、部署、恢复自动化或全局安装权限 |

## 本地落点与变化

- 更新原有 `.claude/skills/animusforge-maintainer/`，导入 ZIP 0.1.1：原 23 个包文件中 10 个不变、11 个更新、2 个新增。
- 在 0.1.1 上增加明确标注的本地适配 `metadata.local-adaptation = af-core-framework-coordination-v1`；不是伪造上游新版本。原 ZIP 保持只读不变。
- 新增 `references/framework-coordination.md`；维护 Skill 主入口澄清分层/public/状态/按影响面更新文档；host-compatibility 移除固定作者目录；ledger 补代码坐标。
- 原框架 Skill 增加分工与真实工作预算说明；根 AGENTS 明确先用通用 workflow，涉及主体/接口时合用框架 Skill。
- 没有新建第二份完整维护规则树；Codex 由项目 AGENTS 指向同一 `.claude` 仓库副本。它不是用户全局安装，已有会话/另一宿主是否自动重新发现仍须实际验证。
- 没有删除生产旧入口或修改 C#；只替换过时规则表述，实际兼容/存档代码保持不动。

## 位置索引（本地实现提交 `6e5d1785`）

下列是规则/工具位置，不是新增游戏代码；完整提交为 `6e5d178559434f930f822c00cd045daa39eb30ae`。

| 位置 | 作用 |
| --- | --- |
| `.claude/skills/animusforge-maintainer/SKILL.md:14-17`，`Repository integration:` | 通用 Skill 与本地适配入口 |
| `.claude/skills/animusforge-maintainer/references/framework-coordination.md:5-8`，`## 谁管什么` | 任务路由与两套规则的共同解释 |
| `.agents/skills/af-core-framework/SKILL.md:10-13`，`## 与通用维护 Skill 分工` | 主体框架专门约束 |
| `AGENTS.md:3-6`，`## AF skill coordination` | 项目实际读取入口 |
| `.claude/skills/animusforge-maintainer/references/host-compatibility.md:19-22`，`The source is the explicitly selected directory` | 移除硬编码源路径 |
| `.claude/skills/animusforge-maintainer/scripts/verify-af-skill.sh:11-14`，`errors=0` | 结构/YAML/版本/资源链接检查 |
| `.claude/skills/animusforge-maintainer/scripts/suggest-reference-route.sh:20-23`，`strong=0` | 项目识别与任务参考路由 |

## 验证与保留边界

- 两个 Skill 的 bundled `quick_validate.py` 通过。
- 导入的 `verify-af-skill.sh` 经阅读后，在指定项目内临时目录运行，0 error / 0 warning；三份 shell 脚本 `bash -n` 通过。没有执行 `install-af-skill.sh`。
- 5 个路由场景通过：AF 普通功能、AF 主体接口、AF 原计划审查各走对应 reference；无 AF 证据的普通 Bannerlord 和 Minecraft 不自动选 AF。
- 38 个相对文档链接有效；两份用户草稿与仅本地简明文档字节未变；C#、项目/解决方案、ModuleData、一键脚本相对本轮起点无变化。
- 完整日志/来源文件摘要在 `.tmp/skill-integration-20260913/`，只留本地。上述验证不能证明实际宿主已重新发现 Skill，也不能证明任何新游戏功能通过验收。
- 本地工作起点仍是 `e40c92d7` 的运行树（生产代码 `8f1cd479`）；远端已抓取到 `bd2ed35f`，但 4 个远端后续提交未合并。本轮 Skill 提交与这些远端提交并行，不能直接推送或用 force 覆盖；后续用户要求同步时先审查再整合。
- 自动化 `af-7-8` 回读仍为 PAUSED。未推送、未部署、未改全局 Skill、未操作真实存档，阶段 8 状态不因 Skill 融合而改变。
- 修改前检查点 `a29cd47`；需要回滚时经用户指示定向反转本轮实现/交接提交，不 hard reset、不清理用户文件。
