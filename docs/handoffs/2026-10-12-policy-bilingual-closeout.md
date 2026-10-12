# Policy 双语化接手修复与全仓验收接续（2026-10-12）

阶段：总结。用户明确确认 Policy 外部写入停止后，本轮已接手修复。产品代码、资源和两份实际 DLL 已冻结；源码/DLL 发行门禁、双版本构建与已列离线契约均通过。实机、旧档、全量语义与正式双包仍未验证，不能宣称全模组实机 100% 完成。

本条接续并取代[上一轮报告](2026-10-12-full-bilingual-audit.md)中的 `POLICY_BLOCKED` 判断，上一轮证据保留。工作区 `F:/AnimusForge-main` / `main` / HEAD `a911d4d0de8964a393b6bdc6eaa83705b5218b02`；本轮 intent `7c045c5fcfd0afe20fc474c61e866be3119a38ac`。当前工作树包含其他作者交付，HEAD 不能代表被验的全部源码。每文件 SHA、实际命令、成功和失败日志位于 `artifacts/policy-bilingual-closeout-20261012/`。

## 实际修复

| 缺陷/边界 | 最终处理与证据 |
| --- | --- |
| Policy 542 个解码后运行汉字位置，35 个文件 | Core 194、Npc 182、根剩余 166 按 UI / 包 Prompt / 兼容原值分类接线；完整原值与迁移逆恢复记录在 `agent-core/`、`agent-npc/`、`root-literal-migration.json`。不是只把中文藏入 Unicode 转义 |
| PR01 地方历史 Source / Mentioned 目标直接显示 raw | 两消费者使用 typed UI 分支；标签随 UI，实际目标名与明确数量由生产数据组装。旧 mapper 内部不再把名字“发布地”翻成标签，或截断“Name，另2处suffix”，也不拆名字中的“、” |
| PR02 目标目录 Description 进入 EN 请求 | 序列化强制携带 request，保 semantic target 同形名称；只投影 selector / plan 固定壳与 63 类完整诊断。生成资格、目标、字段与存档身份不变 |
| PR03 英文治理知识被丢、纯地理知识误留 | 增补英文词边界及缓存词形；42 治理 / 19 地理原中文词表、权重、资格、外国锚及预算保留；`landform` 不误命中 `land` |
| PR04 legacy string route 仍发送中文政策 user 消息 | 使用固定包 Misc Prompt，保原政策任务、JSON-only、system / roles / 序列化与普通 JArray 分支 |
| 知识压缩删除 a / I / 所有格 s / 单字母正文 | 真实知识生产者无此编号协议，移除全句删单字母清洗；原句、数字、未知编号和插入值保留 |
| 政策日期只有中文，展示值可能写回发布/草拟 | 新日期投影仅识别完整 canonical 日期；地方、全国/附庸两 VM 单独持有 `_rawDateText`，UI 随界面，Prompt 随包，发布/AutoDraft 保原。任意原始日期不猜译 |
| 待审政策 AFEF 与周报固定中文进入 EN Prompt | 记忆模板保“待审、尚未通过、没有数值效果/事件/民众反馈”；周报仅在 Full / Short 脱离存档的请求副本投影。已知包装、真实 420 字符截断投影，未知/重复/不完整保原 |
| 216 个英文 UI 说明缺规则/错组/截句 | 处刑拒绝理由、绘图尺寸/默认超时/长度上限、MCM 知识与压缩条件、终端 Hint / Group 定点修复；537 个 Hint / Group 职责行审校。217 个 EN ID 含根额外 MCM Token 文案修复，另 1 个 CN ID 有意修显示错误 |
| 完整 XML 仍被英文 fallback 覆盖 | Registry 209 与 helper 257 个 literal 同步，共 466；根另同步 7 个 MCM / Illustrator 属性 fallback。ID、配置默认值、委托、权限、路由不变 |
| 原生 TextObject 吞掉文档中的 `{token}` | 终端仅已知 Token Hint 绑定 literal sentinel 后一次恢复；MCM 两语言示例改为 `Bearer;` 后接 Token。HTTP 请求头实现未改；真实原生失败与负控保留 |
| 新 reader 未入发行接线 | `content-map.json` 增加 Npc Compatibility、Core Management Compatibility、Core Management Prompt 三个标准路径；3502 条映射。5 个新 C# 文本 helper 均按原请求/显示频率运行，语料/正则/词形一次缓存，无新增 Tick I/O、游戏全量扫描或锁轮询 |

## 冻结后的真实验证

