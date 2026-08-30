# Live Host Readiness Audit

`live_host_readiness_audit.py` 是只读状态审计，不启动 Bannerlord、不部署模块、不修改游戏目录、不读取存档内容。

检查内容：

- Bannerlord 安装目录和可执行文件；
- `F:\AF测试重构` 的 project-local Bootstrap、1.3、1.4 stage；
- 已安装模块是否存在、`SubModule.xml` 是否加载 Bootstrap；
- Bannerlord 当前是否运行；
- 标准存档目录是否存在（只报告目录数量，不读取存档内容）。

示例：

```powershell
python .\tools\LiveHostReadinessAudit\live_host_readiness_audit.py
```

`installedMatchesStage=true` 表示安装模块与当前 project-local stage 的 Bootstrap 内容一致，可以进入 live 测试准备；false 表示尚未部署或版本不一致。工具本身永远不执行部署和启动。