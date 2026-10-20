[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$ProjectRoot, [Parameter(Mandatory = $true)][string]$RunRoot)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($RunRoot)
$allowed = [IO.Path]::GetFullPath((Join-Path $ProjectRoot 'artifacts\j15-content'))
if (-not $root.StartsWith($allowed + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $root)) {
    throw 'New workspace-only storage deploy fixture required.'
}
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $ProjectRoot 'scripts\build\deploy_module.ps1'), [ref]$null, [ref]$errors)
if ($errors.Count) { throw 'Deploy syntax errors.' }
foreach ($name in @('Get-FullPathSafe', 'Get-FileSha256', 'Assert-PathUnderRoot', 'Assert-NotReparsePoint', 'Assert-NoReparseAncestors', 'Assert-DeploymentPath', 'Assert-FeatureBridgesDeploymentBaseline', 'Invoke-ManagedStageDeployment')) {
    $node = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $false))
    if ($node.Count -ne 1) { throw "Missing function: $name" }
    . ([scriptblock]::Create($node[0].Extent.Text))
}
$stage = Join-Path $root 'stage'
$modules = Join-Path $root 'game\Modules'
$target = Join-Path $modules 'AnimusForge'
$recovery = Join-Path $modules '.AnimusForge.Recovery\deploy'
$fakeUser = Join-Path $root 'ignored-user-data'
New-Item -ItemType Directory -Path $stage,$target,$fakeUser | Out-Null
$env:LOCALAPPDATA = $null
$env:ANIMUSFORGE_DATA_ROOT = $fakeUser
function Put([string]$base, [string]$relative, [string]$text, [datetime]$time) {
    $path = Join-Path $base $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [IO.File]::WriteAllText($path, $text)
    [IO.File]::SetLastWriteTimeUtc($path, $time)
}
$old = ([datetime]'2020-01-01T00:00:00Z').ToUniversalTime()
$new = ([datetime]'2021-01-01T00:00:00Z').ToUniversalTime()
$relative = 'PlayerExports\worldbook\data.json'
Put $stage $relative 'default-new' $new
Put $target $relative 'player-old' $old
foreach ($name in @('CustomPrompts\keep.json', 'ModuleData\TerminalSettings.json', 'Logs\ModelDropdownCache.json', 'Configs\TerminalSettings.json', 'Cache\ModelDropdownCache.json', 'Recovery\private.json')) {
    Put $stage $name 'default' $new
    Put $target $name '{broken-player-content' $old
}
Put $target 'PlayerExports\extra\private.json' 'EXTRA' $new
Put $stage 'CustomPrompts\missing.json' 'new-editable-prompt' $new
Put $stage 'CustomPrompts\Policy\missing.json' 'new-policy-prompt' $new
Put $stage 'CustomPrompts\Policy\keep.json' 'replacement' $new
Put $target 'CustomPrompts\Policy\keep.json' 'player-policy' $old
Put $stage 'ModuleData\Defaults\CustomPrompts\PlayerCustomPromptRule.json' 'new-fixed-default' $new
Put $target 'ModuleData\Defaults\CustomPrompts\PlayerCustomPromptRule.json' 'old-fixed-default' $old
$sourceHashes = @{}
foreach ($file in Get-ChildItem -LiteralPath $stage -File -Recurse) { $sourceHashes[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if ([IO.File]::ReadAllText((Join-Path $target $relative)) -ne 'default-new' -or [IO.File]::GetLastWriteTimeUtc((Join-Path $target $relative)) -ne $new) { throw 'Newer default or source timestamp not installed.' }
foreach ($name in @('CustomPrompts\keep.json', 'ModuleData\TerminalSettings.json', 'Logs\ModelDropdownCache.json', 'Configs\TerminalSettings.json', 'Cache\ModelDropdownCache.json', 'Recovery\private.json')) {
    if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne '{broken-player-content') { throw "Player file overwritten: $name" }
}
if ((Get-ChildItem -LiteralPath $fakeUser -Force).Count -ne 0) { throw 'External player root was touched.' }
foreach ($pair in @(@('CustomPrompts\missing.json', 'new-editable-prompt'), @('CustomPrompts\Policy\missing.json', 'new-policy-prompt'), @('CustomPrompts\Policy\keep.json', 'player-policy'))) {
    if ([IO.File]::ReadAllText((Join-Path $target $pair[0])) -cne $pair[1]) { throw "Editable prompt install/preserve failed: $($pair[0])" }
}
if ([IO.File]::ReadAllText((Join-Path $target 'ModuleData\Defaults\CustomPrompts\PlayerCustomPromptRule.json')) -ne 'new-fixed-default') { throw 'Fixed defaults failed to update independently of user prompts.' }
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
foreach ($path in $sourceHashes.Keys) {
    if ((Get-FileHash -LiteralPath $path).Hash -cne $sourceHashes[$path]) { throw 'Stage source changed.' }
}
if (Test-Path -LiteralPath (Join-Path $modules '.AnimusForge.Recovery')) { throw 'Deployment must not create Recovery.' }
Write-Output 'PASS prompt seeds: missing root/policy installed; existing root/policy preserved; fixed defaults updated; source unchanged; no Recovery'
