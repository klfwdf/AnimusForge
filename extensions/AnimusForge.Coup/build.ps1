param(
    [Parameter(Mandatory = $true)][string]$BannerlordRoot,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$Deploy
)
$ErrorActionPreference = 'Stop'
$moduleRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repoRoot = [IO.Path]::GetFullPath((Join-Path $moduleRoot '..\..'))
$gameRoot = (Resolve-Path -LiteralPath $BannerlordRoot).Path
$projectPath = Join-Path $moduleRoot 'src\AnimusForge.Coup.csproj'
$nativeManifest = Join-Path $gameRoot 'Modules\Native\SubModule.xml'
[xml]$nativeXml = Get-Content -LiteralPath $nativeManifest -Raw
$gameVersion = [string]$nativeXml.Module.Version.value
if ($gameVersion -notmatch '^v?(1\.[34])\.') { throw "Unsupported installed game: $gameVersion" }
$installedApi = $Matches[1]
if ($installedApi -ne '1.4') { throw 'This dual build requires a 1.4 game installation; 1.3 is built against its pinned reference package.' }
$afPaths = @{}
$afHashes = @{}
foreach ($api in @('1.3','1.4')) {
    $afPath = Join-Path $gameRoot "Modules\AnimusForge\bin\Win64_Shipping_Client\versions\$api\AnimusForge.dll"
    if (!(Test-Path -LiteralPath $afPath -PathType Leaf)) { throw "Required AF implementation not found: $afPath" }
    $afPaths[$api] = $afPath
    $afHashes[$api] = (Get-FileHash -LiteralPath $afPath -Algorithm SHA256).Hash
    $outputDir = Join-Path $moduleRoot "artifacts\$api"
    $objectDir = ((Join-Path $moduleRoot "artifacts\obj\$api") -replace '\\','/') + '/'
    & dotnet build $projectPath -c $Configuration "/p:BannerlordApi=$api" "/p:BannerlordRoot=$gameRoot" "/p:BaseIntermediateOutputPath=$objectDir" "/p:MSBuildProjectExtensionsPath=$objectDir"
    if ($LASTEXITCODE -ne 0) { throw "Coup $api build failed" }
    $dllPath = Join-Path $outputDir 'AnimusForge.Coup.dll'
    if ([Reflection.AssemblyName]::GetAssemblyName($dllPath).Name -ne 'AnimusForge.Coup') { throw 'Wrong module assembly' }
    @{
        Module = 'AnimusForge_Coup'; Api = $api; Configuration = $Configuration
        Reference = $(if ($api -eq '1.3') { 'Bannerlord.ReferenceAssemblies 1.3.15.110062' } else { $gameVersion })
        AfReferenceSha256 = $afHashes[$api]; DllSha256 = (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash
        SourceRevision = (& git -C $repoRoot rev-parse HEAD); CreatedUtc = [DateTime]::UtcNow.ToString('o')
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDir 'build.json') -Encoding UTF8
}
if (!$Deploy) { Write-Output 'Both independent module variants built. No game files changed.'; return }
if (Get-Process -Name 'Bannerlord','Bannerlord.Native' -ErrorAction SilentlyContinue) { throw 'Close Bannerlord before deploying the module.' }
$modulesRoot = [IO.Path]::GetFullPath((Join-Path $gameRoot 'Modules'))
$target = [IO.Path]::GetFullPath((Join-Path $modulesRoot 'AnimusForge_Coup'))
if ([IO.Path]::GetDirectoryName($target) -ne $modulesRoot -or [IO.Path]::GetFileName($target) -ne 'AnimusForge_Coup') { throw 'Deployment boundary mismatch' }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts\deploy-backups\AnimusForge_Coup\$stamp"))
if (!$backupRoot.StartsWith($repoRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Backup boundary mismatch' }
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$existed = Test-Path -LiteralPath $target
if ($existed) { Copy-Item -LiteralPath $target -Destination (Join-Path $backupRoot 'module') -Recurse }
else { 'Module did not exist before this deployment.' | Set-Content -LiteralPath (Join-Path $backupRoot 'NEW_MODULE.txt') -Encoding UTF8 }
$deployBin = Join-Path $target 'bin\Win64_Shipping_Client'
New-Item -ItemType Directory -Path $deployBin -Force | Out-Null
$files = @{
    (Join-Path $moduleRoot 'SubModule.xml') = (Join-Path $target 'SubModule.xml')
    (Join-Path $moduleRoot "artifacts\$installedApi\AnimusForge.Coup.dll") = (Join-Path $deployBin 'AnimusForge.Coup.dll')
    (Join-Path $moduleRoot "artifacts\$installedApi\AnimusForge.Coup.pdb") = (Join-Path $deployBin 'AnimusForge.Coup.pdb')
    (Join-Path $moduleRoot "artifacts\$installedApi\build.json") = (Join-Path $target 'build.json')
}
try {
    foreach ($source in $files.Keys) {
        Copy-Item -LiteralPath $source -Destination $files[$source] -Force
        if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $files[$source]).Hash) { throw "Copy verification failed: $source" }
    }
    foreach ($api in @('1.3','1.4')) {
        if ((Get-FileHash -LiteralPath $afPaths[$api]).Hash -ne $afHashes[$api]) { throw "AF implementation changed during deployment: $api" }
    }
    @{
        Target = $target; Backup = $backupRoot; Api = $installedApi; GameVersion = $gameVersion
        DllSha256 = (Get-FileHash -LiteralPath (Join-Path $deployBin 'AnimusForge.Coup.dll')).Hash
        AfUnchanged = $afHashes; FilesVerified = $files.Count
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $moduleRoot 'artifacts\deployment.json') -Encoding UTF8
}
catch {
    # Restore only this module's exact files. Keep the backup for inspection.
    foreach ($destination in $files.Values) {
        $relative = $destination.Substring($target.Length + 1)
        $previous = Join-Path (Join-Path $backupRoot 'module') $relative
        if ($existed -and (Test-Path -LiteralPath $previous)) { Copy-Item -LiteralPath $previous -Destination $destination -Force }
        elseif (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination }
    }
    throw
}
Write-Output "Deployed only $target ($gameVersion / API $installedApi); verified $($files.Count) files. AF hashes unchanged. Backup: $backupRoot"
