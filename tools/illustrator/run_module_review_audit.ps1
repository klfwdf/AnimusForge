param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$GamePath = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord',
    [string]$RepoRoot
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
# Windows PowerShell / .NET Framework; launch one fresh process per target DLL.
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
    Add-Type -Path (Join-Path $PSScriptRoot 'ModuleReviewAudit.cs') -ReferencedAssemblies 'System.Xml.dll'
    [ModuleReviewAudit]::Run((Resolve-Path $AssemblyPath).Path, $RepoRoot)
} finally { $resolver.Dispose() }
