# AF 框架装配拆分 HANDOFF（2026-09-15）

## 1. 结论与版本

**已经从“编排蓝图”进入真实代码拆分：本轮 I1 装配切片离线验证完成；不是整个框架或阶段 8 完成。**

- 生产/测试提交：`955a6be314840be8d20f1320d3c7f23c7a93fe77`。
- 拆分前生产对照：`61d578926329ace61bf6b6ae43e12bf7d89b4696`；本轮起点 `812b34b0`，意图检查点 `78711dfb`。
- 唯一工作目录 `G:/AFMOD/AF-REFACTOR`；本地分支 `codex/af-framework-skill-delivery-20260911`。
- 本轮 fresh fetch 的目标 `origin/codex/af-main-refactor-continuation-20260831` 仍为 `af618912`；仅本地提交，**未推送、未部署、未操作存档、未恢复自动化**。
- 制作组政策/宴会/GCCZ 的玩法、提示词、状态机与存档实现未改；公开 Api.V1 仍只读，不开放假成功的提交接口。

## 2. 现在的真实调用链

```text
SubModule.InitializeGameStarter（原引擎签名）
└─ ModuleFrameworkRuntime.RegisterCampaign（现有唯一装配入口）
   └─ CampaignComposition.Register（非 Campaign 不做事）
      ├─ CampaignModelComposition.Register：先注册原 4 个包装模型
      └─ 原 36 个 CampaignBehavior：原类型、原顺序、每次回调新建

SubModule.OnSubModuleLoad
└─ ModuleFrameworkRuntime.Initialize
   └─ TeamModuleRegistration.CreateDirectory
      └─ 现有 InternalModuleDirectory + TeamModuleServices typed 接缝
```

这次拆的是“谁创建和注册”，不是把政策、宴会等业务搬到框架。没有增加第二套 Directory、全局 ServiceLocator、反射扫描、执行队列或公共接口。

## 3. 已完成与保留语义

| 拆分项 | 本轮结果 | 必须保留的责任 |
|---|---|---|
| Campaign 行为装配 | 36 个注册移出 SubModule，独立清单 | 模型先于行为；行为顺序/构造器/异常传播不变 |
| 模型包装装配 | 4 个 helper 移入专门 owner | 选择最后一个非 AF 模型作为 inner；无 inner 用默认；逐项失败记录后继续 |
| 制作组目录声明 | 政策/宴会/GCCZ 3 组 typed 声明从状态管理提取 | 使用原桥 ID/版本/失败原因；真实调用仍走原领域 owner |
| 原大类清理 | SubModule 净减 148 行，ModuleFrameworkRuntime 净减 25 行 | 原引擎 override 保留为真实入口；其余 UI/Harmony/Tick 仍未拆 |
| 兼容 | 无新 public 类型/方法、无行为类型或 Saveable 迁移 | Api.V1 Ready 仍仅表示 adapter 已绑定，绝非读档完成/可提交 |

特意没有加“只注册一次”的全局标志：这会错误跳过后续 Campaign。重复回调沿用旧行为，不把静态缓存或去重包装成生命周期改进。两个 Campaign 得到独立行为与模型包装实例。

## 4. 代码位置（以上述生产提交为准，一基行号）

