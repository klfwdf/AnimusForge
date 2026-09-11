# Native 初始场景准备边界

## 已接上的实际位置

完整 Native 请求在 persona await 之后，需要构造 NPC 数据、文化、已有会话历史标记、会面/场景挑衅规则、传唤与带路候选、前处理规则排除表。此前先在后台构造 NPC，再只排队验证目标，返回后台继续读取场景。

现在原 `request_target_validation` 消费中直接调用 `CaptureNativeConversationPreparation`：

1. 验证在游戏主线程，并调用原 admission 守卫；失效不读取 NPC/场景、不求值规则。
2. 按原函数、原参数和原顺序准备这组数据，返回 private 准备包。
3. 后台只解包同名局部变量；原历史 Task、周报快照、前处理和后续完整正文/动作/记忆链继续原接线。
4. 队列超时由已验证的共用 runner 处理：仅未开始工作会过期，晚到不补做。没有第二次“验证通过后再切回后台读场景”。

## 保持的内容

Hero / Character、场景与无 Agent 的会面、文化 neutral fallback、已有历史标记、Lord 优先/Scene fallback 的挑衅规则、GCCZ 既有资格守卫、原 presentNpcs / resolvedHeroes 参数、传唤最大 PromptId + 1 的带路编号、原规则排除输入和输出都保持。

原生对话 UI 文本仍不作为 AF 历史。主动开场 extraFact 与玩家 routingInput 不截断、不另写一套提示词。没有修改 GCCZ/政策/宴会规则或业务实现。

**准备包不是公共 DTO，也不是完全脱离游戏对象的 immutable snapshot。** 复用的 SceneSummonPromptTarget / SceneGuidePromptTarget 仍含 LocationCharacter / Location 引用；它只把本段构造放到主线程，不代表后续所有读取都安全，不能直接对第三方公开或长期缓存。

## 性能与兼容

复用原队列消费，不增加额外 queue hop、Tick 或全角色扫描。每个 Native 请求新建一个小的 private 容器，复用原函数新建的列表；原本已有的候选查找现在在正确线程执行。没有增加源函数扫描次数，但主线程耗时仍需游戏实测；不要把有网络/检索的整个 prepare 塞进这里。

不改 public API、Bootstrap、程序集身份、存档键、默认渠道。原 Npc/候选/规则 helper 仍有实际调用，必须保留；删除的是当前 Native 请求里被替代的后台准备片段，不是删除那些共用 helper。

## 对照证据

`tools/NativePreparationBoundaryTests` 同时执行原始健康准备片段与候选实际片段，对比 48 组 Hero/NonHero × Scene/Map × 分支输入的全部返回字段和 helper 调用顺序/参数；589 检查 / 5 个行为变异。

实际共用 runner 被提取执行；游戏 helper 与 admission 判定是记录型 fixture。原 admission 的生命周期行为另由 44 检查验证，静态接线仍核对 6 个原位 + 1 个捕获内守卫，不能将 fixture 的 Current bool 冒充真实存档代际。

原准备块逆变换后，整个 Submit 与 ShoutBehavior 全文等于 `50f84818`；新捕获主体也与原 builder 语句逐字对照。Team ports 的精确 SHA 审批和全文逆变换保留。共用 runner suite 仅放行这个独立验证过的声明，并未取消全文断言。

## 下一步尚未完成

实际持久历史仍走 `Task.Run -> BuildNativeConversationPersistedHistoryContextForPrompt -> MyBehavior.BuildHistoryContextById`：

- 上游解析当前 Hero/非 Hero/部队记忆身份与场景发言。
- MyBehavior 的 LoadCompressedMemoryBlocksById / LoadDailyMemoryDraftsById 返回 owner 的可变列表；总览也读当前状态。
- BuildCompressedMemoryContextById 还调用召回候选与前处理选择，不能把整个 builder 搬到主线程。

下一轮应先拆开当前 owner/游戏读取与可独立的记忆输入，追明召回/缓存副作用后再设计副本和返回处理，不可删掉检索或缩减记忆换取线程安全。persona 更早读取、独立周报快照的会话绑定、TTS 直接回调、Courier 双向 prepare 也未由本轮解决。Api.V1 仍只读；整个阶段 8 未完成。
