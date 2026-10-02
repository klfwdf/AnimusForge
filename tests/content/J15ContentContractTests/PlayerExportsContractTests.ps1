[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

$ErrorActionPreference = 'Stop'
$deploy = Get-Content -LiteralPath (Join-Path $ProjectRoot 'scripts\build\deploy_module.ps1') -Raw -Encoding UTF8
if ($deploy.Contains('Merge-PlayerExports') -or $deploy.Contains('Sync-PlayerExportsBackToSource') -or
    $deploy.Contains('PlayerExports') -or $deploy.Contains('/MIR')) {
    throw 'AF2 deployment still contains legacy PlayerExports or broad-copy behavior.'
}
if (-not $deploy.Contains('Legacy AnimusForge module folders were left untouched')) {
    throw 'Legacy dual-module warning disappeared from deployment.'
}
$map = Get-Content -LiteralPath (Join-Path $ProjectRoot 'content\content-map.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$books = @($map.entries | Where-Object { ([string]$_.target) -match '(^|/)PlayerExports(/|$)' })
if ($books.Count -ne 3139 -or @($books | Where-Object {
    $_.owner -ne 'AF.Module.Onboarding' -or
    $_.source -cne ('content/modules/AF.Module.Onboarding/' + $_.target)
}).Count -ne 0) {
    throw 'Only the reviewed built-in worldbooks may enter the installable content map.'
}
& (Join-Path $PSScriptRoot 'ManagedDeployContractTests.ps1') -ProjectRoot $ProjectRoot -RunRoot $RunRoot
if (-not $?) { throw 'Managed deployment contract failed.' }
Write-Output 'playerExportsDeployment noSourceBackSync=1 noInstallMerge=1 fixedDefaultFiles=3139 PASS'
