# AF 重构分支融合 HANDOFF（2026-09-06）

## 交付位置与状态

- GitHub：`klfwdf/AnimusForge`。
- 目标分支：`refactor/prepare-af-restructure`；不向 main 推送。
- 融合代码提交：`fb01c03c4dea6bba91f21c13ed7d79fe09ee357c`。
- 合并父节点：本地 `38c72484`、远端 `8f1fa8db`；本地完整修复基线为 `fea254c3`。
- 后续文档提交只补本文、制作组简报和发布记录，不改变通过验证的生产代码。
- 本交付是重构开发分支的融合，不是“阶段八全部 DONE”或零 BUG 声明。不部署游戏、不操作真实存档、不恢复自动化。

## 融合范围

远端新增的 `36e65cbc` API 引导、`33de401e` 终端设置/资源与保存、`8f1fa8db` 预览忽略规则全部进入合并历史。没有 rebase、hard reset、force push，也没有选一侧整文件覆盖另一侧。

三个冲突文件为 `AnimusForgeTerminalBehavior.cs`、`AnimusForgeTerminalUiModels.cs`、`AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml`，按功能合并：

- 保留远端分类设置、布尔/数值/下拉/文本/按钮/快捷键编辑、分组折叠、设置保存及新增图片资源。
- 保留本地全量查询、50项分页、按身份选择、标签完整详情/来源与快照导出、臣属/周报返回、长周报正文和生命周期保护。
- 搜索统一使用内嵌输入框；子查询只搜索自己的完整快照，首页/分类同时搜索工具与设置。取消重复搜索弹窗实现，匹配设置时展开分组，不截断搜索范围。
- TickActive统一负责周报完成观察、按键监听及ESC退出；切页/筛选会取消不可见监听。ESC取消重绑后释放键不再顺带关闭终端。
- 保留关闭时自动保存，并提供显式“保存设置”按钮；下拉模型初始化或同步到相同索引不再标记修改，避免仅浏览就触发保存。保存失败给明确提示。
- 保留统一战争备用入口、原生子窗交接前释放终端焦点/暂停，以及前序战争和平归档、RAG独立Bridge、周报主线程/读档保护和29个死方法清理。

远端API引导类/VM/XML、设置注册表、DuelSettings.TerminalSave、HotkeyInputGuard、ModOnboardingBehavior、SubModule与新增图片已逐文件核对，与远端对应内容一致（只允许换行差异）。不重写其API预设、扫描或保存规则。

## 融合后重新验证

- Debug/Release × 1.3/1.4/Bootstrap：六项官方构建全部0 warning /0 error，project-local Stage成功。
- 引用版本：1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`；不是1.4.8游戏实测证明。
- PhaseEightParityReplay：设置定义唯一性、AI分类跨页完整性、全局搜索、重绑取消/导航清理、仅浏览不标脏、保存入口、API引导类型/XML，以及原有查询/标签/周报/战争回放全部PASS。
- WeeklyReportOwnerReplay：线程排队、每tick限额、stale、reset完成等待、异常和owner替换PASS。
- ProductionDuelOutcomeReplay：Debug与Release各35项PASS，均检查两个API实现。
- ConfiguredChatValidation、KnowledgeRag（含4种Bridge开关组合）、ProductionDetachedHost、ProductionCourierHost：PASS。
- Persistence/Profile/Config：142 keys /168 typed bindings /43 symbolic sources PASS；没有新增/改名游戏存档键和类型。
- Bridge binding 16/10/6 PASS；入口清单10测试PASS；LiveHostReadiness 8测试PASS；XML解析与冲突标记检查PASS。
- 新增API引导/设置保存相关真实入口已纳入UI候选目录，未自动提升owner或LIVE/SAVE验收状态。

日志位于本机忽略目录 `.tmp/merge-20260906/`：`build-Debug-final.log`、`build-Release-final.log`、`parity-replay.log`、`duel-Debug.log`、`duel-Release.log`及同名Gateway/Host回放日志。最初一次合并构建发现折叠标题构造器缺失，已修正后完整重建；不要引用失败的 `build-Debug.log` 当最终结果。

回放没有保存真实MCM配置、调用真实付费API或运行游戏UI。实际键盘/焦点、图片渲染、设置持久化及引导联网仍需游戏内观察；不把入口/模型验证写成实机PASS。

## 后续接手的真实边界

1. 三渠道新的detached Submit入口还没有完整接管默认流程。Scene的规则/资格/库存债务/归一化、Native流式与主动开场、多人relay不能靠简单切换代替。
2. 仍在用的Legacy facade、Gateway、ActionPlan owner、存档/MCM迁移与双API兼容不得按名称删除。
3. 制作组已有实测基线 `c01a2fcc` /1.4.8及用户确认继续保留，但不自动覆盖此后新增DLL。
4. 20个owner保留ASSIGNED；此前world/social/UI新增入口待复核，候选清单PASS不等于全功能或发布验收通过。
5. 按代码提交+游戏版本记录问题，附具体步骤、预期/实际与日志；优先定向逆补丁回滚，不重写共享分支历史。

## 本地草稿保留

工作区中原 `2026-09-06-integrated-phase8-handoff.md` 和 `2026-09-06-team-brief.md` 有本地未提交占位改动；本轮未修改、未暂存它们。它们在Git中的完整历史版本仍保留。请使用本文及新 `2026-09-06-merged-refactor-team-brief.md` 作为本次交接，不把本地占位草稿误上传。

## 融合后的 Stage 哈希

- `Debug/AnimusForge.Bootstrap.dll` SHA256 `C65CDE94FD1687F550E6C66FA19CAA03E7439732A7F3D99336C8A6D316DCC516`
- `Debug/versions/1.3/AnimusForge.dll` SHA256 `5AA6C4F340F98E265480651350530EF26BCFD82F4F216AB55C4839AFAC070806`
- `Debug/versions/1.4/AnimusForge.dll` SHA256 `5CFDA2E3DB545C2E28D6ACF5D33CE5434E56259A8264BEBA07F4E975E5B62BEB`
- `Release/AnimusForge.Bootstrap.dll` SHA256 `5643266ED25668E80F77B3E11587C3758BA9B315C356CDE55144FC1D1C916687`
- `Release/versions/1.3/AnimusForge.dll` SHA256 `477F3A591848EB94D3891A0D08E4F547A36AEBDF0E08745C9FE9D19D2FF65F63`
- `Release/versions/1.4/AnimusForge.dll` SHA256 `4565CDBBE6EB31C638E9808935A35428D1E1A0B8584621A4E9F18346E0D828AF`
