# Localization is a ZIP-only projection. Never modify the source assets or clean Stage.
function Test-AnimusForgeChineseText {
    param([AllowEmptyString()][string]$Text)
    $decoded = $Text
    if ($Text.Contains('\u')) { $decoded = [regex]::Replace($decoded, '\\u([0-9a-fA-F]{4})', {
        param($match) ([char][Convert]::ToInt32($match.Groups[1].Value, 16)).ToString()
    }) }
    if ($Text.Contains('\U')) { $decoded = [regex]::Replace($decoded, '\\U([0-9a-fA-F]{8})', {
        param($match) [char]::ConvertFromUtf32([Convert]::ToInt32($match.Groups[1].Value, 16))
    }) }
    if ($Text.Contains('\x')) { $decoded = [regex]::Replace($decoded, '\\x([0-9a-fA-F]{1,4})', {
        param($match) ([char][Convert]::ToInt32($match.Groups[1].Value, 16)).ToString()
    }) }
    return [regex]::IsMatch($decoded, '[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]|[\ud840-\ud87e][\udc00-\udfff]')
}

function Get-AnimusForgeWorldbookEnglishIndex {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)
    $chineseBook = -join ([char[]](0x5361, 0x62c9, 0x8fea, 0x4e9a, 0x7f16, 0x5e74, 0x53f2))
    $base = Join-Path $ProjectRoot 'content/modules/AF.Module.Onboarding/PlayerExports'
    $english = Join-Path $base 'Calradia Chronicles'
    $index = @{}
    if (Test-Path -LiteralPath $english -PathType Container) {
        Assert-AnimusForgeNoReparsePoint -Path $english -Label 'English worldbook'
        foreach ($file in Get-ChildItem -LiteralPath $english -Recurse -File -Filter '*.json') {
            Assert-AnimusForgeNoReparsePoint -Path $file.FullName -Label 'English worldbook asset'
            $relative = $file.FullName.Substring($english.Length).TrimStart('\', '/').Replace('\', '/')
            $key = $relative
            if ($relative.StartsWith('knowledge/rules/')) {
                $value = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
                if ([string]::IsNullOrWhiteSpace([string]$value.Id)) { throw "Worldbook rule has no Id: $relative" }
                $key = 'knowledge/rules/id:' + [string]$value.Id
            }
            elseif ($relative.StartsWith('personality_background/')) {
                $key = 'personality_background/hero:' + ($file.BaseName -split '__', 2)[0]
            }
            if ($index.ContainsKey($key)) { throw "Ambiguous English worldbook identity: $key" }
            $index[$key] = $file.FullName
        }
    }
    return @{ ChineseBook = $chineseBook; Files = $index }
}

function Get-AnimusForgeEnglishAsset {
    param([Parameter(Mandatory = $true)]$Entry, [Parameter(Mandatory = $true)]$Worldbook)
    $source = [string]$Entry.SourcePath
    $extension = [IO.Path]::GetExtension($source)
    $stem = $source -replace '(?:\.zh-CN)?\.(?:json|xml)$', ''
    foreach ($candidate in @(($stem + '.en' + $extension), ($stem + '.en-US' + $extension))) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            Assert-AnimusForgeNoReparsePoint -Path $candidate -Label 'English asset'
            return $candidate
        }
    }
    $bookPrefix = 'PlayerExports/' + $Worldbook.ChineseBook + '/'
    if ([string]$Entry.Target -and $Entry.Target.StartsWith($bookPrefix, [StringComparison]::Ordinal)) {
        $key = $Entry.Target.Substring($bookPrefix.Length)
        if ($key.StartsWith('knowledge/rules/')) {
            $value = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
            $key = 'knowledge/rules/id:' + [string]$value.Id
        }
        elseif ($key.StartsWith('personality_background/')) {
            $key = 'personality_background/hero:' + ([IO.Path]::GetFileNameWithoutExtension($key) -split '__', 2)[0]
        }
        if ($Worldbook.Files.ContainsKey($key)) { return [string]$Worldbook.Files[$key] }
    }
    return $null
}

