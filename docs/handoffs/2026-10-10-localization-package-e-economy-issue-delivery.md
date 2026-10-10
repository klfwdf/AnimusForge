# Package E delivery — economy, RP crafting and issue display

Task card: `docs/handoffs/2026-10-10-localization-package-e-economy-issue.md`
Shared rules: `docs/handoffs/2026-10-10-localization-cde-plan.md`
Baseline tag: `33a9ba44f` · Branch: `main`

Scope actually covered: economy/RP-crafting/issue **source and display resources**.
The world book is owned by Package B; diplomacy and civil war are owned by Package D.
This package replaced the earlier "English asset plan" for E and did no world book work.

## Commits

| SHA | Subject |
| --- | --- |
| `6214b925f` | add 64 missing `AF_ECO_UI_*` resource keys for economy UI projections |
| `46b7eb9f8` | project issue error reasons to bilingual display text (gap 1, part 1) |
| `488957016` | project RP-forge and recruitment display text (gaps 2 and 4) |
| `3963e0ef5` | localize unknown-companion placeholder on issue display paths (gap 1, part 2) |
| `06f850483` | repair stale RP-forge display assertions in the economy stress test (pre-existing breakage) |
| `7597c7b16` | assert the bilingual economy display surface in the economy stress test |

## Gap status

| # | Gap (card lines 32–50) | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Issue error reasons reach the player through `ShowInfo` | closed | `IssueDisplayTexts.cs:135` `ForDisplay`; wired at `VanillaIssueOfferBridge.cs:645`; unknown-companion placeholder projected at `IssueActionOwner.cs:136,167` via `IssueDisplayTexts.cs:121` |
| 2 | RP crafting rank/probability refusal, refund and rollback errors | closed | `PlayerRpForgeDisplayTexts.cs:237` `ForDisplay`; 82 ids; wired at `AnimusForgeTerminalBehavior.cs:846,1108` |
| 3 | `FactionIdentityStatusForDisplay` missing branches | closed | six branch keys in `EconomyUiTexts.cs:707-760`; live wiring at `RewardSystemBehavior.cs:16233,16253,16273,16292` |
| 4 | Swallowed fief / transition detail and protocol-tag leakage | closed | `EconomyRecruitmentDetailTexts.cs:37` `Describe`; 11 keys; wired at `EconomyUiTexts.cs:639,789,796,803,822,841`. Regex/label class: 33/33 harness cases, 15/15 probe cases, 0 leaks. Live economy HUD projects through `FactionIdentityStatusForDisplay` (`RewardSystemBehavior.cs:16233,16253,16273,16292`); `KingdomTransferStatusForDisplay` (`:16193`) is the castle-aftermath/abdication entry; the castle-aftermath recruitment path re-enters through `CastleAftermathUiText.cs:175` |
| 5 | Civil-war value reaches giver/receiver facts before the HUD | **NOT MET — Package D owns it** | see "Unfulfilled dependency" below |
| 6 | Re-audit every real UI path; keep genuinely raw legacy text raw | closed | 1023-row economy UI path audit and 137-row issue UI path audit; see "Residual Chinese" below |

## Files written

New:

- `src/modules/AF.Module.Economy/Presentation/PlayerRpForgeDisplayTexts.cs` (523 lines)
- `src/modules/AF.Module.Economy/Presentation/EconomyRecruitmentDetailTexts.cs` (223 lines)
- `src/AF.Module.Issue/Presentation/IssueDisplayTexts.cs` (200 lines, created by `46b7eb9f8`, extended by `3963e0ef5`)

Modified (display or minimal wiring only):

- `src/modules/AF.Module.Economy/Host/EconomyUiTexts.cs`
- `src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalBehavior.cs` — two lines (`:846`, `:1108`), projection moved ahead of the inquiry sanitizer
- `src/AF.Module.Issue/Host/VanillaIssueOfferBridge.cs` — one line (`:645`)
- `src/AF.Module.Issue/Actions/IssueActionOwner.cs` — two display call sites (`:136`, `:167`); the prompt body at `:254` and the AFEF facts at `:165-166` stay byte-identical
- `tests/modules/AF.Module.Economy/GiveAssetTagCodec.StressTests/Program.cs`

Not modified, and why:

- `src/AF.Module.Social/Host/SocialUiLocalization.cs` — outside E's write scope and, from the call graph, not needed: `RewardSystemBehavior.cs:4304` and `:16403` both already pass through `RecruitmentStatusForDisplay`, so no protocol tag reaches the social path.
- `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/**` — outside E's write scope, and the castle-aftermath recruitment path already re-enters E's projector via `CastleAftermathUiText.cs:174-176`.
- `src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs` — business result strings, AFEF facts, save data and internal matching stay original.

## Resources

