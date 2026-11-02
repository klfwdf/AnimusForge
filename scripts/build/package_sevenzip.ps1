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
        '-m0=LZMA:d=512m:fb=273:mf=bt4:mc=1000000:lc=4:lp=2:pb=0',
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

# Preserve the established ZIP layout/build-marker checks internally. Only the
# selected archive is published; the .7z contains the module tree, not a ZIP.
function Write-AnimusForgeProjectedSevenZip {
    param(
        [Parameter(Mandatory = $true)][string]$VerifiedZipPath,
        [Parameter(Mandatory = $true)][string]$ArchivePath,
        [Parameter(Mandatory = $true)]$Projection
    )
    $archiveFull = [IO.Path]::GetFullPath($ArchivePath)
    $parent = [IO.Path]::GetDirectoryName($archiveFull).TrimEnd('\', '/')
    $work = [IO.Path]::GetFullPath((Join-Path $parent ('.af7z-' + [Guid]::NewGuid().ToString('N'))))
    $prefix = $parent + [IO.Path]::DirectorySeparatorChar
    if (-not $work.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw '7-Zip work directory is outside the output directory.' }
    New-Item -ItemType Directory -Path $work -ErrorAction Stop | Out-Null
    try {
        $payload = Join-Path $work 'payload'
        [IO.Compression.ZipFile]::ExtractToDirectory($VerifiedZipPath, $payload)
        $list = Join-Path $work 'include.txt'
        $entries = [string[]]@($Projection | ForEach-Object { 'AnimusForge/' + $_.Target })
        [IO.File]::WriteAllLines($list, $entries, [Text.UTF8Encoding]::new($false))
        Write-AnimusForgeExtremeSevenZip -InputDirectory $payload -IncludeListPath $list -ArchivePath $archiveFull

        $readback = Join-Path $work 'readback'
        $sevenZip = Resolve-AnimusForgeSevenZip
        & $sevenZip 'x' '-y' '-bsp0' ('-o' + $readback) $archiveFull | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "7-Zip readback failed: $archiveFull" }
        $files = @(Get-ChildItem -LiteralPath $readback -Recurse -File -Force)
        if ($files.Count -ne $Projection.Count) { throw '7-Zip entry count differs from the locked localization projection.' }
        foreach ($item in $Projection) {
            $file = Join-Path $readback ('AnimusForge/' + $item.Target)
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "7-Zip is missing a projected entry: $($item.Target)" }
            if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $item.Hash) { throw "7-Zip differs from its localization projection: $($item.Target)" }
        }
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
