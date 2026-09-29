# Native 未压缩历史验证（2026-09-12）

状态：`OFFLINE_COMPLETE / LIVE_SAVE_PENDING`，仅针对这一处调用点。源码基线 `53ddb7d4`，编辑前 intent `75a692ac`。当前本地生产提交在根 HANDOFF 中记录。

改动：`SubmitNativeConversationTextInternalAsync` 在原主线程调度中检查 admission 后，调用原 `BuildUncompressedMemoryRoleMessagesForPrompt`；原 helper 生成独立消息对象，保留 Hero/普通人物 memory ID、AFEF、当前会话排除与主动开场去重。排队过期或失效返回 null，沿既有主线程历史回滚接口撤回本次暂存输入。没有新的每帧扫描、网络、持久字段或默认路由。

验证结果：

- 专用 69 PASS；真实旧源码运行同套 fixture 失败 13 项；三个行为变异失败 4/4/3 项。
- 新调用点定向反转后与旧完整 Shout 源码一致，其他 helper/玩法未变。
- NativePendingHistory 111、NativeHistorySnapshot 27、NativePreparation 589、MainThreadFunction 132、NativeAdmission 44、制作组端口 308 及其三变异 PASS。
- Debug/Release × Bannerlord 1.3/1.4/Bootstrap 六构建和两个项目内 Stage PASS。
- 存档身份保持 146/146 SyncData 与 36/36 CampaignBehavior；模块 AnimusForge 只加载 Bootstrap。
- 实际四实现 DLL 532 元数据断言 PASS，外部 internal 访问按预期拒绝。

DLL SHA256：Debug 1.3 `8042976a93c62cfc6b6e5e5f8a897c5495ab69aa6dcfeb221bf949ff17c08cd1`；Debug 1.4 `229fc93e1f80fd1c5f3b4d42e1f53162dd9e255e4c734d3baee2d2479de3fe16`；Release 1.3 `cc565d9a63f54d25c18ba3d7d37900d3b4be3384cbcebc9d2a61d4b57a6d753f`；Release 1.4 `f606dbeee1521823da1331537a5b1444ef003c48176fe1b1a03965af78444e84`。

原始日志在 `.tmp/goal-20260912/native-uncompressed-*`。四个旧测试 runner 的固定 G 盘 SDK 改为可由 `DOTNET_EXE` 选择的现有 SDK，并使用已有离线配置；不改变一键脚本或系统环境。NativeAdmission 的门禁计数新增本次调用点，旧全文件证明通过具名反转接纳本次差异，没有整体刷新历史 hash。

尚未验证：LIVE/真实 provider/旧存档/实机耗时。此片没有解决 Native 的角色 prompt、persona、共享规则/lore、后处理 prepare/complete 和其他 TTS 实时读取，不代表 Native 或阶段八已完成。
