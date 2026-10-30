# Private contract visibility replay

Build Replay.csproj with HarmonyCorePath pointing to the installed managed 0Harmony.dll;
set OutputPath and BaseIntermediateOutputPath to an isolated artifacts directory.
Run Replay.exe with three arguments: pipe-separated dependency directories (matching game API first),
actual candidate AnimusForge.dll, and the CN or EN SocialPromptTexts JSON path.

The replay uses real candidate code and Harmony-controlled observer/day/resource fixtures.
It checks legacy public prompt filtering, new record rejection before public history writes,
private material scheduling, preserved public ownership events, stable-key compatibility,
and NPC projection without deleting participant records. Run a fresh process per API/package.
No native game, save file, provider, deployment, or existing summary repair is exercised.
