# J12 Economy / Diplomacy / WorldMap 最终离线收口

日期：2026-09-22  
工作树：`G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`  
本地分支：`codex/af-modularize-j04-20260918`  
交付比较分支：`origin/codex/af-main-refactor-continuation-20260831`

## 1. 结论

J12 已达到 **`OFFLINE_VERIFIED`**。产品终点是：

```text
5c3e7b0e2ba73b3d024182f559ae054215bb529e
refactor(j12): close diplomacy and worldmap owners
```

这次结论只覆盖 Economy / Diplomacy / WorldMap 的职责归位、真实消费者接线和离线回归。它不是实机、旧档、Stage、打包或发布完成。

## 2. 实际完成

| 领域 | 新的稳定 owner | 保留边界 |
| --- | --- | --- |
| Direct Diplomacy | `src/modules/AF.Module.Diplomacy/Direct/DiplomacyCrossDomainActionOwner.cs:10` | 游戏对象资格、通知和原 Direct host 生命周期不改 |
| World Diplomacy | `WorldDiplomacyJobRuntimeCoordinator.cs:32`；`WorldDiplomacyBehavior.JobRuntime.cs:46,220` | Campaign/save 状态、真实领域 mutation 与 J08 transport 保留原 owner |
| WorldMap protocol/admission | `WorldMapOrderCoordination.cs:65`；`WorldMapPartyCommandBehavior.Protocol.cs:27`；`Admission.cs:37` | 目标解析和游戏对象读取仍在所属线程 |
| WorldMap queue/events | `QueueRuntime.cs:27`；`EventLifecycle.cs:27` | 原 Campaign event 注册一次，TaleWorlds AI mutation 留 host |
| WorldMap delayed requests | `DelayedRequests.cs:177`；`WorldMapPendingRequestCoordinator.cs:11` | UI、建队、转兵和总督实际变更留 game host |

关键结果：

- 附庸和王国吞并不再由 Economy 主类拥有解析/执行算法；
- 世界外交后台任务只持有冻结 request，不读取可变 `job`；
- queue 仍按优先级、cache affinity、创建日、JobId 排序；
- completion 按捕获的 job/generation/route 判定，迟到结果不能污染新一代；
- WorldMap STOP 只在合法顺序消费，失败标签不推进序号；
- `AddedCommandCount` 是实际成功入队数；
- 同伴关窗和总督延迟请求都使用 one-shot ticket，只能领取自己的请求；
- 未增加 SyncData key、Saveable identity、公有 ABI、默认开关或第二条 Actions 管线。

## 3. 有意保留

- `WorldDiplomacyBehavior` / `WorldMapPartyCommandBehavior` 仍保存 Campaign、Saveable、事件注册和游戏对象操作；这些不是可安全删除的 facade。
- 四个 WorldMap SyncData key 与嵌套保存 DTO 保持原类型和字段。
- `ApplyRewardTags` 等兼容入口仍有 Native / Scene / Courier 真实消费者；J12 不以破坏兼容换取行数下降。
- Policy、Gathering、GCCZ 玩法未搬入主体；J11 内部桥保持不变。
- J14 的 public Scene/Courier submit 能力没有提前开放。

## 4. 验证

| 门禁 | 结果 |
| --- | --- |
| Debug/Release × Bannerlord 1.3/1.4 + Bootstrap | 6/6 PASS，0 warning / 0 error |
| J12 lifecycle / owner source | 68 / 3 PASS |
| Diplomacy Intent / Compression / PolicyHistory / ResultSettlement | 1176 / 297 / 95 / 453 PASS |
| J09 default channel wiring | 25 PASS |
| Native action / completion | 91 / 184 PASS |
| Scene postprocess | 71 PASS |
| Courier commit / domain commit / postprocess owner | 34 / 32 / 39 PASS |
| API/metadata | 1060 PASS，Debug/Release 四份实现 DLL |
| Persistence | Profile 142 keys / 168 bindings / 13 chunked / 44 flattened；Chunk replay PASS；Identity contract 5 PASS |
| Bridge | bindings 16（wired 12）、runtime 12、fixture 10 PASS |
| Phase8 / repository inventory | 73 / 7 PASS |
| 代码地图 | 429 anchors，recorded/working-tree PASS，绑定 `5c3e7b0e` |

负向证据：queue selector 的 `running job` 单变量变异成功编译，并因具名选择断言失败；不是路径或编译失败冒充红例。此前非国王宣战、虚假战争事实和迟到 WorldMap callback 红例也保持拒收。

原 `WorldDiplomacyResultSettlement.SmokeTests` 目标为 net6.0，而本机 SDK 8 缺少离线 net6 reference pack；直接运行会尝试 NuGet。最终使用不改 `Program.cs` 与生产规则源码的临时 net8 host 完成 453 项。这是测试宿主限制，不是产品构建失败。

## 5. 未验证

以下全部保持 `NOT-RUN`：

- 真实 Bannerlord Campaign / Mission；
- 旧 SAVE 实际加载与 round-trip；
- live Economy / Diplomacy / WorldMap / AFEF；
- 直接 Persistence identity audit 与 Phase8 Stage DLL parity（都需要当前 Stage 实现）；
- 真实 provider、TTS/音频；
- 游戏线程帧成本、长局背压和真实性能。

本轮未 Stage、Deploy、Package，未覆盖游戏 DLL，未操作存档。

## 6. 回滚

1. 先 revert J12 最终文档提交；
2. 再 revert `5c3e7b0e`；
3. 若还需撤销审查修复，再 revert `a50ab3ad`。

不要 hard reset、rebase、stash 覆盖或强推。代码回滚不能逆转已经发生的游戏状态。

## 7. 下一步

进入 J13 前只需读取根 `HANDOFF.md`、本文件、主台账置顶 J12 节和 J13 计划。J13 处理 Social / Weekly / Duel / Encounter / Issue 等主体领域 owner；除非有新的具体回归或修改 J12 源码，不再重新调查 J12。
