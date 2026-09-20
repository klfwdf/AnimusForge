param(
    [Parameter(Mandatory = $true)][string]$BannerlordRoot,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$moduleRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repoRoot = [IO.Path]::GetFullPath((Join-Path $moduleRoot '..\..'))
$gameRoot = (Resolve-Path -LiteralPath $BannerlordRoot).Path
[xml]$nativeManifest = Get-Content -LiteralPath (Join-Path $gameRoot 'Modules\Native\SubModule.xml') -Raw
$gameVersion = [string]$nativeManifest.Module.Version.value
if ($gameVersion -notmatch '^v?1\.4\.') { throw "Dual validation requires a verified installed 1.4 line; found $gameVersion" }
$project = Join-Path $moduleRoot 'src\AnimusForge.DialogueUI.csproj'
$beforeAf = @{}
foreach ($api in @('1.3','1.4')) {
    $hostDll = Join-Path $gameRoot "Modules\AnimusForge\bin\Win64_Shipping_Client\versions\$api\AnimusForge.dll"
    if (!(Test-Path -LiteralPath $hostDll -PathType Leaf)) { throw "Missing matching AF host: $hostDll" }
    $beforeAf[$api] = (Get-FileHash -LiteralPath $hostDll -Algorithm SHA256).Hash
}
foreach ($api in @('1.3','1.4')) {
    $obj = ((Join-Path $moduleRoot "artifacts\obj\$api") -replace '\\','/') + '/'
    & dotnet build $project -c $Configuration "/p:BannerlordApi=$api" "/p:BannerlordRoot=$gameRoot" "/p:BaseIntermediateOutputPath=$obj" "/p:MSBuildProjectExtensionsPath=$obj"
    if ($LASTEXITCODE -ne 0) { throw "DialogueUI API $api build failed" }
    $dll = Join-Path $moduleRoot "artifacts\$api\AnimusForge.DialogueUI.dll"
    if ([Reflection.AssemblyName]::GetAssemblyName($dll).Name -ne 'AnimusForge.DialogueUI') { throw 'Unexpected output assembly identity.' }
    $stage = Join-Path $moduleRoot "artifacts\stage\$api\AnimusForge_DialogueUI"
    $stageBin = Join-Path $stage 'bin\Win64_Shipping_Client'
    New-Item -ItemType Directory -Path $stageBin -Force | Out-Null
    Copy-Item -LiteralPath $dll -Destination $stageBin -Force
    Copy-Item -LiteralPath (Join-Path $moduleRoot 'SubModule.xml') -Destination $stage -Force
    foreach ($resourceDir in @('Prefabs','SpriteParts')) {
        $sourceDir = Join-Path $moduleRoot "GUI\$resourceDir"
        $destination = Join-Path $stage "GUI\$resourceDir"
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        foreach ($file in Get-ChildItem -LiteralPath $sourceDir -File) {
            Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        }
    }
    $evidence = @{
        Module = 'AnimusForge_DialogueUI'; Api = $api; Configuration = $Configuration
        Reference = $(if ($api -eq '1.3') { 'Bannerlord.ReferenceAssemblies 1.3.15.110062' } else { $gameVersion })
        AfReferenceSha256 = $beforeAf[$api]; DllSha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
        SourceRevision = (& git -C $repoRoot rev-parse HEAD); CreatedUtc = [DateTime]::UtcNow.ToString('o')
        InGameVerified = $false
    }
    $evidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'build.json') -Encoding UTF8
    $evidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $moduleRoot "artifacts\$api\build.json") -Encoding UTF8
    $manifest = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object {
        @{ Path = $_.FullName.Substring($stage.Length + 1).Replace('\','/'); Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $moduleRoot "artifacts\$api\files.json") -Encoding UTF8
}
foreach ($api in @('1.3','1.4')) {
    $hostDll = Join-Path $gameRoot "Modules\AnimusForge\bin\Win64_Shipping_Client\versions\$api\AnimusForge.dll"
    if ((Get-FileHash -LiteralPath $hostDll -Algorithm SHA256).Hash -ne $beforeAf[$api]) { throw "AF host changed during verification: $api" }
}
Write-Output 'Both independent DialogueUI variants built and staged locally. AF host hashes unchanged. No game files written.'
