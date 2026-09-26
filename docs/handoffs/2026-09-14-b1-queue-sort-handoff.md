# AF 队列排序与 B1 续建 HANDOFF（2026-09-14）

## 1. 一句话结论

**阶段8 / B1继续VERIFY，未整批合格；本轮Daily/Major封存排序已完成影响面离线联验。** 自动化af-7-8每小时ACTIVE，继续同一计划，不进入B2。生产/测试 `8bcde78b9f477a9cc7cdb8ef12b2a4cbf1e60dc3`，意图检查点 `99a7dbfe`，前生产 `9158132c`。未推送、部署、切默认或操作真实存档。

唯一工作区 `G:/AFMOD/AF-REFACTOR`，本地分支 `codex/af-framework-skill-delivery-20260911`；本轮fresh fetch确认远端 `origin/codex/af-main-refactor-continuation-20260831` 仍 `3f00fefa`。不自动融合分叉。原AF完整对照仍沿用既定原计划，本切片只替换具名排序责任，不代表完整功能复现。

## 2. 做了什么，玩家会得到什么

| 部分 | 当前实现与应保留行为 |
|---|---|
| 两类封存尾部 | 移除这条路径最后一次整队列LINQ排序，接入实际可续跑稳定归并排序组件；不是仅新增partial或空接口 |
| 排序预算 | 每次比较/移动或结果copy扣共享metadata授予；正常Campaign周期跨多个caller累计，不刷新128限额。显式同步仍可排空 |
| 源一致性 | 捕获day/name标量，不在后续排序时读取live字段；发布前核对实际owner列表、版本、所有保留job字段与当前culture。追加/替换/同槽修改、重试/日期变动或culture切换会拒绝旧排序 |
| 功能对照 | 仍先判断pending再净化/去重；保留第一个有效job原引用、相同day/name的原次序、culture比较及原HeroId/日期/重试/错误字段净化顺序 |
| 真实主体边界 | 原权威DTO/存档留MyBehavior；内部排序职责到Refactor/Runtime。两个同步Sanitize仍有消费者并保留，不误删为“旧代码” |
| 有意时序变化 | 规范化后的元数据可能先于最终排序完成可见；排序结果列表保持私有，完成前不会由该尾步启动总结。主线程、不新增worker或第二套提交者 |

玩家视角：大积压不必把**所有排序工作**压在一次维护调用中；排队时人物/任务变化不会被旧排序覆盖。没有删历史、降记忆量或跳过失败任务来换测试通过。**不宣称实机不卡、全功能无BUG。**

主体家族本次增加净化分界和续跑状态（MyBehavior主文件净+10行，封存partial净+21行），独立组件85行；这是排序责任解耦与可计费化，不是主大类总体行数已下降或整个主体拆薄完成。

## 3. 代码位置（均绑定8bcde78b9f477a9cc7cdb8ef12b2a4cbf1e60dc3，行号一基）

| 路径 | 行号 / 符号 | 责任 |
|---|---|---|
| `Refactor/Runtime/CooperativeMemoryQueueSort.cs` | 12–85 `CooperativeMemoryQueueSort<T>`；42起`Step` | 捕获immutable keys、稳定归并、分片copy；不持有游戏API/公开入口 |
| `MyBehavior.MemorySealing.cs` | 63–123 `DailyMemorySealQueueTail<T>` | pending/净化/引用快照及私有排序生命周期；两次字段验证，不每个sort slice重扫全部字段 |
| 同上 | 201–321 `ContinueDailyMemorySeal`；299/309两调用点 | Daily/Major实际消费方，继续使用已有Campaign共享窗口与同步兼容策略 |
| `MyBehavior.cs` | 26707–26739 `SanitizeMemorySummaryQueue / NormalizeMemorySummaryQueue` | 原Daily净化体原样提取；同步路径仍完整排序 |
| 同上 | 26873–26905 `SanitizeMajorActionSummaryQueue / NormalizeMajorActionSummaryQueue` | Major原净化体原样提取、共享同一规则而非复制逻辑 |
| 同上 | 17733–17757 `RunCampaignMemoryMaintenanceCycle` | 已有共享窗口消费链，本轮未重写 |
| `tools/MemorySummaryMainThreadBoundaryTests/` | `run_sealing.py / SealingHarness.cs.txt`、`source-review-b1.json / test_source_parity.py` | 真实源提取、预算/别名/异常源反例和严格整文件inverse |

[68点代码图](../architecture/af-framework-code-map.json)记录各文件规范化hash。定位图是导航证据，不是整文件DONE或删除白名单。

## 4. 已验证与证据

