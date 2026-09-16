# 生图子模块：装备、盾牌/旗帜与纹章配色修正

日期：2026-09-17（Asia/Shanghai）。状态：离线源码修正完成，未部署、未推送、未实机出图验收。

## 范围与版本

- 实际工作树由 MCP `pwd` 与 Git 核实：`F:/AnimusForge-main`。
- 分支：`codex/af-main-refactor-continuation-20260831`。
- 起点 HEAD：`5f9f71154f29df3121fbfd21545986e39847f1dd`。
- 本轮空 intent 提交：`28ea8def29140cb9d24296cd0bff63c0b9bdf535`。
- 修正留在工作区；没有把用户预先未提交的改动混入生产提交。以下坐标对应该 HEAD 加本轮工作区变更，不宣称 HEAD 已包含修正。
- 所有本轮源文件、测试和本文都在 `extensions/AnimusForge.Illustrator/` 内。
- 未编辑主模组、根 HANDOFF、既有根 tools 脚本、原生纹章导出管线、游戏安装目录或存档。原有 ShoutBehavior、TtsEngine、工具和其他未跟踪文件保留。
- 子模块预先已有 HeroVisualExtractor、ConversationContextExtractor、VisualDirectorEngine 的未提交调整；保留其身份标签、背景隔离、场景空间硬事实与坐骑限制，在此基础上修正盾牌策略。

## 实际证据，而非猜测

读取本机 IllustratorCache 的最近四条 JSON 元数据，只读取，不修改。下列是缓存文件名及其中保存的最终 Prompt；时间为元数据原始 UTC。

1. `cb4283bed14712abaab208a925a4ea57_20260916000947822_c55940.json`，2026-09-16T00:09:47Z：
   - 已确认玩家装备中有“镶钉包边筝形盾”。
   - 同一提示词尾部却限制“不得因装备栏存在盾牌就自动添加”。
   - 本地回退的氛围模板明确插入“双方军队的旌旗仪仗在身后列阵隐约可见”。
2. `8a48be4c1236ce7eee5d1491114a697d_20260916000840817_2c89c8.json`，2026-09-16T00:08:40Z：
   - 导演正文写出军营与飘扬旌旗，事实区仍确认双方有盾。
   - 奥隆诺斯的 `#CCC3AB` 被程序描述为“暖赭色/深赤金”，实际是低饱和浅米色。
   - 源码将 Clan.Color/Color2 标成纹章主副色，并跳过 Banner.BackgroundDataIndex，不能代表真实纹章底层。
3. `7d6361841a3038321bf0e95564e6a106_20260916000224005_bbc3cb.json`，2026-09-16T00:02:24Z：
   - 原始事实明确含玩家“头部: 呆喵”，说明头部槽不是一概漏读。
   - 导演正文描述布衣、围巾等，却没有保留这件头戴装备。
   - 其他近期会面缓存中的玩家头部槽为空，不能据此断言同一套装备一直有头盔。
4. 源码另有独立缺陷：对话玩家离屏立绘固定 `useCivilian: true`，文字却按场景选择便服或战斗装备，可能把两套装备送入同一次请求。

这些证据支持“装备来源与提示词冲突”。尚未获得用户所指具体图片与对应参考图，不断言某张图片的颜色偏差全由导演造成，也不把文字修正等同于图像模型已完全遵循。

## 修正及责任坐标

所有下列路径均为仓库路径。

