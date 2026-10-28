# Bannerlord v1.5.6.125053 本地反编译参考

来源为真实客户端程序集；每个程序集一个目录，保留 ILSpy 生成的逐类型 C# 和工程，布局沿用既有 1.4.5 参考。
精确来源与 SHA-256 见 _manifest.txt / _manifest.json；跳过的原生文件及实际诊断见 _failures.txt。
此目录用于阅读与适配分析，不能作为 AF 编译引用、游戏原始工程或 1.5 兼容证明。C++ 原生引擎内部不在导出范围。
依赖和原始 DLL 只保存在 ignored artifacts；AF 既有项目已排除“原版游戏本体代码*”目录，发布包不应包含本目录。
官方 CustomBattle / Multiplayer 中同名 DLL 的不同二进制会逐文件比较反编译结果；仅在全部导出文件相同时共用一个目录，两个来源的哈希与比较结果保留在 _manifest.json。
Steam 客户端未附带 Epic 的托管 SDK/provider；如清单标记 KNOWN，该平台连接文件存在明确记录的未解析类型注释。其余程序集仍通过严格诊断检查；不要把这些注释当成原游戏源码或已验证的实现。

## 1.5.5 → 1.5.6 beta 替换

- 实际游戏版本：`v1.5.6.125053`；Steam `beta` 分支，build ID `26242440`。
- 81 个官方托管程序集，81 个生成工程，8,111 个 C# 文件；82 处 DLL 来源（含一组官方重复程序集）覆盖核对通过。
- 对比 `v1.5.5.124170`：81 个程序集二进制均变化；C# 文本变化 220 个、删除 4 个、不变 7,891 个、无新增。详细路径见 `_comparison_to_1.5.5.json`；文本变化不等于 API 语义变化。
- 4 个移除类型为 `GauntletMapParleyAnimationView`、`MapParleyAnimationView`、`MapParleyAnimationVM`、`MapParleyAnimationParentBrushWidget`。
- 按用户明确要求直接删除原 `原版游戏本体代码1.5.5/`；旧树可从本地 Git 检查点 `f022e81` 恢复，1.3.x/1.4.5 参考保留。导出清单记录导出时状态，随后旧树删除记录在 `_source.json`。
- 反编译没有方法失败；`TaleWorlds.PlatformService.Epic` 保留 Steam 版缺少 Epic SDK/provider 导致的 101 处已知类型诊断，详见 `_failures.txt`，其余程序集无此类诊断。
- 已核对 DLL SHA-256 稳定、官方托管覆盖、同名 DLL 导出一致、旧树删除前 8,201 个文件未变，以及 AF 实际 Compile/EmbeddedResource/None 输入排除参考树。
- 本轮只更新游戏反编译参考，未做 AF 1.5.6 兼容适配、编译、实机、部署、打包或 push；C++ 原生引擎不在托管导出范围。
