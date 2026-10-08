# AF-ENGLISH 本轮验证

状态：`INVENTORY_VERIFIED / TRANSLATION_NOT_STARTED / LIVE_NOT_RUN`。

## 已执行

- 建分支时的 GitHub main：`a7d8421425cf96a52208c5019ecdbeb311ad8e48`。官方快照 32,124 个 blob 与 GitHub tree 一致；整树 `04689e8a3823ea67f89dae557dc48390e26679a1`、浅克隆提交 SHA 完全匹配。
- 提取工具 .NET 8 构建：0 警告、0 错误；没有新增 NuGet 依赖。6 项 unittest 通过，覆盖 Unicode/重复 JSON 值、Json.NET 字面换行、XML 实体、C# 字符/插值/注释/两 API 分支，以及精确的世界书路径。
- 对同一源基线完整提取两次，7 个被跟踪清单/压缩流的 SHA-256 逐字节相同。
- 3,144 个选中文件和 1,877 个延期文件的源 SHA-256 在提取后保持；译文状态全部未开始。
- 196,820 个 occurrence ID 不重复；45,826 个含汉字记录与独立解码筛选一致；26,469 个去重原文保留全部引用；原始汉字行 38,410 条。
- 所有 occurrence 均属于源清单，不含延期世界书路径；4 个 gzip 流完整解压与普通原件 SHA 相同。
- MSBuild 生产 Compile 输入不包含新增离线提取器，产品代码/资源、官方构建入口未修改。
- 定向废代码/冲突搜索只命中测试临时目录和说明次级 legacy 加载资源的有效注释；无冲突、弃用的新运行时桥或平行实现。最终 `git diff --check` 通过。

## 产物哈希

- `excluded-worldbook.json`：`b8e89befb99a6d238639ad146808b46e4c2ee9cfab628d96e3a93222d1080803`
- `han-review.jsonl.gz`：`574db3961db658e9f5ab7394cc7b9af07f1149ba422b6ed83f5a35751057296e`
- `han-source-lines.tsv.gz`：`378e047a2a07ca9df5536f7e2da87b69df3a7299c14ba679b28cd0d94ec313b6`
- `manifest.json`：`a1cea4edd3826a4169cde26ba28d7e0fc1211ace00fff93d0c53723ea95f3aa3`
- `occurrences.jsonl.gz`：`792da9c3d6a66ff898b17d10847ae26de5bd6dde76add91e83ae90d96fbb543d`
- `summary.json`：`c2cd9a408a71ce77f8f1913019e15973c8c9d34beb6513fe0dfa604ac59c9ac9`
- `unique-han-texts.jsonl.gz`：`35a9a1bcf418ebb6fd30ae06861e16cca314bb53092b1eef8544315e7d203424`

## 复跑

```powershell
python tools/LocalizationInventory/extract_text.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe
$env:AF_INVENTORY_DOTNET='G:\AFMOD\.dotnet-sdk\dotnet.exe'
python -m unittest discover -s tools/LocalizationInventory -p test_extract_text.py -v
git diff --check
```

本轮新增离线工具，没有被替换的产品路径或需要删除的旧实现。bin/obj/缓存与普通大清单不跟踪；完整压缩清单保留所有文本，并不是删减抽样。

## 未验证

- 尚无英化译文；未构建英化产品 DLL、未部署/发布 MOD 包。
- 未进行游戏内布局/菜单/存档、真实 LLM/provider、三渠道英文输出验收。
- 静态字符串清单不等于所有文本运行可达，也不等于每条含汉字文本都应翻译。
- 未 OCR 图片，未提取原生二进制或玩家个人运行资料；XML 提取是词法盘点，JSON 读取允许现有 Json.NET 风格控制符，不是严格资源语法认证。

<a id="af-english-skill-review-20261009"></a>
## Skill 规范自审（2026-10-09）

本节是同一代理的结构检查与规则/源码推演，不是另一个代理的独立评审，不代表未来译文已经验证。

- 创建位置 `.agents/skills/af-english-localization/`：短入口、UI 元数据、翻译规范和审查门槛；无复制扫描器、假宿主 hook、空资源或初始化 TODO。
- `skill-creator/scripts/quick_validate.py` 通过；frontmatter、UI YAML、显式 `$af-english-localization` 示例、正常隐式选择策略及 Skill/本轮新增文档引用与锚点检查通过。
- 仓库 `AGENTS.md` 只新增英化专用路由，保留原 AF 维护与安全边界。Skill/项目规则不声称改写内置 system prompt、自动刷新旧线程或全局生效。
- 模式推演：只要求审查时只读；创建 Skill 不能启动产品翻译；未来推送需当前授权；缺少相关实机/构建证据不得报告为通过，已验证的独立部分可带限制交付。

### 真实清单样本的审查结论

| 样本/源码 | 正确处置与规范依据 |
| --- | --- |
| `AnimusForge.SiegeAftermathIntervention/SetsSettlementCivilianGatherProfile.cs:39` 的 `不要召集` | 当前方法用它否决召集命令，属于输入/否定匹配；不得直接当文案替换，英文适配要保留必要旧输入并验证反例 |
| `AnimusForge.SiegeAftermathIntervention/SiegeCastlePrisonerAllocationProfile.cs:116` 的 `'十'` | `ParseChineseNumber` 用于数值计算，是字符/解析逻辑；不能改成英语字母或删掉旧中文解析 |
| `content/foundation/AF.Foundation.Localization/ModuleData/Languages/CNs/language_data.xml:2` 的 `id` | 技术语言标识，不能因值含汉字替换为 `English`；需沿用既有语言选择契约 |
| `src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs:668` 的注入标题 | 外壳标题可纳入英化，但 `variant.Content`/关键字资料本体延期、功能不禁用；还须核对标题是否是机器标记 |
| `content/modules/AF.Module.Prompt/CustomPrompts/PlayerCustomPromptRule.json:3` | 自然语言可在获准译文批次修改，AFEF 标记及 user/事实语义必须与消费者一致，不能翻译为新的协议 |
| 去重文本 `座` 的 5 个引用 | 同时出现在 Policy 输入计数和外交/公报展示，不能按一个 text_id 全局替换；引用用途逐个确认 |

这些样本未被修改。结构验证只是格式证据；上述结论为本代理阅读当前实际消费者后的规则自审，未运行真实译文/LLM/存档或游戏 UI 测试。

### 检查与未验证项

- 新 Skill 文件统一为 UTF-8/LF；定向 TODO/冲突/失效路径检查和 `git diff --check` 通过。没有本轮产生的旧产品逻辑需要删除。
- 本轮唯一主台账/HANDOFF/PLAN 互相链接，原始盘点文件和产品/构建路径未改；未修改其他 AF/GCCZ 或全局 Skill。
- 早期整篇链接检查碰到原 HANDOFF 的既有历史锚点缺失；未扩展本轮去修历史文档。本轮引用验证限定新增 Skill 和实际新增的说明/路由，不宣称旧历史链接全部有效。
- 未做全局安装，也未验证另一宿主的实际自动发现/已有线程热刷新。未使用独立子代理或自动 LLM 行为评测；未开始产品翻译或游戏验收。