| 文件与行号 | 符号/职责 |
| --- | --- |
| `extensions/AnimusForge.Illustrator/src/Context/ConversationEquipmentSnapshot.cs:12-49` | Capture/Matches：主线程按人物身份匹配玩家及会话 Agent，复制穿戴槽，并用当前 MissionEquipment 的四个武器槽替换出生武器；找不到现场身份才退回选定的英雄装备栏 |
| `extensions/AnimusForge.Illustrator/src/Context/ConversationContextExtractor.cs:181-196` | 双方文字画像使用上述装备快照，保存 MainHeroCivilian |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs:39-41,80-81,157-173,505-545` | EquipmentCode/EquipmentSource/HeadgearDetail：序列化当前装备、保留来源，将头戴装备独立送入硬事实；陌生 MOD 名称不视为空槽 |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs:583-586` | 已装备盾牌可按现场持握或自然背负，不能为了纹章替换成旗帜；无盾逻辑仍保留 |
| `extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs:927-1004,1040-1092` | ExtractBannerDescription/DescribeBannerComposition/ResolveColorName：实际背景层及第二底色、徽记层的 HEX；移除把 Clan 染色当旗面配色的描述；采用克制颜色名称 |
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs:334-337,367-374` | 玩家与对方的离屏立绘共用各自文字画像的 EquipmentCode，不再强制玩家便服 |
| `extensions/AnimusForge.Illustrator/src/Engine/ScreenCaptureHelper.cs:1217-1241` | 可选 equipmentCodeOverride；在既有主线程舞台初始化中解码，同一装备同时用于 BodyProperties 与舞台装备 |
| `extensions/AnimusForge.Illustrator/src/Core/VisualFidelityRules.cs:5-11` | 共享保真契约：保留头戴、盾牌不是旗帜、旗帜需现场证据、标准纹章本色不受导演色调或王国染色覆盖 |
| `extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs:71-76,152-165` | 导演与最终事实区使用相同保真规则；导演不重新推测家族颜色与图腾 |
| `extensions/AnimusForge.Illustrator/src/Core/UniversalOpenAiImageClient.cs:326-329` | 在画风、负面词与随机指导之后重申保真优先级；仍是模型指令约束，不是图像语义校验器 |
| `extensions/AnimusForge.Illustrator/src/Context/EnvironmentVisualExtractor.cs:191,244,321` | 移除军团/旷野/围城模板中无条件旌旗仪仗暗示 |
| `extensions/AnimusForge.Illustrator/src/Context/ConversationContextExtractor.cs:229` | 对话旷野回退模板同步移除默认旗帜 |
| `extensions/AnimusForge.Illustrator/src/Context/WeeklyReportContextExtractor.cs:258` | 野战模板不凭题材添加军旗 |
| `extensions/AnimusForge.Illustrator/src/UI/Patches/WeeklyReportPopupIllustrationPatch.cs:383` | 构图随机项不再推荐旗帜填留白 |

## 性能与线程

- 新增装备采集只在发起/重绘请求时执行，不在 Tick 轮询。
- 每位英雄匹配成本 O(会话参与者数)，不新增整个 Mission.Agents 扫描；仅复制一份固定大小 Equipment，检查四个武器槽。
- EquipmentCode 在主线程冻结，后台只传字符串；舞台初始化沿用既有主线程分发。
- 纹章文字仍按已有 BannerData 图层数线性处理，只增加原先跳过的背景层。没有增加网络重试、GPU 渲染或图集扫描。
- 新增一条每英雄/每请求的装备来源、头部物品 ID、盾牌存在性诊断；不记录 API 密钥。

## 验证

- 最终子模块 `BannerlordApi=1.3` 和 `1.4` Release 构建：均成功，0 警告、0 错误。
- 构建输出：`extensions/AnimusForge.Illustrator/bin/fidelity/1.3/` 与 `extensions/AnimusForge.Illustrator/bin/fidelity/1.4/`。
- 新增专项：`extensions/AnimusForge.Illustrator/tests/test-fidelity.ps1`，分别加载两份目标 DLL，各 26 checks / 0 failures。
- 专项包含执行颜色转换、画像硬事实组装、导演最终组装、两协议/两随机强度的最终契约，并有装备快照路由与模板的源码断言。
- 现有 `tools/test_illustrator.ps1 -SkipBuild` 对最终 1.4 DLL：178 checks / 0 failures；未修改该脚本。
- 编辑器子模块范围诊断：0 条 error/warning；`git diff --check` 未报空白错误。
- 日志保存在 `extensions/AnimusForge.Illustrator/obj/fidelity/` 的 build-1.3.log、build-1.4.log、fidelity-1.3.log、fidelity-1.4.log、regression-final.log。
- 专项反射测试使用本机游戏依赖解析，不等于真实 1.3 游戏运行验收；双版本编译与目标 DLL 离线检查分别报告。

复验示例：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File extensions/AnimusForge.Illustrator/tests/test-fidelity.ps1 -AssemblyPath extensions/AnimusForge.Illustrator/bin/fidelity/1.4/AnimusForge.Illustrator.dll
```

## 未覆盖与上线条件

- 尚未部署；当前游戏仍可能运行旧 DLL。未经用户明确授权不覆盖游戏。
- 未请求付费生图、未进行真实 GPU 参考图验收、未验证“呆喵”的模型网格/贴图最终外形。
- 当前采集覆盖标准 Agent 穿戴槽与当前武器槽；仅通过额外实体/网格实现、不登记装备槽的 MOD 外观仍需现场图或专门适配。
- 无现场 Agent 时仍按场景选择英雄装备栏；不把另一套服装中的头盔强行移植过来。
- 模型仍可能忽略提示词；未实现输出图像的自动语义检查或保证像素级颜色一致。
- 原生纹章渲染、导出崩溃历史及多模态端点能力未在本轮更改或重新实机验收。
- 缓存默认图没有清除；获准部署并重启后需重新绘制，旧图不会因源码更新自动变化。
