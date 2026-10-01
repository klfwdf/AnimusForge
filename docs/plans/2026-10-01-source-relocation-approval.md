# AF 源码目录归位：精确批量审批摘要（2026-10-01）

状态：**仅提案，未搬迁、未修改产品/测试、未提交；等待一次精确批量批准。**

## 精确范围

- 工作区 `E:/AnimusForge-refactor-continuation-20260831`；分支 `codex/af-main-refactor-continuation-20260831`；HEAD `8f3903e257b2a454763efc9468af47ee0bd37ee1`。
- [完整总清单](2026-10-01-source-relocation-manifest.json)：**179 个 old → new**（P1 41 / P2 7 / P3 131），每项记录 checkout 字节 SHA、HEAD blob SHA、LF 归一化 SHA及包级职责证据。
- 根级 tracked C# **327 → 148**（仅批准并成功实施后）；148 个具体保留项全部列出。未调查保守保留只是库存覆盖，不冒充职责复核。
- 154 个精确消费者文件仅允许必要的当前源码路径接通；先读每处上下文。`git show` 历史对象路径、旧 baseline 键和原 review digest 不机械替换。另 7 个原版/历史文档误命中保持只读。
- 正式交付：既有主台账唯一新条目、HANDOFF 摘要、活动代码范围图/JSON及必要简明目录说明。
- 不改 namespace、类型、程序集、存档键、API、算法、默认入口；只整理物理目录，**不称职责拆分完成**。

## 迁后目录用途（提案）

| 目录 | 本批归位数 | 用途 |
| --- | ---: | --- |
| `src/AF.GameAdapter.Bannerlord` | 103 | 游戏API适配、兼容、UI/patch/诊断，责任不变。 |
| `src/AF.Persistence` | 2 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Conversation` | 24 | Scene/Native/Courier及主动会话现有宿主/partial。 |
| `src/modules/AF.Module.Duel` | 4 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Economy` | 9 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Encounter` | 3 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Issue` | 2 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Knowledge` | 6 | 知识宿主、检索、实体及语义组件。 |
| `src/modules/AF.Module.Llm` | 5 | 现有网络/协议/TTS/Ambient，不扩大owner。 |
| `src/modules/AF.Module.Onboarding` | 1 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Prompt` | 13 | 配置、组合和检索。 |
| `src/modules/AF.Module.Settlement` | 1 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.Social` | 5 | 现有领域实现原样归位，详见包级职责证据。 |
| `src/modules/AF.Module.UI` | 1 | 现有领域实现原样归位，详见包级职责证据。 |

## 明确保留与安全边界

- `MyBehavior` 全部22个根级partial：混合Campaign/save、制作组接缝及外交周报宿主，整家族保留。
- `SubModule`、`IntegratedModuleHost`、`DuelSettings`全家族及其他混合宿主保留，不改默认装配。
- `CourierLetterReplyPopup.cs` / `CourierLetterReplyPopupVM.cs` 因外交测试直接按旧路径读取而留原位；外交源码/tests/fixtures/metadata全部排除。
- Shout 16个根级partial属同批准批次；已有src partial不动。初始dirty `CourierDeliveryBehavior.SessionTransport.cs`保留，不纳入提交。
- 制作组Policy/GCCZ/Gathering/Vengeance、第三方、运行资源、原版参考、私有依赖、历史档、旧tools/NuGet及产物不动。
- 不清理/删除/push/Stage/部署/下载/安装/改一键行为/改全局设置/广域仓外写。合成测试仅沿已查到的旧精确授权 `E:/tmp/af-j17-20260930` 使用新内容，不扩大。旧凭据轮换风险未闭，不输出/继承凭据。

## 已核与批准后退出门

1. [双API无target编译求值基线](2026-10-01-source-relocation-compile-baseline.json)：各Compile **1150唯一tracked输入、0重复**，两集合相同；各7个嵌入资源、显式逻辑名。没有restore/build。
2. 179项byte SHA与提案一致、HEAD内容按LF等价、目标不存在、old/new无重复。checkout常为CRLF而git blob为LF，分别记录hash，不混淆。
3. 批准后P4创建本地意图/checkpoint、精确路径提交。先小批次核真实Compile集合按映射一一等价、资源逻辑名不变，再实施余项。
4. 当前读取/历史读取分别处理；SourceLink/提取/工具literal/反射/清单接通，不盲刷review hash。代码图迁后绑定实际修订，保留真实符号/行号证据。
5. 复用已审 `docs/handoffs/j17-offline-build-gate.py`：新UUID仓内输出、拒已有目录、禁prune、最小环境，Debug/Release × 两API + Bootstrap。原一键脚本删除固定输出，不直接运行、不改行为。
6. 复用严格具名非外交C43和真实1.3/1.4 message/history验证；先审所选runner副作用与隔离。当前历史C43 43 PASS只是复用依据，不代表搬迁后已验。
7. 最终同候选交付：根级前后数/保留理由、定向回归、双构建、提交与未验写入主台账。不称全仓PASS；实机/旧档/真实网络/性能仍须单独验。

## 审批问题

是否允许按总清单的 **179个精确old/new原样批量搬迁**、必要当前路径消费者修正及正式交付材料更新，并在新隔离输出下完成上述本地验证/切片提交？不包含排除项、清理、上传或部署。
