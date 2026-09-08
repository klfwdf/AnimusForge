# AF 重构断线恢复：缺陷修复与验证

## 结论

本轮已经恢复断线修改并实施代码修复，不再只是检测文档归档。**这是一份修复候选验证报告，不是整个重构项目 DONE 或正式发布证明。** 自动化保持暂停；未推送、未部署、未操作真实存档。

基线为 `5ce8767a`，历史缺陷证据仍保存在 `docs/audits/2026-09-08-full-refactor-audit-35524b04.md`，没有改写原红证据。最新代码提交见同日 HANDOFF。

## 本轮实际改动

| 范围 | 修复内容 | 证据边界 |
|---|---|---|
| F1 Economy | 指定资产 ALL 不再转走其他资产；保留 modifier、RP literal、notable/market 的正确资产 owner 和 unknown-after-start 语义 | 真实方法＋库存/游戏转移边界 fixture；未动真实资产 |
| F2 Courier Prompt | 匿名 role/content 消息不再静默丢失；保留顺序、正文/历史、人设上下文和 5000 token 预算；默认回信复用已准备 Prompt，不再由 Host 重建 | 真实 adapter/ports 和生产 DLL；原 builder 更早的后台读取问题另列 |
| F3 Courier owner | 保留完整 WorkItem completion，在主线程做领域归一化，再解析 ActionPlan；无 owner 不授予动作；取消、过期、同 ID Hero 替换和重复回调拒收 | 主线程调度、真实 WorkItem/parser，领域重型准备本身仍需实机 |
| Courier 可见正文 | 正文清理与后处理 raw 分开；嵌套 RichText 资产标签、GUI/ASS/RELAY 不进入共享历史；空/错误/仅标签回复不作为正常回信提交 | 真实平衡协议解析器、正文清理和 coordinator |
| F4/F5 Scene | 输入首次 await 前冻结 Mission/player/session/epoch/序号/目标；旧输入不清新场景；普通回退保留原框选；同 gate waiter 不丢 busy 恢复职责 | 真实入口、反射桥、Task 与受控主线程；不是完整游戏尾部 |
| BattleSpeech 成功路径 | classifier 完成和 StartSession 前校验同一个 opaque 请求，拒绝被新 UI 取代的旧演讲；缺新能力的旧 host 保留普通 observer | 真实分类续体和桥；真实演讲/士气/移动未验收 |
| F6 TTS | RequestId＋请求/代次取消贯穿队列、网络、事件及 Native/Scene 消费；保留合法同 Agent FIFO；旧回调不消费新 wait；终态释放 owner | 完整 TtsEngine 和真实消费方法，网络/音频/游戏边界 fixture |
| Native TTS 兜底 | 延迟后只排主线程；原子校验 owner/request/token 再放行文字，修复 A 检查后 B 插入的竞态 | 独立真实方法红绿与竞争注册测试 |
| Gateway | 精确识别 transport 错误 envelope，拒绝取消/读档后的晚成功；旧 facade 与流式方法保持不变 | 真实 Gateway＋传输 stub，不调用在线 API |
| Bridge/清单 | mandatory safety 不再伪装可选开关；16 个定义中 12 wired / 4 declared-only；渠道状态明确为混合默认 | 不修改安全补丁及安装入口，不实施新默认切换 |

## 正式测试和构建

最终命令、结果、源码和 DLL 指纹见同日 `2026-09-09-closeout-verification.json` 及本地 `.tmp/closeout-20260909/`。

