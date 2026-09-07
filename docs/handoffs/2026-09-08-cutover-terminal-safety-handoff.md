# AF 阶段八接续：外层终态安全修复（2026-09-08）

## 状态与授权

- 唯一工作区：`G:\AFMOD\AF-REFACTOR`；本地分支 `codex/af-main-refactor-continuation-20260831`。
- 已fetch并快进至共享 `origin/refactor/prepare-af-restructure` 的 `aefa02ad15758222b87e4e240a85c52eb3f913d9`，再作计划意图提交 `bdeeadd8`。本文随生产修复同次提交；精确实现SHA可用 `git log -1 -- docs/handoffs/2026-09-08-cutover-terminal-safety-handoff.md` 查询。
- 用户批准自动化开改，复用 `af-7-8` 每30分钟继续本任务；旧 `af` 保持PAUSED。剩余可授权项目内工作完成或只剩外部决策时自动暂停，不重复刷同一状态。
- 本轮为P0-CUTOVER离线VERIFY，不是阶段八全部DONE。Native仍走修复后的完整旧入口。没有推送、部署、游戏/存档操作、默认开关变更或GCCZ跨工作区写入。
- 两份旧本地占位草稿（2026-09-06-integrated-phase8-handoff、2026-09-06-team-brief）不覆盖、不暂存；工作树因此不应声称完全干净。

## 已确认缺陷与修改

### Scene

原外层只处理成功、stale、已回退；Host在提交后返回NonRetryableFailure等终态时，output仍空，继而再次调用旧网络。成功空回复或内部fallback空回复同样触发额外请求。第二份正文可能与第一次动作/记忆错配。

现在以请求是否已交给Host决定外层旧请求资格；Host独占接管后的fallback选择。成功空正文不重复生成；终态/null/提交后异常停止当前轮，用break走已有idle/battle收尾。stale保持直接丢弃，不清理新场景。准备阶段缺少capture或尚未Submit的异常仍可走一次旧生成。

### Courier

原外层终态也会落回GenerateNpcReplyAsync。更隐蔽的是：单独调用旧Fail虽然不重发网络，但其ProcessSession会进入旧动作消费，可能执行残留ReplyPostprocessedText。

现在仅送达后进入detached；送达前保留完整预生成与到达结算，不因DeliveryApplied资格拒绝吞掉正常回信动作。Host接管后终态或异常不重复请求：主线程/世代守卫内先置既有PostprocessConsumed、清空未确认回复与标签，再调用原失败推进。ReplyGenerated guard使重复失败任务幂等，并保护已经完成的合法回信。legacyFallbackStarted保留旧callback已排队的completion，不能因当前ReplyText为空或Dispose异常误清。

不回滚可能已发生的动作，不伪造AFEF成功事实；清理只封闭未确认后续执行。相关玩家提示明确本轮停止且不会自动重试已生效动作。

### 清理与范围

- 删除空回复驱动重请求、Host终态无条件落入旧生成两种旧控制流。
- 复用原生成、状态机、Host与收尾，无新框架、平行解析器、存档key或玩法数值变更。
- 持久化fixture只校正最新远端遗留的3个导航行号：DuelBehavior `_duelCooldowns` 2498→2504；MyBehavior `_patienceStates_v1` 两ref 36463→36467、36472→36476。所有key/ref/type在修正前已确认一致；未修改验证断言。

## 验证结果与证据边界

| 验证 | 本轮结果 |
|---|---|
|真实外层源码抽取、编译、故障注入|基线aefa02ad：19 PASS/25 FAIL；修后44 PASS/0 FAIL|
|抽取器自测|5 tests PASS|
|InteractionPipeline/Host/receipt/native|40+69+39+4 PASS|
|生产1.4 Configured/Detached/Courier Host回放|PASS；Configured含12 postCommitNoFallback、6 requestReceipts|
|官方Debug/Release × 1.3/1.4/Bootstrap|6项各0 warning/0 error，Stage成功|
|Bridge绑定及自测|16 total/13 wired/3 declared-only，20 tests PASS|
|入口目录自测|10 tests PASS|
|Persistence/Profile/Config|142 literal keys/168 typed bindings/43 symbolic sources PASS|
|独立源码审查、diff/冲突清理检查|无本轮阻断，PASS|

外层测试运行生产原始连续block、状态/result及Courier失败/完成方法；Host结果、网络、UI、队列与ProcessSession依赖是确定性stub。测试检查进入状态机前的seal/text与调度顺序，不代替真实到达/返程、金币、旧档或Prompt等价。原有生产DLL Host回放是另一层证据，不证明新增默认入口真实游戏效果。

