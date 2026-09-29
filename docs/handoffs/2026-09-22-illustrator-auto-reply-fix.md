# Illustrator 自动重绘正文触发修复（2026-09-22）

工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。独立 Illustrator UI 适配修复；不修改或部署宿主、DialogueUI。源码基线 `2a01e944`，修复提交 `57b71bb7`、`5c25e28e`、`91d04338`（最终生产源码）。

## 真实证据与根因

- 07:11:38、07:12:06、07:13:04 宿主 `Mod_Logic.txt` 均出现 `main_reply_display_ready`，MCM 自动重绘值为 true。用户反馈这些回复未触发生图；旧诊断没有触发来源，不能把已有两条图请求归为自动。
- 旧补丁仅监听 `ShowNpcSpeechOutput`，它属于场景喊话，原生自由对话正文不经过它；补丁安装成功还禁用了原版续聊事件。
- `QueuePendingAutoRedraw` 误放在百科完成/失败分支，会话分支没有调用。
- 游戏 Bootstrap 日志确认当前加载宿主为 `versions/1.4/AnimusForge.dll`，SHA256 `2C3DD9E54561EC2498906C25EE336991D88559152E94452CCF1B4008CD60A49B`。只读反编译确认安装版 Overlay 调用 External 五参数正文回调入口；工作区新宿主调用 `SubmitNativeConversationForOverlayAsync`。不能将工作区宿主源码当成已安装 DLL。

## 核实源码位置（提交 91d04338）

| 路径（extensions/AnimusForge.Illustrator 下） | 行号 / 符号 | 责任 |
|---|---|---|
| src/UI/Patches/ConversationIllustrationPatch.cs | 239–306 / TryPatchHostNativeConversationReply、WrapReplyCallback | 反射缓存安装版 External 与新源码 Overlay 的正文完成回调补丁；保留原 UI 回调，不监听 TTS/逐字流/喊话。 |
| src/UI/Patches/ConversationIllustrationPatch.cs | 308–349 / LogAutoRedraw、OnConversationEnded、OnConversationContinued | 写宿主有界事件日志；结束会话清缓存；缺宿主正文入口时才降级续聊事件。 |
| src/UI/Overlays/IllustrationCardPopup.cs | 391–399 / QueuePendingAutoRedraw | 单个待补画标记，回调绑定原窗口，不借用新弹窗。 |
| src/UI/Overlays/IllustrationCardPopup.cs | 458–469、543–751 / ExecuteConversationGeneration、ExecuteConversationGenerationCore | 首张手动图成功后启用；完成/失败消费忙时补画；诊断标记 automatic；缓存命中仍检查现场 owner。 |
| src/UI/Overlays/IllustrationCardPopup.cs | 831–937 / AutoRedrawActiveConversation、CaptureAutoReplyObserver、IsSameConversationTarget | 每次提交只捕获 owner/窗口/会话 epoch/人物；正文完成主线程校验后启动；本轮玩家输入和 NPC 正文直接送入导演上下文，避免早于历史提交读到旧回复。 |

运行频率：每次对话请求及正文完成一次；无新增 Tick 轮询、全场扫描或模型请求。忙时连续回复合并为最新一次补画，复用既有环境/人物参考缓存。关闭插画窗口会停止当前窗口自动流程；结束对话/换存档失效，重新进入仍需手动首张。原版无宿主正文入口的降级路径、多人群聊不视为本轮已验收。

## 验证、部署及回滚

- 串行 API 1.3 / 1.4 Release 编译各 0 警告、0 错误；只读检查源码、实际安装宿主反编译及最终部署 DLL。未运行离线测试/审计脚本，未调用付费模型。
- 07:34:44 既有 `tools/deploy_illustrator.ps1 -BannerlordApi auto -Configuration Release` 部署游戏 v1.4.8 独立模块；最终 DLL SHA256 `B1153B7F7ACD330B871674E3EA6AB6F85954D48D17F1946B8550F298B8F411E0`。安装/构建 DLL 一致，模块 XML 和六个 prefab 共七份 XML 哈希一致，部署后无游戏/启动器进程。
- 首次部署前的回滚备份：`artifacts/deploy-backups/AnimusForge_Illustrator/v1.4/20260922-073155`（旧问题版本）。`20260922-073444` 是本轮中间候选，不是原始基线。源码只逆向本节三个提交，不回滚其他任务。
- 尚未游戏验收。重启后开启自动重绘 → 手动生成第一张成功 → 保持插画窗口与对话开启 → 发新消息，NPC 正文完成应进入新生图。绘图中再回复应稍后补画；关闭设置或结束对话后不能启动排队结果。独立面板/顶部75%两种模式均需验证。
- 新日志：`Modules/AnimusForge/Logs/Mod_Logic.txt` 中 `[Illustrator] [AutoRedraw]` 的 `hook_attached → request_observed → reply_received → start`，忙时为 `queued_first_image/queued_busy`，失效时为 `dropped_stale/dropped_disabled`；生成 trace 增加 `conversation_generation_trigger.automatic`。日志不保存正文或密钥。