| 路径与行号 | 符号 / 实际消费者 | 已覆盖 / 未覆盖 |
|---|---|---|
| `SubModule.cs:653–657` | `InitializeGameStarter` → `ModuleFrameworkRuntime.RegisterCampaign` | 入口委托；未改变其他引擎 Hook |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:68–71` | `RegisterCampaign` → `CampaignComposition.Register` | 无额外 Ready 门禁/缓存/锁；不持有 starter |
| `Refactor/Modules/CampaignComposition.cs:14–56` | `Register` | 原 36 个行为构造与登记；不负责业务/SyncData |
| `Refactor/Modules/CampaignModelComposition.cs:17–23` | `Register` | 原模型顺序 |
| `Refactor/Modules/CampaignModelComposition.cs:25–127` | 4 个私有 `Register…Model` | 原 inner 选择、fallback、日志与异常处理 |
| `Refactor/Modules/TeamModuleRegistration.cs:16–52` | `CreateDirectory` / `RegisterAdapter` / bridge 校验 | 原目录声明与门禁；不是新注册器 |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:20–62` | `Initialize` | 仍唯一持有目录与状态，调用新声明 owner |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:73–110` | `Shutdown` / `GetSnapshot` | Stopped/只读投影原语义；公共 DTO 投影尚未独立提取 |
| `tools/CampaignCompositionTests/run.py` | `verify_source` / `restore_submodule` / `main` | 旧新逆变换、真实装配执行、故障反例；不是实机 |

同步机器索引：[代码地图](../architecture/af-framework-code-map.json)（86 锚点）；文字范围：[代码范围说明](../architecture/af-framework-code-scope.md)。旧坐标只在对应历史提交下有效。

## 5. 实例创建与释放现状（不是虚构统一生命周期）

| 对象 / 作用域 | 实际创建或持有点 | 当前停止/失效边界 | 本轮状态 |
|---|---|---|---|
| typed adapter / 程序集 | `TeamModuleServices.cs:6–10` 静态属性首次使用 | 无游戏实例、无 Dispose 责任；目录停止后 adapter 仍为静态无状态实例 | 未改变 |
| 目录 / 模块加载 | Runtime `Initialize` 创建，经验证后持有 | `Shutdown` 只发布 Stopped，保留字符串描述；重载新建目录 | 声明已拆，生命周期未扩充 |
| 36 个行为 / Campaign | `CampaignComposition.Register`，交给引擎 starter | 仍由引擎/各行为原有事件持有；本清单不声称统一释放完成 | 装配已拆，业务未搬 |
| 4 个模型 / Campaign | `CampaignModelComposition.Register`，保留原 inner 引用 | 无新增静态引用，跟随 starter/引擎原生命期 | 包装装配已拆 |
| Memory dispatcher / MyBehavior | `MyBehavior.MemorySummaryMainThread.cs:18–28` 懒创建、CAS 发布 | `:55–56` Reset；`MyBehavior.cs:2475–2510` 新档/读档推进 generation 并退役暂存任务 | 沿用上轮；不是程序集单例 |
| 场景临时状态 / Mission | `ShoutBehavior` 的原 Campaign owner 管理 Mission epoch/队列 | `ShoutBehavior.cs:831–834` → `:26713–26742` 移除行为时原清理 | 本轮未修改/未做实机验收 |
| 请求 coordinator / 调用方 | `LegacyInteractionPipelineComposition.cs:75–114` 构造真实 pipeline/coordinator | `InteractionRequestCoordinator.cs:130–148` Dispose 取消并释放在途源；各消费者触发点仍需后续梳理 | 不冒称全部已接统一生命周期 |

`SubModule.OnGameEnd` 仍只处理原 UI 清理；不假造全局“Campaign 已停止”状态。不能在没有验证每个 owner 的真实 Hook 前增加统一热卸载或重新定义公开 Ready。

## 6. 验证结果与限制

| 验证层 | 结果 | 不代表什么 |
|---|---|---|
| 源码逆变换 | SubModule/Runtime 整文件变化严格限于此次提取；4 个模型 helper 原逻辑、行为顺序、桥策略保持 | 不代表模型实际数值已实测 |
| 装配执行 | 42 断言通过：null/非 Campaign、36 顺序、4 模型、inner、跨档实例、失败、目录各种状态 | 引擎/行为/模型是 test doubles；实际构造副作用未执行 |
| 故障反例 | 5 类均成功编译并被行为断言拒绝 | 不是把编译失败当测试灵敏度 |
| 公开 API | 119 断言、256 并发读取、外部访问 internal 的 CS0122 拒绝 | 未执行独立子 MOD 真实加载 |
| 实际 DLL | Debug/Release 四实现 DLL 元数据 556 断言，V1/存档相关签名一致，新装配类型 internal | 不是 CLR/游戏加载验收 |
| 构建 | Debug/Release × 1.3/1.4/Bootstrap 六项 Stage 成功，只写项目目录 | 不覆盖游戏、不启动 Campaign |
| 存档身份扫描 | SyncData 146、CampaignBehavior 36，零新增/移除，Bootstrap 模块身份保持 | 不等于真实旧存档成功读写 |
| 制作组接缝独立检查 | 原 13 签名匹配、308 port 参数/返回/ref/out/异常断言通过；SubModule 历史逆变换独立通过 | **完整 TeamModulePortParityTests 仍未通过** |

两项真实失败均保留而非隐藏：
1. 首次 Debug 编译因移除仍被 UI 使用的 `AnimusForge.PolicyEffects` using 失败；已恢复，后续 Debug/Release 全成功。
2. 历史制作组完整测试的 Native/Memory 源码定位表仍查找旧 `ProcessMemorySummaryQueueAsync`，该方法在拆分前 `61d57892` 就已不在 `MyBehavior.cs`；未改动该源码，也未豁免旧门禁。后续应按真实迁移证据修复其对照位置，不能只刷新 hash。

审计与本地产物/日志哈希：[验证清单](../audits/2026-09-15-composition-extraction-verification.json)。新生成代码位于忽略的测试 `.generated` 目录，不参与生产编译。

## 7. 后续顺序与门槛

1. **I1 下一段：真实生命周期接缝。** 对照新档/读档/结束、Mission 切换、各渠道取消与晚结果提交，确定 owner 的准确建立/停止边界；先补可复现用例，再提取。注册顺序整理不等于该项完成。
2. 分离现有目录内部状态和 public 只读投影，保持 V1 签名/枚举/原因码；不加空泛公共接口，不额外发明 registry。
3. 回到[14 类职责清单](../phase8/af-core-responsibility-decomposition-plan-20260915.md)：Conversation/Prompt/Actions 与 Native/Scene/Courier 的真实 owner 迁移，Memory 首次 capture/copy、完整 writer/预算继续闭环；不能把 partial 文件增多算成解耦完成。
4. 修复历史源码对照工具的已知漂移，验证真实调用/失败/默认回退；本轮旧新装配一致不等于原 AF 所有功能已经完美复现。
5. 阶段 8/B1 仍未整批合格；真实 Campaign/Mission、旧存档、live Economy/AFEF 和候选游戏验收继续单列，不擅自删仍承担业务责任的旧 owner、切默认或最终发布。

## 8. 回滚与协作

- 原实现可从 `78711dfb` / `61d57892` 精确取证。需要回滚时对 `955a6be3` 做有审查的逆向提交；不 hard-reset、不覆盖他人文件、不改历史。
- 两份已有用户草稿与 `2026-09-11-native-history-snapshot-team-handoff.md` 均未改变；后者保持本地专用、不上传。
- 简明转交版另存本地 `.tmp/composition-20260915/team-handoff.md`，不是可上传文档的必要依赖。
- 本文是本次实现的接续入口，取代蓝图中“尚未改 C#”的当前状态；蓝图的后续设计和未完成门槛仍有效。
