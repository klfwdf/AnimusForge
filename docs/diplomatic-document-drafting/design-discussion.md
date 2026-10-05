# 外交文书代拟设计访谈

日期：2026-10-05。状态：Q1—Q8 已确认，用户随后授权实施；产品22cbb228已本地实现与离线验证，后续已按用户授权部署，详见[交付说明与验证边界](../diplomacy-round-design/UPDATE_20261005.md)。完整方案与验证限定见 [DESIGN.md](DESIGN.md)。

## 用户已明确的范围

- 玩家提供标题或粗略信息，由 AI 帮助起草完整外交公文；参考当前政策帮写的设计。
- 外交文书编辑页增加帮写按钮，位于发布按钮右侧；三个按钮排齐，发布按钮居中，按钮不得遮挡印章。
- 按钮使用符合游戏氛围的文案；依据用户授权由设计者选定“书记官代笔”。
- 本任务是帮写功能设计，不扩展为外交系统大改。
- 使用 grill-with-docs，逐轮访谈并记录术语与明确决定；方案收敛之前不实施。

## 术语与历史约束

沿用 [外交术语表](../diplomacy-dialogue-design/CONTEXT.md) 中的外交宣言、外交文书草稿、文书代拟和已执行外交结果。

此前 [外交衔接访谈](../diplomacy-dialogue-design/design-discussion.md) Q7 的“系统不得替玩家写宣言”，在本功能范围依据最新请求细化为允许玩家主动委托代拟；这不改变玩家掌握正式发布的既有边界，也不授权系统自行决定外交事项。

## 访谈收敛

两轮八项决定均已有用户明确回答。篇幅沿用外交最小/最大字数，写作偏好沿用外交现有MCM编辑器，不增加独立配置。此前默认300—500字及独立提示词/接口的建议被Q5、Q6明确否定，不得作为实施要求。用户已明确授权“按确定的方案开始做”，实施状态见末尾当前代码图。

## 工作区证据

实施分支 main；首次核验 HEAD：14b5d8f6772e71330cd29d98b400b8cd7f42f4f5。首次 git status 显示已有政策代码、所有权/范围图、交接与定居点编辑设计等未提交改动；本任务保留这些改动。

以下是该 HEAD 与当前工作树的源码事实；政策 UI 有既有未提交改动，不声称全部属于上述提交。

- `PolicySystem/UI/KingdomPolicyUi.cs:623–684`：ExecuteAutoDraft / ApplyAutoDraftResult 使用原表单、生成时禁用帮写与发布、成功直接回填、失败保留输入、表单改变丢弃结果；地方政策对应 `PolicySystem/UI/LocalPolicyUi.cs:343–399`。
- `PolicySystem/Core/PlayerPolicyAutoDraft.cs:82–104,151–163`：BuildMessages 只读取专用写作提示词、标题和玩家原文，不读取世界局势、历史政策或评议规则；已有标题要求原样保留。
- `PolicySystem/Core/CustomPolicyBehavior.AutoDraft.cs:69–114`：主线程准备、后台请求、主线程回写和读档失效检查；现政策关闭后忽略回调，网络请求并不取消。
- `content/modules/AF.Module.Diplomacy/GUI/Prefabs/WorldDiplomacyComposePopup.xml:5–15,30–43`：纸面1180×740、正文上限6000、取消146宽/发布180宽/间隔28，当前两按钮整组居中。印章属于卷轴背景；新增右按钮需结合背景调整高度/尺寸并做视觉验收。
- `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPlayerApplication.cs:46–76`：发布会登记外交事件、公开文书并排队解析诉求和动作，擅增实质承诺可能影响既有机制。
- `src/modules/AF.Module.Diplomacy/Presentation/WorldDiplomacyWidgets.cs:430–437,483–517`：现编辑页只有正文栏；同目录 `WorldDiplomacyPresentation.cs:54–60,77–82`：新宣言与外交回应共用编辑页。

## 首轮确认（Q1—Q4）

1. 在现有纸面输入提纲并直接回填，还是另开委托与预览弹窗？建议前者，学习政策流程。
2. 能否补充玩家未提及的具体外交条件？建议只扩写表达，不添加国家、金额、期限、领地或新的外交动作。
3. 只读取玩家输入和专用写作提示词，还是加入世界局势/外交历史？建议首版只读取玩家输入，学习政策当前材料边界。
4. 新宣言与外交回应均提供代拟，还是仅新宣言？建议两处都支持，由玩家提供各自提纲；是否读取收到的原文另随材料范围确认。

用户明确回复四项均采用推荐，故决定如下：

- Q1：在现有纸面输入标题或粗略意图，点击代笔，成功直接回填；玩家可继续编辑，再自行发布。无独立委托/预览弹窗，不新增标题输入栏。
- Q2：只扩写文风、结构和表达，不自行添加国家、金额、期限、领地及新的外交动作。条件不足时不编造具体条款。
- Q3：只读取玩家纸面内容和写作要求，不读取世界局势、外交历史、个人记忆或收到的原文。写作要求的来源经Q6明确为现有外交MCM偏好与简短固定代拟任务约束，不另设可编辑提示词。回复原案所需细节由玩家写进提纲。
- Q4：新宣言与外交回应均提供代拟；两处都由玩家自己提供意图并决定正式发布。代拟不改变原案归属、外交执行或公开传播链路。

