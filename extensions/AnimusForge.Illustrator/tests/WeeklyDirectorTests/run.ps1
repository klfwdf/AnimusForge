param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [ValidateSet('CN','EN')][string]$ContentLanguage = 'CN',
    [string]$GamePath = 'F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord',
    [string]$IncidentBodyPath = ''
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$fixture = Join-Path $root "artifacts/weekly-director-relaxation-20261015/$ContentLanguage/AnimusForge"
$data = Join-Path $fixture 'ModuleData'
New-Item -ItemType Directory -Force $data | Out-Null
Set-Content -LiteralPath (Join-Path $fixture 'SubModule.xml') -Value '<Module><Id value="AnimusForge" /></Module>' -Encoding UTF8
$content = Join-Path $root 'content/modules/AnimusForge.Illustrator/ModuleData'
$prompt = if ($ContentLanguage -eq 'EN') { 'IllustratorPromptTexts.en.json' } else { 'IllustratorPromptTexts.json' }
Copy-Item -LiteralPath (Join-Path $content $prompt) -Destination (Join-Path $data 'IllustratorPromptTexts.json')
Copy-Item -LiteralPath (Join-Path $content 'IllustratorCompatibilityTexts.json') -Destination $data
Add-Type -Path (Join-Path $PSScriptRoot '../audits/OfflineAssemblyResolver.cs')
$api = Split-Path -Leaf (Split-Path -Parent $AssemblyPath)
$dirs = @(
    (Split-Path -Parent (Resolve-Path $AssemblyPath)),
    (Join-Path $root ".tmp/build_check/$api"),
    (Join-Path $GamePath 'bin/Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules/AnimusForge/bin/Win64_Shipping_Client/versions/1.4')
)
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]$dirs))
$previousRoot = $env:ANIMUSFORGE_DATA_ROOT
try {
    $env:ANIMUSFORGE_DATA_ROOT = $fixture
    Add-Type -Path (Join-Path $PSScriptRoot 'WeeklyDirectorChecks.cs')
    [WeeklyDirectorChecks]::Run((Resolve-Path $AssemblyPath).Path, $IncidentBodyPath)
} finally {
    $env:ANIMUSFORGE_DATA_ROOT = $previousRoot
    $resolver.Dispose()
}
