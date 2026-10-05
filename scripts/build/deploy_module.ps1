param(
    [string]$ProjectRoot = "",
    [string]$BannerlordRoot = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$BuildDll13 = "",
    [string]$BuildDll14 = "",
    [string]$BootstrapDll = "",
    [string]$RuntimeDependencyDir = "",
    [string]$StageOnlyOutputDir = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "coup_seam_gate.ps1")
$contentLayoutHelper = Join-Path $PSScriptRoot "content_layout.ps1"
if (-not (Test-Path -LiteralPath $contentLayoutHelper -PathType Leaf)) {
    throw "Content layout helper not found: $contentLayoutHelper"
}
. $contentLayoutHelper

$ModuleId = "AnimusForge"
$ModuleName = "AnimusForge"
$BootstrapAssemblyName = "AnimusForge.Bootstrap"
$BootstrapClassType = "AnimusForge.Bootstrap.BootstrapSubModule"
$FlavorKey = "AnimusForge.BuildFlavor"
$ApiKey = "AnimusForge.BannerlordApi"
$Flavor13 = "ANIMUSFORGE_BANNERLORD_API_1_3"
$Flavor14 = "ANIMUSFORGE_BANNERLORD_API_1_4"
$PrivateRuntimeDlls = @(
    "Microsoft.ML.OnnxRuntime.dll",
    "onnxruntime.dll",
    "onnxruntime_providers_shared.dll",
    "System.Buffers.dll",
    "System.Memory.dll",
    "System.Runtime.CompilerServices.Unsafe.dll"
)

function Get-FullPathSafe {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [System.IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    if (-not (Test-Path -LiteralPath $LiteralPath -PathType Leaf)) {
        throw "File not found for hash: $LiteralPath"
    }

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

function Assert-PathUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $pathFull = (Get-FullPathSafe -Path $Path).TrimEnd('\', '/')
    $rootFull = (Get-FullPathSafe -Path $Root).TrimEnd('\', '/')
    if (-not $pathFull.StartsWith($rootFull + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the allowed root: $pathFull"
    }
}

function Assert-NotReparsePoint {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Refusing a module operation through a reparse point: $Path"
    }
}

function Assert-NoReparseAncestors {
    param([Parameter(Mandatory = $true)][string]$Path)

    $full = Get-FullPathSafe -Path $Path
    $current = [System.IO.Path]::GetPathRoot($full)
    Assert-NotReparsePoint -Path $current
    foreach ($part in ($full.Substring($current.Length) -split '[\\/]')) {
        if ([string]::IsNullOrEmpty($part)) { continue }
        $current = Join-Path $current $part
        Assert-NotReparsePoint -Path $current
    }
}

function Reset-ProjectStageDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$ConfigurationName
    )

    $expected = Get-FullPathSafe -Path (Join-Path $ProjectRoot "bin\$ConfigurationName\single_module_stage\AnimusForge")
    $actual = Get-FullPathSafe -Path $Path
    if (-not $actual.Equals($expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe project stage output. Expected '$expected', actual '$actual'."
    }
    Assert-PathUnderRoot -Path $actual -Root $ProjectRoot
    if (Test-Path -LiteralPath $actual) {
        Assert-NotReparsePoint -Path $actual
        Assert-AnimusForgeCleanStage -ProjectRoot $ProjectRoot -StageModuleDir $actual
        Remove-Item -LiteralPath $actual -Recurse -Force
    }
    New-Item -ItemType Directory -Path $actual -Force | Out-Null
    return $actual
}

function Test-SourceModuleDir {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Source module directory not found: $Path"
    }
    $missing = @("SubModule.xml") | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $Path $_))
    }
    if ($missing.Count -gt 0) {
        throw "Source module is incomplete: $Path`nMissing: $($missing -join ', ')"
    }
}

