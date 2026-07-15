param(
    [string]$ProjectRoot = "",
    [string]$BannerlordRoot = "",
    [string]$Bannerlord13ReferenceDir = "",
    [string]$WorkshopContentDir = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Stage,
    [switch]$Deploy
)

$ErrorActionPreference = "Stop"
if ($Stage -and $Deploy) {
    throw "-Stage and -Deploy are mutually exclusive. Use -Stage for project-local output or -Deploy for Modules\\AnimusForge."
}
$ImplementationProjectName = "AnimusForge.csproj"
$BootstrapProjectRelativePath = "AnimusForge.Bootstrap\AnimusForge.Bootstrap.csproj"
$FlavorKey = "AnimusForge.BuildFlavor"
$ApiKey = "AnimusForge.BannerlordApi"

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

function Get-EmbeddedBannerlordVersion {
    param([Parameter(Mandatory = $true)][string]$ReferenceDir)

    $referenceDirFull = Get-FullPathSafe -Path $ReferenceDir
    $libraryPath = Join-Path $referenceDirFull "TaleWorlds.Library.dll"
    if (-not (Test-Path -LiteralPath $libraryPath -PathType Leaf)) {
        throw "TaleWorlds.Library.dll was not found in the reference directory: $referenceDirFull"
    }

    $binaryText = [System.Text.Encoding]::Unicode.GetString([System.IO.File]::ReadAllBytes($libraryPath))
    $versionTexts = @([regex]::Matches($binaryText, 'v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.(?<build>\d+)') | ForEach-Object {
        $_.Value
    } | Sort-Object -Unique)
    if ($versionTexts.Count -ne 1) {
        throw "Could not read one unambiguous BuildInfo game version from: $libraryPath"
    }

    $versionMatch = [regex]::Match($versionTexts[0], '^v(?<major>\d+)\.(?<minor>\d+)\.')
    return [PSCustomObject]@{
        Text = $versionTexts[0]
        Major = [int]$versionMatch.Groups['major'].Value
        Minor = [int]$versionMatch.Groups['minor'].Value
        Directory = $referenceDirFull
    }
}

function Assert-ReferenceApiLine {
    param(
        [Parameter(Mandatory = $true)][string]$ReferenceDir,
        [Parameter(Mandatory = $true)][int]$ExpectedMinor,
        [Parameter(Mandatory = $true)][string[]]$RequiredFiles,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $version = Get-EmbeddedBannerlordVersion -ReferenceDir $ReferenceDir
    if ($version.Major -ne 1 -or $version.Minor -ne $ExpectedMinor) {
        throw "$Label must use Bannerlord 1.$ExpectedMinor.x references, but '$($version.Text)' was found in '$($version.Directory)'."
    }
    foreach ($fileName in $RequiredFiles) {
        $requiredPath = Join-Path $version.Directory $fileName
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "$Label reference is incomplete. Missing: $requiredPath"
        }
    }
    return $version
}

