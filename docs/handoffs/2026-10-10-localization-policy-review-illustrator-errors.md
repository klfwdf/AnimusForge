# 政策交付复审与配图错误显示

阶段：修改意图；基线 main `116503545`。全英文发行仍 ACTIVE / NOT RELEASE READY。

C交付工作树不是已验收提交：效果描述由UI、周报、AFEF、历史与Prompt共享。主线复审发现特殊分组在playerVisible=false时仍解析显示文本，其他共享摘要也会变为当前游戏语言。新增独立raw描述路径并恢复原消费者，显示路径保留C的本地化；不改存档类型/键、协议、数值和目标规则。原HEAD中已经本地化的状态词单独登记，不能宣称全部内部文本已经语言无关。复审修复由既有residual_ui_audit子智能体执行，主线核对结果后精确提交。

主线处理NPC三条议程通知、SettlementBalanceEditorVM单位范围和缺失ModuleManagerSummary键。范围只包含这些显示表达式和afpol资源；单位范围在VM创建时缓存，既有参数/数量/验证保持。

配图固定失败经Scope及GenerationUpdate同时用于原诊断和通知，保留原生产者异常及ErrorMessage，只在UI ERROR变量绑定边界新增IllustrationFailureUiText.Project。首片覆盖卡片人物采集/试采五项、API常见固定失败及超时/通信异常包装，未知或第三方错误逐字回退，不删信息。仅显示边界解析，不新增Tick扫描、后台TextObject、语言探测或模型请求。涉及IllustratorUiText、新显示helper、中英illustrator_strings资源，以及既有静态验证入口的授权投影还原。

退出门：资源唯一ID/键集/变量/fallback/英文无汉字、真实显示接线、原错误/诊断/Prompt/缓存/调度生产者保持、未知值回退；既有两DLL资源解析只作资源证据。当前产品C#编译、Bootstrap、实机/旧档、正式ZIP均NOT-RUN。保护外部C/E/B和HANDOFF未提交内容，不构建/Stage/部署/push。回滚focused inverse。


## 主线接收结果（2026-10-10）

阶段：静态验证完成并本地提交；产品 `19f11c941`，20个政策源码/资源/测试文件，基线 `b8ad0a54a`。外部C原报告为未提交交付，主线将其与已核对的5个主线资源键、raw修复、36对模块名/说明键共同接收；没有纳入Prompt、世界书、Economy或其他作者文件。

- `PolicyEffectSaveCodec.DescribeRawPlayerVisibleInstances` / `CustomPolicyBehavior.BuildRawPolicyEffect*`：共享分组逻辑保持，原事实/历史/周报/Prompt消费者走raw，UI走显示方法。raw方法逐字对照 `116503545f99e168811f9551ee90bbdfcc73c701`（只允许符号重命名）。原共享清理函数也恢复该基线，新增 `CleanPolicyDisplaySystemText` 仅用于UI，避免改变raw清理规则。
- `PolicyEffectUiText`：18公开模块和10内部别名的36个名称/说明键；管理面板、编辑器菜单和可见效果描述使用，descriptor及Prompt原文保持，未知第三方ID原值回退。仅UI打开/结果刷新解析，不新增Tick或目录扫描。
- `BuildPolicyFailurePopupText`：23个固定原因先映射后走原技术数据过滤；6种效果失败原生产者及后台生成恢复原值。未知原因沿既有过滤规则，不能声称所有未知正文已英文。
- 英文目标列表的COUNT是总数，修为“(N ... total)”，不再误报为N个额外目标。泛目标中文对应“共N”；不改目标选择、数值或原事实格式。
- `Program.cs`：13组完整CN/EN行集、NPC raw原中文精确行集；目标/数值/分组/期限/零值腿/生命周期保留。独立资源展开和fixture还原静态验证272项通过。

主线实际复跑：raw30项、模块显示90项、失败投影92项/23source-derived案例、测试oracle272项、26资源对及diff-check PASS。afpol中英各621唯一键、英文汉字0；既有1.3/1.4 Localization DLL各2484次TextObject解析PASS（中英/其他语言英文fallback/切回）。源码方法/XML静态验证及旧引用资源解析不等于当前产品执行。

配图错误首片另已提交 `b196ecae4`；本政策提交未再触碰Illustrator文件。

限制：基线raw record summary原有四个PolicyUiTexts状态词，仍保留，不能声称全部raw与语言无关。旧孤立收件记录可能显示真实原文；固定中文Prompt、诊断和事实源码外置仍OPEN。当前C#编译、政策程序集契约执行、Bootstrap、实机、旧档、正式CN/EN ZIP均NOT-RUN；未构建、Stage、部署或Push，不新增语言探测。

证据：`.tmp_audit/policy_raw_restore/{verification,effect_ui_verification,failure_projection_verification,policy-ui-oracles-verification}.json`；精确提交路径和文件哈希 `.tmp_audit/policy-review-commit-manifest.json`。临时证据不随产品提交。回滚 `19f11c941` 的focused inverse，禁止整文件恢复共享资源或清理其他作者工作树。
