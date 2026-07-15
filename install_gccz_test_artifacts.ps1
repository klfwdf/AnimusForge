[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$GameRoot = 'e:\steam\steamapps\common\Mount & Blade II Bannerlord',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$BackupRoot = '',

    [Parameter()]
    [switch]$SkipProcessCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ScriptRoot)) {
    $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$WorkspaceRoot = Split-Path -Parent $ScriptRoot
if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
    $BackupRoot = Join-Path $WorkspaceRoot 'backups\game-install'
}

$DeployTimestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$DeployBackupRoot = Join-Path $BackupRoot $DeployTimestamp

$ModuleName = 'AnimusForge'
$ArtifactRoot = Join-Path $ScriptRoot 'bin\Debug\single_module_artifacts'
$Artifacts = @(
    [pscustomobject]@{
        Label = 'Bootstrap DLL'
        SourceRelativePath = 'bootstrap\AnimusForge.Bootstrap.dll'
        TargetRelativePath = 'AnimusForge.Bootstrap.dll'
    },
    [pscustomobject]@{
        Label = 'Bootstrap PDB'
        SourceRelativePath = 'bootstrap\AnimusForge.Bootstrap.pdb'
        TargetRelativePath = 'AnimusForge.Bootstrap.pdb'
    },
    [pscustomobject]@{
        Label = 'Bootstrap build metadata'
        SourceRelativePath = 'bootstrap\AnimusForge.Bootstrap.build.json'
        TargetRelativePath = 'AnimusForge.Bootstrap.build.json'
    },
    [pscustomobject]@{
        Label = '1.3 implementation DLL'
        SourceRelativePath = 'versions\1.3\AnimusForge.dll'
        TargetRelativePath = 'versions\1.3\AnimusForge.dll'
    },
    [pscustomobject]@{
        Label = '1.3 implementation PDB'
        SourceRelativePath = 'versions\1.3\AnimusForge.pdb'
        TargetRelativePath = 'versions\1.3\AnimusForge.pdb'
    },
    [pscustomobject]@{
        Label = '1.3 implementation build metadata'
        SourceRelativePath = 'versions\1.3\AnimusForge.build.json'
        TargetRelativePath = 'versions\1.3\AnimusForge.build.json'
    },
    [pscustomobject]@{
        Label = '1.4 implementation DLL'
        SourceRelativePath = 'versions\1.4\AnimusForge.dll'
        TargetRelativePath = 'versions\1.4\AnimusForge.dll'
    },
    [pscustomobject]@{
        Label = '1.4 implementation PDB'
        SourceRelativePath = 'versions\1.4\AnimusForge.pdb'
        TargetRelativePath = 'versions\1.4\AnimusForge.pdb'
    },
    [pscustomobject]@{
        Label = '1.4 implementation build metadata'
        SourceRelativePath = 'versions\1.4\AnimusForge.build.json'
        TargetRelativePath = 'versions\1.4\AnimusForge.build.json'
    }
)

function Get-Sha256String {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $Sha256 = [System.Security.Cryptography.SHA256]::Create()
    $Stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::ReadWrite
    )

    try {
        $HashBytes = $Sha256.ComputeHash($Stream)
        return -join ($HashBytes | ForEach-Object { $_.ToString('X2') })
    }
    finally {
        $Stream.Dispose()
        $Sha256.Dispose()
    }
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description not found: $Path"
    }
}

function Assert-DirectoryExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Description not found: $Path"
    }
}

function Assert-MatchingHash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedHash,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $ActualHash = Get-Sha256String -Path $Path
    if ($ActualHash -ne $ExpectedHash.ToUpperInvariant()) {
        throw "$Description hash mismatch. Expected $ExpectedHash, got $ActualHash. Path: $Path"
    }

    return $ActualHash
}

function Assert-TargetWritable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        $Stream = $null
        try {
            $Stream = [System.IO.File]::Open(
                $Path,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::ReadWrite,
                [System.IO.FileShare]::ReadWrite
            )
        }
        catch {
            throw "Cannot open $Description for writing: $Path. Close Bannerlord/launcher/Steam file operations, or run this script from a normal/elevated PowerShell outside the Codex sandbox. $($_.Exception.Message)"
        }
        finally {
            if ($null -ne $Stream) {
                $Stream.Dispose()
            }
        }

        return
    }

    $ParentDirectory = Split-Path -Parent $Path
    Assert-DirectoryExists -Path $ParentDirectory -Description "$Description parent directory"

    if ($WhatIfPreference) {
        Write-Host "WhatIf: skipped missing-target write probe for $Description"
        return
    }

    $ProbePath = Join-Path $ParentDirectory ".afmod-write-probe-$([guid]::NewGuid().ToString('N')).tmp"
    $Stream = $null
    try {
        $Stream = [System.IO.File]::Open(
            $ProbePath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::None
        )
    }
    catch {
        throw "Cannot create write probe for $Description in $ParentDirectory. Run this script from a normal/elevated PowerShell outside the Codex sandbox. $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $Stream) {
            $Stream.Dispose()
        }

        if (Test-Path -LiteralPath $ProbePath -PathType Leaf) {
            Remove-Item -LiteralPath $ProbePath -Force
        }
    }
}

