# SceneActions / 西海扩展整合状态

## 当前状态

- 独立模组 `AnimusForge_XihaiAction v1.1.0` 的可维护文件已迁移到
  `extensions/AnimusForge.XihaiAction`。
- `MIGRATION_MANIFEST.json` 记录 93 个源文件、资源、配置、测试和工具的
  SHA-256。生成的 `bin`、`obj` 与运行日志不作为源码迁移输入。
- SceneActions Core 与 Runtime 已直接编译进两个版本的 `AnimusForge.dll`，不增加
  第二个运行时 DLL。
- AF `SubModule` 在 Mission behavior 初始化前注册动作、阵前演讲和演讲表演三个
  behavior，并在初始化后核验会话状态。
- MCM、严格 JSON、双语文本、动作 XML 与 TPAC 由单一 `Modules/AnimusForge`
  发布。独立运行时只保留在 `extensions` 中作为源码边界和测试基线。
- MCM 可见标题为“自然语言动作与阵前演讲”；自然语言玩家/NPC动作、AF闭集兜底和
  演讲自然语义统一由一个“自然语言回复动作”开关控制，旧零散字段仅保留兼容读取。
- `RuntimeIntegrationEnabled` 为 `true`。游戏中不得再启用
  `AnimusForge_XihaiAction`，否则会重复处理同一轮喊话。

## 权威边界

迁移后以本仓库中的 `extensions/AnimusForge.XihaiAction` 为后续整合源码。
游戏目录里的独立模块在部署时改名为禁用备份；运行时只加载 `AnimusForge`。

## 直接整合路线

1. 在 AF `SubModule` 的 `OnBeforeMissionBehaviorInitialize` 与
   `OnMissionBehaviorInitialize` 生命周期中注册并核验 SceneActions Mission
   behaviors；Bootstrap 已转发这两个回调。
2. 用 AF 内部类型化接口替代 `AfCompatV130`：玩家输入、冻结框选、对话代次、
   NPC 回复排队、正文显示/TTS 和回复认领都由 `ShoutBehavior` 直接发布事件，
   不再反射私有字段或 Harmony patch 自身方法。
3. 复杂动作判断复用 AF 统一后处理。动作标签只进入
   `PostprocessRules`/`ActionPostprocessPrompts.json`，不得泄露进主回复 prompt。
   场景喊话和面对面自由对话共享同一解析器；信使没有 Mission Agent，必须由
   显式 chain gate 排除动作执行，但历史和 AFEF 事实结构仍与其他渠道一致。
4. 玩家本地明确命令继续走确定性解析，不调用模型。每条 NPC 回复最多复用一次
   已有后处理结果；禁止在 Mission Tick 中发网络请求、全量扫描 Agent 或重复反射。
5. 把配置、本地化、动作 XML 和 TPAC 合并到单一 `Modules/AnimusForge` 资源布局。
   `SubModule.xml` 仍只加载 `AnimusForge.Bootstrap.dll`，不得恢复独立模块或第二个
   实现 DLL。

## 资源迁移注意项

- `AssetPackages/pack0.tpac`、`action_sets.xml`、`action_types.xml` 和
  `combat_parameters.xml` 必须作为一组验证；在一键单模块 staging 能携带并审计
  AssetPackages 前，不启用自定义跪姿资源。
- `items.xml` 需要改成 AF 唯一资源名并在统一 `SubModule.xml` 中增加对应 XmlNode，
  不能覆盖现有 `animusforge_scene_gold_items.xml`。
- 西海中英文文本需要并入 AF 的统一语言资源并保持稳定 `SAX_*` ID；MCM 设置身份
  需要从独立模块迁移为 AF 内置设置，不能让两个同 ID 设置页同时存在。

## 性能约束

- 输入解析按消息事件执行，不进入每帧 Tick。
- 语义表和正则只初始化一次；允许动作键、别名和反例集合复用缓存。
- Mission 侧只保存冻结快照和有界队列；听众按批次处理，每 Tick 使用固定预算。
- Agent 安全扫描必须节流并缓存，不允许每帧遍历全 Mission。
- 分类请求单飞、可取消且有超时；失败时关闭该次推断，不阻塞 Mission 线程。

## 完整启用前验收

- SceneActions Core 全量测试通过。
- AF `BannerlordApi=1.3`、`BannerlordApi=1.4` 和 Bootstrap 统一构建通过。
- 三渠道前处理、主链路、后处理、历史与 AFEF 事实边界完成对齐检查。
- 单模块 staging 包含并审计所有 SceneActions 配置、本地化、XML 和 TPAC。
- 独立模块关闭时完成游戏内玩家动作、NPC 回复动作、多人强制、阵前演讲、TTS、
  听众反应和 Advance 验证。
