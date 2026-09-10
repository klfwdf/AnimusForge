# 初版框架验证记录（2026-09-11）

## 结论

**本轮生产构建与所选离线回归通过；真实 Bannerlord、新 API 子 MOD 的游戏内加载/升级、live Economy/AFEF 和旧存档没有在本轮执行。** 这不是 release readiness 或整个重构 DONE 证明。

原始日志保留在 `G:\AFMOD\AF-REFACTOR\.tmp\framework-v1-20260911`，新工具日志在各自 `.generated` 目录。精确生产提交 `a616958c`；源码与 DLL SHA、命令和日志 SHA 见 `docs/audits/2026-09-11-framework-v1-verification.json`。

## 生产构建

| 配置 | 1.3 实现 | 1.4 实现 | Bootstrap | 输出 |
|---|---|---|---|---|
| Debug | PASS | PASS | PASS | 项目内 Stage |
| Release | PASS | PASS | PASS | 项目内 Stage |

六次均 0 warnings / 0 errors。参考版本：1.3 `v1.3.15.110062`，1.4 `v1.4.6.115628`。六份 DLL SHA 均与各自构建 marker 一致。未覆盖游戏；构建脚本、程序集名与 Bootstrap 布局不变。

## 新增边界验证

- InternalModuleDirectory：44 PASS / 0 FAIL；Link 真实生产源码，覆盖冲突、版本、依赖、环、未就绪、门禁、不可变快照和并发。
- ModuleFrameworkApiTests：119 assertions、256 次并发读取；独立外部 client 编译；越权引用 internal 预期编译拒绝 `CS0122`。隔离 Host stub，非游戏运行。
- TeamModulePortParityTests：308 assertions，13 个方法的参数/ref/out/默认值/返回和同实例异常转发；3 个行为 mutation 被拒绝。调用方全文逆变换检查保留参数和顺序，不只是查一个方法名。
- 实际 Debug/Release × 1.3/1.4 四份实现 DLL：472 个 PE 元数据断言 PASS，公共 V1 签名一致、内部 ports/adapter/目录未外露。没有加载 DLL，元数据对照不等同于游戏内装载验收。

## 既有流程回归

| 工具/责任 | 本轮结果 |
|---|---|
| ScenePostprocessParity | 71 场景差分 + 2 完成控制 PASS |
| SceneDeferredQueue | 37 PASS |
| Scene 原变异控制 | 5 + 7 全部在运行时被杀；不是编译失败冒充捕获 |
| Scene 提取检查 | 8 PASS |
| CourierPostprocessOwner | 39 PASS，目录无需改动 |
| ChannelCutoverBoundary | 132 PASS，目录无需改动 |
| InteractionPipelineContractTests | PASS；含异步 owner 18、匿名 Prompt 13、完整提交边界/回执 |
| BridgeRuntimeIsolationTests | 12 process scenarios PASS |
| ProductionOptInEntryReplayTests | Native/Scene/Courier + 实际程序集相关回放 PASS；不切默认 |
| ProductionCourierHostReplayTests | PASS |
| ProductionDetachedHostReplayTests | PASS |
| ProductionEconomyAwareCommitReplayTests | PASS；不是真实金币变更 |
| BridgeBindingContractTests | 16 定义、12 wired / 4 declared-only PASS，旧门禁不变 |
| PersistenceProfileConfig | 168 binding / 142 key 身份不变；只刷新 52 处源码行号 |
| Phase8 entry inventory | PASS，无需更新入口 catalog |
| missing-evidence 示例 | 预期 exit 2 / BLOCKED；证明缺验收不会误判 READY |

Scene 旧 Harness 现在 Link 本轮真实 typed ports/adapters/services，原断言保留；为未覆盖业务新增的 stub 明确抛错。没有把缺失 owner 返回默认值来凑 PASS，也没有削弱 mutation 的失败条件。

## 本轮清理与保留

- 被迁移的主体接缝不再直接调用模块业务，而由 3 个内部单例薄桥承接；31 处调用、13 个方法均有实际引用。
- 未产生第二套网络、动作或记忆实现；未引入未使用 handler、运行时开关、TODO 或 NotImplementedException 占位。
- 保留旧公开兼容方法、原业务 owner 和其他未迁移接缝：它们仍承担真实调用/兼容责任，不能以“删旧”为由盲删。
- 未修改的额外模块源码、存档身份、一键脚本不纳入重写范围；两份 2026-09-06 用户草稿改动未覆盖/未提交。

## 未验证与后续

1. 新公共 API 的真实独立子 MOD DLL 在 Bannerlord 的加载顺序、1.3/1.4 与升级兼容。
2. 新接口/薄桥的真实 Campaign/Mission、旧存档、经济/记忆/AFEF、多人场景验收。
3. Native 请求排他/取消和 Courier 更早 prepare 线程缺口尚待实现，不因本轮回放而关闭。
4. 历史 .NET 10 工具本轮未运行；不安装 SDK，不把历史受限项写成 PASS。

详细下一步及回滚入口见 `G:\AFMOD\AF-REFACTOR\HANDOFF.md`。
