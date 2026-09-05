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
$ModuleId = "AnimusForge"
$ModuleName = "AnimusForge"
$BootstrapAssemblyName = "AnimusForge.Bootstrap"
$BootstrapClassType = "AnimusForge.Bootstrap.BootstrapSubModule"
$FlavorKey = "AnimusForge.BuildFlavor"
$ApiKey = "AnimusForge.BannerlordApi"
$Flavor13 = "ANIMUSFORGE_BANNERLORD_API_1_3"
$Flavor14 = "ANIMUSFORGE_BANNERLORD_API_1_4"
$LegacyRootPolicyPromptFileNames = @(
    "CustomPolicyEvaluatorPrompt.json",
    "NpcRulerPolicyPrompt.json",
    "PlayerPolicyAutoDraftPrompt.json",
    "PolicyEffectPrompts.v1.json"
)
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

function Assert-SafeModuleWorkingPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ModulesDir
    )

    $pathFull = Get-FullPathSafe -Path $Path
    $modulesFull = Get-FullPathSafe -Path $ModulesDir
    $parentFull = Get-FullPathSafe -Path (Split-Path -Parent $pathFull)
    $leaf = Split-Path -Leaf $pathFull
    $isExpectedName = $leaf.Equals($ModuleId, [System.StringComparison]::Ordinal) -or
        $leaf -cmatch '^\.AnimusForge\.deploy\.(Debug|Release)\.[0-9a-f]{32}$' -or
        $leaf -cmatch '^\.AnimusForge\.backup\.[0-9a-f]{32}$'
    if (-not $parentFull.Equals($modulesFull, [System.StringComparison]::OrdinalIgnoreCase) -or -not $isExpectedName) {
        throw "Unsafe module working path: $pathFull"
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

function New-SafeModuleWorkingDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ModulesDir
    )

    Assert-SafeModuleWorkingPath -Path $Path -ModulesDir $ModulesDir
    if (Test-Path -LiteralPath $Path) {
        throw "Refusing to reuse an existing module working directory: $Path"
    }
    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Remove-SafeModuleWorkingDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ModulesDir
    )

    Assert-SafeModuleWorkingPath -Path $Path -ModulesDir $ModulesDir
    if (Test-Path -LiteralPath $Path) {
        Assert-NotReparsePoint -Path $Path
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-Robocopy {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$TargetDir,
        [Parameter(Mandatory = $true)][string[]]$ExtraArguments,
        [ValidateRange(0, 60)][int]$RetryCount = 1,
        [ValidateRange(0, 60)][int]$WaitSeconds = 1
    )

    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    $arguments = @(
        $SourceDir,
        $TargetDir,
        "/R:$RetryCount",
        "/W:$WaitSeconds",
        "/XJ",
        "/NP",
        "/NFL",
        "/NDL",
        "/NJH",
        "/NJS"
    ) + $ExtraArguments

    $robocopyOutput = @(& robocopy @arguments 2>&1)
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        $details = @($robocopyOutput | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 12)
        $detailText = if ($details.Count -gt 0) { "`n$($details -join "`n")" } else { "" }
        throw "robocopy failed for '$SourceDir' -> '$TargetDir' with exit code $exitCode.$detailText"
    }
}

function Merge-InstalledCustomPromptsIntoStaging {
    param(
        [Parameter(Mandatory = $true)][string]$SourceModuleDir,
        [Parameter(Mandatory = $true)][string]$TargetModuleDir,
        [Parameter(Mandatory = $true)][string]$StagingModuleDir
    )

    $targetCustomPrompts = Join-Path $TargetModuleDir "CustomPrompts"
    if (-not (Test-Path -LiteralPath $targetCustomPrompts -PathType Container)) {
        return
    }

    $stagingCustomPrompts = Join-Path $StagingModuleDir "CustomPrompts"
    Assert-PathUnderRoot -Path $stagingCustomPrompts -Root $StagingModuleDir
    Assert-NotReparsePoint -Path $targetCustomPrompts
    Assert-NotReparsePoint -Path $stagingCustomPrompts
    Invoke-Robocopy -SourceDir $targetCustomPrompts -TargetDir $stagingCustomPrompts -ExtraArguments @(
        "/MIR",
        "/COPY:DAT",
        "/DCOPY:DAT"
    )

    $targetEffectPrompts = Join-Path $targetCustomPrompts "Policy\Effects"
    if (Test-Path -LiteralPath $targetEffectPrompts -PathType Container) {
        Write-Host "Preserved CustomPrompts: installed split policy prompts and all non-policy prompts"
        return
    }

    foreach ($fileName in $LegacyRootPolicyPromptFileNames) {
        $legacyPromptPath = Join-Path $stagingCustomPrompts $fileName
        Assert-PathUnderRoot -Path $legacyPromptPath -Root $StagingModuleDir
        if (Test-Path -LiteralPath $legacyPromptPath -PathType Leaf) {
            Remove-Item -LiteralPath $legacyPromptPath -Force
        }
    }

    $sourcePolicyPrompts = Join-Path $SourceModuleDir "CustomPrompts\Policy"
    $sourceEffectPrompts = Join-Path $sourcePolicyPrompts "Effects"
    if (-not (Test-Path -LiteralPath $sourceEffectPrompts -PathType Container)) {
        throw "Source split policy prompts are missing: $sourceEffectPrompts"
    }
    $stagingPolicyPrompts = Join-Path $stagingCustomPrompts "Policy"
    Assert-PathUnderRoot -Path $stagingPolicyPrompts -Root $StagingModuleDir
    Invoke-Robocopy -SourceDir $sourcePolicyPrompts -TargetDir $stagingPolicyPrompts -ExtraArguments @(
        "/MIR",
        "/COPY:DAT",
        "/DCOPY:DAT"
    )
    Write-Host "Policy prompts: replaced legacy root files with split defaults; preserved all non-policy prompts"
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
    $missing = @("SubModule.xml", "ModuleData", "GUI", "ONNX", "PlayerExports") | Where-Object {
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

function Get-RelativePathUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $pathFull = Get-FullPathSafe -Path $Path
    $rootFull = (Get-FullPathSafe -Path $Root).TrimEnd('\', '/')
    if (-not $pathFull.StartsWith($rootFull + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the PlayerExports root: $pathFull"
    }
    return $pathFull.Substring($rootFull.Length + 1)
}

function Merge-PlayerExports {
    param(
        [Parameter(Mandatory = $true)][string]$DestinationDir,
        [Parameter(Mandatory = $true)][object[]]$Sources
    )

    if (Test-Path -LiteralPath $DestinationDir) {
        throw "PlayerExports staging destination must not already exist: $DestinationDir"
    }
    New-Item -ItemType Directory -Path $DestinationDir | Out-Null

    $winners = @{}
    foreach ($source in $Sources) {
        $sourceDir = Get-FullPathSafe -Path ([string]$source.Path)
        if (-not (Test-Path -LiteralPath $sourceDir -PathType Container)) {
            continue
        }

        foreach ($file in Get-ChildItem -LiteralPath $sourceDir -Recurse -Force -File) {
            $relativePath = Get-RelativePathUnderRoot -Path $file.FullName -Root $sourceDir
            $candidate = [PSCustomObject]@{
                SourcePath = $file.FullName
                RelativePath = $relativePath
                LastWriteTicks = $file.LastWriteTimeUtc.Ticks
                Priority = [int]$source.Priority
                Label = [string]$source.Label
            }
            $current = $winners[$relativePath]
            if ($null -eq $current -or
                $candidate.LastWriteTicks -gt $current.LastWriteTicks -or
                ($candidate.LastWriteTicks -eq $current.LastWriteTicks -and $candidate.Priority -gt $current.Priority)) {
                $winners[$relativePath] = $candidate
            }
        }
    }

    foreach ($relativePath in @($winners.Keys | Sort-Object)) {
        $winner = $winners[$relativePath]
        $targetPath = Join-Path $DestinationDir $relativePath
        Assert-PathUnderRoot -Path $targetPath -Root $DestinationDir
        $targetParent = Split-Path -Parent $targetPath
        New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        Copy-Item -LiteralPath $winner.SourcePath -Destination $targetPath -Force
        [System.IO.File]::SetLastWriteTimeUtc($targetPath, [System.DateTime]::new($winner.LastWriteTicks, [System.DateTimeKind]::Utc))
    }

    Write-Host "Merged Data  : $($winners.Count) PlayerExports file(s) into staging"
}

function Sync-PlayerExportsBackToSource {
    param(
        [Parameter(Mandatory = $true)][string]$SourceModuleDir,
        [Parameter(Mandatory = $true)][string]$TargetModuleDir
    )

    $targetExports = Join-Path $TargetModuleDir "PlayerExports"
    if (-not (Test-Path -LiteralPath $targetExports -PathType Container)) {
        Write-Warning "PlayerExports source sync skipped because the deployed unified module has no PlayerExports directory."
        return
    }

    $sourceExports = Join-Path $SourceModuleDir "PlayerExports"
    try {
        Invoke-Robocopy -SourceDir $targetExports -TargetDir $sourceExports -ExtraArguments @("/E", "/XO")
        Write-Host "Synced Data  : $targetExports -> $sourceExports (non-deleting /E; newer source files preserved)"
    }
    catch {
        Write-Warning "Deployment succeeded, but PlayerExports could not be synced back to the source with non-deleting /E: $($_.Exception.Message)"
    }
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

function Assert-SameHash {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    if (-not (Test-Path -LiteralPath $TargetPath -PathType Leaf)) {
        throw "Missing deployed file: $TargetPath"
    }
    if ((Get-FileSha256 -LiteralPath $SourcePath) -ne (Get-FileSha256 -LiteralPath $TargetPath)) {
        throw "Hash mismatch after deploy: $TargetPath"
    }
    Write-Host "Verified     : $TargetPath"
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

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot ".."
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
# inside Modules\AnimusForge.  Windows then refuses to rename that directory
# even when the game has never started.  Resolve every input first, then move
# both PowerShell's provider location and the native process CWD to the project.
Set-Location -LiteralPath $projectRootFull
[System.Environment]::CurrentDirectory = $projectRootFull
Write-Host "Deploy CWD   : $projectRootFull"

if (-not [string]::IsNullOrWhiteSpace($StageOnlyOutputDir)) {
    $projectStageDir = Reset-ProjectStageDirectory -Path $StageOnlyOutputDir -ProjectRoot $projectRootFull -ConfigurationName $Configuration
    try {
        Invoke-Robocopy -SourceDir $sourceModuleDir -TargetDir $projectStageDir -ExtraArguments @(
            "/E",
            "/XD",
            (Join-Path $sourceModuleDir "Logs"),
            (Join-Path $sourceModuleDir "PlayerExports"),
            (Join-Path $sourceModuleDir "bin")
        )
        Set-SingleModuleIdentity -ModuleDir $projectStageDir
        Build-DesiredModuleBin -RuntimeDependencyDir $runtimeDependencyDirFull -StagingBinDir (Join-Path $projectStageDir "bin\Win64_Shipping_Client") -Implementation13 $dll13Full -Implementation14 $dll14Full -Bootstrap $bootstrapFull
        Invoke-Robocopy -SourceDir (Join-Path $sourceModuleDir "PlayerExports") -TargetDir (Join-Path $projectStageDir "PlayerExports") -ExtraArguments @("/E")
        Assert-SingleModuleLayout -ModuleDir $projectStageDir
        Assert-SameHash -SourcePath $bootstrapFull -TargetPath (Join-Path $projectStageDir "bin\Win64_Shipping_Client\AnimusForge.Bootstrap.dll")
        Assert-SameHash -SourcePath $dll13Full -TargetPath (Join-Path $projectStageDir "bin\Win64_Shipping_Client\versions\1.3\AnimusForge.dll")
        Assert-SameHash -SourcePath $dll14Full -TargetPath (Join-Path $projectStageDir "bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll")
    }
    catch {
        $stageFailure = $_
        if (Test-Path -LiteralPath $projectStageDir) {
            try {
                Assert-NotReparsePoint -Path $projectStageDir
                Remove-Item -LiteralPath $projectStageDir -Recurse -Force
            }
            catch {
                Write-Warning "Failed to clean project staging directory after an assembly error: $projectStageDir"
            }
        }
        throw $stageFailure
    }

    Write-Host "Stage Mode   : project-local unified module; no game directory was modified"
    Write-Host "Stage Result : success"
    Write-Host "Output       : $projectStageDir"
    return
}

$legacy13ModuleDir = Get-FullPathSafe -Path (Join-Path $modulesDir "AnimusForge_1_3_x")
$legacy14ModuleDir = Get-FullPathSafe -Path (Join-Path $modulesDir "AnimusForge_1_4_5")
$existingLegacyModules = @($legacy13ModuleDir, $legacy14ModuleDir) | Where-Object {
    Test-Path -LiteralPath $_ -PathType Container
}
Write-Warning "This script never deletes legacy AnimusForge_1_3_x / AnimusForge_1_4_5 modules. Disable or remove those legacy modules manually before launching the game."
if ($existingLegacyModules.Count -gt 0) {
    Write-Warning "Legacy module folder(s) detected and left untouched: $($existingLegacyModules -join ', ')"
}

$operationId = [Guid]::NewGuid().ToString("N")
$stagingModuleDir = Get-FullPathSafe -Path (Join-Path $modulesDir ".$ModuleId.deploy.$Configuration.$operationId")
$backupModuleDir = Get-FullPathSafe -Path (Join-Path $modulesDir ".$ModuleId.backup.$operationId")
Assert-SafeModuleWorkingPath -Path $stagingModuleDir -ModulesDir $modulesDir
Assert-SafeModuleWorkingPath -Path $backupModuleDir -ModulesDir $modulesDir
$modulesVolume = [System.IO.Path]::GetPathRoot($modulesDir)
foreach ($workingPath in @($stagingModuleDir, $backupModuleDir)) {
    if (-not ([System.IO.Path]::GetPathRoot($workingPath)).Equals($modulesVolume, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Transactional module path is not on the Bannerlord Modules volume: $workingPath"
    }
}

$deploymentSucceeded = $false
$backupCreated = $false
$targetMutationStarted = $false
try {
    New-SafeModuleWorkingDirectory -Path $stagingModuleDir -ModulesDir $modulesDir

    $sourceCopyArguments = @(
        "/E",
        "/XD",
        (Join-Path $sourceModuleDir "Logs"),
        (Join-Path $sourceModuleDir "PlayerExports"),
        (Join-Path $sourceModuleDir "bin")
    )
    Invoke-Robocopy -SourceDir $sourceModuleDir -TargetDir $stagingModuleDir -ExtraArguments $sourceCopyArguments
    Merge-InstalledCustomPromptsIntoStaging -SourceModuleDir $sourceModuleDir -TargetModuleDir $targetModuleDir -StagingModuleDir $stagingModuleDir
    $targetTerminalSettings = Join-Path $targetModuleDir "ModuleData\TerminalSettings.json"
    $stagingTerminalSettings = Join-Path $stagingModuleDir "ModuleData\TerminalSettings.json"
    if (Test-Path -LiteralPath $targetTerminalSettings -PathType Leaf) {
        Copy-Item -LiteralPath $targetTerminalSettings -Destination $stagingTerminalSettings -Force
        Write-Host "Preserved TerminalSettings: $targetTerminalSettings"
    }
    Set-SingleModuleIdentity -ModuleDir $stagingModuleDir

    $stagingBinDir = Join-Path $stagingModuleDir "bin\Win64_Shipping_Client"
    Build-DesiredModuleBin -RuntimeDependencyDir $runtimeDependencyDirFull -StagingBinDir $stagingBinDir -Implementation13 $dll13Full -Implementation14 $dll14Full -Bootstrap $bootstrapFull

    $targetLogsDir = Join-Path $targetModuleDir "Logs"
    if (Test-Path -LiteralPath $targetLogsDir -PathType Container) {
        Invoke-Robocopy -SourceDir $targetLogsDir -TargetDir (Join-Path $stagingModuleDir "Logs") -ExtraArguments @("/E")
        Write-Host "Preserved Logs: $targetLogsDir"
    }

    $playerExportSources = @(
        [PSCustomObject]@{ Path = (Join-Path $sourceModuleDir "PlayerExports"); Priority = 10; Label = "source" }
    )
    if (-not (Test-Path -LiteralPath $targetModuleDir -PathType Container)) {
        $playerExportSources += @(
            [PSCustomObject]@{ Path = (Join-Path $legacy13ModuleDir "PlayerExports"); Priority = 20; Label = "legacy-1.3" },
            [PSCustomObject]@{ Path = (Join-Path $legacy14ModuleDir "PlayerExports"); Priority = 30; Label = "legacy-1.4" }
        )
        Write-Host "Migration    : first unified deployment; legacy PlayerExports are read-only merge candidates"
    }
    $playerExportSources += [PSCustomObject]@{ Path = (Join-Path $targetModuleDir "PlayerExports"); Priority = 100; Label = "unified-target" }
    Merge-PlayerExports -DestinationDir (Join-Path $stagingModuleDir "PlayerExports") -Sources $playerExportSources

    $sourceRules = Join-Path $sourceModuleDir "ModuleData\RuleBehaviorPrompts.json"
    Assert-SameHash -SourcePath $sourceRules -TargetPath (Join-Path $stagingModuleDir "ModuleData\RuleBehaviorPrompts.json")
    $sourcePreprocessPrompts = Join-Path $sourceModuleDir "ModuleData\PreprocessPrompts.json"
    Assert-SameHash -SourcePath $sourcePreprocessPrompts -TargetPath (Join-Path $stagingModuleDir "ModuleData\PreprocessPrompts.json")
    Assert-SameHash -SourcePath $bootstrapFull -TargetPath (Join-Path $stagingBinDir "AnimusForge.Bootstrap.dll")
    Assert-SameHash -SourcePath $dll13Full -TargetPath (Join-Path $stagingBinDir "versions\1.3\AnimusForge.dll")
    Assert-SameHash -SourcePath $dll14Full -TargetPath (Join-Path $stagingBinDir "versions\1.4\AnimusForge.dll")
    Assert-SingleModuleLayout -ModuleDir $stagingModuleDir

    Write-Host "Source Module: $sourceModuleDir"
    Write-Host "Runtime DLLs : $runtimeDependencyDirFull"
    Write-Host "Target Module: $targetModuleDir"
    Write-Host "Staged Module: $stagingModuleDir"

    $hadExistingTarget = Test-Path -LiteralPath $targetModuleDir -PathType Container
    $preservedRuntimeDirectoryArguments = if ($hadExistingTarget) {
        @("/XD", "Logs", "PlayerExports", "ONNX")
    }
    else {
        @()
    }
    try {
        if ($hadExistingTarget) {
            # A process can keep the module root as its working directory without
            # locking any file inside it.  Windows then rejects a directory rename
            # even while every DLL/data file is replaceable. Logs and PlayerExports
            # can also remain open while the launcher is alive, and the installed
            # ONNX model must be preserved. Back up and replace only the mutable
            # module subset; the three preserved directories are never touched by
            # deployment or rollback.
            New-SafeModuleWorkingDirectory -Path $backupModuleDir -ModulesDir $modulesDir
            Invoke-Robocopy -SourceDir $targetModuleDir -TargetDir $backupModuleDir -ExtraArguments (@(
                "/MIR",
                "/COPY:DAT",
                "/DCOPY:DAT"
            ) + $preservedRuntimeDirectoryArguments) -RetryCount 3 -WaitSeconds 1
            $backupCreated = $true
            Write-Host "Backup Module: $backupModuleDir"
        }

        # Robocopy may update some files before reporting a later failure, so set
        # this flag before it starts.  Any exception after this point must restore
        # the complete pre-deploy backup rather than merely reporting a copy error.
        $targetMutationStarted = $true
        Invoke-Robocopy -SourceDir $stagingModuleDir -TargetDir $targetModuleDir -ExtraArguments (@(
            "/MIR",
            "/COPY:DAT",
            "/DCOPY:DAT"
        ) + $preservedRuntimeDirectoryArguments) -RetryCount 15 -WaitSeconds 1
        Assert-SingleModuleLayout -ModuleDir $targetModuleDir
        Assert-SameHash -SourcePath $bootstrapFull -TargetPath (Join-Path $targetModuleDir "bin\Win64_Shipping_Client\AnimusForge.Bootstrap.dll")
        Assert-SameHash -SourcePath $dll13Full -TargetPath (Join-Path $targetModuleDir "bin\Win64_Shipping_Client\versions\1.3\AnimusForge.dll")
        Assert-SameHash -SourcePath $dll14Full -TargetPath (Join-Path $targetModuleDir "bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll")
    }
    catch {
        $replacementFailure = $_.Exception.Message
        $rollbackErrors = @()

        if ($hadExistingTarget -and -not $targetMutationStarted) {
            # Backup creation failed before the target was touched.  Never use a
            # partial backup for rollback; remove it if possible and leave the
            # existing module exactly where it is.
            if (Test-Path -LiteralPath $backupModuleDir) {
                try {
                    Remove-SafeModuleWorkingDirectory -Path $backupModuleDir -ModulesDir $modulesDir
                }
                catch {
                    $rollbackErrors += "could not remove incomplete backup: $($_.Exception.Message)"
                }
            }

            if ($rollbackErrors.Count -gt 0) {
                throw "Unified module replacement could not begin; the previous module was left untouched: $replacementFailure`nCleanup also failed: $($rollbackErrors -join '; ')`nIncomplete backup (if present): $backupModuleDir"
            }
            throw "Unified module replacement could not begin; the previous module was left untouched: $replacementFailure"
        }

        if ($hadExistingTarget) {
            $rollbackCompleted = $false
            if ($backupCreated -and (Test-Path -LiteralPath $backupModuleDir -PathType Container)) {
                try {
                    Invoke-Robocopy -SourceDir $backupModuleDir -TargetDir $targetModuleDir -ExtraArguments (@(
                        "/MIR",
                        "/COPY:DAT",
                        "/DCOPY:DAT"
                    ) + $preservedRuntimeDirectoryArguments) -RetryCount 15 -WaitSeconds 1
                    $rollbackCompleted = $true
                }
                catch {
                    $rollbackErrors += "could not restore backup: $($_.Exception.Message)"
                }
            }
            else {
                $rollbackErrors += "complete backup directory is missing: $backupModuleDir"
            }

            if (-not $rollbackCompleted) {
                $rollbackErrors += "the previous unified module was not confirmed restored; preserve this backup: $backupModuleDir"
            }
            elseif (Test-Path -LiteralPath $backupModuleDir) {
                try {
                    Remove-SafeModuleWorkingDirectory -Path $backupModuleDir -ModulesDir $modulesDir
                    $backupCreated = $false
                }
                catch {
                    Write-Warning "The previous module was restored, but its recovery backup could not be cleaned: $backupModuleDir ($($_.Exception.Message))"
                }
            }
        }
        elseif (-not $hadExistingTarget -and (Test-Path -LiteralPath $targetModuleDir)) {
            try {
                Remove-SafeModuleWorkingDirectory -Path $targetModuleDir -ModulesDir $modulesDir
            }
            catch {
                $rollbackErrors += "could not remove failed first-time deployment: $($_.Exception.Message)"
            }
        }

        if ($rollbackErrors.Count -gt 0) {
            throw "Unified module replacement failed: $replacementFailure`nRollback also failed: $($rollbackErrors -join '; ')`nRecovery backup (if present): $backupModuleDir"
        }
        if ($hadExistingTarget) {
            throw "Unified module replacement failed and the previous module was restored from its complete backup: $replacementFailure"
        }
        throw "Unified module replacement failed; no previous unified module existed: $replacementFailure"
    }

    $deploymentSucceeded = $true
}
finally {
    if (-not $deploymentSucceeded -and (Test-Path -LiteralPath $stagingModuleDir)) {
        try {
            Remove-SafeModuleWorkingDirectory -Path $stagingModuleDir -ModulesDir $modulesDir
        }
        catch {
            Write-Warning "Failed to clean deployment staging directory: $stagingModuleDir ($($_.Exception.Message))"
        }
    }
}

if (Test-Path -LiteralPath $backupModuleDir) {
    try {
        Remove-SafeModuleWorkingDirectory -Path $backupModuleDir -ModulesDir $modulesDir
        $backupCreated = $false
    }
    catch {
        Write-Warning "Deployment succeeded, but the old-module backup could not be cleaned: $backupModuleDir ($($_.Exception.Message))"
    }
}
if (Test-Path -LiteralPath $stagingModuleDir) {
    try {
        Remove-SafeModuleWorkingDirectory -Path $stagingModuleDir -ModulesDir $modulesDir
    }
    catch {
        Write-Warning "Deployment succeeded, but the staging directory could not be cleaned: $stagingModuleDir ($($_.Exception.Message))"
    }
}

Sync-PlayerExportsBackToSource -SourceModuleDir $sourceModuleDir -TargetModuleDir $targetModuleDir

Write-Host "Deploy Mode  : one unified module with Bootstrap version selection"
Write-Host "Deploy Result: success"
Write-Host "Output       : $targetModuleDir"
exit 0
