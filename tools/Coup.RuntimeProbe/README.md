# 政变子 MOD 实际注册探针

## 2026-10-05：正式构建门禁

正式同 DLL 实现通过 `CoupOutcomeMemoryPort` 强类型工厂及恢复端口提交记忆，不再反射查找 `InteractionMemoryCommit` 构造函数。保留的独立源构建以缓存 delegate 绑定同一内部端口，不新增 public 子 MOD API。

`build_single_module.ps1` 在两实现和 Bootstrap 编译后、任何 Stage/Deploy 之前，分别对最终 DLL 执行 `--seam-only`。新选项：

- `--reference-dirs <目录1|目录2>`：TaleWorlds/SandBox/StoryMode 只从显式允许的引用目录解析，禁止偷偷回退到另一游戏版本；每 API 独立进程。
- `--expected-version <v1.x.x.x>`：核对真实加载的 `BuildInfo.GameVersion`。
- `--seam-only`：生产接缝/Harmony 初始化＋20项实际记忆工厂/恢复拒绝回归；仅正常构造托管拘押行为以检查注册可用性，不启动 Game/Campaign，不执行选兵/场景夹具。与 `--scene-fixture` 互斥。

每次通过写入 `AnimusForge.build.json.CoupSeamGate`，包含候选/API/引用版本、探针/报告 SHA256 和实际加载依赖 MVID/SHA256。失败或重跑失败不留下可用收据。覆盖预检、打包目录及最终 ZIP 校验都拒绝缺失或与 DLL 不匹配的收据；旧候选需从原入口重建，不能手填通过标记。

`Test-ReleaseGate.ps1` 对实际生产校验函数运行39项正负回归；只使用本地合成 DLL 字节/marker 和内存 ZIP，不执行真实部署或打包。`--scene-fixture` 仍执行独立的场景夹具，历史 siege-aftermath 方法已退役，现回归当前胜利反馈存档契约，真实生产回调另由 `extensions/AnimusForge.Coup/tests/Coup.VictoryFlowTests/run.py` 检查。

**边界**：本门禁验证候选托管接缝与当前引用快照，不证明所有原生游戏行为。沿用的1.3引用覆盖尚不完整；既有1.3/1.4快照的 SandBox core 为共享补充依赖，其哈希公开在证据里，不能称为纯1.3原版全栈实机验收。缺少真正匹配的引用应补齐快照，不能删除失败保护或借另版游戏目录凑通过。


复位接缝回归（`--scene-fixture`）：额外验证扣押分支排除原王族、释放后无地原王族可加入、被俘旧王不可加入，以及已建国登记失败的真实 JSON 往返和手动重试不重建国。新内部登记签名随真实 DLL 绑定校验；王位/国名恢复、旧王死亡继承、外部议和不复位、胜利自身停战后的中断续结算由 `CivilWarLifecycle.ContractTests` 链接生产 owner/effect 验证。游戏对象与政治动作是明确夹具，不代表原生战争、游戏存档或实际建国已验。

旧王支持者与处置回归（`--scene-fixture`）：真实ObjectManager注册旧王、真实选择器比较新旧关系，并验证原王族优先、友好于新王仍可反抗、相等关系不反抗、无实际候选、总开关/免疫及支持变化；物理起兵资格为明确桩。实际Coup owner→缓存adapter→SETS入口执行，原生菜单打开函数被截获以检查transferOwnership=false和幸存者名册，验证失败保留重试、成功/读档不重开、城镇归属改变取消；无真实建国、LLM、原生菜单渲染或处置奖励执行。

发动门槛回归（`--scene-fixture`）读取真实MCM三项默认/范围/免重启属性，调用新事件默认资格检查、已存门槛复核及真实街道选兵回调；验证61名健康普通兵、英雄/伤兵排除、实际59/60人、选兵期间影响力或名册变化、登记后不追检和不扣影响力。世界资格上下文、PartyScreen、守军采集为明确桩；MCM真实渲染及UI修改后的全局设置提供者未执行。原选兵数据工厂增加minimum参数与大厅默认1人的检查，不代表实机按钮已验。

此工具在独立 .NET Framework 4.7.2 x64 进程中，加载已安装 AF、真实游戏 managed DLL 与指定的 `AnimusForge.Coup.dll`，调用生产实现的：

- `SettlementEntryTroopSelectionBehavior.Register(Harmony)`
- `CoupRebellionBridge.Initialize()`
- `CoupGuards.Register(Harmony)`

检查四个可用标志，枚举真实 Harmony 目标和 prefix/postfix/transpiler 数量，记录源 DLL 哈希、MVID 与实际依赖路径。默认仅将 AF 的 `GetLogsDirectory` 重定向到工作区；不制造 `Game`、`Campaign`、`Mission`、`Agent`，不替换游戏 API，不发送 LLM 请求，不修改输入 DLL。当前内置 Coup 可将两个 DLL 参数均指定为同一份新构建的 AF 实现；必须与游戏依赖版本匹配。

