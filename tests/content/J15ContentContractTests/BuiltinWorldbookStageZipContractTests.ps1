[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot,
    [Parameter(Mandatory = $true)][string]$ArtifactRoot,
    [Parameter(Mandatory = $true)][string]$RuntimeDependencyDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts\build\content_layout.ps1')

function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$root = Get-AnimusForgeContentFullPath -Path $RunRoot
Assert-AnimusForgePathUnderRoot -Path $root -Root (Join-Path $ProjectRoot 'artifacts\tests') -Label 'Worldbook Stage/ZIP fixture'
Assert-AnimusForgeNoReparsePoint -Path $root -Label 'Worldbook Stage/ZIP fixture'
Check (-not (Test-Path -LiteralPath $root)) 'Fixture must be new; existing evidence is never cleared'
foreach ($inputRoot in @($ArtifactRoot, $RuntimeDependencyDir)) {
    Assert-AnimusForgePathUnderRoot -Path $inputRoot -Root $ProjectRoot -Label 'Workspace build input'
    Assert-AnimusForgeNoReparsePoint -Path $inputRoot -Label 'Workspace build input'
}

$project = Join-Path $root 'project'
$stage = Join-Path $project 'bin\Debug\single_module_stage\AnimusForge'
$layout = @(Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot)
New-Item -ItemType Directory -Path (Join-Path $project 'content') -Force | Out-Null
foreach ($name in @('content-map.json', 'runtime-dependencies.lock.json')) {
    Copy-Item -LiteralPath (Join-Path $ProjectRoot "content\$name") -Destination (Join-Path $project "content\$name")
}
foreach ($entry in $layout) {
    $source = Join-Path $project $entry.Source
    New-Item -ItemType Directory -Path (Split-Path -Parent $source) -Force | Out-Null
    Copy-Item -LiteralPath $entry.SourcePath -Destination $source
}

# Extra source, installed and user files must not become package inputs.
foreach ($relative in @(
    'content\modules\AF.Module.Onboarding\PlayerExports\private.json',
    'installed\Modules\AnimusForge\PlayerExports\private.json',
    'appdata\AnimusForge\UserData\PlayerExports\private.json'
)) {
    $path = Join-Path $project $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [System.IO.File]::WriteAllText($path, 'synthetic-personal-data')
}
Invoke-AnimusForgeContentProjection -ProjectRoot $project -DestinationModuleDir $stage | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'AnimusForge\SubModule.xml') -Destination (Join-Path $stage 'SubModule.xml')

$artifacts = [ordered]@{
    'AnimusForge.Bootstrap.dll' = 'bootstrap\AnimusForge.Bootstrap.dll'
    'AnimusForge.Bootstrap.pdb' = 'bootstrap\AnimusForge.Bootstrap.pdb'
    'AnimusForge.Bootstrap.build.json' = 'bootstrap\AnimusForge.Bootstrap.build.json'
    'versions\1.3\AnimusForge.dll' = 'versions\1.3\AnimusForge.dll'
    'versions\1.3\AnimusForge.pdb' = 'versions\1.3\AnimusForge.pdb'
    'versions\1.3\AnimusForge.build.json' = 'versions\1.3\AnimusForge.build.json'
    'versions\1.4\AnimusForge.dll' = 'versions\1.4\AnimusForge.dll'
    'versions\1.4\AnimusForge.pdb' = 'versions\1.4\AnimusForge.pdb'
    'versions\1.4\AnimusForge.build.json' = 'versions\1.4\AnimusForge.build.json'
}
$bin = Join-Path $stage 'bin\Win64_Shipping_Client'
foreach ($relative in $artifacts.Keys) {
    $source = Join-Path $ArtifactRoot $artifacts[$relative]
    $target = Join-Path $bin $relative
    $currentArtifact = Join-Path $project "bin\Debug\single_module_artifacts\$($artifacts[$relative])"
    New-Item -ItemType Directory -Path (Split-Path -Parent $target), (Split-Path -Parent $currentArtifact) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
    Copy-Item -LiteralPath $source -Destination $currentArtifact
}
$lock = Get-Content -LiteralPath (Join-Path $project 'content\runtime-dependencies.lock.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($name in $lock.files.PSObject.Properties.Name) {
    Copy-Item -LiteralPath (Join-Path $RuntimeDependencyDir $name) -Destination (Join-Path $bin $name)
}
Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage -RequireCurrentArtifacts
$books = @($layout | Where-Object { $_.Target.StartsWith('PlayerExports/') })
Check ($books.Count -eq 3139) 'The complete reviewed library must be projected'
Check (@($books | ForEach-Object { $_.Target.Split('/')[1] } | Sort-Object -Unique).Count -eq 4) 'Four worldbooks must ship'

$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $ProjectRoot 'scripts\build\package_mod.ps1'), [ref]$null, [ref]$errors)
Check ($errors.Count -eq 0) 'Original package script has parse errors'
foreach ($name in @('Get-FullPathSafe', 'Get-FileSha256', 'Get-VersionedSubModuleBytes',
    'Get-RequiredZipEntry', 'Read-ZipEntryText', 'Get-ZipEntrySha256',
    'Assert-ZipBuildMarker', 'Assert-ZipLayout', 'Assert-ZipMatchesCleanStage')) {
    $definitions = @($ast.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    Check ($definitions.Count -eq 1) "Missing package function: $name"
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
$RequiredBuildMarkerSchemaVersion = 2
$ModuleName = 'AnimusForge'
$BootstrapAssemblyName = 'AnimusForge.Bootstrap'
$BootstrapClassType = 'AnimusForge.Bootstrap.BootstrapSubModule'
$Flavor13 = 'ANIMUSFORGE_BANNERLORD_API_1_3'
$Flavor14 = 'ANIMUSFORGE_BANNERLORD_API_1_4'
$AllowedPackageRootDllNames = @('AnimusForge.Bootstrap.dll') + @($lock.files.PSObject.Properties.Name)
$version = ([xml](Get-Content -LiteralPath (Join-Path $stage 'SubModule.xml') -Raw)).Module.Version.value
$zipPath = Join-Path $root 'worldbooks-contract-only.zip'
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File -Force | Sort-Object FullName) {
        $relative = $file.FullName.Substring($stage.Length).TrimStart('\', '/') -replace '\\', '/'
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName,
            "AnimusForge/$relative", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally { $zip.Dispose() }
Assert-ZipLayout -ZipPath $zipPath -ExpectedVersion $version -OnnxMustBeAbsent:$true -CustomPromptsMustBeAbsent:$false
Assert-ZipMatchesCleanStage -ZipPath $zipPath -StageModuleDir $stage -PackageVersion $version

$private = Join-Path $stage 'PlayerExports\private.json'
[System.IO.File]::WriteAllText($private, 'synthetic-personal-data')
$rejected = $false
try { Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage }
catch { $rejected = $_.Exception.Message.Contains('unknown or duplicate file: PlayerExports/private.json') }
Check $rejected 'Stage must reject an unlisted personal export even alongside built-in worldbooks'
Remove-Item -LiteralPath $private
Assert-AnimusForgeCleanStage -ProjectRoot $project -StageModuleDir $stage -RequireCurrentArtifacts
Write-Output "PASS builtin Stage/ZIP contract: books=4 files=3139 content=$($layout.Count) stageFiles=$($layout.Count + 16) personal=REJECTED logs/models=ABSENT"
