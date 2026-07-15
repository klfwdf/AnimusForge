param(
    [string]$ModuleDir = "",
    [string]$BannerlordRoot = "",
    [string]$OutputDir = "$PSScriptRoot\packages",
    [string]$SourceModuleDir = "",
    [string]$Version,
    [string]$PackageLabel,
    [switch]$UseFirstMatch,
    [switch]$NoBump,
    [switch]$BumpMicro,
    [switch]$IncludeOnnx,
    [switch]$IncludeReranker,
    [switch]$ExcludeOnnx,
    [switch]$ExcludeCustomPrompts
)

$ErrorActionPreference = "Stop"
if ($IncludeOnnx -or $IncludeReranker) {
    throw "Unified client packages never include the ONNX model folder. Remove -IncludeOnnx/-IncludeReranker."
}
$VersionPattern = "^(?<prefix>v?)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:\.(?<micro>\d))?$"
$ModuleName = "AnimusForge"
$BootstrapAssemblyName = "AnimusForge.Bootstrap"
$BootstrapClassType = "AnimusForge.Bootstrap.BootstrapSubModule"
$FlavorKey = "AnimusForge.BuildFlavor"
$ApiKey = "AnimusForge.BannerlordApi"
$Flavor13 = "ANIMUSFORGE_BANNERLORD_API_1_3"
$Flavor14 = "ANIMUSFORGE_BANNERLORD_API_1_4"
$RequiredBuildMarkerSchemaVersion = 2
$AllowedModuleRootDllNames = @(
    "AnimusForge.Bootstrap.dll",
    "Microsoft.ML.OnnxRuntime.dll",
    "onnxruntime.dll",
    "onnxruntime_providers_shared.dll",
    "System.Buffers.dll",
    "System.Memory.dll",
    "System.Runtime.CompilerServices.Unsafe.dll"
)
$AllowedPackageRootDllNames = @($AllowedModuleRootDllNames)

Add-Type -AssemblyName "System.IO.Compression"
Add-Type -AssemblyName "System.IO.Compression.FileSystem"

function Get-FullPathSafe {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $stream = [System.IO.File]::OpenRead($LiteralPath)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([System.BitConverter]::ToString($sha256.ComputeHash($stream)) -replace "-", "")
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Parse-Version {
    param(
        [Parameter(Mandatory = $true)][string]$VersionText,
        [string]$Label = "Version"
    )

    if ($VersionText -notmatch $VersionPattern) {
        throw "$Label format invalid: '$VersionText'. Expected 1.2.3, v1.2.3, 1.2.3.4, or v1.2.3.4."
    }
    $micro = $null
    if (-not [string]::IsNullOrWhiteSpace($Matches["micro"])) {
        $micro = [int]$Matches["micro"]
    }
    return [PSCustomObject]@{
        Prefix = $Matches["prefix"]
        Major = [int]$Matches["major"]
        Minor = [int]$Matches["minor"]
        Patch = [int]$Matches["patch"]
        Micro = $micro
    }
}

function Get-NextPatchVersion {
    param([Parameter(Mandatory = $true)][string]$CurrentVersion)

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

function Get-NextMicroVersion {
    param([Parameter(Mandatory = $true)][string]$CurrentVersion)

    $parts = Parse-Version -VersionText $CurrentVersion -Label "Current version"
    if ($null -eq $parts.Micro) {
        return "$($parts.Prefix)$($parts.Major).$($parts.Minor).$($parts.Patch).1"
    }
    if ($parts.Micro -lt 9) {
        return "$($parts.Prefix)$($parts.Major).$($parts.Minor).$($parts.Patch).$($parts.Micro + 1)"
    }
    return "$(Get-NextPatchVersion -CurrentVersion $CurrentVersion).0"
}

function Get-SubModuleVersion {
    param([Parameter(Mandatory = $true)][string]$SubModulePath)

    if (-not (Test-Path -LiteralPath $SubModulePath -PathType Leaf)) {
        throw "SubModule.xml not found: $SubModulePath"
    }
    [xml]$xml = Get-Content -Raw -Encoding UTF8 -LiteralPath $SubModulePath
    $currentVersion = [string]$xml.Module.Version.value
    if ([string]::IsNullOrWhiteSpace($currentVersion)) {
        throw "Version node is missing in: $SubModulePath"
    }
    $null = Parse-Version -VersionText $currentVersion -Label "Current version"
    return $currentVersion
}

function Resolve-PackageVersion {
    param([Parameter(Mandatory = $true)][string]$CurrentVersion)

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        $null = Parse-Version -VersionText $Version -Label "Target version"
        return $Version
    }
    if ($NoBump) {
        return $CurrentVersion
    }
    if ($BumpMicro) {
        return Get-NextMicroVersion -CurrentVersion $CurrentVersion
    }
    return Get-NextPatchVersion -CurrentVersion $CurrentVersion
}

function Set-SubModuleVersion {
    param(
        [Parameter(Mandatory = $true)][string]$SubModulePath,
        [Parameter(Mandatory = $true)][string]$NewVersion
    )

    [xml]$xml = Get-Content -Raw -Encoding UTF8 -LiteralPath $SubModulePath
    if ([string]$xml.Module.Version.value -eq $NewVersion) {
        return
    }
    $xml.Module.Version.value = $NewVersion

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $settings.Indent = $true
    $settings.IndentChars = "    "
    $settings.NewLineChars = "`r`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.OmitXmlDeclaration = $true
    $writer = [System.Xml.XmlWriter]::Create($SubModulePath, $settings)
    try {
        $xml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function Get-BuildMarkerPath {
    param([Parameter(Mandatory = $true)][string]$DllPath)

    return (Join-Path (Split-Path -Parent $DllPath) (([System.IO.Path]::GetFileNameWithoutExtension($DllPath)) + ".build.json"))
}

function Test-AssemblyName {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedName
    )

    try {
        return [System.Reflection.AssemblyName]::GetAssemblyName($DllPath).Name -eq $ExpectedName
    }
    catch {
        return $false
    }
}

function Test-BuildMarker {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedRole,
        [string]$ExpectedApi = "",
        [string]$ExpectedFlavor = "",
        [Parameter(Mandatory = $true)][int]$ExpectedReferenceMinor
    )

    $markerPath = Get-BuildMarkerPath -DllPath $DllPath
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        return $false
    }
    try {
        $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
        if ([string]$marker.Role -ne $ExpectedRole -or [string]$marker.Sha256 -ne (Get-FileSha256 -LiteralPath $DllPath)) {
            return $false
        }
        $expectedAssemblyName = if ($ExpectedRole -eq "Bootstrap") { $BootstrapAssemblyName } else { "AnimusForge" }
        $referenceVersion = [string]$marker.ReferenceGameVersion
        $createdUtc = [string]$marker.CreatedUtc
        $createdTimestamp = [DateTimeOffset]::MinValue
        if ([int]$marker.SchemaVersion -ne $RequiredBuildMarkerSchemaVersion -or
            [string]$marker.FileName -ne [System.IO.Path]::GetFileName($DllPath) -or
            [string]$marker.AssemblyName -ne $expectedAssemblyName -or
            $referenceVersion -notmatch ("^v?1\." + $ExpectedReferenceMinor + "\.\d+\.\d+$") -or
            -not [DateTimeOffset]::TryParse($createdUtc, [ref]$createdTimestamp)) {
            return $false
        }
        if (-not [string]::IsNullOrWhiteSpace($ExpectedApi) -and [string]$marker.BannerlordApi -ne $ExpectedApi) {
            return $false
        }
        if (-not [string]::IsNullOrWhiteSpace($ExpectedFlavor) -and [string]$marker.BuildFlavor -ne $ExpectedFlavor) {
            return $false
        }
        return $true
    }
    catch {
        return $false
    }
}

