param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$GamePath = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
# The audit injects this isolated root into the private diagnostics constructor. No player files.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$artifactRoot = Join-Path $repoRoot 'artifacts\illustrator-optimization\diagnostics'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
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
    Add-Type -Path (Join-Path $PSScriptRoot 'GenerationDiagnosticsAudit.cs') -ReferencedAssemblies @(
        'System.Net.Http.dll', 'System.Drawing.dll', (Join-Path $repoRoot 'bin\Debug\net472\Newtonsoft.Json.dll'))
    [GenerationDiagnosticsAudit]::Run((Resolve-Path $AssemblyPath).Path, $artifactRoot)
} finally { $resolver.Dispose() }
