param([string]$ProjectRoot = (Join-Path $PSScriptRoot '..\..'))
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
. (Join-Path $ProjectRoot 'scripts\build\coup_seam_gate.ps1')
$script:Count = 0
function Check { param([bool]$Ok,[string]$Message) if (-not $Ok) { throw "GATE_FAIL $Message" }; $script:Count++; Write-Output "GATE_PASS $Message" }
function Rejected { param([scriptblock]$Action) try { & $Action | Out-Null; return $false } catch { Write-Host ("EXPECTED_OR_DIAGNOSTIC: " + $_.Exception.Message); return $true } }
function Import-Function {
    param([string]$File,[string]$Name)
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($File,[ref]$null,[ref]$errors)
    if ($errors.Count) { throw "Parse failed: $File" }
    $fn = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $Name },$true)
    if ($null -eq $fn) { throw "Function missing: $Name" }
    Set-Item -Path "Function:script:$Name" -Value ([scriptblock]::Create($fn.Body.Extent.Text.Substring(1,$fn.Body.Extent.Text.Length-2)))
}
$deploy = Join-Path $ProjectRoot 'scripts\build\deploy_module.ps1'
$package = Join-Path $ProjectRoot 'scripts\build\package_mod.ps1'
foreach ($name in @('Get-FileSha256','Get-BuildMarkerPath','Assert-BuildMarker')) { Import-Function $deploy $name }
foreach ($name in @('Test-BuildMarker','Get-RequiredZipEntry','Read-ZipEntryText','Get-ZipEntrySha256','Assert-ZipBuildMarker')) { Import-Function $package $name }
$BootstrapAssemblyName = 'AnimusForge.Bootstrap'
$RequiredBuildMarkerSchemaVersion = 2
$dir = Join-Path $ProjectRoot 'artifacts\coup-memory-seam-20261005\gate-fixtures'
New-Item -ItemType Directory -Path $dir -Force | Out-Null
$dll = Join-Path $dir 'AnimusForge.dll'
[IO.File]::WriteAllBytes($dll,[byte[]](1,2,3,4))
$hash = Get-CoupGateHash $dll
$dummy = 'A' * 64
function New-Marker {
    return [pscustomobject]@{
        SchemaVersion=2; Role='Implementation'; FileName='AnimusForge.dll'; AssemblyName='AnimusForge'
        Sha256=$hash; BannerlordApi='1.3'; BuildFlavor='ANIMUSFORGE_BANNERLORD_API_1_3'
        ReferenceGameVersion='v1.3.15.110062'; CreatedUtc=[DateTime]::UtcNow.ToString('o')
        CoupSeamGate=[pscustomobject]@{SchemaVersion=1; Status='Passed'; BannerlordApi='1.3'; CandidateSha256=$hash; ReferenceGameVersion='v1.3.15.110062'; ProbeSha256=$dummy; ReportSha256=$dummy}
    }
}
$path = Join-Path $dir 'AnimusForge.build.json'
$cases = @('valid','missing','failed','candidate','api','version','schema','probe-hash','report-hash')
Add-Type -AssemblyName System.IO.Compression
foreach ($case in $cases) {
    $marker = New-Marker
    switch ($case) {
        'missing' { $marker.PSObject.Properties.Remove('CoupSeamGate') }
        'failed' { $marker.CoupSeamGate.Status='Failed' }
        'candidate' { $marker.CoupSeamGate.CandidateSha256=$dummy }
        'api' { $marker.CoupSeamGate.BannerlordApi='1.4' }
        'version' { $marker.CoupSeamGate.ReferenceGameVersion='v1.3.5.1' }
        'schema' { $marker.CoupSeamGate.SchemaVersion=99 }
        'probe-hash' { $marker.CoupSeamGate.ProbeSha256='missing' }
        'report-hash' { $marker.CoupSeamGate.ReportSha256='' }
    }
    $marker | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding UTF8
    $expectPass = $case -eq 'valid'
    Check ((-not (Rejected { Assert-CoupSeamReceipt $marker $hash '1.3' })) -eq $expectPass) "shared receipt: $case"
    Check ((-not (Rejected { Assert-BuildMarker -DllPath $dll -ExpectedRole Implementation -ExpectedApi '1.3' -ExpectedFlavor ANIMUSFORGE_BANNERLORD_API_1_3 -ExpectedReferenceMinor 3 })) -eq $expectPass) "deployment preflight: $case"
    Check ((Test-BuildMarker -DllPath $dll -ExpectedRole Implementation -ExpectedApi '1.3' -ExpectedFlavor ANIMUSFORGE_BANNERLORD_API_1_3 -ExpectedReferenceMinor 3) -eq $expectPass) "package directory preflight: $case"
    $stream = New-Object IO.MemoryStream
    $archive = New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create,$true)
    foreach ($name in @('AnimusForge.dll','AnimusForge.build.json')) {
        $entry = $archive.CreateEntry($name); $out=$entry.Open()
        try { $bytes=[IO.File]::ReadAllBytes((Join-Path $dir $name)); $out.Write($bytes,0,$bytes.Length) } finally { $out.Dispose() }
    }
    $archive.Dispose(); $stream.Position=0
    $archive = New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Read,$true)
    try {
        Check ((-not (Rejected { Assert-ZipBuildMarker -Archive $archive -DllEntryName AnimusForge.dll -MarkerEntryName AnimusForge.build.json -ExpectedRole Implementation -ExpectedAssemblyName AnimusForge -ExpectedApi '1.3' -ExpectedFlavor ANIMUSFORGE_BANNERLORD_API_1_3 -ExpectedReferenceMinor 3 })) -eq $expectPass) "ZIP artifact preflight: $case"
    } finally { $archive.Dispose(); $stream.Dispose() }
}
$buildText = Get-Content -Raw -LiteralPath (Join-Path $ProjectRoot 'scripts\build\build_single_module.ps1')
Check ($buildText.IndexOf('Invoke-CoupSeamGate -ProjectRoot') -lt $buildText.IndexOf('if ($Stage) {')) 'gate precedes staging'
Check ($buildText.IndexOf('Invoke-CoupSeamGate -ProjectRoot') -lt $buildText.LastIndexOf('if ($Deploy) {')) 'gate precedes deployment'
Check (($buildText -split 'Invoke-CoupSeamGate -ProjectRoot').Length -eq 3) 'both implementation candidates must pass'
Write-Output "PASS release-gate regression assertions=$script:Count"
Write-Output 'GATE_SCOPE actual validator functions, synthetic DLL bytes/markers, memory ZIP only; no real deployment/package.'
