param(
    [switch]$FrameworkDependent,
    [string]$ModulesRoot = "",
    [switch]$IncludeInstalledMods,
    [switch]$SkipVanillaCatalog,
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\PlayerExportsEditor.App\PlayerExportsEditor.App.csproj"
$catalogProject = Join-Path $root "tools\PlayerExportsEditor.CatalogExporter\PlayerExportsEditor.CatalogExporter.csproj"

function Test-FileLocked {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    try {
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Close()
        return $false
    } catch {
        return $true
    }
}

if ($FrameworkDependent) {
    $output = Join-Path $root "dist\win-x64-framework-dependent"
    dotnet publish $project -c Release -r win-x64 --self-contained false -o $output /p:PublishSingleFile=true
} else {
    $output = Join-Path $root "dist\win-x64-self-contained"
    $targetExe = Join-Path $output "AnimusForgePlayerExportsEditor.exe"
    if (Test-FileLocked -Path $targetExe) {
        $output = Join-Path $root ("dist\win-x64-self-contained-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    }
    dotnet publish $project -c Release -r win-x64 --self-contained true -o $output /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true /p:PublishTrimmed=false
}

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $output "AnimusForgePlayerExportsEditor.exe"
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
    throw "Publish completed but exe was not found: $exe"
}

if (-not $SkipVanillaCatalog) {
    $dataDir = Join-Path $output "Data"
    $catalogPath = Join-Path $dataDir "VanillaConditionCatalog.json"
    $catalogArgs = @("--output", $catalogPath)
    if (-not [string]::IsNullOrWhiteSpace($ModulesRoot)) {
        $catalogArgs += @("--modules-root", $ModulesRoot)
    }
    if ($IncludeInstalledMods) {
        $catalogArgs += "--include-installed-mods"
    }

    dotnet run --project $catalogProject -c Release -- @catalogArgs
    if ($LASTEXITCODE -ne 0) {
        throw "condition catalog export failed with exit code $LASTEXITCODE"
    }
}

$readmePath = Join-Path $output "README_User.txt"
$readmeLines = @(
    "AnimusForge PlayerExports Editor",
    "",
    "Usage:",
    "1. Extract the whole zip package. Do not copy the exe alone.",
    "2. Run AnimusForgePlayerExportsEditor.exe.",
    "3. Open or select your AnimusForge\PlayerExports directory.",
    "4. Create, edit, and save PlayerExports data packages in the editor.",
    "",
    "Files:",
    "- AnimusForgePlayerExportsEditor.exe: standalone editor.",
    "- Data\VanillaConditionCatalog.json: offline metadata index for vanilla heroes, cultures, kingdoms, clans, settlements, identities, skills, and condition dropdowns.",
    "",
    "Notes:",
    "- The editor does not need Bannerlord to be running.",
    "- The editor does not load TaleWorlds DLLs or AnimusForge.dll.",
    "- Data\VanillaConditionCatalog.json contains IDs, display labels, categories, and condition candidates only. It does not include TaleWorlds original XML, textures, models, or resource files.",
    "- Keep the Data folder next to the exe. Without it, dropdown candidates will depend on local game scanning or IDs already present in the opened data package."
)
Set-Content -LiteralPath $readmePath -Value $readmeLines -Encoding UTF8

$zipPath = ""
if (-not $SkipZip) {
    $packageRoot = Join-Path $root "dist\packages"
    New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
    $flavor = if ($FrameworkDependent) { "framework-dependent" } else { "self-contained" }
    $zipPath = Join-Path $packageRoot ("AnimusForgePlayerExportsEditor-win-x64-" + $flavor + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".zip")
    Compress-Archive -Path (Join-Path $output "*") -DestinationPath $zipPath -Force
}

Write-Host "Published:"
Write-Host $exe
if (-not $SkipVanillaCatalog) {
    Write-Host "Offline catalog:"
    Write-Host (Join-Path $output "Data\VanillaConditionCatalog.json")
}
if (-not [string]::IsNullOrWhiteSpace($zipPath)) {
    Write-Host "Package:"
    Write-Host $zipPath
}
