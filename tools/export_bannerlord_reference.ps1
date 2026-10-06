param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$IlSpyPath,
    [string]$ExpectedApi = "1.5",
    [string]$ExpectedVersion = "",
    [string]$EvidenceRoot = "",
    [switch]$AllowKnownEpicDiagnostics
)

# Manual reference export only. This is not a build/deploy entry point.
$ErrorActionPreference = "Stop"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$sourceRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$toolPath = (Resolve-Path -LiteralPath $IlSpyPath).Path
if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path $repoRoot "artifacts\bannerlord15-reference\export"
}
$evidencePath = [IO.Path]::GetFullPath($EvidenceRoot)

function Assert-InRepository([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    if (-not $resolved.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Output must remain inside this repository: $resolved"
    }
}

function Quote-ProcessArgument([string]$Value) {
    if ($Value.Contains('"') -or $Value.EndsWith('\')) {
        throw "Unsupported process argument: $Value"
    }
    return '"' + $Value + '"'
}

function Invoke-Decompiler([string[]]$Arguments, [string]$LogName) {
    $stdout = Join-Path $evidencePath ($LogName + ".stdout.log")
    $stderr = Join-Path $evidencePath ($LogName + ".stderr.log")
    $quoted = @($Arguments | ForEach-Object { Quote-ProcessArgument $_ })
    $process = Start-Process -FilePath $toolPath -ArgumentList $quoted -WindowStyle Hidden -Wait -PassThru `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    return [pscustomobject]@{ ExitCode = $process.ExitCode; Stdout = $stdout; Stderr = $stderr }
}

Assert-InRepository $evidencePath
New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
$libraryPath = Join-Path $sourceRoot "bin\Win64_Shipping_Client\TaleWorlds.Library.dll"
if (-not (Test-Path -LiteralPath $libraryPath -PathType Leaf)) {
    throw "Client TaleWorlds.Library.dll is missing: $libraryPath"
}
$versionResult = Invoke-Decompiler -Arguments @("--disable-updatecheck", "-t", "BuildInfo", $libraryPath) -LogName "BuildInfo"
if ($versionResult.ExitCode -ne 0) { throw "BuildInfo decompilation failed; see $($versionResult.Stderr)" }
$buildInfo = Get-Content -LiteralPath $versionResult.Stdout -Raw
$versionMatch = [regex]::Match($buildInfo, 'GameVersion\s*=\s*"(?<version>[ve](?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.(?<change>\d+))"')
if (-not $versionMatch.Success) { throw "No authoritative BuildInfo.GameVersion was found." }
$actualVersion = $versionMatch.Groups["version"].Value
$actualApi = $versionMatch.Groups["major"].Value + "." + $versionMatch.Groups["minor"].Value
if ($actualApi -ne $ExpectedApi) { throw "Expected API $ExpectedApi, actual BuildInfo.GameVersion is $actualVersion. Export rejected." }
if ($ExpectedVersion -and $actualVersion -ne $ExpectedVersion) { throw "Expected $ExpectedVersion, actual $actualVersion. Export rejected." }
$releaseVersion = $actualApi + "." + $versionMatch.Groups["patch"].Value
$destination = Join-Path $repoRoot ("原版游戏本体代码" + $releaseVersion)
$staging = Join-Path $evidencePath ("staging-" + $actualVersion)
$dependencies = Join-Path $evidencePath "dependencies"
Assert-InRepository $destination
Assert-InRepository $staging
if (Test-Path -LiteralPath $destination) { throw "Reference directory already exists; no overwrite is allowed: $destination" }
if (Test-Path -LiteralPath $staging) { throw "Staging already exists; preserve it and choose a fresh EvidenceRoot: $staging" }
New-Item -ItemType Directory -Path $staging,$dependencies | Out-Null

$toolVersionResult = Invoke-Decompiler -Arguments @("--version") -LogName "ilspy-version"
if ($toolVersionResult.ExitCode -ne 0) { throw "Cannot determine decompiler version." }
$toolVersion = (Get-Content -LiteralPath $toolVersionResult.Stdout -Raw).Trim()
$clientDirectories = [Collections.Generic.List[string]]::new()
$clientDirectories.Add((Join-Path $sourceRoot "bin\Win64_Shipping_Client"))
$modulesRoot = Join-Path $sourceRoot "Modules"
if (Test-Path -LiteralPath $modulesRoot) {
    foreach ($module in (Get-ChildItem -LiteralPath $modulesRoot -Directory | Sort-Object Name)) {
        $candidate = Join-Path $module.FullName "bin\Win64_Shipping_Client"
        if (Test-Path -LiteralPath $candidate -PathType Container) { $clientDirectories.Add($candidate) }
    }
}
$officialModuleNames = @("Native", "SandBoxCore", "SandBox", "StoryMode", "CustomBattle", "NavalDLC", "BirthAndDeath", "Multiplayer")
$managed = [Collections.Generic.List[object]]::new()
$skipped = [Collections.Generic.List[string]]::new()
$duplicates = [Collections.Generic.List[object]]::new()
$seenNames = @{}
foreach ($directory in $clientDirectories) {
    if ($directory -ne $clientDirectories[0]) {
        $moduleName = Split-Path (Split-Path (Split-Path $directory -Parent) -Parent) -Leaf
        if ($moduleName -notin $officialModuleNames) { continue }
    }
    foreach ($file in (Get-ChildItem -LiteralPath $directory -Filter "*.dll" -File | Sort-Object Name)) {
        try { $assemblyName = [Reflection.AssemblyName]::GetAssemblyName($file.FullName) }
        catch {
            $skipped.Add("SKIP native/non-managed: " + $file.FullName)
            continue
        }
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        if ($seenNames.ContainsKey($assemblyName.Name)) {
            if ($seenNames[$assemblyName.Name].SHA256 -ne $hash) {
                # Official CustomBattle and Multiplayer carry different PE builds of this DLL.
                # Keep the existing baseline's CustomBattle source; require identical exports below.
                $canonical = $seenNames[$assemblyName.Name]
                if ($assemblyName.Name -ne 'TaleWorlds.MountAndBlade.Multiplayer' -or
                    $canonical.RelativeSourcePath -ne 'Modules\CustomBattle\bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.Multiplayer.dll' -or
                    $moduleName -ne 'Multiplayer') {
                    throw "Different DLLs share assembly name '$($assemblyName.Name)'; resolve provenance first."
                }
                $duplicates.Add([pscustomobject]@{
                    Assembly = $assemblyName.Name; SourcePath = $file.FullName
                    RelativeSourcePath = $file.FullName.Substring($sourceRoot.Length).TrimStart('\','/')
                    Length = $file.Length; SHA256 = $hash; CanonicalSHA256 = $canonical.SHA256
                    ExportComparison = 'PENDING'; ComparedFiles = 0
                })
                continue
            }
            $skipped.Add("SKIP identical duplicate: " + $file.FullName)
            continue
        }
        $relativePath = $file.FullName.Substring($sourceRoot.Length).TrimStart('\','/')
        $record = [pscustomobject]@{
            Assembly = $assemblyName.Name; AssemblyVersion = $assemblyName.Version.ToString()
            SourcePath = $file.FullName; RelativeSourcePath = $relativePath
            LastWriteTimeUtc = $file.LastWriteTimeUtc.ToString("o"); Length = $file.Length; SHA256 = $hash
        }
        $seenNames[$assemblyName.Name] = $record
        # Reference resolution is local to this frozen snapshot, including third-party dependencies.
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $dependencies $file.Name)
        if ($assemblyName.Name -match '^(TaleWorlds\.|SandBox(?:\.|$)|StoryMode(?:\.|$)|NavalDLC(?:\.|$)|BirthAndDeath(?:\.|$)|CustomBattle(?:\.|$))') {
            $managed.Add($record)
        }
    }
}
if ($managed.Count -eq 0) { throw "No official managed assemblies were found." }
$required = @("TaleWorlds.Library", "TaleWorlds.Core", "TaleWorlds.CampaignSystem", "TaleWorlds.MountAndBlade", "SandBox", "SandBox.View", "SandBox.GauntletUI", "StoryMode", "TaleWorlds.SaveSystem")
foreach ($name in $required) {
    if ($name -notin @($managed | ForEach-Object Assembly)) { throw "Incomplete client snapshot: missing $name" }
}

$failures = [Collections.Generic.List[string]]::new()
$knownDiagnostics = [Collections.Generic.List[string]]::new()
$exports = [Collections.Generic.List[object]]::new()
$index = 0
foreach ($assembly in ($managed | Sort-Object Assembly)) {
    $index++
    Write-Host "[$index/$($managed.Count)] $($assembly.Assembly)"
    $assemblyOutput = Join-Path $staging $assembly.Assembly
    $result = Invoke-Decompiler -Arguments @("--disable-updatecheck", "--nested-directories", "-p", "-r", (Split-Path $assembly.SourcePath -Parent), "-r", $dependencies, "-o", $assemblyOutput, $assembly.SourcePath) -LogName $assembly.Assembly
    $csFiles = @(Get-ChildItem -LiteralPath $assemblyOutput -Filter "*.cs" -File -Recurse -ErrorAction SilentlyContinue)
    $errorComments = @()
    if ($csFiles.Count -gt 0) {
        $errorComments = @(Select-String -LiteralPath $csFiles.FullName -Pattern 'Error decompiling|Failed to decompile|DecompilerException|Could not resolve type reference|Unknown result type|Expected O, but got' | ForEach-Object {
            $_.Path.Substring($staging.Length + 1) + ":" + $_.LineNumber + ": " + $_.Line.Trim()
        })
    }
    $stderr = (Get-Content -LiteralPath $result.Stderr -Raw -ErrorAction SilentlyContinue)
    # Steam omits Epic's managed SDK/provider. Only this reviewed file's type warnings
    # may be accepted explicitly; method failures and every other diagnostic stay fatal.
    $isKnownEpic = $AllowKnownEpicDiagnostics -and $assembly.Assembly -eq 'TaleWorlds.PlatformService.Epic' -and
        $result.ExitCode -eq 0 -and $csFiles.Count -gt 0 -and $errorComments.Count -gt 0 -and
        [string]::IsNullOrWhiteSpace($stderr) -and
        @($errorComments | Where-Object { $_ -notmatch '^TaleWorlds\.PlatformService\.Epic[\\/]TaleWorlds[\\/]PlatformService[\\/]Epic[\\/]EpicPlatformServices\.cs:\d+:\s*//IL_[0-9a-fA-F]+: (Unknown result type|Expected O, but got)' }).Count -eq 0
    if ($isKnownEpic) {
        $knownDiagnostics.Add("KNOWN missing Epic SDK/provider: $($assembly.Assembly), diagnosticComments=$($errorComments.Count)")
        foreach ($comment in $errorComments) { $knownDiagnostics.Add($comment) }
    }
    if ($result.ExitCode -ne 0 -or $csFiles.Count -eq 0 -or ($errorComments.Count -gt 0 -and -not $isKnownEpic) -or -not [string]::IsNullOrWhiteSpace($stderr)) {
        $failures.Add("$($assembly.Assembly): exit=$($result.ExitCode), CSharpFiles=$($csFiles.Count), diagnosticComments=$($errorComments.Count)")
        if ($stderr) { $failures.Add($stderr.Trim()) }
        foreach ($comment in $errorComments) { $failures.Add($comment) }
    }
    if ((Get-FileHash -LiteralPath $assembly.SourcePath -Algorithm SHA256).Hash -ne $assembly.SHA256) {
        throw "Source changed during export: $($assembly.SourcePath)"
    }
    $exports.Add([pscustomobject]@{ Assembly = $assembly.Assembly; ExitCode = $result.ExitCode; CSharpFiles = $csFiles.Count; DiagnosticComments = $errorComments.Count; KnownEpicDiagnostics = [bool]$isKnownEpic })
}

foreach ($duplicate in $duplicates) {
    # Equal directory depth keeps ILSpy's generated relative HintPath values comparable.
    $comparisonOutput = Join-Path (Join-Path $evidencePath 'duplicate-exports') $duplicate.Assembly
    $comparison = Invoke-Decompiler -Arguments @('--disable-updatecheck', '--nested-directories', '-p', '-r', (Split-Path $duplicate.SourcePath -Parent), '-r', $dependencies, '-o', $comparisonOutput, $duplicate.SourcePath) -LogName ('duplicate-' + $duplicate.Assembly)
    if ($comparison.ExitCode -ne 0 -or -not [string]::IsNullOrWhiteSpace((Get-Content $comparison.Stderr -Raw))) {
        throw "Duplicate export failed: $($duplicate.SourcePath)"
    }
    $canonicalOutput = Join-Path $staging $duplicate.Assembly
    $canonicalFiles = @(Get-ChildItem -LiteralPath $canonicalOutput -File -Recurse)
    $comparisonFiles = @(Get-ChildItem -LiteralPath $comparisonOutput -File -Recurse)
    if ($canonicalFiles.Count -ne $comparisonFiles.Count) { throw 'Duplicate exports have different file counts.' }
    foreach ($file in $canonicalFiles) {
        $relative = $file.FullName.Substring($canonicalOutput.Length + 1)
        $other = Join-Path $comparisonOutput $relative
        if (-not (Test-Path -LiteralPath $other -PathType Leaf) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $other).Hash) {
            throw "Duplicate exports differ: $relative. Both source DLLs remain in the snapshot."
        }
    }
    if ((Get-FileHash -LiteralPath $duplicate.SourcePath).Hash -ne $duplicate.SHA256) { throw 'Duplicate source changed during export.' }
    $duplicate.ExportComparison = 'IDENTICAL_ALL_EXPORTED_FILES'
    $duplicate.ComparedFiles = $canonicalFiles.Count
}

$manifestLines = [Collections.Generic.List[string]]::new()
$manifestLines.Add("BannerlordRoot=$sourceRoot")
$manifestLines.Add("GameVersion=$actualVersion")
$manifestLines.Add("GeneratedAtUtc=$([DateTime]::UtcNow.ToString('o'))")
$manifestLines.Add("IlSpy=$($toolVersion -replace '\r?\n','; ')")
$manifestLines.Add("Assembly|SourcePath|LastWriteTimeUtc|Length|SHA256")
foreach ($assembly in ($managed | Sort-Object Assembly)) {
    $manifestLines.Add("$($assembly.Assembly)|$($assembly.SourcePath)|$($assembly.LastWriteTimeUtc)|$($assembly.Length)|$($assembly.SHA256)")
}
$manifestLines | Set-Content -LiteralPath (Join-Path $staging "_manifest.txt") -Encoding utf8
@($skipped.ToArray()) + @($knownDiagnostics.ToArray()) + @($failures.ToArray()) | Set-Content -LiteralPath (Join-Path $staging "_failures.txt") -Encoding utf8
$allSourceFiles = @(Get-ChildItem -LiteralPath $staging -Filter "*.cs" -File -Recurse)
$receipt = [ordered]@{
    GameVersion = $actualVersion; Api = $actualApi; SourceRoot = $sourceRoot; Destination = $destination
    Decompiler = $toolVersion; DecompilerExeSHA256 = (Get-FileHash -LiteralPath $toolPath -Algorithm SHA256).Hash
    AssemblyCount = $managed.Count; CSharpFileCount = $allSourceFiles.Count
    Status = $(if ($failures.Count -gt 0) { "PARTIAL_DIAGNOSTICS" } elseif ($knownDiagnostics.Count -gt 0) { "DECOMPILED_WITH_KNOWN_PLATFORM_DIAGNOSTICS" } else { "DECOMPILED" })
    Assemblies = $managed.ToArray(); Exports = $exports.ToArray(); Skipped = $skipped.ToArray(); Failures = $failures.ToArray()
    DuplicateSources = $duplicates.ToArray()
    KnownDiagnostics = $knownDiagnostics.ToArray(); AllowKnownEpicDiagnostics = [bool]$AllowKnownEpicDiagnostics
    GameCompatibility = "NOT_TESTED"; OriginalDirectoriesChanged = $false
}
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $staging "_manifest.json") -Encoding utf8
@"
# Bannerlord $actualVersion 本地反编译参考

来源为真实客户端程序集；每个程序集一个目录，保留 ILSpy 生成的逐类型 C# 和工程，布局沿用既有 1.4.5 参考。
精确来源与 SHA-256 见 _manifest.txt / _manifest.json；跳过的原生文件及实际诊断见 _failures.txt。
此目录用于阅读与适配分析，不能作为 AF 编译引用、游戏原始工程或 1.5 兼容证明。C++ 原生引擎内部不在导出范围。
依赖和原始 DLL 只保存在 ignored artifacts；AF 既有项目已排除“原版游戏本体代码*”目录，发布包不应包含本目录。
官方 CustomBattle / Multiplayer 中同名 DLL 的不同二进制会逐文件比较反编译结果；仅在全部导出文件相同时共用一个目录，两个来源的哈希与比较结果保留在 _manifest.json。
Steam 客户端未附带 Epic 的托管 SDK/provider；如清单标记 KNOWN，该平台连接文件存在明确记录的未解析类型注释。其余程序集仍通过严格诊断检查；不要把这些注释当成原游戏源码或已验证的实现。
"@ | Set-Content -LiteralPath (Join-Path $staging "README.md") -Encoding utf8

if ($failures.Count -gt 0) {
    throw "Export preserved in $staging with diagnostics; no final directory was published. Review _failures.txt."
}
# Both resolved move targets were checked above; never replace an existing reference tree.
Assert-InRepository $staging
Assert-InRepository $destination
Move-Item -LiteralPath $staging -Destination $destination
Write-Host "Exported $($managed.Count) assemblies / $($allSourceFiles.Count) C# files to $destination ($actualVersion)."
