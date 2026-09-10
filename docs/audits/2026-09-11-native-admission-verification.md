# Native 准入批次验证（2026-09-11）

生产/测试提交：`77d4a940`；开始前检查点：`e109608d`；缺陷反例基线：`14dec2d7`。

## 已修改

- 旧普通输入和 NPC 主动开场入口共用主线程准入；捕获目标、AgentIndex、Mission、ConversationManager/ActiveToken、save generation 与 conversation epoch，再进入原完整后台流程。
- 后端忙碌不影响 Overlay 是否存在；已结束会话的排队请求也被拒绝，不消费重新开场的状态。旧请求只能释放自己的票据。
- 原动作前六个校验点及实际动作队列执行点复查票据；不绕过既有标签/资格判定。
- Overlay 的拒绝分支保留输入/开场；旧或关闭界面的 finally 不再清除新界面的全局流式/busy，也不恢复旧输入。
- 删除两个重复的“先 Task.Run 再捕获”入口块，以及失去所有权后仍恢复全局输入的收尾分支。保留完整原 Native 业务体与公开旧重载；新 V1 提交依然 NotSupported。

## 复现与定向验证

| 证据 | 结果与边界 |
|---|---|
| 原入口/捕获前缀执行 | 2 个缺陷控制：并发均进入，晚捕获变为 B:99；下游业务 stub |
| 排队后结束会话的补充反例 | 新准入初稿曾接收新会话；加入 epoch 后在捕获前拒绝，避免消费新主动开场 |
| NativeConversationAdmissionTests | **44 PASS / 0 FAIL**；真实准入源码、实际入口/动作队列及 UI finally 片段执行 |
| 行为 mutation | **7/7 被拒绝**；包括旧 UI finally 和跳过排队 epoch，均非编译失败冒充捕获 |
| 制作组 ports | 13 签名、31 调用、308 断言、3 个原变异通过 |
| 保存身份 | 168 个 key/ref/type/source 不变，仅刷新 MyBehavior 插入行导致的 50 个行号引用 |

记录器 fixture 仅代替游戏对象/后续业务，不声称 Native 整条游戏链已经全部测过。生产超时仍为 30000ms；测试隔离时缩短为 40ms。

历史 ports 全文逆变换没有删除断言：5 个不包含任何 TeamModuleServices 调用的 Native/ConversationEnded 声明，分别以精确 SHA 冻结为已审阅差异；只有 hash 一致才还原后检查整个旧文件。其它变动依然失败；新声明另受上述行为测试负责。

## 构建与回归

**最终源码 Debug/Release × 1.3/1.4/Bootstrap 共六项构建全部通过，0 warnings / 0 errors，只 Stage。** 最终日志是 `build-*-delivery.log`，不是较早原型的 `build-*-final/verified.log`。

本轮相关回归全部通过：Scene 71 + 2、队列 37、Channel 132、CourierOwner 39、Native TTS fallback 14、InteractionPipeline、BridgeRuntimeIsolation、四个生产程序集回放、BridgeBinding、PersistenceProfileConfig 与 entry inventory。缺证据示例仍按预期 BLOCKED/exit 2。

最后 epoch 修改只涉及新准入 partial；与其无关且源码未变的既有定向检查复用本轮结果。**四个生产程序集回放与公共 API 按最终 Stage 再跑**：公共 API 119 断言、256 并发查询、预期 CS0122 和最终四 DLL 的 472 元数据断言通过。不能把元数据相同当成 CLR/游戏加载验收。

源文件、最终产物和本地日志 SHA 见同名 `.json`。原始日志位于 `G:\AFMOD\AF-REFACTOR\.tmp\native-admission-20260911`，保留原反例、初构建缺 namespace 和中间反例证据，不覆盖为绿色日志。

## 仍未完成

- Native 全部 prepare 的游戏读取/人设/历史操作线程归属、动作后完整事实/记忆回执与原子完成。
- 流式及完成回调与捕获会话的全面绑定；本轮只是修复旧 finally 对新 UI 的干扰，不代表所有展示竞态都结束。
- 物理网络取消、完整 TTS 展示完成回执以及稳定的有限公共提交契约。
- Courier 双向更早 prepare 的线程分拆。
- 新版本的真实 Campaign/Mission、旧存档、经济/AFEF 以及独立子 MOD 加载验收。

没有改额外模块业务、默认渠道路由、程序集/存档身份或构建脚本。未推送、未覆盖游戏；两份用户草稿未提交。每小时自动化仍有可执行代码工作，继续按总 HANDOFF 推进。