- 官方 Debug/Release × 1.3/1.4/Bootstrap 六项 Stage 构建均 PASS（0 warning / 0 error）；仅项目内输出。引用版本分别为 `v1.3.15.110062`、`v1.4.6.115628`。
- 既有 41 个 C# 测试项目：37 个完整 runner 全部 PASS；PolicyEffect 四个安全子集共 1056 断言 PASS；3 个 SDK 10 工具单列环境阻塞，不能算通过。
- Python 26 个检查通过；另 1 个缺失实机证据样例按预期 BLOCKED/exit 2，不能当成发布通过。
- 新正式回归：Hero 67、Courier owner 39、Scene request 30、TTS 43、Gateway 40、Native fallback 14、BattleSpeech trigger 18 个场景；另有 async owner 18、匿名消息 13 个契约案例。
- 旧版反例和定向 mutation 保留；预期 exit 1 的红测表示坏状态被检测，不是生产失败，也不计作游戏验收。
- 三个旧 C# 测试已按当前真实行为更新，并保留破坏性反例：GiveAsset codec 的共享调用链、终端导航与布局/周报绑定分开、外交 commitment 的 action schema。没有恢复已删死 helper 来迎合旧断言。
- 两个生产 Host 回放显式选取仍保留的七参数构造；Courier 无 Campaign 的回放验证“无 owner 就无后处理/动作”，不再虚构第二次 HTTP。有效 owner 的闭环由独立真实方法套件覆盖。
- SyncData 仅刷新 2 个行号；168 个 key/ref/type/source 绑定不变。新 partial 已做入口清单复核；`entryCoverage=COMPLETE` 仅指文件入口盘点，不代表线程、功能或实机完成。

初次集成中的旧反射断言、Stage 新鲜度、fixture 缺 TTS 外部字段、清单漂移与临时 runner 编码问题均保留在 `pre-final-review/`；以最终结果索引为准，不将这些日志悄悄改成旧版 PASS。NuGet 元数据、旧 net6 目标及提取 fixture 字段警告不隐瞒；生产构建状态单列。

## 清理与保留

移除了旧全局 TTS 取消位、无请求身份的内部订阅/等待匹配、重复 detached Speak 重试、缩减的 Courier 后处理 Prompt 路径和无调用的 runtime-game-adapter accessor/无效配置项。

仍保留活跃 Native 旧默认、公开兼容 API/旧事件、存档兼容读写、共享 facade 及未完成迁移的领域 owner。不能凭名字带 Legacy 或文件很大就删除；不可达旧群组代码和 `#if false` 残留仍在后续清理范围，不宣称“旧代码全删”。

## 未完成的项目门槛

1. **Courier 前置准备线程边界仍 NOT_FIXED。** aux 规则、人设落盘、历史/记忆选择混合游戏读取与网络；Lore/semantic 的本地 ONNX 计算也需区分线程和耗时。详细链路及设计见 `2026-09-09-courier-thread-boundary-plan.md`。不把整个 builder 塞进主线程制造卡死式“修复”。
2. 同步 Action API 没有 cancellation 参数：运行中网络无法在该 Gateway 内中止，目前只拒收晚结果。后处理 HTTP 失败时，旧 MOOD fallback 与新空 ActionPlan 尚有兼容差异，未证明实际可见功能受损，也不宣称完全等价。
3. Native 完整流式/主动开场等价接入、其余领域最终 owner/动态调用清理仍需逐项对照。当前是 Native 旧默认、Scene 新正文＋完整场景后处理、Courier 回信 detached 默认的混合状态。
4. 本候选的 Campaign/Mission、真实 Hero/Party/Merchant/债务、AFEF、旧档读写、实际语音/口型、第三方模组和安装回滚未验收。用户曾反馈其他成员已测通过；没有绑定到本轮新增修复候选的材料，不能把反馈扩张成本候选全通过。
5. 当前本地 SDK 8 实际构建三个 net10.0 工具返回 NETSDK1045；没有为此全局安装 SDK。PolicyEffect 不运行默认全套、真实 ONNX 或外部持久化路径。

下一步优先闭合 Courier 前置准备设计，再完成 Native/领域等价矩阵和有证据的旧路径清理；最后对同一个候选做真实验收、打包/回滚及明确授权后的发布。此顺序不被本轮通过的测试数量替代。

环境说明：一次隔离 Policy 编译属性查询启动了新的 CLI home，SDK 输出开发证书安装提示；未检查证书存储，不能保证没有 SDK 自动的用户级初始化。后续已复用工作区 CLI home 并禁用开发证书生成，详见本地 Policy 报告。
