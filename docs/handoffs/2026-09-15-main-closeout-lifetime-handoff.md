# 主体收尾 / 共用请求生命周期 HANDOFF（2026-09-15）

## 当前结论

**收尾已开始，C1 请求生命周期基础切片已修复并离线验证；整个主体收尾仍在进行，三渠道 public SDK 尚未开放完成。**

- 本轮生产/测试：`73774a94fc1d2fcbebc69ea221e9a906a4e70b8e`；意图检查点 `c8c074c8`；起点/上次已推送 `f03557fb`。
- 功能基线：fresh fetch 的 `origin/main = 437925b856fae76b4e9ee207e96ba048f35d5a67`。本次不是拿旧测试基线代替 main。
- 用户范围：只复现/拆分主体，排除政策/宴会/GCCZ等玩法；内部接口稳定；外部 Native、Scene、Courier **全部是必交项**，不能用只读查询替代完成。
- 本轮仅本地提交，未推送、未部署、未操作存档，自动化仍暂停；工作区 `G:/AFMOD/AF-REFACTOR`，原重构分支不变。
- 总体验收清单：[main 主体与双层接口矩阵](../phase8/af-core-main-closeout-matrix-20260915.md)。此表补充原14类职责计划，不是另一套阶段或完成百分比。

## 1. main 复现结果与批准的修复差异

`InteractionRequestCoordinator` 在修改前与固定 main 相同。本轮直接编译 main 的契约/协调器，普通30项通过，以下5类实际行为失败（不是编译失败）：

| main 问题 | 本轮处理 |
|---|---|
| 旧请求取消回调抛异常，阻止新请求开始 | lease 隔离并诊断回调异常，新请求继续 |
| Dispose 遇到一个回调异常就中断，其他渠道仍活跃 | 每个lease独立取消，三渠道都能收到取消 |
| Cancel/Supersede 直接Dispose，仍在执行的代码拿到已释放token | 执行结束才结束资源所有权，不提前释放 |
| 执行finally与取消回调并行，回调运行中source被Dispose | 活动取消调用计数；两方都结束后才释放 |
| 输入token已经取消仍调用pipeline生成 | 生成前明确检查，避免无意义LLM调用；仍返回已有CancelledAsStale状态 |

这五项是修复 main 的已知缺陷，不是恢复 main 的异常泄漏或无效生成。普通正文、模块/provider资格、档代、同session替换、异常降级和旧public签名保持。

## 2. 拆分、真实消费者与代码坐标

所有坐标绑定上述生产提交，一基行号：

| 路径 / 位置 | 符号与责任 |
|---|---|
| `Refactor/Runtime/InteractionRequestCoordinator.cs:30–107` | `ExecuteAsync`：原签名；channel/session接入、替换、pipeline、过期/错误结果；请求资源由lease拥有 |
| `Refactor/Runtime/InteractionRequestCoordinator.cs:109–143` | `Cancel` / `Dispose`：原签名；不直接释放仍在执行的CTS，不让一个回调异常阻止其他请求取消 |
| `Refactor/Runtime/InteractionRequestLease.cs:12–81` | 新内部owner：链接token、取消与完成、活动回调计数、一次最终释放；不读取游戏、无全局注册器 |
| `Refactor/Adapters/LegacyChannelInteractionFacade.cs:38–47` | `GenerateAsync`：三渠道现有共用facade转入真实coordinator；原消费者不改签名 |
| `tools/InteractionRequestLifetimeTests/Program.cs` | main共同用例/五类旧红/当前51项、重入与受控竞争；真实协调器，pipeline为确定性测试替身 |
| `tools/InteractionRequestLifetimeTests/verify_compat.py` | 先用main库编译旧消费者，再换新库执行同一份消费者IL；比较原公开签名 |
| `tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` | 实际pipeline/三渠道提交测试加入真实lease源码，不用假lease |
| `tools/CourierPostprocessOwnerRegressionTests/run.py` | Courier真实后处理组件的源码依赖同步加入lease |

旧的CTS字典和Cancel/Dispose直接释放已从coordinator移除，没有保留并行实现。原公开类型、构造函数、Execute/Cancel/Dispose是兼容入口，不能当死代码删掉。[96点代码地图](../architecture/af-framework-code-map.json)同步本候选。

## 3. 已验证

- main普通30项、新51项；五类main缺陷旧红；三类故障注入成功编译并被行为断言拒绝。
- main编译消费者的public构造/Execute/Cancel/Dispose签名相等；消费者不重编译，替换新核心测试程序集后Native/Scene/Courier调用通过。
- 原pipeline40、Native提交失败4、提交边界69、回执39、async owner18、匿名prompt13全部通过。
- Courier后处理39；制作组内部13签名/31调用点/308断言及3故障反例通过。
- 原API119/256并发、快照32/128并发、3故障、外部internal访问拒绝；实际4DLL元数据584项通过。
- 最终源码Debug/Release×1.3/1.4/Bootstrap六项Stage通过，只写项目目录。
- 与main存档身份对照：146个SyncData键、36个CampaignBehavior，无新增/移除；Bootstrap模块身份保持。

审计记录：[源码、产物、日志哈希](../audits/2026-09-15-main-closeout-lifetime-verification.json)。首次测试夹具有lambda `_`变量与discard冲突的编译错误，修正后才取得旧红/新绿证据；原错误日志保留。pipeline运行有NU1900（离线无法取得NuGet漏洞数据），不能声称做过包漏洞审计。

## 4. 兼容与玩家视角边界

- 本次旧客户端验证是 **main源码构建的核心测试程序集ABI**，不是实际游戏加载独立子MOD；不要把它写成“三渠道public SDK已完成”。
- 取消是协作请求，不强制杀HTTP，不回滚已开始游戏动作；卡死的外部回调仍可能阻塞主动取消者。一个回调抛异常与一个回调永不返回是两类问题，本轮只隔离前者及释放竞态。
- 日志诊断只记取消异常类型，不泄漏请求正文/令牌；日志监听器异常不能打断清理。
- 新lease是每请求状态，没有第二份游戏owner或全局队列；新增少量对象/锁用于资源生命期，本轮未做游戏帧时间测量。
- 玩家角度：新输入替换旧请求、取消后不补交、三渠道共同退出及取消前不发请求已在离线模型执行；真实窗口/场景/送信/资产/AFEF没有游戏内实测。

## 5. 后续不能遗漏

1. **外部三渠道SDK：明确未完成。** 当前Api.V1仍只读；要交付Native/Scene/Courier真实提交/结果/取消并复用同一主体链，不只改能力标志。
2. 内部接口除了已有“AF调用制作组”的贡献ports，还要收口制作组调用主体的服务边界；稳定性包含语义/线程/owner/失败，不只是签名。
3. 完整Campaign/Mission生命周期仍需从真实新档/读档/结束/重进/晚回包路径做；这次lease不等于统一Host已完成。
4. Courier准备仍有后台persona/history/preprocess/live读取；不能简单把整段移主线程导致同步网络阻塞。双向准备必须单独闭环。
5. B1首次深复制/深来源、完整writer和真实分帧预算未完成；C1修复不放行B1。
6. 按矩阵继续主体Prompt/LLM/标签/历史/展示/工具职责迁移、完整main功能对照和符号删除/保留表；最后以同候选实机/旧存档/live Economy/AFEF验收结项。

回滚：对 `73774a94` 做受审查逆向提交，基线 `c8c074c8` / `f03557fb`；不hard-reset或改历史。两份草稿和指定本地Native简明版保持原哈希。简明版仅保存于本地 `.tmp/main-closeout-20260915/team-handoff.md`。
