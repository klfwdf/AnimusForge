param([Parameter(Mandatory=$true)][string]$ProjectRoot,
      [Parameter(Mandatory=$true)][string]$ModuleRoot,
      [Parameter(Mandatory=$true)][ValidateSet('zh-CN','en')][string]$Language)
$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts/build/content_layout.ps1')
. (Join-Path $ProjectRoot 'scripts/build/package_localization.ps1')
$worldbook = @{ MappedFiles=@{}; Files=@{}; RuleMappings=@{} }
$rows = @()
foreach ($entry in Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot) {
    if ($entry.Target -notmatch '^ModuleData/|^CustomPrompts/') { continue }
    $asset = $entry.SourcePath
    if ($Language -eq 'en' -and $entry.LocalizationRole -notin @('compatibility','bilingual-ui') -and $entry.Target -notmatch '^ModuleData/Languages/') {
        $selected = Get-AnimusForgeEnglishAsset -Entry $entry -Worldbook $worldbook
        if ($selected) { $asset = $selected }
    }
    $target = Join-Path $ModuleRoot $entry.Target
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
    Copy-Item -LiteralPath $asset -Destination $target -Force
    $rows += [pscustomobject]@{Source=$asset; Target=$entry.Target; Sha256=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash}
}
$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ModuleRoot 'fixture-projection.json') -Encoding UTF8
Write-Host "Current production selectors projected $($rows.Count) non-worldbook module assets ($Language)."
