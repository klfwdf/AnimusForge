# 说客外交本地验收复跑

最终产品候选：`c6d2b69d8c67403d76c5f2392444af79a506f29d`，包含主体提交 `5ddc8a0b593fc50730e1ad524157a1ec2d125a2e`。实际状态以主台账对应条目为准。全部命令只构建/回放，不启游戏、不 Stage/Deploy、不打包或推送。

## 为什么使用生成的 Recovery.csproj

入口是仓库原有 `tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests/run_response_recovery.py`，不是手工替代业务算法。它从原 smoke 项目读取显式 source membership，加上测试源，在新目录生成 `Recovery.csproj`：链接当前生产 owner/parser/DTO/消费者；排除旧 `PersistenceRecordStubs.cs`；使用本机 .NET 8 SDK 的真正 Newtonsoft 和现有游戏引用。

直接构建原 smoke csproj 会包含旧 persistence/JSON attribute stub，与真正 Newtonsoft 类型重名，不能把那种构建当成当前持久化/实际 owner 回放。既有隔离生成器同时避免污染原项目输出目录，本轮没有改写它或产品构建脚本。

测试使用实际调度/事件/文书/执行协调代码，但 LLM 回答、时间和原生效果端口为受控替身；不等于实机黄金交付、AI 行为或物理 `.sav` 验收。

## 在全新目录生成并跑核心专项

在仓库根目录、PowerShell 中运行。`gameBin` 必须是现有游戏引用目录；当前机器路径如下。每次生成使用新目录，脚本不会覆盖旧测试树。

```powershell
$replayRoot = Join-Path '.tmp' ('lobbyist-accept-' + [Guid]::NewGuid().ToString('N'))
$gameBin = 'E:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client'
$runner = 'tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests/run_response_recovery.py'
python $runner --run-root "$replayRoot/cn" --mode=--lobbyist --package cn --game-bin $gameBin
if ($LASTEXITCODE -ne 0) { throw 'CN replay failed' }
python $runner --run-root "$replayRoot/en" --mode=--lobbyist --package en --game-bin $gameBin
if ($LASTEXITCODE -ne 0) { throw 'EN replay failed' }
```

两次预期末行均为 `PASS lobbyist assertions=168`。最终源码已经使用上述原生成入口在一个全新 EN 目录独立重建并得到 168 PASS，记录见 `fresh-generator.txt`。因此本轮临时 Recovery.csproj 不是唯一可跑入口。

## 六组 CN/EN 完整定向回归

保留上述 `$replayRoot`，继续运行。部分旧回归入口会先执行共享 lifecycle 前置测试，需要把原 Fixtures 中 JSON 放入运行目录。UI 注册使用真实模块 XML，语言 ID 为 `简体中文` / `English`。

```powershell
$repoPath = (Get-Location).Path
$env:AF_SMOKE_UI_MODULE = Join-Path $repoPath 'content/modules/AF.Module.Diplomacy'
foreach ($lang in @('cn', 'en')) {
    $caseRoot = Join-Path $repoPath "$replayRoot/$lang"
    Copy-Item 'tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests/Fixtures/*.json' "$caseRoot/bin/Release/net8.0/"
    $env:AF_SMOKE_PACKAGE_ROOT = Join-Path $caseRoot 'package'
    $env:AF_SMOKE_PACKAGE_LANGUAGE = $lang
    $env:AF_SMOKE_UI_LANGUAGE = if ($lang -eq 'en') { 'English' } else { '简体中文' }
    foreach ($mode in @('--lobbyist', '--sovereign-control', '--bilateral-direct', '--speech-review', '--oral-declaration', '--original-oral-vassalage')) {
        dotnet "$caseRoot/bin/Release/net8.0/Recovery.dll" $mode
        if ($LASTEXITCODE -ne 0) { throw "$lang $mode failed" }
    }
}
```

按上述顺序，CN 和 EN 的断言数各为 **168 / 980 / 194 / 741 / 645 / 343**。不是独立断言去重总数；不同入口包含共享前置回归。`--audit-fixes` 和 `--concurrent-oral-migration` 的既有失败另见 `baseline-failures.txt`，并未宣称全仓 smoke 全绿。

## 双 API / Bootstrap 原脚本纯构建

当前机器已存在以下依赖映射。不要额外传 Stage/Deploy，也不要为复跑改脚本或覆盖游戏文件。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build/build_single_module.ps1 -ProjectRoot . -BannerlordRoot .tmp/stage_game_root -Bannerlord13ReferenceDir .tmp/build_check/1.3 -Bannerlord14ReferenceDir .tmp/build_check/1.4 -RuntimeDependencyDir bin/Release/single_module_stage/AnimusForge/bin/Win64_Shipping_Client -HarmonyCorePath .tmp/stage_game_root/Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client/0Harmony.dll -Configuration Debug
```

最终成功日志和三个 DLL 的路径、SHA256 分别在 `build-output.txt` 与 `verification.json`。保留已有编译警告和离线 NU1900 漏洞源警告。

## 存档专项

```powershell
python tests/AF.Persistence/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py --chunk-contract-only
python tests/modules/AF.Module.Diplomacy/WorldDiplomacyPersistenceAdapter.SmokeTests/run_strict.py --run-root "$replayRoot/strict" --legacy-baseline 61750a208
dotnet run --project tests/AF.Persistence/PersistenceChunkReplayTests/PersistenceChunkReplayTests.csproj -c Release
```

本轮 ChunkReplay 实际在隔离目录生成等价项目，链接原 `Program.cs`、`ReplayStubs.cs`、`CivilWarChunkRegression.cs` 和生产 `CampaignSaveChunkHelper.cs`，没有复制/更改算法；上面最后一条是原项目直接入口。strict 使用本任务检查点验证本轮没有改变 helper；脚本默认更早历史基线在此前工作中已失配。当前验证结果均保存在 `checks.txt`。

## 对照与清理

两个外围失败使用检查点 `61750a208` 的受影响生产/测试源替换生成项目里的对应 Compile 引用，排除检查点不存在的新增文件，并使用该检查点 CN 资源，在同样引用、fixture、语言环境下重跑，复现相同断言；没有因此修改产品。此对照只证明列出的两个失败在本任务前已存在。

本轮临时生成树在证据保存后清理。复跑生成的目录归复跑者维护：确认验收完成后，仅删除自己创建的 `$replayRoot`，不要清理共享 `.tmp`、依赖目录或其他作者产物。最终 build artifacts 与本目录验收资料保留。
