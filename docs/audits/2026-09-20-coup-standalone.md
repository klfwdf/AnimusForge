# 宣权篡位独立子模组交付（2026-09-20）

当前阶段：总结。用户最终要求“先做成子模组部署到游戏，后面再考虑整合”。本轮按此交付独立 `AnimusForge_Coup`，未将玩法编入 AF 主 DLL，未推送。

## 源码与职责

真实工作区 `F:/AnimusForge-main`，分支 `codex/af-main-refactor-continuation-20260831`。初始意图提交 `e081202`；独立交付意图 `2bca39a6`；生产代码与测试提交 **`ce89346cf88de387eb5cb199b16225a5b99fed0c`**。期间其他任务的 Illustrator 提交和未提交改动均保留。本轮对 AF 主体的临时接线已通过定向逆补丁撤销。

以下坐标已按生产提交核实；前缀均为 `extensions/AnimusForge.Coup/src/`：

| 路径与一基行号 | 符号 | 已覆盖职责 |
|---|---|---|
| `CoupSystem/CoupCampaignBehavior.cs:20–528` | `CoupCampaignBehavior`；`OnEngineTick` 314、`CommitVictory` 401、`CommitFailure` 456、`CommitCasualties` 493 | 城镇菜单、身份/特殊任务门禁、选兵、两阶段推进、政治结算、单兵账本、退出与读档隔离 |
| `CoupSystem/CoupMissionBehavior.cs:24–556` | `CoupMissionBehavior`；`InitializeCombat` 133、`OnAgentRemoved` 405、`OnEndMission` 481；`CoupAgentOrigin` 519 | 专用队伍、批量生兵、有限增援、门口互动、擒王、连续血量、原版名册写回隔离 |
| `CoupSystem/CoupSceneBridge.cs:10–75` | `TryValidateScene`、`TryOpenStage` | 原版 center/lordshall 资源、通道和场景入口 |
| `CoupSystem/CoupSession.cs:12–108` | `CoupTroopRecord.TryRecordCasualty`、`CoupSession.TryAdvance/IsValid` | 稳定标识、阶段及副作用收据、非法/重复胜利与伤亡拒绝 |
| `CoupSystem/CoupGuards.cs:13–260` | `Register`、`HasBlockingHostFlow` | 原版失败/犯罪、AF 普通 SETS/挑衅/GCCZ/押俘/闲聊/人口及延迟场景移动的政变专属隔离 |
| `CoupSystem/CoupCaptivityBehavior.cs:17–156` | `RegisterDetention`、`RegisterPatches` | 旧王拘押存档、精确自动同阵营释放例外、转交/释放/死亡清理 |
| `Integration/SettlementEntryTroopSelectionBehavior.cs:16–225` | `Register`、选兵/编队/生成/命令桥 | 复用现装 AF 的真实私有选兵/编队与命令接口，启动时缓存接缝 |
| `Integration/CoupRebellionBridge.cs:21–499` | `Initialize`、`TryQueueCoupRebellion`、`TryRecordCoupOutcome` | 独立持久收据；调用 AF 原资格、命名算法、建国执行、AFEF、行动和周报 owner |
| `SubModule.cs:11–49`；`CoupSettings.cs:7–19` | `SubModule`、`CoupSettings` | 独立加载、行为注册、实时 tick、MCM 开关 |

临时兼容桥使用现装 AF 的历史反射接缝，不扩展/冒充公共 API V1。缺失成员或签名不匹配关闭发动入口。独立命名队列只管理事件身份、等待和重试；Prompt 构建、LLM/provider 算法及政治动作仍由 AF 原 owner 执行。无候选、叛乱关闭、玩家免疫均结束本次判定，不强造旧王党。

性能：没有政变/叛乱待办时 tick 为常数时间直接退出；准备阶段一次构造兵源；10 人/0.15 秒批次；目标及活跃单位血量每秒检查；街道双方各最多 60；门卫最多 10、大厅己方/护卫各最多 20。反射方法在启动缓存，跨场景只存值和稳定 ID。

## 已验证

- 独立项目 `BannerlordApi=1.3`、`1.4` Release 均 **0 警告 / 0 错误**。1.3 使用完整固定引用包 `1.3.15.110062`；1.4 使用实际游戏 **v1.4.8** DLL。没有重编或部署 AF 主体。
- `tools/Coup.ContractTests`：**32 PASS**，覆盖阶段越权、重复结束/死伤、存档往返、部分提交、兵力上限、坏状态及无法伪造胜利。
- 现装 AF 两版反射接缝完整签名/字段核对通过；当前 1.4 实际程序集离线 Harmony 注册检查通过。
- **对已部署的同一 DLL 再次运行注册探针**：4 个可用标志全部 true，**44 个目标、42 个 prefix、2 个 transpiler**，包括真正生成的俘虏自动释放 IL。没有创建 Game/Campaign/Mission，没有发起 LLM。探针只额外将 AF 日志目录重定向到工作区，原 AF 与子模组 DLL 哈希均未变。
- 部署 4 个文件逐个哈希一致；原 AF 两份 DLL 哈希保持不变。`git diff --check` 通过。

本机证据：`artifacts/coup-standalone-build.log`、`artifacts/coup-standalone-deploy.log`、`artifacts/coup-runtime-probe/deployed-1.4/registration.log`、`extensions/AnimusForge.Coup/artifacts/deployment.json`。源码内的 `tools/Coup.RuntimeProbe` 可复跑真实注册检查。

## 部署与启用

2026-09-20 **07:23:55（UTC+8）** 已按用户授权部署到：

`F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge_Coup`

模块 ID `AnimusForge_Coup`；启动器名称 **AnimusForge - 宣权篡位**。启动器勾选它并排在 AnimusForge 后面，MCM 中有独立开关。没有改动启动器默认勾选配置，没有启动游戏。

- 部署 DLL SHA-256：`9353774FE1FB0A3D749094A5C7358CCB422F74E55E1E0789E05967AE9C7203D1`
- 部署 DLL MVID：`a253f962-7beb-4054-b17f-a98a3f4a6c48`
- AF 1.3 SHA-256：`E60DBBED7652D74E0A14E6A51725776DCB988667F857956AFD40232CB7ED36B4`
- AF 1.4 SHA-256：`2C3DD9E54561EC2498906C25EE336991D88559152E94452CCF1B4008CD60A49B`

## 尚未验证与回滚

**未实机验收**：城镇菜单实际显示；不同文化/城墙等级的门口、王座、导航；编队 UI；60 人街战性能；完整两阶段胜败；旧王拘押与释放；真实存读档；AI 内战与记忆/周报呈现；1.3 游戏实际运行。离线注册成功不代表这些已通过。

下一步实际验收：普通新存档进入国王所在本国城镇，从少量兵开始验证街道→大厅→释放旧王的全链，再验证扣押、失败撤退、免疫/无候选及普通 SETS/GCCZ 不受影响；最后扩大到 60/20 人和其他文化场景。

部署回滚目录 `F:/AnimusForge-main/artifacts/deploy-backups/AnimusForge_Coup/20260920-072355`。这是首次安装，目录包含 `NEW_MODULE.txt`，**没有虚构的旧版 DLL**。尚未开始事件时取消勾选或移除新增模块即可撤销安装；进行中的事件先收尾。已经改变的王权、领地和兵损需用发动前存档恢复，不能靠卸载 DLL 撤销。

源码回滚仅针对 `ce89346c` 作定向逆提交，保留其他任务和用户改动。AF 整合不在本轮最终交付范围内。
