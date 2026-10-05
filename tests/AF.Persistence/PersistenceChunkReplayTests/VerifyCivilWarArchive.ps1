param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$SnapshotPath
)
$ErrorActionPreference = 'Stop'
# Synthetic test data only. Does not load a campaign, read/write player saves,
# install a module, or change a game DLL.
$bin = Join-Path $GameRoot 'bin\Win64_Shipping_Client'
$null = [System.Reflection.Assembly]::LoadFrom((Join-Path $bin 'TaleWorlds.Library.dll'))
$assembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $bin 'TaleWorlds.SaveSystem.dll'))
$type = $assembly.GetType('TaleWorlds.SaveSystem.ArchiveDeserializer', $true)
$method = $type.GetMethod('LoadFrom', [System.Reflection.BindingFlags]'Public,Instance')
$utf8 = [System.Text.UTF8Encoding]::new($false, $true)

function New-StringArchive {
    param([string[]]$Values)
    $memory = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($memory)
    try {
        $writer.Write([int]1)
        # parent=-1, global=0, local=-1, folder extension=Strings (4).
        $writer.Write([byte[]]@(255,255,255,0,0,0,255,255,255,4))
        $writer.Write([int]$Values.Count)
        for ($i = 0; $i -lt $Values.Count; $i++) {
            $writer.Write([byte[]]@(0,0,0))
            $writer.Write([byte[]]@(($i -band 255),(($i -shr 8) -band 255),(($i -shr 16) -band 255)))
            $writer.Write([byte]10)
            $bytes = $utf8.GetBytes($Values[$i])
            $length = $bytes.Length + 4
            # Reproduce the game's unchecked signed-short narrowing exactly.
            $stored = (($length + 32768) % 65536) - 32768
            $writer.Write([short]$stored)
            $writer.Write([int]$bytes.Length)
            $writer.Write($bytes)
        }
        $writer.Flush()
        return ,$memory.ToArray()
    }
    finally { $writer.Dispose(); $memory.Dispose() }
}

$state = Get-Content -LiteralPath $SnapshotPath -Raw -Encoding UTF8 | ConvertFrom-Json
$values = [System.Collections.Generic.List[string]]::new()
foreach ($property in $state.PSObject.Properties) {
    $values.Add($property.Name)
    if ($property.Value -is [string]) { $values.Add($property.Value) }
}
$key = '_af_kingdom_civil_war_v2'
$count = [int]$state.($key + '__af_chunk_count')
$complete = [System.Text.StringBuilder]::new()
for ($i = 0; $i -lt $count; $i++) { $null = $complete.Append([string]$state.($key + '__af_chunk_' + $i)) }
$incident = $complete.ToString()
if ($utf8.GetByteCount($incident) -ne 33365 -or $count -ne 3) { throw 'Invalid synthetic incident fixture' }

$unsafe = [System.Activator]::CreateInstance($type, $true)
$unsafeArchive = New-StringArchive -Values @($incident)
$unsafeFailed = $false
try { $null = $method.Invoke($unsafe, [object[]]@(,$unsafeArchive)) }
catch {
    $errorType = $_.Exception.GetBaseException().GetType().FullName
    if ($errorType -ne 'System.OverflowException') { throw }
    $unsafeFailed = $true
    Write-Output "PASS actualGameArchive legacyOverflow=$errorType bytes=33365"
}
if (-not $unsafeFailed) { throw 'Unchunked control unexpectedly parsed successfully' }

$safe = [System.Activator]::CreateInstance($type, $true)
$safeArchive = New-StringArchive -Values $values.ToArray()
$null = $method.Invoke($safe, [object[]]@(,$safeArchive))
$read = 0
foreach ($folder in $safe.RootFolder.ChildFolders) {
    foreach ($entry in $folder.ChildEntries) {
        $text = $entry.GetBinaryReader().ReadString()
        if ($text -cne $values[$entry.Id.Id]) { throw "Game string reader changed entry $($entry.Id.Id)" }
        $read++
    }
}
if ($read -ne $values.Count) { throw 'Game archive did not expose all entries' }
Write-Output "PASS actualGameArchive chunkedEntries=$read chunkCount=$count exactStringReadback=1"