function Resolve-PrivateRuntimeDependencyDir {
    param(
        [string]$RequestedDir,
        [Parameter(Mandatory = $true)][string]$SourceModuleDir,
        [Parameter(Mandatory = $true)][string]$TargetModuleDir
    )

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($RequestedDir)) {
        $candidates.Add((Get-FullPathSafe -Path $RequestedDir))
    }
    else {
        $candidates.Add((Get-FullPathSafe -Path (Join-Path $SourceModuleDir "bin\Win64_Shipping_Client")))
        $candidates.Add((Get-FullPathSafe -Path (Join-Path $TargetModuleDir "bin\Win64_Shipping_Client")))
    }

    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in @($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            $errors.Add("Directory not found: $candidate")
            continue
        }
        $missing = @($PrivateRuntimeDlls | Where-Object {
            -not (Test-Path -LiteralPath (Join-Path $candidate $_) -PathType Leaf)
        })
        if ($missing.Count -eq 0) {
            return $candidate
        }
        $errors.Add("Incomplete runtime dependency directory '$candidate'. Missing: $($missing -join ', ')")
    }

    throw "A complete AnimusForge private runtime dependency directory is required.`n$($errors -join "`n")"
}

function Get-BannerlordModulesDir {
    param([Parameter(Mandatory = $true)][string]$BannerlordRootPath)

    $rootFull = Get-FullPathSafe -Path $BannerlordRootPath
    if (-not (Test-Path -LiteralPath $rootFull -PathType Container)) {
        throw "Bannerlord root not found: $rootFull"
    }
    $modulesDir = Join-Path $rootFull "Modules"
    if (-not (Test-Path -LiteralPath $modulesDir -PathType Container)) {
        throw "Bannerlord Modules directory not found: $modulesDir"
    }
    Assert-NoReparseAncestors -Path $modulesDir
    return (Get-FullPathSafe -Path $modulesDir)
}

function Get-BuildMarkerPath {
    param([Parameter(Mandatory = $true)][string]$DllPath)

    return (Join-Path (Split-Path -Parent $DllPath) (([System.IO.Path]::GetFileNameWithoutExtension($DllPath)) + ".build.json"))
}

function Assert-AssemblyName {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedName
    )

    if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
        throw "Required DLL not found: $DllPath"
    }
    $actualName = [System.Reflection.AssemblyName]::GetAssemblyName($DllPath).Name
    if (-not $actualName.Equals($ExpectedName, [System.StringComparison]::Ordinal)) {
        throw "Unexpected assembly name in '$DllPath': expected '$ExpectedName', actual '$actualName'."
    }
}

function Assert-BuildMarker {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedRole,
        [string]$ExpectedApi = "",
        [string]$ExpectedFlavor = "",
        [Parameter(Mandatory = $true)][int]$ExpectedReferenceMinor
    )

    $markerPath = Get-BuildMarkerPath -DllPath $DllPath
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Build marker not found: $markerPath"
    }
    try {
        $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
    }
    catch {
        throw "Build marker is invalid JSON: $markerPath"
    }

    $actualHash = Get-FileSha256 -LiteralPath $DllPath
    $expectedAssemblyName = if ($ExpectedRole -eq "Bootstrap") { $BootstrapAssemblyName } else { "AnimusForge" }
    $referenceVersion = [string]$marker.ReferenceGameVersion
    $createdUtc = [string]$marker.CreatedUtc
    $createdTimestamp = [DateTimeOffset]::MinValue
    if ([int]$marker.SchemaVersion -ne 2 -or
        [string]$marker.Role -ne $ExpectedRole -or
        [string]$marker.FileName -ne [System.IO.Path]::GetFileName($DllPath) -or
        [string]$marker.AssemblyName -ne $expectedAssemblyName -or
        -not ([string]$marker.Sha256).Equals($actualHash, [System.StringComparison]::OrdinalIgnoreCase) -or
        $referenceVersion -notmatch ("^v?1\." + $ExpectedReferenceMinor + "\.\d+\.\d+$") -or
        -not [DateTimeOffset]::TryParse($createdUtc, [ref]$createdTimestamp)) {
        throw "Build marker does not match its DLL: $markerPath"
    }
    if ($ExpectedRole -eq "Implementation") { Assert-CoupSeamReceipt -Marker $marker -CandidateHash $actualHash -ExpectedApi $ExpectedApi }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedApi) -and [string]$marker.BannerlordApi -ne $ExpectedApi) {
        throw "Build marker API mismatch: $markerPath"
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedFlavor) -and [string]$marker.BuildFlavor -ne $ExpectedFlavor) {
        throw "Build marker flavor mismatch: $markerPath"
    }
}

function Assert-ImplementationArtifact {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedApi,
        [Parameter(Mandatory = $true)][string]$ExpectedFlavor,
        [Parameter(Mandatory = $true)][string]$UnexpectedFlavor
    )

    Assert-AssemblyName -DllPath $DllPath -ExpectedName "AnimusForge"
    $binaryText = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($DllPath))
    foreach ($requiredText in @($FlavorKey, $ApiKey, $ExpectedApi, $ExpectedFlavor)) {
        if ($binaryText.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
            throw "Implementation marker '$requiredText' was not found in: $DllPath"
        }
    }
    if ($binaryText.IndexOf($UnexpectedFlavor, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Implementation contains the wrong build flavor '$UnexpectedFlavor': $DllPath"
    }
    $expectedReferenceMinor = if ($ExpectedApi -eq "1.3") { 3 } else { 4 }
    Assert-BuildMarker -DllPath $DllPath -ExpectedRole "Implementation" -ExpectedApi $ExpectedApi -ExpectedFlavor $ExpectedFlavor -ExpectedReferenceMinor $expectedReferenceMinor
}

function Assert-BootstrapArtifact {
    param([Parameter(Mandatory = $true)][string]$DllPath)

    Assert-AssemblyName -DllPath $DllPath -ExpectedName $BootstrapAssemblyName
    Assert-BuildMarker -DllPath $DllPath -ExpectedRole "Bootstrap" -ExpectedReferenceMinor 3
}

function Copy-RequiredPdb {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDll,
        [Parameter(Mandatory = $true)][string]$TargetDir,
        [Parameter(Mandatory = $true)][string]$TargetBaseName
    )

    $sourcePdb = [System.IO.Path]::ChangeExtension($SourceDll, ".pdb")
    if (-not (Test-Path -LiteralPath $sourcePdb -PathType Leaf)) {
        throw "Required PDB not found: $sourcePdb"
    }
    Copy-Item -LiteralPath $sourcePdb -Destination (Join-Path $TargetDir ($TargetBaseName + ".pdb")) -Force
}

function Assert-ExactDirectoryEntries {
    param(
        [Parameter(Mandatory = $true)][string]$DirectoryPath,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$ExpectedFiles,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$ExpectedDirectories
    )

    if (-not (Test-Path -LiteralPath $DirectoryPath -PathType Container)) {
        throw "Required directory not found: $DirectoryPath"
    }
    $entries = @(Get-ChildItem -LiteralPath $DirectoryPath -Force)
    $actualFiles = @($entries | Where-Object { -not $_.PSIsContainer } | ForEach-Object { $_.Name })
    $actualDirectories = @($entries | Where-Object { $_.PSIsContainer } | ForEach-Object { $_.Name })
    $missingFiles = @($ExpectedFiles | Where-Object { $actualFiles -notcontains $_ })
    $unexpectedFiles = @($actualFiles | Where-Object { $ExpectedFiles -notcontains $_ })
    $missingDirectories = @($ExpectedDirectories | Where-Object { $actualDirectories -notcontains $_ })
    $unexpectedDirectories = @($actualDirectories | Where-Object { $ExpectedDirectories -notcontains $_ })
    if ($missingFiles.Count -gt 0 -or $unexpectedFiles.Count -gt 0 -or $missingDirectories.Count -gt 0 -or $unexpectedDirectories.Count -gt 0) {
        throw "Directory layout is not allowlisted: $DirectoryPath`nMissing files: $($missingFiles -join ', ')`nUnexpected files: $($unexpectedFiles -join ', ')`nMissing directories: $($missingDirectories -join ', ')`nUnexpected directories: $($unexpectedDirectories -join ', ')"
    }
}

function Assert-StrictBinLayout {
    param([Parameter(Mandatory = $true)][string]$BinDir)

    $expectedRootFiles = @(
        "AnimusForge.Bootstrap.dll",
        "AnimusForge.Bootstrap.pdb",
        "AnimusForge.Bootstrap.build.json"
    ) + $PrivateRuntimeDlls
    Assert-ExactDirectoryEntries -DirectoryPath $BinDir -ExpectedFiles $expectedRootFiles -ExpectedDirectories @("versions")

    $versionsDir = Join-Path $BinDir "versions"
    Assert-ExactDirectoryEntries -DirectoryPath $versionsDir -ExpectedFiles @() -ExpectedDirectories @("1.3", "1.4")
    foreach ($version in @("1.3", "1.4")) {
        Assert-ExactDirectoryEntries -DirectoryPath (Join-Path $versionsDir $version) -ExpectedFiles @(
            "AnimusForge.dll",
            "AnimusForge.pdb",
            "AnimusForge.build.json"
        ) -ExpectedDirectories @()
    }
}

function Build-DesiredModuleBin {
    param(
        [Parameter(Mandatory = $true)][string]$RuntimeDependencyDir,
        [Parameter(Mandatory = $true)][string]$StagingBinDir,
        [Parameter(Mandatory = $true)][string]$Implementation13,
        [Parameter(Mandatory = $true)][string]$Implementation14,
        [Parameter(Mandatory = $true)][string]$Bootstrap
    )

    $lockPath = Join-Path $projectRootFull "content\runtime-dependencies.lock.json"
    $dependencyLock = Get-Content -LiteralPath $lockPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([int]$dependencyLock.schemaVersion -ne 1 -or @($dependencyLock.files.PSObject.Properties).Count -ne $PrivateRuntimeDlls.Count) {
        throw "Private runtime dependency lock is incomplete."
    }
    foreach ($runtimeDll in $PrivateRuntimeDlls) {
        $hash = [string]$dependencyLock.files.$runtimeDll
        if ($hash -cnotmatch '^[0-9a-f]{64}$' -or (Get-FileSha256 -LiteralPath (Join-Path $RuntimeDependencyDir $runtimeDll)) -ne $hash) {
            throw "Private runtime dependency is not the locked build input: $runtimeDll"
        }
    }
    if (Test-Path -LiteralPath $StagingBinDir) {
        throw "Staging bin must not already exist: $StagingBinDir"
    }
    New-Item -ItemType Directory -Path $StagingBinDir | Out-Null
    foreach ($runtimeDll in $PrivateRuntimeDlls) {
        $runtimeSource = Join-Path $RuntimeDependencyDir $runtimeDll
        if (-not (Test-Path -LiteralPath $runtimeSource -PathType Leaf)) {
            throw "Required private runtime DLL not found: $runtimeSource"
        }
        Copy-Item -LiteralPath $runtimeSource -Destination (Join-Path $StagingBinDir $runtimeDll) -Force
        if ((Get-FileSha256 -LiteralPath (Join-Path $StagingBinDir $runtimeDll)) -ne [string]$dependencyLock.files.$runtimeDll) {
            throw "Private runtime dependency changed while staging: $runtimeDll"
        }
    }

    $dir13 = Join-Path $StagingBinDir "versions\1.3"
    $dir14 = Join-Path $StagingBinDir "versions\1.4"
    New-Item -ItemType Directory -Path $dir13 -Force | Out-Null
    New-Item -ItemType Directory -Path $dir14 -Force | Out-Null

    Copy-Item -LiteralPath $Bootstrap -Destination (Join-Path $StagingBinDir "AnimusForge.Bootstrap.dll") -Force
    Copy-Item -LiteralPath (Get-BuildMarkerPath -DllPath $Bootstrap) -Destination (Join-Path $StagingBinDir "AnimusForge.Bootstrap.build.json") -Force
    Copy-RequiredPdb -SourceDll $Bootstrap -TargetDir $StagingBinDir -TargetBaseName "AnimusForge.Bootstrap"

    foreach ($spec in @(
        [PSCustomObject]@{ Source = $Implementation13; Target = $dir13 },
        [PSCustomObject]@{ Source = $Implementation14; Target = $dir14 }
    )) {
        Copy-Item -LiteralPath $spec.Source -Destination (Join-Path $spec.Target "AnimusForge.dll") -Force
        Copy-Item -LiteralPath (Get-BuildMarkerPath -DllPath $spec.Source) -Destination (Join-Path $spec.Target "AnimusForge.build.json") -Force
        Copy-RequiredPdb -SourceDll $spec.Source -TargetDir $spec.Target -TargetBaseName "AnimusForge"
    }

    Assert-StrictBinLayout -BinDir $StagingBinDir
}

function Set-SingleModuleIdentity {
    param([Parameter(Mandatory = $true)][string]$ModuleDir)

    $subModulePath = Join-Path $ModuleDir "SubModule.xml"
    [xml]$xml = Get-Content -Raw -Encoding UTF8 -LiteralPath $subModulePath
    $subModules = @($xml.Module.SubModules.SubModule)
    if ($subModules.Count -ne 1) {
        throw "The unified module must contain exactly one SubModule entry: $subModulePath"
    }

    $xml.Module.Id.value = $ModuleId
    $xml.Module.Name.value = $ModuleName
    $subModules[0].Name.value = $ModuleName
    $subModules[0].DLLName.value = "$BootstrapAssemblyName.dll"
    $subModules[0].SubModuleClassType.value = $BootstrapClassType

    $assembliesNode = $xml.SelectSingleNode("/Module/Assemblies")
    if ($null -eq $assembliesNode) {
        throw "SubModule.xml is missing the Assemblies node: $subModulePath"
    }
    $assembliesNode.RemoveAll()
    $assemblyNode = $xml.CreateElement("Assembly")
    $assemblyNode.SetAttribute("value", "$BootstrapAssemblyName.dll")
    $null = $assembliesNode.AppendChild($assemblyNode)

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $settings.Indent = $true
    $settings.IndentChars = "    "
    $settings.NewLineChars = "`r`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.OmitXmlDeclaration = $true
    $writer = [System.Xml.XmlWriter]::Create($subModulePath, $settings)
    try {
        $xml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function Assert-SingleModuleLayout {
    param([Parameter(Mandatory = $true)][string]$ModuleDir)

    $binDir = Join-Path $ModuleDir "bin\Win64_Shipping_Client"
    Assert-StrictBinLayout -BinDir $binDir
    $bootstrap = Join-Path $binDir "AnimusForge.Bootstrap.dll"
    $implementation13 = Join-Path $binDir "versions\1.3\AnimusForge.dll"
    $implementation14 = Join-Path $binDir "versions\1.4\AnimusForge.dll"
    foreach ($requiredFile in @($bootstrap, $implementation13, $implementation14)) {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
            throw "Unified module is missing a required DLL: $requiredFile"
        }
    }
    foreach ($requiredPdb in @(
        (Join-Path $binDir "AnimusForge.Bootstrap.pdb"),
        (Join-Path $binDir "versions\1.3\AnimusForge.pdb"),
        (Join-Path $binDir "versions\1.4\AnimusForge.pdb")
    )) {
        if (-not (Test-Path -LiteralPath $requiredPdb -PathType Leaf)) {
            throw "Unified module is missing a required PDB: $requiredPdb"
        }
    }
    Assert-BootstrapArtifact -DllPath $bootstrap
    Assert-ImplementationArtifact -DllPath $implementation13 -ExpectedApi "1.3" -ExpectedFlavor $Flavor13 -UnexpectedFlavor $Flavor14
    Assert-ImplementationArtifact -DllPath $implementation14 -ExpectedApi "1.4" -ExpectedFlavor $Flavor14 -UnexpectedFlavor $Flavor13

    [xml]$xml = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $ModuleDir "SubModule.xml")
    $subModules = @($xml.Module.SubModules.SubModule)
    $assemblies = @($xml.Module.Assemblies.Assembly)
    if ([string]$xml.Module.Id.value -ne $ModuleId -or [string]$xml.Module.Name.value -ne $ModuleName) {
        throw "SubModule.xml does not use the unified AnimusForge identity."
    }
    if ($subModules.Count -ne 1 -or [string]$subModules[0].DLLName.value -ne "$BootstrapAssemblyName.dll" -or [string]$subModules[0].SubModuleClassType.value -ne $BootstrapClassType) {
        throw "SubModule.xml does not point exclusively to the Bootstrap entry point."
    }
    if ($assemblies.Count -ne 1 -or [string]$assemblies[0].value -ne "$BootstrapAssemblyName.dll") {
        throw "SubModule.xml Assemblies must list only AnimusForge.Bootstrap.dll."
    }
}

function Assert-DeploymentPath {
    param([string]$Path, [string]$Root)

    $rootFull = Get-FullPathSafe -Path $Root
    $pathFull = Get-FullPathSafe -Path $Path
    Assert-PathUnderRoot -Path $pathFull -Root $rootFull
    Assert-NotReparsePoint -Path $rootFull
    $relative = $pathFull.Substring($rootFull.Length).TrimStart('\', '/')
    $current = $rootFull
    foreach ($part in ($relative -split '[\\/]')) {
        if ([string]::IsNullOrEmpty($part)) { continue }
        $current = Join-Path $current $part
        Assert-NotReparsePoint -Path $current
    }
}

function Write-DeploymentMarker {
    param([string]$Directory, [string]$Name)

    $temporary = Join-Path $Directory (".$Name.tmp")
    $final = Join-Path $Directory $Name
    $stream = [System.IO.File]::Open($temporary, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("AF2 deployment`n")
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush($true)
    }
    finally { $stream.Dispose() }
    [System.IO.File]::Move($temporary, $final)
}

