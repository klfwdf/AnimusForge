param(
    [Parameter(Mandatory = $true)][string]$BannerlordRoot,
    [Parameter(Mandatory = $true)][string]$ReferenceDir,
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$PrerequisiteReferenceDir = ''
)

# Explicit local 1.5 candidate only; existing one-click/package/deploy flows stay intact.
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'coup_seam_gate.ps1')
$references = (Resolve-Path -LiteralPath $ReferenceDir).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (-not $output.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Candidate output must remain in the project workspace.'
}
if (Test-Path -LiteralPath $output) { throw 'Choose a new output directory; existing candidates are preserved.' }
$version = Get-CoupGateReferenceVersion $references
if ($version -notmatch '^v1\.5\.') { throw "Expected verified 1.5 references; actual: $version" }
foreach ($name in @('TaleWorlds.Library', 'TaleWorlds.CampaignSystem', 'TaleWorlds.MountAndBlade', 'SandBox')) {
    if (-not (Test-Path -LiteralPath (Join-Path $references ($name + '.dll')) -PathType Leaf)) {
        throw "Incomplete reference snapshot: $name"
    }
}
New-Item -ItemType Directory -Path $output | Out-Null
$bin = Join-Path $output 'versions/1.5'
$obj = (Join-Path $output 'obj').Replace('\', '/') + '/'
$referenceHashes = @(Get-ChildItem -LiteralPath $references -Filter '*.dll' -File | ForEach-Object {
    [ordered]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$prerequisiteArguments = @()
if ($PrerequisiteReferenceDir) {
    $prerequisiteRoot = (Resolve-Path -LiteralPath $PrerequisiteReferenceDir).Path
    $prerequisites = [ordered]@{
        HarmonyCorePath = '0Harmony.dll'; HarmonyPath = 'Bannerlord.Harmony.dll'
        UIExtenderExPath = 'Bannerlord.UIExtenderEx.dll'; Mcmv5Path = 'MCMv5.dll'
        MBOptionScreenPath = 'Bannerlord.MBOptionScreen.v1.5.1.dll'
    }
    foreach ($property in $prerequisites.Keys) {
        $dependency = Join-Path $prerequisiteRoot $prerequisites[$property]
        if (-not (Test-Path -LiteralPath $dependency -PathType Leaf)) { throw "Missing prerequisite snapshot: $dependency" }
        $prerequisiteArguments += "/p:${property}=$dependency"
    }
}
& dotnet build (Join-Path $project 'AnimusForge.csproj') -c $Configuration @(
    '/p:BannerlordApi=1.5', "/p:VersionedDepsDir=$references", "/p:BannerlordBinDir=$references",
    "/p:NativeBinDir=$references", "/p:SandBoxBinDir=$references", "/p:BannerlordRoot=$BannerlordRoot",
    "/p:OutputPath=$($bin.Replace('\', '/'))/", "/p:BaseIntermediateOutputPath=$obj",
    "/p:IntermediateOutputPath=$obj", "/p:MSBuildProjectExtensionsPath=$obj",
    '/p:AppendTargetFrameworkToOutputPath=false', '/p:CopyLegacyAnimusForgeModuleData=false'
) @prerequisiteArguments
if ($LASTEXITCODE -ne 0) { throw 'Bannerlord 1.5 candidate build failed.' }
$dll = Join-Path $bin 'AnimusForge.dll'
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Name -ne 'AnimusForge') { throw 'Incorrect assembly identity.' }
$bytes = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($dll))
if (-not $bytes.Contains('ANIMUSFORGE_BANNERLORD_API_1_5') -or
    $bytes.Contains('ANIMUSFORGE_BANNERLORD_API_1_4') -or $bytes.Contains('ANIMUSFORGE_BANNERLORD_API_1_3')) {
    throw 'Incorrect implementation build flavor.'
}
$marker = [ordered]@{
    SchemaVersion = 2; Role = 'Implementation'; FileName = 'AnimusForge.dll'; AssemblyName = 'AnimusForge'
    BannerlordApi = '1.5'; BuildFlavor = 'ANIMUSFORGE_BANNERLORD_API_1_5'
    ReferenceGameVersion = $version; Sha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    CreatedUtc = [DateTime]::UtcNow.ToString('o')
}
$marker | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $bin 'AnimusForge.build.json') -Encoding UTF8
# Keep raw game/framework dependency DLLs out of the candidate implementation directory.
# Resolve every exact file and validate its parent before removing build copies.
foreach ($file in (Get-ChildItem -LiteralPath $bin -File)) {
    if ($file.Name -in @('AnimusForge.dll', 'AnimusForge.pdb', 'AnimusForge.build.json')) { continue }
    if ($file.Directory.FullName -ne [IO.Path]::GetFullPath($bin)) { throw 'Unexpected build-copy path.' }
    Remove-Item -LiteralPath $file.FullName
}

& dotnet build (Join-Path $project 'tools/Coup.RuntimeProbe/Coup.RuntimeProbe.csproj') -c $Configuration "/p:GameRoot=$BannerlordRoot"
if ($LASTEXITCODE -ne 0) { throw 'Coup seam probe build failed.' }
$probe = Join-Path $project "tools/Coup.RuntimeProbe/bin/$Configuration/net472/Coup.RuntimeProbe.exe"
Invoke-CoupSeamGate -ProjectRoot $project -GameRoot $BannerlordRoot -DllPath $dll -ReferenceDir $references -ExpectedApi '1.5' -ProbePath $probe
foreach ($reference in $referenceHashes) {
    if ((Get-FileHash -LiteralPath (Join-Path $references $reference.Name) -Algorithm SHA256).Hash -ne $reference.Sha256) {
        throw "Reference changed during build: $($reference.Name)"
    }
}
$referenceHashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'reference-hashes.json') -Encoding UTF8
Write-Host "Local Bannerlord $version candidate: $dll (Coup seam passed; no game deployment)."
