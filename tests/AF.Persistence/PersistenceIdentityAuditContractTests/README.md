# PersistenceIdentityAudit contract tests

这些测试只覆盖审计器的离线契约：`git cat-file --batch` 解析、当前源码快照复用、stderr 阶段进度、`--json` 的纯 stdout 以及错误时 fail-closed。审计仍只比较 SyncData、CampaignBehavior 和模块身份，不启动游戏、不读取存档。

```powershell
python -B -m unittest discover -s .\tools\PersistenceIdentityAuditContractTests -p 'test_*.py' -v
python -B .\tools\PersistenceIdentityAudit.py --json
python -B .\tools\PersistenceIdentityAudit.py --json --quiet
```

`--quiet` 仅关闭阶段进度；`--json` 的机器可读结果始终写入 stdout，错误和进度不会混入 JSON。