function New-DirectoryIfMissing {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path -PathType Container) {
        return
    }

    if ($PSCmdlet.ShouldProcess($Path, 'Create directory')) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Copy-ArtifactFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath,

        [Parameter(Mandatory = $true)]
        [string]$BackupDirectory,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $SourceHash = Get-Sha256String -Path $SourcePath
    Write-Host "Verified artifact ${Label}: $SourceHash"

    New-DirectoryIfMissing -Path $BackupDirectory

    if (Test-Path -LiteralPath $DestinationPath -PathType Leaf) {
        $DestinationHashBefore = Get-Sha256String -Path $DestinationPath
        $BackupPath = Join-Path $BackupDirectory (Split-Path -Leaf $DestinationPath)
        if ($PSCmdlet.ShouldProcess($BackupPath, "Back up $DestinationPath")) {
            Copy-Item -LiteralPath $DestinationPath -Destination $BackupPath -Force
        }

        Write-Host "Backed up existing ${Label} hash: $DestinationHashBefore"
    }
    else {
        Write-Warning "Existing target file is missing; no backup was made for $Label`: $DestinationPath"
    }

    if ($PSCmdlet.ShouldProcess($DestinationPath, "Install $SourcePath")) {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
    }

    if (-not $WhatIfPreference) {
        $DestinationHashAfter = Assert-MatchingHash -Path $DestinationPath -ExpectedHash $SourceHash -Description "$Label installed target"
        Write-Host "Verified installed ${Label}: $DestinationHashAfter"
    }
    else {
        Write-Host "WhatIf: skipped installed-target hash check for ${Label}"
    }
}

if (-not $SkipProcessCheck) {
    $BlockedProcessNames = @(
        'Bannerlord',
        'Bannerlord.Native',
        'TaleWorlds.MountAndBlade.Launcher'
    )

    $RunningProcesses = Get-Process -ErrorAction SilentlyContinue |
        Where-Object {
            $BlockedProcessNames -contains $_.ProcessName -or
            $_.ProcessName -like '*Bannerlord*' -or
            $_.ProcessName -like '*TaleWorlds.MountAndBlade*'
        } |
        Select-Object -Property ProcessName, Id, Path

    if ($RunningProcesses) {
        $ProcessText = $RunningProcesses | Format-Table -AutoSize | Out-String
        throw "Bannerlord/TaleWorlds process is still running. Close it before installing.`n$ProcessText"
    }
}

Assert-DirectoryExists -Path $GameRoot -Description 'Bannerlord game root'

Write-Host "Game root: $GameRoot"
Write-Host "Backup root for this install: $DeployBackupRoot"
Write-Host "WhatIf mode: $WhatIfPreference"

$ModuleRoot = Join-Path (Join-Path $GameRoot 'Modules') $ModuleName
$TargetBin = Join-Path $ModuleRoot 'bin\Win64_Shipping_Client'
Assert-DirectoryExists -Path $ModuleRoot -Description "$ModuleName module root"
Assert-DirectoryExists -Path $TargetBin -Description "$ModuleName Win64_Shipping_Client"
Assert-DirectoryExists -Path $ArtifactRoot -Description 'single-module artifact root'

foreach ($Artifact in $Artifacts) {
    $SourcePath = Join-Path $ArtifactRoot $Artifact.SourceRelativePath
    $TargetPath = Join-Path $TargetBin $Artifact.TargetRelativePath
    $TargetDirectory = Split-Path -Parent $TargetPath
    $BackupDirectory = Join-Path $DeployBackupRoot (Split-Path -Parent $Artifact.TargetRelativePath)

    Assert-FileExists -Path $SourcePath -Description $Artifact.Label
    New-DirectoryIfMissing -Path $TargetDirectory
    Assert-TargetWritable -Path $TargetPath -Description $Artifact.Label

    Write-Host ''
    Write-Host "Installing $($Artifact.Label)"
    Copy-ArtifactFile -SourcePath $SourcePath -DestinationPath $TargetPath -BackupDirectory $BackupDirectory -Label $Artifact.Label
}

Write-Host ''
if ($WhatIfPreference) {
    Write-Host 'WhatIf complete: no files were written.'
}
else {
    Write-Host "Install complete. Backups: $DeployBackupRoot"
}
