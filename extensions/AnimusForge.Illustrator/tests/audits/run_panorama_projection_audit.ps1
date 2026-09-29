param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$GamePath = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
# Windows PowerShell / .NET Framework. Uses the built production DLL and the
# managed vector library only; no camera, native game or HTTP API is invoked.
Add-Type -Path (Join-Path $PSScriptRoot 'OfflineAssemblyResolver.cs')
$gameBin = Join-Path $GamePath 'bin\Win64_Shipping_Client'
$dirs = @((Split-Path -Parent (Resolve-Path $AssemblyPath)), $gameBin)
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]$dirs))
try {
    Add-Type -Path (Join-Path $PSScriptRoot 'PanoramaProjectionAudit.cs') -ReferencedAssemblies @(
        'System.Drawing.dll',
        (Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'netstandard.dll'),
        (Join-Path $gameBin 'System.Numerics.Vectors.dll'),
        (Join-Path $gameBin 'TaleWorlds.Library.dll')
    )
    [PanoramaProjectionAudit]::Run((Resolve-Path $AssemblyPath).Path)
} finally { $resolver.Dispose() }
