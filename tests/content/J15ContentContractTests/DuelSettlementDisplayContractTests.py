"""Static settlement-display/authoritative-history separation contract."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[3]
source=(ROOT/'src/modules/AF.Module.Duel/Host/DuelBehavior.cs').read_text(encoding='utf-8-sig')
ui=ROOT/'content/modules/AF.Module.UI/ModuleData/Languages'
en={n.get('id'):n.get('text','') for n in ET.parse(ui/'af_ui_strings.xml').getroot().iter('string')}
zh={n.get('id'):n.get('text','') for n in ET.parse(ui/'CNs/af_ui_strings-zh-CN.xml').getroot().iter('string')}
keys=set(re.findall(r'AF_UI_Duel_(?:Stake|Renown)[A-Za-z0-9_]+',source))
checks=0
def check(ok,label):
    global checks
    checks+=1
    if not ok:raise AssertionError(label)
check(len(keys)==15,'all 15 settlement keys are audited')
for key in keys:
    check(key in en and key in zh,key+' requires both resources')
    check(set(re.findall(r'\{\w+\}',en[key]))==set(re.findall(r'\{\w+\}',zh[key])),key+' variables match')
start=source.index('private static string ApplyDuelStakeSettlementAndBuildResultText(')
stop=source.index('\n\tprivate ',start+1)
body=source[start:stop]
check(body.count('RecordDeferredDuelDebtForNpc(')==2,'two existing mutually exclusive debt branches')
check(body.count('out var dueStatusText, out var dueStatusDisplayText)')==2,'both branches obtain raw and display from one debt')
check(body.count('MyBehavior.AppendExternalDialogueHistory(')==8,'both participants retain the four existing history paths')
check('string text8 = BuildStakeSummaryText(num, dictionary);' in body,
      'history retains raw stake summary')
check('BuildDuelStakeSummaryDisplayText(num, dictionary)' in body,
      'HUD uses independent stake summary')
check('string.Join("，", list)' in body and 'JoinDuelStakeDisplayText(paidDisplay)' in body,
      'raw and display lists are independent')
check('outcomeState = transferUnknown' in body and 'DuelOutcomeEffectState.Confirmed' in body and
      'DuelOutcomeEffectState.Partial' in body,'settlement classification retained')
helper=source[source.index('private static string BuildDuelStakeDebtDisplayText('):source.index('private static string ApplyDuelRenownPenaltyAndBuildResultText(')]
check('dueStatusDisplayText' in helper and '.SetTextVariable("DUE", dueSuffix)' in helper,
      'HUD deadline is projected')
check('RecordDeferred' not in helper and 'Transfer' not in helper and 'AppendExternalDialogueHistory' not in helper,
      'display helper performs no business effects')
check('paidDisplay.Add(BuildDuelStakeItemDisplayText(text3, num4));' in body and
      'unpaidDisplay.Add(BuildDuelStakeItemDisplayText(text3, num3 - num4));' in body,
      'partial transfers display actual transferred and outstanding counts')
print(f'PASS duel settlement display static contract: {checks} checks; compiled/game replay NOT RUN')
