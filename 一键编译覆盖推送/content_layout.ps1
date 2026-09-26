function Get-AnimusForgeContentFullPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Assert-AnimusForgeRelativeContentPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or
        [System.IO.Path]::IsPathRooted($Path) -or
        $Path.StartsWith("/", [System.StringComparison]::Ordinal) -or
        $Path.StartsWith("\\", [System.StringComparison]::Ordinal) -or
        $Path.Contains(":")) {
        throw "$Label must be a relative path without an alternate data stream: $Path"
    }

    $segments = @($Path -split '[\\/]')
    $invalidSegments = @($segments | Where-Object { $_ -eq "" -or $_ -eq "." -or $_ -eq ".." })
    if ($segments.Count -eq 0 -or $invalidSegments.Count -gt 0) {
        throw "$Label contains an invalid path segment: $Path"
    }
}

function Assert-AnimusForgePathUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $pathFull = (Get-AnimusForgeContentFullPath -Path $Path).TrimEnd('\', '/')
    $rootFull = (Get-AnimusForgeContentFullPath -Path $Root).TrimEnd('\', '/')
    if (-not $pathFull.StartsWith($rootFull + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label escapes its allowed root: $pathFull"
    }
}

function Assert-AnimusForgeNoReparsePoint {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $fullPath = Get-AnimusForgeContentFullPath -Path $Path
    $pathRoot = [System.IO.Path]::GetPathRoot($fullPath)
    $current = $pathRoot
    foreach ($segment in @($fullPath.Substring($pathRoot.Length) -split '[\\/]') | Where-Object { $_ }) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            continue
        }

        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label contains a reparse point: $current"
        }
    }
}

function Assert-AnimusForgeProjectionTargetPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = Get-AnimusForgeContentFullPath -Path $Path
    $pathRoot = [System.IO.Path]::GetPathRoot($fullPath)
    $segments = @($fullPath.Substring($pathRoot.Length) -split '[\\/]' | Where-Object { $_ })
    $current = $pathRoot
    for ($index = 0; $index -lt $segments.Count; $index++) {
        $current = Join-Path $current $segments[$index]
        if (-not (Test-Path -LiteralPath $current)) {
            continue
        }
        $item = Get-Item -LiteralPath $current -Force
        $isTarget = $index -eq ($segments.Count - 1)
        if (-not $isTarget -and -not $item.PSIsContainer) {
            throw "Content target parent is not a directory: $current"
        }
        if ($isTarget -and $item.PSIsContainer) {
            throw "Content target is an existing directory: $current"
        }
    }
}

