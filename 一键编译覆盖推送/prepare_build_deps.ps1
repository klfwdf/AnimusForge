param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$BannerlordRoot,
    [string]$WorkshopContentDir = "",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

function Add-ExistingDir {
    param(
        [System.Collections.Generic.List[string]]$Dirs,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        return
    }

    foreach ($existing in $Dirs) {
        if ($existing.Equals($fullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }

    $Dirs.Add($fullPath)
}

$projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot)
$bannerlordRootFull = [System.IO.Path]::GetFullPath($BannerlordRoot)
$targetDir = Join-Path $projectRootFull ("bin\{0}\build_deps" -f $Configuration)
$buildOutputDir = Join-Path $projectRootFull ("bin\{0}\net472" -f $Configuration)

$sourceDirs = New-Object System.Collections.Generic.List[string]
Add-ExistingDir -Dirs $sourceDirs -Path $buildOutputDir
Add-ExistingDir -Dirs $sourceDirs -Path (Join-Path $bannerlordRootFull "Modules\AnimusForge_1_3_x\bin\Win64_Shipping_Client")
Add-ExistingDir -Dirs $sourceDirs -Path (Join-Path $bannerlordRootFull "Modules\AnimusForge_1_4_5\bin\Win64_Shipping_Client")
Add-ExistingDir -Dirs $sourceDirs -Path (Join-Path $projectRootFull "AnimusForge\bin\Win64_Shipping_Client")
Add-ExistingDir -Dirs $sourceDirs -Path (Join-Path $bannerlordRootFull "Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client")
if (-not [string]::IsNullOrWhiteSpace($WorkshopContentDir)) {
    Add-ExistingDir -Dirs $sourceDirs -Path (Join-Path $WorkshopContentDir "2859188632\bin\Win64_Shipping_Client")
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$required = @(
    "0Harmony.dll",
    "Microsoft.ML.OnnxRuntime.dll",
    "System.Memory.dll",
    "System.Buffers.dll",
    "System.Runtime.CompilerServices.Unsafe.dll"
)

$optional = @(
    "onnxruntime.dll",
    "onnxruntime_providers_shared.dll"
)

foreach ($name in ($required + $optional)) {
    foreach ($sourceDir in $sourceDirs) {
        $sourcePath = Join-Path $sourceDir $name
        if (Test-Path -LiteralPath $sourcePath -PathType Leaf) {
            Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $targetDir $name) -Force
            break
        }
    }
}

$missing = New-Object System.Collections.Generic.List[string]
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $targetDir $name) -PathType Leaf)) {
        $missing.Add($name)
    }
}

if ($missing.Count -gt 0) {
    $checked = if ($sourceDirs.Count -gt 0) { $sourceDirs -join "; " } else { "<none>" }
    throw ("Missing build dependencies: {0}`nChecked: {1}" -f ($missing -join ", "), $checked)
}

Write-Host "Build deps   : $targetDir"
Write-Output ("ANIMUSFORGE_BUILD_DEPS_DIR={0}" -f $targetDir)