function Assert-FeatureBridgesDeploymentBaseline {
    param([string]$StageModuleDir, [string]$TargetModuleDir, [string]$ModulesDir)

    $relative = 'ModuleData\FeatureBridges.json'
    $source = Join-Path $StageModuleDir $relative
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { return }
    $target = Join-Path $TargetModuleDir $relative
    Assert-DeploymentPath -Path $target -Root $ModulesDir
    if (-not (Test-Path -LiteralPath $target)) { return }
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
        throw "FeatureBridges deployment conflict: target is not a regular file: $target"
    }

    $installedHash = Get-FileSha256 -LiteralPath $target
    $newHash = Get-FileSha256 -LiteralPath $source
    # Exact LF/CRLF bytes of the three checked-in defaults before content migration.
    # Unknown differences, including malformed JSON, are never silently replaced.
    $knownDefaults = @(
        '70C3637714D3935F8854D487B38B52534D782F470AF83F29EC62EDCC556C7C46', # 231f6cb6 LF
        'CCB8685E0928C179086D4392830B5423D1CE5D64BAD3340BBF6133EEF8AF9BD2', # 231f6cb6 CRLF
        'C732B9034B1DEC21A81EBC5F74FDE19F4A9BF1DC05E98AAF3B4D875633A3F4E9', # d9f974ce LF
        '808A4218BAAD803537F6EF472EC81BAAB1A6A858E380035651C584329FEC5A4A', # d9f974ce CRLF
        '54612D3084A8C95CF1F170B788D9B2345ACE051838C1E5B9EFD8122068FFD276', # 9a4a26dc LF
        '10C573B461EC148EF8478A0D4269F73A7CA106F5A8C6A50C9D37FACC4FF86896'  # 9a4a26dc CRLF
    )
    if ($installedHash -eq $newHash -or $knownDefaults -contains $installedHash) { return }
    throw "FeatureBridges deployment conflict: installed ModuleData/FeatureBridges.json is not a known default (SHA256=$installedHash). No managed target was replaced; inspect and preserve the installed file before an explicitly approved override."
}