| 验证 | 实际结果 / 可复现证据 |
| --- | --- |
| 原统一 Release 构建，1.3.15 / 1.4.6，Bootstrap、双 Coup gate | 两实现各 363 警告 / 0 错误，Bootstrap 0 / 0；8897 输入 SHA 零漂移。`build-unified-final-r4.json` / `.log` |
| 警告分类 | 每 API CS8632 343、CS0649 15、CS0105 3、CS0162 2。`build-warning-inventory-final-r4.json`；未屏蔽警告 |
| 原 English Source 与两实际 DLL 门禁 | 全部 PASS，39 shared resource hash 对照。`english-source-and-dll-gates-final-r4.json`；未移除 Policy 或放宽门禁 |
| 全量源码词法 | 1894 文件中只有非产品 StaticVerifier 79 个运行汉字位置；Policy、src、GCCZ 及其他产品范围 0。`runtime-verified-final-r4.json` |
| 旧候选 | 仅 Memory XML 25 个候选；上一轮逐项消费者归因及真缺口修复证据有效，未重新激活 dormant 资源。Policy 31 已按真实消费/兼容身份处理。候选数不是语义漏译数 |
| 原静态契约 44 项 | 44 / 44 PASS，9035 输入 SHA 零漂移。`contracts-final-r4/`。两历史源码 oracle 只添加精确新 hunk / 209 审定字串逆恢复，整文件业务 token 比较仍保留 |
| 真 DLL 政策记忆 / 周报 / 16 模块 descriptor | 双 API × CN/EN，各 4562，合 18248 断言；原生 English→中文→French→English，真实周报生产与 Prepare Full/Short、源材料身份/字段守恒。`policy-consumer-final-r4/summary.json` |
| 真 DLL 日期、BuildMainMessages、两 VM 发布/草拟、13 旧错误、typed 目标标签 | 双 API × CN/EN，各 1726，合 6904 断言，SHA 零漂移。`agent-core/ui-validation/final-dll-07/summary.json` |
| MCM / Illustrator 实际属性 metadata 与原生消费 | 220 断言 / 128 实际渲染，双 API × 双包、EN/CN/French及反切；不 instantiate Settings 或 patch MCM。`agent-core/ui-validation/final-hints-02/` |
| Core 原值 / Prompt 与 typed UI | 原 194 位置完整逆；replay-07 真 TW 1.3/1.4 与 CN/EN；新增 typed UI 每组合 2976 断言，完整 Raw / Prompt 输出相等。旧 freeze 由 `ui-validation/final-product-inputs.json` 显式接续 |
| Npc 真实 reader / 完整消费方法 / 序列化 | CN724 / EN725 与 12 新资源注入断言；原语料守恒与英语过滤/单字母损失负控。`agent-npc/consumer-probe/receipt.json`；受控游戏对象叶，不等于 Campaign |
| 终端真实 constructor / 完整 helper | 双 TW，注册/缺注册/EN/CN/French/反切：16656 正控，10 有效负控，8328 中性旧 EN XML 对照。`agent-npc/final-resource-validation/terminal-consumer-r4/receipt.json` |
| 全资产及接线 | 6652 content JSON/XML ＋ 22 目录外映射文件可解析；39 对 UI / 12741 ID，两 API 各 50964 native 解析；3263 真实资产选择/合同 PASS；静态缺键 0、扫描 SHA 零漂移。`agent-npc/final-resource-validation/r4/` 与 `r4-extra/` |
| 世界书身份 | 6330 JSON、3133 映射，9 组共享目标均 byte-identical，无不同正文损失；4 有效反例。`worldbook-verified-final.json`。不是逐句英文语义验收 |

最终两份实现 SHA：1.3 `5df6e1eb9e3b47f25b5832a664a2e918ff7f07a881d3870ecc1778bb0a2fb749`；1.4 `a9d3c07bf85c54d1950322a8629260ae15bce5a9941b32f99f099385e14fd2e6`。

原政策 `--policy-all-modules-contract-only` 已在两实际 DLL、TW1.3/1.4、默认英文与 NativeCN 四格各通过1491断言（合5964），18模块。保原项目与全部业务源码；薄host只绑定真实依赖/fixture后调用原入口，未跳过产品初始化器。11个精确hunk（4旧UI预期、1独立locale oracle、6完整技术诊断预期）整份测试逆恢复回最早 `80ed56535d5b8330c7a5f7f0f6b5d121d99ffda5afec85501a45ff5904bdd63f`。失败310/508/606/711日志保留，不删解析拒绝、数值、目标、授权、执行或rollback断言；最后收据 `agent-core/ui-validation/final-validation-freeze.json` 与 `final-dll-07/summary.json`，产品SHA零漂移。

## Jev 辅助复核

按用户调用的 [typesafe-ai](C:/Users/29310/.codex/skills/typesafe-ai/SKILL.md) / [aijiau-jev](C:/Users/29310/.codex/skills/aijiau-jev/SKILL.md)，读取[原语](https://docs.typesafe.ai/primitives/choice)、[API](https://docs.typesafe.ai/api)、[证据复核范式](https://docs.typesafe.ai/cookbooks/citation_check)及网关实际 OpenAPI。本轮只发一次 `model=jev` 请求、14 个 Choice（7 修复样本与 7 反例），14 个结论与受限金标一致；俘虏说明低置信经源代码人工复核，不据概率自动验收。

返回 model `jev`，`underlying_version_pinned=false`。网关报告 input5851 / output693，cost0.0035106，币种未声明；不是全仓语义质量、版本复现或账单确认。脱敏结果、金标、低置信人工记录位于 `outputs/jev-reviewed-result-01.json`，请求仅含当前审查文本、无凭据。会话启用状态只来自用户指令，不写全局开关或授予子智能体自动请求权。

## 未验证与回滚

未验证实机 UI 渲染、NPC / 战役 / 生命周期玩法、真实游戏 LLM provider、全部三渠道动作、玩家旧存档或正式 CN/EN 分发包；完整地方历史组装在无 Campaign 的 `GetPlayerKingdom` 处 NRE，16 次尝试保留，typed 生产/History VM 边界不能代替整方法验收。没有逐句人工审校 234588 个 content JSON 字符串叶。上述均不能写成“全模组实机/语义 100% 完成”。

未正式 Stage、打包、部署、推送或镜像工作区外。最早原字节在 `before/`；三方 focused diff / exact inverse 及本轮 focused Git 回滚证据按原文件保存。只回滚本轮 owned 增量，禁止恢复整个 HEAD / hard reset 覆盖其他作者交付。本轮本地分支 `codex/policy-bilingual-closeout-20261012` 的 intent / dirty原字节checkpoint / 验证切片及原119暂存文件保全收据见 `local-slice-commits.json`；正向/逆向补丁见 `forward-focused.diff` / `rollback-focused.diff`，完整owned清单见 `review-slice-inventory.json`。本地主分支、真实索引与暂存diff保持原状；checkpoint不能当完整WIP或可构建快照。
