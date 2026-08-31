#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$ReferencePath,
    [Parameter(Mandatory = $true)][string]$HarmonyModulePath,
    [Parameter(Mandatory = $true)][string]$McmModulePath,
    [Parameter(Mandatory = $true)][string]$UiExtenderModulePath,
    [Parameter(Mandatory = $true)][string]$PrivateRuntimePath,
    [Parameter(Mandatory = $true)][string]$ScratchDirectory,
    [string]$ConflictingMcmAssemblyPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$helper = Join-Path $PSScriptRoot 'Copy-ReplayDependencies.ps1'
$powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$implementation = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\bin\Debug\single_module_stage\AnimusForge\bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll'))
if (-not [IO.Path]::IsPathRooted($ScratchDirectory)) { throw 'ScratchDirectory must be absolute.' }
$scratch = Join-Path $ScratchDirectory ('replay-dependency-tests-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $scratch
$base = @{
    GameRoot = $GameRoot; ReferencePath = $ReferencePath; HarmonyModulePath = $HarmonyModulePath
    McmModulePath = $McmModulePath; UiExtenderModulePath = $UiExtenderModulePath
    PrivateRuntimePath = $PrivateRuntimePath; ImplementationPath = $implementation
}
$script:passed = 0

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Hash([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Case-Arguments([string]$Name) {
    $values = $base.Clone()
    $values.ProjectDirectory = Join-Path $scratch $Name
    $null = New-Item -ItemType Directory -Path $values.ProjectDirectory -Force
    $values.OutputDirectory = Join-Path $values.ProjectDirectory 'bin\Debug\net8.0'
    return $values
}

function Run-Case([string]$Name, [hashtable]$Values, [string]$ExpectedError = '') {
    $arguments = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $helper)
    foreach ($key in $Values.Keys) { $arguments += @("-$key", [string]$Values[$key]) }
    $log = Join-Path $scratch ($Name + '.log')
    $ErrorActionPreference = 'Continue'
    & $powershell @arguments *> $log
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $text = [IO.File]::ReadAllText($log)
    if ($ExpectedError) {
        Assert ($exitCode -ne 0 -and $text.Contains($ExpectedError)) "$Name did not fail closed with $ExpectedError. Log: $log"
    } else {
        Assert ($exitCode -eq 0 -and $text.Contains('REPLAY_DEPENDENCIES_PASS')) "$Name did not pass. Log: $log"
    }
    $script:passed++
    Write-Output "PASS $Name"
}

function Make-McmFixture([string]$Name, [string]$Assembly = '') {
    $root = Join-Path $scratch $Name
    $bin = Join-Path $root 'bin\Win64_Shipping_Client'
    $null = New-Item -ItemType Directory -Path $bin -Force
    Copy-Item -LiteralPath (Join-Path $McmModulePath 'SubModule.xml') -Destination $root
    if ($Assembly) { Copy-Item -LiteralPath $Assembly -Destination (Join-Path $bin 'MCMv5.dll') }
    return $root
}

if (-not $ConflictingMcmAssemblyPath) { $ConflictingMcmAssemblyPath = Join-Path $ReferencePath 'MCMv5.dll' }
$currentMcm = Join-Path $McmModulePath 'bin\Win64_Shipping_Client\MCMv5.dll'
$oldMcmIdentity = [Reflection.AssemblyName]::GetAssemblyName($ConflictingMcmAssemblyPath)
$currentMcmIdentity = [Reflection.AssemblyName]::GetAssemblyName($currentMcm)
Assert ($oldMcmIdentity.Name -eq 'MCMv5' -and $oldMcmIdentity.Version -ne $currentMcmIdentity.Version) 'Negative identity test requires a real MCMv5 assembly of a different version; no stub DLL is generated.'

$case = Case-Arguments 'missing-reference'
$case.ReferencePath = Join-Path $scratch 'absent'
Run-Case 'missing-reference' $case 'REPLAY_INPUT_MISSING'
Assert (-not (Test-Path -LiteralPath $case.OutputDirectory)) 'Missing input created output.'

$case = Case-Arguments 'wrong-module'
$case.McmModulePath = $UiExtenderModulePath
Run-Case 'wrong-module' $case 'REPLAY_MODULE_ID_MISMATCH'

$case = Case-Arguments 'output-boundary'
$case.OutputDirectory = Join-Path $scratch 'outside-runner-bin'
Run-Case 'output-boundary' $case 'REPLAY_OUTPUT_BOUNDARY'
Assert (-not (Test-Path -LiteralPath $case.OutputDirectory)) 'Out-of-bound output was created.'

$case = Case-Arguments 'missing-assembly'
$case.McmModulePath = Make-McmFixture 'empty-mcm'
Run-Case 'missing-assembly' $case 'REPLAY_DEPENDENCY_MISSING'
Assert (-not (Test-Path -LiteralPath $case.OutputDirectory)) 'Dependency validation wrote partial output.'

$case = Case-Arguments 'identity-mismatch'
$case.McmModulePath = Make-McmFixture 'older-mcm' $ConflictingMcmAssemblyPath
Run-Case 'identity-mismatch' $case 'REPLAY_ASSEMBLY_IDENTITY_MISMATCH'

# All fixture assemblies are copies of real inputs; no generated DLLs/stubs.
$case = Case-Arguments ('spaces ' + [char]0x9A8C + [char]0x8BC1)
Run-Case 'valid-paths' $case
$manifestPath = Join-Path $case.OutputDirectory 'af-replay-dependencies.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
Assert ($manifest.Dependencies.Count -gt 0) 'Dependency manifest is empty.'
Assert (-not (Test-Path -LiteralPath (Join-Path $case.OutputDirectory 'AnimusForge.dll'))) 'Implementation was copied into test runtime dependencies.'
$mcmRecord = @($manifest.Dependencies | Where-Object { $_.Name -eq 'MCMv5.dll' })
Assert ($mcmRecord.Count -eq 1 -and $mcmRecord[0].Source -eq $currentMcm) 'MCM was not selected from its explicit owner.'
$manifestHash = Hash $manifestPath
Run-Case 'idempotent-copy' $case
Assert ((Hash $manifestPath) -eq $manifestHash) 'An unchanged input set produced a different manifest.'

$case = Case-Arguments 'output-conflict'
$null = New-Item -ItemType Directory -Path $case.OutputDirectory -Force
$conflictPath = Join-Path $case.OutputDirectory 'MCMv5.dll'
Copy-Item -LiteralPath $ConflictingMcmAssemblyPath -Destination $conflictPath
$beforeHash = Hash $conflictPath
Run-Case 'output-conflict' $case 'REPLAY_OUTPUT_CONFLICT'
Assert ((Hash $conflictPath) -eq $beforeHash) 'Conflicting output was overwritten.'
Assert (@(Get-ChildItem -LiteralPath $case.OutputDirectory -File).Count -eq 1) 'Output conflict copied partial dependencies.'

$case = Case-Arguments 'nested-duplicate'
$nested = Join-Path $case.OutputDirectory 'stale'
$null = New-Item -ItemType Directory -Path $nested -Force
Copy-Item -LiteralPath $currentMcm -Destination $nested
Run-Case 'nested-duplicate' $case 'REPLAY_OUTPUT_DUPLICATE'
Assert (-not (Test-Path -LiteralPath (Join-Path $case.OutputDirectory 'MCMv5.dll'))) 'Duplicate validation copied a second assembly.'

Write-Output "PASS replayDependencyFramework cases=$script:passed scratch=$scratch"
