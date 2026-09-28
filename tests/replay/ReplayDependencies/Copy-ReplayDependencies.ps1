#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$ReferencePath,
    [Parameter(Mandatory = $true)][string]$HarmonyModulePath,
    [Parameter(Mandatory = $true)][string]$McmModulePath,
    [Parameter(Mandatory = $true)][string]$UiExtenderModulePath,
    [Parameter(Mandatory = $true)][string]$PrivateRuntimePath,
    [Parameter(Mandatory = $true)][string]$ImplementationPath,
    [Parameter(Mandatory = $true)][string]$ProjectDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-Directory([string]$Path, [string]$Label) {
    if (-not [IO.Path]::IsPathRooted($Path) -or -not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "REPLAY_INPUT_MISSING: $Label must be an existing absolute directory: $Path"
    }
    return [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Get-ModuleBin([string]$Path, [string]$ExpectedId) {
    $root = Get-Directory $Path $ExpectedId
    $manifest = Join-Path $root 'SubModule.xml'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
        throw "REPLAY_MODULE_MANIFEST_MISSING: $manifest"
    }
    [xml]$xml = Get-Content -LiteralPath $manifest -Raw
    if ([string]$xml.Module.Id.value -ne $ExpectedId) {
        throw "REPLAY_MODULE_ID_MISMATCH: Expected $ExpectedId at $manifest"
    }
    return Get-Directory (Join-Path $root 'bin\Win64_Shipping_Client') $ExpectedId
}

function Get-Hash([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $sha.Dispose(); $stream.Dispose() }
}

try {
    if ($PSVersionTable.PSEdition -ne 'Desktop') {
        throw 'REPLAY_HOST_UNSUPPORTED: Use Windows PowerShell 5.1 for metadata-only ReflectionOnlyLoad; no production code is executed by this helper.'
    }
    $game = Get-Directory $GameRoot 'GameRoot'
    $null = Get-Directory (Join-Path $game 'bin\Win64_Shipping_Client') 'GameRoot/bin'
    $reference = Get-Directory $ReferencePath 'Bannerlord14ReferencePath'
    $private = Get-Directory $PrivateRuntimePath 'ReplayPrivateRuntimePath'
    $harmony = Get-ModuleBin $HarmonyModulePath 'Bannerlord.Harmony'
    $mcm = Get-ModuleBin $McmModulePath 'Bannerlord.MBOptionScreen'
    $ui = Get-ModuleBin $UiExtenderModulePath 'Bannerlord.UIExtenderEx'
    $project = Get-Directory $ProjectDirectory 'ProjectDirectory'
    $output = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\', '/')
    $bin = (Join-Path $project 'bin') + '\'
    if (-not $output.StartsWith($bin, [StringComparison]::OrdinalIgnoreCase)) {
        throw "REPLAY_OUTPUT_BOUNDARY: Output must stay in this runner's bin directory: $output"
    }
    foreach ($root in @($game, $reference, $private, $harmony, $mcm, $ui)) {
        if ($output -eq $root -or $output.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase) -or $root.StartsWith($output + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw "REPLAY_OUTPUT_BOUNDARY: Output overlaps an input directory: $root"
        }
    }
    for ($parent = $output; $parent.Length -ge $project.Length; $parent = [IO.Path]::GetDirectoryName($parent)) {
        if (Test-Path -LiteralPath $parent) {
            if ((Get-Item -LiteralPath $parent -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "REPLAY_OUTPUT_BOUNDARY: Reparse-point output paths are not supported: $parent"
            }
        }
    }
    if (-not [IO.Path]::IsPathRooted($ImplementationPath) -or -not (Test-Path -LiteralPath $ImplementationPath -PathType Leaf)) {
        throw "REPLAY_IMPLEMENTATION_MISSING: Build the project-local 1.4 Stage separately: $ImplementationPath"
    }

    # Each filename has one source authority. Never scan Modules/** or Workshop/**.
    $sources = New-Object 'System.Collections.Generic.List[object]'
    foreach ($pattern in @('TaleWorlds.*.dll', 'SandBox*.dll')) {
        foreach ($file in Get-ChildItem -LiteralPath $reference -Filter $pattern -File) {
            $sources.Add([pscustomobject]@{ File = $file.FullName; Owner = 'PinnedBannerlord14' })
        }
    }
    foreach ($entry in @(
        @{ Root = $reference; Name = 'TaleWorlds.CampaignSystem.dll'; Owner = 'PinnedBannerlord14' },
        @{ Root = $reference; Name = 'Newtonsoft.Json.dll'; Owner = 'PinnedBannerlord14' },
        @{ Root = $reference; Name = 'StbSharp.dll'; Owner = 'PinnedBannerlord14' },
        @{ Root = $reference; Name = 'Steamworks.NET.dll'; Owner = 'PinnedBannerlord14' },
        @{ Root = $reference; Name = 'GalaxyCSharp.dll'; Owner = 'PinnedBannerlord14' },
        @{ Root = $harmony; Name = '0Harmony.dll'; Owner = 'HarmonyModule' },
        @{ Root = $mcm; Name = 'MCMv5.dll'; Owner = 'McmModule' },
        @{ Root = $ui; Name = 'Bannerlord.UIExtenderEx.dll'; Owner = 'UiExtenderModule' },
        @{ Root = $private; Name = 'Microsoft.ML.OnnxRuntime.dll'; Owner = 'PrivateRuntime' }
    )) {
        $sources.Add([pscustomobject]@{ File = Join-Path $entry.Root $entry.Name; Owner = $entry.Owner })
    }
    foreach ($pattern in @('MonoMod.*.dll', 'Mono.Cecil*.dll')) {
        foreach ($file in Get-ChildItem -LiteralPath $harmony -Filter $pattern -File) {
            $sources.Add([pscustomobject]@{ File = $file.FullName; Owner = 'HarmonyModule' })
        }
    }

    $selected = @{}
    foreach ($source in $sources) {
        $path = [IO.Path]::GetFullPath($source.File)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "REPLAY_DEPENDENCY_MISSING: $($source.Owner): $path"
        }
        $name = [IO.Path]::GetFileName($path)
        if ($selected.ContainsKey($name)) {
            if ($selected[$name].Source -ne $path) {
                throw "REPLAY_SOURCE_CONFLICT: $name has multiple sources: $($selected[$name].Source), $path"
            }
            continue
        }
        $identity = [Reflection.AssemblyName]::GetAssemblyName($path)
        if ($identity.Name + '.dll' -ne $name) {
            throw "REPLAY_ASSEMBLY_NAME_MISMATCH: $path contains $($identity.Name)"
        }
        $selected[$name] = [pscustomobject]@{
            Name = $name; Owner = $source.Owner; Source = $path
            Assembly = $identity.FullName; Version = $identity.Version.ToString(); Sha256 = Get-Hash $path
        }
    }

    # Validate direct non-framework references of the actual staged implementation.
    # Framework assemblies remain owned by net8/NuGet; no System.* copies from mods.
    $implementation = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($ImplementationPath))
    foreach ($required in $implementation.GetReferencedAssemblies()) {
        if ($required.Name -match '^(mscorlib|netstandard|System($|\.)|Microsoft\.CSharp$)') { continue }
        $name = $required.Name + '.dll'
        if (-not $selected.ContainsKey($name)) {
            throw "REPLAY_DEPENDENCY_UNOWNED: Stage requires $($required.FullName); add an explicit source owner instead of a module scan."
        }
        if ($selected[$name].Assembly -ne $required.FullName) {
            throw "REPLAY_ASSEMBLY_IDENTITY_MISMATCH: Stage requires $($required.FullName); selected $($selected[$name].Assembly) from $($selected[$name].Source)"
        }
    }

    # Validate the entire plan before writing any dependency. Never overwrite a
    # differing DLL left by an earlier machine/module selection or an SDK package.
    if (Test-Path -LiteralPath $output) {
        foreach ($existing in Get-ChildItem -LiteralPath $output -Filter '*.dll' -File -Recurse) {
            if ($selected.ContainsKey($existing.Name) -and $existing.DirectoryName -ne $output) {
                throw "REPLAY_OUTPUT_DUPLICATE: The runner resolves recursively; remove this ambiguity in a reviewed clean output: $($existing.FullName)"
            }
        }
    }
    foreach ($dependency in $selected.Values) {
        $destination = Join-Path $output $dependency.Name
        if ((Test-Path -LiteralPath $destination) -and (Get-Hash $destination) -ne $dependency.Sha256) {
            throw "REPLAY_OUTPUT_CONFLICT: $destination differs from $($dependency.Source). Use a reviewed clean runner output; nothing was overwritten."
        }
    }
    $null = New-Item -ItemType Directory -Path $output -Force
    foreach ($dependency in $selected.Values) {
        $destination = Join-Path $output $dependency.Name
        if (-not (Test-Path -LiteralPath $destination)) {
            Copy-Item -LiteralPath $dependency.Source -Destination $destination
        }
    }
    $manifest = [ordered]@{
        SchemaVersion = 1
        ApiLine = '1.4'
        GameRoot = $game
        ReferencePath = $reference
        ImplementationPath = [IO.Path]::GetFullPath($ImplementationPath)
        ImplementationSha256 = Get-Hash $ImplementationPath
        Dependencies = @($selected.Values | Sort-Object Name)
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'af-replay-dependencies.json') -Encoding UTF8
    Write-Output "REPLAY_DEPENDENCIES_PASS: count=$($selected.Count) api=1.4 output=$output"
}
catch {
    [Console]::Error.WriteLine('error REPLAY001: ' + $_.Exception.Message)
    exit 1
}
