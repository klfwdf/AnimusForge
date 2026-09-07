# AF Scene 主请求保真交接（2026-09-08）

## 本轮状态

- 基线 `92ad625a`，意图提交 `8c30424e`；本交接与生产修复同批本地提交。
- 工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。fetch后共享重构远端仍aefa02ad，没有新提交需要合并。
- 自动化 `af-7-8` 每30分钟继续本任务。只在有实质变化/失败/需决策时通知，旧 `af` 保持暂停。
- 本项 `P0-SCENE-MAIN-PARITY` 为主请求交付离线VERIFY，不是全Scene等价或阶段八DONE。两份旧本地占位草稿未修改/暂存。没有推送、部署、游戏/存档或跨工作区写入。

## 真实缺陷与修复

完整Scene流程先经BuildStrictSceneMessagesForNpc组装人设、规则、自定义Prompt、信任、历史role、当前与过去AFEF等消息，并消费当前事实队列。随后default detached入口再CaptureSceneShoutRefactorEnvelopeForExternal，旧main composer从第二份sections/history重组请求，没有使用完整messages：当前事实、场景角色标记等可能丢失，玩家输入又被额外追加，main token预算从原5000变为4096。

现在在同一默认入口将已经准备好的messages通过现有LegacyConfiguredChatGateway.BuildPromptPackage冻结为只读PromptPackage（5000token），private Scene factory的main delegate使用该包；后处理/解析/能力/default设置保持原状。原public CreateSceneShoutDetachedPortsForExternal(tags,int maxActions=64)的ABI和旧opt-in行为保留，委托同一private factory，不复制一套解析/规则实现。

必须注意实际消息形状：CreateChatMessage返回匿名 `{ role, content }`，不是Dictionary。LegacyPromptPackageAdapter.FromLegacyMessages不识别这种匿名形状，直接使用会丢消息；本轮复用已有支持匿名对象的Configured converter，没有修改全局消息创建方式或另写反射转换器。

每轮仅做一次O(messages)冻结转换，Json.NET沿用其类型元数据缓存；包只含不可变消息/字符串，不持有游戏对象或可变列表，没有静态请求缓存或新增Tick扫描。公开无prepared调用仍使用原composer。后处理还依赖旧recapture，重复前处理暂未去掉，不能宣称全面性能优化完成。

## 验证

|项目|结果|
|---|---|
|扩展同一外层harness|原44项保持；新增8项主请求/ABI/阶段隔离用例|
|92ad625a红测|46 PASS/6 FAIL，已准备消息丢失/4096token/追加输入等反例|
|本轮绿测|52 PASS/0 FAIL，主任务再次独立运行通过|
|源码抽取自测|7 PASS|
|官方Debug/Release × 1.3/1.4/Bootstrap|6项0 warning/0 error，project-local Stage成功|
|InteractionPipeline/Host/receipt/native|40+69+39+4 PASS|
|生产1.4 Detached/Configured Host|PASS（Configured含12提交后禁止回退、6receipt场景）|
|ProductionOptInEntry|PASS，含结构/身份/无Campaign状态回放；不是默认实机证明|
|Persistence/Profile|142 keys/168 bindings/43 symbolic sources PASS；本轮未改fixture|
|Bridge validator|16 total/13 wired/3 declared-only PASS|
|独立源码审查、diff/定向残留检查|无本轮新增阻断，PASS|

Interaction runner的NU1900是上轮同一NuGet漏洞元数据服务不可达警告，退出0；没有关闭审计或改源掩盖。六项官方构建本身无警告。

### 测试的真实边界

harness抽取并编译实际Scene连续调用块、public/private factory、CreateChatMessage、BuildPromptPackage、ports、不可变契约、main/postprocess composers与action parser。用已有准备好的完整messages作为入口fixture；Host结果、网络、队列、游戏状态仍为stub。测试覆盖匿名消息、中文/空白/换行/role顺序、事实/信任、5000token、不追加第二份snapshot输入、原列表/字典修改后包不变、两个speaker包独立、原公开签名及postprocess仍用自己的sections。

这不证明BuildStrictSceneMessagesForNpc本身完整、不证明真实API/金币/场景/旧档，也不证明后处理等价。后处理保留测试只证明本轮未误用main包替换它。ProductionOptInEntry末行历史`noDefaultCutover=1`是结构测试标签，不作为当前三渠道default状态证据。

## 重放与产物

