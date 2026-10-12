# Wave 2 B WorldMap Display Projection Review Fix

状态：VERIFIED SLICE / FULL EN RELEASE NOT COMPLETE

范围：只修复 Wave 2 B 世界地图玩家可见通知的英文显示投影。保留 `LogFact` 写入的中文 AFEF 事实、命令状态、协议标签、存档字段和内部匹配文本；只在 `DisplayCommandMessage` 的显示边界将已知事实句式投影为本地化文本。

允许修改：

- `src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior*.cs`
- `src/modules/AF.Module.WorldMap/Runtime/WorldMapPartyCommandBehavior.Admission.cs`
- `content/modules/AF.Module.WorldMap/ModuleData/Languages/afworldmap_strings.xml`
- `content/modules/AF.Module.WorldMap/ModuleData/Languages/CNs/afworldmap_strings-zh-CN.xml`

目标：覆盖审查确认的停留/巡逻/跟随完成、建队结果、队列结束、藏身处结算详情和伤亡摘要；不翻译未知动态事实，未知文本继续原样保留以避免误改模型记忆。

验证边界：运行资源键/占位符契约和本地化 DLL 专项；不构建、不 Stage、不部署、不 Push。

## Reviewed Findings

- P1 WorldMap: `LogFact` is a shared history and HUD entry, not a logger-only exclusion. At `WorldMapPartyCommandBehavior.cs:7996`, facts are written before `DisplayCommandMessage`; the latter projects at `8084`. Previously missing production forms included completed hold/patrol/follow, party creation, army upkeep, return/merge, and timeout messages. The B helper returned unmatched Chinese unchanged.
- P1 Hideout details: `WorldMapPartyCommandBehavior.HideoutClear.cs:216` appends native-battle text and casualty counts; B only projected the enclosing result shell. The settlement-capture matcher also expected “已经被占领” while producers at `WorldMapPartyCommandBehavior.cs:2081` and `2165` emit “已经被攻下”.
- P1 Issue (outside this fix): `VanillaIssueOfferBridge.cs:699` assigns the supposedly excluded log phrase to `out error`; `IssueActionOwner.cs:303` displays that error. Finalization errors at `VanillaIssueOfferBridge.cs:342,361,366,375` likewise reach the UI.
- P1 Crafting (outside this fix): `RewardSystemBehavior.PlayerRpCrafting.cs:740` and other fixed validation/refund/rollback reasons reach `AnimusForgeTerminalBehavior.cs:1101` / `ShowPlayerRpForgeError:1144`. A strict test oracle is not a permanent business prohibition: replace display text without changing the safety decision, and update focused assertions when required.
- P1 Economy (outside this fix): faction result projection omits live failure producers at `RewardSystemBehavior.cs:6104,6202,6217,6246,6254,6314`; the fallback in `EconomyUiTexts.cs` returns the original Chinese.
- P2 Economy (outside this fix): kingdom and clan success regexes in `EconomyUiTexts.cs:626,735,741,747` consume transition/fief summaries without displaying them. These summaries describe governors, succession and carried fiefs, so the loss affects Chinese as well as English.

## Implementation

Intent checkpoint: `d1ff11314`.

Only the new `WorldMapPartyCommandBehavior.DisplayLocalization.cs`, 11 added display-boundary lines in the existing behavior, and the WorldMap resource pair are changed. No producer, history write, storage schema, protocol label or adapter is changed. 55 prepared rules plus 8 component strings add 63 paired keys. Known nested casualty descriptions are split and independently projected; optional prisoners and ships remain represented; unknown dynamic text is preserved.

Frequency: projection runs on actual notifications. Rule regexes and group metadata initialize once. Already-English messages return before matching; no reflection, queue enumeration or per-frame resource search was added.

## Verification

- PASS `LocalizationResourceContractTests.py`: 25 resource pairs.
- PASS `LocalizationRuntimeTests.ps1 -ReferenceDir _deps_auto -ResourceName afworldmap_strings`: 1112 resolutions, English / Chinese / French fallback / switch back.
- PASS `WorldMapDisplayProjectionTests.ps1`: 19 real-producer format fixtures, multi-digit days, optional prisoner/ship counts, nested hideout casualties, unknown-text preservation, and result-shell protection.
- PASS `git diff --check` for this slice.
- NOT-RUN production builds, Stage, deployment, live game, old saves, full command-cycle runtime execution. Regex fixture checks read actual source patterns; they do not execute the full C# behavior.

## English Asset Delivery

The runtime loads `TownAmbientDialogue.json` at `TownAmbientDialogueBehavior.cs:1615`. Parallel English assets must be selected for the EN distribution and copied under that same destination filename, while the CN asset remains preserved in its source set. Adding `.en.json` alone has no runtime effect and requires no C# language detection under this packaging model.

Current default ambient asset has 1667 lines, 1449 with Chinese Memory and 1449 with Chinese ReplyHints. Those are model inputs; the revised full-English release requires a separate English content asset. `TownAmbientAiClient.cs:172` also explicitly asks for Chinese output, which asset filename substitution cannot remove. These remain separate prompt work.

The B render helper is shared: `TownAmbientDialogueBehavior.cs:1355` also renders Memory, and `769` passes rendered anchor speech into the model. The “display only” claim is too broad for that method. No protocol break was confirmed from that change.

Residual: unknown externally supplied text/exception messages can still be Chinese. This slice is not proof of zero Chinese in every WorldMap runtime path, or zero Chinese hardcodes in source. The latest full-English requirement needs the remaining Prompt/diagnostic/default-data work too.

Rollback: focused inverse of the repair commit only; never reset shared work. The original B delivery report is preserved.
