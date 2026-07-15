# AF 797adf7e + SETS/GCCZ 融合适配（2026-07-15）

## 基线

- AF 上游：`Mount-Blade-Bannerlord-AnimusForge-mod` 的 `origin/main`，提交 `797adf7e69f7f43e5fb550422645462f93408737`。
- 融合树：`NEW-10`；SETS、GCCZ 城镇、GCCZ 城堡继续由此树编译。
- GCCZ 可复用核心：`GCCZ/src/AnimusForge.SiegeAftermathIntervention`，必须与融合树同名目录逐文件同步。

## 新 AF 运行结构

新上游改为单模块、多实现 DLL：

1. `Modules/AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.dll` 是唯一 `SubModule` 入口。
2. Bootstrap 根据游戏版本加载 `versions/1.3/AnimusForge.dll` 或 `versions/1.4/AnimusForge.dll`。
3. 旧 `AnimusForge_1_3_x` / `AnimusForge_1_4_5` 不能与统一模块同时启用，否则 Bootstrap 会拒绝加载旧实现。
4. ONNX 模型与本机运行依赖仍位于统一模块 `bin/Win64_Shipping_Client`，部署时不得遗漏或覆盖为空。

## 保留的融合边界

- **SETS**：保留自有城镇内部冲突、胜利结算和原版胜利菜单链路。SETS 不建立独立城堡入口；内部夺下城堡后，仍应由原版围城胜利菜单进入 GCCZ 城堡处置。
- **GCCZ 城镇**：保留 active-stage 独占规则、AF 对话/喊话上下文、postprocess、标签归一化/路由和 Bannerlord 实际后果。
- **GCCZ 城堡**：保留原版胜利菜单入口、50 名己方士兵 + 200 名俘虏选择、原版围城战争场景层、城墙破坏映射、原版指挥编队、战俘/领主生成、两名旗帜兵、NPC 身份与领主战败来源提示、离场默认宽恕。
- 不复活旧 GCCZ-C；主体规则留在独立 GCCZ core，`NEW-10` 只承担 active-stage gate、运行时数据和实际副作用。

## 本轮发现并处理的不和谐点

1. **双版本引用混用**：本机游戏是 1.3，旧脚本会把 1.3 安装目录误用于 1.4 实现。`build_single_module.ps1` 新增 `Bannerlord14ReferenceDir`，验证 1.4 `BuildInfo` 并把所有 1.4 游戏引用定向到同一 overlay，失败时停止构建。
2. **城堡误用城镇语义**：城堡 active stage 原本仍注入普通民众、救济、搜掠、血洗、迁殖等城镇提示与后处理标签。新增 `SiegeCastleRuntimePromptProfile`，明确战后城堡角色、指挥编队不改变俘虏身份、直接回应门槛，并在城堡阶段禁用所有城镇 GCCZ 标签及固定词旁路。
3. **上游设置移除残留**：上游已移除百科英雄人格自动生成开关；合并后残留的旧读取 helper 已删除，避免引用不存在的设置属性。
4. **预处理架构变化**：新 AF 增加 preprocess。GCCZ active stage 继续走独占薄桥：不让普通 AF 规则抢占，不让 preprocess 改写 GCCZ 上下文；非 GCCZ 场景保持上游新流程。
5. **启动器冲突**：部署统一模块后必须停用旧 `AnimusForge_1_3_x` / `AnimusForge_1_4_5`，只启用 `AnimusForge`。旧目录本身保留，便于回滚。

## 当前城堡动作边界

- 城堡专用战俘“收编/屠戮”、己方士兵“收编后不满/安抚”和领主处置仍是独立接口边界，不能用城镇搜掠/血洗标签冒充。
- 领主处决仍只保留接口，不在本轮上游融合中落地。
- 本轮首先保证入口、场景、生成、对话身份、标签隔离和安全离场不因 AF 更新回归；城堡专用真实副作用应在后续独立实现并增加 core 测试。

## 验证要求

- GCCZ standalone core tests 全部通过。
- `GCCZ/src/AnimusForge.SiegeAftermathIntervention/*.cs` 与 `NEW-10/AnimusForge.SiegeAftermathIntervention/*.cs` 文件名和 SHA-256 完全一致。
- 统一模块同时构建 1.3、1.4 与 Bootstrap，0 error；stage/deploy 后校验三份 DLL、ONNX 和私有依赖。
- 游戏内仍需分别验证：SETS 城镇胜利、GCCZ 城镇完整标签链、GCCZ 城堡进场/编队/对话/离场。
