param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipStageBuild,
    [string]$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..")),
    [string]$BannerlordRoot = $(if ($env:BANNERLORD_ROOT) { $env:BANNERLORD_ROOT } else { "" }),
    [string]$Bannerlord13ReferenceDir = "",
    [string]$Bannerlord14ReferenceDir = "",
    [string]$WorkshopContentDir = $(if ($env:WORKSHOP_CONTENT_DIR) { $env:WORKSHOP_CONTENT_DIR } else { "" }),
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
    # Leave resolution to the official unified build script.  It validates a
    # complete private-runtime set from the source module or the explicit game
    # module and keeps Debug/Release staging from copying a stale historical
    # workspace.
    $RuntimeDependencyDir = ""
}

$dotnetCandidates = @()
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
    if ([string]::IsNullOrWhiteSpace($BannerlordRoot)) {
        throw "BANNERLORD_ROOT_REQUIRED: pass -BannerlordRoot or set BANNERLORD_ROOT when rebuilding the Stage."
    }
    # Resolve the repository build entry by its ASCII file name instead of a
    # non-ASCII directory literal. Windows PowerShell 5.1 can parse a UTF-8
    # script without a BOM using the active code page, which previously turned
    # the Chinese directory name into mojibake and made a valid checkout look
    # incomplete. The immediate-child search remains bounded and deterministic.
    $buildScriptCandidates = @(
        Get-ChildItem -LiteralPath $ProjectRoot -Directory -ErrorAction Stop |
            ForEach-Object {
                $candidate = Join-Path $_.FullName "build_single_module.ps1"
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    [IO.Path]::GetFullPath($candidate)
                }
            }
    )
    if ($buildScriptCandidates.Count -ne 1) {
        throw "STAGE_BUILD_SCRIPT_MISSING_OR_AMBIGUOUS: candidates=$($buildScriptCandidates.Count)"
    }
    $buildScript = [string]$buildScriptCandidates[0]
    Write-Host "STAGE_BUILD configuration=$Configuration deploy=false"
    $stageBuildArguments = @(
        "-ProjectRoot", $ProjectRoot,
        "-BannerlordRoot", $BannerlordRoot,
        "-Bannerlord13ReferenceDir", $Bannerlord13ReferenceDir,
        "-Bannerlord14ReferenceDir", $Bannerlord14ReferenceDir,
        "-Configuration", $Configuration,
        "-Stage"
    )
    if (-not [string]::IsNullOrWhiteSpace($WorkshopContentDir)) {
        $stageBuildArguments = @(
            "-WorkshopContentDir", $WorkshopContentDir
        ) + $stageBuildArguments
    }
    if (-not [string]::IsNullOrWhiteSpace($RuntimeDependencyDir)) {
        $stageBuildArguments = @(
            "-RuntimeDependencyDir", $RuntimeDependencyDir
        ) + $stageBuildArguments
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File $buildScript @stageBuildArguments
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
