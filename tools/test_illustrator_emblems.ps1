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
$handler = [ResolveEventHandler]{
    param($sender, $args)
    $name = (New-Object Reflection.AssemblyName($args.Name)).Name
    foreach ($directory in $dependencyDirs) {
        $candidate = Join-Path $directory "$name.dll"
        if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
try {
    Add-Type -Path (Join-Path $PSScriptRoot 'illustrator\EmblemOfflineAudit.cs') -ReferencedAssemblies System.Drawing
    $results = [EmblemOfflineAudit]::Run($AssemblyPath, $OutputDirectory, $SampleDirectory)
    $failures = @($results | Where-Object { !$_.Passed }).Count
    $report = [ordered]@{
        Assembly = $AssemblyPath
        SHA256 = (Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash
        Api = $BannerlordApi
        Checks = $results.Count
        Failures = $failures
        Boundary = 'CPU composition only. No native GPU export, shader golden comparison, live game or image API.'
        Results = @($results)
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding UTF8
    $results | ForEach-Object { Write-Output (('{0} {1}: {2}' -f $(if ($_.Passed) {'PASS'} else {'FAIL'}), $_.Name, $_.Evidence)) }
    Write-Output "$($results.Count) checks / $failures failures; $OutputDirectory"
    if ($failures -gt 0) { exit 1 }
}
finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($handler) }
