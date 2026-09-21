# J11 制作组内部模块接缝离线收口 HANDOFF（2026-09-21）

## 结论

**J11 已达到 `OFFLINE_VERIFIED`。** AF 主体与编入 `AnimusForge.dll` 的 Policy / Gathering / Siege 制作组功能之间，已经形成独立于 public 子 MOD API 的 internal contracts、薄 adapter 和装配层；原玩法 owner、资格、状态、存档、Prompt、MCM、Harmony 和失败语义不变。

本结论只覆盖源码、离线行为、双版本构建、API/存档兼容和组合门禁。真实 Bannerlord Campaign/Mission、旧 SAVE、实际制作组玩法结果及真实 GCCZ 场景仍 `NOT-RUN`。

## 代码结果

### Internal contracts

| 接口 | 路径 | 方法 |
| --- | --- | ---: |
| `IPolicyModulePort` | `src/AF.Contracts/Internal/TeamModules/IPolicyModulePort.cs:7` | 4 |
| `IGatheringModulePort` | `src/AF.Contracts/Internal/TeamModules/IGatheringModulePort.cs:7` | 5 |
| `ISiegeModulePort` | `src/AF.Contracts/Internal/TeamModules/ISiegeModulePort.cs:7` | 4 |

三个接口保留原 `AnimusForge.Refactor.Modules` namespace、`internal` 可见性、Hero/Character/PostprocessRuleEntry 类型、默认参数、ref/out、返回值与异常。没有增加预留方法、状态、I/O、服务定位或 public DTO。

### Thin adapters

| Adapter | 路径 | 真实 owner / 调用点 |
| --- | --- | --- |
| `PolicyModuleAdapter` | `src/bridges/Policy/PolicyModuleAdapter.cs:7` | `KingdomAgendaCustomPolicyBehavior` / `NpcRulerPolicyBehavior`；8 调用点 |
| `GatheringModuleAdapter` | `src/bridges/Gathering/GatheringModuleAdapter.cs:7` | `NobleGatheringBehavior`；12 调用点 |
| `SiegeModuleAdapter` | `src/bridges/Siege/SiegeModuleAdapter.cs:7` | `AfGcczShoutBridge`；11 调用点 |

六个新 declaration 与 J11 启动前旧文件中的 interface/class body 逐声明一致。adapter 不 catch、不缓存、不 retry、不拼 Prompt、不解析标签、不写历史/AFEF、不显示通知；只是一次直接接口调用。完整方法/调用点/频率/gate/副作用 owner 见 `docs/architecture/af-team-module-seam-matrix.md`。

### Composition 与清理

- `src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs` 继续只创建三个静态无状态实例。
- `TeamModuleRegistration` 继续登记 `af.team.policy`、`af.team.gathering`、`af.team.siege`；Directory `Ready` 不代替实际业务资格。
- 旧 `Refactor/Modules/TeamModulePorts.cs`、`Refactor/Modules/TeamModuleAdapters.cs` 已删除，没有 Link、复制实现或同名声明。
- `FeatureBridgeRuntime` 未新增 binding；仍是 16 bindings、12 wired、4 declared-only。Siege 继续由 `AfGcczShoutBridge` 的 cached `conversation-siege` gate 和 active-stage gate 决定。
- 未修改 `G:/AFMOD/GCCZ`、其他工作树、游戏目录、存档或玩家数据。

## 稳定接口保证

1. **主体 → 制作组**：主体只依赖三个 internal semantic ports，不读取具体 Behavior 私有字段、存档字典或 Harmony 顺序。
2. **制作组玩法归原 owner**：PolicySystem、NobleGathering、GCCZ 继续拥有全部业务算法与数据；adapter 不是第二 owner。
3. **事实唯一提交**：Gathering facts/notifications 和其他领域结果仍由真实渠道调用方提交；桥层没有新 AFEF/history/UI 写入。
4. **内部/外部分离**：本轮没有改变 `AnimusForge.Api.V1`；独立子 MOD 的 Scene/Courier submit 仍属 J14。
5. **性能**：热路径没有 registry lookup、反射、程序集/目录扫描、文件/网络 I/O、轮询、队列或新缓存。
6. **兼容**：未新增或修改 SyncData key、Saveable type、程序集名、public ABI、默认开关或资源身份。

