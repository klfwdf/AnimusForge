param(
    [string]$ProjectRoot = "",
    [string]$BuildDll = "",
    [string]$BannerlordRoot = ""
)

$ErrorActionPreference = "Stop"

function Get-FullPathSafe {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.Path]::GetFullPath($Path)
}

function Test-SourceModuleDir {
    param([Parameter(Mandatory = $true)][string]$Path)

    $requiredEntries = @("SubModule.xml", "bin", "ModuleData", "GUI", "ONNX")
    $missing = New-Object System.Collections.Generic.List[string]

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Source module directory not found: $Path"
    }

    foreach ($entry in $requiredEntries) {
        if (-not (Test-Path -LiteralPath (Join-Path $Path $entry))) {
            $missing.Add($entry)
        }
    }

    if ($missing.Count -gt 0) {
        throw ("Source module directory is incomplete: {0}`nMissing: {1}" -f $Path, ($missing -join ", "))
    }
}

function Get-TargetModuleDirs {
    param([string]$BannerlordRootPath)

    $targets = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($BannerlordRootPath)) {
        $bannerlordRootFull = Get-FullPathSafe -Path $BannerlordRootPath
        if (-not (Test-Path -LiteralPath $bannerlordRootFull -PathType Container)) {
            throw "Bannerlord root not found: $bannerlordRootFull"
        }

        $modulesDir = Join-Path $bannerlordRootFull "Modules"
        if (-not (Test-Path -LiteralPath $modulesDir -PathType Container)) {
            throw "Bannerlord Modules directory not found: $modulesDir"
        }

        $targets.Add((Join-Path $modulesDir "AnimusForge"))
        return @($targets)
    }

    $candidateRoots = New-Object System.Collections.Generic.List[string]
    $drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
    foreach ($rootRaw in $drives) {
        $root = $rootRaw.TrimEnd('\', '/')
        $candidateRoots.Add($root + "\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord")
        $candidateRoots.Add($root + "\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
        $candidateRoots.Add($root + "\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord")
    }

    foreach ($candidateRoot in ($candidateRoots | Sort-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidateRoot -PathType Container)) {
            continue
        }

        $modulesDir = Join-Path $candidateRoot "Modules"
        if (-not (Test-Path -LiteralPath $modulesDir -PathType Container)) {
            continue
        }

        $targets.Add((Join-Path $modulesDir "AnimusForge"))
    }

    $resolved = @($targets | Sort-Object -Unique)
    if ($resolved.Count -eq 0) {
        throw "No Bannerlord install found under common Steam paths."
    }

    return $resolved
}

function Sync-BuildOutputIntoSourceModule {
    param(
        [Parameter(Mandatory = $true)][string]$SourceModuleDir,
        [string]$BuildDllPath
    )

    if ([string]::IsNullOrWhiteSpace($BuildDllPath)) {
        return
    }

    $buildDllFull = Get-FullPathSafe -Path $BuildDllPath
    if (-not (Test-Path -LiteralPath $buildDllFull -PathType Leaf)) {
        throw "Build DLL not found: $buildDllFull"
    }

    $moduleBinDir = Join-Path $SourceModuleDir "bin\Win64_Shipping_Client"
    New-Item -ItemType Directory -Path $moduleBinDir -Force | Out-Null

    $targetDllPath = Join-Path $moduleBinDir "AnimusForge.dll"
    Copy-Item -LiteralPath $buildDllFull -Destination $targetDllPath -Force
    Write-Host "Updated     : $targetDllPath"

    $buildPdbPath = [System.IO.Path]::ChangeExtension($buildDllFull, ".pdb")
    if (Test-Path -LiteralPath $buildPdbPath -PathType Leaf) {
        $targetPdbPath = Join-Path $moduleBinDir "AnimusForge.pdb"
        Copy-Item -LiteralPath $buildPdbPath -Destination $targetPdbPath -Force
        Write-Host "Updated     : $targetPdbPath"
    }
}

function Invoke-RobocopySync {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$TargetDir
    )

    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    $arguments = @(
        $SourceDir,
        $TargetDir,
        "/E",
        "/R:1",
        "/W:1",
        "/NP",
        "/NFL",
        "/NDL",
        "/NJH",
        "/NJS",
        "/XD",
        "Logs"
    )

    & robocopy @arguments | Out-Null
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "robocopy failed for target '$TargetDir' with exit code $exitCode"
    }
}

function Assert-SameHash {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][string]$TargetRoot,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $sourcePath = Join-Path $SourceRoot $RelativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        return
    }

    $targetPath = Join-Path $TargetRoot $RelativePath
    if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
        throw "Missing deployed file: $targetPath"
    }

    $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash
    if ($sourceHash -ne $targetHash) {
        throw "Hash mismatch after deploy: $RelativePath"
    }

    Write-Host "Verified    : $RelativePath"
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot ".."
}

$projectRootFull = Get-FullPathSafe -Path $ProjectRoot
$sourceModuleDir = Join-Path $projectRootFull "AnimusForge"
$sourceModuleDir = Get-FullPathSafe -Path $sourceModuleDir

Test-SourceModuleDir -Path $sourceModuleDir
Sync-BuildOutputIntoSourceModule -SourceModuleDir $sourceModuleDir -BuildDllPath $BuildDll

$targetModuleDirs = Get-TargetModuleDirs -BannerlordRootPath $BannerlordRoot

Write-Host "Source Module: $sourceModuleDir"
Write-Host "Targets      : $($targetModuleDirs.Count)"

foreach ($targetModuleDir in $targetModuleDirs) {
    $targetModuleDirFull = Get-FullPathSafe -Path $targetModuleDir

    if ($targetModuleDirFull.Equals($sourceModuleDir, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Skipped      : $targetModuleDirFull (source and target are the same)"
        continue
    }

    Write-Host "Deploying    : $targetModuleDirFull"
    Invoke-RobocopySync -SourceDir $sourceModuleDir -TargetDir $targetModuleDirFull
    Assert-SameHash -SourceRoot $sourceModuleDir -TargetRoot $targetModuleDirFull -RelativePath "SubModule.xml"
    Assert-SameHash -SourceRoot $sourceModuleDir -TargetRoot $targetModuleDirFull -RelativePath "ModuleData\RuleBehaviorPrompts.json"
    Assert-SameHash -SourceRoot $sourceModuleDir -TargetRoot $targetModuleDirFull -RelativePath "bin\Win64_Shipping_Client\AnimusForge.dll"
}

Write-Host "Deploy Mode  : whole module copy (excluding Logs)"
Write-Host "Deploy Result: success"
