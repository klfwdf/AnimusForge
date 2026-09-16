# 场景描写要求与近景半身构图策略

2026-09-17 用户明确要求强化场景描写与百科近景半身构图，解决肖像仅有装备清单或压成纯黑背景、缺乏环境空间感的问题。

## 背景与修复目标

此前百科肖像与会话插画中，视觉导演（VisualDirectorEngine）偶现以下偏离：
1. 导演输出退化为机械复述装备部位与武器清单，缺乏镜头机位、环境空间与光线色彩的视觉描述。
2. 背景处理倾向纯黑、近乎全黑或抠像黑幕，使画面丢失卡拉迪亚世界的纵深感与真实材质。
3. 镜头距离过远或动作过僵，未能突出百科肖像所需的“近景半身（含头盔与肩甲）”视觉重点。

现在：
- **近景半身构图（PortraitFraming）**：百科肖像优先近景半身（头顶至腰部入镜，保留完整头戴与肩甲），按重绘次数在“近景半身（约占2/3画幅）”、“轻微侧身近景半身（头顶至胸腹）”与“中景环境半身（更多画幅留给可辨环境）”之间平滑轮换，不强制做复杂动作或摆拍道具。
- **四段式场景描写契约（SceneComposition）**：导演提示词必须严格分为四个段落：
  - `【人物与镜头】`：镜头景别、人物站位，装备简要概括（最多一句，严禁罗列清单）；
  - `【场景空间】`：背景空间、几何形体、地面材质与纵深（不少于20字符）；
  - `【光线与色彩】`：主光源、环境光与反射填充，暗部保留灰阶与材质细节，拒绝低曝光死黑（不少于15字符）；
  - `【空间关系】`：人物与前中后景遮挡、景深关系（不少于15字符）。
- **门禁与本地回退（HasRequiredSceneDescription）**：
  - 后三段（环境、光线、空间）正文合计必须占主体描述一半以上（`>= 50%`）。
  - 严禁纯黑/全黑/黑幕背景描述（如“背景纯黑”“黑色背景”“black background”等），命中即视为不合格。
  - 不合格或空输出时，直接通过 `BuildLocalSceneDirection` 进入本地规则合成回退，绝不自动额外发起付费 API 重试。
- **真实性契约与非具名布景**：百科肖像无现场依据时设计“非具名艺术布景”，不把布景说成真实城堡或已发生事件；会话/周报依据真实事实，严禁凭空添加未发生事实。
- **约束优先级置顶（UniversalOpenAiImageClient）**：`SceneComposition` 与 `Contract` 作为最终优先级块追加于提示词末尾，在任何画风偏好与随机度（0~100）下均不被覆盖，不削弱无盾、禁背盾、无旗与固定 RGB 显示契约。

## 验证与测试结果

在工作树 `F:\AnimusForge-main` 下完成双 API（1.3 / 1.4）编译与全量离线回归测试：

1. **双 API Release 编译**：
   - `dotnet build src/AnimusForge.Illustrator.csproj -c Release -p:BannerlordApi=1.3`: **0 警告，0 错误**。
   - `dotnet build src/AnimusForge.Illustrator.csproj -c Release -p:BannerlordApi=1.4`: **0 警告，0 错误**。
2. **全模块审查回归（test-full-audit.ps1）**：
   - 1.4 DLL：**108/108 checks passed, 0 failures**（含新增 `Scene null director output safely falls back locally` 断言）。
   - 1.3 DLL：**108/108 checks passed, 0 failures**。
3. **装备/纹章/保真回归（test-fidelity.ps1）**：
   - 1.4 DLL：**53/53 checks passed, 0 failures**。
   - 1.3 DLL：**53/53 checks passed, 0 failures**。
4. **子模组离线全量回归（tools/test_illustrator.ps1）**：
   - 双 API 联编 + 180 项测试：**180 checks, 0 failures**。
   - `-SkipBuild` 离线断言：**178 checks, 0 failures**。

## 源码范围与 SHA-256

工作分支：`codex/af-main-refactor-continuation-20260831`

| 文件（相对仓库根） | 行数 | SHA-256 |
|---|---:|---|
| `extensions/AnimusForge.Illustrator/src/Core/VisualFidelityRules.cs` | 35 | `7ef9492b959d4808fc6bf54e60575cb4684ecb775f365654280848579be49e48` |
| `extensions/AnimusForge.Illustrator/src/Core/VisualDirectorEngine.cs` | 537 | `03419c37fdcb0b0674cd97bee697d3d03d70e8e9d1a9cb0f72c2bc9fdee235a5` |
| `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs` | 481 | `751219a3e0e1f9744e28ab165cb224105e37a8a5fa982ed0b4aaf4ecf11fba32` |
| `extensions/AnimusForge.Illustrator/src/Core/UniversalOpenAiImageClient.cs` | 894 | `12af22ce739c93eb5ef0783026ab8fad4e2713927377f1fe3506a0a66122f358` |
| `extensions/AnimusForge.Illustrator/tests/test-full-audit.ps1` | 266 | `5da8ab1e4fb4fb38ae20971f93eb3cdf9c8b5749a99bfb5a54d84a824433fdde` |
| `extensions/AnimusForge.Illustrator/AGENTS.md` | 14 | `240189433ceb20b7ccd985be087e7dd7c934849c830c6a4599a097622a1d7fb6` |
| `extensions/AnimusForge.Illustrator/README.md` | 295 | `12244201a8bb44f3878df5c82647f0e8cb9bf3a04f1954340524afb7b2b6ff22` |
