# 肖像、地图对话与刷新优化（2026-09-25）

本次仅修改 DialogueUI 子模块。修改前检查点：`42aebcb9`。该检查点保存了三个相关文件原有的未提交修改；其他工作区改动保留。

## 修复内容

- `PresentationRouter.cs` 新增 `MapConversation -> AFDialogueMapConversation` 路由，不再以 `Mission.Current != null` 作为地图对话前提。
- 新地图 prefab 保留原版 `MapConversationScreenButtonWidget`、`TableauData`、`DialogController`、`IsBarterActive` 和 `ExecuteContinue`，内部引用共享的 `AFDialogueConversation` 布局。地图界面按电影/VM 生命周期释放，不因临时 mission 变化而丢失适配器。
- `AFDialogueConversation.xml` 在圆形裁剪区域内、人物 tableau 之前添加不透明深棕纹理背景；纹理由已有输入面板的无边框区域派生，生成脚本为 `tools/prepare_portrait_background.py`。同时修复原有 `AnswerList` 路径遗漏 `AFDialogueConsoleFrame` 的问题。
- `LiveSpeakerPortrait.cs` 不再采样场景人物动作频道，改用原版 `act_inventory_idle_start`、双手武器索引 `-1`、关闭换装动作，避免卫兵持械/攻击动作被误作完整肖像动作重放。不改场景 Agent。
- 外观采样上限 10 Hz，卡顿后不补跑循环；身体编码和缩放仅在身体/种族/性别变化时重算，装备按固定 12 槽比较后按需编码；旗帜使用原版 `BannerCode` 缓存。缓存归属单个肖像会话，关闭后随会话释放。
- 移除覆盖层未绑定的六个选项及重复对话文字刷新，继续使用原版 VM 的 `AnswerList`。
- 输入限制补丁增加包装器归属检查，避免换肤回退后继续接管原界面输入。

## 验证记录

- 通过现有 `build.ps1` 构建 `BannerlordApi=1.3`（引用 1.3.15.110062）和 `1.4`（本机 1.4.8）：各 0 警告、0 错误；脚本确认 AF 宿主 DLL 哈希未变。
- `python tools/verify_presentation.py`：4 个 XML、21 个已注册素材、地图交易/继续/控制器绑定、原版答案列表路径、不透明圆形背景层及已删除 VM 属性的无引用检查通过。
- `dotnet run --project tools/portrait-tests/PortraitTests.csproj -p:BaseIntermediateOutputPath=../../artifacts/portrait-tests/obj/ -p:MSBuildProjectExtensionsPath=../../artifacts/portrait-tests/obj/`：130 项断言通过。测试直接链接生产肖像更新器，用替身统计编码调用；不调用游戏渲染器。
- 连续 120 次不变外观刷新，身体/装备/旗帜各只编码 1 次；装备原地修改、等值装备替换、身体变化、种族/性别变化、旗帜变化和无 Agent 普通 NPC 切换均覆盖。

## 尚未验证与产物

未向游戏部署，未取得游戏内验收。待实际检查：卫兵姿态与脸部可见性、野外领主/普通兵对话、普通与 AI 模式切换、交易开关、继续对话、不同分辨率和关闭重开。缓存测试不能证明 GPU 成本或实际帧率收益。

本地可部署产物：`artifacts/stage/1.3/AnimusForge_DialogueUI` 和 `artifacts/stage/1.4/AnimusForge_DialogueUI`。
修改前文件备份：`artifacts/portrait-map-fix-baseline`。回滚应逆向撤销本次修复提交，保留检查点及其他作者改动，不使用 hard reset。