## 最终验证

### Ports / Policy / Composition

- `TeamModulePortParityTests`：13 方法、31 个生产 call expressions、308 行为断言；3 个可编译变异全部被具名行为断言拒绝。
- Policy 1.3：all-modules **1406**（18 modules）、history **1115**。
- Policy 1.4：all-modules **1406**（18 modules）、history **1115**。
- `CampaignCompositionTests`：42 assertions；drop behavior、reverse models、discard inner、directory gate、abort model failure 五个变异拒绝。
- `CompositionMatrixContractTests`：18 cases / 24 invariants。
- `ModuleFrameworkApiTests`：119 public API + 256 concurrent reads；snapshot 36/128；5 个 snapshot 变异与 internal-access 编译拒绝通过。
- Bridge：16 bindings、23 tests、12 isolation processes。

### 三渠道影响面

- Scene postprocess parity：71 fixtures。
- J09 default channel wiring：25 checks。
- Courier：Prompt 550 / 76 scenarios；postprocess 39；domain commit 32。
- Native：Action dispatch 91；Completion 184。
- 三渠道仍复用 J09 Tags/Plan/Execute/Receipts；未增加 parser 或提交点。

### 构建 / API / persistence / readiness

- `.tmp/build-local.ps1`：Debug + Release × Bannerlord 1.3、1.4、Bootstrap 六项均 0 warning / 0 error；实际引用 1.3.15.110062、1.4.6.115628，SDK 8.0.422。
- Debug/Release 四个实现 DLL：1060 API/metadata assertions；三个 port、三个 adapter 和 TeamModuleServices 仍为 internal。
- Persistence/Profile：142 literal keys / 168 typed bindings / 13 chunked / 44 flattened；Identity contract 5；Chunk replay 8。
- Phase8 readiness：73 tests。
- 394 锚点代码地图 recorded / working-tree 两模式通过，绑定产品 `cdbd077af3abb4614594eff4b198052a4841e63e`。

## 测试维护说明

- `TeamModulePortParityTests` 和 `ScenePostprocessParityTests` 改为链接 3 port + 3 adapter + composition 的真实新路径，不用兼容 Link 保留旧文件。
- `CampaignCompositionTests` 不再因无关 GameLifetime runner hash 阻断；现在对 live `SubModule.InitializeGameStarter` 的唯一委托、partial-start cleanup 和异常传播做 scoped source 检查，同时保留 runtime exact inverse、42 行为断言与 5 变异。没有刷新旧 hash 或删除运行断言来“做绿”。

## 清理结果

- 新 contracts/adapters 无冲突标记、TODO/HACK/TEMP、具体状态、catch/retry、I/O、事实写入或无消费者接口。
- 主体 Conversation/Prompt 范围内未发现 13 个领域方法的直接 owner 绕过。
- 旧混合 contracts/adapters 路径只在历史文档和 J11 启动基线叙述中保留，不参与 Compile。
- `TeamModuleServices` / `TeamModuleRegistration` / 原领域 owner 都有活动消费者，不能作为“旧代码”删除。

## Git 与回滚

- J11 计划：`b58e41d0`。
- J11 产品/测试：`cdbd077a`。
- 最终地图/文档提交：以本文件所在提交为准。
- 回滚顺序：先 revert 最终文档/地图，再 revert `cdbd077a`；不 hard reset、不 rebase、不强推。

## 未验证和下一步

未运行真实 Campaign/Mission、旧档 round-trip、真实政策/宴会/GCCZ 玩家场景、实际子 MOD CLR、provider、音频或性能；未 Stage/Deploy/Package。下一阶段应先制定 J12 Economy / Diplomacy / WorldMap 的有限计划，不因还能包装更多方法而重开 J11。
