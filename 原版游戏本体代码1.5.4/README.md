# Bannerlord v1.5.4.123627 本地反编译参考

来源为真实客户端程序集；每个程序集一个目录，保留 ILSpy 生成的逐类型 C# 和工程，布局沿用既有 1.4.5 参考。
2026-10-06 从官方 Steam beta 分支独立取得本体 app 261550 与战帆 app 2927200；精确 depot/manifest 和工具来源见 [_source.json](_source.json)。真实版本由 TaleWorlds.Library 的 BuildInfo 核实为 **v1.5.4.123627**。
共 **81 个程序集、8106 个 C# 文件、81 份生成工程**；包含本体、沙盒、剧情、自定义战斗、多人、BirthAndDeath 和 NavalDLC。
精确来源与 SHA-256 见 _manifest.txt / _manifest.json；跳过的原生文件及实际诊断见 _failures.txt。
此目录用于阅读与适配分析，不能作为 AF 编译引用、游戏原始工程或 1.5 兼容证明。C++ 原生引擎内部不在导出范围。
依赖和原始 DLL 只保存在 ignored artifacts；AF 既有项目已排除“原版游戏本体代码*”目录，发布包不应包含本目录。
官方 CustomBattle / Multiplayer 中同名 DLL 的不同二进制会逐文件比较反编译结果；仅在全部导出文件相同时共用一个目录，两个来源的哈希与比较结果保留在 _manifest.json。
80 个程序集没有命中反编译诊断；EpicPlatformServices.cs 因 Steam 客户端未附带 Epic 的托管 SDK/provider，保留 **101 条**未解析类型注释（既有 1.4.5 参考也有相同数量）。该限制逐行记录在 _failures.txt / _manifest.json，状态为 DECOMPILED_WITH_KNOWN_PLATFORM_DIAGNOSTICS；没有隐藏或伪造缺失方法体。
手动导出工具为 tools/export_bannerlord_reference.ps1；默认拒绝全部诊断，只有显式 AllowKnownEpicDiagnostics 才接受这一文件的类型注释，其他方法失败仍阻止发布参考目录。
