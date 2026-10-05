# 玩家外交文书代拟验证

`dotnet run --project tests/modules/AF.Module.Diplomacy/PlayerDocumentDraftTests -c Release`

直接链接正式VM、应用、代拟合同、输入清理、外交LLM client、协议、HTTP transport和SaveRuntimeGuard；只替代Gauntlet ViewModel基类、MCM设置/世界行为及日志接收器。用受控HttpMessageHandler实际异步等待和迟到响应，不访问真实provider或游戏，也不持有真实凭据。

覆盖当前输入/偏好/长度合同、坏JSON/重复字段/多字段/长度/截断、设置快照/路由回退、后台不读MCM不写UI、单请求/重写、编辑后恢复原文仍失效、关闭取消/旧窗口迟到、读档generation、失败不重试、provider协议降级及旧外交发送路径。自然语言条款忠实程度和游戏内布局/焦点仍需实机及真实provider验收。
