# AF 主体重构：阶段 7 / 阶段 8 制作组简报

日期：2026-09-01

## 六、最准确的结论

可以这样回答：

> **是的，阶段 7 作为“模块接入阶段”，大部分工作已经完成，模块、Gateway、owner、ActionPlan、
> Memory 与主要失败边界基本接上了；但阶段 7 作为完整交付阶段还没有完成，因为真实游戏验收、
> 旧存档、live Economy、AFEF 和默认入口仍未验证。**

因此：

> **可以进入阶段 8 的准备和设计工作，但不能把阶段 7 标记为 DONE，也不能直接进行阶段 8 的
> 破坏性清理或默认路径切换。**

## 当前推荐顺序

### 阶段 7

- 模块接入 + 离线 / 等价 Host 验证：**基本完成，当前为 VERIFY**。
- 真实 Campaign/Mission Host、旧存档、live Economy、AFEF：**待验收**。
- 默认 Native / SceneShout / Courier 入口：**尚未统一切换**。

### 阶段 8 准备（现在可以并行开始）

- Bridge 矩阵与各领域 owner 边界。
- 旧 facade / bridge / feature flag / compatibility shim 清理候选。
- 每项候选的替代证据、回滚提交、数据影响和真实验收要求。
- 最终验收包、打包清单和发布门禁。

### 阶段 8 执行（现在不能开始）

- 删除旧 facade 或仍有调用/反射/存档责任的兼容代码。
- 切换默认三渠道入口。
- 覆盖游戏、最终打包发布和宣布阶段 7 / 8 DONE。

这些操作必须等真实 Host、旧档、live Economy、AFEF 等硬门禁完成后再做。

## 已完成的主要技术闭环

- Native / SceneShout / Courier 共用 detached LLM 管线、Gateway、Prompt、后处理与ActionPlan边界。
- Hero / Party / Merchant Economy owner，known partial与`UnknownAfterStart`结构化回执。
- 请求级commit防重、Memory-only durable recovery、Courier inbound completion。
- Economy-only weekly exact outcome（`AFWM1`）与detached Notoriety exact line/session witness（`AFNR1`）。
- Duel actual-session typed outcome/readback：三条结算路径已接入，成功绑定时先锁胜负再记录分量；
  legacy detached dispatch保持`UnknownAfterStart`，exact request provenance仍由`LOCAL-7-M2`补齐。
- 1.3 / 1.4 / Bootstrap 的Debug与Release项目本地Stage构建通过；主要contract/production回放通过。

以上证明的是**当前源码、受控Host和compiled边界**，不是实机游戏验收。

## 制作组成员接下来交付什么

1. 每个领域给出真实入口、owner、Prompt/ActionPlan、save key/type与失败降级。
2. 提供Campaign/Mission实测步骤、前后状态、PASS日志和可重复存档。
3. 验证旧档加载、缺失/损坏数据隔离和保存后重载。
4. 对每个清理候选提供“替代路径已通过”的证据和`git revert`回滚点。
5. 没有真实证据的项目继续写 `NOT-RUN / VERIFY`，不要用截图、fixture或DLL加载代替。

## 一句话结论

> **我们现在可以“并行准备阶段 8”，但不能“跳过阶段 7 的真实验收直接收尾阶段 8”。**

最新技术交接：`docs/handoffs/2026-09-01-duel-actual-session-outcomes.md`。
