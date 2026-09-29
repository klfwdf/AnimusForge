# 完整装备离屏立绘：百科与现场来源统一

日期：2026-09-17。状态：子模块源码与离线验证完成，未部署、未推送、未实机验收。

## 范围和版本

- 仅修改 `extensions/AnimusForge.Illustrator/`，主模组、游戏安装目录及用户先前改动未覆盖。
- 工作树 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。
- 本轮开始于 `28ea8def29140cb9d24296cd0bff63c0b9bdf535` 加上一轮未提交工作区修正。
- 本轮 intent 为 `a42ba1fd`；本轮生产修正仍是工作区变更，没有把用户已有修改混入生产提交。
- 本文补充并取代 `extensions/AnimusForge.Illustrator/docs/mcp-equipment-heraldry-fidelity.md` 中“普通NPC使用模板、仅四个武器槽、百科沿用原立绘导出”的历史状态。

## 生效规则

1. 百科人物包括玩家：每次生成/重绘读取当前 CharacterTableauWidget.EquipmentCode，解码为文字装备快照，同时把同一个完整码交给独立离屏舞台。不再按贵族、流浪者或非战斗身份猜选装备，不再复用首次截图，不向百科共享 Tableau 发起保存。
2. 现场玩家与对方英雄：优先匹配身份的现场 Agent 穿戴与武器；无匹配 Agent 时，Mission.DoesMissionRequireCivilianEquipment 优先于敌对、围城和职业判断。无 Mission 才使用定居点/野外回退规则。
3. 普通对话 NPC：同一个匹配会话 Agent 同时采集 BodyProperties 与完整装备。无该实例则停止本次生成，不用兵种模板重新随机装备和脸型。
4. 完整标准装备共 12 槽：5 个穿戴槽（头、身、腿、手、披风/肩部）、5 个武器槽（包括额外槽）、坐骑和挽具。保留空槽，不从另一套服装补入缺失装备。
5. 所有当前武器槽使用 MissionEquipment 更新，避免把已经丢弃/交换的武器仍按出生装备画入。
6. 文字与人物参考图共用装备快照；图库默认图仍可展示，但重绘重新采集。完整读取不等于单一视角能看清全部遮挡部件，也不强制坐骑入最终画面。
7. 新生成需要离屏渲染。开关关闭则明确提示开启，不更改用户设置；读取/渲染失败则停止而非悄悄降级为其他服装或旧截图。

## 源码坐标

| 路径 | 行号与责任 |
| --- | --- |
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs` | 146-184：百科当前装备采集及独立离屏；318-339：设置与完整快照校验；368-396：玩家、英雄、普通NPC各自完整装备参考图及失败停止 |
| `extensions/AnimusForge.Illustrator/src/Context/ConversationContextExtractor.cs` | 167-202：场景服装回退与普通NPC画像；312：不再另用首个会话对象覆写已匹配脸型 |
| `extensions/AnimusForge.Illustrator/src/Context/ConversationEquipmentSnapshot.cs` | 12-34：英雄来源；37-55：普通NPC身份匹配及脸型；58-68：完整装备复制及全部五个实时武器槽 |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs` | 164-184：ApplyEquipmentSnapshot 共用全槽文字及原生序列化；580：全部武器槽 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs` | 1217-1241：英雄装备码；1272-1300：普通NPC也按装备码初始化舞台，脸型采用同一个实例快照 |
| `extensions/AnimusForge.Illustrator/tests/test-fidelity.ps1` | 扩展百科来源、场景优先级、NPC一致性、全部槽位与旧装备清空专项 |

## 验证与性能

- 最终子模块 1.3 / 1.4 Release 构建均 0 警告、0 错误。
- 专项分别加载两份 DLL，各 53 checks / 0 failures。
- 专项新增真实 Equipment 序列化 fixture：12 个槽分别装入不同 ID 物品，逐槽确认全部出现在完整码中；执行 ApplyEquipmentSnapshot 验证空头槽、清除旧头饰/盾牌。来源路由另有源码断言。
- 通用回归针对最终 1.4 DLL：178 checks / 0 failures。子模块编辑器诊断无 error/warning。
- 初次新增专项缺少显式加载 TaleWorlds.Core 导致测试宿主 TypeNotFound，修复测试依赖加载后复跑两份 DLL 通过；不是掩盖生产编译失败。
- 日志：`extensions/AnimusForge.Illustrator/obj/fidelity/full-equipment-build-1.3.log`、`full-equipment-build-1.4.log`、`full-equipment-test-1.3.log`、`full-equipment-test-1.4.log`、`full-equipment-regression.log`（均在同目录）。
- 按生成请求采集，不新增 Tick 扫描。匹配限于会话参与者，复制固定 12 槽、更新固定 5 个武器槽；百科直接读已定位控件。引擎读写仍在主线程，worker 使用冻结字符串。

## 未覆盖

- 未进行游戏内百科换装、城镇便服、野外战甲、普通NPC随机装备、丢盾、模组头盔外形及真实 GPU 图像验收；离线通过不代表视觉验收。
- 标准 EquipmentCode 仅表达原生物品/修饰词槽位；MOD 通过额外实体、独立网格或原生装备码不表达的外观层实现的装备，不在本轮通用保证范围。
- 只读不到具体普通NPC实例时会停止，需要真实会话场景而非用兵种模板代替。无法解析自定义百科控件时同样停止，不猜装备。
- 周报是事件回顾，不在本轮改成采集玩家当前现场；没有历史装备快照时不宣称还原历史穿着。
- 未部署。本轮 DLL 仅在子模块 `bin/fidelity/1.3/`、`bin/fidelity/1.4/`；获准部署、重启并重绘后才可实机检查。
