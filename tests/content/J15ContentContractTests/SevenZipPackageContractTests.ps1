param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts/build/content_layout.ps1')
. (Join-Path $ProjectRoot 'scripts/build/package_sevenzip.ps1')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Assert-AnimusForgePathUnderRoot -Path $RunRoot -Root (Join-Path $ProjectRoot 'artifacts/tests') -Label '7-Zip fixture'
Assert-AnimusForgeNoReparsePoint -Path $RunRoot -Label '7-Zip fixture'
if (Test-Path -LiteralPath $RunRoot) { throw 'Fixture must be new; existing evidence is never cleared.' }
New-Item -ItemType Directory -Path $RunRoot | Out-Null
$script:Checks = 0
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:Checks++
}
function Reject([scriptblock]$Action, [string]$Expected) {
    $actual = ''
    try { & $Action | Out-Null } catch { $actual = $_.Exception.Message }
    Check ($actual.Contains($Expected)) "Expected '$Expected'; received '$actual'."
}
function FixtureText([string]$Path, [string]$Text) {
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($Path)) -Force | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

foreach ($name in @('package_sevenzip.ps1', 'package_mod.ps1')) {
    $parseErrors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $ProjectRoot "scripts/build/$name"), [ref]$null, [ref]$parseErrors)
    Check ($parseErrors.Count -eq 0) "$name must parse."
    if ($name -eq 'package_sevenzip.ps1') {
        $methodArguments = @($ast.FindAll({ param($node)
            $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value.StartsWith('-m0=')
        }, $true))
        Check ($methodArguments.Count -eq 1 -and $methodArguments[0].Value -ceq '-m0=LZMA2:d=16m:fb=273') 'The shared encoder must use exactly the measured LZMA2/16MiB profile.'
        $source = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'scripts/build/package_sevenzip.ps1'))
        foreach ($flag in @('-mx=9', '-ms=on', '-mqs=on', '-mmt=2', '-mtm=off', '-mtc=off', '-mta=off', '-scsUTF-8')) {
            Check ($source.Contains("'$flag'")) "The encoder must retain $flag."
        }
    }
}
$sevenZip = Resolve-AnimusForgeSevenZip
$unicodeName = 'lord_fixture__Vuk Brankovi' + [char]0x0107 + '.json'
$names = @(
    'SubModule.xml',
    'bin/Win64_Shipping_Client/AnimusForge.Bootstrap.dll',
    'bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll',
    'bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll',
    'ModuleData/fixture.json',
    ('PlayerExports/fixture/personality_background/' + $unicodeName)
)
# DLL paths hold fixture text, not game assemblies. No build, version bump or deployment is performed.
foreach ($language in @('CN', 'EN')) {
    $inputDirectory = Join-Path $RunRoot "$language-input"
    foreach ($name in $names) {
        $value = if ($name -eq 'SubModule.xml') { '<Module><Id value="AnimusForge" /></Module>' } else { '{"Fixture":"' + $language + ':' + $name + '"}' }
        FixtureText (Join-Path $inputDirectory ('AnimusForge/' + $name)) $value
    }
    $projection = @($names | ForEach-Object {
        [pscustomobject]@{ Target = $_; Hash = (Get-FileHash -LiteralPath (Join-Path $inputDirectory ('AnimusForge/' + $_)) -Algorithm SHA256).Hash }
    })
    $zipPath = Join-Path $RunRoot "$language-verified.zip"
    $zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($item in $projection) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $inputDirectory ('AnimusForge/' + $item.Target)), ('AnimusForge/' + $item.Target)) | Out-Null
        }
    } finally { $zip.Dispose() }
    $archive = Join-Path $RunRoot "$language-fixture.7z"
    Write-AnimusForgeProjectedSevenZip -VerifiedZipPath $zipPath -ArchivePath $archive -Projection $projection
    Check (Test-Path -LiteralPath $archive) "$language must produce a real 7z archive."
    $listing = & $sevenZip l -slt $archive
    Check ($LASTEXITCODE -eq 0) "$language archive listing must succeed."
    Check (($listing -join "`n") -match 'Method = .*LZMA2') "$language must contain LZMA2 streams."
    $readback = Join-Path $RunRoot "$language-readback"
    & $sevenZip x -y -bsp0 ('-o' + $readback) $archive | Out-Host
    Check ($LASTEXITCODE -eq 0) "$language readback must succeed."
    Check (@(Get-ChildItem -LiteralPath $readback -Force).Count -eq 1 -and (Test-Path -LiteralPath (Join-Path $readback 'AnimusForge'))) 'The archive must have a single AnimusForge root.'
    Check (@(Get-ChildItem -LiteralPath $readback -Recurse -File).Count -eq $projection.Count) 'No projected file may be lost or added.'
    foreach ($item in $projection) {
        $file = Join-Path $readback ('AnimusForge/' + $item.Target)
        Check (Test-Path -LiteralPath $file) "Missing $($item.Target)."
        Check ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ceq $item.Hash) "Hash mismatch: $($item.Target)."
    }
    $beforeHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
    Reject { Write-AnimusForgeProjectedSevenZip -VerifiedZipPath $zipPath -ArchivePath $archive -Projection $projection } 'Refusing to overwrite an archive:'
    Check ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ceq $beforeHash) 'An existing archive must remain byte-for-byte unchanged.'
    $badProjection = @($projection | ForEach-Object { [pscustomobject]@{ Target = $_.Target; Hash = ('0' * 64) } })
    Reject { Write-AnimusForgeProjectedSevenZip -VerifiedZipPath $zipPath -ArchivePath (Join-Path $RunRoot "$language-rejected.7z") -Projection $badProjection } '7-Zip differs from its localization projection:'
    Check (@(Get-ChildItem -LiteralPath $RunRoot -Directory -Filter '.af7z-*').Count -eq 0) 'Private projection work directories must be cleaned after success or failure.'
}
Check ((Get-FileHash -LiteralPath (Join-Path $RunRoot 'CN-readback/AnimusForge/ModuleData/fixture.json')).Hash -cne (Get-FileHash -LiteralPath (Join-Path $RunRoot 'EN-readback/AnimusForge/ModuleData/fixture.json')).Hash) 'CN and EN must retain their own projected content.'

