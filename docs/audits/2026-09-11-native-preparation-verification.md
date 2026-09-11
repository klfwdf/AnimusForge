# Native 初始准备验证（2026-09-11）

源码/测试 `0306beba`；检查点 `62468e7c`；对照 `50f84818`。日志 `.tmp/native-preparation-20260911/`；可提交的精确源文件和六份 DLL 哈希见同名 JSON。

## 复现与行为证据

- 原始片段 589 检查中 52 个运行期失败：后台游戏读取，以及 stale/退休/超时前后的越界准备。原始健康片段仍在 fixture 中实际执行，作为候选的输出/参数/顺序对照。
- 候选 589 检查全通过；48 组 Hero/非 Hero、Scene/Map、文化、已有历史、挑衅资格/fallback、传唤/带路编号和规则输入组合均对照原结果。
- 5 个变异都编译并按运行断言失败：移除准入、搬回后台、丢文化、丢规则、改带路起始编号。
- 真实 entry slice、capture、共用队列 helper 被提取执行；game helper 和 admission 判定是 recording stubs，不是真实游戏。生产队列常量仍 30 秒，fixture 缩为 120 ms。
- 当前准备块逆变换后，整个 Submit/整个 ShoutBehavior 与基线一致；capture 中的旧 builder 语句逐字相同。全部共用 builder / Action Core / 存档逻辑未变。

## 回归与修正过程

- 原准入静态测试最初仍要求入口内 7 个守卫而失败。已改为严格核对入口 6 + capture 1、调用一次且 guard 在 NPC 读取前。最终 44 检查、7 个原缺陷变异均通过；不是直接把 7 改为 6。
- 共用 runner 的全文逆变换只额外接受精确 SHA 固定、由本轮独立 suite 证明的 Submit 声明。132 检查和 7 个原变异全部保留并通过。
- 展示 46、动作派发 88、收尾/严格记忆 184、前置历史 111 检查通过；Team ports 308 / 13 方法 / 31 receiver / 3 变异通过。
- 新 partial 初次生产构建发现缺少既有 `TownAfRuleRoutingPolicy` namespace import，修正后最终 Debug/Release × 1.3/1.4/Bootstrap 六项 Stage 全通过；原失败日志保留为 `build-Debug-before-namespace-import.log`。
- 最终 16 组相关回归符合预期；四份实际实现 DLL 532 元数据断言通过，引用版本为 `v1.3.15.110062` / `v1.4.6.115628`。168 个存档绑定身份未改，fixture 无需刷新。
- `all-missing` 仍 exit 2、接受实机证据 0 条，不能写成阶段 8 实机 PASS。

## 清理及边界

删除本条 Native 请求里被替代的后台准备片段；共用 builder 仍有真实调用，必须保留。新准备包为 private，仍包含既有 LocationCharacter/Location-bearing targets，不是第三方 immutable DTO；没有增加队列跳转、Tick 或候选扫描次数。

本轮源码/测试定向 diff --check、冲突/废弃片段搜索通过。用户两份 2026-09-06 草稿保持原样，未 stage；其中既有空标题尾随空白不属于本轮修改。

未推送、未部署、未操作真实存档、未进行真实游戏验收。更早 persona、深层持久记忆/检索、独立周报快照的上下文绑定、后续持有游戏引用的访问、TTS 和 Courier prepare 仍需接续。构建与 fixture 不证明这些未做部分安全。
