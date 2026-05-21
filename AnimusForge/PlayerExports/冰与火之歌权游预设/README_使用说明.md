# 冰与火之歌 / 权游 AnimusForge PlayerExports 预设

生成时间：2026-04-29 19:40:46
来源目录：D:\下载\权游全家桶
目标目录：D:\steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\PlayerExports\冰与火之歌权游预设

## 已生成内容
- event_data/WorldOpeningSummary.json：权游世界总背景。
- event_data/KingdomOpeningSummaries.json：按文化/势力整理的开局摘要。
- knowledge/rules/*.json：世界书、文化传统、外交战争、事件编年史规则。
- personality_background/_TEMPLATE__*.json：给具体NPC改写人设时使用的模板。
- voice_mapping/VoiceMapping.json：复制自现有 AnimusForge 导出目录。
- unnamed_persona/UnnamedNpcProfiles.json：保持安全空结构。

## 设计取舍
我没有把 actionrules.txt 里“双重身份、解限、喵AI、忽略成本”等元AI/越狱式内容照搬进预设，因为 AnimusForge 是游戏内NPC系统，直接搬会污染NPC口吻并可能破坏行为约束。
已保留并优化为：封建世界观、中世纪口吻、有限认知、文化偏见、家族/誓言/婚盟/战争成本、不得替玩家读心、不得把玩家宣称当事实。

## 使用方式
在 AnimusForge 里如果能选择 PlayerExports 配置档，选择“冰与火之歌权游预设”。
如果没有选择界面，可以先备份现有“卡拉迪亚编年史”，再按需复制本目录内的 knowledge/rules、event_data、personality_background 模板到当前存档导出目录。

## 注意
如果你的底层游戏世界仍是原版卡拉迪亚，本预设会提供“权游式叙事层”，但不会自动把游戏实体变成临冬城、凯岩城或史塔克/兰尼斯特。若你装了权游总转换模组，效果会更接近完整权游世界。
