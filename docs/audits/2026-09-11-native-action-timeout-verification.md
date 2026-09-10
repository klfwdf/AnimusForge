# Native 未消费动作队列验证（2026-09-11）

生产/测试：`9a5335be`；检查点：`841e8751`；前序派发失败修复：`8da4fbd7`，交接：`b7128a7d`。

## 实际解决

旧派发方法在没有主线程消费时一直 pending。新增用例先在旧代码上产生 `FAIL unconsumed queue settles without a pump`，再改真实派发器：复用 30 秒预算，仅 CAS 从 queued 转到 expired 的请求可超时；其 callback 晚到也不执行。已 claim 请求继续等待真实成功/失败，不因时钟提前释放结果或重试。计时器结束时取消/释放，日志失败不能改变结果。

`native.actions.dispatch_timeout` 是明确的队列状态，不从任意 TimeoutException 猜测。原业务 Core 与前序实现逐字相同；没有改制作组玩法、三渠道规则、默认路由、API 能力或存档键。

## 证据

- 真实源码定向 **88 检查 / 9 行为变异 PASS**；保留原 59 项，新增未消费等待、晚到 callback、跨期限物理线程成功/失败、守卫超时误分类等检查。
- 原准入 **44 / 7 变异**、展示 **46 / 6 变异**、ports **308 / 3 变异** 全部通过，原断言不减。
- **Debug/Release × 1.3/1.4/Bootstrap 六项构建与 Stage PASS**。四实现 DLL 的公共 API 472 元数据断言 PASS；六产物与 marker SHA 一致。
- 16 组相关回归全部符合预期：管线、隔离、四组生产程序集回放、桥接绑定、168 存档绑定、入口清单、Scene 后处理/队列、Channel、Courier owner、Native TTS fallback、公共 API、缺失证据门禁。
- 缺失证据示例仍是 BLOCKED / exit 2；这是成功保留验收门禁，不代表已有实机证据。

原反例、修复前红日志和最终绿日志分开保留在 `.tmp/native-action-timeout-20260911`。同名 JSON 含源码/DLL/日志 SHA 与回归命令，构建参数复用框架实施台账。只缩短 fixture 的时钟预算；业务 Core、游戏对象、最终历史写入仍是明确 stub，不冒充实机。

## 清理 / 风险

替换了后台动作派发无限等待返回；没有留下可同时运行的旧等待路径。ports SHA 仅刷新已批准的无模块 receiver 派发声明，完整反向对照继续通过。

该期限不负责终止已经开始且卡死的游戏动作；主线程完全卡死时，也不能保证立即显示 UI。它不回滚此前 raw/taunt/自然动作或已经记录的事实，不撤销已播放/已流出的正文，更不是物理网络取消。Native 成功后的主线程事实/记忆回执、更早 prepare 与直接 TTS 回调、Courier prepare 仍待继续。历史 .NET 10 工具未跑。

本轮未推送、未部署、未操作真实存档、未改其他工作树；两份用户草稿保留。公共 V1 仍只读，自动化继续。