function Test-ImplementationDll {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedApi,
        [Parameter(Mandatory = $true)][string]$ExpectedFlavor,
        [Parameter(Mandatory = $true)][string]$UnexpectedFlavor
    )

    if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf) -or -not (Test-AssemblyName -DllPath $DllPath -ExpectedName "AnimusForge")) {
        return $false
    }
    $binaryText = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($DllPath))
    foreach ($requiredText in @($FlavorKey, $ApiKey, $ExpectedApi, $ExpectedFlavor)) {
        if ($binaryText.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
            return $false
        }
    }
    if ($binaryText.IndexOf($UnexpectedFlavor, [System.StringComparison]::Ordinal) -ge 0) {
        return $false
    }
    $expectedReferenceMinor = if ($ExpectedApi -eq "1.3") { 3 } else { 4 }
    return (Test-BuildMarker -DllPath $DllPath -ExpectedRole "Implementation" -ExpectedApi $ExpectedApi -ExpectedFlavor $ExpectedFlavor -ExpectedReferenceMinor $expectedReferenceMinor)
}

function Test-AnimusForgeModuleDir {
    param([Parameter(Mandatory = $true)][string]$Path)

    $missing = New-Object System.Collections.Generic.List[string]
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        $missing.Add("Module folder")
        return [PSCustomObject]@{ IsValid = $false; Missing = @($missing) }
    }
    if (-not (Split-Path -Leaf (Get-FullPathSafe -Path $Path)).Equals($ModuleName, [System.StringComparison]::Ordinal)) {
        $missing.Add("folder name must be AnimusForge")
    }
    foreach ($entry in @("bin", "SubModule.xml", "ModuleData", "GUI", "PlayerExports")) {
        if (-not (Test-Path -LiteralPath (Join-Path $Path $entry))) {
            $missing.Add($entry)
        }
    }

    $binRoot = Join-Path $Path "bin"
    $binDir = Join-Path $binRoot "Win64_Shipping_Client"
    if (Test-Path -LiteralPath $binRoot -PathType Container) {
        foreach ($entry in (Get-ChildItem -LiteralPath $binRoot -Force)) {
            if (-not $entry.PSIsContainer -or -not $entry.Name.Equals("Win64_Shipping_Client", [System.StringComparison]::Ordinal)) {
                $missing.Add("unexpected bin entry: $($entry.Name)")
            }
        }
    }
    $bootstrap = Join-Path $binDir "AnimusForge.Bootstrap.dll"
    $dll13 = Join-Path $binDir "versions\1.3\AnimusForge.dll"
    $dll14 = Join-Path $binDir "versions\1.4\AnimusForge.dll"
    if (-not (Test-Path -LiteralPath $bootstrap -PathType Leaf) -or -not (Test-AssemblyName -DllPath $bootstrap -ExpectedName $BootstrapAssemblyName) -or -not (Test-BuildMarker -DllPath $bootstrap -ExpectedRole "Bootstrap" -ExpectedReferenceMinor 3)) {
        $missing.Add("valid AnimusForge.Bootstrap.dll + build marker")
    }
    if (-not (Test-ImplementationDll -DllPath $dll13 -ExpectedApi "1.3" -ExpectedFlavor $Flavor13 -UnexpectedFlavor $Flavor14)) {
        $missing.Add("valid versions/1.3 implementation + build marker")
    }
    if (-not (Test-ImplementationDll -DllPath $dll14 -ExpectedApi "1.4" -ExpectedFlavor $Flavor14 -UnexpectedFlavor $Flavor13)) {
        $missing.Add("valid versions/1.4 implementation + build marker")
    }
    if (Test-Path -LiteralPath (Join-Path $binDir "AnimusForge.dll") -PathType Leaf) {
        $missing.Add("legacy root AnimusForge.dll must not exist")
    }
    foreach ($requiredRuntimeDll in $AllowedModuleRootDllNames) {
        if (-not (Test-Path -LiteralPath (Join-Path $binDir $requiredRuntimeDll) -PathType Leaf)) {
            $missing.Add("required root runtime DLL: $requiredRuntimeDll")
        }
    }
    foreach ($requiredPdb in @(
        (Join-Path $binDir "AnimusForge.Bootstrap.pdb"),
        (Join-Path $binDir "versions\1.3\AnimusForge.pdb"),
        (Join-Path $binDir "versions\1.4\AnimusForge.pdb")
    )) {
        if (-not (Test-Path -LiteralPath $requiredPdb -PathType Leaf)) {
            $missing.Add("required PDB: $requiredPdb")
        }
    }

    if (Test-Path -LiteralPath $binDir -PathType Container) {
        $allowedShippingRootFiles = @($AllowedModuleRootDllNames) + @(
            "AnimusForge.Bootstrap.pdb",
            "AnimusForge.Bootstrap.build.json"
        )
        foreach ($entry in (Get-ChildItem -LiteralPath $binDir -Force)) {
            if ($entry.PSIsContainer) {
                if (-not $entry.Name.Equals("versions", [System.StringComparison]::Ordinal)) {
                    $missing.Add("unexpected shipping-bin directory: $($entry.Name)")
                }
            }
            elseif ($allowedShippingRootFiles -notcontains $entry.Name) {
                $missing.Add("unexpected shipping-bin file: $($entry.Name)")
            }
        }
    }

    $versionsDir = Join-Path $binDir "versions"
    $allowedVersionFiles = @(
        "1.3\AnimusForge.dll",
        "1.3\AnimusForge.pdb",
        "1.3\AnimusForge.build.json",
        "1.4\AnimusForge.dll",
        "1.4\AnimusForge.pdb",
        "1.4\AnimusForge.build.json"
    )
    if (Test-Path -LiteralPath $versionsDir -PathType Container) {
        foreach ($directory in (Get-ChildItem -LiteralPath $versionsDir -Recurse -Directory -Force)) {
            $relativeDirectory = $directory.FullName.Substring($versionsDir.Length).TrimStart('\', '/') -replace '/', '\'
            if ($relativeDirectory -notin @("1.3", "1.4")) {
                $missing.Add("unexpected versions directory: $relativeDirectory")
            }
        }
        foreach ($file in (Get-ChildItem -LiteralPath $versionsDir -Recurse -File -Force)) {
            $relativeFile = $file.FullName.Substring($versionsDir.Length).TrimStart('\', '/') -replace '/', '\'
            if ($allowedVersionFiles -notcontains $relativeFile) {
                $missing.Add("unexpected versions file: $relativeFile")
            }
        }
    }

    if (Test-Path -LiteralPath $binDir -PathType Container) {
        foreach ($dll in (Get-ChildItem -LiteralPath $binDir -Recurse -File -Filter "*.dll" -Force)) {
            $relativeDll = $dll.FullName.Substring($binDir.Length).TrimStart('\', '/') -replace '/', '\'
            $isExpectedImplementation = $relativeDll -in @(
                "versions\1.3\AnimusForge.dll",
                "versions\1.4\AnimusForge.dll"
            )
            $isExpectedBootstrap = $relativeDll -eq "AnimusForge.Bootstrap.dll"
            if ($relativeDll.IndexOf('\') -lt 0 -and $AllowedModuleRootDllNames -notcontains $dll.Name) {
                $missing.Add("unexpected root runtime DLL: $relativeDll")
                continue
            }
            if ($relativeDll.IndexOf('\') -ge 0 -and
                -not $relativeDll.StartsWith("versions\", [System.StringComparison]::OrdinalIgnoreCase)) {
                $missing.Add("unexpected nested runtime DLL: $relativeDll")
                continue
            }
            if ($dll.Name -match '^(TaleWorlds\.|SandBox(?:\.|$)|StoryMode(?:\.|$)|Native\.dll$|CustomBattle\.dll$)') {
                $missing.Add("game-owned DLL in module bin: $relativeDll")
                continue
            }
            if ($relativeDll.IndexOf('\') -lt 0 -and $dll.Name -like "AnimusForge*.dll" -and -not $isExpectedBootstrap) {
                $missing.Add("unexpected root implementation DLL: $relativeDll")
                continue
            }
            if ($relativeDll.StartsWith("versions\", [System.StringComparison]::OrdinalIgnoreCase) -and -not $isExpectedImplementation) {
                $missing.Add("unexpected implementation DLL in versions: $relativeDll")
                continue
            }

            try {
                $managedName = [System.Reflection.AssemblyName]::GetAssemblyName($dll.FullName).Name
                if (($managedName -eq "AnimusForge" -and -not $isExpectedImplementation) -or
                    ($managedName -eq $BootstrapAssemblyName -and -not $isExpectedBootstrap) -or
                    $managedName -match '^(TaleWorlds\.|SandBox(?:\.|$)|StoryMode(?:\.|$)|Native$|CustomBattle$)') {
                    $missing.Add("unexpected managed assembly in module bin: $relativeDll ($managedName)")
                }
            }
            catch {
                # Native runtime DLLs are allowed outside versions; filename checks above still apply.
            }
        }
    }

    if ((Test-Path -LiteralPath $dll13 -PathType Leaf) -and (Test-Path -LiteralPath $dll14 -PathType Leaf)) {
        if ((Get-FileSha256 -LiteralPath $dll13) -eq (Get-FileSha256 -LiteralPath $dll14)) {
            $missing.Add("1.3 and 1.4 implementation DLL hashes must differ")
        }
    }

    $subModulePath = Join-Path $Path "SubModule.xml"
    if (Test-Path -LiteralPath $subModulePath -PathType Leaf) {
        try {
            [xml]$xml = Get-Content -Raw -Encoding UTF8 -LiteralPath $subModulePath
            $subModules = @($xml.Module.SubModules.SubModule)
            $assemblies = @($xml.Module.Assemblies.Assembly)
            if ([string]$xml.Module.Id.value -ne $ModuleName -or [string]$xml.Module.Name.value -ne $ModuleName) {
                $missing.Add("SubModule.xml unified Id/Name")
            }
            if ($subModules.Count -ne 1 -or [string]$subModules[0].DLLName.value -ne "$BootstrapAssemblyName.dll" -or [string]$subModules[0].SubModuleClassType.value -ne $BootstrapClassType) {
                $missing.Add("SubModule.xml Bootstrap entry point")
            }
            if ($assemblies.Count -ne 1 -or [string]$assemblies[0].value -ne "$BootstrapAssemblyName.dll") {
                $missing.Add("SubModule.xml Bootstrap-only Assemblies")
            }
        }
        catch {
            $missing.Add("SubModule.xml valid XML")
        }
    }

    return [PSCustomObject]@{ IsValid = ($missing.Count -eq 0); Missing = @($missing) }
}

function Resolve-AnimusForgeModuleDir {
    param(
        [string]$RequestedPath,
        [string]$BannerlordRootPath,
        [switch]$AllowFirstMatch
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $requestedFull = Get-FullPathSafe -Path $RequestedPath
        $check = Test-AnimusForgeModuleDir -Path $requestedFull
        if (-not $check.IsValid) {
            throw "ModuleDir is not a valid unified AnimusForge module: $requestedFull`nMissing/Invalid: $($check.Missing -join ', ')"
        }
        return [PSCustomObject]@{ Path = $requestedFull; AutoDetected = $false }
    }

    if (-not [string]::IsNullOrWhiteSpace($BannerlordRootPath)) {
        $candidate = Join-Path (Get-FullPathSafe -Path $BannerlordRootPath) "Modules\AnimusForge"
        $check = Test-AnimusForgeModuleDir -Path $candidate
        if (-not $check.IsValid) {
            throw "BannerlordRoot does not contain a valid unified AnimusForge module: $candidate`nMissing/Invalid: $($check.Missing -join ', ')"
        }
        return [PSCustomObject]@{ Path = Get-FullPathSafe -Path $candidate; AutoDetected = $false }
    }

    $validCandidates = New-Object System.Collections.Generic.List[string]
    foreach ($rootRaw in (Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root)) {
        $root = $rootRaw.TrimEnd('\', '/')
        foreach ($candidate in @(
            "$root\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge",
            "$root\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge",
            "$root\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge"
        )) {
            $check = Test-AnimusForgeModuleDir -Path $candidate
            if ($check.IsValid) {
                $validCandidates.Add((Get-FullPathSafe -Path $candidate))
            }
        }
    }
    $validCandidates = @($validCandidates | Sort-Object -Unique)
    if ($validCandidates.Count -eq 0) {
        throw "Auto-detect failed: no valid unified AnimusForge module was found. Pass -ModuleDir explicitly."
    }
    if ($validCandidates.Count -gt 1 -and -not $AllowFirstMatch) {
        throw "Auto-detect found multiple unified modules. Pass -ModuleDir or -UseFirstMatch.`n$($validCandidates -join "`n")"
    }
    return [PSCustomObject]@{ Path = $validCandidates[0]; AutoDetected = $true }
}

function Get-RequiredZipEntry {
    param(
        [Parameter(Mandatory = $true)][System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory = $true)][string]$EntryName
    )

    $matches = @($Archive.Entries | Where-Object { $_.FullName -ceq $EntryName })
    if ($matches.Count -ne 1) {
        throw "ZIP must contain exactly one '$EntryName' entry; found $($matches.Count)."
    }
    return $matches[0]
}

function Read-ZipEntryText {
    param([Parameter(Mandatory = $true)][System.IO.Compression.ZipArchiveEntry]$Entry)

    $stream = $Entry.Open()
    try {
        $reader = New-Object System.IO.StreamReader($stream, (New-Object System.Text.UTF8Encoding($false, $true)), $true)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-ZipEntrySha256 {
    param([Parameter(Mandatory = $true)][System.IO.Compression.ZipArchiveEntry]$Entry)

    $stream = $Entry.Open()
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            return ([System.BitConverter]::ToString($sha256.ComputeHash($stream)) -replace "-", "")
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Assert-ZipBuildMarker {
    param(
        [Parameter(Mandatory = $true)][System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory = $true)][string]$DllEntryName,
        [Parameter(Mandatory = $true)][string]$MarkerEntryName,
        [Parameter(Mandatory = $true)][string]$ExpectedRole,
        [Parameter(Mandatory = $true)][string]$ExpectedAssemblyName,
        [string]$ExpectedApi = "",
        [string]$ExpectedFlavor = "",
        [Parameter(Mandatory = $true)][int]$ExpectedReferenceMinor
    )

    $dllEntry = Get-RequiredZipEntry -Archive $Archive -EntryName $DllEntryName
    $markerEntry = Get-RequiredZipEntry -Archive $Archive -EntryName $MarkerEntryName
    try {
        $marker = Read-ZipEntryText -Entry $markerEntry | ConvertFrom-Json
    }
    catch {
        throw "ZIP build marker is invalid JSON: $MarkerEntryName"
    }

    $actualHash = Get-ZipEntrySha256 -Entry $dllEntry
    $referenceVersion = [string]$marker.ReferenceGameVersion
    $createdUtc = [string]$marker.CreatedUtc
    $createdTimestamp = [DateTimeOffset]::MinValue
    if ([int]$marker.SchemaVersion -ne $RequiredBuildMarkerSchemaVersion -or
        [string]$marker.Role -ne $ExpectedRole -or
        [string]$marker.FileName -ne [System.IO.Path]::GetFileName($DllEntryName) -or
        [string]$marker.AssemblyName -ne $ExpectedAssemblyName -or
        -not ([string]$marker.Sha256).Equals($actualHash, [System.StringComparison]::OrdinalIgnoreCase) -or
        $referenceVersion -notmatch ("^v?1\." + $ExpectedReferenceMinor + "\.\d+\.\d+$") -or
        -not [DateTimeOffset]::TryParse($createdUtc, [ref]$createdTimestamp)) {
        throw "ZIP build marker does not match its DLL: $MarkerEntryName"
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedApi) -and [string]$marker.BannerlordApi -ne $ExpectedApi) {
        throw "ZIP build marker API mismatch: $MarkerEntryName"
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedFlavor) -and [string]$marker.BuildFlavor -ne $ExpectedFlavor) {
        throw "ZIP build marker flavor mismatch: $MarkerEntryName"
    }
    return $actualHash
}

function Assert-ZipLayout {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$ExpectedVersion,
        [bool]$OnnxMustBeAbsent,
        [bool]$CustomPromptsMustBeAbsent
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName })
        $outsideRoot = $names | Where-Object { -not $_.StartsWith("AnimusForge/", [System.StringComparison]::Ordinal) }
        if ($outsideRoot) {
            throw "ZIP contains an entry outside the AnimusForge root: $($outsideRoot[0])"
        }

        $subModuleEntryName = "AnimusForge/SubModule.xml"
        $bootstrapDllEntryName = "AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.dll"
        $bootstrapPdbEntryName = "AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.pdb"
        $bootstrapMarkerEntryName = "AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.build.json"
        $dll13EntryName = "AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll"
        $pdb13EntryName = "AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.pdb"
        $marker13EntryName = "AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.build.json"
        $dll14EntryName = "AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll"
        $pdb14EntryName = "AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.pdb"
        $marker14EntryName = "AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.build.json"
        $shippingPrefix = "AnimusForge/bin/Win64_Shipping_Client/"
        $requiredEntries = @(
            $subModuleEntryName,
            $bootstrapDllEntryName,
            $bootstrapPdbEntryName,
            $bootstrapMarkerEntryName,
            $dll13EntryName,
            $pdb13EntryName,
            $marker13EntryName,
            $dll14EntryName,
            $pdb14EntryName,
            $marker14EntryName
        )
        foreach ($required in $requiredEntries) {
            $null = Get-RequiredZipEntry -Archive $archive -EntryName $required
        }
        foreach ($requiredRuntimeDll in $AllowedPackageRootDllNames) {
            $null = Get-RequiredZipEntry -Archive $archive -EntryName ($shippingPrefix + $requiredRuntimeDll)
        }

        $allowedVersionEntries = @(
            $dll13EntryName,
            $pdb13EntryName,
            $marker13EntryName,
            $dll14EntryName,
            $pdb14EntryName,
            $marker14EntryName
        )
        $versionPrefix = "AnimusForge/bin/Win64_Shipping_Client/versions/"
        $unexpectedVersionEntry = $names | Where-Object {
            $_.StartsWith($versionPrefix, [System.StringComparison]::OrdinalIgnoreCase) -and $allowedVersionEntries -cnotcontains $_
        } | Select-Object -First 1
        if ($unexpectedVersionEntry) {
            throw "ZIP contains an unexpected versions entry: $unexpectedVersionEntry"
        }

        $allowedShippingRootEntries = @($AllowedPackageRootDllNames | ForEach-Object { $shippingPrefix + $_ }) + @(
            $bootstrapPdbEntryName,
            $bootstrapMarkerEntryName
        )
        $unexpectedShippingEntry = $names | Where-Object {
            if (-not $_.StartsWith($shippingPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $false
            }
            $relativeEntry = $_.Substring($shippingPrefix.Length)
            return $relativeEntry.IndexOf('/') -lt 0 -and $allowedShippingRootEntries -cnotcontains $_
        } | Select-Object -First 1
        if ($unexpectedShippingEntry) {
            throw "ZIP contains an unexpected shipping-bin root entry: $unexpectedShippingEntry"
        }

        foreach ($dllEntryName in ($names | Where-Object { $_.StartsWith($shippingPrefix, [System.StringComparison]::OrdinalIgnoreCase) -and $_ -match '\.dll$' })) {
            $dllName = [System.IO.Path]::GetFileName($dllEntryName)
            $relativeDllEntry = $dllEntryName.Substring($shippingPrefix.Length)
            if ($relativeDllEntry.IndexOf('/') -lt 0 -and $AllowedPackageRootDllNames -notcontains $dllName) {
                throw "ZIP contains an unexpected root runtime DLL: $dllEntryName"
            }
            if ($relativeDllEntry.IndexOf('/') -ge 0 -and -not $relativeDllEntry.StartsWith("versions/", [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "ZIP contains an unexpected nested runtime DLL: $dllEntryName"
            }
            if ($dllName -match '^(TaleWorlds\.|SandBox(?:\.|$)|StoryMode(?:\.|$)|Native\.dll$|CustomBattle\.dll$)') {
                throw "ZIP contains a game-owned DLL: $dllEntryName"
            }
            if ($dllName -like "AnimusForge*.dll" -and $dllEntryName -notin @($bootstrapDllEntryName, $dll13EntryName, $dll14EntryName)) {
                throw "ZIP contains an unexpected AnimusForge implementation DLL: $dllEntryName"
            }
        }

        if ($names -contains "AnimusForge/bin/Win64_Shipping_Client/AnimusForge.dll") {
            throw "ZIP contains the forbidden root implementation DLL."
        }
        if ($names | Where-Object { $_ -match '(^|/)Logs(/|$)' } | Select-Object -First 1) {
            throw "ZIP must not contain Logs."
        }
        if ($OnnxMustBeAbsent -and ($names | Where-Object { $_ -match '(^|/)ONNX(/|$)' } | Select-Object -First 1)) {
            throw "ZIP must not contain ONNX files."
        }
        if ($CustomPromptsMustBeAbsent -and ($names | Where-Object { $_ -match '(^|/)CustomPrompts(/|$)' } | Select-Object -First 1)) {
            throw "ZIP must not contain CustomPrompts files."
        }

        try {
            [xml]$subModuleXml = Read-ZipEntryText -Entry (Get-RequiredZipEntry -Archive $archive -EntryName $subModuleEntryName)
        }
        catch {
            throw "ZIP SubModule.xml is invalid XML."
        }
        $subModules = @($subModuleXml.Module.SubModules.SubModule)
        $assemblies = @($subModuleXml.Module.Assemblies.Assembly)
        if ([string]$subModuleXml.Module.Id.value -ne $ModuleName -or
            [string]$subModuleXml.Module.Name.value -ne $ModuleName -or
            [string]$subModuleXml.Module.Version.value -ne $ExpectedVersion -or
            $subModules.Count -ne 1 -or
            [string]$subModules[0].DLLName.value -ne "$BootstrapAssemblyName.dll" -or
            [string]$subModules[0].SubModuleClassType.value -ne $BootstrapClassType -or
            $assemblies.Count -ne 1 -or
            [string]$assemblies[0].value -ne "$BootstrapAssemblyName.dll") {
            throw "ZIP SubModule.xml must use version '$ExpectedVersion' and point only to Bootstrap."
        }

        $null = Assert-ZipBuildMarker -Archive $archive -DllEntryName $bootstrapDllEntryName -MarkerEntryName $bootstrapMarkerEntryName -ExpectedRole "Bootstrap" -ExpectedAssemblyName $BootstrapAssemblyName -ExpectedReferenceMinor 3
        $hash13 = Assert-ZipBuildMarker -Archive $archive -DllEntryName $dll13EntryName -MarkerEntryName $marker13EntryName -ExpectedRole "Implementation" -ExpectedAssemblyName "AnimusForge" -ExpectedApi "1.3" -ExpectedFlavor $Flavor13 -ExpectedReferenceMinor 3
        $hash14 = Assert-ZipBuildMarker -Archive $archive -DllEntryName $dll14EntryName -MarkerEntryName $marker14EntryName -ExpectedRole "Implementation" -ExpectedAssemblyName "AnimusForge" -ExpectedApi "1.4" -ExpectedFlavor $Flavor14 -ExpectedReferenceMinor 4
        if ($hash13 -eq $hash14) {
            throw "ZIP contains identical 1.3 and 1.4 implementation DLLs."
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Write-ZipFromModule {
    param(
        [Parameter(Mandatory = $true)][string]$ModulePath,
        [Parameter(Mandatory = $true)][string]$PackageVersion,
        [bool]$AutoDetected
    )

    $moduleFull = (Get-FullPathSafe -Path $ModulePath).TrimEnd('\', '/')
    $outputFull = (Get-FullPathSafe -Path $OutputDir).TrimEnd('\', '/')
    if ($outputFull.Equals($moduleFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "OutputDir must not be the module directory itself: $outputFull"
    }
    New-Item -ItemType Directory -Path $outputFull -Force | Out-Null
    $isOutputInsideModule = $outputFull.Equals($moduleFull, [System.StringComparison]::OrdinalIgnoreCase) -or $outputFull.StartsWith($moduleFull + "\", [System.StringComparison]::OrdinalIgnoreCase)
    $onnxFull = (Get-FullPathSafe -Path (Join-Path $moduleFull "ONNX")).TrimEnd('\', '/')
    $customPromptsFull = (Get-FullPathSafe -Path (Join-Path $moduleFull "CustomPrompts")).TrimEnd('\', '/')

    $versionForName = $PackageVersion -replace "[^\w\.\-]", "_"
    $labelForName = ""
    if (-not [string]::IsNullOrWhiteSpace($PackageLabel)) {
        $labelForName = "_" + ($PackageLabel -replace "[^\w\.\-]", "_")
    }
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
    $baseName = "AnimusForge_$versionForName$labelForName`_$timestamp"
    $zipPath = Join-Path $outputFull ($baseName + ".zip")
    $suffix = 1
    while (Test-Path -LiteralPath $zipPath) {
        $zipPath = Join-Path $outputFull ("{0}_{1}.zip" -f $baseName, $suffix)
        $suffix += 1
    }
    $temporaryZipPath = Join-Path $outputFull (".{0}.{1}.tmp" -f $baseName, [Guid]::NewGuid().ToString("N"))
    $zip = $null
    $finalZipCreated = $false
    try {
        try {
            $zip = [System.IO.Compression.ZipFile]::Open($temporaryZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
            $files = Get-ChildItem -LiteralPath $moduleFull -Recurse -File -Force | Where-Object {
                $fullPath = (Get-FullPathSafe -Path $_.FullName).TrimEnd('\', '/')
                $isLog = $fullPath -match '[\\/]+Logs[\\/]+'
                $isOutput = $isOutputInsideModule -and ($fullPath.Equals($outputFull, [System.StringComparison]::OrdinalIgnoreCase) -or $fullPath.StartsWith($outputFull + "\", [System.StringComparison]::OrdinalIgnoreCase))
                $isOnnx = $fullPath.Equals($onnxFull, [System.StringComparison]::OrdinalIgnoreCase) -or $fullPath.StartsWith($onnxFull + "\", [System.StringComparison]::OrdinalIgnoreCase)
                $isCustomPrompt = $fullPath.Equals($customPromptsFull, [System.StringComparison]::OrdinalIgnoreCase) -or $fullPath.StartsWith($customPromptsFull + "\", [System.StringComparison]::OrdinalIgnoreCase)
                $isGameOwnedDll = $_.Extension -eq ".dll" -and $_.Name -match '^(TaleWorlds\.|SandBox(?:\.|$)|StoryMode(?:\.|$)|Native\.dll$|CustomBattle\.dll$)'
                -not $isLog -and -not $isOutput -and -not $isOnnx -and (-not $ExcludeCustomPrompts -or -not $isCustomPrompt) -and -not $isGameOwnedDll
            }

            foreach ($file in $files) {
                $relative = $file.FullName.Substring($moduleFull.Length).TrimStart('\', '/') -replace '\\', '/'
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, "AnimusForge/$relative", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        }
        finally {
            if ($null -ne $zip) {
                $zip.Dispose()
                $zip = $null
            }
        }

        Assert-ZipLayout -ZipPath $temporaryZipPath -ExpectedVersion $PackageVersion -OnnxMustBeAbsent:$true -CustomPromptsMustBeAbsent:$ExcludeCustomPrompts
        [System.IO.File]::Move($temporaryZipPath, $zipPath)
        $finalZipCreated = $true

        Write-Host "Package Mode : one ZIP for both supported game versions"
        Write-Host "Module Path  : $moduleFull"
        Write-Host "Module Detect: $(if ($AutoDetected) { 'Auto' } else { 'Manual' })"
        Write-Host "Output ZIP   : $zipPath"
        Write-Host "Exclude Rule : Logs/**/*"
        if ($ExcludeCustomPrompts) {
            Write-Host "Exclude Rule : CustomPrompts/**/*"
        }
        Write-Host "ONNX ZIP     : Excluded by unified client package policy"
        return $zipPath
    }
    catch {
        $zipFailure = $_
        $cleanupFailures = New-Object System.Collections.Generic.List[string]
        if ($null -ne $zip) {
            try {
                $zip.Dispose()
            }
            catch {
                $cleanupFailures.Add("ZIP stream: $($_.Exception.Message)")
            }
        }
        if (Test-Path -LiteralPath $temporaryZipPath -PathType Leaf) {
            try {
                Remove-Item -LiteralPath $temporaryZipPath -Force
            }
            catch {
                $cleanupFailures.Add("${temporaryZipPath}: $($_.Exception.Message)")
            }
        }
        if ($finalZipCreated -and (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
            try {
                Remove-Item -LiteralPath $zipPath -Force
            }
            catch {
                $cleanupFailures.Add("${zipPath}: $($_.Exception.Message)")
            }
        }
        if ($cleanupFailures.Count -gt 0) {
            throw "ZIP creation failed and cleanup was incomplete.`nOriginal error: $($zipFailure.Exception.Message)`nCleanup errors:`n$($cleanupFailures -join "`n")"
        }
        throw $zipFailure
    }
}

$resolved = Resolve-AnimusForgeModuleDir -RequestedPath $ModuleDir -BannerlordRootPath $BannerlordRoot -AllowFirstMatch:$UseFirstMatch
$moduleFull = $resolved.Path
$moduleXml = Join-Path $moduleFull "SubModule.xml"
$outputFullPreflight = (Get-FullPathSafe -Path $OutputDir).TrimEnd('\', '/')
if ($outputFullPreflight.Equals($moduleFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDir must not be the module directory itself: $outputFullPreflight"
}

$versionSourceXml = $moduleXml
if (-not [string]::IsNullOrWhiteSpace($SourceModuleDir)) {
    $sourceFull = Get-FullPathSafe -Path $SourceModuleDir
    $versionSourceXml = Join-Path $sourceFull "SubModule.xml"
    if (-not (Test-Path -LiteralPath $versionSourceXml -PathType Leaf)) {
        throw "SourceModuleDir must contain SubModule.xml: $versionSourceXml"
    }
}

$currentVersion = Get-SubModuleVersion -SubModulePath $versionSourceXml
$packageVersion = Resolve-PackageVersion -CurrentVersion $currentVersion
$xmlSnapshots = New-Object System.Collections.Generic.List[object]
foreach ($xmlPath in @($versionSourceXml, $moduleXml)) {
    $xmlFullPath = Get-FullPathSafe -Path $xmlPath
    $alreadyCaptured = $false
    foreach ($snapshot in $xmlSnapshots) {
        if ($snapshot.Path.Equals($xmlFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            $alreadyCaptured = $true
            break
        }
    }
    if (-not $alreadyCaptured) {
        $xmlSnapshots.Add([PSCustomObject]@{
            Path = $xmlFullPath
            Bytes = [System.IO.File]::ReadAllBytes($xmlFullPath)
        })
    }
}

$createdZipPath = ""
try {
    Set-SubModuleVersion -SubModulePath $versionSourceXml -NewVersion $packageVersion
    if (-not (Get-FullPathSafe -Path $versionSourceXml).Equals((Get-FullPathSafe -Path $moduleXml), [System.StringComparison]::OrdinalIgnoreCase)) {
        Set-SubModuleVersion -SubModulePath $moduleXml -NewVersion $packageVersion
    }

    $checkAfterVersion = Test-AnimusForgeModuleDir -Path $moduleFull
    if (-not $checkAfterVersion.IsValid) {
        throw "Module became invalid before packaging: $($checkAfterVersion.Missing -join ', ')"
    }

    Write-Host "Version      : $currentVersion -> $packageVersion"
    Write-Host "Version XML  : $versionSourceXml"
    $createdZipPath = Write-ZipFromModule -ModulePath $moduleFull -PackageVersion $packageVersion -AutoDetected:$resolved.AutoDetected
    Write-Host "Package Result: success"
    Write-Host "Package       : $createdZipPath"
}
catch {
    $packageFailure = $_
    $rollbackFailures = New-Object System.Collections.Generic.List[string]
    foreach ($snapshot in $xmlSnapshots) {
        try {
            [System.IO.File]::WriteAllBytes($snapshot.Path, $snapshot.Bytes)
        }
        catch {
            $rollbackFailures.Add("$($snapshot.Path): $($_.Exception.Message)")
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($createdZipPath) -and (Test-Path -LiteralPath $createdZipPath -PathType Leaf)) {
        try {
            Remove-Item -LiteralPath $createdZipPath -Force
        }
        catch {
            $rollbackFailures.Add("${createdZipPath}: $($_.Exception.Message)")
        }
    }

    if ($rollbackFailures.Count -gt 0) {
        throw "Packaging failed and rollback was incomplete.`nOriginal error: $($packageFailure.Exception.Message)`nRollback errors:`n$($rollbackFailures -join "`n")"
    }
    throw $packageFailure
}

exit 0
