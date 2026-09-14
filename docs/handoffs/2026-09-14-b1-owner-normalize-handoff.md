# AF owner草稿逐记录净化 HANDOFF（2026-09-14）

## 一句话结论

**阶段8 / B1继续VERIFY，未整批合格；本轮owner净化记录预算已完成影响面离线联验。** 生产/测试`86805518259ee44e3924da787bfba97654208074`，检查点`7c1da946`，前生产`40b92e67`。自动化af-7-8仍每小时ACTIVE，不进入B2；未推送、部署、改默认或操作真实存档。

唯一写入`G:/AFMOD/AF-REFACTOR`；分支`codex/af-framework-skill-delivery-20260911`；fresh fetch远端`origin/codex/af-main-refactor-continuation-20260831`仍`3f00fefa`。

## 实际修改

| 责任 | 当前结果 |
|---|---|
| owner封存末尾净化 | 不再拿一个昂贵任务额度净化整个owner。逐draft消耗实际共享授予，257/65记录的owner由一次全净化改为每个有限窗口最多8条 |
| 原规则 | 单条entry体从旧Sanitize精确提取；主线程原地/后台clone、先占key再检查empty、标签/AFEF/marker与别名副作用顺序不变 |
| 来源与删减安全 | normalized结果保持私有；检查实际owner列表引用/版本、各条key/日期和empty/include状态。旧empty决定不能删掉后来长出的新lines |
| 改key后恢复 | 重新走该owner封存与live索引，不只是重新排序；防止日期变后原job失效但新job没建 |
| 排序 | 复用已验证稳定排序，day-only并传constant-name，不新增HeroName排序规则 |
| 空owner | 只扣metadata并移除空项，不再创建原deep sanitizer的空集合与排序对象 |
| 保留入口 | 原同步Sanitize仍服务现有读/存档调用；它和cooperative路径共用同一entry规则，未留下复制的第二套规则 |

**有意时序变化：** 元数据按每个draft的原子净化操作提前可见；整列表去重/删除/排序仅验证后发布。不是整个owner事务，不承诺回滚已发生的元数据净化；规范化UTC元数据的时间自然随记录处理时间变化。

实际体量：MyBehavior主文件净+3行，封存partial净+100行；复用独立排序组件，没有新增空接口/假模块。不把增加状态机包装为“大类整体已拆薄完成”。

## 代码位置（86805518259ee44e3924da787bfba97654208074，一基行号）

| 路径 | 行号 / 符号 | 责任 |
|---|---|---|
| `MyBehavior.cs` | 26573–26583 `SanitizeDailyMemoryDrafts` | 保留同步入口与原day稳定顺序 |
| 同上 | 26585–26655 `SanitizeDailyMemoryDraftEntry` | 原内层净化体；只把continue/list.Add转成返回值，规则/字段不变 |
| `MyBehavior.MemorySealing.cs` | 127–199 `DailyMemoryDraftNormalization` | per-record游标/seen/绑定/私有结果/排序；发布前读取实际owner，而非只检查入口旧变量 |
| 同上 | 469–561 `RunDailyMemorySealDrafts` | 真实消费方；空owner快路，失效重走封存与索引，完成才发布 |
| `Refactor/Runtime/CooperativeMemoryQueueSort.cs` | `Step` | 原独立排序组件复用，本轮文件未改 |
| `tools/MemorySummaryMainThreadBoundaryTests/` | `run_sealing.py / SealingHarness.cs.txt / test_source_parity.py` | 原40b sanitizer oracle、真实记录/line计数、并发与原体精确还原 |

[74点定位图](../architecture/af-framework-code-map.json)通过记录提交与工作树双校验；不是整文件完成白名单。

## 验证结果

[验收JSON](../audits/2026-09-14-b1-owner-normalize-verification.json)记录命令、返回码、hash、6份产物/Stage比对与183份冻结日志/manifest。日志仅在`.tmp/b1-owner-normalize-20260914/final-evidence/`，源码与runner可复现。

