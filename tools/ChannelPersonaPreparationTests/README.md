# 三渠道人设消费与信使准入

运行仓库实际 `MyBehavior.PersonaReadiness.cs`、`ShoutBehavior.PersonaPreparation.cs`、`src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PreparationAdmission.cs`、值快照与 `PersonaGenerationWaiter`。游戏/生成/渠道dispatcher接缝为替身；主线程是独立可观测执行路径，不把本测试当实机。

```powershell
G:/Python310/python.exe -X utf8 -B tools/ChannelPersonaPreparationTests/run.py
G:/Python310/python.exe -X utf8 -B tools/ChannelPersonaPreparationTests/run.py --original
G:/Python310/python.exe -X utf8 -B tools/ChannelPersonaPreparationTests/test_source_parity.py
```

- 当前169检查通过。原三消费者98项中65失败；对应三个原声明已逐个核实与main437925b8相同。新增准入、快照与pending-generation检查只对当前组件，不虚报同样在旧代码中执行。
- 42个原消费者场景：Native正常/已有/单字段/失败/超时/失效；Courier双方向同类；Scene双空生成、单字段回退、匿名、重复、档代/场景/候选替换。额外覆盖双向无效目标延迟清理、排队前失效、完成前换目标、生成Task仍pending时超时/退场。
- 10个 `--mutate` 变体必须编译成功再行为失败：native_skip_admission、native_accept_failure、courier_drop_session、courier_reject_fallback、scene_generate_partial、scene_skip_scope、scene_accept_replaced、invalid_target_cleanup、waiter_ignore_deadline、waiter_ignore_scope。
- 只在生成测试源码中将500ms轮询改1ms、180000ms等待上限改40ms；生产常量不变。Scene未新增失败超时策略，只退出失效场景/候选；共享生成可以继续为其他渠道填充人设。
- 全文件逆变换固定4140bd04，精确恢复Shout/Courier其他代码；新文件及fixture有审查哈希。原Native/历史/内部ports的完整对照不删弱、不刷新旧哈希来跳过差异。

真实Native主线程函数另由132项测试验证，Native原准备589项；Courier原owner phase另由独立执行回执测试和后处理回放验证。本套替身生成不代替HeroPersonaGenerationTests的125项实际生成owner验证。

范围不包括整个preprocess/lore/角色与库存消息准备、升格同伴技能、人设生产模型质量、完整GameEnd/预算、外部三渠道SDK或真实游戏/存档。180秒是协作式等待截止，主线程排队延迟不受本轮硬抢占；不等于HTTP取消。
