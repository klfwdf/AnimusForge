# 生图全链路排查：立绘、端点、提示词与耗时

## 适用与验证边界

本次覆盖百科、会面/对话、周报/快报的现有生图链路。代码修复和双版本实际 DLL 离线回归已完成；不是玩家现场、原生 GPU 或真实服务实机验收。没有自动推送、部署或更新之前的 1.5 ZIP。另一会话正在改对话 UI，完整工作树曾因 `ConversationReplyWaitOwner` 缺失编译失败；本轮先以已提交基线加精确生图改动隔离构建，不回退或替换其他作者文件。

**收尾补验：**另一会话完成提交后，完整当前工作树双API+Bootstrap也已构建通过，并对完整树的两套实际DLL重新运行上述18组生图审查全部通过；18553源码输入hash无变化。早期组合编译失败和隔离证据均保留，不改写历史。

## 1. 接口到底怎么填

- OpenAI 标准编辑接口为 **POST `/v1/images/edits`**；`images` 是复数，`image/edits` 不是标准路径。
- 非完整 URL 模式建议填服务根地址或 `/v1`。有参考图默认走 edits/multipart，真实上传 `image[]`；没有参考图才走 generations。Gemini 等对话图像模型或显式对话通道使用 chat/completions。
- 端点 query 与路径分别处理：query 原样保留用于请求路由，不拼在后缀中间，不写入日志。已给出完整接口时，切换协议保留服务原有前缀，不重复追加 `/v1`。
- 非精确模式的 `/image/edits` 拼写错误在本地拒绝，并提示复数路径。特殊第三方别名必须由服务文档确认，不能猜；完整 URL 模式保留用户明确的地址。
- 配置 edits 但没有输入图会拒绝，不付费发出伪装的文生图请求。精确 generations 加参考图也拒绝，避免静默丢失人物身份参考。
- 服务或模型不支持 edits 时停止并记录 HTTP/错误。不会偷偷去掉参考图改文生图；应选择确实支持图像输入的模型/通道。不会替玩家重置已有模型、服务或密钥。

## 2. 人物立绘失败查哪一步

原生 Gauntlet 舞台仍由游戏主线程创建/更新并串行退休，不用屏幕截图、随机人物或去掉装备冒充成功。每人物全身与头部是两次独立原生采集。

此次将默认每视图等待从 3500ms 调整为 12000ms；人物不再因 240 个应用帧提前结束（高帧率下可能不到两秒），仍受墙钟超时/取消限制。旗帜原有帧数限制不变。文件探测最多每 50ms 一次，不增加每帧目录扫描；预热与退出状态仅在必要节点复制到后台日志。

| 日志/失败码 | 含义 |
|---|---|
| `portrait_queue_wait` / `portrait_queue_acquired` | 等待串行采集舞台；查看等待耗时 |
| `portrait.appearance_snapshot_missing` / snapshot_exception | 外观快照未取得/准备异常 |
| `portrait.no_top_screen` | 没有可以挂舞台的屏幕 |
| `portrait.prefab_root_missing` / widget_missing | prefab 或控件创建失败 |
| `portrait.provider_warmup_timeout` | 控件的原生 provider 没有获得足够有效 UI 更新 |
| `portrait.tableau_unavailable` | provider 有更新但没解析到 TableauView |
| `portrait.export_request_failed` | 原生保存请求失败 |
| `portrait.export_file_missing` | 请求保存后没有等到 PNG 文件 |
| `portrait.export_file_unstable` | 已出现文件但长度没有稳定 |
| `portrait.png_decode_failed` | PNG 读取/解码或尺寸校验失败 |
| `portrait.screen_changed` / cancelled | 场景/界面变化或用户取消，不应记成服务异常 |

`portrait_capture_state` 保留实际 provider 更新次数、原生状态、保存标记与退休快照；它不能证明 GPU 完成。全身失败停止必需人物参考流程；头图可选，缺失时保留已取得的全身图。

百科之前无条件采集立绘，本轮使其遵守“启用原生离屏渲染”开关。主动关闭会记录 `portrait_capture_disabled`，属于玩家选择的无立绘路径；开启后失败不会自动绕开。

本机保留的近期 trace 中确有成功百科与对话立绘，但没有该玩家的失败 trace，不能以这些成功记录否定玩家反馈。部署后的准确失败码才用于下一次现场判断。

## 3. 自定义规则是否影响生图

- 正文 `RuleBehaviorPrompts`、动作后处理规则没有直接注入视觉导演请求。复用正文 API 地址/密钥/模型不等于复用正文 system prompt。
- 导演使用生图自己的 system prompt，加人物/环境硬事实、导演专属背景、艺术指导和画风配置。
- 正文规则会改变 NPC 实际对白；对话生图会参考最近对白，因此可能有**间接影响**。这不同于把正文整套规则直接发送给生图端。
- 自定义画风与自定义负面词只有选择自定义画风预设时生效；预设画风使用自己的配置。画风既进入导演，也进入实际图片请求。
- `prompt_sources` / `image_prompt_sources` 记录模式、各输入长度、规则来源和自定义选项是否启用。`director_request` 与 `image_request` 保留实际发出的脱敏请求；台词专属背景不直接附加成图片提示词字幕。