Interaction runner出现NU1900（NuGet漏洞元数据服务不可达）；测试退出0。没有关闭审计或修改包源来掩盖；官方六项构建本身均0警告。

## 本机重放命令

```powershell
# 旧版红测预期退出1；工作树绿测必须退出0。
python -B tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --source-ref aefa02ad --output-name baseline-aefa02ad
python -B tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --output-name current
python -B tools/ChannelCutoverBoundaryTests/test_extraction.py

$env:DOTNET_ROOT='G:\AFMOD\.dotnet-sdk'
$env:PATH=$env:DOTNET_ROOT+';'+$env:PATH
$env:DOTNET_CLI_HOME='G:\AFMOD\AF-REFACTOR\.tmp\dotnet-cli'
$env:NUGET_PACKAGES='G:\AFMOD\AF-REFACTOR\.tmp\nuget-packages'
dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj
dotnet run --project tools/ProductionConfiguredHostReplayTests/ProductionConfiguredHostReplayTests.csproj
dotnet run --project tools/ProductionDetachedHostReplayTests/ProductionDetachedHostReplayTests.csproj
dotnet run --project tools/ProductionCourierHostReplayTests/ProductionCourierHostReplayTests.csproj
python -B tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py
python -B tools/BridgeBindingContractTests/validate_bridge_bindings.py
python -B tools/BridgeBindingContractTests/test_validate_bridge_bindings.py
python -B tools/PhaseEightReadiness/test_entry_inventory.py

foreach($configuration in @('Debug','Release')) {
  powershell -NoProfile -ExecutionPolicy Bypass -File '.\一键编译覆盖推送\build_single_module.ps1' `
    -ProjectRoot 'G:\AFMOD\AF-REFACTOR' `
    -BannerlordRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
    -Bannerlord13ReferenceDir 'G:\AFMOD\NEW-10\_deps_auto' `
    -Bannerlord14ReferenceDir 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
    -WorkshopContentDir 'E:\steam\steamapps\workshop\content\261550' `
    -RuntimeDependencyDir 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' `
    -Configuration $configuration -Stage
}
```

生产回放须使用本轮重新构建的Debug Stage；不得把旧Stage或其他配置DLL当作新代码通过。SDK需8；官方引用版本1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`。构建不修改官方脚本，不部署游戏。

日志：`G:\AFMOD\AF-REFACTOR\.tmp\cutover-20260908\`（build-Debug/Release、interaction-contract、三个Production...、persistence-final、outer-boundary-final）；红绿故障日志另在 `.tmp/channel-cutover-boundary/{baseline-aefa02ad,current,root-verification}/`。

## Stage SHA256

|配置|Bootstrap|1.3|1.4|
|---|---|---|---|
|Debug|89CD4A21721F0210B1743A9F8895A69B61448830E7DE3CFE2BD267D740F23DC5|8C24F843A800FFC679D668B67D77DDC3C4F2C534D8B95685D005BC7CDA79D5BC|C67915950270C78084CB70F697718054F02E848BB443ADF2E559DF30CD50727F|
|Release|22A7C57767BA7DA631BF7CE6AD60B2B528031D59BFFB5EEEA436D19A9AB9239A|67AF1B275CFD521638FAE55ECB12F1B2A55B895D047840B22ADCAA6122F8A239|FCDEC63F1238A70511AFC6F00A413B5D931AB1950DAAA3D02C23C380BE3F7592|

## 下一动作与玩家验收

下一项 `P0-SCENE-PARITY`：对比HandleGroupResponsePerHeroIndependent已计算的完整上下文/messages/动态PostprocessRules与BuildSceneShoutDetachedPromptSectionsForExternal重新捕获路径，先证明缺失的事实/信任/历史role/规则和重复前处理，再优先复用权威完整上下文。保持Native完整旧默认；不从某几个Host测试通过推导所有领域完成。

玩家侧待测：送达前预生成回信到达后仅结算一次；送达后生成失败/拒绝不重试、不残留标签；失败后信使返程、NPC恢复活动；切场景/读档期间请求完成不污染新状态；成功正文与实际动作/AFEF一致。需用户批准后才部署或操作测试存档。真实游戏LIVE/SAVE仍NOT_RUN。

回滚：基线aefa02ad，意图bdeeadd8；对本轮具体修复提交作逆提交，不hard reset、rebase或force push。自动化继续下一项，遇到仅剩外部证据/权限时暂停并报告。
