# Phase 3 GameAdapter API Fixtures

纯设计 fixture，不接入生产 `.csproj`，不加载 Bannerlord，不调用反射/网络/存档。

覆盖 1.3/1.4 helper、RestartPlayerEncounter 参数差异、SpawnTroop、BattleRewards、missing member、Bootstrap metadata mismatch、unknown version、reflection cache、main-thread apply、SubModule.xml 单 Bootstrap 和双实现选择边界。