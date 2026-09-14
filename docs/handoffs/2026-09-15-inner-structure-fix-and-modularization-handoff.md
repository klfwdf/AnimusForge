# AF 内层记忆覆盖修复与真正模块化计划 HANDOFF（2026-09-15）

## 结论

**本轮已修复上次检查复现的内层同数量变动覆盖问题，并完成影响面离线验证；整体仍阶段8/B1，未整批验收。** 同时制定了到收尾评审前的真正职责拆分计划；规划内容尚未全部实施，不声称主体已拆干净或零Bug。

- 修复生产/测试：`9617f96af8d821bf6317db0e43b8b1fbf1ee7a4d`。
- 本轮拉取起点：`af618912`；修复前生产：`4d6994bc`；意图检查点：`b467179b`。
- 工作区 `G:/AFMOD/AF-REFACTOR`，本地分支 `codex/af-framework-skill-delivery-20260911`，未来获准推送目标仍为 `origin/codex/af-main-refactor-continuation-20260831`。
- **仅本地提交，未推送。自动化仍PAUSED；未部署、操作存档、改默认或重写制作组业务。**

## 1. 修了什么

原缺口：分段净化已处理一部分lines/trigger后，让出到下一窗口；只检查列表引用与Count，识别不了64→64的槽位替换、删补和换位。恢复后缓存的旧line结果会覆盖新事实，trigger可能漏绑定正确身份/日期。

修复：在现有 `DailyMemoryDraftEntryNormalization` 上给lines和预净化trigger输入保存 `List<T>.Enumerator`，以结构版本做O(1)检查，即使探针枚举结束仍能发现结构变化。发现变化沿已有Invalidated→重新封存/索引路径恢复，不吞错误、不删除新事实、不加第二套净化算法。

触发器整表sanitize会合法替换其列表，完成该阶段后不再检查旧输入列表的结构探针；当前发布列表的引用检查仍保留。line最终发布后状态立即Done，原先永远不会再执行的 `_linesPublished` / `_publishedLines` 及死检查分支已移除。

**保持：**主线程净化原语义、同步入口、先占key的去重规则、稳定顺序、引用/别名、唯一owner、存档身份、实际metadata预算。**限制：**结构版本不是元素字段版本，不使集合线程安全；trigger整表sanitize、长字符串、初capture/raw/最终绑定/Apply等仍有原子成本。

## 2. 源码位置与责任（9617f96a，一基行号）

| 位置 | 符号 / 责任 | 未覆盖 |
|---|---|---|
| `MyBehavior.MemorySealing.cs:219–280` | `DailyMemoryDraftEntryNormalization`：lines/trigger结构探针字段和输入绑定，229–230、277–278为新增点 | 不迁移持久化DTO、不复制一套同步规则 |
| 同文件 `337–379` | `InnerCurrent`：原身份/引用/count检查 + 两个List结构版本检查；变化令当前工作失效 | 不判断任意in-place字段编辑，也不承诺并发writer同步 |
| 同文件 `650–742` | `RunDailyMemorySealDrafts`：真实消费者；沿现有Invalidated分支重走owner封存/索引后再发布 | 不是整owner事务，不回滚此前元数据净化 |
| `MyBehavior.cs:26585–26679` | 原 `BindDailyMemoryDraftWeeklyTrigger` / `SanitizeDailyMemoryDraftLine` / `SanitizeDailyMemoryDraftEntry` 共用规则 | 本轮文件完全未改 |
| `tools/MemorySummaryMainThreadBoundaryTests/SealingHarness.cs.txt` | `OwnerNormalizationInnerStructureChange`：12个两类列表的真实入口场景 | game/owner/provider仍是明示fixture，不是实机 |
| 同目录 `run_sealing.py` | 支持修复前4d来源、逐trigger计数、两个结构故障反例；旧ignore-line-source保持整体移除line保护的含义 | 未把提取/编译失败计为有效红例 |
| 同目录 `test_source_parity.py` / `source-review-b1.json` | whole-component与测试输入锁、针对4d的精确窄差异回放，共13项守卫 | 精确差异不是全项目功能等价证明 |

[78点代码图](../architecture/af-framework-code-map.json)记录源码commit/符号/行号，不能当整文件已重构白名单。标准元数据计费仍128，草稿身份8；1024行fixture仍9窗、最大127行，本轮没有通过放大额度修Bug。

## 3. 本轮实际验证

[验收JSON](../audits/2026-09-15-inner-structure-fix-verification.json)包含源hash、日志、94份冻结材料hash与六份DLL/marker/Stage比对。原始日志和生成测试输入只在本地 `.tmp/inner-structure-fix-20260915/final-evidence/`，不上传游戏产物。