沿用上一份cutover-terminal-safety HANDOFF中的官方构建命令与本机路径；依然仅使用-Stage，禁止-Deploy。固定引用：1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`。

```powershell
python -B tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --source-ref 92ad625a --output-name prompt-red-92ad625a
# 红测预期退出1；下面绿测必须退出0。
python -B tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --output-name prompt-green-current
python -B tools/ChannelCutoverBoundaryTests/test_extraction.py
```

harness现在使用真实Json.NET转换方法，需要现有Newtonsoft.Json.dll。默认项目`.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll`，可用`--newtonsoft`显式覆盖；不自动下载依赖、不使用游戏DLL。

项目本轮日志：`.tmp/scene-main-20260908/`中的build-Debug、build-Release、interaction-contract、ProductionDetachedHostReplayTests、ProductionConfiguredHostReplayTests、production-opt-in、outer-main-final、两个Python validator日志。红绿详细case与抽取源码指纹位于`.tmp/channel-cutover-boundary/prompt-red-92ad625a/`、`prompt-green-current/`、`scene-main-root-verification/`。

ProductionOptIn依赖使用既有ReplayDependencies，显式参数与以前相同：GameRoot=`E:\steam\steamapps\common\Mount & Blade II Bannerlord`，Bannerlord14ReferencePath=`G:\AFMOD\NEW-10\.tmp\build_check\1.4`，ReplayHarmony/Mcm/UiExtenderModulePath分别指向Workshop 2859188632/2859238197/2859222409；ReplayPrivateRuntimePath=`G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client`。不改变测试TFM或弱化断言。

|配置|Bootstrap SHA256|1.3 SHA256|1.4 SHA256|
|---|---|---|---|
|Debug|DB429196F9FC2351E61F6FE1D53ABE345401C6E02FA7512DF91618C5BE0E6E7E|1BA5200F266DB85784B03E8BF64200E1407B6FE2818125DCDD37FC49C85B9AFD|D063614ACF775B4F7293C03F6C6B69BFD982DB4D2E1F55FA949E06EC90223F3C|
|Release|9215B72A3343381FEABCBE1E2EA7A5C7A4688A5362980DB44282A67718299246|C54FCFDA7F8075782C001FABFECA36B98F10FD7A5EB391445E1A58A9D858E880|02B906038983ECB020D4078F05CB106934F537F0B54D2110F471733EFDFFF5FF|

## 明确剩余缺口与下一精确任务

`P0-SCENE-POST-PREP`：

1. 旧权威链是生成回复后计算firstTurn、结束/战斗抑制、GCCZ direct-command、实时relay候选，再QueueDeferredScenePostprocessActions，然后TryRunSceneUnifiedActionPostprocess。新detached在此前已commit并禁用旧queue；这不等价。
2. detached当前将knowledgeExtras+主ruleBlock当tag_rules，缺真实PostprocessRules/动态资产债务候选；解析缺旧逐领域Normalize；executor把replyIsDirectPlayerResponse固定true、NPC回复为空。补静态Prompt不能解决这些问题。
3. 先在原Queue调用时机下，将TryRunSceneUnifiedActionPostprocess原实现分离为prepare（规则/资格/候选与完整system/user）、network（字符串）、normalize/complete（原逐领域归一化）；不复制第二套业务规则、不改变旧入口行为。用捕获请求与固定响应比较旧/新归一化结果。
4. Scene候选必须保留duel/kingdom/mechanism、summon/guide、entity context、preprocess hits、实际reply/firstTurn、relay candidates/primary/single-framed等；不能直接用缺Scene relay且传空移动候选的Courier builder替代。
5. 后续接入detached前必须解决生成后的准备时机、解析及commit上下文；未达到等价前不删除旧queue/facade、不切Native默认。若需要新的默认调整或广泛删除，提交具体方案待用户确认。

另记录P1诊断问题：LegacyNativePromptParity.CompareMainMessages调用只识别字典的FromLegacyMessages，而实际Native也使用匿名CreateChatMessage。现有字典fixture的PASS不能作为真实Native请求等价证明；Native迁移前需修正该诊断的实际消息形状支持并补实测样式回归。本轮没有改Native默认/诊断。

回滚采用定向逆提交；基线92ad625a、意图8c30424e。旧public opt-in、旧Scene后处理和存档兼容仍保留，原因是实际调用与尚未解决的等价责任，不按Legacy名称删除。自动化继续上述项目内准备；实机LIVE/SAVE待独立授权与验收。
