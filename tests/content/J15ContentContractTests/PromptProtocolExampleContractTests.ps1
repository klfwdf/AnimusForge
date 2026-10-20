param(
    [Parameter(Mandatory=$true)][string]$ProjectRoot,
    [Parameter(Mandatory=$true)][string]$RunRoot
)
$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts/build/content_layout.ps1')
. (Join-Path $ProjectRoot 'scripts/build/package_localization.ps1')
Assert-AnimusForgePathUnderRoot -Path $RunRoot -Root (Join-Path $ProjectRoot 'artifacts/tests') -Label 'Protocol example replay'
if (Test-Path -LiteralPath $RunRoot) { throw 'Use a new evidence directory.' }
New-Item -ItemType Directory -Path $RunRoot | Out-Null
$checks = 0
$utf8 = [Text.UTF8Encoding]::new($false)
function Check([bool]$ok, [string]$label) { if (-not $ok) { throw $label }; $script:checks++ }
function Reject([scriptblock]$action, [string]$label, [string]$message) {
    $errorText = ''
    try { & $action | Out-Null } catch { $errorText = $_.Exception.Message }
    Check ($errorText.Contains($message)) ($label + ': ' + $errorText)
}
$source = Join-Path $ProjectRoot 'content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts.json'
$englishSource = Join-Path $ProjectRoot 'content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts.en.json'
$cn = Join-Path $RunRoot 'RuleBehaviorPrompts.json'
$en = Join-Path $RunRoot 'RuleBehaviorPrompts.en.json'
$beforeHash = (Get-FileHash -LiteralPath $source).Hash + (Get-FileHash -LiteralPath $englishSource).Hash
Copy-Item -LiteralPath $source -Destination $cn
Copy-Item -LiteralPath $englishSource -Destination $en
Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en
Check $true 'Actual CN/EN rule templates preserve the reviewed protocol schemas.'
function Mutate([scriptblock]$change) {
    $value = Get-Content -LiteralPath $englishSource -Raw -Encoding UTF8 | ConvertFrom-Json
    & $change $value
    [IO.File]::WriteAllText($en, ($value | ConvertTo-Json -Depth 100), $utf8)
}
Mutate { param($v) $v.Duel.PostprocessRules[1].Tag = '[AD:value:days:P/N:note]' }
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Debt direction specimen cannot change' 'Prompt variable/protocol changed:'
Mutate { param($v) $v.Duel.PostprocessRules[1].Tag = '[AD:value:days:N/P]' }
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Debt field cannot be dropped' 'Prompt variable/protocol changed:'
Mutate { param($v) $v.RulePrompts[12].PostprocessRules[0].Tag = $v.RulePrompts[12].PostprocessRules[0].Tag.Replace('action=Peace', 'action=War') }
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Commit action remains exact' 'Prompt variable/protocol changed:'
Mutate { param($v) $v.RulePrompts[13].PostprocessRules[0].Tag = $v.RulePrompts[13].PostprocessRules[0].Tag.Replace('receiving=', 'recipient=') }
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Commit parameter names remain exact' 'Prompt variable/protocol changed:'
Mutate { param($v) $v.RulePrompts[0].Id = $v.RulePrompts[0].Id + '_changed' }
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Stable identity remains exact' 'Stable JSON identity changed:'
function Tiny([string]$left, [string]$right) {
    [IO.File]::WriteAllText($cn, $left, $utf8)
    [IO.File]::WriteAllText($en, $right, $utf8)
}
Tiny '{"Text":"{0} {VALUE} [ACTION:DUEL:1]"}' '{"Text":"{1} {VALUE} [ACTION:DUEL:1]"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Numeric variables remain exact' 'Prompt variable/protocol changed:'
Tiny '{"Text":"{0} {VALUE} [ACTION:DUEL:1]"}' '{"Text":"{0} {VALUE} [ACTION:DUEL:2]"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Executable values remain exact' 'Prompt variable/protocol changed:'
$specimen = [regex]::Unescape('[AD:\u4ef7\u503c:\u5929\u6570:N/P:\u5907\u6ce8\u5185\u5bb9]')
Tiny ('{"Text":"' + $specimen + '"}') '{"Text":"[AD:value:days:N/P:note]"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Reviewed example is not allowed at an unrelated path' 'Prompt variable/protocol changed:'
$unknown = [regex]::Unescape('[AD:\u53e6\u503c:\u53e6\u5929:N/P:\u5907\u6ce8]')
Tiny ('{"Duel":{"PostprocessRules":[{}, {"Tag":"' + $unknown + '"}]}}') '{"Duel":{"PostprocessRules":[{}, {"Tag":"[AD:value:days:N/P:note]"}]}}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Unknown specimen does not receive a generic schema exemption' 'Prompt variable/protocol changed:'
$marker = [regex]::Unescape('[AFEF NPC\u884c\u4e3a\u8865\u5145]')
Tiny ('{"Text":"' + $marker + ' example"}') '{"Text":"[AFEF NPC-action fact] example"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'AFEF identity is never translated' 'Prompt variable/protocol changed:'
$composerCn = Join-Path $ProjectRoot 'content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.zh-CN.json'
$composerEn = Join-Path $ProjectRoot 'content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.en.json'
Assert-AnimusForgeEnglishJsonContract -OriginalPath $composerCn -EnglishPath $composerEn
Check $true 'Actual composer templates preserve known CN/EN fact identities.'
$sceneSpecimen = [regex]::Unescape('[ACTION:SCENE_ACT:\u52a8\u4f5c\u7a0b\u5e8f]')
Tiny ('{"Text":"' + $sceneSpecimen + '"}') '{"Text":"[ACTION:SCENE_ACT:action_sequence]"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Composer specimen is not allowed at an unrelated path' 'Prompt variable/protocol changed:'
$composerValue = Get-Content -LiteralPath $composerEn -Raw -Encoding UTF8 | ConvertFrom-Json
$composerValue.scene_action_rule_template = '[ACTION:SCENE_ACT:wrong_sequence]'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $composerCn -EnglishPath $composerEn -EnglishValue $composerValue } 'Composer action parameter specimen remains exact' 'Prompt variable/protocol changed:'
Tiny ('{"Text":"' + $marker + ' {0}"}') '{"Text":"[AFEF NPC_ACTION] {0}"}'
Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en
Check $true 'Known NPC fact marker projects to its exact runtime English identity.'
Tiny ('{"Text":"' + $marker + ' {0}"}') '{"Text":"[AFEF PLAYER_ACTION] {0}"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'NPC facts cannot become player facts' 'Prompt variable/protocol changed:'
Tiny ('{"Text":"' + $marker + ' {0}"}') '{"Text":"{0}"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Fact marker cannot disappear' 'Prompt variable/protocol changed:'
$playerMarker = [regex]::Unescape('[AFEF\u73a9\u5bb6\u884c\u4e3a\u8865\u5145]')
Tiny ('{"Text":"' + $playerMarker + ' {0}"}') '{"Text":"[AFEF PLAYER_ACTION] {0}"}'
Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en
Check $true 'Known player fact marker projects to its exact runtime English identity.'
Tiny ('{"Text":"' + $playerMarker + ' {0}"}') '{"Text":"[AFEF NPC_ACTION] {0}"}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $cn -EnglishPath $en } 'Player facts cannot become NPC facts' 'Prompt variable/protocol changed:'
Check ($beforeHash -ceq ((Get-FileHash -LiteralPath $source).Hash + (Get-FileHash -LiteralPath $englishSource).Hash)) 'Production sources unchanged by contract replay'
Write-Output "PASS protocol examples: $checks checks; exact specimen/path exemptions and strict variable/identity/action/AFEF negative controls."
