# Shared encoder for the one-click package flow and explicitly prepared package inputs.
function Resolve-AnimusForgeSevenZip {
    foreach ($base in @($env:ProgramFiles, [Environment]::GetEnvironmentVariable('ProgramFiles(x86)'))) {
        if ([string]::IsNullOrWhiteSpace($base)) { continue }
        $candidate = Join-Path $base '7-Zip/7z.exe'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    $command = Get-Command 7z.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) { return $command.Source }
    throw '7-Zip is required for extreme .7z packages. Install 64-bit 7-Zip or put 7z.exe on PATH.'
}

function Write-AnimusForgeExtremeSevenZip {
    param(
        [Parameter(Mandatory = $true)][string]$InputDirectory,
        [Parameter(Mandatory = $true)][string]$IncludeListPath,
        [Parameter(Mandatory = $true)][string]$ArchivePath
    )
    $inputFull = [IO.Path]::GetFullPath($InputDirectory)
    $listFull = [IO.Path]::GetFullPath($IncludeListPath)
    $archiveFull = [IO.Path]::GetFullPath($ArchivePath)
    if (Test-Path -LiteralPath $archiveFull) { throw "Refusing to overwrite an archive: $archiveFull" }
    if (-not (Test-Path -LiteralPath $inputFull -PathType Container)) { throw "Package input not found: $inputFull" }
    if (-not (Test-Path -LiteralPath $listFull -PathType Leaf)) { throw "Package include list not found: $listFull" }
    $sevenZip = Resolve-AnimusForgeSevenZip
    $arguments = @(
        'a', '-t7z', '-mx=9',
        # Keep the measured low-memory profile; legacy lc/lp overrides break liblzma readers.
        '-m0=LZMA2:d=16m:fb=273',
        '-ms=on', '-mqs=on', '-mmt=2', '-mtm=off', '-mtc=off', '-mta=off',
        '-scsUTF-8', '-bsp0', $archiveFull, ('@' + $listFull)
    )
    Push-Location -LiteralPath $inputFull
    try {
        & $sevenZip @arguments | Out-Host
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) { throw ('7-Zip encoding failed with exit code {0}: {1}' -f $exitCode, $archiveFull) }
    }
    finally { Pop-Location }
}

# Encode the locked projection directly; no ZIP compression or extraction occurs.
function Assert-AnimusForgeProjectionSources {
    param([Parameter(Mandatory = $true)]$Projection)
    foreach ($item in $Projection) {
        if ($null -eq $item.Bytes) {
            $actual = (Get-FileHash -LiteralPath $item.SourcePath -Algorithm SHA256).Hash
        } else {
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $actual = [BitConverter]::ToString($sha.ComputeHash([byte[]]$item.Bytes)).Replace('-', '') }
            finally { $sha.Dispose() }
        }
        if ($actual -ine $item.Hash) { throw "Localization source changed during packaging: $($item.Target)" }
    }
}
function Write-AnimusForgeProjectedSevenZip {
    param(
        [Parameter(Mandatory = $true)][string]$ArchivePath,
        [Parameter(Mandatory = $true)]$Projection,
        [scriptblock]$ValidatePayload
    )
    $archiveFull = [IO.Path]::GetFullPath($ArchivePath)
    if (Test-Path -LiteralPath $archiveFull) { throw "Refusing to overwrite an archive: $archiveFull" }
    $parent = [IO.Path]::GetDirectoryName($archiveFull).TrimEnd('\', '/')
    $work = [IO.Path]::GetFullPath((Join-Path $parent ('.af7z-' + [Guid]::NewGuid().ToString('N'))))
    $prefix = $parent + [IO.Path]::DirectorySeparatorChar
    if (-not $work.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw '7-Zip work directory is outside the output directory.' }
    New-Item -ItemType Directory -Path $work -ErrorAction Stop | Out-Null
    try {
        $payload = Join-Path $work 'payload'
        $targets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($item in $Projection) {
            Assert-AnimusForgeRelativeContentPath -Path $item.Target -Label 'Package projection target'
            if (-not $targets.Add($item.Target)) { throw "Duplicate projection target: $($item.Target)" }
            $file = Join-Path $payload ('AnimusForge/' + $item.Target)
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($file)) | Out-Null
            if ($null -eq $item.Bytes) {
                Assert-AnimusForgeNoReparsePoint -Path $item.SourcePath -Label 'Package projection source'
                [IO.File]::Copy($item.SourcePath, $file, $false)
            } else {
                [IO.File]::WriteAllBytes($file, [byte[]]$item.Bytes)
            }
            if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $item.Hash) { throw "Payload differs from its localization projection: $($item.Target)" }
        }
        if ($ValidatePayload) { & $ValidatePayload $payload }
        $list = Join-Path $work 'include.txt'
        $entries = [string[]]@($Projection | ForEach-Object { 'AnimusForge/' + $_.Target })
        [IO.File]::WriteAllLines($list, $entries, [Text.UTF8Encoding]::new($false))
        $candidate = Join-Path $work 'candidate.7z'
        Write-AnimusForgeExtremeSevenZip -InputDirectory $payload -IncludeListPath $list -ArchivePath $candidate

        $readback = Join-Path $work 'readback'
        $sevenZip = Resolve-AnimusForgeSevenZip
        & $sevenZip 'x' '-y' '-bsp0' ('-o' + $readback) $candidate | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "7-Zip readback failed: $archiveFull" }
        $files = @(Get-ChildItem -LiteralPath $readback -Recurse -File -Force)
        if ($files.Count -ne $Projection.Count) { throw '7-Zip entry count differs from the locked localization projection.' }
        foreach ($item in $Projection) {
            $file = Join-Path $readback ('AnimusForge/' + $item.Target)
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "7-Zip is missing a projected entry: $($item.Target)" }
            if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $item.Hash) { throw "7-Zip differs from its localization projection: $($item.Target)" }
        }
        if ($ValidatePayload) { & $ValidatePayload $readback }
        Assert-AnimusForgeProjectionSources -Projection $Projection
        if (Test-Path -LiteralPath $archiveFull) { throw "Refusing to overwrite an archive: $archiveFull" }
        [IO.File]::Move($candidate, $archiveFull)
    }
    finally {
        $cleanup = [IO.Path]::GetFullPath($work)
        if (-not $cleanup.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or
            -not [IO.Path]::GetFileName($cleanup).StartsWith('.af7z-', [StringComparison]::Ordinal)) {
            throw 'Refusing to clean an unexpected 7-Zip work directory.'
        }
        if (Test-Path -LiteralPath $cleanup -PathType Container) {
            if (([IO.File]::GetAttributes($cleanup) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Refusing to clean a redirected 7-Zip work directory.' }
            Remove-Item -LiteralPath $cleanup -Recurse -Force
        }
    }
}
