param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts\build\content_layout.ps1')
Assert-AnimusForgePathUnderRoot -Path $RunRoot -Root (Join-Path $ProjectRoot 'artifacts\tests') -Label 'Package version fixture'
Assert-AnimusForgeNoReparsePoint -Path $RunRoot -Label 'Package version fixture'
if (Test-Path -LiteralPath $RunRoot) { throw 'Fixture must be new; existing evidence is never cleared.' }
New-Item -ItemType Directory -Path $RunRoot | Out-Null

function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:Checks += 1
}
$script:Checks = 0
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $ProjectRoot 'scripts\build\package_mod.ps1'), [ref]$null, [ref]$errors)
Check ($errors.Count -eq 0) 'Package script must parse.'
foreach ($name in @('Get-FullPathSafe', 'Parse-Version', 'Get-NextPatchVersion',
    'Get-NextMicroVersion', 'Get-SubModuleVersion', 'Resolve-PackageVersion', 'Get-VersionedSubModuleBytes')) {
    $definitions = @($ast.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    Check ($definitions.Count -eq 1) "Missing package function: $name"
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
$VersionPattern = '^(?<prefix>v?)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:\.(?<micro>\d))?$'
$Version = ''
$NoBump = $false
$BumpMicro = $false
Check ((Resolve-PackageVersion 'v1.5.5') -ceq 'v1.5.6') 'Next release must be 1.5.6.'
Check ((Get-NextPatchVersion 'v1.5.9') -ceq 'v1.6.0') 'Patch 9 must carry into minor.'
Check ((Get-NextPatchVersion '1.9.9') -ceq '2.0.0') 'Patch/minor 9 must carry into major.'
Check ((Get-NextPatchVersion '1.5.6.0') -ceq '1.5.7') 'Four-part input must increment the patch component.'
Check ((Get-NextMicroVersion 'v1.5.9.9') -ceq 'v1.6.0.0') 'Explicit micro rollover must use the next patch.'
$NoBump = $true
Check ((Resolve-PackageVersion 'v1.5.5') -ceq 'v1.5.5') 'NoBump must keep the release.'
$Version = 'v1.5.8'
Check ((Resolve-PackageVersion 'v1.5.5') -ceq 'v1.5.8') 'Explicit version must take precedence.'
$Version = ''
$NoBump = $false

foreach ($name in @('Get-PackageFileVersion', 'Invoke-VersionedModulePackage')) {
    $definitions = @($ast.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    Check ($definitions.Count -eq 1) "Missing package function: $name"
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
Check ((Get-PackageFileVersion 'v1.5.6') -ceq '1.5.6.0') 'Module release must map to a four-part DLL version.'
$rejected = $false
try { Get-PackageFileVersion '1.5.65536' | Out-Null } catch { $rejected = $true }
Check $rejected 'DLL version components beyond 65535 must be rejected before build.'

# Real small PE files verify FileVersionInfo; the game build and content/ZIP validation are fixture boundaries.
$templates = Join-Path $RunRoot 'templates'
New-Item -ItemType Directory -Path $templates | Out-Null
$csc = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Check (Test-Path -LiteralPath $csc) 'Framework compiler must be available for real Win32 file-version resources.'
foreach ($number in @('1.5.5.0', '1.5.6.0', '1.5.7.0', '1.5.8.0', '1.6.0.0', '2.0.0.0')) {
    $typeName = 'PackageVersionFixture_' + $number.Replace('.', '_')
    $sourcePath = Join-Path $templates "$number.cs"
    [System.IO.File]::WriteAllText($sourcePath, "[assembly: System.Reflection.AssemblyVersion(`"0.0.0.0`")] [assembly: System.Reflection.AssemblyFileVersion(`"$number`")] public class $typeName {}")
    & $csc /nologo /target:library "/out:$(Join-Path $templates "$number.dll")" $sourcePath
    if ($LASTEXITCODE -ne 0) { throw "Fixture compilation failed: $number" }
}
$Build = $true
$Configuration = 'Debug'
$WorkshopContentDir = ''
$BannerlordRoot = 'fixture-game'
$script:FailZip = $false
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function New-Fixture([string]$Name) {
    $root = Join-Path $RunRoot $Name
    New-Item -ItemType Directory -Path (Join-Path $root 'Properties'), (Join-Path $root 'AnimusForge'), (Join-Path $root 'scripts\build'), (Join-Path $root 'templates') | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $root 'Properties\AssemblyInfo.cs'), "[assembly: AssemblyVersion(`"0.0.0.0`")]`r`n[assembly: AssemblyFileVersion(`"1.5.6.0`")]`r`n")
    [System.IO.File]::WriteAllText((Join-Path $root 'AnimusForge\SubModule.xml'), '<Module><Version value="v1.5.5" /></Module>')
    foreach ($file in Get-ChildItem -LiteralPath $templates -File -Filter '*.dll') {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $root 'templates')
    }
    $builder = @'
