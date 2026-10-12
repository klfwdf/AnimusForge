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
    $mappedFiles = @{}
    $ruleMappings = @{}
    $mapPath = Join-Path $base 'english-worldbook-identity-map.json'
    if (Test-Path -LiteralPath $mapPath -PathType Leaf) {
        $identityMap = Get-Content -LiteralPath $mapPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($bookProperty in $identityMap.worldbooks.PSObject.Properties) {
            $book = $bookProperty.Value
            foreach ($section in @('globals', 'heroes', 'rules')) {
                foreach ($mapping in $book.$section.PSObject.Properties) {
                    $englishRelative = if ($section -eq 'globals') { [string]$mapping.Value } else { [string]$mapping.Value.en_file }
                    $sourceRelative = [string]$book.en_directory + '/' + $englishRelative
                    $chineseRelative = [string]$book.zh_directory + '/' + $mapping.Name
                    Assert-AnimusForgeRelativeContentPath -Path $sourceRelative -Label 'Mapped English worldbook'
                    Assert-AnimusForgeRelativeContentPath -Path $chineseRelative -Label 'Mapped Chinese worldbook'
                    $target = 'PlayerExports/' + $chineseRelative
                    $mappedFiles.Add($target, (Join-Path $base $sourceRelative))
                    if ($section -eq 'rules') { $ruleMappings.Add($target, $mapping.Value) }
                }
            }
        }
    }
    return @{ ChineseBook = $chineseBook; Files = $index; MappedFiles = $mappedFiles; RuleMappings = $ruleMappings }
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
    if ($Worldbook.MappedFiles -and $Worldbook.MappedFiles.ContainsKey([string]$Entry.Target)) {
        $candidate = [string]$Worldbook.MappedFiles[[string]$Entry.Target]
        Assert-AnimusForgeNoReparsePoint -Path $candidate -Label 'Mapped English worldbook asset'
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Mapped English worldbook asset is missing: $candidate" }
        return $candidate
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
    param([Parameter(Mandatory = $true)][string]$OriginalPath, [Parameter(Mandatory = $true)][string]$EnglishPath, $EnglishValue = $null)
    $originalText = Get-Content -LiteralPath $OriginalPath -Raw -Encoding UTF8
    $englishText = Get-Content -LiteralPath $EnglishPath -Raw -Encoding UTF8
    if ($originalText.TrimStart().StartsWith('[') -ne $englishText.TrimStart().StartsWith('[')) {
        throw "JSON root array shape changed: $EnglishPath"
    }
    $original = $originalText | ConvertFrom-Json
    $english = if ($null -ne $EnglishValue) { $EnglishValue } else { $englishText | ConvertFrom-Json }
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
            $pattern = '\{(?:[A-Za-z_][A-Za-z0-9_]*|[0-9]+)\}|\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]|' + (Get-AnimusForgeAfefFactMarkerPattern)
            $before = @([regex]::Matches($left, $pattern) | ForEach-Object { ConvertTo-AnimusForgeProtocolContractToken -Token $_.Value -JsonPath $node.Path -OriginalPath $OriginalPath } | Sort-Object -Unique)
            $after = @([regex]::Matches($right, $pattern) | ForEach-Object { ConvertTo-AnimusForgeProtocolContractToken -Token $_.Value -JsonPath $node.Path -OriginalPath $OriginalPath } | Sort-Object -Unique)
            if (($before -join "`n") -cne ($after -join "`n")) { throw "Prompt variable/protocol changed: $($node.Path) in $EnglishPath" }
        }
        elseif ($left -cne $right) { throw "JSON non-text value changed: $($node.Path) in $EnglishPath" }
    }
}