function Resolve-Bannerlord13ReferenceDir {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [string]$RequestedDir
    )

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($RequestedDir)) {
        $candidates.Add((Get-FullPathSafe -Path $RequestedDir))
    }
    else {
        $candidates.Add((Get-FullPathSafe -Path (Join-Path $Root "_deps_auto")))
        $candidates.Add((Get-FullPathSafe -Path (Join-Path $Root "..\_deps_261550_managed")))
    }

    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in @($candidates | Select-Object -Unique)) {
        try {
            $version = Assert-ReferenceApiLine -ReferenceDir $candidate -ExpectedMinor 3 -Label "Bannerlord 1.3 implementation/Bootstrap" -RequiredFiles @(
                "Newtonsoft.Json.dll",
                "SandBox.View.dll",
                "SandBox.ViewModelCollection.dll",
                "TaleWorlds.Library.dll",
                "TaleWorlds.Core.dll",
                "TaleWorlds.DotNet.dll",
                "TaleWorlds.Engine.dll",
                "TaleWorlds.Engine.GauntletUI.dll",
                "TaleWorlds.MountAndBlade.dll",
                "TaleWorlds.CampaignSystem.dll",
                "TaleWorlds.CampaignSystem.ViewModelCollection.dll",
                "TaleWorlds.GauntletUI.dll",
                "TaleWorlds.InputSystem.dll",
                "TaleWorlds.Localization.dll",
                "TaleWorlds.MountAndBlade.View.dll",
                "TaleWorlds.MountAndBlade.ViewModelCollection.dll",
                "TaleWorlds.ObjectSystem.dll",
                "TaleWorlds.ScreenSystem.dll"
            )
            Write-Host "1.3 References: $($version.Directory) [$($version.Text)]"
            return $version.Directory
        }
        catch {
            $errors.Add($_.Exception.Message)
        }
    }

    throw "A verified Bannerlord 1.3.x reference directory is required; the build will not mark a DLL as 1.3 after compiling against another API line.`n$($errors -join "`n")"
}

function Assert-PathUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $pathFull = (Get-FullPathSafe -Path $Path).TrimEnd('\', '/')
    $rootFull = (Get-FullPathSafe -Path $Root).TrimEnd('\', '/')
    if (-not $pathFull.StartsWith($rootFull + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the project root: $pathFull"
    }
}

function Reset-Directory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$AllowedRoot
    )

    Assert-PathUnderRoot -Path $Path -Root $AllowedRoot
    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to reset a build directory through a reparse point: $Path"
        }
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Keep-OnlyBuildArtifacts {
    param(
        [Parameter(Mandatory = $true)][string]$OutputDir,
        [Parameter(Mandatory = $true)][string[]]$AllowedFileNames,
        [Parameter(Mandatory = $true)][string]$AllowedRoot
    )

    Assert-PathUnderRoot -Path $OutputDir -Root $AllowedRoot
    $outputItem = Get-Item -LiteralPath $OutputDir -Force
    if (($outputItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Refusing to prune a build directory through a reparse point: $OutputDir"
    }
    Get-ChildItem -LiteralPath $OutputDir -Force | Where-Object {
        $_.PSIsContainer -or $AllowedFileNames -notcontains $_.Name
    } | ForEach-Object {
        Assert-PathUnderRoot -Path $_.FullName -Root $AllowedRoot
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}

function Assert-AssemblyName {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ExpectedName
    )

    if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
        throw "Built DLL not found: $DllPath"
    }

    $actualName = [System.Reflection.AssemblyName]::GetAssemblyName($DllPath).Name
    if (-not $actualName.Equals($ExpectedName, [System.StringComparison]::Ordinal)) {
        throw "Unexpected assembly name in '$DllPath': expected '$ExpectedName', actual '$actualName'."
    }
}

function Assert-BuildPdb {
    param([Parameter(Mandatory = $true)][string]$DllPath)

    $pdbPath = [System.IO.Path]::ChangeExtension($DllPath, ".pdb")
    if (-not (Test-Path -LiteralPath $pdbPath -PathType Leaf)) {
        throw "Built PDB not found: $pdbPath"
    }
}

function Assert-ImplementationFlavor {
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
}

function Write-BuildMarker {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$Role,
        [string]$BannerlordApi = "",
        [string]$BuildFlavor = "",
        [string]$ReferenceGameVersion = ""
    )

    $markerPath = Join-Path (Split-Path -Parent $DllPath) (([System.IO.Path]::GetFileNameWithoutExtension($DllPath)) + ".build.json")
    $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($DllPath).Name
    $marker = [ordered]@{
        SchemaVersion = 2
        Role = $Role
        FileName = [System.IO.Path]::GetFileName($DllPath)
        AssemblyName = $assemblyName
        BannerlordApi = $BannerlordApi
        BuildFlavor = $BuildFlavor
        ReferenceGameVersion = $ReferenceGameVersion
        Sha256 = Get-FileSha256 -LiteralPath $DllPath
        CreatedUtc = [DateTime]::UtcNow.ToString("o")
    }
    $marker | ConvertTo-Json | Set-Content -LiteralPath $markerPath -Encoding UTF8
    Write-Host "Marker      : $markerPath"
}

function Invoke-DotNetBuild {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$OutputDir,
        [Parameter(Mandatory = $true)][string]$IntermediateDir,
        [string]$BannerlordApi = "",
        [string]$VersionedReferenceDir = "",
        [string]$BootstrapReferenceDir = ""
    )

    Reset-Directory -Path $OutputDir -AllowedRoot $projectRootFull
    Reset-Directory -Path $IntermediateDir -AllowedRoot $projectRootFull
    $outputPropertyPath = ($OutputDir -replace "\\", "/").TrimEnd('/') + "/"
    $intermediatePropertyPath = ($IntermediateDir -replace "\\", "/").TrimEnd('/') + "/"

    $arguments = @(
        "build",
        $ProjectPath,
        "-c", $Configuration,
        "/p:OutputPath=$outputPropertyPath",
        "/p:AppendTargetFrameworkToOutputPath=false",
        "/p:BaseIntermediateOutputPath=$intermediatePropertyPath",
        "/p:IntermediateOutputPath=$intermediatePropertyPath",
        "/p:MSBuildProjectExtensionsPath=$intermediatePropertyPath",
        "/p:CopyLegacyAnimusForgeModuleData=false"
    )

    if (-not [string]::IsNullOrWhiteSpace($BannerlordApi)) {
        $arguments += "/p:BannerlordApi=$BannerlordApi"
        $arguments += "/p:BannerlordApiCompatibility=$BannerlordApi"
    }
    if (-not [string]::IsNullOrWhiteSpace($VersionedReferenceDir)) {
        $arguments += "/p:VersionedDepsDir=$VersionedReferenceDir"
        if (-not [string]::IsNullOrWhiteSpace($BannerlordApi) -and -not $BannerlordApi.StartsWith("1.4", [System.StringComparison]::Ordinal)) {
            $arguments += "/p:Bannerlord13ReferencesVerified=true"
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($BootstrapReferenceDir)) {
        $arguments += "/p:BootstrapReferenceDir=$BootstrapReferenceDir"
    }
    if (-not [string]::IsNullOrWhiteSpace($BannerlordRoot)) {
        $arguments += "/p:BannerlordRoot=$BannerlordRoot"
    }
    if (-not [string]::IsNullOrWhiteSpace($WorkshopContentDir)) {
        $arguments += "/p:WorkshopContentDir=$WorkshopContentDir"
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for '$ProjectPath' with exit code $LASTEXITCODE."
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot ".."
}
$projectRootFull = Get-FullPathSafe -Path $ProjectRoot
$implementationProject = Join-Path $projectRootFull $ImplementationProjectName
$bootstrapProject = Join-Path $projectRootFull $BootstrapProjectRelativePath

foreach ($requiredProject in @($implementationProject, $bootstrapProject)) {
    if (-not (Test-Path -LiteralPath $requiredProject -PathType Leaf)) {
        throw "Required project not found: $requiredProject"
    }
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK was not found in PATH."
}

if ([string]::IsNullOrWhiteSpace($BannerlordRoot)) {
    throw "-BannerlordRoot is required so the 1.4 reference line can be verified."
}
$bannerlordRootFull = Get-FullPathSafe -Path $BannerlordRoot
$bannerlord14ReferenceDir = Join-Path $bannerlordRootFull "bin\Win64_Shipping_Client"
$version14 = Assert-ReferenceApiLine -ReferenceDir $bannerlord14ReferenceDir -ExpectedMinor 4 -Label "Bannerlord 1.4 implementation" -RequiredFiles @(
    "TaleWorlds.Library.dll",
    "TaleWorlds.Core.dll",
    "TaleWorlds.MountAndBlade.dll",
    "TaleWorlds.CampaignSystem.dll"
)
$bannerlord13ReferenceDirFull = Resolve-Bannerlord13ReferenceDir -Root $projectRootFull -RequestedDir $Bannerlord13ReferenceDir
$version13 = Get-EmbeddedBannerlordVersion -ReferenceDir $bannerlord13ReferenceDirFull
Write-Host "1.4 References: $($version14.Directory) [$($version14.Text)]"

$artifactRoot = Join-Path $projectRootFull "bin\$Configuration\single_module_artifacts"
$intermediateRoot = Join-Path $projectRootFull "obj\single_module\$Configuration"
Reset-Directory -Path $artifactRoot -AllowedRoot $projectRootFull
Reset-Directory -Path $intermediateRoot -AllowedRoot $projectRootFull
$dll13 = Join-Path $artifactRoot "versions\1.3\AnimusForge.dll"
$dll14 = Join-Path $artifactRoot "versions\1.4\AnimusForge.dll"
$bootstrapDll = Join-Path $artifactRoot "bootstrap\AnimusForge.Bootstrap.dll"
$flavor13 = "ANIMUSFORGE_BANNERLORD_API_1_3"
$flavor14 = "ANIMUSFORGE_BANNERLORD_API_1_4"

Write-Host "[1/3] Building AnimusForge implementation for Bannerlord 1.3.x..."
Invoke-DotNetBuild -ProjectPath $implementationProject -OutputDir (Split-Path -Parent $dll13) -IntermediateDir (Join-Path $intermediateRoot "implementation_1.3") -BannerlordApi "1.3" -VersionedReferenceDir $bannerlord13ReferenceDirFull
Keep-OnlyBuildArtifacts -OutputDir (Split-Path -Parent $dll13) -AllowedFileNames @("AnimusForge.dll", "AnimusForge.pdb") -AllowedRoot $artifactRoot
Assert-ImplementationFlavor -DllPath $dll13 -ExpectedApi "1.3" -ExpectedFlavor $flavor13 -UnexpectedFlavor $flavor14
Assert-BuildPdb -DllPath $dll13
Write-BuildMarker -DllPath $dll13 -Role "Implementation" -BannerlordApi "1.3" -BuildFlavor $flavor13 -ReferenceGameVersion $version13.Text

Write-Host ""
Write-Host "[2/3] Building AnimusForge implementation for Bannerlord 1.4.x..."
Invoke-DotNetBuild -ProjectPath $implementationProject -OutputDir (Split-Path -Parent $dll14) -IntermediateDir (Join-Path $intermediateRoot "implementation_1.4") -BannerlordApi "1.4"
Keep-OnlyBuildArtifacts -OutputDir (Split-Path -Parent $dll14) -AllowedFileNames @("AnimusForge.dll", "AnimusForge.pdb") -AllowedRoot $artifactRoot
Assert-ImplementationFlavor -DllPath $dll14 -ExpectedApi "1.4" -ExpectedFlavor $flavor14 -UnexpectedFlavor $flavor13
Assert-BuildPdb -DllPath $dll14
Write-BuildMarker -DllPath $dll14 -Role "Implementation" -BannerlordApi "1.4" -BuildFlavor $flavor14 -ReferenceGameVersion $version14.Text

if ((Get-FileSha256 -LiteralPath $dll13) -eq (Get-FileSha256 -LiteralPath $dll14)) {
    throw "The 1.3 and 1.4 implementation DLL hashes are identical. Refusing a potentially swapped build."
}

Write-Host ""
Write-Host "[3/3] Building AnimusForge Bootstrap..."
Invoke-DotNetBuild -ProjectPath $bootstrapProject -OutputDir (Split-Path -Parent $bootstrapDll) -IntermediateDir (Join-Path $intermediateRoot "bootstrap") -BootstrapReferenceDir $bannerlord13ReferenceDirFull
Keep-OnlyBuildArtifacts -OutputDir (Split-Path -Parent $bootstrapDll) -AllowedFileNames @("AnimusForge.Bootstrap.dll", "AnimusForge.Bootstrap.pdb") -AllowedRoot $artifactRoot
Assert-AssemblyName -DllPath $bootstrapDll -ExpectedName "AnimusForge.Bootstrap"
Assert-BuildPdb -DllPath $bootstrapDll
Write-BuildMarker -DllPath $bootstrapDll -Role "Bootstrap" -BannerlordApi "1.3-compatible baseline" -ReferenceGameVersion $version13.Text

Write-Host ""
Write-Host "Build Result : success"
Write-Host "Bootstrap    : $bootstrapDll"
Write-Host "Version 1.3 : $dll13"
Write-Host "Version 1.4 : $dll14"

if ($Stage -or $Deploy) {
    $deployScript = Join-Path $PSScriptRoot "deploy_module.ps1"
    if (-not (Test-Path -LiteralPath $deployScript -PathType Leaf)) {
        throw "Deploy script not found: $deployScript"
    }
}

if ($Stage) {
    $stageOutputDir = Join-Path $projectRootFull "bin\$Configuration\single_module_stage\AnimusForge"
    Write-Host ""
    Write-Host "Assembling the project-local unified module staging output..."
    & $deployScript -ProjectRoot $projectRootFull -BannerlordRoot $BannerlordRoot -Configuration $Configuration -BuildDll13 $dll13 -BuildDll14 $dll14 -BootstrapDll $bootstrapDll -StageOnlyOutputDir $stageOutputDir
}

if ($Deploy) {
    if ([string]::IsNullOrWhiteSpace($BannerlordRoot)) {
        throw "-Deploy requires -BannerlordRoot."
    }

    Write-Host ""
    Write-Host "Deploying the unified module..."
    & $deployScript -ProjectRoot $projectRootFull -BannerlordRoot $BannerlordRoot -Configuration $Configuration -BuildDll13 $dll13 -BuildDll14 $dll14 -BootstrapDll $bootstrapDll
}
