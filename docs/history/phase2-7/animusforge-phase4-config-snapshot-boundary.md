# RuntimeConfigSnapshot 边界

`RuntimeConfigSnapshot` 是阶段 4 的第一版不可变配置 contract，位于现有 `AnimusForge.dll` 的 `AnimusForge.Refactor.Contracts` 命名空间。

## 规则

- 快照包含 profile、配置 generation、模块启用状态和不含凭据的 `LlmProviderSnapshot` 元数据。
- 构造时复制字典并只读暴露；调用方不能通过 reload 改变已启动请求。
- `DuelSettings`/MCM 仍是当前运行时配置 authority；`RuntimeConfigSnapshotStore`
  保存当前已发布实例，旧入口继续直接使用原配置。
- `LegacyInteractionSnapshotAdapters` 的 detached capture 只做一次原子引用读取；
  `AIConfigHandler.ReloadConfig()` 收尾后请求 store 构建并发布完整替换，不修改已经
  捕获的请求实例。
- `apiKey`、Bearer token、密码、私有完整路径和完整 prompt/response 不进入该 contract。
- 持久化/Harmony/CampaignBehavior 模块的开关变更仍需要 save-load boundary 或重启，不能由此 DTO 宣称支持热卸载。

## 验收

纯测试已证明：旧快照仍使用旧配置、新快照使用 reload 后配置；reload 失败时保留
last-known-good；字典暴露不可写；快照不携带凭据。InteractionPipeline runner 为
`39 cases PASS`，双版本与 Bootstrap stage 均通过。

仍未验证：真实 MCM reload 时序、真实 provider 网络、旧存档和游戏内端到端行为。
