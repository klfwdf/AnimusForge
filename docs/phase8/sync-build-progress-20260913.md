# 最新重构工程同步与本机构建（2026-09-13）

## 当前任务：同步与本地构建已完成；LIVE/SAVE 未验收

用户最新要求：拉取最新项目，先读内置 HANDOFF，再按双 Skill 开始构建。本任务先完成最新候选的本地整合和构建基线，不顺带推送、部署、恢复自动化或改制作组业务。

- 唯一写入目录：`G:\AFMOD\AF-REFACTOR`，本地分支 `codex/af-framework-skill-delivery-20260911`。
- 同步前本地 `7f0fb904`；远端 `origin/codex/af-main-refactor-continuation-20260831@bd2ed35f`。共同基线 `e40c92d7`，本地 Skill/文档 3 提交，远端代码/测试/文档 4 提交。
- 预检只有根 HANDOFF 内容冲突；普通 merge，不 rebase/reset/强推。保留本地维护 Skill 0.1.1 适配和框架 Skill，以及远端生产 `9040d184`。
- 接续依据：远端暂停文档要求后续考虑三类 summary job 的主线程输入快照/source fingerprint；此前审查还指出实际工作预算及业务回归不足。它们保留未完成，本轮先验证同步候选，不用旧暂停文字覆盖用户的新手动构建要求。
- 验证：对比远端生产树、保护用户草稿和本地专用 HANDOFF；双 Skill/28 点坐标；聚焦 memory/history/ports/persistence/实际 DLL 元数据；既有脚本 Debug/Release × 1.3/1.4/Bootstrap 项目内 Stage。无游戏内/真实 provider/存档测试授权。

## 实际结果

- 普通合并提交：`4304f5bb`，父提交为本地检查点 `09f52234` 与远端 `bd2ed35f`；仅根 HANDOFF 冲突，保留远端暂停记录和本地 Skill 融合历史，新增唯一当前入口。没有 rebase/reset/强推。
- 运行代码与远端生产候选一致（最后生产 `9040d184`）；双 Skill 的本地协调适配保留。本轮没有另外实现新游戏功能或修复此前审查缺口。
- SDK 使用本机既有 .NET 8（未安装新 SDK）。既有 `build_single_module.ps1 -Stage` 的 Debug/Release × 1.3/1.4/Bootstrap 六项成功，两套统一模块 Stage 成功。没有执行 `-Deploy`。
- 原总清单、closeout 台账和框架执行记录顶部统一指向当前根 HANDOFF，旧状态保留为历史，不再作为独立的“最新”入口；没有把未完成范围改成 DONE。

## 验证分层

| 检查 | 本轮结果 | 能证明什么 |
|---|---|---|
| 两套 Skill | 格式通过 | 本地规则文件结构有效，非宿主重新发现证明 |
| 代码坐标 | 28 点，记录提交/当前树均通过 | 位置与内容对应 `9040d184`，非功能测试 |
| Memory summary helper | 17 项通过，3 个运行时变异被拒绝 | 主线程队列 helper 的线程/owner/generation/回调数限制；非完整业务链 |
| 历史原版控制 | 预期 exit 1 | **仅源码检测后主动失败**，不算执行旧业务流程的红例 |
| 记忆相关回归 | HistorySnapshot 852、Native 27、MemoryFailureUI 85、Team ports 308/3 通过 | 既有 fixture/source-linked 边界未发现这次整合引起的回归 |
| Memory recovery / weekly material | 两个契约套件通过 | 相邻恢复/事实契约；不代表真实存档 |
| 存档身份 | SyncData 146/146、CampaignBehavior 36/36 | 相对 `e40c92d7` 未增删对应身份，不代表旧档实测 |
| 构建 | Debug/Release × 两实现/Bootstrap；两套 Stage 成功 | 在当前引用/SDK 下可构建，未安装进游戏 |
| 公共 API / 实际 DLL | 119 断言、256 并发读、预期 CS0122、四 DLL 532 项元数据通过 | 公共/内部表面隔离与产物身份，不代表实际子 MOD 加载 |
| 产物摘要 | 六 DLL 与各自 `.build.json` SHA256 一致 | 最终文件与元数据对应 |

首次 Skill 校验因给 Python 套用了 dotnet 的隔离 APPDATA 而找不到现有 PyYAML；改为 Skill 校验使用正常 Python 环境、dotnet 保留项目内 APPDATA 后通过。原失败日志保留，未安装依赖、未修改产品代码来消除失败。测试夹具原有 CS0649 警告未屏蔽。

## 产物与复现

- Debug：`G:\AFMOD\AF-REFACTOR\bin\Debug\single_module_stage\AnimusForge`
- Release：`G:\AFMOD\AF-REFACTOR\bin\Release\single_module_stage\AnimusForge`
- 每套都只有一个 AnimusForge 模块，包含 Bootstrap 及 `versions/1.3`、`versions/1.4` 两实现；这是项目内 staging，不是游戏已部署。
- 可提交证据：`docs/audits/2026-09-13-sync-build-verification.json`，含准确构建参数、测试结果、六项产物 SHA、日志摘要。
- 本地原始日志/运行器：`.tmp/sync-build-20260913/`，不上传；本轮构建引用仍是 1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`。
- 新生产位置：`MyBehavior.MemorySummaryMainThread.cs:44-67`（发布）、`:69-90`（接受）、`:92-110`（消费）；`MyBehavior.cs:4995-5048`（整批结果委托），对应源码 `9040d184`。完整坐标见现有 28 点 code map。

## 下一步和玩家验收边界

内置 HANDOFF 指向三个 Execute summary job 的主线程只读输入与 source fingerprint。但在进一步改动这条链之前，建议先补真实业务方法执行的红绿回归与实际 job/record 预算，避免只验证 helper/回调计数。随后按真实 source 变化拒绝旧结果；不扩大到 Courier、TTS、公共写 API 或制作组业务。本轮没有为这些未完成项标记完成，也没有自行部署来验证。

玩家侧仍需在以后获准的测试环境检查：多 NPC/积压记忆日结是否卡顿；生成期间读档/换 Campaign 后是否仍弹旧提示或写旧记忆；失败后能否继续日结；旧存档能否正常往返。本轮仅源码/离线/构建验证，没有游戏内实测。

自动化 `af-7-8` 仍 PAUSED，未推送、未部署、未操作真实存档。两份用户草稿和仅本地简明 HANDOFF 保持原样。任务到“最新基线已整合并构建”结束，不把阶段 8 或整个项目写成 DONE。需要回滚时由用户决定普通反向提交；merge 的主父为同步前本地线，不能盲目反转远端/用户的其他工作。