```powershell
dotnet build .\tools\Coup.RuntimeProbe\Coup.RuntimeProbe.csproj -c Release -p:GameRoot='<游戏目录>'
& '.\tools\Coup.RuntimeProbe\bin\Release\net472\Coup.RuntimeProbe.exe' `
  '<游戏目录>' `
  '<游戏目录>\Modules\AnimusForge\bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll' `
  '.\extensions\AnimusForge.Coup\artifacts\1.4\AnimusForge.Coup.dll' `
  '.\artifacts\coup-runtime-probe\1.4'
```

必须选择与实际游戏安装匹配的 AF 和子 MOD 实现。不要用 1.4 游戏依赖运行 1.3 实现并宣称验证了 1.3。输出目录必须位于游戏目录之外。退出码 `0` 表示注册通过，`1` 表示绑定或可用性失败，`2` 表示参数错误；详细记录在输出目录的 `registration.log`。

本检查比成员存在性检查多验证了私有 delegate 创建、Harmony 绑定和拘押 transpiler 的实际 IL 匹配。它不启动游戏，不验证各补丁在真实 AF 全部其他补丁挂载后的组合行为，不验证原生场景、导航、指挥 UI、伤亡政治结算或存档兼容性。

`--scene-fixture` 还执行 Coup 快报宿主回归：真实 `TryRecordCoupOutcomeForBulletin` / `CaptureWorldBulletinEvent`，只替换设置开关并使用未构造的 MyBehavior；验证分类、触发分数、区域归属、超过64条后的去重、真实 Newtonsoft JSON 往返及关闭快报不阻断结算。没有执行真实政治结算、游戏存档、生成式报道或UI。完整文案、阶段事实和NPC注入另由 `extensions/AnimusForge.Coup/tests/Coup.ContractTests` 链接生产会话/报告/快报策略验证。

`--scene-fixture` 同时验证七项篡位MCM的实际属性元数据、默认/范围/免重启、设置快照及真实Newtonsoft旧格式/损坏格式；执行真实adapter→SETS队列→进场准备→mission构造，验证120/40人数和普通场景隔离。真实首波/定时波次方法验证间隔、存活波数门槛、剩余兵源耗尽；真实BuildDefenders验证0/3/45/75名健康守军及伤兵排除。世界查询、任务时间、活跃波数与原生生成被明确替换，不声称测试真实Agent生成、战斗性能或MCM渲染。大厅门夹具增加40人幸存者选择与最小上限检查。

入口回归调用真实 Coup delegate 和主体 owner 查询，逐项注入/清理六种 pending 对象、随行活动/保护/叛乱状态，验证缺失 delegate 和查询异常继续关闭入口。叛乱回归使用跳过构造的 MyBehavior fixture，驱动真实调度 owner 的命名、完成、消费和取消；未构造游戏或任务对象，随行 Active 仅作为状态 fixture 直接设置。拘押回归先验证未构造行为时不可用，再正常构造 CoupCaptivityBehavior 验证注册成功，并验证存档有效标志保护。因此四标志检查在行为构造后执行，避免把加载期无实例误报为拘押补丁失败；不声称测试了真实存档加载。

追加可选第五参数 `--scene-fixture` 可运行进场崩溃及大厅门转场回归。它**会创建绕过构造的 Mission/Settlement/CharacterObject 及 Campaign/Agent fixture**，初始化空 managed 行为列表；在独立 Harmony owner 下临时替换城镇/家族上下文读取、菜单注册和入口资格查询，结束时卸载 fixture 补丁并还原实例。真实生产 Origin 工厂与 OnScoreHit 回调、真实 MbEvent 逆注册顺序、SETS handler/postfix/Coup 挂载与幂等、菜单阶段显示均执行。门测试调用真实已补丁的 PassageUsePoint.OnUse，以最低优先级计数桩观测是否漏回原版；实际生产目标判断、阶段切换、清空原版转场、任务结束处理与大厅选兵回调均执行。原生 EndMission、血量采样、显示与 PartyScreen 是桩，ScriptComponentBehavior 仅补空的脚本类型元数据供静态初始化，并还原原字段。未启动 Game/原生场景，不证明真实 F 输入、完整资格、渲染/受击、选兵到结算或旧档成功。日志分别标 `SCENE_SCOPE` / `PASSAGE_SCOPE`，不能写成实机验收。

选兵回归另调用生产 `CreateInitializationData`，用真实managed类型的受限fixture验证owner/ItemRoster引用、四份临时名册隔离、人数上限与确认/取消回调。PartyBase真实构造需要Campaign，fixture明确用FormatterServices跳过构造；没有创建Game/Campaign/PartyVM，没有执行原版升级信息刷新、转移箭头或重置UI。日志会单列此验证边界，不能将29项数据工厂检查称作实机选兵通过。
