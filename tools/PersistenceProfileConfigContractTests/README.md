# Persistence/Profile/Config Contract Tests

这是阶段 4 的纯 Python runner。它不引用 Bannerlord、生产程序集、网络、存档或游戏目录。

验证内容：

- `persistence-catalog.json` 的 95 个真实字面量 `SyncData` key 与指定生产 owner 文件同步；
- key 去重、来源文件存在、符号 key/chunk 待盘点项明确；
- JSON/PlayerExports 分类和旧身份保护不变量；
- profile closure、SafeMode 数据保留、配置 reload 快照隔离和凭据排除。

运行：

```powershell
python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py
python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py --json
```

第二条路径仅在 runner 目录名称被重命名后适用；标准命令使用第一条路径。