function Assert-AnimusForgeEnglishJsonContract {
    param([Parameter(Mandatory = $true)][string]$OriginalPath, [Parameter(Mandatory = $true)][string]$EnglishPath)
    $originalText = Get-Content -LiteralPath $OriginalPath -Raw -Encoding UTF8
    $englishText = Get-Content -LiteralPath $EnglishPath -Raw -Encoding UTF8
    if ($originalText.TrimStart().StartsWith('[') -ne $englishText.TrimStart().StartsWith('[')) {
        throw "JSON root array shape changed: $EnglishPath"
    }
    $original = $originalText | ConvertFrom-Json
    $english = $englishText | ConvertFrom-Json
    $queue = [Collections.Generic.Queue[object]]::new()
    $queue.Enqueue([pscustomobject]@{ Original=$original; English=$english; Path='$'; Stable=$false })
    while ($queue.Count -gt 0) {
        $node = $queue.Dequeue()
        $left = $node.Original; $right = $node.English
        if ($null -eq $left -or $null -eq $right) {
            if ($null -ne $left -or $null -ne $right) { throw "JSON null shape changed: $($node.Path) in $EnglishPath" }
            continue
        }
        if ($left.GetType() -ne $right.GetType()) { throw "JSON value type changed: $($node.Path) in $EnglishPath" }
        if ($left -is [pscustomobject]) {
            $leftNames = @($left.PSObject.Properties.Name | Sort-Object)
            $rightNames = @($right.PSObject.Properties.Name | Sort-Object)
            if (($leftNames -join "`n") -cne ($rightNames -join "`n")) { throw "JSON field set changed: $($node.Path) in $EnglishPath" }
            foreach ($key in $leftNames) {
                $queue.Enqueue([pscustomobject]@{ Original=$left.$key; English=$right.$key; Path=($node.Path + '.' + $key); Stable=($key -match '^(Id|TopicId|RuleId|ActionId|Version|SchemaVersion)$') })
            }
        }
        elseif ($left -is [array]) {
            # Match/accept keyword lists are translated vocabularies; other arrays
            # retain all original topics, variants, constraints and records.
            $vocabulary = $node.Path -match '\.(Keywords|TriggerKeywords|AcceptKeywords)$'
            if ($left.Count -ne $right.Count -and -not $vocabulary) { throw "JSON array length changed: $($node.Path) in $EnglishPath" }
            if (-not $vocabulary) {
                for ($index = 0; $index -lt $left.Count; $index++) {
                    $queue.Enqueue([pscustomobject]@{ Original=$left[$index]; English=$right[$index]; Path=($node.Path + '[' + $index + ']'); Stable=$node.Stable })
                }
            }
        }
        elseif ($left -is [string]) {
            if ($node.Stable -and $left -cne $right) { throw "Stable JSON identity changed: $($node.Path) in $EnglishPath" }
            $pattern = '\{[A-Za-z_][A-Za-z0-9_]*\}|\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]'
            $before = @([regex]::Matches($left, $pattern) | ForEach-Object Value | Sort-Object -Unique)
            $after = @([regex]::Matches($right, $pattern) | ForEach-Object Value | Sort-Object -Unique)
            if (($before -join "`n") -cne ($after -join "`n")) { throw "Prompt variable/protocol changed: $($node.Path) in $EnglishPath" }
        }
        elseif ($left -cne $right) { throw "JSON non-text value changed: $($node.Path) in $EnglishPath" }
    }
}

function Assert-AnimusForgePromptXmlContract {
    param([string]$OriginalPath, [string]$EnglishPath)
    [xml]$original = Get-Content -LiteralPath $OriginalPath -Raw -Encoding UTF8
    [xml]$english = Get-Content -LiteralPath $EnglishPath -Raw -Encoding UTF8
    if ($original.base.language -cne 'zh-CN' -or $english.base.language -cne 'en') { throw 'Prompt XML edition is incorrect.' }
    $left = @{}; $right = @{}
    foreach ($row in $original.base.strings.string) { $left.Add([string]$row.id, [string]$row.text) }
    foreach ($row in $english.base.strings.string) { $right.Add([string]$row.id, [string]$row.text) }
    if ((@($left.Keys | Sort-Object) -join "`n") -cne (@($right.Keys | Sort-Object) -join "`n")) { throw 'Prompt XML key set changed.' }
    $pattern = '\{[A-Za-z_][A-Za-z0-9_]*\}|\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]'
    foreach ($key in $left.Keys) {
        $before = @([regex]::Matches($left[$key], $pattern) | ForEach-Object Value | Sort-Object -Unique)
        $after = @([regex]::Matches($right[$key], $pattern) | ForEach-Object Value | Sort-Object -Unique)
        if (($before -join "`n") -cne ($after -join "`n")) { throw "Prompt XML variable/protocol changed: $key" }
    }
}

