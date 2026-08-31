# Phase Eight Readiness — 只读证据门禁

阶段 8 **准备态**工具，不是新的 ModuleHost，也不是部署/删除执行器。它只校验已采集、人工复核的证据清单；不启动游戏、不读取实际存档内容、不运行输入中的命令、不改游戏、不推送。

## 运行

```powershell
Set-Location -LiteralPath 'G:\AFMOD\AF-REFACTOR'
python -B -m unittest discover -s .\tools\PhaseEightReadiness -p 'test_*.py' -v
python -B .\tools\PhaseEightReadiness\readiness.py `
  --project-root 'G:\AFMOD\AF-REFACTOR' `
  --manifest 'G:\AFMOD\AF-REFACTOR\docs\phase8\all-missing.evidence.json'
```

第二条命令必须输出 `BLOCKED`、退出 **2**：这是缺失证据的成功演示，不是工具测试失败。示例报告见 `G:\AFMOD\AF-REFACTOR\docs\phase8\all-missing-report.example.json`。它是生成时的快照，不是自动刷新的项目状态。

真实采集时在明确的 artifact root 保存 manifest、记录和日志，不修改已验收源码：

```powershell
python -B .\tools\PhaseEightReadiness\readiness.py `
  --project-root 'G:\AFMOD\AF-REFACTOR' `
  --artifact-root 'G:\AFMOD\.build-cache\af-framework-20260901' `
  --manifest 'G:\AFMOD\.build-cache\af-framework-20260901\acceptance\manifest.json'
