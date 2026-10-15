param(
    [string]$SdkDirectory = '',
    [string]$RuntimeDependencyDirectory = '',
    [switch]$BuildPackage
)
# Builds only a project-local stage. Never deploys to an installed game.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$keys = @('PATH','BannerlordBinDir','NativeBinDir','SandBoxBinDir','HarmonyPath','UIExtenderExPath','Mcmv5Path','MBOptionScreenPath')
$old = @{}
$oldCulture = [Globalization.CultureInfo]::CurrentCulture
foreach ($key in $keys) { $old[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
try {
    # PS 7 auto-parses JSON dates; keep legacy marker round-trips culture invariant.
    [Globalization.CultureInfo]::CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
    if ($SdkDirectory) { $env:PATH = ([IO.Path]::GetFullPath($SdkDirectory)) + [IO.Path]::PathSeparator + $env:PATH }
    & dotnet run --project (Join-Path $root 'tests/ExpeditionParade.Rules/ExpeditionParade.Rules.csproj') -- $root
    if ($LASTEXITCODE -ne 0) { throw 'Parade rule regression tests failed.' }
    if (-not $BuildPackage) { return }
    if (-not $RuntimeDependencyDirectory) { throw '-BuildPackage requires -RuntimeDependencyDirectory containing the six AF private runtime DLLs.' }
    $ref13 = Join-Path $root '.tmp/build_check/1.3'
    $ref14 = Join-Path $root '.tmp/build_check/1.4'
    # These are saved, version-checked reference overlays, not a game installation.
    $fixture = Join-Path $root '.tmp/reference-only-game'
    New-Item -ItemType Directory -Path (Join-Path $fixture 'Modules') -Force | Out-Null
    $env:BannerlordBinDir = $ref13; $env:NativeBinDir = $ref13; $env:SandBoxBinDir = $ref13
    $env:HarmonyPath = Join-Path $ref14 'Bannerlord.Harmony.dll'
    $env:UIExtenderExPath = Join-Path $ref14 'Bannerlord.UIExtenderEx.dll'
    $env:Mcmv5Path = Join-Path $ref14 'MCMv5.dll'
    $env:MBOptionScreenPath = Join-Path $ref14 'Bannerlord.MBOptionScreen.v1.4.0.dll'
    & (Join-Path $PSScriptRoot 'build_single_module.ps1') -ProjectRoot $root -BannerlordRoot $fixture `
        -Bannerlord13ReferenceDir $ref13 -Bannerlord14ReferenceDir $ref14 `
        -RuntimeDependencyDir $RuntimeDependencyDirectory -HarmonyCorePath (Join-Path $ref14 '0Harmony.dll') `
        -Configuration Debug -Stage
    if ($LASTEXITCODE -ne 0) { throw 'Parade package build failed.' }
}
finally {
    [Globalization.CultureInfo]::CurrentCulture = $oldCulture
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $old[$key], 'Process') }
}
