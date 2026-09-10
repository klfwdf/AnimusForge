# Native 展示作用域验证（2026-09-11）

代码/测试：`32230a64`；本轮检查点：`ee6b7baf`；前序 Native 准入：`77d4a940`。

## 结果

- 新展示检查 **46 PASS / 0 FAIL**；六个行为变异全部被运行时断言拒绝。
- 原准入 **44 PASS / 0 FAIL**；七个原变异全部保留并通过反例控制。
- 最终 Debug/Release × 1.3/1.4/Bootstrap **六项构建通过，0 warnings / 0 errors**，仅项目内 Stage。
- 原接口/薄桥、Scene/Courier、管线与所选生产程序集回放通过。最终四 DLL 的 V1 公共 API 元数据 472 个断言通过，公共 API 仍只读。

**以上均不是新版本的真实游戏/旧存档验收；没有推送或覆盖游戏。**

## 关键证据

1. 从 `36e04059` 抽取实际旧 stream 回调和 generation guard，在隔离 UI 队列执行：排入 A 回复后切换显示至有效对象 B，旧回调仍把 B 写成 A 回复。原反例保存在工具 `.generated/presentation-original/run.log`。
2. 新测试链接真实 `ShoutBehavior.NativeAdmission.cs` 与 `AnimusForgeNativeConversationOverlay.Presentation.cs`，执行两条真实 stream 回调、主线程调度、generation、通知与动画方法。游戏实体、格式器和完整下游业务用明确 fixture 代替。
3. 验证 scope 绑定真实内部准入、backend slot 已释放后仍可显示合法最终结果、scope 单次使用、后台不访问游戏状态、不同身份戳/同上下文新 revision 的失效、等待动画/通知/最终收尾及本地 busy 释放。
4. 14 个异步 UI 消费入口在出队后验证；仅 finally 使用单独的本地-owner 清理。新 helper 不让失效结果恢复旧文本、结束新流式或发旧提示。
5. 变异覆盖：移除出队 guard、移除 revision、错误依赖 backend slot、跳过轻量 Tick 退休、允许失效最终收尾、允许失效通知。不能靠编译失败当作捕获。

## 相关回归

| 项目 | 结果 |
|---|---|
| 制作组 ports | 13 签名 / 31 调用 / 308 断言 / 3 变异 PASS |
| Scene 后处理、队列 | 71 + 2、37 PASS |
| Channel、Courier owner、Native TTS fallback | 132、39、14 PASS |
| InteractionPipeline、BridgeRuntimeIsolation | PASS |
| ProductionOptInEntry / CourierHost / DetachedHost / EconomyAwareCommit | PASS，最终 Stage 后重跑 |
| BridgeBinding、PersistenceProfileConfig、entry inventory | PASS；168 个存档绑定不变，未改存档目录 |
| 公共 API | 119 断言、256 并发、预期 CS0122、最终四 DLL 472 元数据断言 PASS |
| 缺失证据示例 | 仍按预期 BLOCKED / exit 2，不把离线结果提升为实机验收 |

本轮最后只清理了旧可用性查询的三行误导注释，因此逻辑未变的定向检查复用本轮结果；最终产物相关回放和 API 元数据按最终六项构建再次验证。最终构建日志为 `.tmp/native-presentation-20260911/build-*-final.log`。

## 清理与兼容

删除了重复的当前目标可用性分支以及会将旧原文写入新 NPC 的 fallback；统一到作用域消费 helper。公共旧 ForExternal 重载/查询保留二进制与源码兼容，更新其注释明确新 Overlay 不再依赖此查询。原 ports 全文对照仅新增精确的三行注释改写表，且强制只允许 `//` 注释，不忽略其它变化或放宽行为断言。

## 下一项与风险

- 本轮保护的是 Overlay 队列、提示、动画与收尾；TTS 引擎自身直接显示/播放回调仍须按其原生命周期单独核对，不能把 UI scope 当物理取消或动作授权。
- Native prepare 仍有后台游戏读取/历史操作，动作后的完整事实/记忆回执尚未完成。继续这两项后再评估有限公共提交。
- Courier 双向早期 prepare 仍待拆分。
- 实机 Campaign/Mission、同 NPC 关闭重开、普通/主动开场、旧存档、经济/AFEF、子 MOD 加载均待真实验收。

源码、产物、日志 SHA 与命令摘要见同名 JSON；原始日志保持在项目内 `.tmp` / 工具 `.generated`，不是打包实机证据。自动化仍有可执行代码工作，保持运行。
