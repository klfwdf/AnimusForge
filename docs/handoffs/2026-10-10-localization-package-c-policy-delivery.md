# Package C（政策系统）本地化 — 交付 / 交接文档

> 主线接收更新（2026-10-10）：本报告以下是外部C交付时的历史快照，“未提交/585键/783行”不再代表当前状态。主线产品提交 `19f11c941` 已精确接收C与主线修复，afpol现各621唯一键；raw30、模块90、失败92、测试oracle272项及26资源对PASS，既有双API引用各2484资源解析PASS。当前源码编译、程序集契约执行、实机/旧档及正式包仍NOT-RUN。详见[主线复审结果](2026-10-10-localization-policy-review-illustrator-errors.md#主线接收结果2026-10-10)。原报告保留，禁止按旧“未提交”状态回滚或覆盖当前共享文件。


- 日期：2026-10-10
- 工作单：`docs/handoffs/2026-10-10-localization-external-policy-diplomacy.md`
- 计划：`docs/handoffs/2026-10-10-localization-cde-plan.md`
- 范围切片：`docs/handoffs/2026-10-10-localization-package-c-policy.md`
- 交接时 HEAD：`577f29b7a`（分支 `main`）
- 当前分支 HEAD：`b196ecae4`（分支 `main`；本包工作树仍 dirty）
- 状态：**固定 UI 文案投影已补齐并通过资源/语法验证；未提交。整包 CJK 计数已刷新，见 §5/§7。**

---

## 0. 给接手 agent 的一句话

在当前 `aa102ec17` 上完成了发布就绪/资格/失败/AI 编写状态的显示映射，
保留 raw prompt、日志、存档和事实字段；中英资源共 585 keys，资源契约与 1.3/1.4 真实资源解析通过。
**剩余事项是处理 §2 所述共享资源文件中其他主线未提交键的提交隔离，再决定是否允许偏离原 `git commit --only` 方式提交。**
不要重做已完成的改动，不要 `git add -A`，不要动其他作者的源文件或 27 个未跟踪资产。

---

## 1. 已完成切片（S1 / S1b / S1c / S1d / S1e / S2 / S8）

| 切片 | 内容 | 落点 | 状态 |
| --- | --- | --- | --- |
| S1 | `Management.cs` 主体玩家可见壳/后缀/原因改写（59 处替换） | `CustomPolicyBehavior.Management.cs` | 已落地 |
| S1b | 63 组新 key（壳/分隔符/目标标签/结束原因投影/定居点类型）+ 4 处 `Management.cs` + 2 处 `Targets.cs` | 两份资源 + `Management.cs` + `Targets.cs` | 已落地 |
| S1c | 20 组新 key + 修 `NpcRulerPolicyBehavior.cs:309` 的 `Get()` 误用占位符 key | 两份资源 + `NpcRulerPolicyBehavior.cs` | 已落地 |
| S1d | 37 组新 key + `Generation.cs` 20 处（玩家侧重试弹窗全套 + `MapPlayerPolicyFailureStageToDisplay`） | 两份资源 + `Generation.cs` | 已落地 |
| S1e | 23 组新 key + `Effects.cs` 9 处 `NoticeLines` + `Management.cs` 目标标签显示投影 | 两份资源 + `Effects.cs` + `Management.cs` | 已落地 |
| S2 | `PolicyEffectSaveCodec.cs` 玩家可见描述 24 组 key | 两份资源 + `PolicyEffectSaveCodec.cs` | 已落地 |
| S8 | `KingdomAgendaCustomPolicyBehavior.cs` + `SettlementBalanceRules.cs` + `SettlementBalanceRuntime.cs` | 同左 | 已落地 |
| 接续复核 | 固定 readiness/eligibility/failure/cost/AutoDraft 显示投影、固定存档 fallback 的 UI 投影及当前 HEAD CJK 分类 | `Generation.cs`、`Management.cs`、`AutoDraft.cs`、`LocalPolicyUi.cs`、双语资源、Policy 契约测试 | 工作树完成；契约构建未跑；提交受共享资源隔离阻塞 |

S1e 之后有一次修复（`fix_s1e.py`），修掉了 4 个缺陷，详见 §6「已修复缺陷」。

---

## 2. Package C 路径及当前 numstat（14 个 tracked 文件；本文仍 untracked）

基于当前 HEAD `b196ecae4` 实测；旧表的行数属于 `577f29b7a` 工作树，不能沿用。资源验证使用相同的 PolicySystem/资源内容；`b196ecae4` 仅改动 Illustrator 文件。

```
5	1	PolicySystem/Agenda/KingdomAgendaCustomPolicyBehavior.cs
7	1	PolicySystem/Balance/SettlementBalanceRules.cs
4	4	PolicySystem/Core/CustomPolicyBehavior.AutoDraft.cs
10	10	PolicySystem/Core/CustomPolicyBehavior.Effects.cs
257	54	PolicySystem/Core/CustomPolicyBehavior.Generation.cs
619	81	PolicySystem/Core/CustomPolicyBehavior.Management.cs
18	2	PolicySystem/Core/CustomPolicyBehavior.Targets.cs
295	7	PolicySystem/Effects/PolicyEffectSaveCodec.cs
23	22	PolicySystem/History/PolicyRecordPresentation.cs
6	3	PolicySystem/Npc/NpcRulerPolicyBehavior.cs
37	1	PolicySystem/UI/LocalPolicyUi.cs
248	1	content/modules/PolicySystem/ModuleData/Languages/CNs/afpol_ui_strings-zh-CN.xml
248	1	content/modules/PolicySystem/ModuleData/Languages/afpol_ui_strings.xml
101	8	tests/bridges/Policy/PolicyEffectModule.ContractTests/Program.cs
```

交付文档 `docs/handoffs/2026-10-10-localization-package-c-policy-delivery.md` 当前 untracked，若获得提交确认需用精确路径单独纳入；没有新建其他产品文件。

### 共享资源文件的提交隔离阻塞

两份 `afpol_ui_strings.xml` 相对当前 HEAD 的工作树差异为各 248 additions / 1 deletion。除本包约 36 个新 key 外，还包含主线正在修改的 5 个 key：`AFPOL_UI_BalanceUnitRange`、`AFPOL_UI_ModuleManagerSummary`、`AFPOL_UI_NpcAgendaConfirmed`、`AFPOL_UI_NpcAgendaUnconfirmed`、`AFPOL_UI_NpcAgendaResubmitted`；对应源文件 `PolicySystem/UI/SettlementBalanceEditorVM.cs`、`PolicySystem/UI/PolicyEffectModuleManagerUi.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs` 及 `PolicySystem/Npc/NpcRulerPolicyBehavior.Persistence.cs` 均有其他作者的未提交改动。

因此，原计划对整份资源路径执行 `git commit --only <精确路径>` 会连带提交主线的 5 个资源 key，却不包含其生产者。没有擅自 stage/commit；需先决定是允许按精确 patch 做 partial-index commit（偏离原 `--only` 步骤），还是保留全部改动待主线协调。

### `git status` 的 CRLF 假阳性（仍适用）

`core.autocrlf=true`。若 `git status --porcelain` 报出文件修改但 `git diff --numstat` 对该路径为空，则是 CRLF/stat 差异；以逐路径 numstat 与 diff 内容决定归属。当前仍有多作者未提交源文件和未跟踪资产，**一律不碰、不回滚、不纳入本包提交**。

- 禁止 `git add -A` / `git add .`；在提交隔离确认前不 stage 共享资源。
- Memory 模块/adapter/测试、`MyBehavior.*.cs`、`UI/Errors/*`、`src/modules/AF.Module.Kingdom/CivilWar/*`、notoriety / LLM failure / diplomacy bulletin 等均属于其他切片。

---

## 3. 资源现状（`verify_res.py` 实测，PASS）

| 项 | EN | ZH |
| --- | --- | --- |
| 路径 | `content/modules/PolicySystem/ModuleData/Languages/afpol_ui_strings.xml` | `.../Languages/CNs/afpol_ui_strings-zh-CN.xml` |
| bytes | 65593 | 61459 |
| BOM | False | False |
| CRLF | 593 | 593 |
| 裸 LF | 0 | 0 |
| keys / unique | 585 / 585 | 585 / 585 |
| sha256 | `2E549910F1FB3655D94A10DB1BCFD5FB3F46E54F05254557EC7F60F2CA2CDA8D` | `FC348CF49E52FD9A751CCA1A24FBC72ECB009E831462EBFD3E9C340F49CE9D29` |

- key 集合一致：True；key 顺序一致：True；资源占位符不匹配：0；空值：0。
- EN `<tag language="English" />`；ZH `<tag language="简体中文" />`（`verify_res.py` 打印 `zh` 是脚本侧解码显示问题，
  `xml_wellformed.py` 用 `ElementTree` 独立确认标签存在且文件可解析）。
- 本文原交接快照为 544 keys；当前为 585 keys，净增 41（本次接续新增 36 个 key，另有 §2 登记的 5 个并行主线 key）。
- HEAD 已推进，当前相对 HEAD 的资源 numstat 是每份 `248 added / 1 deleted`，不能用旧 375-key 初始基线推算本次 diff。

### 注册（两侧都在）

- `content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml:13`
  → `<LanguageFile xml_path="afpol_ui_strings.xml" />`
- `content/foundation/AF.Foundation.Localization/ModuleData/Languages/CNs/language_data.xml:13`
  → `<LanguageFile xml_path="CNs/afpol_ui_strings-zh-CN.xml" />`
- `content/content-map.json` 两条 `{owner, source, target}` 对（EN + ZH）均在。

---

## 4. 交接时已运行的验证与实测结果（历史快照）

以下四项为原交接会话实测；本次复跑与环境变化见 §5：

```
py .tmp_audit\roslyn_check.py        → RESULT: PASS
py .tmp_audit\verify_res.py          → RESULT: PASS（数字见 §3）
py .tmp_audit\xml_wellformed.py      → RESULT: PASS
py .tmp_audit\check_placeholders.py  → Get-on-placeholder=0  Format-mismatch=0  missing=0
```

Roslyn（`csc.dll /nologo /target:library /nostdlib+ /langversion:10`，逐文件）：

```
CustomPolicyBehavior.Generation.cs   total_errors=859   syntax_errors=0  unclassified=0  OK
CustomPolicyBehavior.Management.cs   total_errors=697   syntax_errors=0  unclassified=0  OK
CustomPolicyBehavior.Targets.cs      total_errors=445   syntax_errors=0  unclassified=0  OK
CustomPolicyBehavior.Effects.cs      total_errors=1352  syntax_errors=0  unclassified=0  OK
PolicyUiTexts.cs                     total_errors=11    syntax_errors=0  unclassified=0  OK
PolicyRecordPresentation.cs          total_errors=106   syntax_errors=0  unclassified=0  OK
NpcRulerPolicyBehavior.cs            total_errors=377   syntax_errors=0  unclassified=0  OK
RESULT: PASS
```

`total_errors` 是 `/nostdlib+` 产生的 CS0518/CS0246 等预期工件，**不是语法错误**；
判据是 `syntax_errors=0 and unclassified=0`。

### CJK 普查（交接时快照；当前复核见 §7）

```
CustomPolicyBehavior.Generation.cs        lines=5732  cjk_lines=311
CustomPolicyBehavior.Management.cs        lines=4865  cjk_lines=117
CustomPolicyBehavior.Targets.cs           lines=2121  cjk_lines=76
CustomPolicyBehavior.Effects.cs           lines=8193  cjk_lines=72
PolicyUiTexts.cs                          lines=22    cjk_lines=0
LocalPolicyUi.cs                          lines=693   cjk_lines=0
KingdomPolicyUi.cs                        lines=1625  cjk_lines=0
PolicyRecordPresentation.cs               lines=346   cjk_lines=1
NpcRulerPolicyBehavior.cs                 lines=1483  cjk_lines=7
PolicyEffectSaveCodec.cs                  lines=3695  cjk_lines=80
KingdomAgendaCustomPolicyBehavior.cs      lines=407   cjk_lines=15
SettlementBalanceRules.cs                 lines=121   cjk_lines=0
TOTAL cjk_lines=679
```

变化：`Effects.cs` 81 → 72（9 处 `NoticeLines` 已本地化）；
`Management.cs` 101 → 117（**上升是设计内**：S1e 新增的显示投影需要
14 个 EndReason 精确匹配 `case`、10 个 shell 精确匹配 `case`，以及 `（` `）` `、` `，另` `处`
`指定定居点：` `当前领地及附属村庄` 这些结构字面量，全部是**持久化字面量的匹配器**，
与既有 `MapPolicyEndReasonToDisplay` 同形，不是漏改）。
总数 672 → 679（−9 +16 = +7，吻合）。

---

## 5. 本次复核的验证结果与未验证项（Policy 输入 HEAD `aa102ec17`；当前 HEAD `b196ecae4`）

**PASS（本会话实测）**

1. `py .tmp_audit/roslyn_check.py`：7 个源文件 `syntax_errors=0`、`unclassified=0`。`/nostdlib+` 的 `total_errors` 是预期引用缺失，不是生产编译。
2. 对本脚本未列入的 `CustomPolicyBehavior.AutoDraft.cs`、`LocalPolicyUi.cs`、测试 `Program.cs` 单独执行 Roslyn `/nostdlib+` 语法筛选：三者均 `syntax_errors=0`；这仍不是类型/程序集编译。
3. `py .tmp_audit/verify_res.py`：PASS；资源编码与 CRLF 见 §3。
4. `py .tmp_audit/xml_wellformed.py`：PASS。
5. `py tests/content/J15ContentContractTests/LocalizationResourceContractTests.py`：`PASS localization resource pairs=26`。
6. 两个真实资源解析：
   - `LocalizationRuntimeTests.ps1 -ReferenceDir .tmp/build_check/1.3 -ResourceName afpol_ui_strings`
     → `PASS real localization DLL: pairs=1, 2340 text resolutions`。
   - 相同命令改 `-ReferenceDir .tmp/build_check/1.4` → 同样 PASS。
7. `git diff --check`（本包 14 个 tracked 路径）无输出。

**需如实限定的检查**

- `py .tmp_audit/check_placeholders.py` → `Get-on-placeholder=0, Format-mismatch=3, missing=0`。
  三个命中在其他作者的多行 `PolicyUiTexts.Format` 调用：
  `NpcRulerPolicyBehavior.Generation.cs:1456-1458`（`POLICY/KINGDOM/DAYS`）、`:1885-1887`（`POLICY`）与
  `SettlementBalanceEditorVM.cs:71-74`（`UNIT/MIN/MAX`）。逐行检查确认变量实参存在；该脚本只扫描调用首行，故此处为多行调用误报。未改这些作者文件。
- `inventory_untranslated.py --output .tmp_audit/untranslated-packagec-final2.json` 扫描整个共享工作树：HEAD `aa102ec17`、`consumer_review=1225`、135 files；发现一份无关的损坏世界书 JSON，且扫描期间 `extensions/AnimusForge.Illustrator/src/UI/IllustratorUiText.cs` 发生变化，进程退出码 1。**不能作为稳定全仓基线或 Package C 通过结果。**旧基线 JSON 被 ignore 规则拒绝读取，因此本次没有宣称前后数量改善。

**NOT-RUN / 不在授权范围**

- `PolicyEffectModule.ContractTests` 未运行：工程只有 `obj/Debug/net472`、无当前 `bin`，`PolicyTestAssemblyPath` 未设置；`.tmp/build_check/1.3|1.4/AnimusForge.dll` 时间为 2026-08-21，早于当前源码，不拿旧程序集冒充本次代码测试。
- AF 生产构建、1.3/1.4 双版本构建、Bootstrap、Stage、进游戏、正式打包、部署与 push 均未运行；本工作单禁止这些动作。资源运行时解析不等于产品编译或实机验收。

---

## 6. 已修复缺陷（接手时不要再踩）

| # | 缺陷 | 根因 | 修法 |
| --- | --- | --- | --- |
| 1 | `Management.cs` 编译不过，`CS1056 意外的字符"\u0001"`（`:2004`、`:2005`） | 行号补丁用了 `\x01` 缩进标记，而 `expand()` **只剥掉一个** `\x01`，`\x01\x01` 残留 | `fix_s1e.py` 改为**逐行原样重写**三条保留行 |
| 2 | `:2006` 闭合花括号缩进错误 | 基线缩进取自 `lines[start-1]`，与同块其它保留行不一致 | 同上，原样写 `'\t\t}'`、`'\t}'` |
| 3 | 新插入的投影里有 `\uff08` / `\uff09` / `\u3001` / `\u5904` / `\uff0c\u53e6` 等**转义残留** | 补丁脚本用转义写 CJK，直接落进了 C# 源码，违反 cde-plan「源码不得出现 Unicode 转义中文」 | 只在**插入行范围内**解码 76 个 `\uXXXX` |
| 4 | `CompactPolicyContextText` 前的空行被吃掉 | 被替换区间包含了它且未重新发出 | `lines.insert(compact_idx, '')` |

修复输出（实测）：
`OK: 3 markers removed, closing brace fixed, 76 \uXXXX escapes decoded, blank separator restored`

**教训（写进规矩）：**
- 多行保留区**不要用 `\x01` 标记**，直接写显式 tab 字面量；改完必须重跑 `roslyn_check.py` 抓 CS1056。
- **绝不整文件解码 `\uXXXX`**：`Management.cs:1580` 有一个合法的 `string.Join("\u001f", …)` 分隔符，
  整文件解码会破坏它。只能限定在插入行范围。
- CJK 锚点必须用 `.tmp_audit/*.py` 的 `ascii()` / `repr()` 探针落盘后再读（控制台是乱码），
  补丁脚本里 CJK 写成 `\uXXXX`，但**落进 C# 源码前必须解码成真字符**。

历史同类缺陷：
- `patch_s1c.py` 把 `text=""{NAME}""` 写进 EN 资源 → XML 不良构 → 正则解析器静默跳过该行 →
  EN 483 / ZH 484 keys 不一致。`fix_quot.py` 修为 `text="&quot;{NAME}&quot;"`。
  **XML 属性里的双引号必须写 `&quot;`。**
- `NpcRulerPolicyBehavior.cs:309` 对含占位符的 key 调 `Get()` → UI 里出现字面 `{TARGET}`。
  已改为 `AFPOL_UI_TargetKingdomLabelOnly`。
- 19 个被引用但资源里不存在的 key（已全部补齐，现 `missing=0`）。

---

## 7. 当前 CJK 逐类复核（Policy 输入 `aa102ec17`；当前 HEAD `b196ecae4`）

原交接快照的 679 行已过时。当前 `.tmp_audit/cjk_census.txt` 共 **783 行**；逐行清点结果：

| 文件 | CJK 行 | 判定摘要 |
| --- | ---: | --- |
| `CustomPolicyBehavior.Generation.cs` | 326 | Prompt/世界快照、一次性校验与事务诊断保留；可见 readiness/eligibility/已知失败/费用摘要经固定映射显示；raw formatter 保持事实值。 |
| `CustomPolicyBehavior.Management.cs` | 164 | 模型输出解析哨兵、续约/validation 错误及 raw 历史事实保留；玩家结果与历史使用单独 display builder。 |
| `CustomPolicyBehavior.Targets.cs` | 76 | LLM 目标目录/选择规则、实体别名、存档句柄名称与 resolver 诊断；保留原值。 |
| `CustomPolicyBehavior.Effects.cs` | 72 | `EndReason` 写入与内部效果错误；写入保持原值，显示走投影。9 个可见 `NoticeLines` 已本地化。 |
| `PolicyEffectSaveCodec.cs` | 109 | JSON/schema/migration 错误与 raw 效果描述；raw 用于事实/Prompt/历史，UI 使用独立 localized display。 |
| `KingdomAgendaCustomPolicyBehavior.cs` | 15 | 资格/队列 `failureReason` 由调用方记录或处理，不作为通知正文。 |
| `NpcRulerPolicyBehavior.cs` | 7 | RAG focus/领域词/地理词与启动失败诊断，供召回或日志。 |
| `PolicyRecordPresentation.cs` | 1 | 删除记录后通知刷新失败的日志。 |
| `LocalPolicyUi.cs` | 13 | AutoDraft 原始错误身份的精确 matcher，由 UI 映射后显示；未知错误回退原文。 |
| `PolicyUiTexts.cs`、`KingdomPolicyUi.cs`、`SettlementBalanceRules.cs` | 0 | 无待判 CJK 行。 |
| **TOTAL** | **783** | 不表示全仓无中文。 |

原 `cjk_dump.py` 覆盖的 8 个核心文件及额外 `LocalPolicyUi.cs` 行均已审查；其余模块、Prompt 与世界书不属于此表。

### 当前逐行结论

- `Generation.cs` 的 `FailureStage` 生产字面量仍为日志/暂态身份，显示只经 `MapPlayerPolicyFailureStageToDisplay`；主评议/模块/超时/配置错误的已知固定中文经 `TryMapPlayerPolicyFailureReasonToDisplay` 投影，`PolicyLlmClient.TryResolvePlayerPolicyProfile` 的固定配置错误同样只在失败/冻结通知边界映射。未知技术错误继续沿原技术诊断分支处理。
- `BuildReadyStatus` 与 `PolicyEligibility.Reason` 的生产者值不改；compose/submit/local-publish/AutoDraft 消费点调用 `MapPlayerPolicyEligibilityReasonToDisplay`，金额按 invariant digits 映射到 `{AMOUNT}`。
- 费用上下文/存档继续调用 raw `FormatCostText`、`FormatGoldCostText`、`BuildPlayerPolicyGoldCostSummary`；用户弹窗/议程通知调用独立 `FormatCostTextForDisplay`、`BuildPlayerPolicyGoldCostSummaryForDisplay`。不把语言相关 UI 文案写回 Prompt、`SecondaryEffects` 或原始请求。
- 四条固定默认反馈/影响/附庸中性文本只在结果弹窗、历史/附庸 VM 使用 `MapPlayerPolicyNarrativeFallbackToDisplay`；玩家/模型正文与存档原文不重写。
- `Management.cs` 的 TargetLabel/EndReason/Cleaner 字面量是兼容投影的匹配键或 raw history/事务错误；UI 使用对应独立显示 builder。renewal 的异常由既有通用 UI 错误通知处理，不把内部 serializer/replay 错误直接显示。
- `Targets.cs` 76 行均属 Prompt target directory/selection、实体别名匹配、持久化 `DisplayName` 或 resolver 校验；保持原值。目标/实体名称不猜译。
- `PolicyEffectSaveCodec` 的 schema/migration 字符串是 raw save diagnostics；`DescribeRawPlayerVisibleInstances`/`DescribeRawDisplayGroup` 服务共享事实、Prompt/历史，玩家 UI 调 `DescribePlayerVisibleInstances`。已知 `PlayerDisplayName` 名称由 `PolicyEffectUiText.Name(module)` 按资源 key 投影，故模块 descriptor 的中文 fallback 不等于当前 UI 缺口。
- `Effects.cs` 剩余 72 行为 raw `EndReason` 写入与内部效果错误；`NoticeLines` 已双语显示。`Agenda` 的 15 个资格/队列原因只写日志/返回机制状态；NPC 的 7 行为 RAG/query/内部诊断；`PolicyRecordPresentation` 的一行是日志。
- 补充原 census 之外的 AutoDraft 消费追踪：`CustomPolicyBehavior.AutoDraft.cs` 资格原因与选中目标失效经资源化；`LocalPolicyUi.cs:398-428` 显示端精确映射已知 parser/request errors，未知错误回退原文。Prompt builder 的 raw parse error contract 保持不变。

**明确排除在 Package C 之外（不要动）：**
- `PolicyEffectSaveCodec.cs:193` `normalized["EndReason"] = "旧政策系统升级后停止，可重新评议"` —— 存档载荷，逐字节保留，交给存档 key owner。
- `content/modules/PolicySystem/CustomPrompts/**`、`PolicyEffectPromptService` 的 LLM 规则内容、跨模块接口。
- `Context/WorldDiplomacyPolicyContext.cs`、`PolicyHistoryRetrievalService.cs` 中共享政策事实/Prompt/log 串，属于外交/跨包工作。
- 动态用户/模型原文、游戏实体名、未知旧存档目标/回执无法安全映射时保持原文；不得猜译或删去信息。

### 已确认的显示投影（精确映射，未知值不猜译）

本包的投影器均在 UI 边界按已知 raw literal 精确匹配并输出资源文本，
**default 返回原文，绝不猜译**：

1. `MapPolicyEndReasonToDisplay(string reason, string fallback)`（`:1986`，14 个 `case`）
2. `MapPlayerPolicyFailureStageToDisplay(string stage)`（`Generation.cs`，default 返回 trim 后的原值）
3. `MapPolicyTargetLabelToDisplay(string label)`（`:2017`）+ 三个 helper
   `MapPolicyTargetLabelHeadToDisplay` / `MapPolicyTargetLabelShellToDisplay` /
   `MapPolicyTargetLabelTargetsToDisplay`

TargetLabel 投影覆盖记录摘要/效果摘要中的组合式存档标签；生产者继续保存原始 `TargetLabel` / `KingdomName`。

4. `MapPlayerPolicyEligibilityReasonToDisplay` projects fixed eligibility reasons and strictly parsed gold amounts; unknown values stay raw.
5. `TryMapPlayerPolicyFailureReasonToDisplay` projects known failure strings; unknown diagnostics keep the existing fallback behavior.
6. `MapPlayerPolicyNarrativeFallbackToDisplay` maps only exact fixed fallback facts, leaving authored/saved text untouched.
7. `PolicyEffectUiText.Name(module)` maps known module IDs to `AFPOL_UI_Effect_*Name`; descriptors remain raw for prompts/facts.
8. `LocalPolicyComposePopupVM.MapPlayerPolicyAutoDraftErrorToDisplay` maps known AutoDraft errors at the UI boundary; unknown strings pass through.

Raw fact/prompt builders remain separate from localized UI builders.

**为什么不做生产者侧改写**：`BuildPolicyTargetDisplayLabel` /
`BuildLocalPolicyEffectTargetLabel` 的输出被存进
`AppliedKingdomEffect.TargetLabel` / `KingdomName`，并经
`new PolicyTargetHandleSaveData { DisplayName = baseLabel }` 往返存档。
改写生产者 = 改存档语义，被禁止。所以只在显示边界投影。

**必须如实登记的向后兼容限制**：
老存档里已经存下的**原始中文回执**，只有在能匹配到已知 shell 时才会被投影；
匹配不到的部分（例如模型自撰的标签、`、` 连接但不在 shell 表里的名字）
会**原样显示**。绝不能声称「所有老存档都会被清成英文」。

当前 `BuildRawPolicyRecordEffectSummary` 中既有的状态词仍可能随 active language 写入共享摘要；最新复审文档已单独登记此限制，本包保留该既有行为，不声称 raw 事实全程与语言无关。

---

## 8. 当前交付状态与剩余动作

### 本次已完成

- HEAD/分支及多作者 dirty 状态已复核；当前只允许处理 §2 Package C 路径。
- 已运行资源/XML/占位符、Roslyn 语法筛选、1.3/1.4 本地化 DLL 解析及当前 CJK census；结果与限定见 §5。
- 当前 CJK census 的 783 行及 AutoDraft 补充消费者已分类，见 §7；raw Prompt/存档内容保持。
- CJK/盘点输出保存在 `.tmp_audit/`，仅为临时证据，不纳入提交。

### 剩余动作

用户选择「保持未提交」；因此本次不 stage、不 commit。共享资源中的 5 个主线 key 与 Package C 改动继续留在当前工作树，待主线另行拆分/协调。禁止 `git add -A`、不修改其他作者文件、不 force、不 push。

### 提交边界（本次按用户选择保持未提交）

当前两份资源文件包含 Package C 的本地化 key，也含 5 个并行主线 key；整路径 `git commit --only <精确路径>` 会把两者一起提交，而对应的 Npc/SettlementBalance/ModuleManager 生产者文件属于其他作者的 dirty slice。为避免把并行工作混入本包，当前没有 stage 或 commit。

用户已选择保持未提交，因此没有执行 partial-index 或组合提交。未来如需拆分共享资源，必须获得新的明确授权并以精确 patch 确认只含 Package C hunks，再单独安排提交方式。

- 不使用 `git add -A` / `git add .` / `git reset --hard` / 历史改写；不纳入 `NpcRulerPolicyBehavior.Generation.cs`、`NpcRulerPolicyBehavior.Persistence.cs`、`PolicyEffectModuleManagerUi.cs`、`SettlementBalanceEditorVM.cs`、root `HANDOFF.md`、CDE 计划及其他临时/未跟踪资产。
- 回滚方式仍为对已确认 focused SHA 执行 `git revert <SHA>`，或做锚点反向编辑；不清理现有证据。

---

## 9. 硬约束（违反即返工）

- 本包**只管** `PolicySystem/` 源码 + `afpol_ui_strings` 双资源 + Policy 测试 + 本文档。
  Memory / Knowledge / Weekly / Diplomacy / 内战不在本包内。
- 不打包、不产品构建、不 Stage、不部署、不 push、不加 C# 语言探测。
- 不动：Memory 模块/adapter/`MyBehavior.cs`；WorldMap 模块与资源；
  MCM/Terminal/Courier 主题、`UI/Errors`、`af_ui_strings*`；
  `package_mod` / `package_localization` 与打包脚本/测试；LanguageData、content-map、
  主 ledger/HANDOFF；Prompt、world-book 中英原文、协议标签、存档 key、程序集身份、构建脚本。
- 变量、换行、数字、模块 ID、规则匹配值必须逐字节不变。
- 复用已有 key ID，不要造重复语义的新 key。
- 换行在资源里必须写成 XML 实体 `&#10;`，不能写字面 `\n`。
- 写文件必须走二进制或 `newline=''`：`io.open(path,'w',encoding='utf-8')` 会把 CRLF 翻译掉。
  本仓库工作区是 **CRLF + UTF-8 无 BOM**。
- 热路径规矩：不得新增 Tick 扫描、反射、无效分配。显示投影只在「打开显示 / 发布结果」边界解析。
- 完成一个专项包之后，**不得**声称全仓无中文，**不得**声称构建已达发布状态。

---

## 10. 环境备忘

- `python` 不可用，用 `py`。
- 只有 Windows PowerShell 5.1（无 pwsh 7）。
- `py -c "…"` 内联在源码含 `"` 或 `[` 时会解析失败 → **一律写 `.tmp_audit/*.py` 再跑**。
- `pwsh -Command` 里塞超大内联字面量数组会 `Error: spawn ENAMETOOLONG` → 同样走文件脚本。
- 用 `>` 重定向出的文件会被 `read` 工具判成二进制 → 脚本里用
  `open(path,'w',encoding='utf-8')` 自己写。
- Roslyn 检查：
  `dotnet "C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore\csc.dll" /nologo /target:library /nostdlib+ /langversion:10 <file>`
  - 真语法错误码：CS1002/1003/1022/1026/1031/1513/1514/1519/1525/1528/0116，**CS1056 = 意外字符（泄漏的 `\x01` 标记）**
  - `/nostdlib+` 工件码：CS0518/CS0246/CS0656/CS8137/CS8179/CS0407/CS1729/CS0234

---

## 11. 结论

- 当前分支 HEAD `b196ecae4`（仅更新 Illustrator 资源/错误显示，不改本包 PolicySystem 或 afpol 资源）；Package C 显示投影与资源补充在 14 个 tracked 包路径落地，中英资源各 585 unique keys、键序/占位符一致。
- 当前 census 为 783 CJK 行，逐行按 UI 投影、raw Prompt/存档事实、内部诊断/匹配类别完成归类；这不是全仓无中文或发行验收。
- 资源/XML、J15 资源契约、1.3/1.4 本地化 DLL 解析、Roslyn 语法筛选及 diff-check 通过；`PolicyEffectModule.ContractTests`、生产构建、Bootstrap、Stage、实机/旧档均 NOT-RUN。
- `inventory_untranslated.py` 全仓结果因无关坏 JSON、扫描期间 Illustrator 文件变化及 ignore 保护的旧基线而不能作前后对比。
- **按用户选择保持未提交。** 未 stage/commit；两份共享资源的 Package C 与并行主线键留在当前工作树，后续由主线协调拆分。
