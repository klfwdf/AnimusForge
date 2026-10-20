# Nearby map contact replay

Run from the repository root:

```powershell
python tests/modules/AF.Module.Conversation/ProactiveNearbyScanTests/run.py
```

The runner uses the current production range helper, native-locator capture, and qualification prefix through the point immediately before food/snapshot calculation. A counter replaces the expensive tail. It checks the fixed two-times sight radius, changing sight ranges, inclusive range boundaries, visibility/activity/lord filters, empty neighborhoods, candidates moving away, long/invalid routes and existing sea/busy exclusions. A throwing global-list fixture ensures capture never enumerates AllLordParties. Each native locator line runs 15 assertions against 2,000 far-away fixture parties plus local cases. Source/resource checks verify removal of the retired multiplier from product code and both UI languages.

The native 1.3 and 1.4 LocatorGrid/LocatableSearchData/ILocatable source files come from the repository references. The generated 1.3 fixture restores two object casts omitted by its decompiler; a MathF alias makes that reference compile on net8. The tested grid algorithm is otherwise retained. Source hashes and logs remain under a fresh repository-local artifacts directory. Game objects, route distances and the expensive snapshot tail are substitutes; this is not live-game frame-time acceptance.

For the regression negative control, pass `--qualification-ref 24d319a`. The original qualification prefix must fail at `far party rejected before route or snapshots`. Both full product API builds still use the repository's original unified build entry point.
