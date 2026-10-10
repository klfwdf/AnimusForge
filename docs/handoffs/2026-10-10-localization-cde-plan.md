# C / D / E 外部并行接力计划

阶段：计划已建立，等待用户交给三个外部 Agent。基线 `43360e691`，main。
最新目标：N 网完整英文发行；两个ZIP名区分CN/EN，包内原文件名保持；中文原版完整保留。
三个包是本轮新分工，不是历史第二轮B/C。旧合并接力已被本计划替代。

| 包 | 工作 | 独占生产范围 | 接力 |
| --- | --- | --- | --- |
| C | 政策效果正文、历史与结果显示 | PolicySystem源码、afpol资源及政策专项测试 | [C文档](2026-10-10-localization-package-c-policy.md) |
| D | 外交内政面板、内战动态显示与结果通知 | Diplomacy、Kingdom/CivilWar源码、afdip/afkingdom资源及专项测试 | [D文档](2026-10-10-localization-package-d-diplomacy-civilwar.md) |
| E | 缺失英文世界书、Prompt和默认内容资产 | 平行英文JSON资产、英文资产对应清单及资产专项验证工具 | [E文档](2026-10-10-localization-package-e-english-assets.md) |

## 共同规则

- 读取真实 AGENTS.md、维护Skill和对应案例；先检查根、分支、HEAD、dirty。
  日常维护Skill位于 `.claude/skills/animusforge-maintainer/SKILL.md`；代码/API敏感任务
  阅读 `docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md`，不把其他历史工作当新授权。
- 阶段标明调查/修改/验证/总结；修改前说明具体文件，修改用apply_patch，修改后验证。
- 不构建模组、不Stage、不部署、不正式打包、不push，不加入C#语言探测。
  可运行资源解析、纯脚本测试、prepare-only；编译型回放标NOT-RUN，不冒称已执行。
- 不改一键脚本、Bootstrap、程序集、存档类型/键、模型协议；不重写共享事实。
  原事实若兼有UI用途，新增独立显示投影，保留完整信息和变量。
- 各包只能写独占路径。共享注册、content-map、主台账/HANDOFF、af_ui_strings、
  UI/Errors、MCM/Terminal、Memory、WorldMap、Economy/Issue与打包由主线负责。
  跨边界只发送精确符号与窄补丁建议，主线接线；不随意抢文件。
- 源码运行时中文外置是最终目标；当前切片完成不代表全仓清零。
  禁止把中文转Unicode逃逸、删原信息、隐藏错误、改标签制造“0中文”。
- 不回滚他人；保留未跟踪英文资产、NuGet与临时证据。只提交自己变更，
  用精确路径 `git commit --only`，不要提交他人共享index内容。每切片focused commit。
  独立新文件先 `git add -- <精确路径>`，回滚focused inverse，不hard reset。
- 允许维护本包测试使其反映双语行为，必须保留线程、生命周期、事实/数值断言；
  不改共享hash基线、不放宽门禁来绕过不相关失败。无法跑的检查说明具体原因。
- 中英资源同键集、唯一ID、占位符完全相同。英文fallback与英文资源一致，
  换行用XML实体，避免字面反斜线n。只在打开/状态/发布边界解析，不增加Tick扫描。
- 交付列SHA、真实文件/行号、键数与资产覆盖数、PASS/FAIL/NOT-RUN、跨包待接线。
  未译/延期/未验证不算完成。不因“测试锁中文”或“动态尾巴来自事实”排除实际UI。

## 已核实基线与主线任务

- B原交付253对新键，C原交付234对新键；原报告不等于全英文验收。
- Weekly注册主线 `7341afe55`；MCM下拉34对主线 `4e24e532d`；
  prefab非法尾巴 `5d9b42359`；WorldMap复审63对修复 `e1d625e1b`；
  两ZIP投影/门禁 `ced9a8d8b` 已提交，但EN实际缺项导致不能发行。
- 盘点 `artifacts/localization/untranslated-only-current.json` 基线 `cc81193c3`：
  UI确认293位置，消费者候选1289位置；这些是旧快照下限，不是当前全部未译总数。
- 标准英文世界书45文件，中文标准829文件。按稳定身份15匹配候选，
  1个中文JSON非法导致仅14可解析验证；814无可验证英文对应。不能说英文世界书全套完成。
- 另外三套世界书未英化：权游1431、低配519、战锤345个含中文JSON；
  与标准814共3109个待英文对应文件，文件数不是待翻译字符串数。
- 主线保留：Memory复审收口、共享错误UI、经济/Issue遗漏、固定诊断/协议抽取、
  shared DLL内嵌默认内容与CN/EN装配/最终验收。三个外部包不能替代这些任务。

验证入口：资源契约 `tests/content/J15ContentContractTests/LocalizationResourceContractTests.py`；
真实解析 `LocalizationRuntimeTests.ps1`，引用 `.tmp/build_check/1.3` 与 `1.4`；
资产盘点 `tools/localization/inventory_untranslated.py`。记录引用解析不是当前产品编译。

当前未构建、未验实机/长战役/旧档、未生成正式CN/EN包、未上传N网。