param($ProjectRoot, $BannerlordRoot, $Configuration, $WorkshopContentDir, [switch]$Stage)
if (-not $Stage) { throw 'Fixture build must be Stage-only.' }
if (Test-Path -LiteralPath (Join-Path $ProjectRoot 'fail-build')) { throw 'injected build failure' }
$info = [System.IO.File]::ReadAllText((Join-Path $ProjectRoot 'Properties\AssemblyInfo.cs'))
$number = [regex]::Match($info, 'AssemblyFileVersion\("([^"]+)"\)').Groups[1].Value
$stageRoot = Join-Path $ProjectRoot "bin\$Configuration\single_module_stage\AnimusForge"
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'AnimusForge\SubModule.xml') -Destination (Join-Path $stageRoot 'SubModule.xml') -Force
foreach ($api in @('1.3', '1.4')) {
    $dir = Join-Path $stageRoot "bin\Win64_Shipping_Client\versions\$api"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $fixtureNumber = if (Test-Path -LiteralPath (Join-Path $ProjectRoot 'stale-dll')) { '1.5.5.0' } else { $number }
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "templates\$fixtureNumber.dll") -Destination (Join-Path $dir 'AnimusForge.dll') -Force
}
if (Test-Path -LiteralPath (Join-Path $ProjectRoot 'concurrent-edit')) {
    [System.IO.File]::AppendAllText((Join-Path $ProjectRoot 'Properties\AssemblyInfo.cs'), '// concurrent author edit')
}
'@
    [System.IO.File]::WriteAllText((Join-Path $root 'scripts\build\build_single_module.ps1'), $builder)
    return $root
}
function Assert-AnimusForgeCleanStage { param($ProjectRoot, $StageModuleDir, [switch]$RequireCurrentArtifacts) }
function Write-ZipFromModule {
    param($ModulePath, $PackageVersion, [switch]$AutoDetected)
    if ($script:FailZip) { throw 'injected ZIP failure' }
    Check ((Get-SubModuleVersion (Join-Path $ModulePath 'SubModule.xml')) -ceq $PackageVersion) 'Stage XML must already match the ZIP version.'
    $zipPath = Join-Path $RunRoot ('package-' + [guid]::NewGuid().ToString('N') + '.zip')
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($relative in @('SubModule.xml', 'bin\Win64_Shipping_Client\versions\1.3\AnimusForge.dll', 'bin\Win64_Shipping_Client\versions\1.4\AnimusForge.dll')) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $ModulePath $relative), ('AnimusForge/' + $relative.Replace('\', '/'))) | Out-Null
        }
    } finally { $zip.Dispose() }
    return $zipPath
}
function Run-Fixture([string]$Root) {
    return Invoke-VersionedModulePackage -ProjectRoot $Root -StageModuleDir (Join-Path $Root 'bin\Debug\single_module_stage\AnimusForge')
}

$root = New-Fixture 'success'
$zip = Run-Fixture $root
Check (Test-Path -LiteralPath $zip) 'First release must create a ZIP.'
Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq 'v1.5.6') 'Successful first package must persist 1.5.6.'
$zip = Run-Fixture $root
Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq 'v1.5.7') 'The following package must persist 1.5.7.'
foreach ($api in @('1.3', '1.4')) {
    $dll = Join-Path $root "bin\Debug\single_module_stage\AnimusForge\bin\Win64_Shipping_Client\versions\$api\AnimusForge.dll"
    Check ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).FileVersion -ceq '1.5.7.0') "API $api DLL must carry the new version."
    Check ([System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString() -ceq '0.0.0.0') 'Assembly identity must remain unchanged.'
}
$Build = $false
$NoBump = $true
$zip = Run-Fixture $root
Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq 'v1.5.7') 'NoBump must repackage matching existing DLLs.'
$NoBump = $false
$Build = $true

