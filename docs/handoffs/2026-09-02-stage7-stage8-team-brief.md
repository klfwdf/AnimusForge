# AF 主体重构：阶段 7 / 阶段 8 制作组简报

日期：2026-09-02

## 六、最准确的结论

> **阶段7作为“模块接入与离线验证阶段”，大部分工作已经完成：统一Gateway、三渠道管线、
> ActionPlan、Memory/AFEF边界、Economy owner、请求回执以及Duel exact provenance都已接入并通过
> 相应离线/compiled验证。阶段7作为完整交付仍未完成，因为真实Campaign/Mission、旧存档、
> live Economy/AFEF、Duel真实副作用和默认入口尚未验收。**

因此：

> **可以继续阶段8的Bridge矩阵、清理候选、回滚与验收包准备；不能把阶段7标为DONE，也不能
> 删除旧facade、切默认路径、覆盖游戏或直接收尾阶段8。**

## 当前推荐顺序

### 阶段7

- 模块接入、契约、离线/等价Host与compiled回放：**大部分完成，整体仍为VERIFY**。
- Duel M1/M2：actual-session结果owner和exact detached request-to-DuelId均已离线`LOCAL-PASS`。
- 五个managed production replay（含Shout SSE）已统一显式依赖owner，不再由consumer硬编码盘符或扫描全部Modules。
- 真实Host、旧档、live金币/物品/商人/债务、Memory/AFEF、Duel死亡/赌注/Fourberie：**待验收**。
- Native / SceneShout / Courier默认入口：**尚未统一切换**。

### 阶段8准备（可以并行）

- 20领域owner、16组Bridge和完整真实入口清单。
- 每项旧facade/bridge/flag/shim的KEEP/HOLD/REVIEW_REMOVAL证据。
- 逐项回滚点、存档副作用说明、LIVE/SAVE验收记录和最终打包清单。

### 阶段8执行（当前禁止）

- 删除仍有调用、反射、存档或兼容责任的旧入口。
- 切默认三渠道、打包发布或覆盖游戏。
- 在LIVE/SAVE证据不完整时宣布阶段7或阶段8完成。

## 本轮新增闭环：Duel M2

- canonical request/trace/channel/session/subject/runtime/save/action fingerprint在副作用前绑定唯一DuelId。
- Native/Scene精确区分Rejected、Queued、Started、UnknownAfterStart；Courier明确拒绝。
- Queue先于Economy/gameplay；duplicate/conflict/capacity/load全部fail-closed，不fallback、不重放。
- 三条结算路径先记录同一result receipt，再进入Memory、renown、stake/death等副作用。
- contract 16/16 + outcome 18/18，Production Duel Debug/Release各35/35，1.3/1.4/Bootstrap
  Debug+Release均0 warning / 0 error；这些仍不是实机验收。

## 制作组成员接下来交付

1. 每个领域登记真实入口、owner、Prompt/ActionPlan适用性、save key/type和失败降级。
2. 在隔离存档上提供Campaign/Mission步骤、前后状态、BuildInfo、DLL hash、PASS日志和回滚点。
3. Duel重点覆盖accept/reject/queue/start/cancel/death/exit、stake/debt、Memory/AFEF和Fourberie。
4. 验证代表旧档加载、保存后重载、缺失/损坏数据隔离，不用fixture或DLL加载冒充SAVE PASS。
5. 每个清理候选先证明替代路径和rollback drill；没有真实证据继续写`NOT-RUN / BLOCKED`。

## 一句话结论

> **现在可以并行完善阶段8准备，但不能跳过阶段7真实验收直接做破坏性清理、默认切换或发布。**

最新技术交接：`docs/handoffs/2026-09-02-shout-sse-replay-dependency-closure.md`。
