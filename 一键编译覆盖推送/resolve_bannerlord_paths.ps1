param(
    [string]$BannerlordRoot = $env:BANNERLORD_ROOT,
    [string]$WorkshopContentDir = $env:WORKSHOP_CONTENT_DIR
)

$ErrorActionPreference = "Stop"

function Get-FullPathSafe {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Test-BannerlordRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    try {
        $fullPath = Get-FullPathSafe -Path $Path
    } catch {
        return $false
    }

    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        return $false
    }

    $requiredEntries = @(
        "bin\Win64_Shipping_Client",
        "Modules\Native",
        "Modules\SandBox"
    )

    foreach ($entry in $requiredEntries) {
        if (-not (Test-Path -LiteralPath (Join-Path $fullPath $entry))) {
            return $false
        }
    }

    return $true
}

function Add-UniquePath {
    param(
        [System.Collections.Generic.List[string]]$Paths,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    try {
        $fullPath = Get-FullPathSafe -Path $Path
    } catch {
        return
    }

    foreach ($existing in $Paths) {
        if ($existing.Equals($fullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }

    $Paths.Add($fullPath)
}

function Get-SteamRoots {
    $roots = New-Object System.Collections.Generic.List[string]
    $registryKeys = @(
        "HKCU:\Software\Valve\Steam",
        "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam",
        "HKLM:\SOFTWARE\Valve\Steam"
    )

    foreach ($key in $registryKeys) {
        try {
            $props = Get-ItemProperty -Path $key -ErrorAction Stop
            Add-UniquePath -Paths $roots -Path $props.SteamPath
            Add-UniquePath -Paths $roots -Path $props.InstallPath

            if ($props.SteamExe) {
                Add-UniquePath -Paths $roots -Path (Split-Path -Parent ([Environment]::ExpandEnvironmentVariables($props.SteamExe)))
            }
        } catch {
        }
    }

    Add-UniquePath -Paths $roots -Path "C:\Program Files (x86)\Steam"
    Add-UniquePath -Paths $roots -Path "C:\Program Files\Steam"

    return @($roots | Where-Object { Test-Path -LiteralPath $_ -PathType Container })
}

function Get-SteamLibraries {
    $libraries = New-Object System.Collections.Generic.List[string]

    foreach ($steamRoot in Get-SteamRoots) {
        Add-UniquePath -Paths $libraries -Path $steamRoot

        $libraryFoldersPath = Join-Path $steamRoot "steamapps\libraryfolders.vdf"
        if (-not (Test-Path -LiteralPath $libraryFoldersPath -PathType Leaf)) {
            continue
        }

        foreach ($line in Get-Content -LiteralPath $libraryFoldersPath) {
            if ($line -match '^\s*"\d+"\s+"([^"]+)"') {
                Add-UniquePath -Paths $libraries -Path ($matches[1] -replace "\\\\", "\")
                continue
            }

            if ($line -match '^\s*"path"\s+"([^"]+)"') {
                Add-UniquePath -Paths $libraries -Path ($matches[1] -replace "\\\\", "\")
            }
        }
    }

    $drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
    foreach ($rootRaw in $drives) {
        $root = $rootRaw.TrimEnd('\', '/')
        Add-UniquePath -Paths $libraries -Path ($root + "\SteamLibrary")
        Add-UniquePath -Paths $libraries -Path ($root + "\Program Files (x86)\Steam")
        Add-UniquePath -Paths $libraries -Path ($root + "\Program Files\Steam")
    }

    return @($libraries | Where-Object { Test-Path -LiteralPath $_ -PathType Container })
}

function Resolve-BannerlordRoot {
    param([string]$RequestedPath)

    if (Test-BannerlordRoot -Path $RequestedPath) {
        return (Get-FullPathSafe -Path $RequestedPath)
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($library in Get-SteamLibraries) {
        Add-UniquePath -Paths $candidates -Path (Join-Path $library "steamapps\common\Mount & Blade II Bannerlord")
    }

    foreach ($candidate in $candidates) {
        if (Test-BannerlordRoot -Path $candidate) {
            return (Get-FullPathSafe -Path $candidate)
        }
    }

    throw "Bannerlord root not found. Set BANNERLORD_ROOT to your 'Mount & Blade II Bannerlord' folder and retry."
}

function Resolve-WorkshopContentDir {
    param(
        [string]$RequestedPath,
        [Parameter(Mandatory = $true)][string]$ResolvedBannerlordRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $requestedFull = Get-FullPathSafe -Path $RequestedPath
        if (Test-Path -LiteralPath $requestedFull -PathType Container) {
            return $requestedFull
        }
    }

    $commonDir = Split-Path -Parent $ResolvedBannerlordRoot
    $steamAppsDir = Split-Path -Parent $commonDir
    $sameLibraryWorkshop = Join-Path $steamAppsDir "workshop\content\261550"
    if (Test-Path -LiteralPath $sameLibraryWorkshop -PathType Container) {
        return (Get-FullPathSafe -Path $sameLibraryWorkshop)
    }

    foreach ($library in Get-SteamLibraries) {
        $candidate = Join-Path $library "steamapps\workshop\content\261550"
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return (Get-FullPathSafe -Path $candidate)
        }
    }

    return ""
}

$resolvedBannerlordRoot = Resolve-BannerlordRoot -RequestedPath $BannerlordRoot
$resolvedWorkshopContentDir = Resolve-WorkshopContentDir -RequestedPath $WorkshopContentDir -ResolvedBannerlordRoot $resolvedBannerlordRoot

Write-Output ("BANNERLORD_ROOT={0}" -f $resolvedBannerlordRoot)
if (-not [string]::IsNullOrWhiteSpace($resolvedWorkshopContentDir)) {
    Write-Output ("WORKSHOP_CONTENT_DIR={0}" -f $resolvedWorkshopContentDir)
}