## 4. 后台一分钟，游戏三分钟：如何拆开

供应商的“处理一分钟”与点击到显示并不是同一个区间。必须匹配同一次请求与供应商时间戳，不能直接认定额外两分钟是游戏卡顿。

完整顺序：

```text
Scope接受 → worker_started → 场景/人物参考 → prompt_sources
→ director_http_begin → director_http_headers → director_response
→ image_route → image_request → image_http_begin → image_http_headers
→ image_response → image_response_extract_begin
→（若返回URL）image_download_begin/headers/complete
→ image_result → cache_save_begin/complete → worker_complete/pipeline_finish
→ main_thread_delivery → ui_texture_begin → ui_publish_complete
```

- 每个 trace 事件都有 `requestElapsedMs`；相邻时间差可以定位采集、导演、上传/网络等待、响应体读取、解码与缓存。
- `worker_started.elapsedMs` 是 Scope 接受到工作开始的等待，不包含它之前 UI 收集上下文的时间。
- `main_thread_delivery.totalMs` 是 Scope 接受到主线程回写开始；`dispatchWaitMs` 是后台收尾后等待主线程的时间，不是 HTTP 时间。
- `ui_publish_complete.publishMs` 是本次纹理准备/注册和绑定耗时。
- UI 发布完成意味着纹理注册与 datasource 已绑定，**不等于 GPU 已呈现某一帧**。如果仍看不到，需实机验证绘制、遮挡与帧提交。
- URL 下载、安全策略拒绝、缓存写入和 UI 纹理失败有独立错误步骤；HTTP200并不代表完整链路已经成功。

本机既有成功例子（不是报告玩家）：一条 edits 流程总计98.9秒，约28.5秒到图片请求、70.0秒请求到响应；另一条 chat 流程总计201.2秒，约77.0秒到图片请求、124.0秒请求到响应。后者导演单独约67.4秒。没有新增真实外网调用，也未凭这些记录推断供应商所说一分钟的真实区间。

## 5. 日志位置与取证

安装模块下：

```text
<游戏>/Modules/AnimusForge/Logs/image save/Diagnostics/<请求ID>/trace.json
<游戏>/Modules/AnimusForge/Logs/image save/Diagnostics/<请求ID>/steps.log
```

缓存根不可写时使用既有 LocalAppData 回退。AF 通常还在下面保存小型步骤摘要：

```text
%LOCALAPPDATA%/AnimusForge/Logs/Mod_Logic.txt
```

后台细节查看 trace/steps；关闭后台 scope 之后的 `main_thread_delivery`、UI发布与迟到丢弃摘要，在 `Mod_Logic.txt` 按同一 `id=` 检索。保持 AF 日志开关开启，重现后及时复制该ID的文件与对应摘要。

JSON 根包含 `outcome`、`lastStage`、`failedStage`、`failureCode` 与最近 HTTP 状态。首先看最具体失败码，而不是最后一条“未取得立绘”。错误不会被后续泛化信息覆盖。

存储仍有界：最近12个请求目录、96事件（保留最新错误/结算）、512KiB JSON、既有图片证据预算；文本步骤日志128KiB滚动，最多再保留一份 previous。主日志不包含完整prompt、参考图片或base64。密钥、Bearer、URL query/userinfo会脱敏；trace含实际对话/提示词与参考图，分享前仍须检查角色故事及其他个人内容。日志关闭/存储失败不阻断正常生成。

## 6. 离线回归范围

双API+Bootstrap以原脚本在仓内隔离源树构建；两实现各验证：端点174、诊断65、人物双视图37、不向屏幕绘制16、提示词194、导演状态45、颜色47、参考路由64、缓存/画廊72条断言。HTTP测试在内存拦截，不消耗真实生图额度。

审查还确认并修复未指定分类时把所有类别一起排序的缓存问题：新对话图不能覆盖百科默认图的类别优先级。按类别内已索引匹配项排序，不扫描全量世界或每帧解码图片。原优先级断言保留并通过。

原先离线脚本的可选反射参数与全程序集类型枚举问题已修复：补齐可选参数、只枚举目标owner及嵌套状态机，不绕过断言；周报接线核对更新到真实共享StartGeneration，而非旧UI薄入口。

**尚未验证：**该玩家失败/延迟现场、对应GPU、真实provider、旧档；并行对话UI真实交互与游戏组合表现仍未实机验收（其提交后完整树构建及18组生图审查已通过）。本次不改变构建/覆盖脚本，不部署、不推送、不重打包。
