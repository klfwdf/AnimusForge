# AF 重构恢复与缺陷修复 HANDOFF（2026-09-09）

## 最准确的结论

**断线修改已恢复，本轮实际缺陷修复已提交，现交付至独立 GitHub 重构分支；整个重构项目尚未收尾。** 当前仍是阶段八实施中的修复候选，不是可宣称“功能全部复现、旧代码全删、没有 BUG”的发布版本。

- 工作区：`G:\AFMOD\AF-REFACTOR`。
- 分支：`codex/af-main-refactor-continuation-20260831`。
- **代码提交：`9a4a26dc1716329d699833d5da0b3679ff3fd192`**。
- **GitHub 交付分支**：`origin/codex/af-main-refactor-continuation-20260831`；[打开重构代码](https://github.com/klfwdf/AnimusForge/tree/codex/af-main-refactor-continuation-20260831)。
- 原共享分支 `origin/refactor/prepare-af-restructure` 保持不动；不推 `main`。
- 修复前回滚 checkpoint：`5ce8767a`；历史缺陷对照 `35524b04`，不能继续引用 `a096c1b1` 作为当前最新版。
- 用户于本轮明确授权 GitHub 推送；本交接与制作组文案随独立重构分支交付。本地保留同名文件；自动化继续暂停，未部署游戏、操作真实存档或改变 Native 默认路径。
- 两份 2026-09-06 用户草稿仍有原工作树改动，未纳入提交；**不能据此硬重置工作区**。

## 为什么使用独立重构分支

推送前 fetch 发现共享分支从 `aefa02ad` 被改写到 `03eb33f1`，与本地不是快进关系。远端最新两文件改动与本地已有 `aefa02ad` 的 patch-id 相同，但共享历史删去了其他祖先提交。为避免擅自恢复他人移除的历史，本次不强推、不合并回共享分支，直接发布本地已验证候选至同名 `codex/` 重构分支。后续如需合回共享分支，应由制作组先确认历史取舍；不能直接执行不加审查的 pull/reset。

**状态优先级：**旧 09-07/09-08 交接中关于“阶段八完成、三渠道全切换、实机全通过”的陈述只作历史记录；当前以本 09-09 HANDOFF 和验证报告的未完成门槛为准。验证索引里的 `gitPush=false` 是验证时点快照，不代表本次后续交付状态。

## 已完成

- 修复原审查 F1–F6：指定资产 ALL、Courier 消息丢失/后处理权威性、Scene 旧输入/框选回退、TTS 请求取消隔离。
- 补修交叉审查发现的 Gateway 错误文本当成功、Courier 可见协议泄漏、BattleSpeech 成功分类晚启动，以及 Native TTS 延迟兜底旧请求放行新等待。
- 保持合法同 Agent TTS FIFO、原正文 raw 作为后处理证据、领域归一化后才解析动作、取消不伪造已开始副作用的回滚。
- 清理无效 Bridge 配置/accessor、替代的内部 TTS 路径和缩减 Courier 后处理；公开兼容 ABI/旧事件、存档 owner 和活跃旧默认继续保留。
- 六项官方构建通过；37 个完整 C# runner 通过；Python 26 检查通过，另 1 个缺证据样例按预期阻塞。Policy 仅四个隔离安全模式通过，共 1056 断言。
- 新的七组正式回归和旧红/变异证据已保存。详细场景数与命令见各测试 README，不能把隔离 fixture 当真实 Campaign/Mission。

## 没有完成

1. **Courier 更早的前置准备线程边界仍未修复**。aux、人设落盘、历史/记忆选择需完整拆分；不能只移动整个 builder 或靠缓存掩盖重复网络。
2. Native 完整默认等价接入、其余领域 owner 与最终旧代码清理仍需继续。当前为混合默认，不是“三渠道全部切完”。
3. 同步 Action 网络无法在当前接口内中止；已拒收晚结果，但不等于底层取消完成。后处理失败的旧 MOOD fallback 兼容差异仍应复核。
4. 三个 net10.0 工具实际 NETSDK1045；本轮没有全局安装 SDK。Policy 默认全套/实际 ONNX 未运行。
5. 本候选真实游戏、旧档、实际金币物品债务、AFEF、声音口型、第三方及安装回滚仍缺绑定证据。此前其他成员通过的反馈不被否定，但不能自动覆盖本轮新增修复。

## 接手先读与先做

1. `G:\AFMOD\AF-REFACTOR\AGENTS.md`。
2. `G:\AFMOD\AF-REFACTOR\docs\phase8\project-closeout-execution-20260908.md`。
3. `G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-09-refactor-recovery-fixes.md`。
4. `G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-09-courier-thread-boundary-plan.md`：下一实施重点与完整四链路，不要再误报 Lore 主查询走在线网络。
5. `G:\AFMOD\AF-REFACTOR\docs\phase8\refactor-execution-plan-20260908.md`：20 领域功能对照清单。

先检查 status、分支、实际远端 ahead/behind，再按线程边界计划推进。不要新建自动化、重复整个已经通过的窄修或删除旧红证据；不要覆盖用户草稿、其他工作树、游戏或存档。

## 构建与证据

工具链使用 `G:\AFMOD\.dotnet-sdk`，CLI home 使用既有 `G:\AFMOD\AF-REFACTOR\.tmp\dotnet-cli`，NuGet cache 使用工作区 `.tmp\nuget-packages`。设 `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`，不要再用新 CLI home 触发 SDK 初始化。

```powershell
$env:DOTNET_ROOT = 'G:\AFMOD\.dotnet-sdk'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
$env:DOTNET_CLI_HOME = 'G:\AFMOD\AF-REFACTOR\.tmp\dotnet-cli'
$env:NUGET_PACKAGES = 'G:\AFMOD\AF-REFACTOR\.tmp\nuget-packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
powershell -NoProfile -ExecutionPolicy Bypass -File '.\一键编译覆盖推送\build_single_module.ps1' -ProjectRoot 'G:\AFMOD\AF-REFACTOR' -BannerlordRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' -Bannerlord13ReferenceDir 'G:\AFMOD\NEW-10\_deps_auto' -Bannerlord14ReferenceDir 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' -WorkshopContentDir 'E:\steam\steamapps\workshop\content\261550' -RuntimeDependencyDir 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' -Configuration Debug -Stage
```

再以 Release 重复。只使用 `-Stage`，不改项目的一键流程；引用版本已验证为 1.3.15 与 1.4.6。完整 SHA/测试索引：`docs/audits/2026-09-09-closeout-verification.json`。

原始日志和构建 DLL 属本地忽略产物，在 `.tmp/closeout-20260909/`、`bin/Debug/single_module_stage/` 与 `bin/Release/single_module_stage/`；**不随本次源代码与文档交付上传**。Policy 为无 ONNX 隔离投影，报告保留 SDK 默认开发证书提示及未检查其用户级效果的限制。

回滚优先针对 `9a4a26dc` 做审查后的 inverse/revert 提交；不 hard-reset、force-push 或删参考资料。真实部署另行确认，并先备份 DLL/PDB/ModuleData、保留 ONNX。
