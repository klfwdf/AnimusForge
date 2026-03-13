$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$vanillaRoot = Get-ChildItem $repoRoot -Directory | Where-Object {
    (Test-Path (Join-Path $_.FullName 'TaleWorlds.CampaignSystem')) -and
    (Test-Path (Join-Path $_.FullName 'SandBox'))
} | Select-Object -First 1

if ($null -eq $vanillaRoot) {
    throw 'Could not locate the vanilla source mirror directory.'
}

$roots = @(
    (Join-Path $vanillaRoot.FullName 'TaleWorlds.CampaignSystem\Issues'),
    (Join-Path $vanillaRoot.FullName 'SandBox\Issues')
)
$outputPath = Join-Path $repoRoot 'docs\vanilla_issue_audit.csv'

$files = Get-ChildItem $roots -Recurse -File | Where-Object {
    $_.Name -like '*IssueBehavior.cs' -or $_.Name -like '*IssueQuestBehavior.cs'
} | Sort-Object FullName

$rows = foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Encoding UTF8 -Raw
    $relativePath = $file.FullName.Substring($repoRoot.Length + 1)
    $issueClasses = ([regex]::Matches($text, 'class\s+([A-Za-z0-9_]+Issue)\b') | ForEach-Object {
        $_.Groups[1].Value
    } | Select-Object -Unique) -join '; '
    $questClasses = ([regex]::Matches($text, 'class\s+([A-Za-z0-9_]+Quest)\b') | ForEach-Object {
        $_.Groups[1].Value
    } | Select-Object -Unique) -join '; '
    $hasBaseQuestSuccess = [bool]($text -match 'base\.CompleteQuestWithSuccess\(')
    $hasDirectQuestSuccess = [bool]($text -match '(?<!base\.)\bCompleteQuestWithSuccess\(')
    $goldCalls = ([regex]::Matches($text, 'GiveGoldAction\.ApplyBetweenCharacters').Count)
    $relationCalls = ([regex]::Matches($text, 'ChangeRelationAction\.ApplyPlayerRelation').Count)
    $relPropSets = ([regex]::Matches($text, 'RelationshipChangeWithQuestGiver\s*=').Count)
    $hasAlternative = [bool]($text -match 'AlternativeSolution')
    $hasDiscussFlow = [bool]($text -match 'DiscussDialogFlow')
    $hasOfferFlow = [bool]($text -match 'OfferDialogFlow')
    $kind = if ($file.Name -like '*IssueQuestBehavior.cs') { 'QuestOnly' } else { 'Primary' }
    $risk = if ($hasDirectQuestSuccess -or -not $hasBaseQuestSuccess -or $goldCalls -gt 1 -or $relationCalls -ge 5) {
        'High'
    }
    elseif ($goldCalls -ge 1 -or $relationCalls -ge 1 -or $relPropSets -ge 1) {
        'Medium'
    }
    else {
        'Low'
    }
    $notes = New-Object System.Collections.Generic.List[string]
    if (-not $hasBaseQuestSuccess) {
        $notes.Add('NoBaseSuccess')
    }
    if ($hasDirectQuestSuccess) {
        $notes.Add('DirectSuccessWrapper')
    }
    if ($goldCalls -gt 0) {
        $notes.Add("Gold=$goldCalls")
    }
    if ($relationCalls -gt 0) {
        $notes.Add("RelCalls=$relationCalls")
    }
    if ($relPropSets -gt 0) {
        $notes.Add("RelProp=$relPropSets")
    }

    [pscustomobject]@{
        Kind                  = $kind
        Risk                  = $risk
        File                  = $relativePath
        IssueClasses          = $issueClasses
        QuestClasses          = $questClasses
        HasBaseQuestSuccess   = $hasBaseQuestSuccess
        HasDirectQuestSuccess = $hasDirectQuestSuccess
        GoldCalls             = $goldCalls
        RelationCalls         = $relationCalls
        RelPropSets           = $relPropSets
        HasAlternative        = $hasAlternative
        HasDiscussFlow        = $hasDiscussFlow
        HasOfferFlow          = $hasOfferFlow
        Notes                 = ($notes -join '|')
    }
}

$outputDir = Split-Path -Parent $outputPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$rows | Sort-Object Risk, File | Export-Csv -LiteralPath $outputPath -NoTypeInformation -Encoding UTF8

$summary = $rows | Group-Object Risk | Sort-Object Name | ForEach-Object {
    '{0}={1}' -f $_.Name, $_.Count
}

Write-Host ('Exported {0} rows to {1}' -f $rows.Count, $outputPath)
Write-Host ($summary -join ', ')