```

工具只向 stdout 写 JSON；重定向报告由调用者选择，工具没有写文件/部署模式。每次命令单独检查 `$LASTEXITCODE`。

## 结果不是授权

| 输出 | 含义 |
| --- | --- |
| `BLOCKED` / exit 2 | 必填证据缺失、失败、过期、版本不符、未经 owner 审核或文件不可信 |
| `FIXTURE-VALID` / exit 0 | 合成数据只证明工具正向分支；绝不是真实游戏/存档/发布 PASS |
| `READY-FOR-OWNER-REVIEW` / exit 0 | 本初版目录范围的证据结构/引用门槛满足；仍需集成人员审查证据真实性和全领域覆盖 |

所有结果中 `fullProjectReleaseReady=false`；`authorization.delete/defaultSwitch/deploy/push/publish` 恒为 false。即使全部绿色，也不授权删 facade、切默认入口、安装 SDK、覆盖游戏或发布。真实证据缺失绝不会因 `Stage`、进程存在、主菜单或 `installedMatchesStage=true` 变为 LIVE/SAVE PASS。

## 复用与覆盖边界

直接读取以下已有契约，不另造生产注册/领域体系：

- `G:\AFMOD\AF-REFACTOR\docs\fixtures\phase3-module-catalog\module-catalog.json`：现有 8 个逻辑 ID、owner、maintainers；这些仍是设计目录，不代表 8 个物理 DLL 已上线。
- `G:\AFMOD\AF-REFACTOR\docs\fixtures\phase2-settlement-policy-bridges\settlement-siege-composition.json`。
- `G:\AFMOD\AF-REFACTOR\docs\fixtures\phase2-settlement-policy-bridges\policy-diplomacy-composition.json`。
- `G:\AFMOD\AF-REFACTOR\docs\fixtures\phase3-composition-matrix\composition-matrix.json`。

初版跟踪：`af.foundation.runtime`、`af.game-adapter`、`af.module.conversation`、`af.module.siege-aftermath`、`af.module.policy-effects`、`af.module.world-diplomacy`、`af.bridge.conversation-siege`、`af.bridge.policy-diplomacy`。**不是完整 20 领域验收表**；Economy/Duel/WorldMap 等未进入这份早期目录的 owner 不会被假装已覆盖。后续由既有目录 owner 审查扩展范围，而不是在本工具中私自创建生产模块。

两组 Bridge 必须覆盖各自原有 5 个 case ID，再加已有组合矩阵的 `incompatible-contract-version`、`bridge-runtime-failure`、`bridge-disabled-data-preserved`、`safe-mode`。每个 Bridge 的 OFFLINE，以及 LIVE 1.3/1.4 分别检查覆盖；两个 maintainer 都必须审核。不复制 fixture 的期望值为“真实运行结果”，记录必须提供自己的观察证据。

Foundation 还必须在 OFFLINE、LIVE 1.3、LIVE 1.4 分别覆盖现有 CompositionMatrix 的**全部 18 个 case ID**，包括 required/optional provider、failure cascade、stale completion、partial-start cleanup、生命周期冲突和 health bounds；不能只通过四个 Bridge case 就宣称组合验收齐备。

已有 `BridgeFixtureContractTests` / `CompositionMatrixContractTests` 只证明设计 fixture；`LiveHostReadinessAudit` 只证明环境可用。可将它们日志登记为 OFFLINE，不提升成 LIVE/SAVE。本工具不自动执行这些 runner。

## 输入契约

从 `G:\AFMOD\AF-REFACTOR\docs\phase8\all-missing.evidence.json` 复制清单，从 `G:\AFMOD\AF-REFACTOR\docs\phase8\evidence-record.template.json` 复制记录。模板故意为 `NOT-RUN`/空值，不包含伪造 PASS。

### 路径与 SHA-256

所有文件引用均为：

```json
{"root":"artifact","path":"acceptance/logs/case.log","sha256":"64位小写SHA256"}
```

`root` 只能是 `project` 或 CLI 显式提供的 `artifact`。路径是 `/` 分隔的相对普通文件路径；拒绝绝对路径、`..`、Windows 盘符/ADS、反斜杠、越界 symlink/junction。manifest 本身也必须在指定根内。**绝不从 JSON 自动增加信任根**。文件在读取前解析边界，SHA-256 与读取前后 stat 均检查；日志附件、证据 JSON、源码绑定和产物分别校验。

manifest 是人工审核的信任根，不是加密签名。SHA-256 能识别引用内容变化，不能阻止有权重写 manifest 的人同时伪造哈希，更不能证明证据真实发生。reviewer ID 是既有 maintainer 角色声明，不是身份认证。归档时由 owner 复核来源、步骤、版本和观察记录。

### 源码、产物与时效

- `source.commit` 必须为当前 Git HEAD 的完整 40 位小写 SHA，工作树必须 clean（含未跟踪文件）。Git 只使用固定只读命令，禁用 fsmonitor，不运行任何证据命令。
- `source.files` 必须绑定上述四份现有契约文件，可额外绑定本轮相关源码。Git commit/clean 绑定 tracked 源码；外部构建依赖仍需 owner 提供版本/来源证据。
- 在完成源码提交之后采集真实证据；不要把声明当前 commit 的证据 manifest 再提交进该源码 commit，避免自引用版本漂移。保存到明确 artifact root，引用所测提交。
- manifest 的 `releaseVersion` 必须是明确数字版本（例如 `v0.8.7` / `1.3.7.2`，可加 prerelease 后缀）；全部产物 `version`、证据 `releaseVersion` 必须精确相同。
- `artifacts` 每条包含 `id`、`file`、`sourceCommit`、`version`、`apiLine`；必填 ID 为 `bootstrap`、`implementation-1.3`、`implementation-1.4`、`module-manifest`、`package`。两个 implementation 的 apiLine 必须对应 1.3/1.4，其他为 agnostic。记录中的 `artifactHashes` 必须对应当前已校验文件，不能只写“最新 DLL”。
- 记录与产物的 `sourceCommit` 必须等于当前源码 commit。产物来源/版本字段是人工构建记录，本工具不反编译 DLL 或证明构建 provenance。
- `recordedAt` / `reviewedAt` 必须有时区；记录不能在未来、不能超过 **14 天**，审核不能早于记录。CLI 不允许覆盖时钟或放宽窗口。
- 固定限制：JSON 2 MiB、单附件/产物 512 MiB、累计读取 2 GiB、清单/候选数组 256、文件引用 2048；不递归扫描生产树，不在 Tick 中运行。大型包须先由 owner 拆分受审计工件，不能悄悄跳哈希。

### 分层记录

每个既有模块 ID 必须至少具备以下 6 份有效记录；API 不适用的排除尚未实现，初版保守阻塞，不能靠省略字段跳验收。

| layer / apiLine | kind | 额外要求 |
| --- | --- | --- |
| OFFLINE / agnostic | contract、production-replay、metadata、stage、readiness、cleanup-inventory | 不能替代实机、旧档和发布 |
| LIVE / 1.3 与 1.4 | game-scenario | initialized Campaign/Mission、对应完整数字 BuildInfo（`v1.3.15.110062` 形状）、存档/新档 ID、Bootstrap+对应实现哈希 |
| SAVE / 1.3 与 1.4 | save-roundtrip | 同上，加真实旧档已加载、存储后重载已核验 |
| RELEASE / agnostic | package-validation | 各 module 均需此 kind，绑定全部 5 类产物，不代替发布授权 |

此外 Foundation 必须单独提供一份 RELEASE `rollback-drill`。**package-validation 和 rollback-drill 不能相互替代**；完整 8-ID 合成样例为 48 个层/API 单元加 1 个回滚记录。

每份记录包含稳定 `id`、module/layer/kind/api、`mode=real` 或 `fixture`、完整 source commit、带时区时间、`result=PASS`、非空 steps/expected/observed、显式 caseIds、artifactHashes 和至少一份哈希日志/观察附件。命令可作为说明文字，但永远不执行。ownerReview 必须为 ACCEPTED、含目录要求的全部 maintainer、reviewedAt 和人工审核说明。**不得把模板中的 false/NOT-RUN 改成 true/PASS 来代替实际测试**。

场景覆盖仍需 owner 审查：工具检查每条记录的 Host/API 形状，不自动证明所有 Campaign/Mission/Encounter、三渠道、主体和失败场景都已穷尽。源码总纲与真实联合验收要求仍有效。

### 清理候选与回滚

`cleanup.auditEvidenceId` 必须引用 Foundation 的 OFFLINE `cleanup-inventory` 审核记录；即使候选数组为空，也不能绕过盘点。每个候选至少包含：

```json
{
  "file": {"root":"project","path":"relative/source.cs","sha256":"实际64位小写SHA256"},
  "moduleId": "af.module.conversation",
  "auditEvidenceId": "同owner的cleanup-inventory记录ID",
  "disposition": "KEEP",
  "rationale": "说明活跃调用、反射注册、存档兼容或替代证据",
  "activeCallers": [],
  "dynamicEntryPoints": [],
  "saveIdentityRequired": true,
  "replacementEvidenceIds": []
}
```

`KEEP` 保留有责任的旧入口；`REVIEW_REMOVAL` 只表示进入删除评审，要求 active/dynamic caller 都为空、没有 save identity 责任、同 owner 替代证据覆盖全部 6 个层/API 单元。工具**不扫描证明调用为零、不删除任何代码**，静态调用/反射/存档来源必须由 owner 复核。

`rollback.commit` 必须是当前历史中的完整祖先提交；`rollback.evidenceId` 引用 Foundation 的 RELEASE `rollback-drill`，绑定同一候选版本/产物，其 `rollbackTargetCommit` 必须精确等于清单声明的回滚提交；`saveSideEffectsNotUndone=true` 明确认知源码回滚不撤销存档金币/债务/领地等副作用。回滚操作、测试存档备份与数据恢复真实性仍须人工确认。

## 自测与未验证范围

自测只在本工具目录的 `.fixture-*` 私有临时子目录创建合成证据，结束删除自己的临时目录；每条合成记录及附件标注 FIXTURE ONLY，未采集真实游戏数据。覆盖缺失、篡改、版本/时效、路径边界、fixture升级拒绝、Bridge、清理保护、回滚和命令不执行等正负路径。

当前不替代真实 Bannerlord 启动、1.3/1.4 实机、旧档、AFEF、生产性能、包内容 allowlist 或全量 20 领域签收；工具 PASS 也不能替代这些验证。新的框架没有替换旧 production/fixture runner，无已失效旧路径可删。
