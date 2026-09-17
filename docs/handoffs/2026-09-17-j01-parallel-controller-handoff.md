# J01 完成后的并行总控交接（2026-09-17）

这是交接，不是第二份总计划。唯一台账仍是 [原计划](../animusforge-refactoring-and-repository-reorganization-plan.md#parallel-controller-handover)。

## 最新请求与调度方式

用户要求先写交接，再开新任务接管总控，按工作包调度：**3 个 Sol 实施代理 + 1 个核验代理**，合计 4 个子代理，Astra 负责计划、整合与最终验收。本旧任务已经按要求停在 J01，不启动 J02。

- 新任务沿用 **GPT-6 Astra / medium** 总控；实施代理使用 `collaboration`、`agent_type=worker`、`model=gpt-5.6-sol`。核验代理同用 Sol，负责独立证据审查，默认只读、不代写生产。不要用固定模型的 reviewer 角色冒充 Sol。
- 总控按风险选择思考深度：机械任务 medium，跨文件/状态/兼容 high，确有疑难才 xhigh/max；模型覆盖使用 `fork_turns=none` 或有限历史，附自包含任务单。
- 一轮最多 3 个互不重叠的实施包，而非把 J02/J03/J04 无视依赖同时启动。先定 owner、文件/符号白名单、依赖和退出条件；同一大文件不能有两个同时写入者。依赖未齐时减少并发，空余代理可做下一包只读准备。
- 一包从实现、接线到聚焦测试连续完成，不再每个小步骤停下让总控重新批准。出现真实来源、数据、范围或验证问题才暂停对应包；不重复询问已经取得的同项许可。
- **总控独占 Git 索引、提交、共享 Stage/bin/obj、集成地图与两入口文档**。实施代理只改所属文件，返回 diff/测试/风险；核验代理不与实施者同时改同一文件。
- 独立测试输出各用唯一目录；共享原 runner 的固定 `.generated` 或完整构建必须串行调度。优先聚焦测试，集成后集中跑必须的双版本矩阵；源码/依赖不变且证据完整时不重复造基线。
- 核验代理并行审调用者/失败反例/兼容性，总控再看真实 diff 与日志、做最小独立复验，不要求机械再跑三套相同矩阵。核验通过不等于实机/旧存档通过。

## 接管基线与保护项

- 当前工作区：`E:/AnimusForge-refactor-continuation-20260831`，同一保存目录，不换 worktree；分支 `codex/af-main-refactor-continuation-20260831`。
- 本交接写入前 HEAD：`4f7081a71de1e414b92d4e418a767617b0334c6e`；J01 生产源码修订：`02f1747c4e226d9c8e187f2503c6197ed6148156`。启动时重新读实际 Git，不将此快照当新 HEAD。
- 既有六份 dirty：`AGENTS.md`、`HANDOFF.md`、原台账、`.agents/skills/af-core-framework/SKILL.md`、`.claude/skills/animusforge-maintainer/references/framework-coordination.md`、同目录 `repository-structure.md`。交接前索引为空；不要整份暂存或覆盖这些文件。
- **禁止推送/发布本分支**：`ae8e6b89` 曾误纳既有本地文档，`3a57007d` 已用 focused inverse 修复净差异、保留工作树，但材料仍留在祖先历史。不能把最终树已恢复当历史已清除；禁止 reset/rebase/强推。未来发布需另获授权并按交付规则从安全祖先隔离。
- 禁止 `git commit -- <paths>`：它会按工作树文件提交，绕过精确暂存 hunk。总控先审 `git diff --cached`，再无路径 `git commit -m ...`。
- 不动其他工作区/游戏目录/真实存档/源 PlayerExports，不装软件、不改全局配置/一键入口/CI/CD、不恢复自动化。Stage/日志可能含私密副本，不上传或输出玩家文本/密钥。

## J01 已交付，不重做

以下坐标均核实于生产修订 `02f1747c`：

| 路径 / 一基范围 | 已完成责任 | 仍未覆盖 |
| --- | --- | --- |
| `src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs:9-235` | 8 方法/4 常量真实提取，13 个旧宿主调用直连；无共享可变状态 | 网络、SSE、取消/重试调度 |
| `src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs:1-720` | API URL/payload/认证头/响应解析原字节归位，public/namespace 不变 | 真实供应商网络兼容 |
| `src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs:1-485`，`StreamFilter:62-144` | envelope 与每流状态原字节归位 | 既有单字符 Unicode 流发射缺陷未修 |
| `ShoutNetwork.cs:133-159,665-904,906-1365` | 原发送/主调用仍保留，只删除协议算法并重接 13 处 | 仍为混合宿主，不能标整类或三渠道完成 |

- 原 B0、五文件 B1 已完成；J01 状态 **OFFLINE_VERIFIED**。提取提交 `156e6836`，overlay 清单修复 `0e6be296`，两文件迁移 `02f1747c`，最终地图/交接 `4f7081a7`。
- 协议 13 用例、7 个编译成功后的指定变异拒绝；Courier 39/8 变异及单测 8，LegacyShout 单测 3；两 API 完整 Compile 754、7 资源；Debug/Release 各 1.3/1.4/Bootstrap+Stage；API 四实际 DLL/1056 元数据，Composition 42/5 反例，Native 正常/重排各 41/8 反例；地图两模式 163 锚点通过。准确命令/退出码见原台账，不用数字代替新候选证据。
- 证据：`artifacts/workspace-j01-llm-protocol/{before,after}/`、`artifacts/tests/llm-protocol/j01cd_*`。六产物/Stage SHA 一致；overlay 修复后的 297 文件/类别仅按 Compat 路径映射变化。
- 已知原缺陷：`{"reply":"a\\u4F60\\nq"}` 逐字符流累计发射可能为 `a4F6`，最终 NormalizedText 正确；已经锁定原行为，不趁迁移修复。LIVE、旧 SAVE、真实 provider 均 NOT-RUN。

## 必须继承的门禁与下一轮入口

1. 读根 `AGENTS.md`、维护 Skill、framework Skill/coordination、原台账 G0/J 路线、当前 HANDOFF。Git 实况与最新用户指令优先于旧 ACTIVE/STOPPED 快照。
2. **新任务调度授权不等于 G0.7 自动 CLOSED。** G0.2–G0.5 仍有分类/数据/来源/产物 HOLD，J02 等广泛提取须满足原门禁；J02–J17 目前只是路线，不能按目录名批量移动。不要将新开任务当对未知递归清理、资产搬迁或外部写入的批准。
3. 高效首轮建议用 3 个有界并行只读包产出可执行门禁证据：A 核对 tracked/untracked/ignored 元数据分类与剩余 owner；B 核对实际引用来源/许可、数据/产物 HOLD 的准确范围；C 沿现有 J 路线核对下一实施波次的符号/状态/消费者与文件冲突。第四代理交叉核验其结论；不要读玩家文本、递归清理或写独立总计划。
4. 总控将结果集中回写原台账，区分可立即执行的既有许可与真正缺失的高风险许可；门禁闭合且工作包精确后，使用三实施包+核验的并发波次推进。不得用无限只读盘点代替实施，也不得通过放宽门禁假装提速。
5. 当前交接只改变后续调度方式，不宣称新包已实施。新总控完成接管后自主选择依赖齐备且收益明确的包，不要求用户逐小步说“继续”。

## 环境与失败经验

- 固定 SDK：`local/dotnet/8.0.425/dotnet.exe`；测试 Newtonsoft：其 `sdk/8.0.425/Newtonsoft.Json.dll`。Python 用 `python -X utf8 -B`；原 Stage 参数/六个限界输出根见原台账 J5/P9.5，不改原脚本。
- 当前 worktree Git 元数据位于主仓库目录，索引写入可能需要工具权限提升；这是本地提交权限，不是切换工作区或写主仓库源码的许可。
- Windows SDK 沙箱访问拒绝须诊断权限，不篡改工程。原 Stage 返回后 `$LASTEXITCODE` 可能残留 robocopy 成功码 1–7；记录脚本真实成功状态/throw/命令退出，不能误判，也不能仅凭日志单词覆盖失败。
- 1.4 manifest 的旧 projectSha256 对应 B0 BOM+混合换行字节；已精确重建确认，不能随意刷新 hash。依赖/源码变化才重新建立对应基线。
