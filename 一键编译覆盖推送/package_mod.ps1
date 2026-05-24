param(
    [string]$ModuleDir = "",
    [string]$BannerlordRoot = "",
    [string]$OutputDir = "$PSScriptRoot\packages",
    [string]$Version,
    [string]$PackageLabel,
    [switch]$UseFirstMatch,
    [switch]$NoBump,
    [switch]$IncludeOnnx,
    [switch]$IncludeReranker,
    [switch]$ExcludeOnnx,
    [switch]$DualClientPackages
)

$ErrorActionPreference = "Stop"
$VersionPattern = "^(?<prefix>v?)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$"

function Parse-Version {
    param(
        [Parameter(Mandatory = $true)][string]$VersionText,
        [string]$Label = "Version"
    )

    if ($VersionText -notmatch $VersionPattern) {
        throw "$Label format invalid: '$VersionText'. Expected: 1.2.3 or v1.2.3"
    }

    return [PSCustomObject]@{
        Prefix = $Matches["prefix"]
        Major  = [int]$Matches["major"]
        Minor  = [int]$Matches["minor"]
        Patch  = [int]$Matches["patch"]
    }
}

function Get-NextPatchVersion {
    param([string]$CurrentVersion)

    $parts = Parse-Version -VersionText $CurrentVersion -Label "Current version"
    $major = $parts.Major
    $minor = $parts.Minor
    $patch = $parts.Patch + 1

    if ($patch -ge 10) {
        $patch = 0
        $minor += 1
    }
    if ($minor -ge 10) {
        $minor = 0
        $major += 1
    }

    return "$($parts.Prefix)$major.$minor.$patch"
}

function Test-AnimusForgeModuleDir {
    param([Parameter(Mandatory = $true)][string]$Path)

    $requiredEntries = @("bin", "SubModule.xml", "ModuleData", "GUI", "PlayerExports")
    $missing = New-Object System.Collections.Generic.List[string]

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        $missing.Add("Module folder")
        return [PSCustomObject]@{
            IsValid = $false
            Missing = @($missing)
        }
    }

    foreach ($entry in $requiredEntries) {
        if (-not (Test-Path -LiteralPath (Join-Path $Path $entry))) {
            $missing.Add($entry)
        }
    }

    $subModulePathLocal = Join-Path $Path "SubModule.xml"
    if (-not $missing.Contains("SubModule.xml")) {
        try {
            [xml]$subXml = Get-Content -LiteralPath $subModulePathLocal
            $moduleId = [string]$subXml.Module.Id.value
            $validModuleIds = @("AnimusForge", "AnimusForge_1_3_x", "AnimusForge_1_4_5")
            if ($validModuleIds -notcontains $moduleId) {
                $missing.Add("SubModule.xml: Module/Id must be AnimusForge, AnimusForge_1_3_x, or AnimusForge_1_4_5")
            }
        } catch {
            $missing.Add("SubModule.xml: invalid XML")
        }
    }

    return [PSCustomObject]@{
        IsValid = ($missing.Count -eq 0)
        Missing = @($missing)
    }
}

