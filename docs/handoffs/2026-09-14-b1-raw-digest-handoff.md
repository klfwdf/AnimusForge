# AF 完整来源摘要降成本 HANDOFF（2026-09-14）

## 当前结论

**阶段8 / B1仍VERIFY，未整批合格；本轮完整raw摘要编码已通过影响面离线联验。** 自动化af-7-8继续每小时ACTIVE，不进入B2。生产/测试`40b92e671f41beba14e5d4d876195961835dac98`，检查点`9076ab95`，前生产`8bcde78b`。未推送、部署、改默认或操作真实存档。

- 唯一写入：`G:/AFMOD/AF-REFACTOR`；分支`codex/af-framework-skill-delivery-20260911`。
- 本轮fresh fetch：目标远端`origin/codex/af-main-refactor-continuation-20260831`仍`3f00fefa`，未合并/重置其他作者工作。
- 上轮[队列排序](2026-09-14-b1-queue-sort-handoff.md)与[Campaign共享窗口](2026-09-14-b1-campaign-budget-handoff.md)继续有效，不重做它们。

## 改了什么，为什么不会靠漏查换性能

| 责任 | 本轮变化与保持项 |
|---|---|
| 三类完整raw来源 | Capture/重试/最终IsCurrent沿用同一读取和owner/generation/队列/动态context门禁；把JSON反射序列化改为完整字段编码后SHA256 |
| 独立组件 | `MemorySourceFingerprintWriter`负责固定4096-byte缓冲、长度/空值编码、UTF16码元、SHA及dispose生命周期；真实消费者不是空接口 |
| 私有数据边界 | 10种原private DTO、122字段由owner侧映射，字段/数组/列表顺序、null元素、null/empty及state absent/present全部进入摘要。数据模型与存档身份不搬迁 |
| 格式范围 | 仅瞬时`MemorySummaryInput.SourceFingerprint`换编码；无持久化/外部API格式迁移。计划job、编辑器和context的`ComputeMemorySummaryFingerprint`仍原JSON算法 |
| 字符可靠性 | 旧UTF8 replacement fallback可把不同未配对UTF16代理项算成相同摘要；新raw编码保留每个码元，加入有效反例。不是宣称所有旧JSON指纹问题都已修复 |
| 原功能 | 未改原Clone、六个Build、Parse、Apply/Mark、Prompt、provider或业务存档；真实末端来源/成功/失败接受与过期拒绝继续回归 |

新增字段时要同步映射并跑反射驱动的全字段修改测试；目前294次递归字段修改全部能拒绝旧来源。规则不冻结主体功能/Prompt/配置，数据编码也不能凭新增字段后只刷新hash“通过”。没有用只在Save更新的revision代替真实来源。

拆分实情：`MyBehavior.cs`未改；Input净减2行，owner侧私有数据编码新增228行，独立runtime72行。减少的是重复序列化成本，**不能据此说主体大类已整体拆薄**。

## 代码位置（绑定40b92e671f41beba14e5d4d876195961835dac98，一基行号）

| 路径 | 行号 / 符号 | 覆盖责任 |
|---|---|---|
| `MyBehavior.MemorySummaryInput.cs` | 115–125 `MemorySummarySourceView`；127–161 `ReadMemorySummarySource` | 去掉匿名Identity临时对象，用原job/数据引用与state-presence构成主线程瞬时view；不留给异步 |
| 同上 | 250–317 `CaptureMemorySummaryInput` | 261行初始raw摘要接入；其余复制/有效context/原Prompt和最终绑定保持 |
| 同上 | 336–353 `IsMemorySummaryInputCurrent` | 349行最终完整raw摘要；原owner、generation、队列与动态依赖门禁保留 |
| 同上 | 321–334 `ComputeMemorySummaryFingerprint` | **原样保留**给context/plan/editor，不把新raw格式扩散过去 |
| `MyBehavior.MemorySourceFingerprint.cs` | 12–43 `ComputeMemorySummarySourceFingerprint`；47–227各`WriteMemorySource*` | private DTO的122字段与根类型/state存在性，完整可变图编码 |
| `Refactor/Runtime/MemorySourceFingerprintWriter.cs` | 13–72 `MemorySourceFingerprintWriter` | 强类型标量/列表、全部UTF16码元、定长缓冲、完整多块SHA与释放 |
| `tools/MemorySummaryMainThreadBoundaryTests/` | `run_captured.py / CapturedHarness.cs.txt`、`run_fingerprint.py` | 全字段、原版/故障、成本及独立编码oracle；实际组件进入captured/terminal/sealing |

[71点代码图](../architecture/af-framework-code-map.json)分别校验记录提交和工作树；只是定位，不是全文件DONE。

## 量测与验证

