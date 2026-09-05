# Persistence/Profile/Config Contract Tests

这是阶段 4 的纯 Python runner。它不引用 Bannerlord、生产程序集、网络、存档或游戏目录。

验证内容：

- `persistence-catalog.json` 的 142 个真实字面量（原 95 + WarStats 47） `SyncData` key 与指定生产 owner 文件同步；
- key 去重、来源文件存在、符号 key/chunk 待盘点项明确；
- JSON/PlayerExports 分类和旧身份保护不变量；
- profile closure、SafeMode 数据保留、配置 reload 快照隔离和凭据排除。

运行：

```powershell
python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py
python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py --json
```

第二条路径仅在 runner 目录名称被重命名后适用；标准命令使用第一条路径。

2026-09-06 纳入 WarStats v1-v5 的 47 个既有键：总计 168 个类型绑定。保留原 95 个键的类型与 ref 身份；这里只登记现有生产存档契约，不新增/迁移/删除实际存档键，不代表实机旧档 round-trip 通过。
