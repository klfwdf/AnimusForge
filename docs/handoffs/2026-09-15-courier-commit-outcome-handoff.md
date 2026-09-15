# AF 全范围收尾：Courier 不确定提交结果补充

**当前生产/测试 `518448004b1fe5983651e8e501ad60040004617c`；阶段8连续收尾，整体仍ACTIVE。** 起点29448d1b，checkpoint dd33a7d2；目录G:/AFMOD/AF-REFACTOR。最后确认已推送GitHub的仍是10defeb4，本包只在本地。

本说明补充并更新[Game生命周期详细HANDOFF](2026-09-15-game-lifetime-closeout-handoff.md)，不把这项修复冒充整个项目完成。

## 改了什么

已进入Courier最终commit后抛异常或返回空回执，旧分类是 `RejectedByValidation / NoConfirmedEffect`，不能准确表达可能已有部分副作用。现在是 `NonRetryableFailure / UnknownAfterStart`：不自动重试整轮，不伪造已成功的历史/动作，不声称已全部回滚。

- 开始前的null callback、队列拒绝、退休和未claim超时保持原拒绝语义。
- 实际成功回执原样返回；入站缺回执清理保留已有 `EffectState`，不抹掉确认成功或不确定的效果状态。
- 日志失败不改变回执，也不让内联调用和排队调用表现不同。
- 没改原信使会话/送达/资产/后处理业务，没增加第二个动作执行器或开放旁路API。

## 代码位置（51844800，一基行号）

| 位置 | 符号 |
|---|---|
| `CourierDeliveryBehavior.CommitDispatch.cs:16-20` | `private static InteractionCommitResult CreateUnconfirmedCourierCommit` |
| `CourierDeliveryBehavior.CommitDispatch.cs:22-97` | `private Task<InteractionCommitResult> DispatchCourierRefactorCommitAsync` |
| `CourierDeliveryBehavior.CommitDispatch.cs:99-148` | `private InteractionCommitResult InvokeCourierRefactorCommit` |

调用关系：两个派发路径都经过原 `InvokeCourierRefactorCommit`；其中真实callback异常/空回执使用新的私有分类方法，入站清理只转译错误码和保留效果状态。实际 `DetachedInteractionHost` 消费该回执时终止而不调用legacy fallback；此消费链已用本次1.4 DLL回放。

## 验证

| 层级 | 结果 |
|---|---|
| 原队列/退役行为 + 新结果分类 | 34检查＝原19＋新增15，通过 |
| 旧红/故障反例 | 固定29448d1b真实旧声明运行失败；4个编译成功的行为反例有效 |
| 邻接 | 原owner phase16、后处理39、内部ports308+3反例通过；全文件逆变换保留原证据链，不刷新旧生产hash |
| 同候选构建 | Debug/Release × Bootstrap/1.3/1.4，六Stage通过，未覆盖游戏 |
| 公共接口/产物 | API119、快照32、4DLL元数据728通过；公开ABI未变 |
| 实际1.4 DLL | 原Courier Host回放＋新不确定回执→真实Host消费后不重试，通过；游戏/网络端口仍为fixture |

[审计JSON](../audits/2026-09-15-courier-commit-outcome-verification.json)记录源提交、6DLL hash和17份冻结日志。当前代码地图125点。命令见 `tools/CourierCommitOutcomeTests/README.md`。测试首次空callback排队等待假设错误已修复，失败日志单独保留，未计作生产旧红。

## 全范围还未结束

本包与此前三渠道人设、GameEnd/待办退役修复已完成对应离线验证，但以下仍是必交，不能用历史实机反馈替代当前候选：

1. 规则/lore和角色、库存、消息构造的线程边界；共享My Prompt方法770行及AIConfig候选资格、KnowledgeLibrary内部仍混合游戏读取与同步网络，需完整拆捕获/后台/接受，不将整段网络搬主线程。
2. B1记忆首次深复制、全积压和终步实际成本预算，及剩余主体owner拆薄和逐符号清理。
3. 制作组→主体双向稳定服务；外部Native/Scene/Courier全部提交、结果与取消SDK。Api.V1当前仍只读，不能标完成。
4. 对固定main主体功能全面核对，以及当前候选真实Campaign/Mission、旧存档、live Economy/AFEF、独立子MOD验收。政策/宴会/GCCZ业务不扩围。

无推送、无部署、无存档操作、无默认切换、无自动化恢复。两份9月6日用户草稿及9月11日本地专用简明版hash不变；本地新的简明稿为 `.tmp/courier-commit-outcome-20260915/team-handoff.md`。回滚用针对51844800的逆向提交，不能hard reset或强推。
