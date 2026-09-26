# AF 全范围收尾续点：三渠道人设消费者与信使执行回执

生产/测试 **807bc5b944907a10d2626f3936fa7109aa74422a**，起点4140bd04，checkpoint b6cdaa19；当前仅本地，最后已核对GitHub发布仍10defeb4。目录G:/AFMOD/AF-REFACTOR，本地分支codex/af-framework-skill-delivery-20260911。全范围任务仍ACTIVE、阶段8/B1未整批验收。

## 已完成的责任包

- Native、Courier回信/来信、Scene人设消费者改为所属主线程读取人物/档案/状态，后台等待生成，主线程重验并接受。保持Native失败中止、Courier失败降级、Scene两个字段都空才生成/单字段事实回退、匿名读取和提示去重。
- Native绑定原admission；Scene绑定generation、单调scene session、epoch、Mission和候选引用；Courier绑定原session/participant/方向。Scope退休时退出等待，不取消其他渠道仍可能使用的人设生成。
- 信使入口的session、人物有效性和备用信捕获移到主线程。无效目标仍延迟标记/处理，但延迟回调不再清掉替换后的会话/目标。
- 删除无调用的Native旧等待和两个live名称getter。Shout/Courier大类合计净减259行；其余职责仍在运行，不是整文件重写完成。
- Courier主线程动作未claim可取消/超时退役；已claim则等待实际结果，不把正在执行当作“没执行”，避免丢失回执。不能撤销已执行副作用，档代变化仍拒绝交付旧结果。

## 已核实位置（807bc5b944907a10d2626f3936fa7109aa74422a，一基行号）

| 位置 | 符号 | 责任 |
|---|---|---|
| `MyBehavior.PersonaReadiness.cs:12-27` | `internal static NpcPersonaReadinessSnapshot CaptureNpcPersonaReadiness` | 同DLL值快照，所属主线程读取 |
| `Refactor/Runtime/PersonaGenerationWaiter.cs:11-32` | `internal static async Task<bool> WaitAsync` | 协作式退出等待，不取消共享生成 |
| `ShoutBehavior.PersonaPreparation.cs:18-70` | `private async Task<bool> EnsureNativeConversationPersonaReadyAsync` | 原admission重验，失败仍中止 |
| `ShoutBehavior.PersonaPreparation.cs:96-174` | `private async Task EnsurePersonaForCandidatesAsync` | 按场景/候选捕获及接受，原双空生成/单字段fallback |
| `CourierDeliveryBehavior.PreparationAdmission.cs:20-44` | `private CourierPreparationAdmission CaptureCourierPreparationAdmission` | 两方向主线程准入，无效目标保留延迟清理 |
| `CourierDeliveryBehavior.PreparationAdmission.cs:46-87` | `private async Task<bool> EnsureCourierPersonaContextReadyAsync` | 两方向等待/降级/失效重验 |
| `CourierDeliveryBehavior.DetachedPostprocess.cs:117-157` | `private async Task<T> RunCourierOwnerPhaseAsync<T>` | 未claim可退役，已claim等待真实回执 |

值DTO在`Refactor/Contracts/NpcPersonaReadinessSnapshot.cs`，不带Hero/Session；渠道内部scope持有身份引用，只在主线程访问游戏属性。没有新增队列或公开SDK能力；旧同步公开接口和Saveable身份不改。新版Native诊断使用已捕获名字代替后台解引用Hero/Character ID，提示词/规则未改。

## 验证与真实限制

| 验证 | 本候选结果 |
|---|---|
| 三消费者/准入 | 169检查，10个有效故障反例；6逆变换守卫 |
| 原消费者对照 | 98检查65失败，三个原声明与main437925b8相同；仅该helper层，不冒称外层全无保护 |
| Courier已claim回执 | 16检查，旧4失败/当前0失败，3有效反例 |
| 邻接链路 | 原主线程132、Native准备589、历史852、渠道132、Courier后处理39及8反例/历史122、Hero生成125通过 |
| 接口与产物 | 内部ports308及3反例、API119/快照32、4DLL700元数据、实际Courier Host回放、六Stage通过 |
| 存档身份 | main对照146键/36行为保持，未实际读旧档 |

原39项后处理测试中“已开始后取消”有1项旧期望为取消异常，按本次有意修复改为真实结果且副作用仍一次；独立16项复现旧问题，不是仅删掉失败断言。消费者测试的游戏/生成/dispatcher接缝为替身，实际dispatcher由独立原套件验证；阶段性PASS不是实机证明。

完整证据：[审计JSON](../audits/2026-09-15-channel-persona-verification.json)，绑定源码、6DLL和33份本地冻结日志。复现命令见`tools/ChannelPersonaPreparationTests/README.md`与`tools/CourierOwnerPhaseTests/README.md`。测试只在生成副本缩短等待；生产500ms轮询/180秒名义等待/30秒未claim主线程deadline不改。主线程排队延迟不受硬抢占，已claim卡住的动作也不能安全强停。

## 接下来继续，不标完成

1. GameEnd真实生命周期缺口：当前SubModule.OnGameEnd只移除地图按钮并调base；需绑定实际Game身份，退出先让旧generation失效，再退役owner队列/任务与静态订阅，防止旧回包在主菜单或下一局继续。
2. preprocess/lore、角色/库存/消息构造还有后台live读；升格同伴人设/技能另有旧链，继续完整捕获→后台→接受，不将同步网络搬主线程。
3. B1首次深复制/全部writer/实际预算、内部双向主体服务与Native/Scene/Courier版本化提交/结果/取消SDK仍必做。
4. 按main主体矩阵完成逐功能对照/清理；政策宴会GCCZ玩法不重写。实机/旧档/live Economy/AFEF/独立子MOD验收仍待当前候选证据。

没有推送、部署、操作存档或恢复自动化，三份保护文件不变。回滚用针对本提交的逆向提交，不hard reset。总HANDOFF和112点地图同步；仅本地简明版位于.tmp/channel-persona-20260915/team-handoff.md。
