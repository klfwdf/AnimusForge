# AnimusForge 宣权篡位

当前主线已将 Coup 编入 `AnimusForge.dll`，由 `IntegratedModuleHost` 启动，只需启用统一的 AnimusForge 模块。MCM 仍保留独立设置页。下方独立子模组构建说明仅供维护旧输出；旧模块检测到主体接管后会跳过注册。

## 使用

- MCM 的“AnimusForge - 宣权篡位”可开关新政变，默认开启；关闭后已有事件仍收尾。
- 玩家必须是本国正式封臣及家族族长，在本国国王实际停留的本国城镇发动“宣权篡位”。至少两名健康普通士兵，一人留守接应；先退出军团及其他战斗/处置流程。
- 选定突击队后，点击城镇菜单的“率领政变突击队攻入城镇”。SETS 带入这次实际选中的士兵，最多 60 人，并留一名接应。击败门口守军后到门口按 **F**，再选最多 20 名幸存者；返回城镇菜单点击“率领政变突击队攻入领主大厅”。两个专用入口按当前阶段显示，点击时重验国王、城镇与战斗状态。
- 大厅门使用原版交互键触发（默认 F），由政变接管实际 `PassageUsePoint.OnUse`：门卫未清除时提示阻止进入，清除后先结束街道并选兵，不会直接跳进普通大厅。政变内其他 location 门不能绕过转场流程；场景出口/退出战斗仍按撤退处理。普通场景门不受此限制。
- 制服国王并击败大厅护卫后，选择释放或扣押旧王。玩家取得原王国王位及本城，然后立即按 AF 原规则判定一次叛乱；尊重总开关、玩家王国免疫、家族关系及被俘/无地限制。
- 倒地、撤退或放弃大厅进攻会失败：实际兵损保留，家族带原有封地脱离旧王国并开战。场景技术故障停止自动政治提交；部分结算可在城镇菜单重试。
- 扣押只阻止同阵营自动放人。主动释放、赎回、越狱、死亡及转出玩家保管仍沿原版处理。

## 架构和兼容

本子模组持有政变目标、状态和存档。街道与大厅的进场、随行生成、编队、守军波次和指挥交给已安装 AF 的 SETS。政变只登记本次进城、判定国王与大厅门口，并在胜利后执行王位、归属和叛乱。它不改 SETS 的日常随行存档，也不走 SETS 的占领菜单。

这些接缝不是 AF 公共 API V1 的兼容承诺。SETS 不可用时关闭发动入口并记录原因。菜单结算由实时引擎 tick 驱动，避免城镇暂停时卡住。退出与读档后不保留 Agent 引用。政治执行仍在游戏主线程。

入口与叛乱调度的忙碌状态通过主体的 `HasBlockingFlowForCoup` / `HasBlockingRebellionFlowForCoup` 查询，启动时绑定并缓存 delegate，不依赖主体私有字段布局。菜单重算和已有引擎调度仅作 O(1) 状态判断；缺失接口或 SETS 查询异常仍关闭入口。统一主线使用仓库 `scripts/build/build_single_module.ps1` 构建两版和 Bootstrap，未授权时不添加 `-Deploy`。

场景挂载在 SETS 的 `OnMissionStarted` 完成后补通知，不依赖 Campaign 事件的注册顺序；重复通知仅附加一次政变 owner。兵员 Origin 的非公开构造器及伤亡回调在启动时验证，Origin 创建失败不允许以空 Origin 继续生成政变兵员。每场事件只作一次挂载检查，构造器仅首次解析；普通场景和原版伤害逻辑保持原路由。

`Guards registered` 启动日志里的 `captivity=False` 还可能表示战役拘押行为尚未构造；它不是单独的补丁失败证据。需结合 `Automatic detention protection unavailable` 错误与战役生命周期验证判断。

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
