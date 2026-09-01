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
- `G:\AFMOD\AF-REFACTOR\docs\phase8\full-domain-readiness-catalog.json`：总纲20个验收责任桶、代表性真实入口与`entryCoverage`、owner assignment、Prompt/ActionPlan适用性、存档责任、fallback、default、当前证据和Bridge矩阵。
- `G:\AFMOD\AF-REFACTOR\docs\phase8\cleanup-candidates.json`：逐symbol清理盘点、调用/动态入口/兼容责任、替代门禁、风险与回滚checkpoint。

早期module catalog仍只跟踪8个逻辑ID：`af.foundation.runtime`、`af.game-adapter`、`af.module.conversation`、`af.module.siege-aftermath`、`af.module.policy-effects`、`af.module.world-diplomacy`、`af.bridge.conversation-siege`、`af.bridge.policy-diplomacy`。这些设计ID和20个完整领域责任桶同时受门禁，**但20领域不是20个物理DLL，也不把`entryTypeStatus=Pending`伪装成ModuleHost已上线**。当前domain maintainer都是`ROLE_PLACEHOLDER`角色ID、入口覆盖均为`REPRESENTATIVE`；real manifest在角色改为`ASSIGNED`且入口由owner确认为`COMPLETE`前，必定`UNASSIGNED_DOMAIN_OWNER/INCOMPLETE_DOMAIN_ENTRY_INVENTORY/BLOCKED`。每份证据必须显式列出`domainIds`和`bridgeIds`并获得相关maintainer审核；缺任一领域的OFFLINE/LIVE/SAVE/RELEASE覆盖都会BLOCKED。

两组既有 Bridge 必须覆盖各自原有5个case ID，再加已有组合矩阵的`incompatible-contract-version`、`bridge-runtime-failure`、`bridge-disabled-data-preserved`、`safe-mode`。每个Bridge的OFFLINE、LIVE 1.3/1.4和SAVE 1.3/1.4分别检查覆盖；两个maintainer都必须审核。不复制fixture期望值为“真实运行结果”，记录必须提供自己的观察证据。20领域目录中的13组`PAIR`使用A/B case，3组`CROSS_CUT`使用`EACH_OWNER_ALONE/ALL_WITHOUT_COORDINATOR/ALL_WITH_COORDINATOR`等多owner case；证据必须在`bridgeIds`中精确绑定对应Bridge，单纯把case文本放进generic record不计覆盖。这仍是责任/证据门禁，不会把责任桶变成已上线Bridge。

Foundation还必须在OFFLINE、LIVE 1.3/1.4和SAVE 1.3/1.4分别覆盖现有CompositionMatrix的**全部18个case ID**，包括required/optional provider、failure cascade、stale completion、partial-start cleanup、生命周期冲突和health bounds；不能只通过四个Bridge case或通用save-roundtrip就宣称组合验收齐备。

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

此外 Foundation 必须单独提供一份 RELEASE `rollback-drill`。**package-validation 和 rollback-drill 不能相互替代**；完整8-ID合成样例仍为48个层/API单元加1个回滚记录，但这些记录还必须通过`domainIds`显式覆盖全部20领域，不能让8-ID绿色掩盖剩余责任桶。

每份记录包含稳定`id`、module/layer/kind/api、`domainIds`、显式`bridgeIds`、可选`cleanupCandidateIds`、`mode=real`或`fixture`、完整source commit、带时区时间、`result=PASS`、非空steps/expected/observed、显式caseIds、artifactHashes和至少一份哈希日志/观察附件。`domainIds`中的每个领域maintainer与原8-ID module maintainer都必须出现在ACCEPTED ownerReview中；Bridge endpoints必须包含在domainIds中。同一记录可覆盖多个领域/Bridge，但必须明确声明并由全部相关owner共同签收。命令可作为说明文字，但永远不执行。

场景覆盖仍需 owner 审查：工具检查每条记录的 Host/API 形状，不自动证明所有 Campaign/Mission/Encounter、三渠道、主体和失败场景都已穷尽。源码总纲与真实联合验收要求仍有效。

### 清理候选与回滚

`cleanup.auditEvidenceId` 必须引用 Foundation 的 OFFLINE `cleanup-inventory` 审核记录；即使候选数组为空，也不能绕过盘点。每个候选至少包含：

```json
{
  "inventoryCandidateId": "cleanup-candidates.json中的稳定ID",
  "file": {"root":"project","path":"relative/source.cs","sha256":"实际64位小写SHA256"},
  "moduleId": "af.module.conversation",
  "auditEvidenceId": "同owner的cleanup-inventory记录ID",
  "disposition": "KEEP",
  "rationale": "说明活跃调用、反射注册、存档兼容或替代证据",
  "activeCallers": [],
  "dynamicEntryPoints": [],
  "saveIdentityRequired": true,
  "replacementEvidenceIds": [],
  "rollback": {
    "commit": "严格早于HEAD的40位提交",
    "evidenceId": "绑定本候选的rollback-drill记录",
    "saveSideEffectsNotUndone": true
  }
}
```

`KEEP`保留有责任的旧入口；`REVIEW_REMOVAL`只表示进入删除评审。静态目录中的每个symbol必须是其source file中存在的英文identifier；同一文件可登记多个独立candidate ID。active/dynamic caller必须为空、没有save identity责任；同owner替代证据要覆盖全部6个层/API单元，并在记录的`cleanupCandidateIds`中精确绑定本候选和owner domain。逐候选rollback drill同样必须绑定该ID。工具**不证明反射/外部caller为零、不删除任何代码**，动态入口与存档责任仍须owner复核。

全局与逐候选`rollback.commit`都必须等于静态inventory的pre-cleanup checkpoint，并由全局Git检查证明是**严格早于HEAD**的祖先，不能拿HEAD本身冒充回退点；Foundation RELEASE `rollback-drill`的`rollbackTargetCommit`必须精确等于声明提交且绑定candidate ID。`saveSideEffectsNotUndone=true`明确认知源码回滚不撤销存档副作用。

## 自测与未验证范围

自测只在本工具目录的 `.fixture-*` 私有临时子目录创建合成证据，结束删除自己的临时目录；每条合成记录及附件标注 FIXTURE ONLY，未采集真实游戏数据。覆盖缺失、篡改、版本/时效、路径边界、fixture升级拒绝、Bridge、清理保护、回滚和命令不执行等正负路径。

当前不替代真实 Bannerlord 启动、1.3/1.4 实机、旧档、AFEF、生产性能、包内容 allowlist 或全量 20 领域签收；工具 PASS 也不能替代这些验证。新的框架没有替换旧 production/fixture runner，无已失效旧路径可删。
