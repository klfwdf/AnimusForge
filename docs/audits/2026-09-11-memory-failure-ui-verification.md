# 记忆失败提示验证（2026-09-11）

生产/测试 `6f0bac67`；检查点 `88777e45`；原始对照 `38488ed2`。**本地验证，未推送/部署/真实存档访问。**

## 复现与行为

- 原实现 85 检查中 37 个运行断言失败；候选 85 全通过。不是用编译或 fixture 越界代替行为失败。
- 7 个变异均编译、exit 1 且 FAIL：后台 UI、丢 generation、丢静态 owner、丢实际 Campaign owner、丢 revision、重置不清待提示、日志异常外抛。
- 实际 notice owner、SaveRuntimeGuard、EngineTick 入口和 9 条生产报错调用语句参与测试。UI/主线程标识/Campaign 对象为 fixture，完整 ONNX/网络 producer 未执行。
- 正文/标题、“知道了”按钮和暂停行为保持；并发最多一个待提示、失效、重置、迟到确认、显示失败/重入和日志失败均覆盖。
- 8 个 MyBehavior 声明精确逆变换；除提示 owner 外，算法/分支与原始正文逐字相同，整个 MyBehavior 恢复基线。9 个调用点都显式传入操作 generation，不在网络返回后重新捕获。

## 回归与构建

- 原 Native 准备 589、调度 132、准入 44、展示 46、动作派发 88、收尾 184、前置历史 111 检查通过；Team ports 308 / 13 方法 / 31 receiver / 3 变异通过。
- 最终 Debug/Release × 1.3/1.4/Bootstrap 六项 Stage 通过；引用 `v1.3.15.110062` / `v1.4.6.115628`。最终四份实现 DLL 532 元数据断言通过。
- 16 组相关回归符合预期，包括 Scene/Courier/生产回放/存档契约；missing-evidence 仍为 exit 2、接受实机证据 0 条。
- 存档契约初次因源码行号漂移报红，只修 `_patienceStates_v1` 两处 MyBehavior 定位：36435→36421、36444→36430。168 个 key/ref/type/source 身份不变，最终验证通过；不是修改存档格式。

原始证据：`.tmp/memory-failure-ui-20260911/`。`before-live-campaign-guard-*` 是自审前构建，不作为最终 DLL 证据；最终使用无该前缀的 build-Debug.log / build-Release.log / regression-results.json 和 current-delivery.log。六个 DLL 哈希与旁置 build.json 已逐一核对，见同名 JSON。

## 清理与未验证

移除旧后台 ShowInquiry 和无展示身份的直接 active 复位；共用错误出口仍有 9 个实际调用，保留薄 facade。复用现有 Tick，仅新增一个有界瞬态槽和展示 revision，不新增通用队列、网络路径或存档键。本轮定向清理与 diff --check 通过；用户两份草稿未修改/提交。

未运行真实 UI/暂停/旧存档测试。当前绑定的是记忆构造/总结操作起点，不能替代外层 Native/Courier 起点；当前存档内的主动清理也不取消先前网络任务。本轮未完成 blocks/drafts、场景/日期、Hero/部队身份的数据快照，不能据此宣称完整记忆线程安全或阶段 8 DONE。
