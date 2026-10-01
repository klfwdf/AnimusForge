# AF 当前源码目录简明说明（2026-10-01）

本次只做物理归位，保持同一 `AnimusForge.dll`、namespace/类型/存档/API/算法/默认入口；目录名不表示新增生命周期、职责抽取或拆DLL。

## 本批实际布局

| 目录 | 本批原样归位文件数 | 说明 |
| --- | ---: | --- |
| `src/AF.GameAdapter.Bannerlord` | 103 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/AF.Persistence` | 2 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Conversation` | 24 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Duel` | 4 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Economy` | 8 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Encounter` | 3 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Issue` | 2 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Knowledge` | 6 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Llm` | 5 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Onboarding` | 1 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Prompt` | 13 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Settlement` | 1 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.Social` | 5 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |
| `src/modules/AF.Module.UI` | 1 | 现有职责原样归位；详见[逐文件职责及映射](../plans/2026-10-01-source-relocation-manifest.json)。 |

- `src/AF.GameAdapter.Bannerlord`：游戏API/兼容、UI/patch/诊断；TaleWorlds对象线程责任不变。
- `src/modules/AF.Module.Conversation`：Scene/Native/Courier及主动会话现有宿主；Shout全部16个根级partial同包归位，与已有partial仍是同类型。
- Prompt/LLM/Knowledge/Issue/Duel/Economy/Social等目录对应本批已审核的具体文件，**不是整个领域全部改写**。
- `src/AF.Bootstrap`、既有Contracts/Foundation/Persistence、其他已存在模块与资源目录保持原职责。

## 明确保留在根级

根级tracked C#实际 **327 → 149**：批准179候选中178归位，1项因外交消费者hold；原提案148保留项不变，新增 `RewardSystemBehavior.cs`。

- `MyBehavior`全部22个根级partial：混合Campaign/save、制作组接缝与外交周报宿主，不按文件名拆家族。
- `SubModule`、`IntegratedModuleHost`、`DuelSettings`全家族及其他混合宿主保留；默认入口不改。
- `RewardSystemBehavior.cs`因J12外交domain测试读取旧路径原样留根；其获准partial归位不改变同一类型。`CourierLetterReplyPopup.cs/VM`原有外交消费者hold也保持。
- 制作组Policy/GCCZ/Gathering/Vengeance、第三方、运行资源、原版参考、私有依赖、历史档和已有产物保留。未调查保守保留是库存分类，不冒充职责审查完成。
- 初始dirty `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs`不纳入本次提交。

## 如何定位源码和历史

1. 现态定位读[代码范围图](af-framework-code-scope.md#source-relocation-20261001)与[代码坐标JSON](af-framework-code-map.json)。历史证据/`git show`仍使用当时的旧路径，不机械改写旧台账。
2. 测试 `tests/output_isolation.py::current_source_path`只解析178个当前路径，旧review键/历史Git路径不变；不提供第二份生产源码。
3. 主项目SDK默认Compile已包含src，本次不改主csproj/sln或增加重复Include。嵌入资源7项显式逻辑名保持。
4. [审批总清单](../plans/2026-10-01-source-relocation-manifest.json)保留原179项；实际178+1hold替代关系及验证/提交见[唯一主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#source-relocation-20261001)。

仅为本批结构导航；实机、旧档、真实网络和性能验收与离线结构/构建/契约验证分开。
