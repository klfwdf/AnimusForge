# 王国稳定度百科显示本地化切片

阶段：主线已合入资源并配套修复百科刷新接线；静态验证完成，编译与实机 NOT-RUN。

- 基线：`da490e177`，分支 `main`。
- 修改：`src/AF.GameAdapter.Bannerlord/Kingdom/KingdomStabilityGameAdapter.cs` 的 `BuildKingdomStabilityEncyclopediaText`，新增私有显示评级/TextObject 辅助方法和 `TaleWorlds.Localization` 引用。
- 键表：`2026-10-10-kingdom-stability-encyclopedia-ui-keys.json`，7 个新增键，9 个既有键复用；主线已合入共享 XML 中英资源对。
- 保留：`GetKingdomStabilityTierText` 的原中文返回值，以及概率、关系修正、免疫、家族数量、忠诚度计算。其周报/运行结果消费者不能被显示英化改变。
- 运行频率：沿现有百科刷新调用进行文本解析；未新增 Tick、扫描、反射或缓存游戏对象。

验证：`python tests/content/J15ContentContractTests/KingdomStabilityEncyclopediaLocalizationContractTests.py` PASS（键表 fallback/变量一致，已注册复用键一致，生产方法与 raw tiers 保留）；定向 `git diff --check` PASS。

刷新接线：`EncyclopediaKingdomStabilityPatch.cs` 使用 ConditionalWeakTable 按页面缓存前次追加块，精确移除匹配的末尾块，覆盖语言切换和仅有外交 standing 的情况；另外兼容完整旧中文标题行、英文标题行及当前本地化标题行，避免把百科正文中的同名词组当成块起点。关闭显示时清理缓存块。只在既有 RefreshValues 入口运行，无新增 Tick 或反射。静态契约检查接线；尚未实际执行 C# 或游戏刷新。

主线重跑：26 对注册资源契约 PASS；af_ui_strings 经已有 1.3 参考 DLL 进行 12556 次真实解析 PASS（英文、简体中文、French 回退、切回英文），这不是本轮产品编译。

后续：`KingdomStabilityLabController.cs` 的 4 个评级显示入口仍调用 raw port，须在 UI owner 投影，不能改共享生产返回值。

`KingdomRebellionEditorController.cs:259` worker 异常前缀方案：保持 worker raw namingResult/ex.Message，主线程 UI 显示时用独立文本键投影错误前缀；禁止在 Task.Run 中解析 TextObject，禁止改日志/命名结果的原始诊断。本切片未修改该文件。

NOT-RUN：1.3/1.4/Bootstrap 编译、C# 执行、实机百科刷新/语言切换、旧存档；未部署、未 Push。回滚须对本切片使用 focused inverse，不 reset 共享工作树。
