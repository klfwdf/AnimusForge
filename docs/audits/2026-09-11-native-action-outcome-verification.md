# Native 动作派发结果验证（2026-09-11）

生产/测试提交：`8da4fbd7`；检查点：`861dd7a7`；旧反例基线：`646dd987`。

## 结论

实际旧派发方法已复现「部分效果后抛错仍按成功收尾」及「日志异常让回复提前结束但队列动作随后执行」。新实现不再返回预建成功 fallback；direct/queued 使用同一执行边界，claim/完成归属唯一。类型化失败沿真实 await 传播到两个 Overlay 失败分支，不自动重放。

原 `ApplyNativeConversationGameActionsCore` 完整声明与 `646dd987` 逐字相同；没有重写规则、数值、标签或制作组业务。已有准入/展示保护和正常返回语义保留。

## 验证

| 检查 | 结果 |
|---|---|
| 新动作派发 | 59 检查 / 6 行为变异 PASS |
| 原 Native 准入 / 展示 | 44 / 7 变异、46 / 6 变异 PASS |
| 制作组 ports | 13 方法 / 31 调用 / 308 断言 / 3 变异 PASS |
| Debug、Release × 1.3、1.4、Bootstrap | 六构建及项目内 Stage PASS，六 DLL 与 marker SHA 一致 |
| Scene 后处理 / 队列 | 71 + 2、37 PASS |
| Channel / Courier owner / Native TTS fallback | 132、39、14 PASS |
| 管线、BridgeRuntimeIsolation、四组生产回放 | PASS |
| BridgeBinding / PersistenceProfileConfig / Entry inventory | PASS；168 存档绑定未变 |
| 公共 API | 119 / 256 并发 / 预期 CS0122 / 四 DLL 472 元数据断言 PASS |
| 缺失实机证据示例 | 预期 BLOCKED / exit 2，未把离线 PASS 提升为实机证据 |

测试执行真实派发/异常边界及实际消费分支；队列、游戏对象、业务 Core 和最后历史存储为明确 fixture。历史计数只证明正常完成分支是否被越过。生产回放、元数据和源码片段都不是实机/旧存档验证。详细命令见工具 README；原始日志在 `.tmp/native-action-outcome-20260911`，SHA 与产物路径见同名 JSON。

## 清理与兼容

移除原队列异常返回正常 Content 的 fallback、重复 direct/queued 业务异常处理；诊断异常在独立观察边界处理。原准入测试只把 guard 变异转到迁移后的真实 helper，展示 UI 校验从每入口 7 增至 8，原断言仍保留。ports 对照仅刷新无模块 receiver 的派发方法 SHA，完整业务调用仍作反向全文对照。

## 限制与下一步

- NoConfirmedEffect 仅表示本次派发未进入 owner，不表示整个回合没有更早副作用。
- UnknownAfterStart 不回滚或否认已发生效果；正常同步返回也不证明所有异步玩法效果最终成功。
- 未消费动作队列的超时/取消、成功路径主线程完成与事实回执、Native prepare/TTS 直接回调、Courier prepare 待后续。
- 实机、旧存档和历史 .NET 10 工具未验证；公共 V1 仍只读。
- 未推送、未部署，保留两份用户草稿和其他工作树。自动化继续。