function ConvertTo-AnimusForgeProtocolContractToken {
    param([string]$Token, [string]$JsonPath, [string]$OriginalPath)
    if ([IO.Path]::GetFileName($OriginalPath) -cne 'RuleBehaviorPrompts.json') { return $Token }
    if ($null -eq $script:AnimusForgeReviewedProtocolExamples) {
        # Only exact documented parameter specimens at reviewed paths may translate.
        # Executable tags, unknown examples, identifiers and fixed command words stay strict.
        $script:AnimusForgeReviewedProtocolExamples = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.Duel.PostprocessRules[1].Tag|[AD:\u4ef7\u503c:\u5929\u6570:N/P:\u5907\u6ce8\u5185\u5bb9]'), '[AD:value:days:N/P:note]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.Duel.PostprocessRules[2].Tag|[ACTION:DUEL_LINE_WIN:\u5439\u5618\u7684\u5185\u5bb9]'), '[ACTION:DUEL_LINE_WIN:boast text]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.Duel.PostprocessRules[3].Tag|[ACTION:DUEL_LINE_LOSE:\u5410\u69fd\u7684\u5185\u5bb9]'), '[ACTION:DUEL_LINE_LOSE:loss line]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.Reward.PostprocessRules[0].Tag|[ACTION:GIVE_ASSET:\u8d44\u4ea7\u540d\u79f0\u6216ID:\u6570\u91cf]'), '[ACTION:GIVE_ASSET:asset name or ID:quantity]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.Reward.PostprocessRules[1].Tag|[AD:\u4ef7\u503c:\u5929\u6570:N/P:\u5907\u6ce8\u5185\u5bb9]'), '[AD:value:days:N/P:note]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.Reward.PostprocessRules[2].Tag|[ADP:\u503a\u52a1ID]'), '[ADP:debt ID]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[3].PostprocessRules[3].Tag|[ACTION:SCENE_SUMMON:\u4eba\u7269\u540d\u79f0\u5217\u8868]'), '[ACTION:SCENE_SUMMON:character names]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[3].PostprocessRules[4].Tag|[ACTION:SCENE_GUIDE:\u4eba\u7269\u540d\u79f0]'), '[ACTION:SCENE_GUIDE:character name]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[9].PostprocessRules[0].Tag|[ACTION:AGENDA:\u8bae\u7a0bID:\u9009\u9879ID:\u6743\u91cd]'), '[ACTION:AGENDA:agenda ID:option ID:weight]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].PostprocessRules[0].Tag|[ACTION:DIPLOMACY:COMMIT:action=Peace;move=NewMatter;target=\u738b\u56fdID]'), '[ACTION:DIPLOMACY:COMMIT:action=Peace;move=NewMatter;target=kingdom ID]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].PostprocessRules[1].Tag|[ACTION:DIPLOMACY:COMMITMENT:arrangement=\u7ea6\u5b9aID;state=deferred;reason=\u660e\u786e\u539f\u56e0]'), '[ACTION:DIPLOMACY:COMMITMENT:arrangement=agreement ID;state=deferred;reason=explicit reason]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].PostprocessRules[4].Tag|[ACTION:DIPLOMACY:MAKE_PEACE:id1:id2:\u91d1\u989d:\u5929\u6570]'), '[ACTION:DIPLOMACY:MAKE_PEACE:id1:id2:amount:days]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].PostprocessRules[5].Tag|[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:\u5929\u6570]'), '[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:days]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].PostprocessRules[7].Tag|[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:\u5929\u6570]'), '[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:days]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].PostprocessRules[9].Tag|[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=\u81e3\u5c5e\u56fdID;agreement=\u672c\u8f6e\u6761\u7ea6\u51ed\u8bc1]'), '[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=subject kingdom ID;agreement=current round treaty proof]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].RuntimeInstructionTemplates.native_action_postprocess|[ACTION:DIPLOMACY:DECLARE_WAR:NPC\u738b\u56fdID:\u76ee\u6807\u738b\u56fdID]'), '[ACTION:DIPLOMACY:DECLARE_WAR:NPC kingdom ID:target kingdom ID]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].RuntimeInstructionTemplates.native_action_postprocess|[ACTION:DIPLOMACY:MAKE_PEACE:\u4ed8\u6b3e\u56fdID:\u6536\u6b3e\u56fdID:\u6bcf\u65e5\u91d1\u989d:\u652f\u4ed8\u5929\u6570]'), '[ACTION:DIPLOMACY:MAKE_PEACE:paying kingdom ID:receiving kingdom ID:daily amount:payment days]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].RuntimeInstructionTemplates.native_action_postprocess|[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:\u5929\u6570]'), '[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:days]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[12].RuntimeInstructionTemplates.native_action_postprocess|[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:\u5929\u6570]'), '[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:days]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[13].PostprocessRules[0].Tag|[ACTION:DIPLOMACY:COMMIT:action=Tributary;move=NewMatter;target=\u738b\u56fdID;receiving=\u5b97\u4e3b\u56fdID;joining=\u81e3\u5c5e\u56fdID]'), '[ACTION:DIPLOMACY:COMMIT:action=Tributary;move=NewMatter;target=kingdom ID;receiving=suzerain kingdom ID;joining=subject kingdom ID]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[13].PostprocessRules[1].Tag|[ACTION:DIPLOMACY:COMMITMENT:arrangement=\u7ea6\u5b9aID;state=deferred;reason=\u660e\u786e\u539f\u56e0]'), '[ACTION:DIPLOMACY:COMMITMENT:arrangement=agreement ID;state=deferred;reason=explicit reason]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[13].PostprocessRules[2].Tag|[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=\u81e3\u5c5e\u56fdID;agreement=\u672c\u8f6e\u6761\u7ea6\u51ed\u8bc1]'), '[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=subject kingdom ID;agreement=current round treaty proof]')
        $script:AnimusForgeReviewedProtocolExamples.Add([regex]::Unescape('$.RulePrompts[15].PostprocessRules[0].Tag|[RELAY:\u63a5\u529b\u7f16\u53f7]'), '[RELAY:relay number]')
    }
    $canonical = $null
    if ($script:AnimusForgeReviewedProtocolExamples.TryGetValue($JsonPath + "|" + $Token, [ref]$canonical)) { return $canonical }
    return $Token
}

