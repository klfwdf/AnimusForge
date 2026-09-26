[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

$ErrorActionPreference = 'Stop'
$deploy = Get-Content -LiteralPath (Join-Path $ProjectRoot '一键编译覆盖推送\deploy_module.ps1') -Raw -Encoding UTF8
if ($deploy.Contains('Merge-PlayerExports') -or $deploy.Contains('Sync-PlayerExportsBackToSource') -or
    $deploy.Contains('AnimusForge_1_3_x') -or $deploy.Contains('AnimusForge_1_4_5') -or
    $deploy.Contains('/MIR')) {
    throw 'AF2 deployment still contains legacy PlayerExports or broad-copy behavior.'
}
$map = Get-Content -LiteralPath (Join-Path $ProjectRoot 'content\content-map.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (@($map.entries | Where-Object { ([string]$_.target) -match '(^|/)PlayerExports(/|$)' }).Count -ne 0) {
    throw 'PlayerExports entered the installable content map.'
}
& (Join-Path $PSScriptRoot 'ManagedDeployContractTests.ps1') -ProjectRoot $ProjectRoot -RunRoot $RunRoot
if (-not $?) { throw 'Managed deployment contract failed.' }
Write-Output 'playerExportsDeployment noSourceBackSync=1 noInstallMerge=1 noStageData=1 PASS'
