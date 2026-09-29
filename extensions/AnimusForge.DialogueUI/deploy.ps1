param([Parameter(Mandatory=$true)][string]$BannerlordRoot)
$ErrorActionPreference='Stop'
$moduleRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$gameRoot=(Resolve-Path -LiteralPath $BannerlordRoot).Path
$native=[xml](Get-Content -Raw (Join-Path $gameRoot 'Modules\Native\SubModule.xml'))
$version=[string]$native.Module.Version.value
$api=if($version -match '^v?1\.3\.'){'1.3'}elseif($version -match '^v?1\.4\.'){'1.4'}else{throw "Unsupported game version: $version"}
if(Get-Process -Name 'Bannerlord','Bannerlord.Native','TaleWorlds.MountAndBlade.Launcher' -ErrorAction SilentlyContinue){throw 'Close Bannerlord and Launcher before deploying.'}
$stage=Join-Path $moduleRoot "artifacts\stage\$api\AnimusForge_DialogueUI"
$target=[IO.Path]::GetFullPath((Join-Path $gameRoot 'Modules\AnimusForge_DialogueUI'))
$modules=[IO.Path]::GetFullPath((Join-Path $gameRoot 'Modules'))
if([IO.Path]::GetDirectoryName($target) -ne $modules){throw 'Target boundary mismatch.'}
$backup=Join-Path $moduleRoot ('artifacts\deploy-backups\clean-rebuild-'+(Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
if(Test-Path -LiteralPath $target){Copy-Item -LiteralPath $target -Destination (Join-Path $backup 'old-module') -Recurse -Force; Remove-Item -LiteralPath $target -Recurse -Force}
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path (Join-Path $stage '*') -Destination $target -Recurse -Force
$host13=Join-Path $gameRoot 'Modules\AnimusForge\bin\Win64_Shipping_Client\versions\1.3\AnimusForge.dll'
$host14=Join-Path $gameRoot 'Modules\AnimusForge\bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll'
[pscustomobject]@{Target=$target;Api=$api;Version=$version;Backup=$backup;UiDll=(Get-FileHash (Join-Path $target 'bin\Win64_Shipping_Client\AnimusForge.DialogueUI.dll')).Hash;Af13=(Get-FileHash $host13).Hash;Af14=(Get-FileHash $host14).Hash} | ConvertTo-Json | Set-Content (Join-Path $moduleRoot 'artifacts\deployment-clean-rebuild.json') -Encoding UTF8
Write-Output "Deployed clean independent DialogueUI $api to $target; backup $backup"
