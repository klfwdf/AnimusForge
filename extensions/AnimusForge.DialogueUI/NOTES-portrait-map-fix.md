# 肖像、地图对话与刷新优化（2026-09-25）

## 第二轮：重新生成背景并匹配地图实际外观

本轮修改前意图检查点：`b4a23db`。以下内容替代上一轮的棕色纹理和地图普通 NPC 默认外观回退。

- 用户确认通过已配置 relay 重新生图；工具实际返回模型为 `gemini-3.1-flash-image`，1024×1024。原图归档为 `assets/source/portrait-background-generated.png`，完整提示词同目录 `portrait-background-generated.prompt.txt`。最终风格为场景中性的暗炭灰—古铜灰柔和渐变，无人物、无建筑、无风景、无粗颗粒，贴合胡桃木金框，适用于室内和野外。
- 从金框贴图中提取真实透明开口，背景遮罩向内沿下方延伸 4 个源像素。独立背景控件放在金框后方，不再受人物圆形裁剪限制。贴图仅 223×239，不增加每帧处理；生成工具提供静态空框预览 `artifacts/portrait-background-fitted-preview.png`。
- `MapPortraitSource` 在原版 `MapConversationTableau.SpawnOpponentLeader` 完成后捕获实际 `AgentVisuals` 的身体、装备、种族、性别、服装颜色和旗帜，不复算脸型种子、不选兵种默认装备、不读取场景动作。只保存托管外观数据和装备副本，不保留场景或原生模型。
- 缓存按 `MapConversationVM.TableauData` 对象身份隔离；原版切换数据、销毁 tableau 时清理，人物重新生成时替换。准备完成前暂不显示地图肖像，避免短暂显示错误的兵种默认脸。每次 10 Hz 外观检查仅查询弱键缓存，不反射、不遍历场景、不重建装备。
- 新增四项回归断言：本次遭遇装备/脸型覆盖兵种默认值，使用实际种族/性别/颜色，重复刷新不编码，同兵种不同遭遇更新个体。肖像断言共 134 项。
- `verify_presentation.py` 验证真实椭圆开口每个像素均被覆盖、背景在金框后方及布局坐标匹配；`verify_map_portrait_contract.ps1` 以只读元数据检查本机 1.4.8 实际 DLL 的捕获与清理入口。
- 游戏内仍需确认劫匪头巾/脸型一致及各分辨率边缘表现；静态预览和替身回归不等于游戏实测。

## 上一轮记录

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