每类1000条记录、重复12次摘要，先warmup；这里是受控数据的分配量，不是游戏帧率/最大存档保证：

| 来源 | 旧JSON分配bytes | 新raw分配bytes | 减少 |
| daily | 2,578,792 | 97,960 | 96.2% |
| major | 1,903,048 | 62,248 | 96.73% |
| overview | 5,074,600 | 98,824 | 98.05% |

完整1000行初捕获仍有约1.42MB分配（包括复制/Prompt等），重复来源检查约32,920 bytes/次，实测仍有数毫秒原子工作。**不是深记录分片，也不是单帧硬预算已达成。**

| 检查 | 本轮实际结果 |
|---|---|
| Capture/Execute/来源 | 116/0；原8bc同116例112绿4红（3成本门槛+1码元区分）；294递归字段修改覆盖 |
| Captured故障 | 原28+新增7，35个全部BUILD_PASS后EXIT=1。原版/工具/编译失败严格分开 |
| 字节编码独立oracle | 9个BinaryWriter/MemoryStream/SHA256向量，跨4096边界、负数/long高位、null/UTF16；checked arithmetic；5个finish/dispose/列表变动拒绝守卫 |
| 重试/释放观察 | 7/0；实际三类初捕获/重试/最终检查，不重建Prompt、不丢完成后的大payload释放。JSON与raw计数分栏 |
| 相邻主业务 | sealing60、business36、planning24、writers238、terminal85、commit51、admission54、materials23全通过 |
| 真实末端故障/历史Input | terminal旧e776 Input 55绿30红；忽略parse-source为54绿31红，忽略final-source为69绿16红，均有效runtime反例 |
| 历史/界面/渠道 | helper32/history852/Native history27/failure UI85/Native preparation589/ChannelCutover132通过 |
| 精确差异 | 主文件58声明/4新增span/2删除仍精确恢复90201155；Input仅4具名声明反换回完整8bc源，其他generic JSON/async/parse/release未改；8组件锁、11项守卫通过 |
| 构建/API/身份 | 原脚本-Stage：Debug/Release×1.3/1.4/Bootstrap六项通过且6份DLL与项目Stage hash一致；API119+256并发、预期CS0122、实际4 DLL元数据532；146 SyncData键/36 behaviors保持 |

[验收JSON](../audits/2026-09-14-b1-raw-digest-verification.json)包含命令/返回码、源hash、产物、原始量测、反例及223份冻结日志/manifest的hash。日志位于`.tmp/b1-raw-digest-20260914/final-evidence/`，只本地；仓库源码/runner可以复现，不需要上传大日志。

门禁没有绕过：更新审查表的小脚本首次遇到历史证据键的正反斜杠风格差异，在写入前停止；inverse与failure-UI因此如实拒绝未更新的来源锁。修正**脚本精确路径识别**并完成具名审查后，11/0与85/0复跑通过；初次失败日志保留，不冒充生产Bug或业务红例。

## 尚未完成 / 下一轮从哪里继续

1. 继续[原P0–P6计划第16节](../phase8/af-core-precloseout-plan-20260913.md)剩余B1：初次完整capture/复制、owner净化、全部owner绑定、Apply/public/weekly尾步；raw遍历虽显著降成本仍原子O(N)。本轮也没有解决任意长单字符串、全部存档峰值内存或硬帧时。
2. 不重做已完成素材索引、Campaign共享窗口、排序、typed raw摘要；如要用revision替代最后完整校验，先覆盖所有实际writer（包含Save外、原地净化、嵌套修改、queue/state和跨owner迁移），保留源变动/换档/部分失败的拒绝语义。
3. B1整批关键风险闭合或明确接受、同候选门禁过后才进B2：Courier后台live读取和完整三渠道；再B3内部接缝与已选择的public能力。D-A/D-B没确认不自动开放public提交/取消/写入。
4. Campaign/Mission实机、旧档、真实经济/AFEF、TTS/provider、外部DLL实际加载仍NOT_RUN。不是“只剩实机”，也不是阶段8DONE。内外接口层和制作组玩法边界不变。

仍有已授权可独立做的B1工作，所以自动化保持ACTIVE；达到获准收尾评审或确实只剩外部阻塞时暂停并留两份交接，未来用户暂停优先。不自动推送、部署或改默认。

## 回滚与制作组简明版

- 定向revert生产/测试`40b92e67`及随后本轮文档提交；`9076ab95`为实改前检查点。保留之后的协作改动，不hard reset/force。
- 本地简明版：`G:/AFMOD/AF-REFACTOR/.tmp/af-core-precloseout-team-handoff.md`。两份2026-09-06用户草稿和指定旧Native简明版均保持原hash，未暂存/上传。
