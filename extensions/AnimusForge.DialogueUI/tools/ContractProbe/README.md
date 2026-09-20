# DialogueUI 实际程序集隔离探针

在独立 net472 进程读取指定的 AF 与 DialogueUI DLL。不得从游戏进程启动；不创建战役、场景、Agent、UIContext 或纹理，不部署，也不调用模型。

构建时传入 `GameRoot`、`AfReferencePath`、`DialogueUiPath` 三个 MSBuild 属性，分别指向已安装游戏根、实际 AF DLL、实际 DialogueUI DLL。探针引用均设为 `Private=false`；入口先注册 AssemblyResolve，再通过不内联方法运行测试，避免误载输出目录里的旧游戏程序集。

运行参数依次为：

```text
AnimusForge.DialogueUI.ContractProbe.exe <game-root> <installed-af.dll> <dialogue-ui.dll> <output-directory>
```

两份 API 产物各运行一个独立进程。输出目录必须位于游戏安装目录之外；任务默认使用本子模块 `artifacts`。运行前应重新构建生产 DLL，日志会记录两份 DLL 的 SHA256、MVID，以及实际加载的 TaleWorlds 程序集路径和版本。

覆盖内容：

- 逐个验证必需的 Harmony prefix/postfix/finalizer，核对总目标数和挂载数；缺少接缝直接 FAIL。
- 喊话包装层复用原 Host、草稿、多行发送、失效阻断、取消、历史按锁定 index/260 行上限首开读取一次、释放与资源刷新复用。
- 原版对话包装层的属性通知、派生状态、命令唯一转发、忙碌守卫、重复解绑后原 VM 可用。
- 使用实际 WidgetFactory 的目录登记方法验证 prefab 首次注册、二次命中、外部 owner 冲突；仅初始化托管字典，不创建 UI。
- 对实际 XML 按 Host 子数据源作用域验证所有绑定属性及命令方法，检查生产注册路径能拼出真实 XML。

日志隔离使用独立 Harmony owner：先把 AF 日志目录转向输出目录，再拦截 AF 的 void Log*、本模块 Log 及 Debug.Print*，不执行原日志后端。正式挂载计数不包括日志隔离或 fixture patches。

喊话有效上下文测试对 fixture 自有的空上下文临时替换 `IsCurrent`，并注入计数历史读取委托；这是对实际包装 DLL 的行为测试，不证明游戏里的 Mission/Agent 身份或界面焦点正确。若 1.3 DLL 使用当前已安装的 1.4 游戏依赖执行，结果只证明该依赖组合下的托管接缝，不能称为 1.3 游戏实测。真实布局、暂停/百科恢复、纹理 GPU 加载和实际会话仍需实机验收。
