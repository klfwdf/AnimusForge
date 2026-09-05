# Live Host Readiness Audit

`live_host_readiness_audit.py` 是只读状态审计，不启动 Bannerlord、不部署模块、不修改游戏目录、不读取存档内容。

检查内容：

- Bannerlord 安装目录和可执行文件；
- 指定 project-local 根目录下的 Debug Bootstrap、1.3、1.4 stage；
- 已安装模块是否存在、`SubModule.xml` 是否唯一声明 `AnimusForge` 模块和正确 Bootstrap 入口类；
- Bannerlord 当前是否运行；
- 标准存档目录是否存在（只报告目录数量，不读取存档内容）。

示例：

```powershell
python .\tools\LiveHostReadinessAudit\live_host_readiness_audit.py `
  --game-root 'E:\steam\steamapps\common\Mount & Blade II Bannerlord'
```

`installedMatchesStage=true` 表示安装模块的 Bootstrap、1.3、1.4 三份 DLL 均与当前 project-local Debug stage 哈希一致，且 XML 的唯一 `Id` 为 `AnimusForge`，唯一 `SubModules/SubModule` 的唯一 `DLLName` 为 `AnimusForge.Bootstrap.dll`、唯一 `SubModuleClassType` 为 `AnimusForge.Bootstrap.BootstrapSubModule`。缺失、过期、不可读或错误/重复入口均返回 FAIL。PASS 才可以进入 live 测试准备；false 表示尚未部署或版本不一致。工具本身永远不执行部署和启动，也不验证全部 XML/资源内容。

`--game-root` 必须显式提供，避免审计误读另一台机器的游戏目录；`--project-root` 默认使用仓库根目录，也可以显式指定 fixture 或其他项目根目录。审计通过只代表离线环境检查通过，不代表真实 Campaign/Mission、LIVE/SAVE 或发布许可。

回归命令：`python -B -m unittest discover -s tools/LiveHostReadinessAudit -p 'test_*.py' -v`。测试使用临时 fixture 与隔离 profile，覆盖三份 DLL 缺失/过期，以及模块 ID、入口类、子模块和 DLL 声明的缺失、错误与重复；有效 fixture 的入口身份与仓库 `AnimusForge/SubModule.xml` 对照。
