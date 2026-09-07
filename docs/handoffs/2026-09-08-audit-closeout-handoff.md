# AF 重构检测收尾 HANDOFF（2026-09-08）

## 当前结论

**检测与资料归档已收尾，缺陷修复和阶段八交付尚未完成。** 用户最新要求关闭自动化、全面检测、做结尾工作。本轮不将收尾理解为自动修复、推送或发布授权。

- 自动化 `af-7-8`、旧 `af` 均 **PAUSED**，不恢复。
- 唯一工作区：`G:\AFMOD\AF-REFACTOR`。
- 本地分支：`codex/af-main-refactor-continuation-20260831`；既有共享远端：`origin/refactor/prepare-af-restructure`。
- 已检测生产源码：`35524b043c5daabca6e51a02caf084131e87e16e`。
- 完整检测报告提交：`fd5f0faa724b0d01c299c64b38b209fc63379568`，只改文档，不能当成另一次生产版本验收。
- 本次仅增加收尾 HANDOFF、制作组短文和证据索引，更新台账入口；不改生产代码、正式测试、构建脚本、配置、游戏或存档。未推送。

## 接手先读

1. `G:\AFMOD\AF-REFACTOR\AGENTS.md`。
2. `G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-08-full-refactor-audit-35524b04.md`：问题、调用链、真实验证边界与未验证事项。
3. `G:\AFMOD\AF-REFACTOR\docs\phase8\refactor-execution-plan-20260908.md`：原功能清单；自动化段落属于历史，暂停状态以本 HANDOFF 与最新台账为准。
4. `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-08-scene-postprocess-milestone-handoff.md`：已完成 Scene 修复的范围，不覆盖后来发现的剩余问题。

## 未修复问题清单

| ID | 级别 | 接手要修什么 |
|---|---|---|
| F1 | P1 | Hero 的指定物品 `ALL` 未按资产筛选，会一起转移其他库存 |
| F2 | P1 | Courier 匿名消息被字典适配器丢弃，正文只剩通用续写提示 |
| F3 | P1 | Courier 丢掉完整后处理 completion，未授权领域标签可进入 ActionPlan |
| F4 | P1 | 玩家输入等待 gate 后未验证原 session，旧输入可继续影响新场景 |
| F5 | P2 | BattleSpeech 普通回退丢 frozen 框选范围/目标 |
| F6 | P2 | TTS 取消缺少请求身份，旧 job/失败事件会干扰新轮等待 |

另有：mandatory safety 与 optional Bridge 清单矛盾；渠道默认状态目录滞后；双 waiter 方法级状态丢失风险；Courier 后台游戏读取/重复准备；错误文本当成功正文风险；不可达旧群组分支待审查清理。精确位置和证明见完整报告，不在本短交接复制全部代码分析。

**F1–F6 均未在本次收尾修复。** 隔离 witness 的 exit 0 是坏状态成功重现，不是修复通过，也不是实机已出故障的证明。

## 已做验证、不能扩张的结论

- 官方 Debug/Release × Bannerlord 1.3/1.4/Bootstrap 六项构建通过；源码与产物指纹已复核。本次收尾未重复编译不变源码。
- C# 共 41 个测试项目：37 个完整 runner 执行，34 PASS、3 FAIL；1 个只执行安全子集，3 个缺 SDK 10。
- Python 27 个命令：24 PASS、2 FAIL（同一 Bridge 清单原因）、1 个按预期 BLOCKED/exit 2。
- 3 个 C# FAIL 分别涉及旧源码位置断言、旧 UI 顶距断言和外交 schema/测试合同冲突；保留原 FAIL，未改断言换绿灯。失败点之后未执行的内容不可当作通过。
- PolicyEffect 四个安全 only-flags 通过，默认全套/ONNX未运行。
- 真实 Campaign/Mission、旧档、live Economy/AFEF、实际语音/口型、第三方模组及部署/回滚均未验收。当前不能发布、广泛删旧或标阶段八 DONE。

## 归档证据

- 证据包：`G:\AFMOD\AF-REFACTOR\artifacts\handoffs\af-refactor-audit-35524b04-20260908-evidence.zip`。
- SHA-256：`e75aad3ed57c4b2a704bb922c47540d3cabb9f24a02217d5aad75c335e5b3b4f`。
- 大小：354,097 字节；116 项 ZIP entry，其中 115 个内容文件逐项校验 SHA-256，另 1 项为清单本身。
- 仓库中的索引：`G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-08-audit-evidence-manifest.json`；ZIP 内另有 `AUDIT-README.md` 与同一份 `MANIFEST.json`。
- 原证据：`G:\AFMOD\AF-REFACTOR\.tmp\full-audit-35524b04`，完整保留，没有清除原 FAIL、retry、探针源码或 provenance。
- ZIP 只在本地忽略的 artifacts 中，不是本轮 Git 提交里的二进制，也未上传。已排除 bin/obj/temp/cli、DLL/PDB/EXE、游戏资源、玩家配置和存档。
- ZIP 是检测证据，不是安装包。部分探针仍依赖记录时的本机路径、源码、SDK 和项目内 Stage；其他机器需按 README 在独立副本适配，不能宣传解压即跑。
- `PASS_AFTER_RESTORE` 的原记录仍保留第一次 exitCode=1，最终通过看 `retryLog`；缺失证据示例的预期 exit 2 不代表发布 PASS。

## 协作与回滚

两份用户旧草稿保持原样、未暂存：

- `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-06-integrated-phase8-handoff.md`
- `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-06-team-brief.md`

因此工作树不是全 clean；不能为了收尾重置或覆盖草稿。此前其他作者的遭遇安全提交也保留。回滚只做审查后的定向逆提交，禁止 hard-reset、重写历史、force push 或顺手清除证据。

## 后续启动语

> 请先读取 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-08-audit-closeout-handoff.md` 与完整检测报告。先核对当前 HEAD/工作树/远端，保护两份旧草稿及其他作者修改，不恢复自动化。当前源码验收基线是 `35524b04`，6 个功能问题仍未修复；确认当前用户已经授权修复后，先做 F1–F3 的资产范围和 Courier 完整链路闭环，再处理 F4–F6 的请求生命周期、框选回退与 TTS。保留已复现的坏状态作红测，修改后统一回归。单独核对 Bridge/目录与过时测试，不能通过恢复不安全 gate、弱化断言或删除仍有功能责任的旧接口换取全绿。推送、部署、实机存档操作、自动化恢复和广泛删旧须有明确授权。
