# Duel literal text replay

Run from the repository root:

```powershell
python tests/content/J15ContentContractTests/DuelLiteralTextReplay/run.py --run-root artifacts/localization-audit-repair-20261012/agent-behavior/duel-literal-final
```

The harness links the actual `DuelFactTexts.cs`, extracts entire current and saved-before methods from `DuelBehavior.cs`, and uses actual TaleWorlds Localization/Library DLLs for both reference lines. `inputs.json` seals paths, versions, full-file hashes, extracted method hashes and line numbers. Before and current method bodies differ only by the recorded 31 formatting expressions and new UI helper; inverse application restores all remaining source bytes exactly.

Production resources are the unchanged raw Duel JSON and actual Duel/UI XML catalogs. English uses the native fallback; CN uses the registered UI XML, explicitly checked with a deliberately incorrect fallback. Locale switching is English → 简体中文 → English. Raw/UI formatter coverage includes every migrated template, literal braces/slot-looking names, `$1`, null/empty values, unknown keys/slots and nested debt suffixes. Ordinary outputs are compared to the unmodified saved-before formatter using en-US, zh-CN, de-DE and fr-FR cultures. Exact settlement methods cover win/loss, stake present/absent, deferred debt present/absent, full/partial/zero payment, failed debt recording, missing reward owner and transfer exceptions. Exact renown methods compare the actual formatted output and outgoing facts for both actors with fractional values.

The real postprocessor's AD capture expression is compiled, and exact cache, consume and outcome-bound consume methods are exercised. Campaign leaves are controlled: Hero names have already been captured as strings; transfer/debt methods return declared fixture results; `AppendExternalDialogueHistory` captures the exact outgoing `extraFact`. Its real forwarding body and the postprocessor cache call are checked statically. This does not run a live campaign, real inventory mutation, real debt ledger persistence, upstream Hero.Name localization, the full AF assemblies, Bootstrap, installed 1.4.8 or all English semantic review.

`LanguageData.LoadFromXml` is invoked reflectively only to register the test's controlled language definitions, pointing to unchanged production catalogs. No product reflection is added. CN is registered with the native English grammar processor because these templates have named slots and no native conditional grammar. This tests native text evaluation and actual CN resource lookup; it does not claim a game startup/module registration acceptance test.

The initial exploratory `tr-TR` ordinary-native comparison returned an empty old `AF_DUEL_StakeItems` output (run-01/run-02), whereas the raw replacement retained the fields. Turkish current-culture parity is not certified by this test; the final ordinary parity matrix is the four cultures listed above. Discovery runs are retained and must not be described as functional regression proof or final passing runs.

Rollback bytes are under `artifacts/localization-audit-repair-20261012/agent-behavior/duel-literal-before/`. Revert only this slice's recorded formatting replacements/helper after checking later edits; do not overwrite the whole file over another author's changes.
