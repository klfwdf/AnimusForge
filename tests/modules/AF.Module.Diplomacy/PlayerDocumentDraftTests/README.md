# 玩家外交文书代拟验证

`dotnet run --project tests/modules/AF.Module.Diplomacy/PlayerDocumentDraftTests -c Release`

直接链接正式VM、应用、代拟合同、输入清理、外交LLM client、协议、HTTP transport和SaveRuntimeGuard；只替代Gauntlet ViewModel基类、MCM设置/世界行为及日志接收器。用受控HttpMessageHandler实际异步等待和迟到响应，不访问真实provider或游戏，也不持有真实凭据。

覆盖生成前MCM范围/目标篇幅/扩写与文风优先级、随篇幅变化的真实HTTP输出预算（取消固定1800但保留玩家API配置上限）、范围外完整稿直接回填且无字数提示/修稿重试、坏JSON/重复字段/多字段/空稿/技术容量/截断、设置快照/路由回退、后台不读MCM不写UI、单请求/重写、编辑后恢复原文仍失效、关闭取消/旧窗口迟到、读档generation、失败不重试、provider协议降级及旧外交发送路径。自然语言条款忠实程度、实际生成篇幅和游戏内布局/焦点仍需实机及真实provider验收。
