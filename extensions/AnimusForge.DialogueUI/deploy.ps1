param([Parameter(Mandatory = $true)][string]$BannerlordRoot)
$ErrorActionPreference = 'Stop'
$moduleRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$gameRoot = (Resolve-Path -LiteralPath $BannerlordRoot).Path
[xml]$native = Get-Content -LiteralPath (Join-Path $gameRoot 'Modules\Native\SubModule.xml') -Raw
$version = [string]$native.Module.Version.value
if ($version -notmatch '^v?(1\.[34])\.') { throw "Unsupported game version: $version" }
$api = $Matches[1]
$stage = Join-Path $moduleRoot "artifacts\stage\$api\AnimusForge_DialogueUI"
if (!(Test-Path -LiteralPath (Join-Path $stage 'SubModule.xml'))) { throw 'Build and stage this game-line variant first.' }
$modulesRoot = [IO.Path]::GetFullPath((Join-Path $gameRoot 'Modules'))
$target = [IO.Path]::GetFullPath((Join-Path $modulesRoot 'AnimusForge_DialogueUI'))
if ([IO.Path]::GetDirectoryName($target) -ne $modulesRoot -or [IO.Path]::GetFileName($target) -ne 'AnimusForge_DialogueUI') { throw 'Module boundary mismatch.' }
if (Get-Process -Name 'Bannerlord','Bannerlord.Native' -ErrorAction SilentlyContinue) { throw 'Close Bannerlord before deploying the UI module.' }
$beforeAf = @{}
foreach ($line in @('1.3','1.4')) {
    $hostFile = Join-Path $gameRoot "Modules\AnimusForge\bin\Win64_Shipping_Client\versions\$line\AnimusForge.dll"
    if (Test-Path -LiteralPath $hostFile) { $beforeAf[$hostFile] = (Get-FileHash -LiteralPath $hostFile -Algorithm SHA256).Hash }
}
if ($beforeAf.Count -eq 0) { throw 'AF must already be installed.' }
$backup = [IO.Path]::GetFullPath((Join-Path $moduleRoot ('artifacts\deploy-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))))
if (!$backup.StartsWith($moduleRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Backup boundary mismatch.' }
New-Item -ItemType Directory -Path $backup -Force | Out-Null
$existed = Test-Path -LiteralPath $target
if ($existed) { Copy-Item -LiteralPath $target -Destination (Join-Path $backup 'module') -Recurse }
else { 'First installation; this independent module did not exist.' | Set-Content -LiteralPath (Join-Path $backup 'NEW_MODULE.txt') -Encoding UTF8 }
$written = New-Object 'System.Collections.Generic.List[string]'
$files = @()
try {
    foreach ($source in Get-ChildItem -LiteralPath $stage -File -Recurse) {
        $relative = $source.FullName.Substring($stage.Length + 1)
        $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
        if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'File boundary mismatch.' }
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
        $written.Add($relative)
        Copy-Item -LiteralPath $source.FullName -Destination $destination -Force
        $expected = (Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $expected) { throw "Copy mismatch: $relative" }
        $files += @{ Path = $relative.Replace('\','/'); Sha256 = $expected }
    }
    foreach ($hostFile in $beforeAf.Keys) {
        if ((Get-FileHash -LiteralPath $hostFile -Algorithm SHA256).Hash -ne $beforeAf[$hostFile]) { throw 'AF host changed during deployment.' }
    }
    $result = @{
        Module = 'AnimusForge_DialogueUI'; Target = $target; Backup = $backup
        Api = $api; GameVersion = $version; FirstInstall = !$existed
        CreatedUtc = [DateTime]::UtcNow.ToString('o'); Files = $files; AfUnchanged = $beforeAf
        DllSha256 = (Get-FileHash -LiteralPath (Join-Path $target 'bin\Win64_Shipping_Client\AnimusForge.DialogueUI.dll')).Hash
        OfflineTestsRun = $false; InGameVerified = $false
    }
    $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $moduleRoot 'artifacts\deployment.json') -Encoding UTF8
    $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backup 'deployment.json') -Encoding UTF8
    Write-Output "Deployed only $target; API $api / $version; $($files.Count) file hashes matched; AF hashes unchanged. Backup: $backup"
}
catch {
    foreach ($relative in $written) {
        $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
        if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { continue }
        $oldFile = Join-Path (Join-Path $backup 'module') $relative
        if ($existed -and (Test-Path -LiteralPath $oldFile)) { Copy-Item -LiteralPath $oldFile -Destination $destination -Force }
        elseif (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination }
    }
    throw
}
