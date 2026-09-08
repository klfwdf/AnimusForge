# AF 整个重构项目收尾执行台账

## 最新状态（2026-09-09）

**阶段八继续实施，整个项目 NOT_DONE。** 已恢复断线修改并形成实际修复提交 `9a4a26dc`，不是继续只写检测文档。最新接续入口：`docs/handoffs/2026-09-09-recovery-fixes-handoff.md`。

- 唯一工作区：`G:\AFMOD\AF-REFACTOR`；分支 `codex/af-main-refactor-continuation-20260831`。
- 修复前 checkpoint `5ce8767a`；原红基线 `35524b04`。原审查报告/坏状态证据未改写。
- 自动化保持 PAUSED；本轮未推送、未部署或操作真实存档，未切新的默认入口。
- 2026-09-06 两份用户占位草稿保留、不暂存；其他工作树与 GCCZ 主体不改。
- 本轮代码已验证、本地提交；推送、部署/真实存档操作、新默认切换和广泛删除按明确范围另行处理。已有用户“其他成员验收过”的反馈保留，但没有本修复候选的对应实机证据。

## 收尾清单

| 范围 | 本轮结果 | 仍需完成 |
|---|---|---|
| F1 Hero asset ALL | 指定资产、modifier、RP、市场 owner；67 场景与旧红/变异通过 | 真实资产/商人职业/旧档 |
| F2/F3 Courier | 完整匿名 Prompt、复用捕获、权威主线程 completion、资格/取消/同 ID 替换、可见文字与 raw 分离；39 场景 | 更早的前置准备线程边界 NOT_FIXED；失败 MOOD 兼容审查 |
| F4/F5 Scene/BattleSpeech | 请求冻结、框选回退、双 waiter；30 场景；分类成功路径 18 场景 | 实际分类/演讲、场景动作及 UI |
| F6 TTS | 请求级取消与合法 FIFO；43 场景；独立 Native 延迟兜底 14 场景 | 实际声音/口型/暂停/地图对话 |
| Gateway | 实际错误 envelope 不当成功，晚结果拒收；40 场景 | 底层同步 Action 网络可中断取消、真实 provider |
| Bridge/清单/旧测试 | mandatory safety 独立，16/12/4；混合默认明确；旧测试按真实当前协议/行为更新且保留反例 | 运行时/实机隔离 |
| 其余领域/Native/旧代码 | 没有广泛删旧或切 Native；公开 ABI/存档 owner 保留 | 按 20 领域逐项补齐，证明无职责后清理 |
| 统一离线验收 | 六构建 PASS；37 个完整 C# runner PASS；Python 26 检查 PASS＋1 预期缺证据 BLOCKED；Policy 四安全子集 PASS | 三个 SDK 10 工具未运行；所有真实验收 |
| 文档交付 | 同日验证报告、证据索引、HANDOFF、制作组文案 | 发布/最终打包/真实回滚演练尚未交付 |

## 接续顺序

1. 先读 `docs/audits/2026-09-09-courier-thread-boundary-plan.md`，完整设计并实现前置 prepare/network/complete，保留 aux/semantic fallback、记忆选择 mode、persona 和规则资格。不把含同步网络的整个 builder 搬到主线程。
2. 按 `docs/phase8/refactor-execution-plan-20260908.md` 的 20 领域清单接续 Native 与领域 owner；对照旧行为、取消和部分成功，不以删功能换清理数字。
3. 清理只针对有调用/反射/存档/外部 ABI 证明的已替代路径。共享 facade、旧事件或 Legacy 文件名本身不是删除依据。
4. 对最终同一候选补齐真实 Campaign/Mission、旧档、live Economy、AFEF、TTS、第三方与打包回滚证据；最终门槛满足前不标项目 DONE。

## 验证来源与协作

本轮报告：`docs/audits/2026-09-09-refactor-recovery-fixes.md`；机器可读索引：`docs/audits/2026-09-09-closeout-verification.json`。原始日志、阴性对照、源码及产物摘要在 `.tmp/closeout-20260909/`，不上传真实日志/凭据或忽略目录。

根代理统筹集成/公共台账/构建；经济、场景、TTS 分范围实现并交叉复核。一次只由一个 owner 写同一区域，只有根代理操作 Stage/提交。全部代理已停写，最终源码摘要与 `9a4a26dc` 规范化换行后的 Git blob 一致。
