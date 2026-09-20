# 当前交接：J08 整体复核后闭合，J09 计划已就绪（2026-09-21）

- **状态**：J07_OFFLINE_VERIFIED；J08_OFFLINE_VERIFIED；J09 `PLANNED / NOT_STARTED`。自动化 `af-7-8` 仍为 PAUSED。
- **复核纠正**：原 `eb4b0bee` 过早宣称 J08 完成；整体扫描发现 Policy 与 WorldDiplomacy 仍各自持有 POST/request/read/dispose。产品提交 `5a2df9d6` 已把两者接到唯一 `LlmNonStreamingTransport`，领域 Prompt、兼容降级、重试、结果解析和 hard timeout 保留原 owner。
- **J08 产品包**：`5dc17947` 非流；`4776b691` SSE/部分回复禁止重放；`1e1fdfad` ModelCatalog/TTS 归位；`5a2df9d6` Policy/World transport 收敛。
- **验证**：非流 240 + 5 变异、流式 17 + 5 变异；最终 Debug DLL 的 Policy/World loopback Gateway 回放通过取消、重试等待取消、hard timeout、credential 边界；Debug/Release × 1.3/1.4/Bootstrap 六构建 0 warning/0 error；实际四 DLL API/metadata 1060；Persistence/Profile 142/168/13/44；Phase8 inventory；322 锚点双模式通过。
- **边界**：TTS 是独立协议 transport，游戏 voice/playback 生命周期仍归 `TtsEngine`。真实 provider、Campaign/Mission、旧 SAVE、真实音频和帧/网络性能均 NOT-RUN；未 Stage/Deploy/Package。
- **J09 计划**：`docs/plans/j09-actions-facts-plan.md`。顺序为 G0 矩阵 → Tags → Plan/typed ports → Receipts → 三渠道接线 → 清理/整包验收；J10/J11/J12/J13/J14 不偷渡。
- **位置**：唯一施工树 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`；本地分支 `codex/af-modularize-j04-20260918`；交付目标 `origin/codex/af-main-refactor-continuation-20260831`。未推送，`.dotnet-cli-home/` 保留。
