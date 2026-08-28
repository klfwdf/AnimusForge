# 敌对城堡原版会面直接开放 HANDOFF

## 结论

- 本版本不再提供 MCM 实验开关，敌对城堡城门处的原版“请求与某人会面”按安全条件直接开放。
- 这不是无条件放行：原版模型本来允许、目标确实为敌对城堡、当前存在有效聚落遭遇，且 AF 原版会面 guard 成功武装时才会开放。
- 任一判断或运行时调用失败，继续显示“AnimusForge 已禁用城堡中的请求会面”，不会带病进入会面。

## 原因与历史

- `LordEncounterBehavior` 中的敌对聚落原版会面 guard 在历史提交 `3355dfb4` 中加入。
- `AnimusForgeSettlementAccessModel` 在后续提交 `cd43fe25` 中对所有城堡会面做了全局禁用。
- 这说明当时的做法是“先保留 guard，再把城堡入口整体熔断”。本次不删除熔断模型，而是只在安全条件全部通过时不执行禁用。

## 当前作用域

### 会改变

- 玩家已进入敌对城堡的 `castle_guard` / 聚落遭遇现场。
- 原版 `SettlementAccessModel.IsRequestMeetingOptionAvailable(...)` 返回可用。
- AF 能确认当前是敌对聚落原版会面上下文。

### 不会改变

- 敌对城镇：原有行为保持不变。
- 友方或中立城堡：仍按 AF 原有策略禁用。
- 大地图远程 Parley：仍保持原状。
- 普通大地图领主遭遇和 AF 自定义会面：不改目标解析、不调用 `SetTarget`。
- 原版因无可见领主、访问限制等原因禁用时：AF 不覆盖原版禁用原因。

## 代码点

- `AnimusForgeSettlementAccessModel.cs`
  - `HostileCastleNativeMeetingEnabled = true`：代码级发布开关，非 MCM 设置。
  - `CanSafelyEnableHostileCastleRequestMeeting(...)`：敌对与 guard 检查，失败即关闭入口。
  - `IsHostileCastleForMainHero(...)`：只允许 `settlement.IsCastle` 且聚落派系正与玩家派系交战。
- `LordEncounterBehavior.cs`
  - 会话启动时使用现有 native settlement meeting guard。
  - `OnSessionLaunched` 和 `OnMissionEnded` 清理可能残留的 guard 上下文。
- `DuelSettings.cs`
  - 已删除 `EnableHostileCastleNativeMeeting` MCM 属性和设置访问器。
  - 旧 MCM JSON 中如果已有同名字段，新版本不再读取，对行为没有影响。

## 紧急关闭新版本

如果实机出现严重 BUG，不需要恢复 MCM，只需将下列代码级开关改为 `false`，重新编译并发布紧急版：

```csharp
private static readonly bool HostileCastleNativeMeetingEnabled = false;
```

该改动会立即恢复“所有城堡请求会面都禁用”的旧行为，不需要删除 guard、Harmony patch 或会面目标解析代码。

## 发布前实机测试

1. 敌对城堡：城门菜单中按钮可用，能正常打开 NPC 列表。
2. 多领主城堡：选 A 实际会面 A，返回后选 B 实际会面 B。
3. 选择“算了”：能回到 `castle_guard`，不进入 AF 自定义领主菜单。
4. 正常结束会面：任务退出不黑屏、不卡死、不残留会面 guard。
5. 会面后立即接触野外领主：仍正常进入 AF 自定义遭遇菜单。
6. 敌对城镇：行为与当前正式版一致。
7. 友方城堡、无领主城堡、被围攻城堡、围攻方会面、慢加载会面分别回归。
8. Bannerlord 1.3.x 和 1.4.x 各测一次完整进入与退出流程。

## 建议观测日志

- `SettlementAccess`
  - `Hostile castle request meeting failed closed...`
  - `Disabled castle request meeting option...`
- `LordEncounter`
  - `Native hostile settlement request meeting detected...`
  - `Native hostile settlement request meeting guard cleared...`

如果收到 BUG，至少收集：游戏版本、城堡 ID/名称、玩家所属派系、城堡派系、当前菜单 ID、选中 NPC、`Mod_Logic.txt` 中上述两类日志。

## 构建与发布状态

- 必须使用 `一键编译覆盖推送/build_single_module.ps1` 同时构建 1.3、1.4 和 Bootstrap。
- 会面直接开放增量已在上一个通过统一构建的会面基线 `cef30f2b` 上做隔离验证：
  - 1.3 实现：引用版本 `v1.3.15.110062`，0 警告、0 错误，SHA-256 `3B5A2A9E4B7B7D8FEBA8CCDF75771327B9CC68BEAB691FD00884D2289EA92FE4`。
  - 1.4 实现：引用版本 `v1.4.6.115628`，0 警告、0 错误，SHA-256 `1D364357CB4263CC7493665948DED938E1BF5BB511F207761184B841DC64D506`。
  - Bootstrap：0 警告、0 错误，SHA-256 `B6252ECDE377CCB75BA38D5A3E06807931539CBC278E1CC99122655A3C72202B`。
- SETS 配套方法随后已补齐。干净集成快照 `5abeb7fe` 也已通过统一构建：
  - 1.3 实现：0 警告、0 错误，SHA-256 `8F83730868057CEA44DDA3C19029FEABC33B687FEC6AC3BBA75E1B818577574D`。
  - 1.4 实现：0 警告、0 错误，SHA-256 `65DB4A02B4195DA3B1FFBCD8DF5F47DFDFF59315ABBDD20E495218E4BE5D3834`。
  - Bootstrap：0 警告、0 错误，SHA-256 `F0BE2BE66294D63DE2AAE04F50860699ACF62684359194EEE3EB9C63767FADDB`。
- 制作组如果在 `5abeb7fe` 之后再合并其他运行时改动，打包前仍必须对最终发布提交重跑一次统一构建。
- 本次代码修改不自动覆盖游戏模块；覆盖前仍需确认游戏进程已退出并备份 DLL/PDB/ModuleData。
