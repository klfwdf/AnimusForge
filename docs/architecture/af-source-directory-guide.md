# AF 当前源码目录简明说明（第三轮，2026-10-01）

三轮为源码物理归位，不新增DLL、不拆算法。namespace、CLR类型、程序集、存档键/数值身份、公开API、默认入口及运行资源布局不变。

## 当前目录

| 目录 | 内容 |
| --- | --- |
| `src/modules` | 同DLL领域模块；Conversation/Prompt/LLM/Memory/Knowledge/Economy等此前归位保持 |
| `src/AF.GameAdapter.Bannerlord/Composition` | SubModule、IntegratedModuleHost、MyBehavior22等整体Campaign宿主 |
| `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath` | 本轮19个GCCZ游戏绑定实质业务宿主，含规则、状态机和保存责任；不是薄桥 |
| `src/AF.GameAdapter.Bannerlord/Composition/Gathering` | 本轮NobleGatheringBehavior完整业务宿主 |
| `src/modules/AF.Module.Diplomacy` | 外交现有canonical源码目录；本轮18个原根级文件归Direct/World/Annexation/Profiles/Vassalage/Agenda/Guards |
| `src/bridges` | 第二轮30个具证AF-side薄桥，业务owner不因路径变化而转移 |
| `AnimusForge.SiegeAftermathIntervention` | 184项依赖free纯core Compile保持；本轮20游戏宿主在其递归SDK项目树之外 |

## 根级与以后新外交

根级tracked C# **327→149→38→0**，三轮累计327文件原样归位（178+111+38）。这不表示根目录所有资源、工程配置、说明、依赖、第三方或历史产物已清空；这些不在本轮清理范围。

未来新外交实现应以 `src/modules/AF.Module.Diplomacy` 的对应canonical文件为替换目标，另行核对接口/保存身份及获得实施批准。本轮没有覆盖新实现、删除旧业务、镜像G盘或创建兼容副本。不要在root恢复同类型副本或新增重复Compile；单源码双API仍只发布一个Modules/AnimusForge模块，Bootstrap只加载一个实现。

## 现态和历史

- [唯一主台账第三轮](../animusforge-refactoring-and-repository-reorganization-plan.md#source-relocation-round3-20261001)、[范围说明](af-framework-code-scope.md#source-relocation-round3-20261001)、[813代码坐标](af-framework-code-map.json)为当前导航；[第三轮精确批准清单](../plans/2026-10-01-source-relocation-round3-manifest.json)提供38 old/new与逐项责任/字节证据。
- 前两轮外交/团队root保留状态已被第三轮明确批准替代，历史正文不改。`tests/output_isolation.py::current_source_path`累计327映射只处理当前checkout；历史git show/review键和原hash不刷。
- 主csproj/sln/纯core项目不改，双API1150 Compile一一映射/7嵌入资源LogicalName保持，纯core184不变。目录名称不宣称职责抽取完成或新的生命周期。
- 一键编译/覆盖/打包不变；离线验证用最小环境与新UUID输出。无push、Stage、部署、清理、下载安装、仓外镜像。原SessionTransport dirty和旧NuGet/tools/产物保留。

实机、旧档、真实网络/音频/性能与未来新外交覆盖未验；完整历史外交/TeamModuleServices业务不因有限契约检查而宣称通过。