foreach ($case in @(@('v1.5.9', 'v1.6.0'), @('v1.9.9', 'v2.0.0'))) {
    $root = New-Fixture ('carry-' + $case[0])
    [System.IO.File]::WriteAllText((Join-Path $root 'AnimusForge\SubModule.xml'), ('<Module><Version value="' + $case[0] + '" /></Module>'))
    $zip = Run-Fixture $root
    Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq $case[1]) 'Package workflow must persist the carried version.'
    foreach ($api in @('1.3', '1.4')) {
        $dll = Join-Path $root "bin\Debug\single_module_stage\AnimusForge\bin\Win64_Shipping_Client\versions\$api\AnimusForge.dll"
        Check ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).FileVersion -ceq (Get-PackageFileVersion $case[1])) 'Both carried DLL versions must match.'
    }
}

$root = New-Fixture 'retry'
$zip = Run-Fixture $root
$stageXml = Join-Path $root 'bin\Debug\single_module_stage\AnimusForge\SubModule.xml'
$stageBefore = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($stageXml))
$script:FailZip = $true
$rejected = $false
try { Run-Fixture $root | Out-Null } catch { $rejected = $true }
Check $rejected 'A failed second package must be rejected.'
Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq 'v1.5.6') 'A failed package must not consume the next release number.'
Check ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($stageXml)) -ceq $stageBefore) 'Existing Stage XML must be restored byte-for-byte.'
$script:FailZip = $false
$zip = Run-Fixture $root
Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq 'v1.5.7') 'Retry after failure must still create 1.5.7.'

foreach ($mode in @('build', 'zip', 'stale')) {
    $root = New-Fixture ("failure-$mode")
    $xml = Join-Path $root 'AnimusForge\SubModule.xml'
    $info = Join-Path $root 'Properties\AssemblyInfo.cs'
    $xmlBefore = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($xml))
    $infoBefore = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($info))
    if ($mode -eq 'build') { [System.IO.File]::WriteAllText((Join-Path $root 'fail-build'), 'fixture') }
    if ($mode -eq 'stale') { [System.IO.File]::WriteAllText((Join-Path $root 'stale-dll'), 'fixture') }
    $script:FailZip = $mode -eq 'zip'
    $rejected = $false
    try { Run-Fixture $root | Out-Null } catch { $rejected = $true }
    Check $rejected "$mode failure must reject packaging."
    Check ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($xml)) -ceq $xmlBefore) "$mode failure must restore source XML byte-for-byte."
    Check ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($info)) -ceq $infoBefore) "$mode failure must restore assembly attributes byte-for-byte."
}
$script:FailZip = $false
$root = New-Fixture 'concurrent-edit'
[System.IO.File]::WriteAllText((Join-Path $root 'concurrent-edit'), 'fixture')
$rejected = $false
try { Run-Fixture $root | Out-Null } catch { $rejected = $true }
Check $rejected 'Concurrent version source edits must stop packaging.'
Check ([System.IO.File]::ReadAllText((Join-Path $root 'Properties\AssemblyInfo.cs')).Contains('// concurrent author edit')) 'Rollback must not overwrite concurrent author edits.'
Check ((Get-SubModuleVersion (Join-Path $root 'AnimusForge\SubModule.xml')) -ceq 'v1.5.5') 'Unchanged release source must still roll back.'
foreach ($entry in @('一键打包AnimusForge.bat', '一键打包AnimusForge小版本更新.bat')) {
    $text = [System.IO.File]::ReadAllText((Join-Path $ProjectRoot "一键编译覆盖推送\$entry"))
    Check (-not $text.Contains('-BumpMicro')) "$entry must default to patch +1."
}
$baseEntry = [System.IO.File]::ReadAllText((Join-Path $ProjectRoot '一键编译覆盖推送\一键打包AnimusForge.bat'))
Check ([regex]::Matches($baseEntry, ' -Build ').Count -eq 2) 'Both workshop/non-workshop paths must stamp before build.'
Check (-not $baseEntry.Contains('-File "%BUILD_SCRIPT%"')) 'Entry must not build before selecting the release version.'
Check ($baseEntry.Contains('-ExcludeOnnx %*')) 'Entry must keep package options and the ONNX exclusion.'
Write-Output "PASS package version contract: checks=$script:Checks; successive releases=1.5.6,1.5.7; both DLL file versions verified; build/ZIP/stale-DLL failures rolled back."