function Invoke-ManagedStageDeployment {
    param([string]$StageModuleDir, [string]$TargetModuleDir, [string]$ModulesDir)

    Assert-FeatureBridgesDeploymentBaseline -StageModuleDir $StageModuleDir -TargetModuleDir $TargetModuleDir -ModulesDir $ModulesDir

    $localAppData = [Environment]::GetEnvironmentVariable('LOCALAPPDATA')
    if ([string]::IsNullOrWhiteSpace($localAppData) -or -not [System.IO.Path]::IsPathRooted($localAppData)) {
        throw 'LOCALAPPDATA must be an absolute user-data root for deployment recovery.'
    }
    $localAppData = Get-FullPathSafe -Path $localAppData
    if (-not (Test-Path -LiteralPath $localAppData -PathType Container)) {
        throw 'LOCALAPPDATA is not an existing directory.'
    }
    Assert-NoReparseAncestors -Path $localAppData
    $recoveryRoot = Join-Path $localAppData 'AnimusForge\Recovery\deploy'
    $recoveryParent = $localAppData
    foreach ($part in @('AnimusForge', 'Recovery', 'deploy')) {
        $recoveryParent = Join-Path $recoveryParent $part
        Assert-NotReparsePoint -Path $recoveryParent
        if ((Test-Path -LiteralPath $recoveryParent) -and -not (Test-Path -LiteralPath $recoveryParent -PathType Container)) {
            throw 'Deployment recovery path is not a directory.'
        }
    }
    New-Item -ItemType Directory -Path $recoveryRoot -Force | Out-Null

    foreach ($previous in @(Get-ChildItem -LiteralPath $recoveryRoot -Directory -Filter 'deploy-*' -Force)) {
        Assert-NotReparsePoint -Path $previous.FullName
        $recordPath = Join-Path $previous.FullName 'manifest.json'
        if (-not (Test-Path -LiteralPath $recordPath -PathType Leaf)) {
            throw 'Incomplete deployment recovery record requires inspection before another deploy.'
        }
        Assert-NotReparsePoint -Path $recordPath
        $record = Get-Content -LiteralPath $recordPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ([string]$record.target -eq $TargetModuleDir -and
            (Test-Path -LiteralPath (Join-Path $previous.FullName 'activating')) -and
            -not (Test-Path -LiteralPath (Join-Path $previous.FullName 'complete')) -and
            -not (Test-Path -LiteralPath (Join-Path $previous.FullName 'rolled-back'))) {
            throw 'An interrupted deployment has a private recovery record; refusing another deploy.'
        }
    }

    $planned = [System.Collections.Generic.List[object]]::new()
    foreach ($source in @(Get-ChildItem -LiteralPath $StageModuleDir -File -Recurse -Force | Sort-Object FullName)) {
        $relative = $source.FullName.Substring($StageModuleDir.Length).TrimStart('\', '/')
        $target = Join-Path $TargetModuleDir $relative
        Assert-DeploymentPath -Path $target -Root $ModulesDir
        if ((Test-Path -LiteralPath $target) -and -not (Test-Path -LiteralPath $target -PathType Leaf)) {
            throw 'A managed target path is not a regular file.'
        }
        $newHash = Get-FileSha256 -LiteralPath $source.FullName
        $oldHash = if (Test-Path -LiteralPath $target -PathType Leaf) { Get-FileSha256 -LiteralPath $target } else { '' }
        if ($newHash -ne $oldHash) {
            $planned.Add([PSCustomObject]@{ Relative = $relative; Source = $source.FullName; Target = $target; OldHash = $oldHash; NewHash = $newHash })
        }
    }
    if ($planned.Count -eq 0) {
        Write-Host 'Deploy Result: already current; no managed file changed'
        return
    }

    $operationId = [Guid]::NewGuid().ToString('N')
    $recoveryDir = Join-Path $recoveryRoot "deploy-$operationId"
    New-Item -ItemType Directory -Path $recoveryDir | Out-Null
    $record = [ordered]@{
        schemaVersion = 1
        target = $TargetModuleDir
        files = @($planned | ForEach-Object { [ordered]@{ relative = $_.Relative; oldSha256 = $_.OldHash; newSha256 = $_.NewHash } })
    }
    $manifest = Join-Path $recoveryDir 'manifest.json'
    [System.IO.File]::WriteAllText($manifest, ($record | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
    $touched = [System.Collections.Generic.List[object]]::new()
    try {
        foreach ($item in $planned) {
            if (-not $item.OldHash) { continue }
            $backup = Join-Path (Join-Path $recoveryDir 'files') $item.Relative
            Assert-DeploymentPath -Path $backup -Root $recoveryRoot
            New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
            Assert-DeploymentPath -Path $backup -Root $recoveryRoot
            [System.IO.File]::Copy($item.Target, $backup, $false)
            if ((Get-FileSha256 -LiteralPath $backup) -ne $item.OldHash -or
                (Get-FileSha256 -LiteralPath $item.Target) -ne $item.OldHash) {
                throw 'Managed target changed during private backup.'
            }
        }
        Write-DeploymentMarker -Directory $recoveryDir -Name 'activating'
        foreach ($item in $planned) {
            Assert-DeploymentPath -Path $item.Target -Root $ModulesDir
            $currentHash = if (Test-Path -LiteralPath $item.Target -PathType Leaf) { Get-FileSha256 -LiteralPath $item.Target } else { '' }
            if ($currentHash -ne $item.OldHash) { throw 'Managed target changed during deployment.' }
            if ((Get-FileSha256 -LiteralPath $item.Source) -ne $item.NewHash) { throw 'Stage changed during deployment.' }
            $parent = Split-Path -Parent $item.Target
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
            Assert-DeploymentPath -Path $item.Target -Root $ModulesDir
            $candidate = Join-Path $parent ('.af2-deploy-' + $operationId + '.tmp')
            $replaceBackup = Join-Path $parent ('.af2-replaced-' + $operationId + '.tmp')
            [System.IO.File]::Copy($item.Source, $candidate, $false)
            try {
                if ((Get-FileSha256 -LiteralPath $candidate) -ne $item.NewHash) { throw 'Same-volume deployment candidate changed.' }
                $touched.Add($item)
                if ($item.OldHash) { [System.IO.File]::Replace($candidate, $item.Target, $replaceBackup) }
                else { [System.IO.File]::Move($candidate, $item.Target) }
                if ((Get-FileSha256 -LiteralPath $item.Target) -ne $item.NewHash) { throw 'Managed target hash mismatch after replacement.' }
            }
            finally {
                if (Test-Path -LiteralPath $candidate -PathType Leaf) { [System.IO.File]::Delete($candidate) }
                if (Test-Path -LiteralPath $replaceBackup -PathType Leaf) { [System.IO.File]::Delete($replaceBackup) }
            }
        }
        Write-DeploymentMarker -Directory $recoveryDir -Name 'complete'
    }
    catch {
        $failure = $_.Exception.Message
        $rollbackErrors = [System.Collections.Generic.List[string]]::new()
        for ($index = $touched.Count - 1; $index -ge 0; $index--) {
            $item = $touched[$index]
            try {
                Assert-DeploymentPath -Path $item.Target -Root $ModulesDir
                $currentHash = if (Test-Path -LiteralPath $item.Target -PathType Leaf) { Get-FileSha256 -LiteralPath $item.Target } else { '' }
                if ($currentHash -eq $item.OldHash) { continue }
                if ($currentHash -ne $item.NewHash) { throw 'Target changed again; automatic rollback refused.' }
                if ($item.OldHash) {
                    $backup = Join-Path (Join-Path $recoveryDir 'files') $item.Relative
                    Assert-DeploymentPath -Path $backup -Root $recoveryRoot
                    if ((Get-FileSha256 -LiteralPath $backup) -ne $item.OldHash) { throw 'Private backup hash mismatch.' }
                    $restore = Join-Path (Split-Path -Parent $item.Target) ('.af2-restore-' + $operationId + '.tmp')
                    $replaced = Join-Path (Split-Path -Parent $item.Target) ('.af2-restore-replaced-' + $operationId + '.tmp')
                    [System.IO.File]::Copy($backup, $restore, $false)
                    try { [System.IO.File]::Replace($restore, $item.Target, $replaced) }
                    finally {
                        if (Test-Path -LiteralPath $restore -PathType Leaf) { [System.IO.File]::Delete($restore) }
                        if (Test-Path -LiteralPath $replaced -PathType Leaf) { [System.IO.File]::Delete($replaced) }
                    }
                }
                else { [System.IO.File]::Delete($item.Target) }
                $restoredHash = if (Test-Path -LiteralPath $item.Target -PathType Leaf) { Get-FileSha256 -LiteralPath $item.Target } else { '' }
                if ($restoredHash -ne $item.OldHash) { throw 'Rollback hash mismatch.' }
            }
            catch { $rollbackErrors.Add($_.Exception.Message) }
        }
        if ($rollbackErrors.Count -eq 0) {
            Write-DeploymentMarker -Directory $recoveryDir -Name 'rolled-back'
            throw "Managed deployment failed; all touched files were restored: $failure"
        }
        throw "Managed deployment failed and rollback is incomplete; inspect private Recovery before retry: $failure; $($rollbackErrors -join '; ')"
    }
    Write-Host "Deploy Result: success; managed files updated: $($planned.Count)"
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot "..\.."
}
if ([string]::IsNullOrWhiteSpace($BannerlordRoot)) {
    throw "-BannerlordRoot is required."
}
foreach ($argument in @($BuildDll13, $BuildDll14, $BootstrapDll)) {
    if ([string]::IsNullOrWhiteSpace($argument)) {
        throw "-BuildDll13, -BuildDll14, and -BootstrapDll are all required."
    }
}

$projectRootFull = Get-FullPathSafe -Path $ProjectRoot
Assert-NoReparseAncestors -Path $projectRootFull
$sourceModuleDir = Get-FullPathSafe -Path (Join-Path $projectRootFull "AnimusForge")
$modulesDir = Get-BannerlordModulesDir -BannerlordRootPath $BannerlordRoot
$targetModuleDir = Get-FullPathSafe -Path (Join-Path $modulesDir $ModuleId)
$targetParent = Get-FullPathSafe -Path (Split-Path -Parent $targetModuleDir)
if (-not $targetParent.Equals($modulesDir, [System.StringComparison]::OrdinalIgnoreCase) -or -not (Split-Path -Leaf $targetModuleDir).Equals($ModuleId, [System.StringComparison]::Ordinal)) {
    throw "Unsafe module target path: $targetModuleDir"
}
if ($sourceModuleDir.Equals($targetModuleDir, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The source module and deployment target must be different directories: $sourceModuleDir"
}
if ((Test-Path -LiteralPath $targetModuleDir) -and -not (Test-Path -LiteralPath $targetModuleDir -PathType Container)) {
    throw "The unified module target exists but is not a directory: $targetModuleDir"
}
Assert-NotReparsePoint -Path $targetModuleDir

Test-SourceModuleDir -Path $sourceModuleDir
$runtimeDependencyDirFull = Resolve-PrivateRuntimeDependencyDir -RequestedDir $RuntimeDependencyDir -SourceModuleDir $sourceModuleDir -TargetModuleDir $targetModuleDir
$dll13Full = Get-FullPathSafe -Path $BuildDll13
$dll14Full = Get-FullPathSafe -Path $BuildDll14
$bootstrapFull = Get-FullPathSafe -Path $BootstrapDll
Assert-ImplementationArtifact -DllPath $dll13Full -ExpectedApi "1.3" -ExpectedFlavor $Flavor13 -UnexpectedFlavor $Flavor14
Assert-ImplementationArtifact -DllPath $dll14Full -ExpectedApi "1.4" -ExpectedFlavor $Flavor14 -UnexpectedFlavor $Flavor13
Assert-BootstrapArtifact -DllPath $bootstrapFull
if ((Get-FileSha256 -LiteralPath $dll13Full) -eq (Get-FileSha256 -LiteralPath $dll14Full)) {
    throw "The 1.3 and 1.4 implementation DLL hashes are identical."
}

# A caller may launch the BAT/PowerShell script with its current directory
# inside Modules\AnimusForge. Resolve inputs first, then leave that directory
# before creating same-volume candidates or replacing managed files.
Set-Location -LiteralPath $projectRootFull
[System.Environment]::CurrentDirectory = $projectRootFull
Write-Host "Deploy CWD   : $projectRootFull"

$projectStagePath = Join-Path $projectRootFull "bin\$Configuration\single_module_stage\AnimusForge"
if (-not [string]::IsNullOrWhiteSpace($StageOnlyOutputDir) -and
    -not (Get-FullPathSafe -Path $StageOnlyOutputDir).Equals((Get-FullPathSafe -Path $projectStagePath), [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Stage-only output must be the exact project-local Stage path.'
}
$projectStageDir = Reset-ProjectStageDirectory -Path $projectStagePath -ProjectRoot $projectRootFull -ConfigurationName $Configuration
Copy-Item -LiteralPath (Join-Path $sourceModuleDir "SubModule.xml") -Destination (Join-Path $projectStageDir "SubModule.xml")
Invoke-AnimusForgeContentProjection -ProjectRoot $projectRootFull -DestinationModuleDir $projectStageDir | Out-Null
Set-SingleModuleIdentity -ModuleDir $projectStageDir
Build-DesiredModuleBin -RuntimeDependencyDir $runtimeDependencyDirFull -StagingBinDir (Join-Path $projectStageDir "bin\Win64_Shipping_Client") -Implementation13 $dll13Full -Implementation14 $dll14Full -Bootstrap $bootstrapFull
Assert-SingleModuleLayout -ModuleDir $projectStageDir
Assert-AnimusForgeCleanStage -ProjectRoot $projectRootFull -StageModuleDir $projectStageDir -RequireCurrentArtifacts

if (-not [string]::IsNullOrWhiteSpace($StageOnlyOutputDir)) {
    Write-Host "Stage Mode   : project-local unified module; no game directory was modified"
    Write-Host "Stage Result : success"
    Write-Host "Output       : $projectStageDir"
    return
}

$legacyModules = @('AnimusForge_1_3_x', 'AnimusForge_1_4_5') | Where-Object {
    Test-Path -LiteralPath (Join-Path $modulesDir $_) -PathType Container
}
if ($legacyModules.Count -gt 0) {
    Write-Warning 'Legacy AnimusForge module folders were left untouched; disable them before launching the game to avoid duplicate module loading.'
}
Invoke-ManagedStageDeployment -StageModuleDir $projectStageDir -TargetModuleDir $targetModuleDir -ModulesDir $modulesDir
Write-Host "Deploy Mode  : Stage-managed files only; unknown installed files untouched"
Write-Host "Output       : $targetModuleDir"