function Get-AnimusForgeAfefFactMarkerPattern {
    # These exact fact envelopes are consumed by the history/AFEF classifiers.
    # Do not exempt arbitrary bracketed Chinese prose or an entire prompt value.
    return '\[AFEF(?: NPC|\u73a9\u5bb6| )?\u884c\u4e3a\u8865\u5145\]|\u3010AFEF(?: NPC|\u73a9\u5bb6| )?\u884c\u4e3a\u8865\u5145\u3011'
}

function Test-AnimusForgeChineseJsonProse {
    param([Parameter(Mandatory = $true)][string]$Text)
    $queue = [Collections.Generic.Queue[object]]::new()
    $queue.Enqueue(($Text | ConvertFrom-Json))
    while ($queue.Count -gt 0) {
        $value = $queue.Dequeue()
        if ($value -is [pscustomobject]) {
            foreach ($property in $value.PSObject.Properties) {
                if ($property.Name -notmatch '^(Id|TopicId|RuleId|ActionId|Version|SchemaVersion)$') { $queue.Enqueue($property.Value) }
            }
        }
        elseif ($value -is [array]) { foreach ($item in $value) { $queue.Enqueue($item) } }
        elseif ($value -is [string]) {
            $prose = [regex]::Replace($value, '\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]', '')
            $prose = [regex]::Replace($prose, (Get-AnimusForgeAfefFactMarkerPattern), '')
            if (Test-AnimusForgeChineseText -Text $prose) { return $true }
        }
    }
    return $false
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

function Get-AnimusForgeSharedTextResourceHashes {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)
    $resources = @{}
    foreach ($entry in Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot) {
        if ($entry.LogicalName -and $entry.LocalizationRole -in @('compatibility', 'bilingual-ui')) {
            $resources.Add($entry.LogicalName, (Get-FileHash -LiteralPath $entry.SourcePath -Algorithm SHA256).Hash)
        }
    }
    return $resources
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
    foreach ($entry in $layout | Where-Object { $_.LocalizationRole -in @('compatibility', 'bilingual-ui') }) {
        $sharedLocalizationTargets[$entry.Target] = $true
    }
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
        elseif ($Language -eq 'EN' -and $relative -match '\.json$' -and $byTarget.ContainsKey($relative) -and -not $sharedLocalizationTargets.ContainsKey($relative)) {
            $english = Get-AnimusForgeEnglishAsset -Entry $byTarget[$relative] -Worldbook $worldbook
            if ($english) { $source = $english }
            try {
                $null = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
                if ($english -and $worldbook.RuleMappings.ContainsKey($relative)) {
                    $identity = $worldbook.RuleMappings[$relative]
                    $original = Get-Content -LiteralPath $byTarget[$relative].SourcePath -Raw -Encoding UTF8 | ConvertFrom-Json
                    $translated = Get-Content -LiteralPath $english -Raw -Encoding UTF8 | ConvertFrom-Json
                    if ($original.Id -cne $identity.zh_id -or $translated.Id -cne $identity.en_id) { throw "Worldbook rule identity mapping is stale: $relative" }
                    $translated.Id = $original.Id
                    Assert-AnimusForgeEnglishJsonContract -OriginalPath $byTarget[$relative].SourcePath -EnglishPath $english -EnglishValue $translated
                    $bytes = [Text.Encoding]::UTF8.GetBytes(($translated | ConvertTo-Json -Depth 100))
                }
                elseif ($english) { Assert-AnimusForgeEnglishJsonContract -OriginalPath $byTarget[$relative].SourcePath -EnglishPath $english }
            }
            catch { $failures.Add("JSON contract failed: $relative ($($_.Exception.Message))"); continue }
        }
        if ($Language -eq 'EN' -and -not $sharedLocalizationTargets.ContainsKey($relative) -and $relative -match '\.(json|xml|txt|md|csv|yml|yaml)$') {
            $text = if ($null -ne $bytes) { [Text.Encoding]::UTF8.GetString($bytes) } else { [IO.File]::ReadAllText($source, [Text.Encoding]::UTF8) }
            $containsChinese = if ($relative.EndsWith('.json') -and $byTarget.ContainsKey($relative)) {
                Test-AnimusForgeChineseJsonProse -Text $text
            } else { Test-AnimusForgeChineseText -Text $text }
            if ($containsChinese) {
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
    $embedded = @(Get-AnimusForgeContentLayout -ProjectRoot $ProjectRoot | Where-Object { $_.LogicalName })
    foreach ($entry in $embedded) {
        if ($entry.LocalizationRole -in @('compatibility', 'bilingual-ui')) { continue }
        if (Test-AnimusForgeChineseText ([IO.File]::ReadAllText($entry.SourcePath, [Text.Encoding]::UTF8))) {
            throw "Shared DLL embedded default is still Chinese: $($entry.Source). Disk-only localization is insufficient."
        }
    }
}

function Assert-AnimusForgeEnglishRuntimeDll {
    param([Parameter(Mandatory = $true)][string]$DllPath, [hashtable]$SharedTextResourceHashes = @{})
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
    $tablesStart = 0
    $stringsStart = 0
    for ($index = 0; $index -lt $streamCount; $index++) {
        $offset = & $read32 $cursor
        $size = & $read32 ($cursor + 4)
        $nameStart = $cursor + 8
        $nameEnd = $nameStart
        while ($data[$nameEnd] -ne 0) { $nameEnd++ }
        $name = [Text.Encoding]::ASCII.GetString($data, $nameStart, $nameEnd - $nameStart)
        $cursor = $nameStart + 4 * [int][Math]::Ceiling(($nameEnd - $nameStart + 1) / 4.0)
        if ($name -eq '#~' -or $name -eq '#-') { $tablesStart = $metadata + $offset }
        if ($name -eq '#Strings') { $stringsStart = $metadata + $offset }
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
        $heapSizes = $data[$tablesStart + 6]
        $stringWidth = if ($heapSizes -band 1) { 4 } else { 2 }
        $guidWidth = if ($heapSizes -band 2) { 4 } else { 2 }
        $blobWidth = if ($heapSizes -band 4) { 4 } else { 2 }
        $valid = [BitConverter]::ToUInt64($data, $tablesStart + 8)
        $rowCounts = New-Object 'uint32[]' 64
        $tableCursor = $tablesStart + 24
        for ($table = 0; $table -lt 64; $table++) {
            if ($valid -band ([uint64]1 -shl $table)) {
                $rowCounts[$table] = & $read32 $tableCursor
                $tableCursor += 4
            }
        }
        $tableWidth = { param([int]$Table) if ($rowCounts[$Table] -ge 65536) { 4 } else { 2 } }
        $codedWidth = {
            param([int]$Bits, [int[]]$Tables)
            foreach ($table in $Tables) { if ($rowCounts[$table] -ge (1 -shl (16 - $Bits))) { return 4 } }
            return 2
        }
        $typeRefWidth = & $codedWidth 2 @(2, 1, 27)
        $implementationWidth = & $codedWidth 2 @(38, 35, 39)
        $rowSizes = @(
            (2 + $stringWidth + 3 * $guidWidth),
            ((& $codedWidth 2 @(0, 26, 35, 1)) + 2 * $stringWidth),
            (4 + 2 * $stringWidth + $typeRefWidth + (& $tableWidth 4) + (& $tableWidth 6)),
            (& $tableWidth 4),
            (2 + $stringWidth + $blobWidth),
            (& $tableWidth 6),
            (8 + $stringWidth + $blobWidth + (& $tableWidth 8)),
            (& $tableWidth 8),
            (4 + $stringWidth),
            ((& $tableWidth 2) + $typeRefWidth),
            ((& $codedWidth 3 @(2, 1, 26, 6, 27)) + $stringWidth + $blobWidth),
            (2 + (& $codedWidth 2 @(4, 8, 23)) + $blobWidth),
            ((& $codedWidth 5 @(6, 4, 1, 2, 8, 9, 10, 0, 14, 23, 20, 17, 26, 27, 32, 35, 38, 39, 40, 42, 44, 43)) + (& $codedWidth 3 @(6, 10)) + $blobWidth),
            ((& $codedWidth 1 @(4, 8)) + $blobWidth),
            (2 + (& $codedWidth 2 @(2, 6, 32)) + $blobWidth),
            (6 + (& $tableWidth 2)),
            (4 + (& $tableWidth 4)),
            $blobWidth,
            ((& $tableWidth 2) + (& $tableWidth 20)),
            (& $tableWidth 20),
            (2 + $stringWidth + $typeRefWidth),
            ((& $tableWidth 2) + (& $tableWidth 23)),
            (& $tableWidth 23),
            (2 + $stringWidth + $blobWidth),
            (2 + (& $tableWidth 6) + (& $codedWidth 1 @(20, 23))),
            ((& $tableWidth 2) + 2 * (& $codedWidth 1 @(6, 10))),
            $stringWidth,
            $blobWidth,
            (2 + (& $codedWidth 1 @(4, 6)) + $stringWidth + (& $tableWidth 26)),
            (4 + (& $tableWidth 4)),
            8,
            4,
            (16 + $blobWidth + 2 * $stringWidth),
            4,
            12,
            (12 + 2 * $blobWidth + 2 * $stringWidth),
            (4 + (& $tableWidth 35)),
            (12 + (& $tableWidth 35)),
            (4 + $stringWidth + $blobWidth),
            (8 + 2 * $stringWidth + $implementationWidth)
        )
        for ($table = 0; $table -lt 40; $table++) { $tableCursor += $rowCounts[$table] * $rowSizes[$table] }
        $readIndex = { param([int]$Offset, [int]$Width) if ($Width -eq 4) { & $read32 $Offset } else { [BitConverter]::ToUInt16($data, $Offset) } }
        for ($resourceIndex = 0; $resourceIndex -lt $rowCounts[40]; $resourceIndex++) {
            $offset = & $read32 $tableCursor
            $nameIndex = & $readIndex ($tableCursor + 8) $stringWidth
            $implementation = & $readIndex ($tableCursor + 8 + $stringWidth) $implementationWidth
            $tableCursor += 8 + $stringWidth + $implementationWidth
            if ($implementation -ne 0) { continue }
            $nameStart = $stringsStart + $nameIndex
            $nameEnd = $nameStart
            while ($data[$nameEnd] -ne 0) { $nameEnd++ }
            $name = [Text.Encoding]::UTF8.GetString($data, $nameStart, $nameEnd - $nameStart)
            $length = & $read32 ($resourceStart + $offset)
            $resourceBytes = New-Object byte[] $length
            [Array]::Copy($data, ($resourceStart + $offset + 4), $resourceBytes, 0, $length)
            if ($SharedTextResourceHashes.ContainsKey($name)) {
                $sha = [Security.Cryptography.SHA256]::Create()
                try { $hash = [BitConverter]::ToString($sha.ComputeHash($resourceBytes)).Replace('-', '') }
                finally { $sha.Dispose() }
                if ($hash -cne $SharedTextResourceHashes[$name]) { throw "Shared text resource differs from its verified source: $name in $DllPath" }
            }
            elseif (Test-AnimusForgeChineseText ([Text.Encoding]::UTF8.GetString($resourceBytes))) {
                throw "Chinese embedded text remains in actual DLL: $DllPath ($name)"
            }
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
