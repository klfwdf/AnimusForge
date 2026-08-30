# Live Host Readiness Audit

`live_host_readiness_audit.py` 是只读启动前审计，不启动 Bannerlord、不部署模块、不修改游戏目录、不读取存档内容。

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

`installedMatchesStage=false` 不表示代码失败，只表示本轮没有把 project-local stage 部署到游戏目录；这是当前安全边界。只有明确授权 live 测试后，才允许另行执行部署和启动步骤。