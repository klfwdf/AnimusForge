# 政策交付复审与配图错误显示

阶段：修改意图；基线 main `116503545`。全英文发行仍 ACTIVE / NOT RELEASE READY。

C交付工作树不是已验收提交：效果描述由UI、周报、AFEF、历史与Prompt共享。主线复审发现特殊分组在playerVisible=false时仍解析显示文本，其他共享摘要也会变为当前游戏语言。新增独立raw描述路径并恢复原消费者，显示路径保留C的本地化；不改存档类型/键、协议、数值和目标规则。原HEAD中已经本地化的状态词单独登记，不能宣称全部内部文本已经语言无关。复审修复由既有residual_ui_audit子智能体执行，主线核对结果后精确提交。

主线处理NPC三条议程通知、SettlementBalanceEditorVM单位范围和缺失ModuleManagerSummary键。范围只包含这些显示表达式和afpol资源；单位范围在VM创建时缓存，既有参数/数量/验证保持。

配图固定失败经Scope及GenerationUpdate同时用于原诊断和通知，保留原生产者异常及ErrorMessage，只在UI ERROR变量绑定边界新增IllustrationFailureUiText.Project。首片覆盖卡片人物采集/试采五项、API常见固定失败及超时/通信异常包装，未知或第三方错误逐字回退，不删信息。仅显示边界解析，不新增Tick扫描、后台TextObject、语言探测或模型请求。涉及IllustratorUiText、新显示helper、中英illustrator_strings资源，以及既有静态验证入口的授权投影还原。

退出门：资源唯一ID/键集/变量/fallback/英文无汉字、真实显示接线、原错误/诊断/Prompt/缓存/调度生产者保持、未知值回退；既有两DLL资源解析只作资源证据。当前产品C#编译、Bootstrap、实机/旧档、正式ZIP均NOT-RUN。保护外部C/E/B和HANDOFF未提交内容，不构建/Stage/部署/push。回滚focused inverse。