function Get-AnimusForgePackageProjection {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$StageModuleDir,
        [Parameter(Mandatory = $true)][ValidateSet('CN', 'EN')][string]$Language,
        [Parameter(Mandatory = $true)][string]$PackageVersion
    )
    $layout = @(Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot)
    $byTarget = @{}
    foreach ($entry in $layout) { $byTarget[$entry.Target] = $entry }
    # Both editions retain the registered bilingual interface. Only package
    # prompt/worldbook assets select an English counterpart below.
    $sharedLocalizationTargets = @{}
    foreach ($entry in $layout | Where-Object { $_.Target -match '^ModuleData/Languages/(?:CNs/)?language_data\.xml$' }) {
        $sharedLocalizationTargets[$entry.Target] = $true
        [xml]$registration = Get-Content -LiteralPath $entry.SourcePath -Raw -Encoding UTF8
        foreach ($languageFile in $registration.LanguageData.LanguageFile) {
            $target = 'ModuleData/Languages/' + [string]$languageFile.xml_path
            if (-not $byTarget.ContainsKey($target)) { throw "Registered localization is not mapped: $target" }
            $sharedLocalizationTargets[$target] = $true
        }
    }
    $worldbook = Get-AnimusForgeWorldbookEnglishIndex -ProjectRoot $ProjectRoot
    $stage = (Get-AnimusForgeContentFullPath -Path $StageModuleDir).TrimEnd('\', '/')
    $projection = [Collections.Generic.List[object]]::new()
    $failures = [Collections.Generic.List[string]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File -Force | Sort-Object FullName) {
        $relative = $file.FullName.Substring($stage.Length).TrimStart('\', '/').Replace('\', '/')
        $source = $file.FullName
        $bytes = $null
        if ($relative -ceq 'SubModule.xml') {
            $bytes = Get-VersionedSubModuleBytes -SubModulePath $source -NewVersion $PackageVersion
        }
        elseif ($Language -eq 'EN' -and $relative -ceq 'ModuleData/SceneActionPromptTexts.xml') {
            $english = Get-AnimusForgeEnglishAsset -Entry $byTarget[$relative] -Worldbook $worldbook
            if (-not $english) { throw "English scene prompt corpus is missing: $relative" }
            Assert-AnimusForgePromptXmlContract -OriginalPath $byTarget[$relative].SourcePath -EnglishPath $english
            $source = $english
        }
        elseif ($Language -eq 'EN' -and $relative -match '\.json$' -and $byTarget.ContainsKey($relative)) {
            $english = Get-AnimusForgeEnglishAsset -Entry $byTarget[$relative] -Worldbook $worldbook
            if ($english) { $source = $english }
            try {
                $null = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
                if ($english) { Assert-AnimusForgeEnglishJsonContract -OriginalPath $byTarget[$relative].SourcePath -EnglishPath $english }
            }
            catch { $failures.Add("JSON contract failed: $relative ($($_.Exception.Message))"); continue }
        }
        if ($Language -eq 'EN' -and -not $sharedLocalizationTargets.ContainsKey($relative) -and $relative -match '\.(json|xml|txt|md|csv|yml|yaml)$') {
            $text = if ($null -ne $bytes) { [Text.Encoding]::UTF8.GetString($bytes) } else { [IO.File]::ReadAllText($source, [Text.Encoding]::UTF8) }
            if (Test-AnimusForgeChineseText -Text $text) {
                $failures.Add("Chinese text remains or English asset is missing: $relative")
                continue
            }
        }
        $hash = if ($null -ne $bytes) {
            $sha = [Security.Cryptography.SHA256]::Create()
            try { [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '') }
            finally { $sha.Dispose() }
        } else { (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash }
        $projection.Add([pscustomobject]@{ Target = $relative; SourcePath = $source; Bytes = $bytes; Hash = $hash })
    }
    if ($failures.Count -gt 0) {
        throw "EN package content is incomplete ($($failures.Count) files). First failures:`n$(@($failures | Select-Object -First 12) -join "`n")"
    }
    $targets = @{}
    foreach ($item in $projection) { $targets[$item.Target] = $true }
    foreach ($item in $projection | Where-Object { $_.Target -match '\.xml$' }) {
        try {
            $xmlText = if ($null -ne $item.Bytes) { [Text.Encoding]::UTF8.GetString($item.Bytes) } else { [IO.File]::ReadAllText($item.SourcePath, [Text.Encoding]::UTF8) }
            [xml]$xml = $xmlText
        } catch { throw "Invalid projected XML: $($item.Target)" }
        if ($item.Target -match '^ModuleData/Languages/(?:CNs/)?language_data\.xml$') {
            foreach ($languageFile in $xml.LanguageData.LanguageFile) {
                $target = 'ModuleData/Languages/' + [string]$languageFile.xml_path
                if (-not $targets.ContainsKey($target)) { throw "Projected LanguageData references a missing file: $target" }
            }
        }
    }
    return $projection
}

function Assert-AnimusForgeEnglishRuntimeSource {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)
    # Conservative lexical release gate: comments are excluded, all runtime literals
    # (including protocols and logs) are gated. No UI-only claim or silent whitelist.
    $token = [regex]'(?s)//[^\r\n]*|/\*.*?\*/|(?<literal>(?:\$@|@\$|@)"(?:""|[^"])*"|\$?"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\])'')'
    $failures = [Collections.Generic.List[string]]::new()
    $failureCount = 0
    :runtimeRoots foreach ($name in @('src', 'extensions/AnimusForge.Illustrator/src', 'extensions/AnimusForge.DialogueUI/src',
        'extensions/AnimusForge.Coup/src', 'extensions/AnimusForge.XihaiAction/src/CoreProject',
        'extensions/AnimusForge.XihaiAction/src/Runtime', 'PolicySystem', 'Vengeance', 'WarStats', 'AnimusForge.SiegeAftermathIntervention')) {
        $directory = Join-Path $ProjectRoot $name
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) { continue }
        foreach ($file in Get-ChildItem -LiteralPath $directory -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName.Substring($directory.Length) -notmatch '[\\/](bin|obj|tests|NuGet)[\\/]' }) {
            $text = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
            foreach ($match in $token.Matches($text)) {
                if ($match.Groups['literal'].Success -and (Test-AnimusForgeChineseText $match.Value)) {
                    $failureCount++
                    if ($failures.Count -lt 12) {
                        $relative = $file.FullName.Substring($ProjectRoot.TrimEnd('\', '/').Length).TrimStart('\', '/')
                        $line = 1 + [regex]::Matches($text.Substring(0, $match.Index), '\n').Count
                        $failures.Add("${relative}:$line")
                    }
                    if ($failures.Count -ge 12) { break runtimeRoots }
                }
            }
        }
    }
    if ($failureCount -gt 0) {
        throw "Runtime Chinese literals block EN packaging (at least $failureCount occurrences; this is a release gate, not a full inventory). First locations:`n$($failures -join "`n")"
    }
    # A shared implementation cannot have Chinese embedded defaults, even if the
    # disk copy has been projected to English.
    $embedded = @(Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot | Where-Object { $_.LogicalName })
    foreach ($entry in $embedded) {
        if (Test-AnimusForgeChineseText ([IO.File]::ReadAllText($entry.SourcePath, [Text.Encoding]::UTF8))) {
            throw "Shared DLL embedded default is still Chinese: $($entry.Source). Disk-only localization is insufficient."
        }
    }
}

function Assert-AnimusForgeEnglishRuntimeDll {
    param([Parameter(Mandatory = $true)][string]$DllPath)
    # Inspect ECMA-335 user strings and embedded resource bytes without loading
    # game assemblies or executing module initializers.
    $data = [IO.File]::ReadAllBytes($DllPath)
    $read32 = { param([int]$Offset) [BitConverter]::ToUInt32($data, $Offset) }
    $pe = & $read32 0x3c
    if ([BitConverter]::ToUInt32($data, $pe) -ne 0x00004550) { throw "Invalid PE: $DllPath" }
    $sectionCount = [BitConverter]::ToUInt16($data, $pe + 6)
    $optionalSize = [BitConverter]::ToUInt16($data, $pe + 20)
    $optional = $pe + 24
    $magic = [BitConverter]::ToUInt16($data, $optional)
    $directories = switch ($magic) { 0x10b { $optional + 96 }; 0x20b { $optional + 112 }; default { throw "Unsupported PE: $DllPath" } }
    $sections = $optional + $optionalSize
    $mapRva = {
        param([uint32]$Rva)
        for ($index = 0; $index -lt $sectionCount; $index++) {
            $section = $sections + 40 * $index
            $start = & $read32 ($section + 12)
            $size = & $read32 ($section + 16)
            if ($Rva -ge $start -and $Rva -lt ($start + $size)) {
                return [int]((& $read32 ($section + 20)) + $Rva - $start)
            }
        }
        throw "Invalid managed RVA in $DllPath"
    }
    $cliRva = & $read32 ($directories + 14 * 8)
    if ($cliRva -eq 0) { throw "Runtime artifact is not managed: $DllPath" }
    $cli = & $mapRva $cliRva
    $metadata = & $mapRva (& $read32 ($cli + 8))
    if ((& $read32 $metadata) -ne 0x424a5342) { throw "Invalid managed metadata: $DllPath" }
    $versionLength = & $read32 ($metadata + 12)
    $header = $metadata + 16 + $versionLength
    $streamCount = [BitConverter]::ToUInt16($data, $header + 2)
    $cursor = $header + 4
    $foundStrings = $false
    for ($index = 0; $index -lt $streamCount; $index++) {
        $offset = & $read32 $cursor
        $size = & $read32 ($cursor + 4)
        $nameStart = $cursor + 8
        $nameEnd = $nameStart
        while ($data[$nameEnd] -ne 0) { $nameEnd++ }
        $name = [Text.Encoding]::ASCII.GetString($data, $nameStart, $nameEnd - $nameStart)
        $cursor = $nameStart + 4 * [int][Math]::Ceiling(($nameEnd - $nameStart + 1) / 4.0)
        if ($name -ne '#US') { continue }
        $foundStrings = $true
        $position = $metadata + $offset + 1
        $end = $metadata + $offset + $size
        while ($position -lt $end) {
            $first = [int]$data[$position]; $position++
            if (($first -band 0x80) -eq 0) { $length = $first }
            elseif (($first -band 0xc0) -eq 0x80) {
                $length = (($first -band 0x3f) -shl 8) -bor [int]$data[$position]; $position++
            } elseif (($first -band 0xe0) -eq 0xc0) {
                $length = (($first -band 0x1f) -shl 24) -bor ([int]$data[$position] -shl 16) -bor ([int]$data[$position + 1] -shl 8) -bor [int]$data[$position + 2]; $position += 3
            } else { throw "Invalid managed user string in $DllPath" }
            if ($length -eq 0) { continue }
            if ($position + $length -gt $end -or (($length - 1) % 2) -ne 0) { throw "Truncated managed string in $DllPath" }
            if (Test-AnimusForgeChineseText ([Text.Encoding]::Unicode.GetString($data, $position, $length - 1))) {
                throw "Chinese runtime string remains in actual DLL: $DllPath (metadata offset $position)"
            }
            $position += $length
        }
    }
    if (-not $foundStrings) { throw "Managed user string heap was not found: $DllPath" }
    $resourceRva = & $read32 ($cli + 24)
    $resourceSize = & $read32 ($cli + 28)
    if ($resourceSize -gt 0) {
        $resourceStart = & $mapRva $resourceRva
        if (Test-AnimusForgeChineseText ([Text.Encoding]::UTF8.GetString($data, $resourceStart, $resourceSize))) {
            throw "Chinese embedded text remains in actual DLL: $DllPath"
        }
    }
}

function Write-AnimusForgeProjectedZip {
    param([Parameter(Mandatory = $true)][string]$ZipPath, [Parameter(Mandatory = $true)]$Projection)
    $zip = [IO.Compression.ZipFile]::Open($ZipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($item in $Projection) {
            if ($null -eq $item.Bytes) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $item.SourcePath, ('AnimusForge/' + $item.Target), [IO.Compression.CompressionLevel]::Optimal) | Out-Null
            } else {
                $entry = $zip.CreateEntry('AnimusForge/' + $item.Target, [IO.Compression.CompressionLevel]::Optimal)
                $stream = $entry.Open()
                try { $stream.Write($item.Bytes, 0, $item.Bytes.Length) }
                finally { $stream.Dispose() }
            }
        }
    } finally { $zip.Dispose() }
}

function Assert-AnimusForgeProjectedZip {
    param([Parameter(Mandatory = $true)][string]$ZipPath, [Parameter(Mandatory = $true)]$Projection)
    $zip = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        if ($zip.Entries.Count -ne $Projection.Count) { throw 'ZIP entry count differs from the locked localization projection.' }
        foreach ($item in $Projection) {
            if ($null -eq $item.Bytes -and (Get-FileHash -LiteralPath $item.SourcePath -Algorithm SHA256).Hash -cne $item.Hash) {
                throw "Localization source changed during packaging: $($item.Target)"
            }
            $entry = Get-RequiredZipEntry -Archive $zip -EntryName ('AnimusForge/' + $item.Target)
            if ((Get-ZipEntrySha256 -Entry $entry) -ine $item.Hash) { throw "ZIP differs from its localization projection: $($item.Target)" }
        }
    } finally { $zip.Dispose() }
}
