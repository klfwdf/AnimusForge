# Package E：完整英文世界书与默认内容资产

阶段：待接力。先读[共同规则与文件边界](2026-10-10-localization-cde-plan.md)。
任务：补全平行英文资产；逐条保留中文原件，翻译忠实、不删内容或用摘要替代原文。
不改C#语言探测/加载、协议解析或打包脚本；英文资产由主线现有双包投影落原文件名。

## 独占范围

- `content/modules/AF.Module.Onboarding/PlayerExports/Calradia Chronicles/**` 英文资产。
  这是现有未跟踪英文目录，必须读已有内容后合并，不清空或覆盖其他作者。
- 对其他世界书新建平行英文目录或同级 `.en.json`；英文目录名可用
  `Calradia Chronicles Lite`、`Game of Thrones Chronicles`、`Warhammer Old World Chronicles`。
- `content/modules/**` 的 `.en.json` / `.en-US.json` 平行默认内容与Prompt文件；
  允许增补缺失EN，不写任何原始中文 `.json` 或 `.zh-CN.json`、语言XML/Prefab。
- 对应/验证工具独立新增 `tools/localization/validate_english_assets.py`，
  清单 `content/localization/english-asset-map.json`（新文件，主线后续接线）。
  清单至少 source中文路径、english路径、原包target、状态、哈希、身份保持/需联动项。
- 独立交付 `docs/handoffs/2026-10-10-localization-package-e-english-assets-delivery.md`。
- 禁止修改 `inventory_untranslated.py`、package_localization/package_mod、content-map、
  LanguageData、任何C#、原中文世界书、历史基线或其他Agent源码/资源。

## 实际缺口与顺序

1. 标准卡拉迪亚：中文829 JSON，现EN45；匹配候选15（6配置/事件+9persona），
   中文拉盖娅原JSON含非法换行，故可解析匹配14。814文件暂无稳定可验证EN对应。
   其中中文337知识规则、486人物；EN只有30规则、9人物。
   先逐文件建立对应关系，再补漏译；现45文件也要对照中文核实无缩写、改写或漏字段。
2. 低配卡拉迪亚519个含中文JSON：只有原文/结构一致时才复用标准EN结果，
   不能按heroId相同就推断人物描述等价；记录原文hash。
3. 战锤旧世界345、权游大帝版1431个含中文JSON，分别保其设定，不混用标准世界书。
   四套当前共3109待EN对应文件，文件数不是字符串数；分套分批交付，不能仅做标准就报全量。
4. 默认JSON与已有平行Prompt逐schema验证：TownAmbientDialogue.json 的
   Memory/ReplyHints仍中文，先新增忠实EN资产；Prompt nested模板、关键词、
   数值约束、JSON类型/数组条目、占位符和action tokens逐一核实，已有EN不是自动PASS。

## ID、协议与格式

- 保留JSON结构、数字/布尔、实体ID、HeroId、VoiceId、TopicNumber/Code、动作/存档字段。
  Translate文案、Keywords时保留检索语义和别名覆盖，不任意删数组项以减少翻译量。
- 当前EN30规则把中文Id也改成英文，无法与CN规则稳定匹配。不要再仅按英文显示名
  猜测对应；人工对照完整中文内容，清单给明确原source/EN target。
- 中文字符存在于机器ID时，不能为“0中文”单改Id破坏引用；保持原值并标
  `requires_host_contract_migration`，给主线精确依赖。这不算纯英文发行完成。
- 两个已确认精确协议 `[ACTION:SCENE_ACT:动作程序]` 与 `[RELAY:接力编号]`
  原样保留并登记联动；不能单改EN JSON。还要审查执行仪式阶段/说话人标签：
  现EN RuleBehaviorPrompts把中文阶段翻成English，C#是否支持需主线联动核实。
- 中文原件非法JSON：完整保留字节，记录错误；可以从原始文本人工准确提取并生成
  有效EN，但不能修中文原件、吞掉非法段落或声称原文件解析通过。
- EN内容不含意外中文文案；0Han只是必要条件，不能代替翻译忠实与协议一致。
  使用结构化JSON解析，不粗暴字符串替换、不输出Unicode转义假装清零。

## 验证与交付

- 翻译前记录中文原件SHA，之后全套原件SHA相同，完整保存原文件集。
- 每套source/EN覆盖矩阵、缺失/重复对应、schema/变量/关键机器字段一致；
  专用验证器覆盖合法与非法JSON、错ID、缺数组项、中文残留负例。
- 对每条新增翻译逐内容复核，术语一致，不把定制背景改写为原版百科简介。
- 英文资源可与CN原路径/文件名不同，但清单给原包target，包内落原名由主线负责。
  当前打包支持相对路径/人物前缀/相同ruleId，新的清单接线需主线实施。
- Embedded defaults内嵌C# DLL仍是主线接入任务，磁盘EN完成不算运行时全英文。
- 分套分批focused commit；提交自己实际英化资产，不顺带add未核实的其他未跟踪EN。
  不调用翻译网站发送私有内容或使用未配置的翻译API；保留授权与来源证据。
- 标明未做C#加载/协议联动/正式ZIP/实机与旧档；不冒称可上N网。

退出门：四套世界书及实际打包默认资产有完整逐文件EN对应、原CN字节保持，
结构/占位符/协议差异逐项处理或明确交主线，验证器与focused提交齐全。
