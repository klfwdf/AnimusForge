# 城堡处置选择器与处刑状态显示

阶段：修改意图；基线 main `b196ecae4`。主线 A 的已确认玩家可见残余，独立于外部 C/E。

真实消费者：CastleAftermathRuntimeBridge 的 PartyScreen 初始化直接读取原中文 AvailableRosterTitle、SelectedRosterTitle、ScreenHeader；CastleAftermathLordExecutionRuntimeBridge 的退出阻止、死亡登记等待和取消/失败通知仍读取 SiegeCastleActionOutcomeTextProfile 原中文。既有 CastleAftermathUiText 已覆盖执行成功、打开失败等通知，但未接这些分支。

修改范围：两个 RuntimeBridge 的显示表达式、CastleAftermathUiText 的四个处刑状态方法、现有 af_ui_strings 中英资源七对新键，复用 ExecutionFailed。原 Profile、处刑/取消/死亡状态转换、PartyScreen 名册/上限/委托、日志/事实/存档不变。标题在打开时解析，状态在退出尝试或通知边界解析；不新增 Tick 扫描、后台本地化或语言探测。

验证：26 对资源合同、英文 fallback/中文原句/变量一致、精确反向替换后两 Bridge 与基线相同、既有城堡动态本地化契约、既有两版本 Localization DLL 的 af_ui_strings 解析。当前源码编译、游戏界面、旧档和正式包 NOT-RUN；不构建、Stage、部署或 push。回滚采用本切片 focused inverse，不覆盖 Policy/Economy/世界书等作者工作。


## 实际结果

阶段：静态验证完成。意图提交 `d650b5e8c`；产品提交 `b8ad0a54a`（6文件、+74/−9，7对新键）。两个Bridge只替换所列显示表达式，精确反向替换后与意图基线完全相同；原Profile、状态转换、逻辑、日志/事实/存档保持。

城堡专项PASS：75个helper键/52个后续键、21个既有通知消费者；名册3个中文标题与原Profile一致。26对注册资源契约PASS；既有1.3/1.4 Localization DLL各12792次af_ui_strings解析PASS，diff-check PASS。当前C#编译、实际PartyScreen/处刑UI、游戏与旧档NOT-RUN；未构建、Stage、部署、正式打包或Push。回滚使用 `b8ad0a54a` focused inverse。
