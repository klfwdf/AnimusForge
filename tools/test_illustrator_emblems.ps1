param(
    [ValidateSet('1.3', '1.4')][string]$BannerlordApi = '1.4',
    [string]$AssemblyPath = '',
    [string]$OutputDirectory = '',
    [string]$SampleDirectory = '',
    [string]$BannerlordRoot = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$SampleDirectory) { $SampleDirectory = Join-Path $PSScriptRoot 'illustrator\fixtures' }
if (!$AssemblyPath) { $AssemblyPath = Join-Path $root "extensions\AnimusForge.Illustrator\bin\test\$BannerlordApi\AnimusForge.Illustrator.dll" }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root "artifacts\tests\emblem-offline-$BannerlordApi" }
$AssemblyPath = (Resolve-Path -LiteralPath $AssemblyPath).Path
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($OutputDirectory)
Add-Type -AssemblyName System.Drawing
$dependencyDirs = @(
    (Join-Path $root "bin\Release\single_module_artifacts\versions\$BannerlordApi"),
    (Join-Path $root 'bin\Debug\net472'),
    (Join-Path $BannerlordRoot 'bin\Win64_Shipping_Client'),
    (Join-Path $BannerlordRoot 'Modules\Native\bin\Win64_Shipping_Client')
)
Add-Type -Path (Join-Path $PSScriptRoot 'illustrator\OfflineAssemblyResolver.cs')
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,[string[]]$dependencyDirs)
try {
    $assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)
    $native = $null -ne $assembly.GetType('AnimusForge.Illustrator.Engine.NativeBannerPipeline')
    if ($native) {
        Add-Type -Path (Join-Path $PSScriptRoot 'illustrator\NativeEmblemPipelineAudit.cs') -ReferencedAssemblies System.Drawing
        $results = [NativeEmblemPipelineAudit]::Run($AssemblyPath, $OutputDirectory, $SampleDirectory)
    } else {
        Add-Type -Path (Join-Path $PSScriptRoot 'illustrator\EmblemOfflineAudit.cs') -ReferencedAssemblies System.Drawing
        $results = [EmblemOfflineAudit]::Run($AssemblyPath, $OutputDirectory, $SampleDirectory)
    }
    $failures = @($results | Where-Object { !$_.Passed }).Count
    $report = [ordered]@{
        Assembly = $AssemblyPath
        SHA256 = (Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash
        Api = $BannerlordApi
        Checks = $results.Count
        Failures = $failures
        Boundary = $(if ($native) { 'Native adapter with simulated GPU output: image preservation, cache, cancellation and no-blit widget. Not live native rendering or shader fidelity acceptance.' } else { 'Legacy CPU composition only. No native GPU export, shader golden comparison, live game or image API.' })
        RuntimeDependencies = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name.StartsWith('TaleWorlds.') } | ForEach-Object { [ordered]@{ Name = $_.GetName().Name; Version = $_.GetName().Version.ToString(); Path = $_.Location } })
        Results = @($results)
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding UTF8
    $results | ForEach-Object { Write-Output (('{0} {1}: {2}' -f $(if ($_.Passed) {'PASS'} else {'FAIL'}), $_.Name, $_.Evidence)) }
    Write-Output "$($results.Count) checks / $failures failures; $OutputDirectory"
    if ($failures -gt 0) { exit 1 }
}
finally { $resolver.Dispose() }
