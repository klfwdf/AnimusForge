# Terminal map-bar icon sizing regression

Run `python -B tests/modules/AF.Module.WarStats/MapBarIconSizingTests/run.py --run-root <new-local-directory>`.

The runner compiles the complete current `AfTerminalMapBarIconSprite` production class and local vanilla 1.4.5 `IconBrushWidget` without rewriting their logic. Native texture, resource factories and base widget plumbing are substitutes. Covers normal and UseIconSize template behavior, logical vs physical dimensions, delayed/unusual native size, cached texture and replaced brushes, native fallback and missing layer. It does not render native GPU/Gauntlet UI or identify the player's template/mod stack.

`--baseline` uses intent checkpoint `a267f67b4`; it must compile and fail `skin-use-original-sprite-size-stays-inside-native-icon-slot` with the old 128x128 logical sprite.