function Resolve-AnimusForgeModuleDir {
    param(
        [string]$RequestedPath,
        [string]$BannerlordRootPath,
        [switch]$AllowFirstMatch
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $requestedFull = [System.IO.Path]::GetFullPath($RequestedPath)
        $check = Test-AnimusForgeModuleDir -Path $requestedFull
        if (-not $check.IsValid) {
            throw ("ModuleDir is not a valid AnimusForge module: {0}`nMissing/Invalid: {1}" -f $requestedFull, ($check.Missing -join ", "))
        }
        return [PSCustomObject]@{
            Path = $requestedFull
            AutoDetected = $false
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($BannerlordRootPath)) {
        $bannerlordRootFull = [System.IO.Path]::GetFullPath($BannerlordRootPath)
        if (-not (Test-Path -LiteralPath $bannerlordRootFull -PathType Container)) {
            throw "Bannerlord root not found: $bannerlordRootFull"
        }

        $modulesDir = Join-Path $bannerlordRootFull "Modules"
        $preferredPaths = @(
            (Join-Path $modulesDir "AnimusForge_1_4_5"),
            (Join-Path $modulesDir "AnimusForge_1_3_x"),
            (Join-Path $modulesDir "AnimusForge")
        )
        foreach ($preferredPath in $preferredPaths) {
            $check = Test-AnimusForgeModuleDir -Path $preferredPath
            if ($check.IsValid) {
                return [PSCustomObject]@{
                    Path = [System.IO.Path]::GetFullPath($preferredPath)
                    AutoDetected = $false
                }
            }
        }

        throw ("BannerlordRoot does not contain a valid AnimusForge module: {0}`nChecked: {1}" -f $bannerlordRootFull, ($preferredPaths -join ", "))
    }

    $candidatePaths = New-Object System.Collections.Generic.List[string]
    $roots = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
    foreach ($rootRaw in $roots) {
        $root = $rootRaw.TrimEnd('\', '/')
        foreach ($moduleId in @("AnimusForge_1_4_5", "AnimusForge_1_3_x", "AnimusForge")) {
            $candidatePaths.Add($root + "\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\$moduleId")
            $candidatePaths.Add($root + "\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\$moduleId")
            $candidatePaths.Add($root + "\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\$moduleId")
        }
    }

    $validCandidates = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in ($candidatePaths | Sort-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            continue
        }
        $check = Test-AnimusForgeModuleDir -Path $candidate
        if ($check.IsValid) {
            $validCandidates.Add([System.IO.Path]::GetFullPath($candidate))
        }
    }

    if ($validCandidates.Count -eq 0) {
        throw "Auto-detect failed: no valid AnimusForge module found. You can pass -ModuleDir explicitly."
    }
    if ($validCandidates.Count -gt 1 -and -not $AllowFirstMatch) {
        $list = ($validCandidates | ForEach-Object { " - $_" }) -join "`r`n"
        throw "Auto-detect found multiple AnimusForge modules. Please pass -ModuleDir explicitly or use -UseFirstMatch.`r`n$list"
    }

    return [PSCustomObject]@{
        Path = $validCandidates[0]
        AutoDetected = $true
    }
}

Add-Type -AssemblyName "System.IO.Compression"
Add-Type -AssemblyName "System.IO.Compression.FileSystem"

function Get-BannerlordModulesDir {
    param([Parameter(Mandatory = $true)][string]$BannerlordRootPath)

    $bannerlordRootFull = [System.IO.Path]::GetFullPath($BannerlordRootPath)
    if (-not (Test-Path -LiteralPath $bannerlordRootFull -PathType Container)) {
        throw "Bannerlord root not found: $bannerlordRootFull"
    }
    $modulesDir = Join-Path $bannerlordRootFull "Modules"
    if (-not (Test-Path -LiteralPath $modulesDir -PathType Container)) {
        throw "Bannerlord Modules directory not found: $modulesDir"
    }
    return [System.IO.Path]::GetFullPath($modulesDir)
}

function Write-ZipFromModule {
    param(
        [Parameter(Mandatory = $true)][string]$ModulePath,
        [bool]$AutoDetected = $false,
        [string]$LabelSuffix = ""
    )

    $moduleDirFullLocal = [System.IO.Path]::GetFullPath($ModulePath).TrimEnd('\', '/')
    $subModulePath = Join-Path $moduleDirFullLocal "SubModule.xml"
    if (-not (Test-Path -LiteralPath $subModulePath)) {
        throw "SubModule.xml not found: $subModulePath"
    }

    [xml]$xml = Get-Content -LiteralPath $subModulePath
    $currentVersion = $xml.Module.Version.value
    if ([string]::IsNullOrWhiteSpace($currentVersion)) {
        throw "Version node is missing in SubModule.xml"
    }
    $null = Parse-Version -VersionText $currentVersion -Label "Current version"

    if ($Version) {
        $null = Parse-Version -VersionText $Version -Label "Target version"
        $newVersion = $Version
    } elseif ($NoBump -or $DualClientPackages) {
        $newVersion = $currentVersion
    } else {
        $newVersion = Get-NextPatchVersion -CurrentVersion $currentVersion
    }

    if ($newVersion -ne $currentVersion) {
        $xml.Module.Version.value = $newVersion
        $settings = New-Object System.Xml.XmlWriterSettings
        $settings.Indent = $true
        $settings.IndentChars = "    "
        $settings.NewLineChars = "`r`n"
        $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
        $settings.OmitXmlDeclaration = $true

        $writer = [System.Xml.XmlWriter]::Create($subModulePath, $settings)
        try {
            $xml.Save($writer)
        } finally {
            $writer.Dispose()
        }
    }

    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    $outputDirFull = [System.IO.Path]::GetFullPath($OutputDir).TrimEnd('\', '/')
    $isOutputInsideModule = $outputDirFull.StartsWith($moduleDirFullLocal + "\", [System.StringComparison]::OrdinalIgnoreCase) -or
        $outputDirFull.Equals($moduleDirFullLocal, [System.StringComparison]::OrdinalIgnoreCase)
    $onnxDirFull = [System.IO.Path]::GetFullPath((Join-Path $moduleDirFullLocal "ONNX")).TrimEnd('\', '/')
    $rerankerDirFull = [System.IO.Path]::GetFullPath((Join-Path $moduleDirFullLocal "ONNX\reranker")).TrimEnd('\', '/')

    $moduleName = Split-Path -Path $moduleDirFullLocal -Leaf
    $versionForName = ($newVersion -replace "[^\w\.\-]", "_")
    $forceExcludeOnnx = $ExcludeOnnx -or $DualClientPackages
    $effectiveIncludeOnnx = $IncludeOnnx -and -not $forceExcludeOnnx
    $effectiveIncludeReranker = $IncludeReranker -and -not $forceExcludeOnnx
    $labelForName = ""
    if (-not [string]::IsNullOrWhiteSpace($PackageLabel)) {
        $labelForName = "_" + ($PackageLabel -replace "[^\w\.\-]", "_")
    }
    if (-not [string]::IsNullOrWhiteSpace($LabelSuffix)) {
        $labelForName += "_" + ($LabelSuffix -replace "[^\w\.\-]", "_")
    }
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
    $zipBaseName = "$moduleName`_$versionForName$labelForName`_$timestamp"
    $zipPath = Join-Path $OutputDir ($zipBaseName + ".zip")
    $suffix = 1
    while (Test-Path -LiteralPath $zipPath) {
        $zipPath = Join-Path $OutputDir ("{0}_{1}.zip" -f $zipBaseName, $suffix)
        $suffix += 1
    }

    $mode = [System.IO.Compression.ZipArchiveMode]::Create
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, $mode)

    try {
        $files = Get-ChildItem -LiteralPath $moduleDirFullLocal -Recurse -File -Force | Where-Object {
            $fullPath = [System.IO.Path]::GetFullPath($_.FullName)
            $isLogFile = $fullPath -match "[\\/]+Logs[\\/]+"
            $isOutputFile = $isOutputInsideModule -and (
                $fullPath.StartsWith($outputDirFull + "\", [System.StringComparison]::OrdinalIgnoreCase) -or
                $fullPath.Equals($outputDirFull, [System.StringComparison]::OrdinalIgnoreCase)
            )
            $isOnnxFile = $fullPath.StartsWith($onnxDirFull + "\", [System.StringComparison]::OrdinalIgnoreCase) -or
                $fullPath.Equals($onnxDirFull, [System.StringComparison]::OrdinalIgnoreCase)
            $isRerankerFile = $fullPath.StartsWith($rerankerDirFull + "\", [System.StringComparison]::OrdinalIgnoreCase) -or
                $fullPath.Equals($rerankerDirFull, [System.StringComparison]::OrdinalIgnoreCase)
            $excludeOnnxFile = $isOnnxFile -and -not $effectiveIncludeOnnx -and (-not $effectiveIncludeReranker -or -not $isRerankerFile)
            -not $isLogFile -and -not $isOutputFile -and -not $excludeOnnxFile
        }

        foreach ($file in $files) {
            $relative = $file.FullName.Substring($moduleDirFullLocal.Length).TrimStart('\', '/')
            $entryName = "$moduleName/$($relative -replace '\\', '/')"
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $file.FullName,
                $entryName,
                $compressionLevel
            ) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }

    Write-Host "Version      : $currentVersion -> $newVersion"
    Write-Host "Module Path  : $moduleDirFullLocal"
    Write-Host "Module Detect: $(if ($AutoDetected) { 'Auto' } else { 'Manual' })"
    Write-Host "Output ZIP   : $zipPath"
    if (-not [string]::IsNullOrWhiteSpace($PackageLabel)) {
        Write-Host "Package Label: $PackageLabel"
    }
    Write-Host "Exclude Rule : Logs/**/* (all files under Logs)"
    Write-Host "ONNX ZIP     : $(if ($forceExcludeOnnx) { 'Excluded by package policy' } elseif ($effectiveIncludeOnnx) { 'Included' } elseif ($effectiveIncludeReranker) { 'Only ONNX/reranker included' } else { 'Excluded by default, pass -IncludeOnnx to include it' })"
    return $zipPath
}

function Assert-ZipDoesNotContainOnnx {
    param([Parameter(Mandatory = $true)][string]$ZipPath)

    $zipFullPath = [System.IO.Path]::GetFullPath($ZipPath)
    if (-not (Test-Path -LiteralPath $zipFullPath -PathType Leaf)) {
        throw "Expected package ZIP was not created: $zipFullPath"
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($zipFullPath)
    try {
        $onnxEntry = $archive.Entries | Where-Object {
            $_.FullName -match '(^|/)ONNX(/|$)'
        } | Select-Object -First 1
        if ($onnxEntry) {
            throw "Package must not contain ONNX files: $zipFullPath entry=$($onnxEntry.FullName)"
        }
    }
    finally {
        $archive.Dispose()
    }
}

if ($DualClientPackages) {
    if ([string]::IsNullOrWhiteSpace($BannerlordRoot)) {
        throw "DualClientPackages requires -BannerlordRoot."
    }
    $modulesDir = Get-BannerlordModulesDir -BannerlordRootPath $BannerlordRoot
    $module13 = Join-Path $modulesDir "AnimusForge_1_3_x"
    $module14 = Join-Path $modulesDir "AnimusForge_1_4_5"
    foreach ($modulePath in @($module13, $module14)) {
        $check = Test-AnimusForgeModuleDir -Path $modulePath
        if (-not $check.IsValid) {
            throw ("Dual client module is not valid: {0}`nMissing/Invalid: {1}" -f $modulePath, ($check.Missing -join ", "))
        }
    }
    Write-Host "Packaging dual client modules..."
    $zip13 = Write-ZipFromModule -ModulePath $module13 -AutoDetected:$false -LabelSuffix "bannerlord_1.3.x"
    $zip14 = Write-ZipFromModule -ModulePath $module14 -AutoDetected:$false -LabelSuffix "bannerlord_1.4.5"
    Assert-ZipDoesNotContainOnnx -ZipPath $zip13
    Assert-ZipDoesNotContainOnnx -ZipPath $zip14
    Write-Host "Dual Packages:"
    Write-Host " - $zip13"
    Write-Host " - $zip14"
    exit 0
}

$resolvedModule = Resolve-AnimusForgeModuleDir -RequestedPath $ModuleDir -BannerlordRootPath $BannerlordRoot -AllowFirstMatch:$UseFirstMatch
$ModuleDir = $resolvedModule.Path
$wasAutoDetected = $resolvedModule.AutoDetected
$null = Write-ZipFromModule -ModulePath $ModuleDir -AutoDetected:$wasAutoDetected
