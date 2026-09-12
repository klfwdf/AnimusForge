# AF 本地目标模式续作（2026-09-12）

## 当前授权与基线

- 用户在全项目分析后明确要求“开目标模式做吧”，授权恢复本地主体重构和验证。
- 实际工作区：`F:\AnimusForge-main`；当前分支：`codex/af-main-refactor-continuation-20260831`。
- 起点：`bd2ed35f`；最后生产代码：`9040d184`。历史交接中的 C/G 盘 worktree、旧暂停指令和一次性 push 授权不是本轮操作目标。
- 原有 75 个未跟踪文件（74 张信使图片及 `preview_courier_scroll.html`）保留，不纳入本轮提交。
- 本轮只在当前工作区实现、验证和提交；不 push、不部署/覆盖游戏、不操作真实存档、不恢复定时自动化、不开放新公共能力或切换默认入口。

## 执行顺序

1. 使用既有 `build_single_module.ps1 -Stage` 重建当前源码的 Debug/Release × 1.3/1.4/Bootstrap 基线。此前本机 Debug/Release 产物分别来自 9 月 8 日/5 日，不能沿用其结果。
2. 为三个压缩记忆任务捕获主线程只读输入与精确来源身份；后台仅处理网络/解析；结果接受时拒绝变化来源。保留重试、RPM、错误提示、AFEF、存档键和每 EngineTick 最多两个完成动作。
3. 补齐 Native 剩余实时输入读取；复用现有 admission/owner/generation 和历史快照，不缩减原历史语义。
4. 收敛 TTS 工作线程的游戏对象查询与回调边界；保留 FIFO、请求身份、取消和原声音/口型行为。
5. 拆分 Courier 双向前置准备：主线程捕获、后台网络与计算、主线程接受；保留规则资格、人设、记忆选择、送达和动作事实语义。
6. 每片运行有实际故障反例的相关回归、双版本 Stage、存档身份/实际 DLL 检查，更新代码范围和 HANDOFF 后独立提交。

## 性能与验证边界

- 输入快照按实际请求/低频压缩任务捕获，不在每帧扫描全世界或重复构造所有人物输入；后台不得共享可变游戏对象。
- 记忆维护预算前检查的 Hero 查询与草稿扫描已记录为后续性能观察点；不以放宽资格或丢弃事实换性能。
- 保留制作组业务 owner，主体只改实际受影响接缝。
- 合同、提取源码回归、DLL 元数据、构建与真实游戏分别记录。LIVE、真实 provider、旧存档与读档晚返回仍待独立实机验收。
- 回滚使用具名切片的定向 inverse/revert，不 reset、不改写历史、不处理其他作者文件。

## 进度

- `PASS`：当前源码基线 Debug/Release × 1.3/1.4/Bootstrap 六构建和两套 Stage；基线 history 852、main-thread 17、实际四 DLL 532 PASS。日志 `.tmp/goal-20260912/baseline-*`。
- `OFFLINE_COMPLETE / LIVE_SAVE_PENDING`：压缩输入切片 `53ddb7d4`。net8/net472 各 137 项、六变异、相关回归、六构建/两 Stage、四 DLL 元数据和存档身份通过；详见 [验证记录](../audits/2026-09-12-memory-summary-input-verification.md)。
- `IN_PROGRESS`：Native 剩余实时读取调查。
- Native 下一原子切片：将 `PrepareNativeConversationPendingHistoryAsync` 后的未压缩历史读取纳入原 admission 主线程调度；保留主动开场/已有会话选择、AFEF、普通人物 memory ID 和去重语义。角色 prompt、persona、共享规则/lore 与后处理 prepare/complete 仍有实时读取，作为后续依赖继续处理，不能仅修一处就标 Native 完成。此记录为编辑前 intent。
- `NOT_STARTED`：TTS、Courier 后续切片。
- SDK：测试使用项目内 dotnet8.cmd 调用已安装 8.0.421；没有更改系统 SDK 或根 global.json。构建仍使用原脚本。
- 可回滚生产提交：`53ddb7d4`；执行定向 revert 后需按影响面复验，不改写历史。

