# AnimusForge 宣权篡位（独立子模组）

依赖已安装的 AnimusForge、Harmony 与 MCM。启动器勾选 **AnimusForge - 宣权篡位**，排在 AnimusForge 后面。模块 ID 为 `AnimusForge_Coup`，只加载自己的 `AnimusForge.Coup.dll`，不替换 AF 或原版 DLL。

## 使用

- MCM 的“AnimusForge - 宣权篡位”可开关新政变，默认开启；关闭后已有事件仍收尾。
- 玩家必须是本国正式封臣及家族族长，在本国国王实际停留的本国城镇发动“宣权篡位”。至少两名健康普通士兵，一人留守接应；先退出军团及其他战斗/处置流程。
- 选定突击队后，从城镇菜单进入城镇中心。SETS 带入这次实际选中的士兵，最多 60 人，并留一名接应。击败门口守军后到门口按 **F**，再选最多 20 名幸存者进入领主大厅。
- 制服国王并击败大厅护卫后，选择释放或扣押旧王。玩家取得原王国王位及本城，然后立即按 AF 原规则判定一次叛乱；尊重总开关、玩家王国免疫、家族关系及被俘/无地限制。
- 倒地、撤退或放弃大厅进攻会失败：实际兵损保留，家族带原有封地脱离旧王国并开战。场景技术故障停止自动政治提交；部分结算可在城镇菜单重试。
- 扣押只阻止同阵营自动放人。主动释放、赎回、越狱、死亡及转出玩家保管仍沿原版处理。

## 架构和兼容

本子模组持有政变目标、状态和存档。街道与大厅的进场、随行生成、编队、守军波次和指挥交给已安装 AF 的 SETS。政变只登记本次进城、判定国王与大厅门口，并在胜利后执行王位、归属和叛乱。它不改 SETS 的日常随行存档，也不走 SETS 的占领菜单。

这些接缝不是 AF 公共 API V1 的兼容承诺。SETS 不可用时关闭发动入口并记录原因。菜单结算由实时引擎 tick 驱动，避免城镇暂停时卡住。退出与读档后不保留 Agent 引用。政治执行仍在游戏主线程。

两版从同一源码编译，分别位于 `artifacts/1.3` 与 `artifacts/1.4`。部署只放与当前游戏匹配的一个实现；不把两份实现同时声明到启动器。当前双构建脚本需要 1.4 游戏安装，1.3 编译使用固定的完整 `Bannerlord.ReferenceAssemblies 1.3.15.110062` 引用包。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\extensions\AnimusForge.Coup\build.ps1 -BannerlordRoot '<游戏目录>'
# 用户授权覆盖后，显式添加 -Deploy；脚本只写 Modules/AnimusForge_Coup。
dotnet run --project .\extensions\AnimusForge.Coup\tests\Coup.ContractTests\Coup.ContractTests.csproj -c Release
```

构建不重编 AF；部署前备份同名模块，逐文件核对哈希，并检查原 AF 两个实现的哈希未变。`artifacts/deployment.json` 记录本机部署路径、版本及备份。

## 验证边界与回滚

离线状态/伤亡契约、两版编译与 DLL 成员核查不能代替实际场景验收。各文化城镇门口/大厅导航、真实指挥 UI、60 人街战性能、原生 Harmony 实际挂载及旧存档读写需要在游戏中验证。日志位于 AF 的 `Logs/Mod_Logic.txt`，检索 `[Coup]`。

尚未开始政变时可在启动器取消勾选本模块。进行中的事件应先收尾，再停用；已经发生的兵损、王权和领地变化属于存档内容，卸载 DLL 不会撤销。部署备份可恢复旧模块文件；战役结果回滚使用发动前的存档。
