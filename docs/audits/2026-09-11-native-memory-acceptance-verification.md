# Native 运行期记忆接受结果验证（2026-09-11）

代码/测试：`18f48678`；检查点：`c5de2186`；旧缺陷基线：`29ca75c9`。

## 结论

旧实际 Native tail + MyBehavior void 外壳已复现三种误报正常完成：owner 不存在、底层返回 false、底层抛异常。Native 现改用一个支持 sceneSessionId 的 internal 严格 owner，并检查 MemoryCommitResult；失败成为明确的记忆未确认状态，不重放动作、不抹掉部分记录。原 public 六参 facade 与 -1 loose 语义保留。

这不是第二套记忆存储。新严格入口逆变换方法名/scene 参数后与原 strict 方法逐字相同；原 Action core、AppendDialogueHistory、AppendDialogueHistoryById 完整声明未变。日记资格、AFEF 格式和数值没有修改。

## 最终验证

| 项目 | 结果 |
|---|---|
| 真实 Native tail → strict entry → fixture append | 184 检查 / 15 行为变异 PASS |
| 原动作派发 / 准入 / 展示 | 88/9、44/7、46/6 PASS |
| 制作组 ports | 13 方法 / 31 调用 / 308 断言 / 3 变异 PASS |
| Debug/Release × 1.3/1.4/Bootstrap | 六构建及项目内 Stage PASS，marker SHA 一致 |
| 实际四实现 DLL PE 元数据 | 532 PASS：原 472 保留，新增 60 个 legacy memory ABI/internal owner 检查 |
| Api.V1 外部调用/并发/拒绝编译 | 119 / 256 / 预期 CS0122 PASS，新 V1 仍只读 |
| 相关 16 组回归 | 经下述行号 fixture 复核后全部符合预期 |
| MemoryCommitRecoveryContractTests | PASS；未改恢复 ledger 或存档 key |

已验证正常/主动、Hero/非 Hero、原 scene 与 loose、非持久 NPC/空 payload、owner 缺失/false/异常、原资格与主线程门禁、部分记录不回滚、不正常回显/重复 TTS、失败后仍保留必要原会话关窗、两条 UI 的专用记忆提示。正常完整 pipeline 没有改走 detached 缩减路径。

## 真实失败与修正

PersistenceProfileConfig 首次报告 catalog drift。逐项比较确认 168 个 key/ref/type/source 身份不变，仅 MyBehavior 中 `_patienceStates_v1` 的两处来源行由 36469/36478 变为 36435/36444（抽取 strict 方法减少 34 行）。只更新文档 fixture 的 line，不改键、类型或断言；定向复验 PASS。原红日志/原回归 JSON 保留，最终审计以独立 recheck 记录解析，不覆盖历史证据。

原始日志在 `.tmp/native-memory-acceptance-20260911`。同名 JSON 包含源码、原 owner、六 DLL、日志 SHA、回归命令及行号变化；构建参数沿用框架台账。元数据只读 PE，不加载游戏 DLL。

## 清理与边界

删除 Native 对四个 void 外壳返回的成功假设，将原 strict 方法收敛到同一 scene-aware owner；旧六参 facade 为 ABI 兼容保留。关窗代码集中到同一原会话 helper，正常/记忆失败两条互斥分支调用，仍由一次 action claim 防重入。

**Applied 只表示运行期日记/最近历史接受，不等于磁盘、SyncData、跨动作原子事务或稳定请求恢复 receipt。** Game、底层 Append、实际 AFEF/日记与 TTS 仍有 fixture；没有进行本轮实机/旧存档验收。更早 Native prepare/失败清理、TTS 直接回调、Courier prepare 和完整公共请求生命周期继续后续。历史 .NET 10 工具未跑。

未推送、未部署、未操作真实存档、未改其他工作树；两份用户草稿保留。自动化继续。