## 第二轮确认（Q5—Q8）

- Q5：用户指出已有外交公文长度MCM设置，要求代拟使用同一最小/最大字数区间；撤销默认300—500字建议。
- Q6：用户要求不设独立配置，文风提示词与现有外交MCM偏好一并沿用，设计尽量简略。代拟沿用外交现有接口配置，不增加政策式独立接口下拉或新的提示词文件。
- Q7：以纸面当前文本为本次输入。玩家没有改内容时，再点击也表示重新措辞；不返回缓存稿，不保留第一份提纲作为固定重写来源。不新增版本列表或撤回按钮。
- Q8：生成期间不能发布或重复代笔，但可编辑与取消。文本变化则不覆盖新输入，关闭/读档后不回填；失败保留原文并可手动重试。不自动重试或发布。

## 第二轮设置证据

再次核验 main @ 9fedde7a98da598cd99362203675dfd147344507，政策等先前dirty已被其他工作提交；本任务未回滚或重新提交那些产品变更。本任务访谈检查点为7b48ac62。

- `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs:601–607,2250–2256`：外交最小/最大字数范围1—1000，默认40—200，标点计入。
- `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs:1700–1724`：GetDiplomaticDeclarationCharacterRange，分别Clamp后将最大值提升为不小于最小值；沿用该设置归一化语义。
- `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs:2303–2311,4034–4040,4055–4065`：WorldDiplomacyPrompt为可编辑原偏好；共同契约getter拼接固定外交合同和guard，不等同于原偏好。
- `src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyPromptContractRules.cs:174–238`：现DECLARE任务合同还要求actions等结构及自主外交内容，不适合作为代拟完整任务。代拟仅共享MCM原偏好，附简短忠实扩写与正文输出合同。
- `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyGeneratedCompletionApplication.cs:102–106` 及同模块 `Domain/WorldDiplomacyTextRules.cs:27–31`：当前生成正文归一化/6000上限处理与非空检查不构成最小/最大字数硬校验。实施需验证代拟输出长度，不宣称已有现成校验器。
- `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyLlmClient.cs:60–105,128–132,448–516,550–559`：外交优先事件/叛乱API、配置不完整回退主API，沿用所选温度与Token；可传maxAttempts=1，支持CancellationToken与读档失效检查。`:153–174` 的思考参数协议降级属于既有provider兼容路径，不新增内容重写循环。默认两次失败重试不直接照搬到代拟。

上述访谈时尚未构建或实施；本轮实现已另行完成构建与离线验证，未运行游戏、调用真实AI、推送或部署。

<a id="player-draft-code-map-22cbb228"></a>
## 当前实现代码图（22cbb228）

以下坐标对应本任务产品提交，替代前文历史源码坐标作为接续入口；前文保留当时调查事实。

| 源码与一基行范围 | 符号 / 覆盖职责 | 未覆盖边界 |
| --- | --- | --- |
| src/modules/AF.Module.Diplomacy/Presentation/WorldDiplomacyComposePopupVM.cs:69–105 | BodyText修订计数、CanPublish、ExecutePublish；原公开类型保持 | 正式外交权限与动作继续由原发布链校验 |
| 同目录 WorldDiplomacyComposePopupVM.AutoDraft.cs:31–149 | ExecuteAutoDraft、单槽ProcessAutoDraftCompletion、OnFinalize；单窗口请求、关闭取消、修订/generation拒收与主线程回填 | 无存档/外交历史写入，实机UI未验 |
| 同目录 WorldDiplomacyWidgets.cs:322–345,396–424 | 原窗口tick只在活跃时检查一槽结果，读档关闭与Finalize接入 | 不改变发布/回应消费者 |
| src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPlayerDraftApplication.cs:9–38 | TryPrepare；主线程读取现有篇幅/原偏好及准备网络闭包，后台只请求/解析 | 不读取世界局势、档案、来文或人物 |
| src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyPlayerDraftRules.cs:9–93 | 冻结输入、单body输出合同、严格JSON/重复字段/长度检查及可见字符计数 | 自然语言忠实度由prompt与玩家审阅约束，非硬语义证明 |
| src/modules/AF.Module.Diplomacy/World/WorldDiplomacyLlmClient.cs:62–107,208–229 | TryPrepareSingleCall主线程冻结接口、单次请求；SendPreparedAsync与既有发送共享protocol fallback/transport/解析 | 不改旧外交默认失败重试，不进入自主生成/执行队列 |
| 同目录 WorldDiplomacyBehavior.cs:1700–1724 | 既有字数归一化入口由private改internal，代拟消费同一实现 | 无MCM新设置、公开API或行为变更 |
| content/modules/AF.Module.Diplomacy/GUI/Prefabs/WorldDiplomacyComposePopup.xml:9–52 | 输入区、提示文字、对称三按钮和真实数据绑定；发布中心对齐纸面 | 原始卷轴/印章资源未改，实机缩放/焦点未验 |

58项生产链接测试、原双API/Bootstrap构建、真图像mask几何核验已PASS；实际产物/源码hash与日志在本地receipt.json。原Compose VM从Widgets文件物理移动到同一namespace/类型的partial文件，类型身份不变。意图检查点87e6e58e，产品22cbb228；本轮仅本地提交，未Stage/部署/push/打包，源码回滚使用产品inverse commit。
