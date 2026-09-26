[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
. (Join-Path $ProjectRoot "一键编译覆盖推送\content_layout.ps1")

function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function MustReject([scriptblock]$Action, [string]$Message) {
    $rejected = $false
    try { & $Action } catch { $rejected = $true }
    Check $rejected $Message
}

$root = Get-AnimusForgeContentFullPath -Path $RunRoot
Assert-AnimusForgePathUnderRoot -Path $root -Root (Join-Path $ProjectRoot "artifacts\tests") -Label "Stage fixture"
Assert-AnimusForgeNoReparsePoint -Path $root -Label "Stage fixture"
Check (-not (Test-Path -LiteralPath $root)) "Stage fixture must be new"
$project = Join-Path $root "project"
$stage = Join-Path $project "bin\Debug\single_module_stage\AnimusForge"
$source = Join-Path $project "content\modules\Test\ModuleData\one.json"
New-Item -ItemType Directory -Path (Split-Path -Parent $source) -Force | Out-Null
[System.IO.File]::WriteAllText($source, "synthetic", [System.Text.UTF8Encoding]::new($false))
$map = @{ schemaVersion = 1; entries = @(@{ owner = "Test"; source = "content/modules/Test/ModuleData/one.json"; target = "ModuleData/one.json" }) }
[System.IO.File]::WriteAllText((Join-Path $project "content\content-map.json"), ($map | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Invoke-AnimusForgeContentProjection -ProjectRoot $project -DestinationModuleDir $stage | Out-Null
[System.IO.File]::WriteAllText((Join-Path $stage "SubModule.xml"), "<Module />", [System.Text.UTF8Encoding]::new($false))

$bin = Join-Path $stage "bin\Win64_Shipping_Client"
New-Item -ItemType Directory -Path (Join-Path $bin "versions\1.3") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $bin "versions\1.4") -Force | Out-Null
foreach ($name in @("AnimusForge.Bootstrap.dll", "AnimusForge.Bootstrap.pdb", "AnimusForge.Bootstrap.build.json")) {
    [System.IO.File]::WriteAllText((Join-Path $bin $name), "synthetic")
}
foreach ($version in @("1.3", "1.4")) {
    foreach ($name in @("AnimusForge.dll", "AnimusForge.pdb", "AnimusForge.build.json")) {
        [System.IO.File]::WriteAllText((Join-Path $bin "versions\$version\$name"), "synthetic")
    }
}
$runtimeNames = @("Microsoft.ML.OnnxRuntime.dll", "onnxruntime.dll", "onnxruntime_providers_shared.dll", "System.Buffers.dll", "System.Memory.dll", "System.Runtime.CompilerServices.Unsafe.dll")
$locked = @{}
foreach ($name in $runtimeNames) {
    $path = Join-Path $bin $name
    [System.IO.File]::WriteAllText($path, "synthetic-$name")
    $locked[$name] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
}
$lock = @{ schemaVersion = 1; files = $locked }
[System.IO.File]::WriteAllText((Join-Path $project "content\runtime-dependencies.lock.json"), ($lock | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))

Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage
$private = Join-Path $stage "PlayerExports\private.json"
New-Item -ItemType Directory -Path (Split-Path -Parent $private) | Out-Null
[System.IO.File]::WriteAllText($private, "synthetic")
MustReject { Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage } "Stage accepted PlayerExports"
Remove-Item -LiteralPath $private
Remove-Item -LiteralPath (Split-Path -Parent $private)

$unknown = Join-Path $stage "credential.txt"
[System.IO.File]::WriteAllText($unknown, "synthetic")
MustReject { Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage } "Stage accepted unknown file"
Remove-Item -LiteralPath $unknown

$projected = Join-Path $stage "ModuleData\one.json"
[System.IO.File]::WriteAllText($projected, "changed")
MustReject { Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage } "Stage accepted content hash drift"
[System.IO.File]::WriteAllText($projected, "synthetic", [System.Text.UTF8Encoding]::new($false))
$dependency = Join-Path $bin "System.Memory.dll"
[System.IO.File]::WriteAllText($dependency, "changed")
MustReject { Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage } "Stage accepted dependency hash drift"

MustReject { Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir (Split-Path -Parent $stage) } "Stage accepted a non-module root"
Write-Output "PASS AF2 Stage whitelist synthetic"