| 检查 | 实际结果 |
|---|---|
| 当前封存 | **88/0**，含原76和新增12个line/trigger场景 |
| 修复前4d，同88例 | **80/8**，BUILD_PASS后同数量变动断言失败 |
| 正反例 | 两类unchanged/append对照通过；slot、remove-add、swap、finalize-slot在旧版各失败，修复后全通过 |
| 两个新故障控制 | ignore-line-structure、ignore-trigger-structure均84/4；BUILD_PASS后EXIT=1 |
| 相邻旧故障控制 | ignore-line-source80/8；现在整体移除line引用/count与结构保护，不只删一半逻辑造成假绿 |
| 相邻回归 | captured116/0、business36/0、terminal85/0 |
| 源码守卫/导航 | 13/0；地图记录版/工作树78点校验 |
| 原脚本构建 | **Debug/Release × 1.3/1.4/Bootstrap六项Stage成功**，项目内6份产物/marker/Stage哈希一致 |
| API/实际DLL | API119、并发读256、预期外部访问internal的CS0122；4份实现DLL元数据532断言 |
| 存档身份 | 对4d，146 SyncData keys / 36 behaviors不变；模块仍AnimusForge，仅Bootstrap加载 |

第一次Stage误选系统runtime-only `dotnet`，报“No .NET SDKs were found”。仅给该进程PATH/DOTNET_ROOT指定现有G盘SDK后两配置成功；失败日志已保留，未安装工具或修改原构建脚本。

历史其他mutation与无关全量suite没有全重跑。旧记录保留其绑定版本，不冒称本轮全测。**未实机测试Campaign/Mission、旧档加载/往返、真实资产/AFEF、TTS/provider、独立子MOD加载。**

### 复跑关键命令（仓库根）

```powershell
$env:DOTNET_EXE = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --source-baseline 4d6994bc
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate ignore-line-structure
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py --mutate ignore-trigger-structure
& G:\Python310\python.exe -X utf8 -B tools/MemorySummaryMainThreadBoundaryTests/test_source_parity.py
```

旧版/故障反例必须是BUILD_PASS后的预期断言失败；环境或编译失败不能算红例。

## 4. 玩家视角与覆盖边界

离线直接执行了真实抽取 `TrySealPastDailyMemoryDrafts` 调用链：64条内容处理一部分后让出，保持列表数量做替换/删补/换位，再继续；修复后整对象图与原同步规则处理最新来源一致，新事实、顺序和trigger绑定不丢。

这不是游戏操作录像；未证明所有编辑器/存档writer都能触发原Bug。普通 `SaveDailyMemoryDraftsById` 会更换外层owner列表，可能本来就被外层守卫拦住。不能说所有玩家编辑都曾丢失，也不能把用户遇到的所有问题归因于这一处。

将来实机至少验证：长历史维护期间正常对话/AFEF写入、编辑/导入和存档切换，核对新内容保留、顺序/身份、无重复事实和旧回包写入；只能用获准测试副本，不自行覆盖玩家存档。

## 5. 接下来如何真正拆干净

新[完整职责拆分计划](../phase8/af-core-responsibility-decomposition-plan-20260915.md)细化原P0–P6/B1–B3，原计划第18节作为最新入口。包含14类职责包、源头/目标owner、真实调用迁移、旧符号删除条件与收尾门禁。

1. **B1 / Memory**：首次capture/copy、writer与最终接受/预算闭合；同时提取有独立状态和真实消费者的来源/维护/历史事实owner。
2. **B2 / 完整对话核心**：先Courier线程，再Prompt/规则、ActionPlan/回执、回合编排；Native/Scene/Courier退为渠道适配，保留接力/旁听/流式/到达语义。
3. **B3 / 剩余大类及内部模块**：编辑器、导入导出、存档兼容适配与展示责任迁出；内部生命周期/异常/组合接线不变成制作组业务重写。
4. **P4**：按明确选择实现版本化公共能力，不假装当前只读V1已是完整SDK。
5. **P5/P6**：同候选功能/性能/双版本/旧档/实机/组合验收，逐符号清理/兼容保留表和回滚，达READY_FOR_CLOSEOUT_REVIEW后才决定最终动作。

最重要的完成条件：**迁移实际职责 + 所有真实消费者切到新owner + 等价/异常验证 + 删除已被替代的旧算法**。partial、空接口、包装旧大类或单纯行数下降一律不算完成。保留必要的引擎/存档ABI薄壳，不为“全删”破坏兼容。

当前仍不是只剩实机；不要直接执行af618912旧检查点里仅ref/count的捕获方案，新快照也要覆盖实际结构/字段变化，不重复本次错误。

## 6. 回滚和交付

- 定向逆转本轮 `9617f96a` 修复及相关测试/地图即可退回前生产；先核对后续依赖，完整回滚后恢复旧8项红例是已知事实。不hard reset、不强推。
- 本轮只有MemorySealing生产文件净+8行；两个死发布标志/相应分支已删除。MyBehavior/渠道/Prompt/领域玩法未改，不能把未来14个职责包算已完成。
- 两份用户草稿和指定本地Native简明版保持原hash、未暂存。本地成员说明 `.tmp/AF修复与模块化计划简明交接-20260915.md` 不上传。
- 自动化仍暂停，代码/计划仅本地提交。后续执行按最新用户指示；推送、游戏部署和最终切默认不从历史授权推断。
