# DialogueUI 独立子模组交接（2026-09-20）

## 当前交付

工作区 `F:/AnimusForge-main`；当前任务使用已有分支 `codex/af-main-refactor-continuation-20260831`，其他作者会并行提交。主体未修改，根 HANDOFF 未更新，未推送。用户后续明确授权部署，已于2026-09-20 08:36首次部署独立 `Modules/AnimusForge_DialogueUI` 至游戏1.4.8。只加载自己的 `AnimusForge.DialogueUI.dll`，依赖 AF 先加载。

生产初版 `1b2605fb34b1d015fb0f9a5d681d9ac93adc90c1`；默认 AI 与离开行为 `e5794537`；美术 `c97c7388`；意图检查点 `d3d4679e`。最终编译使用 e5794537 所包含的同一源码工作树，编译时提交基线仍是1b2605fb，随后精确提交；产物元数据同时记录编译基线与源码归档提交。

场景原生对话现在默认打开AI输入，支持手动切回原版全部选项；右侧“离开”排到下一UI tick，验证会话身份后调用AF原关闭与原版EndConversation。场景喊话卷轴中的“离开”仅调用原取消输入。AI输入/默认初始化不扩展到MapConversation。默认模式只记在每个原AF VM的弱关联状态内，资源刷新不覆盖玩家手动选择。

## 核实源码位置与责任

下列位置以本子目录为相对根；源码版本以对应文件最后修改的 `1b2605fb`／`e5794537` 为准。行号用于定位，迁移时优先追踪符号。

| 文件、行号与符号 | 源码版本 | 覆盖责任 |
|---|---|---|
| `src/PresentationRouter.cs:27–85`，Install / LoadPrefix | 1b2605fb | 仅指定movie使用原VM包装；外层movie名称不变，使Illustrator仍能注入原按钮。 |
| `src/PresentationRouter.cs:91–144`，CleanupFailedMovie / PreparePrefab | 1b2605fb | 半加载事件/节点清理；按原WidgetFactory目录规则注册；资源归属冲突拒绝接管。 |
| `src/PresentationRouter.cs:147–195`，LayerLoadPostfix / ReleaseOwned / Shutdown | 1b2605fb | 使用私有identifier加载入口覆盖资源刷新；喊话包装保留到原Popup.Close。 |
| `src/Shout/ShoutUiAdapter.cs:34–131`，Install / TryWrap / Release | 1b2605fb | 只标记原普通喊话与交易输入调用上下文，原提交、暂停、百科和释放owner不变。 |
| `src/Shout/ShoutUiAdapter.cs:164–232`，Capture / ShoutContext.IsCurrent | 1b2605fb | 主Agent、Mission、targeting、epoch/session身份；失效拒绝提交，不另选人。 |
| `src/Shout/ShoutPresentationVM.cs:40–111`，ExecuteToggleHistory / RefreshAvailability / OnFinalize | 1b2605fb | 草稿由原VM持有；按需读取一次最多260行当前目标可见历史；退出不finalize原VM。 |
| `src/Native/NativeUiAdapter.cs:28–146`，Install / TryWrap / Tick / Release | e5794537 | 原SP布局成功后才接管overlay；鼠标命中只覆盖已接管原VM；原生命周期释放。 |
| `src/Native/NativeUiAdapter.cs:153–219`，OwnerPresentationState / RequestLeave / ProcessPendingExit | e5794537 | 默认AI一次标记、延后离开、owner复核、原EndConversation；不承诺取消网络。 |
| `src/Native/NativeUiAdapter.cs:304–315`，OverlayLayout.Tick | e5794537 | 首次自动调用原SwitchTalk；之后尊重手动模式；同步可选绘图入口可见性。 |
| `src/Native/NativeUiAdapter.cs:350–459`，NativeLayout.TryCreate / Build | e5794537 | 三分区、保留原答案/说服/继续控件；已有布局标识时拒绝二次套用。 |
| `src/Native/NativeUiAdapter.cs:478–565`，Tick / StyleOption / RefreshPortrait / Dispose | e5794537 | 新选项完成创建后装饰；按人物变化更新真实头像；恢复原控件与清理。 |
| `src/Native/NativeOverlayVM.cs`，属性、命令转发与OnFinalize | e5794537 | 原VM唯一业务命令，更多菜单；离开排队及释放后禁止晚事件写入。 |
| `src/DialogueUiSprites.cs:31–94`，EnsureLoaded / Shutdown；`src/DialogueUiButtons.cs:9–32`，Style | 1b2605fb | 按需缓存九切素材、资源刷新复挂、独立三态brush与纹理释放。 |
| `GUI/Prefabs/AFDialogueShout.xml` / `AFDialogueNativeOverlay.xml` | e5794537 | 多行卷轴/记录/摘要；三栏工具条、AI输入、发送蜡封和离开。 |

