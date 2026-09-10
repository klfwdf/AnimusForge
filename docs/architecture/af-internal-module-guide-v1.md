# 制作组内部模块接入说明 V1

## 使用者和位置

政策、宴会、GCCZ 等编进 `AnimusForge.dll` 的制作组代码使用 `AnimusForge.Refactor.Modules` 的 internal 契约。它不是独立子 MOD SDK，也不是从磁盘扫描插件的容器。

源码入口：

- `G:\AFMOD\AF-REFACTOR\Refactor\Modules\TeamModulePorts.cs`：专用方法签名及责任注释。
- `G:\AFMOD\AF-REFACTOR\Refactor\Modules\TeamModuleAdapters.cs`：唯一转接原业务 owner 的薄实现。
- `G:\AFMOD\AF-REFACTOR\Refactor\Modules\TeamModuleServices.cs`：类型确定的单例接线。
- `G:\AFMOD\AF-REFACTOR\Refactor\Modules\InternalModuleDirectory.cs`：登记、冻结、依赖/版本校验和状态查询。
- `G:\AFMOD\AF-REFACTOR\Refactor\Modules\ModuleFrameworkRuntime.cs`：显式装配和外部只读映射。

## 当前登记范围

| 模块 ID | 接缝能力 ID | 契约版本 | 目录门禁 |
|---|---|---|---|
| af.team.policy | af.team.policy.dialogue | 1 | 不新增门禁；仍由既有政策资格规则执行 |
| af.team.gathering | af.team.gathering.dialogue | 1 | 不新增门禁；仍由既有宴会资格规则执行 |
| af.team.siege | af.team.siege.dialogue | 1 | conversation-siege，原薄桥也保留同一实际门禁 |

`dialogue` 在这里指本次列明的方法组，不代表模块所有功能都已迁入新框架。没有为了凑齐表格给所有旧领域伪造 Ready 条目，也不把历史设计 fixture 当运行 manifest。

## 薄桥契约

1. 保留每个调用的目标、渠道、playerText、replyIsDirectPlayerResponse、AgentIndex 及 ref/out。
2. 后处理规则由原模块构建；正文不新塞动作标签；normalize 不变成 execute。
3. apply 返回 facts/notifications 后，仍由原调用方写历史/AFEF、展示通知。本层不二次提交。
4. 业务方法原有异常语义保持；不能 catch 后返回空字符串伪造正常。
5. 静态单例只保存无状态 adapter；不保存 Hero、Mission、会话、存档或请求结果。
6. 原有 NpcRulerPolicy 活动政策上下文兼容方法目前返回空正文；薄桥原样保留，不把包了一层说成恢复了新功能。

## 登记生命周期

```text
new Directory → TryRegister(definition) → CompleteRegistration()
              → owner 确认 adapter 已装配 → UpdateRuntimeState(Ready)
              → GetCapabilityStatus / GetSnapshot
```

- 登记并不自动 Ready。版本为精确匹配的正整数，不是未经验证的 SemVer。
- 空 ID/非法版本/重复模块或能力等拒绝；不覆盖既有 provider。调用方必须检查拒绝结果。
- 冻结时校验缺失依赖、版本和环；受影响模块及依赖者不可用，无关模块不被一概判死。
- 冻结后拒绝新增登记；首版不支持运行时替换 provider/热卸载/子 MOD 注册。
- 查询结果中的门禁状态不是实际请求授权；执行资格继续在原 owner 检查。

## 新增制作组接缝时的维护步骤

1. 先确定业务 owner，保持业务源码原位；写一个有实际调用者的 typed 方法，不加入未使用占位接口。
2. 给 adapter 做参数一一转发；在对应真实主体入口替换直接调用。跨渠道的同类接缝一起检查。
3. 在装配根绑定实例和唯一 ID；只有实际接线的方法组可以登记为已装配。
4. 选用确实支配该能力的既有 FeatureBridge；没有合适的不要擅自套更宽的功能开关。
5. 补参数/结果/ref/out 和原行为对照；不要只测“能找到模块”。
6. 更新能力矩阵和 HANDOFF。新增 public 能力是另一项工作，internal 方法不能自动外露。

## 性能与兼容

登记/依赖校验在冷启动；实际主体调用通过 typed 单例直达原 adapter，不逐次查字典。公共查询按需构造少量只读快照，不新增每帧扫描或网络请求。框架目录不持久化，不改变任何 SaveableTypeDefiner/SyncData 键。
