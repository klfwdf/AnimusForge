# 当前NPC年龄绑定与背景人群反馈

工作区 F:/AnimusForge-main，分支 codex/af-main-refactor-continuation-20260831；源码基线 c0d58e05，修改前检查点 7162d9c。只修改独立Illustrator；保留其他作者未提交内容。

## 已确认的证据

- 《旅舍红砖下的邀舞》诊断 20260921T164441_1de2cd67185e4ec3939711097db758c9，输入女镇民约59岁，来源标记为普通NPC会话Agent；原安装DLL的ResolveAppearanceAge优先解析现场BodyProperties，失败才回落CharacterObject.Age。旧记录未存原始BodyProperties或年龄来源分支，不能断言历史原始值已直接取证。
- 安装游戏的DefaultAgeModel确认TavernVisitor范围20到60，上限不含；原版CommonTownsfolkCampaignBehavior.CreateTownsWomanForTavern随机分配年龄。因此59合法，但不能据此证明这张脸必须画出明显衰老。小兵《大殿无虞》输入39，使用同一普通NPC分支。
- 《旅肆踏歌》诊断 20260921T170003_cdf001540ec74be6bcab6076105a2781，MVID仍2082849d-7de6-4489-8dbe-ee4091d4fbce。请求有附近角色名单，未含【附近人群活动依据】，导演场景段只描述建筑家具。它未运行6d166f51人群修复，不能作为新版失败证据。

## 本轮实现位置

前缀 extensions/AnimusForge.Illustrator/src/，行号对应本轮修改。

- Context/ConversationEquipmentSnapshot.cs:10-21，ConversationNpcAgeSnapshot：只持有原始年龄、实例编号和来源的托管值。55-83，CaptureCharacter：只接受当前一对一Agent，冻结BodyProperties一次并直接读取Age；Mission内匹配失败不退回模板。无Mission的大地图原装备路径保持，年龄未知。
- Context/CharacterAppearanceSnapshot.cs:35-39，FromAgent：可接收冻结BodyProperties，使年龄与两张立绘来自同一次读取。
- Context/ConversationContextExtractor.cs:195-215，普通NPC分支：删除模板年龄解析兜底，保存年龄证据与明确来源。
- Context/HeroVisualExtractor.cs:23、95：事实输出年龄来源，不改百科/英雄年龄取值。
- UI/Overlays/IllustrationCardPopup.cs:471-483：在既有生成诊断上下文记录conversation_npc_age，不增加模型请求。
- Core/VisualFidelityRules.cs:9：数值年龄不允许替代可见成熟度和皮肤/须发特征。

未宣称旧版确实串人或解析错误。此次收紧来源、去掉兜底并补证据，实际值仍可能为59；不将其强改年轻。未修改染色、场景复制/投影、画廊或主模组。此前人群修复6d166f51保持并包含于当前构建。

## 验证、性能和部署边界

API1.3与API1.4 Release均0警告/0错误，git diff --check通过。源码核对现场未知/装备缺失停止、大地图未知年龄、同一快照传入立绘与诊断。没有跑离线测试/审计脚本，没有调用付费模型。

每请求只读一次准确Agent身体参数，无新增扫描、反射、Tick或模型调用；诊断在原scope后台写一次有界元数据。

产物 artifacts/illustrator-npc-age-20260922/{1.3,1.4}/AnimusForge.Illustrator.dll。

- 1.3 SHA256：6CB84996D603726E7B3BB29AC88FC5BA288DE1029F5C45B246DA795F17463AB8
- 1.4 SHA256：BF4DAFD06675D71908CDF9660AF7CE709807CE043632DECCFC3AA07F7D55BBEE

本轮未部署，安装仍为2082849d版；人群修复和年龄改动尚未游戏验收。部署后同一女镇民检查conversation_npc_age原始值、实例编号、立绘身体参数与提示词；换同模板NPC检查编号和值随实例变化。现场背景人群及面部保真仍须看成图，不以编译或提示词宣称已保证。

回滚只反向撤销本轮源码提交，保留6d166f51人群修复；修改前检查点7162d9c，不hard reset。游戏文件没有改变。
