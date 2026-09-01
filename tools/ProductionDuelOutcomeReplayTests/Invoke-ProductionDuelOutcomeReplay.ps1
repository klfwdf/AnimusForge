param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipStageBuild,
    [string]$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..")),
    [string]$BannerlordRoot = $(if ($env:BANNERLORD_ROOT) { $env:BANNERLORD_ROOT } else { "E:\steam\steamapps\common\Mount & Blade II Bannerlord" }),
    [string]$Bannerlord13ReferenceDir = "",
    [string]$Bannerlord14ReferenceDir = "",
    [string]$WorkshopContentDir = $(if ($env:WORKSHOP_CONTENT_DIR) { $env:WORKSHOP_CONTENT_DIR } else { "E:\steam\steamapps\workshop\content\261550" }),
    [string]$RuntimeDependencyDir = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
if ([string]::IsNullOrWhiteSpace($Bannerlord13ReferenceDir)) {
    $Bannerlord13ReferenceDir = Join-Path $ProjectRoot "_deps_auto"
}
if ([string]::IsNullOrWhiteSpace($Bannerlord14ReferenceDir)) {
    $Bannerlord14ReferenceDir = Join-Path $ProjectRoot ".tmp\build_check\1.4"
}
if ([string]::IsNullOrWhiteSpace($RuntimeDependencyDir)) {
    $RuntimeDependencyDir = [IO.Path]::GetFullPath((Join-Path $ProjectRoot "..\NEW-10\AnimusForge\bin\Win64_Shipping_Client"))
}

$dotnetCandidates = @()
$dotnetCandidates += "C:\Users\28358\AppData\Local\Microsoft\dotnet\dotnet.exe"
if ($env:DOTNET_ROOT) {
    $dotnetCandidates += (Join-Path $env:DOTNET_ROOT "dotnet.exe")
}
$dotnetCandidates += "dotnet"
$dotnet = $null
foreach ($candidate in $dotnetCandidates) {
    if ([IO.Path]::IsPathRooted($candidate)) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $sdkList = & $candidate --list-sdks 2>$null
            if ($sdkList -match '^8\.') {
                $dotnet = $candidate
                break
            }
        }
    }
    else {
        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($command) {
            $sdkList = & $command.Source --list-sdks 2>$null
            if ($sdkList -match '^8\.') {
                $dotnet = $command.Source
                break
            }
        }
    }
}
if (-not $dotnet) {
    throw "DOTNET_SDK_MISSING: A .NET 8 SDK is required."
}

$env:DOTNET_ROOT = Split-Path -Parent ([IO.Path]::GetFullPath($dotnet))
$env:PATH = $env:DOTNET_ROOT + ";" + $env:PATH
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
if (-not $env:DOTNET_CLI_HOME) {
    $env:DOTNET_CLI_HOME = Join-Path $ProjectRoot ".dotnet_cli"
}

if (-not $SkipStageBuild) {
    $buildScript = Join-Path $ProjectRoot "一键编译覆盖推送\build_single_module.ps1"
    if (-not (Test-Path -LiteralPath $buildScript -PathType Leaf)) {
        throw "STAGE_BUILD_SCRIPT_MISSING: $buildScript"
    }
    Write-Host "STAGE_BUILD configuration=$Configuration deploy=false"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $buildScript `
        -ProjectRoot $ProjectRoot `
        -BannerlordRoot $BannerlordRoot `
        -Bannerlord13ReferenceDir $Bannerlord13ReferenceDir `
        -Bannerlord14ReferenceDir $Bannerlord14ReferenceDir `
        -WorkshopContentDir $WorkshopContentDir `
        -RuntimeDependencyDir $RuntimeDependencyDir `
        -Configuration $Configuration `
        -Stage
    if ($LASTEXITCODE -ne 0) {
        throw "STAGE_BUILD_FAILED: exit=$LASTEXITCODE"
    }
}
else {
    Write-Host "STAGE_BUILD skipped=true; freshness guard remains enabled"
}

$project = Join-Path $PSScriptRoot "ProductionDuelOutcomeReplayTests.csproj"
& $dotnet run --project $project --configuration $Configuration -- `
    --project-root $ProjectRoot `
    --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "PRODUCTION_DUEL_OUTCOME_REPLAY_FAILED: exit=$LASTEXITCODE"
}