function Get-AnimusForgeContentLayout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [string]$MapPath = ""
    )

    $projectRootFull = Get-AnimusForgeContentFullPath -Path $ProjectRoot
    if (-not (Test-Path -LiteralPath $projectRootFull -PathType Container)) {
        throw "Project root not found: $projectRootFull"
    }
    Assert-AnimusForgeNoReparsePoint -Path $projectRootFull -Label "Project root"

    if ([string]::IsNullOrWhiteSpace($MapPath)) {
        $MapPath = Join-Path $projectRootFull "content\content-map.json"
    }
    $mapPathFull = Get-AnimusForgeContentFullPath -Path $MapPath
    Assert-AnimusForgePathUnderRoot -Path $mapPathFull -Root $projectRootFull -Label "Content map"
    Assert-AnimusForgeNoReparsePoint -Path $mapPathFull -Label "Content map"
    if (-not (Test-Path -LiteralPath $mapPathFull -PathType Leaf)) {
        throw "Content map not found: $mapPathFull"
    }

    try {
        $map = Get-Content -LiteralPath $mapPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Content map is not valid JSON: $mapPathFull"
    }
    if ($null -eq $map -or $map.schemaVersion -ne 1) {
        throw "Unsupported content map schema version: $mapPathFull"
    }
    $entries = @($map.entries)
    if ($entries.Count -eq 0) {
        throw "Content map has no entries: $mapPathFull"
    }

    $targets = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $logicalNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $validated = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in $entries) {
        $owner = [string]$entry.owner
        $source = [string]$entry.source
        $target = [string]$entry.target
        $logicalNameProperty = $entry.PSObject.Properties["logicalName"]
        $logicalName = if ($null -eq $logicalNameProperty) { "" } else { [string]$logicalNameProperty.Value }
        if ([string]::IsNullOrWhiteSpace($owner)) {
            throw "Content map owner is required."
        }
        Assert-AnimusForgeRelativeContentPath -Path $source -Label "Content source"
        Assert-AnimusForgeRelativeContentPath -Path $target -Label "Content target"
        if (-not $targets.Add($target)) {
            throw "Duplicate content target: $target"
        }
        if (-not [string]::IsNullOrWhiteSpace($logicalName) -and -not $logicalNames.Add($logicalName)) {
            throw "Duplicate embedded logical name: $logicalName"
        }

        $sourcePath = Get-AnimusForgeContentFullPath -Path (Join-Path $projectRootFull ($source -replace '/', [System.IO.Path]::DirectorySeparatorChar))
        Assert-AnimusForgePathUnderRoot -Path $sourcePath -Root $projectRootFull -Label "Content source"
        Assert-AnimusForgeNoReparsePoint -Path $sourcePath -Label "Content source"
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Content source not found: $source"
        }

        $validated.Add([PSCustomObject]@{
            Owner = $owner
            Source = $source
            SourcePath = $sourcePath
            Target = $target
            LogicalName = $logicalName
        })
    }

    return $validated
}

function Get-AnimusForgeContentSourcePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$Target,
        [string]$MapPath = ""
    )

    $matches = @(Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot -MapPath $MapPath | Where-Object {
        $_.Target.Equals($Target, [System.StringComparison]::OrdinalIgnoreCase)
    })
    if ($matches.Count -ne 1) {
        throw "Content target was not found exactly once: $Target"
    }
    return $matches[0].SourcePath
}

function Invoke-AnimusForgeContentProjection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$DestinationModuleDir,
        [string]$MapPath = ""
    )

    $layout = @(Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot -MapPath $MapPath)
    $destinationFull = Get-AnimusForgeContentFullPath -Path $DestinationModuleDir
    Assert-AnimusForgeNoReparsePoint -Path $destinationFull -Label "Content destination"

    $projection = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in $layout) {
        $targetPath = Get-AnimusForgeContentFullPath -Path (Join-Path $destinationFull ($entry.Target -replace '/', [System.IO.Path]::DirectorySeparatorChar))
        Assert-AnimusForgePathUnderRoot -Path $targetPath -Root $destinationFull -Label "Content target"
        Assert-AnimusForgeNoReparsePoint -Path $targetPath -Label "Content target"
        Assert-AnimusForgeProjectionTargetPath -Path $targetPath
        $projection.Add([PSCustomObject]@{
            Entry = $entry
            TargetPath = $targetPath
            SourceHash = (Get-FileHash -LiteralPath $entry.SourcePath -Algorithm SHA256).Hash
        })
    }

    foreach ($item in $projection) {
        $targetParent = Split-Path -Parent $item.TargetPath
        New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        Copy-Item -LiteralPath $item.Entry.SourcePath -Destination $item.TargetPath -Force
        $targetHash = (Get-FileHash -LiteralPath $item.TargetPath -Algorithm SHA256).Hash
        if (-not $targetHash.Equals($item.SourceHash, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Projected content hash mismatch: $($item.Entry.Target)"
        }
        [PSCustomObject]@{
            Owner = $item.Entry.Owner
            Source = $item.Entry.Source
            SourcePath = $item.Entry.SourcePath
            Target = $item.Entry.Target
            TargetPath = $item.TargetPath
            LogicalName = $item.Entry.LogicalName
            Sha256 = $targetHash
        }
    }
}
