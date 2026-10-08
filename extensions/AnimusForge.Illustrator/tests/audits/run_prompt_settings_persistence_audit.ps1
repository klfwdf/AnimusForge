param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$GamePath = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
# Windows PowerShell/.NET Framework. Real MCM/Harmony; no installed config writes.
Add-Type -Path (Join-Path $PSScriptRoot 'OfflineAssemblyResolver.cs')
$dirs = @(
    (Split-Path -Parent (Resolve-Path $AssemblyPath)),
    (Join-Path $GamePath 'bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\SandBox\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\Native\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\Bannerlord.MBOptionScreen\bin\Win64_Shipping_Client')
)
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]$dirs))
try {
    $mcm = Join-Path $GamePath 'Modules\Bannerlord.MBOptionScreen\bin\Win64_Shipping_Client\MCMv5.dll'
    $harmony = Join-Path $GamePath 'Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll'
    $json = Join-Path $GamePath 'bin\Win64_Shipping_Client\Newtonsoft.Json.dll'
    Add-Type -Path (Join-Path $PSScriptRoot 'PromptSettingsPersistenceAudit.cs') -ReferencedAssemblies $mcm,$harmony,$json,(Join-Path $GamePath 'bin\Win64_Shipping_Client\mono\lib\mono\4.7.2-api\Facades\netstandard.dll'),'System.Core.dll'
    [PromptSettingsPersistenceAudit]::Run((Resolve-Path $AssemblyPath).Path, [System.IO.Path]::GetFullPath($OutputDirectory))
} finally { $resolver.Dispose() }