| Pair | Keys per side |
| --- | --- |
| `content/modules/AF.Module.Economy/ModuleData/Languages/afeconomy_strings.xml` + `CNs/afeconomy_strings-zh-CN.xml` | 350 |
| `content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml` + `CNs/afconv_ui_strings-zh-CN.xml` | 337 |
| `content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml` + `CNs/af_ui_strings-zh-CN.xml` | 39 `AF_TERMINAL_UI_Forge_*` keys (paired; not created by E) |

The economy pair grew 188 → 252 (`6214b925f`) → 350 (81 RP-forge keys plus the recruitment keys). Both pairs are LF on both sides (`git ls-files --eol` → `i/lf`), have no duplicate ids, have identical key sets, and have identical placeholder sets per key. Newlines are XML entities, never literal newlines inside an attribute value.

No shared registry needed editing, because three language pairs already existed. Creating a *new* pair would have been reported as unmet instead.

## Residual Chinese and what is deliberately not claimed

Classified by the two path audits plus a scope-wide CJK literal count of 1619 (`EconomyUiTexts.cs` 117, `RewardSystemBehavior.cs` 1256, `RewardSystemBehavior.PlayerRpCrafting.cs` 111, `VanillaIssueOfferBridge.cs` 29, `IssueActionOwner.cs` 31, `IssueDisplayTexts.cs` 14).

Genuinely raw on purpose:

- Business outcomes, AFEF facts, save data, internal `string` matching and protocol labels keep their original semantics. Only UI boundaries project display text.
- `EconomyUiTexts.cs` returns unprojected input verbatim at `:106`, `:584`, `:648`, `:850`, which lets producer text reach the speaker at `RewardSystemBehavior.cs` 16170/16193/16233/16253/16273/16292/16346/17080.
- `VanillaIssueOfferBridge.cs:568` `"未命名任务"` is an unreachable fallback; `:588` `"玩家"` has zero callers. `:573` `"未知同伴"` is projected at display time and intentionally left byte-identical in the generator.
- Prompt/model bodies (`VanillaIssueOfferBridge.cs:631,635`, `IssueActionOwner.cs:254`, `IssueRuntimePromptOwner.cs:190`) are model-facing, not player-visible, and asserted as-is.
- Projector pass-through is a feature: unrecognised input is returned verbatim rather than blanked or replaced. A future unknown producer degrades to untranslated-but-intact text.

Known remaining leaks, all outside E's write scope:

- `RewardSystemBehavior.DebtLedger.cs:134-159` `BuildDebtDueStatusText` produces raw Chinese, reaching `AF.Module.Duel/Host/DuelBehavior.cs:5844` and `:5962` via `RecordDeferredDuelDebtForNpc` (`:1029`/`:1041`) with no projection. `BuildDebtEditorSummary` (`:599`) reaches `DeveloperRootEditorController.cs:522`.
- `DebtPromiseQuest.cs` has 7 hardcoded Chinese fallbacks that bypass `EconomyUiTexts` (`:60`, `:77`, `:78`, `:79`, `:80`, `:169`, `:188`). These need *new* keys; they are the only XML work the evidence still supports, and were not done here.
- `CourierLetterReplyPopup.Show` content at `RewardSystemBehavior.cs:13399`/`:13413` originates outside the module.
- `SocialUiLocalization`'s `AF_SOCIAL_UI_*` keys live outside the economy language directory.

Explicit non-claims:

- **No claim of complete English coverage.** 76 `UI_DISPLAY` rows were identified, of which 35 are not fully projected. 14 executable CJK lines were deliberately excluded and are all comments (`PlayerRpForgeDisplayTexts.cs:136-208`, `:508`).
- Four greedy `(.+)` captures remain in `FixedAssetStatusForDisplay` (`EconomyUiTexts.cs:453`, `:466`, `:509`, `:543`). They were left as-is because the tagged/abdication class is where tag leakage was proven; this class is not claimed leak-free.
- `GetPlayerRpTemplateTypeLabel` (`RewardSystemBehavior.PlayerRpCrafting.cs:2727`) is not on the terminal path, so `TypeDisplayForDisplay` currently has no caller.

## Unfulfilled dependency (gap 5, Package D)

The economy HUD needs localized civil-war outcome text, but its only reachable port member is `ICivilWarModulePort.TryApplyTag` (`src/AF.Contracts/Internal/TeamModules/ICivilWarModulePort.cs:217`), which returns a bare `out string` carrying the owner's raw Chinese rule sentence. `MessageProjectionKey` (line 53) is created in Kingdom and destroyed at `KingdomCivilWarOwner.cs:856`/`:899`/`:914`, so it cannot cross the port. Four of six tag classes leak (JOIN/RECRUIT/LEAVE/DETONATE).

Two possible fixes, both Package D's to choose:

- **A (Kingdom only):** `KingdomCivilWarOwner.cs:856`/`:899`/`:914` → `CivilWarUiText.ResultNotice(result)`, matching `:941`. Note `RewardSystemBehavior.cs:16211-16212` and `:16213` consume the *same* string, so they must be split or the fact channel would receive English.
- **B:** add one port member returning localized civil-war display text, implemented in `CivilWarModuleAdapter.cs`, called from `RewardSystemBehavior.cs:16213`.