# ASCII-only tar probe isolates compression support from the separately tracked Unicode filename issue.
$tar = Get-Command tar.exe -ErrorAction SilentlyContinue
if ($tar) {
    $asciiRoot = Join-Path $RunRoot 'ascii-input'
    FixtureText (Join-Path $asciiRoot 'AnimusForge/fixture.txt') ('ASCII compression compatibility fixture. ' * 100)
    $includeList = Join-Path $RunRoot 'ascii-include.txt'
    [IO.File]::WriteAllLines($includeList, [string[]]@('AnimusForge/fixture.txt'), [Text.UTF8Encoding]::new($false))
    $asciiArchive = Join-Path $RunRoot 'ascii-fixture.7z'
    Write-AnimusForgeExtremeSevenZip -InputDirectory $asciiRoot -IncludeListPath $includeList -ArchivePath $asciiArchive
    $tarReadback = Join-Path $RunRoot 'tar-readback'
    New-Item -ItemType Directory -Path $tarReadback | Out-Null
    & $tar.Source -xf $asciiArchive -C $tarReadback
    Check ($LASTEXITCODE -eq 0) 'Windows tar must decode the new compression profile for ASCII paths.'
    Check ((Get-FileHash -LiteralPath (Join-Path $asciiRoot 'AnimusForge/fixture.txt')).Hash -ceq (Get-FileHash -LiteralPath (Join-Path $tarReadback 'AnimusForge/fixture.txt')).Hash) 'Windows tar must preserve ASCII payload bytes.'
} else {
    Write-Output 'NOT-RUN: Windows tar is unavailable; no cross-decoder claim is made.'
}
Write-Output "PASS 7-Zip package contract: checks=$script:Checks; real CN/EN ZIP-to-7Z projection, dual implementation paths, Unicode names, SHA256 readback, no-overwrite, invalid projection rejection. Fixture-only; no release/build/deploy."