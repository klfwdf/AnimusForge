# Deploy/validate script for AnimusForge.Illustrator
param(
    [string]$GamePath = "F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord",
    [ValidateSet("auto", "1.3", "1.4")]
    [string]$BannerlordApi = "auto",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $ScriptDir
$SourceDir = Join-Path $RepoRoot "extensions\AnimusForge.Illustrator"
$TargetDir = Join-Path $GamePath "Modules\AnimusForge_Illustrator"
$BuildOut = Join-Path $SourceDir "bin\deploy\$BannerlordApi"
$BuildObj = Join-Path $SourceDir "obj\deploy\$BannerlordApi"

function Read-XmlFile {
    param([string]$Path)
    $document = New-Object System.Xml.XmlDocument
    $document.PreserveWhitespace = $true
    $document.Load($Path)
    return $document
}

function Get-GameVersion {
    param([string]$Root)
    $nativeManifest = Join-Path $Root "Modules\Native\SubModule.xml"
    if (-not (Test-Path $nativeManifest)) {
        throw "Native manifest not found: $nativeManifest"
    }
    $xml = Read-XmlFile $nativeManifest
    return [string]$xml.Module.Version.value
}

function Resolve-BannerlordApi {
    param([string]$Requested, [string]$Version)
    if ($Requested -ne "auto") { return $Requested }
    if ($Version -match '^v?1\.3\.') { return "1.3" }
    if ($Version -match '^v?1\.4\.') { return "1.4" }
    throw "Unsupported Bannerlord version '$Version'; pass -BannerlordApi explicitly."
}

$gameVersion = Get-GameVersion $GamePath
$BannerlordApi = Resolve-BannerlordApi $BannerlordApi $gameVersion
$BuildOut = Join-Path $SourceDir "bin\deploy\$BannerlordApi"
$BuildObj = Join-Path $SourceDir "obj\deploy\$BannerlordApi"

Write-Host "==> [Illustrator] Building $Configuration / BannerlordApi=$BannerlordApi (game $gameVersion)..." -ForegroundColor Cyan
dotnet build "$SourceDir\src\AnimusForge.Illustrator.csproj" `
    -c $Configuration `
    -p:BannerlordApi=$BannerlordApi `
    -p:OutputPath="$BuildOut\" `
    -p:BaseIntermediateOutputPath="$BuildObj\"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed for BannerlordApi=$BannerlordApi."
}

$moduleXml = Read-XmlFile (Join-Path $SourceDir "SubModule.xml")
$moduleId = [string]$moduleXml.Module.Id.value
$dllName = [string]$moduleXml.Module.SubModules.SubModule.DLLName.value
if ($moduleId -ne "AnimusForge_Illustrator") {
    throw "Unexpected Illustrator module id: $moduleId"
}
if ([string]::IsNullOrWhiteSpace($dllName)) {
    throw "SubModule.xml does not declare DLLName."
}
$declaredAssemblies = @($moduleXml.Module.Assemblies.Assembly | ForEach-Object { [string]$_.value })
if ($declaredAssemblies -notcontains $dllName) {
    throw "SubModule.xml Assemblies does not declare $dllName."
}
foreach ($dependency in @($moduleXml.Module.DependedModules.DependedModule)) {
    $dependencyPath = Join-Path $GamePath "Modules\$($dependency.Id)"
    if (-not (Test-Path (Join-Path $dependencyPath "SubModule.xml"))) {
        throw "Declared dependency is not installed under Modules: $($dependency.Id)"
    }
    [void](Read-XmlFile (Join-Path $dependencyPath "SubModule.xml"))
}

$builtDll = Join-Path $BuildOut $dllName
if (-not (Test-Path $builtDll)) {
    throw "Built DLL missing: $builtDll"
}

$expectedPrefabs = @(
    "ConversationIllustrationOverlay.xml",
    "EncyclopediaIllustrationOverlay.xml",
    "IllustratorGalleryPopup.xml",
    "IllustratorOffscreenStage.xml",
    "WeeklyReportIllustrationOverlay.xml"
)
foreach ($prefabName in $expectedPrefabs) {
    $prefabPath = Join-Path $SourceDir "GUI\Prefabs\$prefabName"
    if (-not (Test-Path $prefabPath)) {
        throw "Expected prefab missing: $prefabName"
    }
    [void](Read-XmlFile $prefabPath)
}

$disallowedRuntimeCopies = Get-ChildItem $BuildOut -File -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match '^(TaleWorlds\.|SandBox\.|0Harmony\.dll$|MCMv5\.dll$|Newtonsoft\.Json\.dll$|AnimusForge\.dll$)'
}
if ($disallowedRuntimeCopies) {
    throw "Runtime/private dependency was copied into output: $($disallowedRuntimeCopies.Name -join ', ')"
}

Write-Host "==> [Illustrator] Validation passed: $dllName, $($moduleXml.Module.Name.value), $($moduleXml.Module.Version.value)" -ForegroundColor Green
if ($ValidateOnly) {
    Write-Host "==> [Illustrator] ValidateOnly complete; no files were deployed." -ForegroundColor Green
    exit 0
}

Write-Host "==> [Illustrator] Deploying to $TargetDir..." -ForegroundColor Cyan

$backupRoot = Join-Path $RepoRoot "artifacts\deploy-backups\AnimusForge_Illustrator\v$BannerlordApi\$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if (Test-Path $TargetDir) {
    $existingFiles = Get-ChildItem $TargetDir -Recurse -File
    foreach ($file in $existingFiles) {
        $relative = $file.FullName.Substring($TargetDir.Length).TrimStart('\')
        $destination = Join-Path $backupRoot $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item $file.FullName $destination -Force
    }
    Write-Host "==> [Illustrator] Backup captured: $backupRoot" -ForegroundColor DarkCyan
}

New-Item -ItemType Directory -Path "$TargetDir\bin\Win64_Shipping_Client" -Force | Out-Null
New-Item -ItemType Directory -Path "$TargetDir\GUI\Prefabs" -Force | Out-Null

Copy-Item -Path "$SourceDir\SubModule.xml" -Destination "$TargetDir\SubModule.xml" -Force
try {
    Copy-Item -Path $builtDll -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
    $builtPdb = [IO.Path]::ChangeExtension($builtDll, ".pdb")
    if (Test-Path $builtPdb) {
        Copy-Item -Path $builtPdb -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
    }
}
catch {
    Write-Warning "DLL is locked. Attempting rename-and-replace fallback..."
    $backupDll = "$TargetDir\bin\Win64_Shipping_Client\$dllName.old"
    Remove-Item $backupDll -Force -ErrorAction SilentlyContinue
    Move-Item -Path "$TargetDir\bin\Win64_Shipping_Client\$dllName" -Destination $backupDll -Force -ErrorAction SilentlyContinue
    Copy-Item -Path $builtDll -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
    $builtPdb = [IO.Path]::ChangeExtension($builtDll, ".pdb")
    if (Test-Path $builtPdb) {
        Copy-Item -Path $builtPdb -Destination "$TargetDir\bin\Win64_Shipping_Client\" -Force
    }
}
Copy-Item -Path "$SourceDir\GUI\Prefabs\*" -Destination "$TargetDir\GUI\Prefabs\" -Force

Write-Host "==> [Illustrator] Deployment complete!" -ForegroundColor Green
Get-ChildItem -Path $TargetDir -Recurse | Where-Object { -not $_.PSIsContainer } | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