[本轮验收JSON](../audits/2026-09-14-b1-queue-sort-verification.json)包含命令、实际返回码、原始/规范化源hash、六份构建产物与Stage一致性、138份冻结日志/manifest/hash。冻结目录 `.tmp/b1-queue-sort-20260914/final-evidence/`仅本地；仓库保留可复现runner与源码版本，不依赖把大日志上传。

| 检查 | 实际结果 / 层级 |
|---|---|
| 封存/真实维护入口 | 60/0；旧9158132c相同60例46/14，都是BUILD_PASS后的断言差异 |
| 故障反例 | 原14个保留+新增6个，20个均BUILD_PASS/EXIT=1；不靠编译报错充当业务红例 |
| 513项Daily/Major排序 | 新版各窗口最多128工作单元，总5643；整个Seal分别245/113窗口（观察值，不是固定合同） |
| 旧版排序见证 | 一个调用9224/8250次实际标量key比较；新单元包含最多一次entry比较及move，或一次copy。度量层不同，不相减当速度比/帧率提升 |
| 直接相邻回归 | business36；原e40c92d7为4绿32红；maintenance故障34绿2红；captured109/planning24/writers238/terminal85/commit51/admission54/materials23全通过 |
| 历史/UI/渠道 | helper32、history852、Native history27、failure UI85、Native preparation589、ChannelCutover132通过 |
| 严格源差异 | 58声明/4新增span/2删除/5组件锁精确恢复到90201155；10个防误放测试通过，含净化体精确反拼旧源 |
| 构建/兼容 | 原一键脚本-Stage：Debug/Release × 1.3/1.4/Bootstrap六项通过；6份DLL/marker与项目内Stage hash一致；未改脚本 |
| API/身份 | public119断言+256并发读取、预期CS0122；4个实际实现DLL元数据532断言；146 SyncData键/36 behaviors保持 |
| 文档/协作 | 68定位点记录提交与工作树双通过；两份用户草稿和本地专用Native简明版3份hash保持；未顺带暂存 |

测试适配实情：planning首次因抽取未纳入两条新Normalize声明而出现CS0103（工具/编译失败），补入**真实生产声明**后24/0；没有换stub、删断言或只刷hash。最终inverse也核对了新增helper的原语义。

离线game identity/summary-start/其他维护domain依然是标明的fixture，不能把本节说成Campaign实机验收、实际网络provider或DLL已被游戏加载。

## 5. 仍未完成 / 下一轮顺序

继续[原P0–P6计划第16节](../phase8/af-core-precloseout-plan-20260913.md)，**不重新盘点已通过的素材索引、Campaign窗口或本轮排序**。

1. B1深来源：首次Capture/完整raw重验、单owner原地净化、completed-owner绑定、Apply/public/weekly尾部等原子成本。特别是本轮队列normalize、数组分配/key捕获、两次全字段绑定和单次string比较仍不可抢占；大积压内存上限/真实帧耗时仍缺验收。
2. 如用revision代替raw fingerprint，先覆盖全部实际writer（包括Save之外、嵌套字段、净化、queue/state及跨owner迁移），保留读档/同generation/部分失败/晚结果拒绝，不能直接去掉hash。
3. B1整批关键预算风险关闭或明确接受且同候选综合验收合格后，才进入B2 Courier后台live读取→Native/Scene完整三渠道；然后B3内部接缝与选定public能力。D-A/D-B没确认不开放public提交/取消/写入。
4. 真实Campaign/Mission、旧档、真实资产/AFEF、TTS、provider与子MOD实际加载仍NOT_RUN；目前不是只剩实机，尚有主体代码工作。

同DLL internal制作组层/public子MOD层分开；不重写政策/宴会/GCCZ玩法。自动化保持每小时ACTIVE，有实际独立工作继续；只有达成获准收尾评审或仅剩真正外部阻塞才暂停。未来用户暂停立即优先。

## 6. 回滚与转交

- 定向revert本轮生产/测试提交`8bcde78b`和随后本轮文档提交，回到前生产9158132c语义；99a7dbfe是实改前检查点。先确认没有后续协作依赖；不hard reset/force或覆盖用户草稿。
- 本地制作组简明版：`G:/AFMOD/AF-REFACTOR/.tmp/af-core-precloseout-team-handoff.md`。指定旧Native简明版与两份2026-09-06草稿均保持原样，本轮未上传任何文件。
- 下一位先读根HANDOFF当前段与本文件，按上节第一项接着做；不要用历史手动暂停/其他机器缺依赖的记录覆盖本机当前自动化与六项Stage证据。
