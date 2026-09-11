# 共用主线程函数验证（2026-09-11）

生产/测试 `5bf830f3`；检查点 `84d7097b`；对照基线 `613ac245`。**仅本地验证，未推送/部署/真实存档访问。**

## 实际复现与结果

- 基线链接真实两个方法，达到 119 检查、25 个运行期失败：claim 后超时提前返回、重复 callback 再执行、诊断改变结果/阻断完成、排队格式异常被吞、失败发布遗留晚到工作。不是用编译失败充数。
- 候选 132 检查全部通过；7 个行为变异均编译并按预期 exit 1 / FAIL。日志异常与 `Exception.Message` 格式化异常均隔离。
- 测试使用物理消费线程和 fixture 队列；deadline 缩短为 120 ms，生产常量核对仍为 30,000 ms。真实游戏对象/网络不在该 fixture 内。
- 规范化全文逆变换后等于 `613ac245`，只有两个调度声明有变化，25 个业务调用点/所有存档及动作 Core 均未改变。局部白名单只增两个无 Team receiver 的精确声明 SHA，未削弱 Team ports 全文逆变换。

## 回归与构建

- Native 准入 44、展示 46、动作派发 88、收尾 184、前置历史 111 检查通过。这些旧 suite 对通用 helper 使用 stub，本轮新 132 检查才执行真实 helper，不能互相冒充覆盖。
- Team ports 最终 308 断言 / 13 方法 / 31 live receivers / 3 变异通过。
- Debug/Release × 1.3/1.4/Bootstrap 六项最终 Stage 通过。实际引用 `v1.3.15.110062` / `v1.4.6.115628`；未修改构建脚本、布局或程序集身份。
- 四份最终实现 DLL 的 532 元数据断言通过；public V1、legacy memory ABI、internal 模块与 memory owner 均保持。
- 最终 16 组相关回归符合预期：管线/隔离、4 组生产回放、bridge、168 存档绑定、入口清单、Scene parity/queue、三渠道边界、Courier owner、TTS、API、missing-evidence。
- `all-missing` 明确返回 **exit 2 / acceptedEvidenceCount=0**；这是门禁拒绝缺失证据，不是阶段 8 实机 PASS。

原始日志和命令：`.tmp/mainthread-functions-20260911/`。自审前构建以 `before-diagnostic-format-*` 留存；最终证据为无此前缀的 `build-Debug.log` / `build-Release.log`、`current-delivery.log`、`regression-results.json`、`team-ports-final.log`。六份 DLL 哈希与旁置 `.build.json` 已逐一核对，见同名 JSON。

## 清理与边界

删除非原子取消 bool、旧 wait 吞错分支和重复 direct/queued 执行处理；未新增调度器或第二套 LLM/动作/记忆实现。定向清理和本轮 diff --check 通过。全仓 diff --check 仍可见用户两份 2026-09-06 草稿第 1 行空标题的尾随空白；未修改或提交那些草稿。

保留普通异常 fallback 的历史责任：它不证明没有部分副作用，也不是自动重试许可。claimed 操作无法硬取消，owner 若一直不返回则仍需等待真实结果。新接口只读；真实 1.3/1.4、旧存档、完整第三方 DLL、慢请求/换场景仍待玩家验收。
