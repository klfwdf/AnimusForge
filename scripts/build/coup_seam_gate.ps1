# Shared fail-closed validation. This file only defines functions when dot-sourced.
function Get-CoupGateHash {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Get-CoupGateReferenceVersion {
    param([Parameter(Mandatory = $true)][string]$Directory)
    $library = Join-Path $Directory "TaleWorlds.Library.dll"
    if (-not (Test-Path -LiteralPath $library -PathType Leaf)) { return "" }
    $text = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes($library))
    $versions = @([regex]::Matches($text, 'v\d+\.\d+\.\d+\.\d+') | ForEach-Object { $_.Value } | Sort-Object -Unique)
    if ($versions.Count -ne 1) { throw "Ambiguous game reference version: $library" }
    return $versions[0]
}

function Assert-CoupSeamReceipt {
    param(
        [Parameter(Mandatory = $true)]$Marker,
        [Parameter(Mandatory = $true)][string]$CandidateHash,
        [Parameter(Mandatory = $true)][string]$ExpectedApi
    )
    $gate = $Marker.CoupSeamGate
    if ($null -eq $gate -or [int]$gate.SchemaVersion -ne 1 -or
        [string]$gate.Status -ne "Passed" -or [string]$gate.BannerlordApi -ne $ExpectedApi -or
        -not ([string]$gate.CandidateSha256).Equals($CandidateHash, [StringComparison]::OrdinalIgnoreCase) -or
        [string]$gate.ReferenceGameVersion -ne [string]$Marker.ReferenceGameVersion -or
        [string]$gate.ProbeSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
        [string]$gate.ReportSha256 -notmatch '^[A-Fa-f0-9]{64}$') {
        throw "Coup seam gate missing, failed or bound to another DLL/API. Rebuild through build_single_module.ps1; refusing unverified delivery."
    }
}

function Invoke-CoupSeamGate {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$GameRoot,
        [Parameter(Mandatory = $true)][string]$DllPath,
        [Parameter(Mandatory = $true)][string]$ReferenceDir,
        [Parameter(Mandatory = $true)][string]$ExpectedApi,
        [Parameter(Mandatory = $true)][string]$ProbePath
    )
    $markerPath = Join-Path (Split-Path -Parent $DllPath) "AnimusForge.build.json"
    $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
    $hash = Get-CoupGateHash $DllPath
    $version = Get-CoupGateReferenceVersion $ReferenceDir
    if ($version -notmatch ('^v1\.' + ($ExpectedApi.Split('.')[1]) + '\.') -or
        $marker.ReferenceGameVersion -ne $version -or $marker.Sha256 -ne $hash -or $marker.BannerlordApi -ne $ExpectedApi) {
        throw "Candidate DLL/build marker/reference version mismatch before coup seam gate: $DllPath"
    }
    # A failed rerun must invalidate a previous pass before any probe executes.
    if ($null -ne $marker.CoupSeamGate) {
        $marker.PSObject.Properties.Remove('CoupSeamGate')
        $marker | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $markerPath -Encoding UTF8
    }
    $dirs = New-Object System.Collections.Generic.List[string]
    $dirs.Add([IO.Path]::GetFullPath($ReferenceDir))
    $cached = Join-Path $ProjectRoot (".tmp\build_check\" + $ExpectedApi)
    if ((Get-CoupGateReferenceVersion $cached) -eq $version) { $dirs.Add([IO.Path]::GetFullPath($cached)) }
    $liveBin = Join-Path $GameRoot "bin\Win64_Shipping_Client"
    if ((Get-CoupGateReferenceVersion $liveBin) -eq $version) {
        $dirs.Add([IO.Path]::GetFullPath($liveBin))
        foreach ($module in @('Native','SandBox','SandBoxCore','StoryMode','CustomBattle')) {
            $dir = Join-Path $GameRoot ("Modules\" + $module + "\bin\Win64_Shipping_Client")
            if (Test-Path -LiteralPath $dir -PathType Container) { $dirs.Add([IO.Path]::GetFullPath($dir)) }
        }
    }
    $dirs = @($dirs | Select-Object -Unique)
    if (-not @($dirs | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'SandBox.dll') -PathType Leaf }).Count) {
        throw "Coup registration requires matching real SandBox.dll runtime references ($version), not a different installed game line. Supply a complete reference overlay."
    }
    $output = Join-Path $ProjectRoot ("artifacts\coup-seam-gate\" + $ExpectedApi + "\" + $hash)
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    Write-Host "[Coup gate $ExpectedApi] $version; candidate SHA256=$hash"
    & $ProbePath $GameRoot $DllPath $DllPath $output --seam-only --reference-dirs ($dirs -join '|') --expected-version $version
    $exitCode = $LASTEXITCODE
    $report = Join-Path $output 'registration.log'
    if ($exitCode -ne 0 -or -not (Test-Path -LiteralPath $report -PathType Leaf)) {
        throw "Coup seam gate FAILED for $ExpectedApi (exit=$exitCode). Build cannot stage/deploy/package. Evidence: $output"
    }
    $reportText = Get-Content -Raw -LiteralPath $report
    if ($reportText -notmatch '(?m)^PASS registration smoke\s*$' -or
        $reportText -notmatch '(?m)^PASS memory port regression assertions=\d+\s*$' -or
        (Get-CoupGateHash $DllPath) -ne $hash) { throw "Coup gate report/candidate changed: $output" }
    $dependencies = @([regex]::Matches($reportText, '(?m)^RUNTIME_DEPENDENCY (\S+) MVID=(\S+) SHA256=([A-Fa-f0-9]{64}) path=([^\r\n]+)') | ForEach-Object {
        [ordered]@{ Name = $_.Groups[1].Value; Mvid = $_.Groups[2].Value; Sha256 = $_.Groups[3].Value }
    })
    if ($dependencies.Count -lt 4) { throw "Coup gate dependency manifest incomplete: $output" }
    $receipt = [ordered]@{
        SchemaVersion = 1; Status = "Passed"; BannerlordApi = $ExpectedApi
        CandidateSha256 = $hash; ReferenceGameVersion = $version
        ProbeSha256 = (Get-CoupGateHash $ProbePath); ReportSha256 = (Get-CoupGateHash $report)
        Coverage = "ManagedSeamBindingsAndMemoryAdmissionNotLiveGame"; Dependencies = $dependencies
    }
    $marker | Add-Member -NotePropertyName CoupSeamGate -NotePropertyValue ([pscustomobject]$receipt) -Force
    Assert-CoupSeamReceipt -Marker $marker -CandidateHash $hash -ExpectedApi $ExpectedApi
    $marker | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $markerPath -Encoding UTF8
    $receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'receipt.json') -Encoding UTF8
}
