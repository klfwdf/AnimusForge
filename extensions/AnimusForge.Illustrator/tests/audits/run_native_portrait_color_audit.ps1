param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$EvidencePng,
    [string]$CorrectedOutput,
    [string]$GamePath = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'OfflineAssemblyResolver.cs')
$dirs = @(
    (Split-Path -Parent (Resolve-Path $AssemblyPath)),
    (Join-Path $GamePath 'bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\SandBox\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\Native\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\Bannerlord.MBOptionScreen\bin\Win64_Shipping_Client'),
    (Join-Path $GamePath 'Modules\AnimusForge\bin\Win64_Shipping_Client\versions\1.4')
)
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]$dirs))
try {
    Add-Type -Path (Join-Path $PSScriptRoot 'PortraitNoDrawAudit.cs'),(Join-Path $PSScriptRoot 'NativePortraitColorAudit.cs') -ReferencedAssemblies 'System.Drawing.dll','System.Core.dll'
    [NativePortraitColorAudit]::Run((Resolve-Path $AssemblyPath).Path, $EvidencePng, $CorrectedOutput)
} finally { $resolver.Dispose() }