Do **not** wire to any of: `TryApplyTag(..., out CivilWarActionResult ...)`, `CivilWarUiText.ResultNotice(string)`, `TryGetDisplayText`, `ProjectForDisplay`, `TryProjectCivilWar`, `GetCivilWarDisplayText`, `CivilWarDisplayText`, `TryApplyTagProjected`, `EconomyUiTexts.CivilWarStatusForDisplay`, `EconomyUiTexts.CivilWarResultForDisplay`, any `AF_ECO_UI_CivilWar*` id other than `AF_ECO_UI_CivilWarFaction`, or any `AF_KINGDOM_CW_Notice_*` key outside the 28-key switch at `CivilWarUiText.cs:124-153`.

## Pre-existing test breakage repaired (not E functionality)

`4f694e894` ("Localize economy crafting and party transfer UI", von.branden, 2026-10-10, 108 commits before HEAD) externalized the RP-forge display strings but did not update `GiveAssetTagCodec.StressTests`, so the suite aborted on its first stale conjunct (`exit 1`) and never reached the later assertions. Repaired in `06f850483`.

**The task card cites `Program.cs:499`; that line exists and has always passed.** The real first failure was `:396`. The five repaired sites are `:396`, `:437`, `:488-493`, `:521`, `:528`.

Each repair kept the behaviour, rejection and safety conjuncts byte-identical and only redirected the stale Chinese display literal to its authoritative source: `:396` → `AF_ECO_UI_RpForgeConfirmationChance`; `:437` → the four `AF_ECO_UI_RpForgeEquipment*` keys plus their `EconomyUiTexts.PlayerRpEquipment*` call sites; `:488` → `AF_TERMINAL_UI_Forge_SelectCrafter`/`_CrafterEntry`; `:521` → `AF_TERMINAL_UI_Forge_ConfirmationWeaponTitle`; `:528` → `EconomyUiTexts.PlayerRpConfirmationPlayerChoice`. The rejected-submission guard at `:517` and the negative numeric-header guards at `:519-520` are untouched. No check was deleted or weakened, and no information was removed.

Provenance was formally established only for `:396` and `:437`; whether `:517`, `:521`, `:528` and `:605` were broken by the same commit or an earlier one is **not established**.

Note for future probes in this file: several `File.ReadAllText` variables are multi-file concatenations (`rewardSystem` spans 5 files at `:206-210`; `sharedPostprocessOwner` and `scenePostprocess` are also concatenations). A single-file literal probe will produce false "missing" results.

## Verification

| Entry point | Result |
| --- | --- |
| `dotnet run -c Release` in `tests/modules/AF.Module.Economy/GiveAssetTagCodec.StressTests` | **PASS** `assertions=80596 fuzz=20000 pressureTags=25000`, exit 0 |
| `.tmp_pkg_e_audit/verify_gap4_regex.py` | **PASS** projection cases 33, failures 0; describe cases 11, failures 0; candidate-fix probes 18, newly fixed 0, still broken 0 |
| `.tmp_pkg_e_audit/probe_fixed_patterns.py` | **PASS** cases 15, failures 0; known-limitation-passthrough 3/3 |
| `.tmp_pkg_e_audit/check_issue_projection.py` | **PASS** resources 337/337, projector literals 14, wiring 7, passthrough 15, idempotent 1, whitespace 1, eol 4, companion sites 2, raw fact paths 3 |
| `tests/content/J15ContentContractTests/LocalizationResourceContractTests.py` | **PASS** (static resource contract) |
| `LocalizationRuntimeTests.ps1` | **NOT RUN** — needs `.tmp/build_check/1.3`/`1.4` reference directories |
| Both `BannerlordApi=1.3` and `1.4` compile replay | **NOT RUN** — per card line 70 no compiled replay was performed |

Because no compiled replay was run, the terminal edit, the two new display files, the `CompanionNameForDisplay` wiring and the new test block are **not compile-verified**. The regex work is simulation-verified only; the issue-side wiring is contract-test verified only.

## Known mainline issues observed but not caused by this package

- `tests/content/J15ContentContractTests/run.py:969` fails on `content map entry count` inside `verify_map_and_resources()`, before the hash checks. Pre-existing.
- `run.py:1054` has pin drift at `HEAD~1` (`3D6062B5…` / `7A1A7559…`): `eda6992f9` changed files without refreshing the pins. Refreshing them requires editing `run.py`, which is outside this package's write scope. The content map and `language_data.xml:21` do register the affected files.
- `tests/replay/PhaseEightParityReplayTests/IssueActionOwnerReplay.cs:44` is marked stale and fails on a clean HEAD independently of Package E; `:46` is locale-sensitive. Not repaired here.