主模块只读接缝：`ShoutTextInputPopup.Open/Close`、`ShoutBehavior.OpenShoutTextInput/ShowShoutTradeChatInput/GetAuxiliarySceneDialogueHistoryLinesForExternal`、`AnimusForgeNativeConversationOverlay`及其VM；不修改其源码。当前独立桥不是 `AnimusForge.Api.V1` 的公开承诺；将来主模块改名/签名不兼容时应保留原界面并更新这层桥，不能复制另一条LLM或记忆管线。

## 构建证据与限制

最新双API Release均0警告、0错误。1.3引用固定完整NuGet包 `1.3.15.110062`；1.4引用本机实际 `v1.4.8`。构建前后原AF两份DLL哈希相同。

| API | 最终 DLL SHA-256 |
|---|---|
| 1.3 | `19E98EB30BF6C3E88DD2900A9DD259DE626CE7D2F2DBAA79B1DB18B771F24CE0` |
| 1.4 | `623FCBBF1755F6BCBEE2595B5AD286590F569E7033F41350A6F0CAC422460481` |

用户明确要求不再运行离线测试；默认AI/离开修订仅做源码核对与双API编译，没有运行探针、fixture或静态测试脚本。早前两份旧DLL曾各138项检查通过，其记录仍在 `artifacts/verification`，**不可作为当前最终DLL或新增离开行为的通过证据**。这些旧检查本来也使用本机1.4运行库，并非1.3实机。

未验证：实际Gauntlet布局、GPU九切/原生肖像、1366×768至超宽的真实UI缩放、中文输入法、焦点、百科/历史/赠礼往返、说服与任务退出、等回复时离开、多人喊话/TTS、资源刷新及退出清理。静态预览只证明美术方向与示例布局，不证明实际控件位置。没有启动游戏或旧存档实测。

运行频率：纹理首次需要时加载并缓存；资源刷新只重新注册缓存；历史每输入实例首开记录时读取一次；目标校验为已有引用/代际比较；人物变化才重建肖像；选项按新增队列更新。没有新增逐帧Agent全扫描、重复反射查找、后台轮询或业务存档。

## 后续整合

保留 `GUI`、包装显示和布局代码。由主模块原owner直接加载展示层，去掉独立Gauntlet重定向与私有反射调用标记；提交、取消、暂停、历史、赠礼及后处理继续由原owner持有。公开API无需在本次扩展。

整合时停用独立版，或显式选择唯一展示provider；当前原生面板已有标识会阻止重复重排，但这不替代整合方的加载选择。应一并迁移默认AI的一次初始化、离开owner检查、原模式切换和资源刷新生命周期。不要删除原SPConversation答案与说服子控件，也不要把场景喊话换成一对一目标。

## 产物与回滚

`artifacts/packages/AnimusForge_DialogueUI_0.1.0_bannerlord_1.3.zip` 和 `..._1.4.zip` 各含一个 `AnimusForge_DialogueUI` 模块目录。使用匹配游戏API的一份，不加载两份。运行包不包含生成原图、测试工具、AF实现或TaleWorlds程序集。模块内README、此HANDOFF及build.json随包交付。

本次为首次安装，之前没有同名模块；退出游戏并取消勾选本模块即可停用，不新增存档字段。首次安装回滚标记为 `artifacts/deploy-backups/20260920-083603/NEW_MODULE.txt`。需要移除部署时仅处理 `F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge_DialogueUI`，不得动相邻模块。源码只定向逆向本任务提交：先 `e5794537`（默认AI/离开），再 `1b2605fb`（独立代码），需要移除资源时再 `c97c7388`。其他作者提交穿插在同分支中，禁止按整段范围回滚或hard reset；交接文档提交可独立撤销。

部署由本模块 `deploy.ps1` 完成，16文件哈希一致，游戏进程未运行；原AF两份实现哈希不变。详细文件哈希、实际目标与回滚位置见 `artifacts/deployment.json`。部署1.4 DLL哈希为上表最终1.4值。没有修改启动器勾选状态；需在启动器勾选“AnimusForge - 羊皮纸对话界面”并放在AF后。