| 检查 | 本轮结果 |
|---|---|
| 封存/owner实际入口 | 75/0；旧40b相同75例62绿13红，BUILD_PASS后断言差异 |
| 故障反例 | 原20+新6共26个均BUILD_PASS/EXIT=1；包含不限额、忽略key/empty/keep-empty/当前列表、不重建封存，以及此前队列/预算反例 |
| 工作量 | 257记录owner85窗口，65记录实际Campaign33窗口，最多8记录/窗口，稳定源只净化一次；窗口数是观察值不是协议 |
| 原语义 | 同步oracle核对输出及原输入图（包括输掉去重的对象、共享line/trigger）；原引用、empty-first保留行为保持 |
| 中途变更 | 追加/替换/同slot、key/日期变更、empty长新lines、清空lines、reset、异常、发布时替换owner通过；过期清理不覆盖新事实 |
| 相邻回归 | captured116、旧8bc112/4、两个clone故障113/3；business36/planning24/writers238/terminal85/commit51/admission54/materials23；raw writer9向量/5守卫通过 |
| 历史/UI/渠道 | helper32/history852/Native history27/failure UI85/Native preparation589/ChannelCutover132通过 |
| 源码守卫 | 主文件58声明/5新增span/2删除精确回到90201155；Input4声明精确回8bc；8组件锁、12项守卫，新增单entry原40b体精确反拼 |
| 构建/ABI/存档 | 原脚本-Stage，Debug/Release×1.3/1.4/Bootstrap六项通过；6份DLL/marker与项目Stage一致；API119+256并发、预期CS0122、4 DLL元数据532；146 SyncData键/36 behaviors保持 |

反例语义：旧版13红包含新要求的可续跑时序/额度见证，不说成13个原游戏Bug。保留旧资料的35个raw控制时明确绑定40b，本轮只重跑受影响的两个clone控制和116正常场景；Input/编码器源未改，单entry体精确同旧，不冒称全35个重跑。

测试纪律：原`ignore-sort-source`使用的guard文字在新owner类里也出现，旧精确selector拒绝匹配（工具失败，未当业务红例）。已限定到原QueueTail类并实际重跑，最终72绿3红；未扩大fault到两个类凑红。新owner-sort与queue-sort计数分栏，避免老队列用例误停在owner排序。

## 未完成与下一步

- **一条draft里1024行仍一次处理。** 本轮专门记录1 draft/1024 line真实操作，未伪装成8行限额；触发器/长字符串/单draft文本、key/empty最终绑定和全owner绑定仍原子。
- 继续[原计划第16节](../phase8/af-core-precloseout-plan-20260913.md)的首次capture/复制、深line/trigger净化、全owner与最终来源绑定、Apply/public/weekly尾步预算。raw摘要虽降成本仍O(N)。不重做索引、共享窗口、排序、typed raw或本轮owner记录额度。
- 若要用revision替代完整检查，先证所有实际writer覆盖，包含Save外、嵌套修改、原地净化、queue/state与跨owner迁移；不能为了预算删raw验证或历史数据。
- B1整批风险关闭/明确接受并完成同候选门槛后，才进B2 Courier后台live读取及完整三渠道，再B3内部接缝与已确认public能力。D-A/D-B未确认不自行开放public提交/取消/写入。
- Campaign/Mission实机、旧档、真实资产/AFEF、TTS/provider及外部DLL加载仍NOT_RUN，不把fixture/Stage/元数据当实机；阶段8未DONE，也不是只剩实机。

还有获准可独立做的B1工作，所以自动化保持每小时ACTIVE；达到获准收尾评审或确实只剩外部阻塞后暂停，未来用户暂停优先。制作组玩法、内外接口分层与唯一权威提交边界不变。

## 回滚 / 简明版

- 定向revert生产/测试`86805518`及随后本轮文档提交；`7c1da946`是实改前检查点。先确认后续依赖，不hard reset/force或覆盖他人修改。
- 本地制作组简明版：`G:/AFMOD/AF-REFACTOR/.tmp/af-core-precloseout-team-handoff.md`。
- 两份2026-09-06用户草稿和指定旧Native简明版保持原hash、未暂存上传。本轮没有推送或游戏部署。
