param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference = 'Stop'
$module = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Extract([string]$Path, [string]$Signature) {
    $source = Get-Content $Path -Raw -Encoding UTF8
    $start = $source.IndexOf($Signature)
    if ($start -lt 0) { throw "Missing production signature: $Signature" }
    $opening = $source.IndexOf('{', $start)
    $arrow = $source.IndexOf('=>', $start)
    if ($arrow -ge 0 -and $arrow -lt $opening) {
        return $source.Substring($start, $source.IndexOf(';', $arrow) - $start + 1)
    }
    $depth = 1; $end = $opening + 1
    while ($depth -gt 0) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    return $source.Substring($start, $end - $start)
}
$adapter = Join-Path $module 'src/Native/NativeUiAdapter.cs'
$methods = @('public static bool TryWrap(', 'public static void Release(', 'private static void ReleaseOverlay(', 'private static void OverlayClosed(', 'private static void OverlayRestored(')
$native = ($methods | ForEach-Object { Extract $adapter $_ }) -join "`n"
$router = Extract (Join-Path $module 'src/PresentationRouter.cs') 'private static void ReleaseOwned('
$generated = "using System; using TaleWorlds.Library; using TaleWorlds.CampaignSystem; using TaleWorlds.MountAndBlade; namespace AnimusForge.DialogueUI.Native { public static partial class NativeUiAdapter { $native } } namespace AnimusForge.DialogueUI { internal static partial class PresentationRouter { $router } }"
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($Output), $generated)
