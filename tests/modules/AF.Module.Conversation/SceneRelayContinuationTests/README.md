# 场景接力回接边界

运行 `python tests/modules/AF.Module.Conversation/SceneRelayContinuationTests/run.py`。

从当前生产文件直接提取轮数/预算准入、候选筛选、后处理选择、下一目标校验和终止分支；编译真实接力标签规范化、运行时协议和规则 fallback。城镇预算链接真实 GCCZ 预算源码。网络、实时 Agent eligibility 与调度使用脱离游戏的确定性 fixture；不复制生产接力算法，不投影历史源码或刷新历史绑定。

默认两人 A→B→A 往返最多八次；新增覆盖可调1–8轮、越界值限制、包含首次回复、运行中限额冻结/下次喊话生效、主链路与后处理实际轮序，以及原有城镇人数预算和提前终止边界。每轮按一次NPC发言计，不按不同人物计。产物与源码指纹写入仓内隔离目录。

`settings_probe.py --api 1.3 --candidate <实际DLL> --run-root <新建artifacts目录>`（1.4同理）使用已编译产品、MCM原生属性发现/JSON formatter、实际终端注册/读写委托以及CN/EN本地化资源，核对默认8、范围1–8、原owner保存重载和旧配置缺字段。所有文件写入隔离夹具，不接触玩家设置。

只覆盖具名接力边界，不代表完整三渠道、真实游戏人物过滤、LLM选择质量、历史持久化或实机验收。
