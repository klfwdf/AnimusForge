param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$EvidencePng,
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
    Add-Type -Path (Join-Path $PSScriptRoot 'NativeEmblemPipelineAudit.cs') -ReferencedAssemblies 'System.Drawing.dll','System.Core.dll'
    $results = [NativeEmblemPipelineAudit]::Run((Resolve-Path $AssemblyPath).Path, [IO.Path]::GetFullPath($OutputDirectory), (Join-Path $PSScriptRoot 'fixtures'))
    $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding UTF8
    $failed = @($results | Where-Object { -not $_.Passed })
    if ($failed.Count -gt 0) { throw "Native emblem audit failed: $($failed.Name -join ', ')" }
    if ($EvidencePng) {
        $assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
        $encode = $assembly.GetType('AnimusForge.Illustrator.Engine.NativeBannerImage', $true).GetMethod('Encode', [Reflection.BindingFlags]'Static,NonPublic')
        $result = $encode.Invoke($null, @([IO.File]::ReadAllBytes((Resolve-Path $EvidencePng).Path), 256))
        if (-not $result) { throw 'Captured banner reference failed conversion' }
        [IO.File]::WriteAllBytes((Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) 'captured-banner-corrected.png'), [Convert]::FromBase64String($result))
    }
    Write-Output "NATIVE EMBLEM AUDIT: $($results.Count) PASS / 0 FAIL (no GPU or external API)"
} finally { $resolver.Dispose() }
