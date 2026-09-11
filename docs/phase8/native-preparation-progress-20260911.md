# Native 初始场景准备闭环（2026-09-11）

基线 `50f84818`；检查点 `62468e7c`；生产/测试 `0306beba`。

## 本轮完成

- 原 persona await 之后的 NPC/场景目标/挑衅规则/规则资格准备，接入原 request_target_validation 主线程消费；消费时先验证 admission。
- 原 builder、参数、数据与顺序保留，移除重复后台片段；private 包不作为公共 API，也不宣称脱离 Location/LocationCharacter 引用。
- 589 检查 / 5 变异；调度 132/7、准入 44/7、展示 46、动作 88、收尾 184、前置历史 111、ports 308/3；六项最终 Stage、16 组相关回归、四 DLL 532 元数据通过。
- 原准入守卫数仍 7（6 + 1），严格静态接线和精确源码逆变换保留。初次 namespace 编译失败与旧断言漂移已定位修正，原始日志保留。
- 本地提交，未推送/部署/实机/存档访问，用户草稿保留。

## 下一步按实际依赖拆解

1. 持久历史：Task.Run 当前仍解析 Hero/非 Hero/部队身份，取得 owner 可变 blocks/drafts、总览，然后执行 recall 和 preprocess selection。先证明数据读取/检索/缓存更新的责任，不能整段主线程化或删掉召回。
2. 为该流程准备必要的主线程数据捕获、后台输入及原上下文完成/丢弃边界；若发现召回会修改缓存，须保留真实缓存更新责任，不能只复制后默默丢掉。
3. 尚未迁移的 persona/周报绑定/后续目标引用读取、TTS 直接回调与 Courier 双向 prepare 接续。
4. 这些验证闭环后再评估有限公共文本提交；真实游戏/旧存档仍独立验收。

技术入口：`docs/architecture/af-native-initial-preparation-boundary.md`。更改其他 host 部分时补独立审查证据，不删除已有 whole-host inverse 或变异测试换绿灯。
