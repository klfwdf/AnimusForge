"""Static debt presentation boundary, save-field and calendar contract checks."""
from pathlib import Path
import re
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
def read(path): return (ROOT / path).read_text(encoding="utf-8-sig")
quest = read("src/modules/AF.Module.Economy/Host/DebtPromiseQuest.cs")
ledger = read("src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtLedger.cs")
reward = read("src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs")
ui = read("src/modules/AF.Module.Economy/Host/EconomyUiTexts.cs")
checks = 0
def check(condition, message):
    global checks
    checks += 1
    if not condition: raise AssertionError(message)

fields = re.findall(r'\[SaveableField\((\d+)\)\]\s*private (\w+) (\w+);', quest)
check(len(fields) == 10 and [int(x[0]) for x in fields] == list(range(1, 11)), "saved field identities remain 1..10")
check('private const int CurrentJournalFormatVersion = 8;' in quest, "one-time presentation migration version")
check('DebtPromiseCounterpartyForDisplay(_debtorName)' in quest and 'DebtPromiseNoteForDisplay(_debtNote)' in quest,
      "default journal values project at display boundary")
check('GetDisplayText(player?.Name?.ToString(), EconomyUiTexts.DebtPromisePlayer)' in quest,
      "missing player link gets localized display fallback")
source_values = json.loads(read('content/modules/AF.Module.Economy/ModuleData/EconomySourceTexts.json'))
check(source_values['S066'] == '对方', 'raw participant compatibility value is unchanged')
check('_debtorName = GetDisplayText(debtorName, EconomySourceTexts.Get("S066"));' in quest, "raw stored participant default uses the stable reader")
for method in ('DebtPromiseCounterpartyForDisplay', 'DebtPromiseNoteForDisplay'):
    start = ui.index('public static string ' + method)
    end = ui.index('\n\t}', start)
    check(': value;' in ui[start:end], method + " retains arbitrary user content")
start = ledger.index('public bool RecordDeferredDuelDebtForNpc(')
end = ledger.index('\n\tpublic ', ledger.index('private bool TryRecordDeferredDuelDebtForNpc(', start))
defer = ledger[start:end]
check(defer.count('SetDebtForNpc(') == 1, "both overloads use one authoritative debt creation")
check('out DebtRecord.DebtLine debtLine' in defer and 'BuildDebtEditorDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited)' in defer,
      "HUD uses the same debt line rather than recomputing a deadline")
check('dueStatusDisplayText = dueStatusText;' in defer, "display failure retains confirmed debt result")
check('dueStatusText = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) ?? "";' in defer,
      "raw deadline producer preserved")
for token in ('GetDaysInSeasonSafe()', 'GetDaysInYearSafe()', 'Math.Max(0, absDay)'):
    check(token in ledger[ledger.index('private static string FormatDebtDisplayDate'):], "display calendar uses " + token)
check('int season = NormalizeSeasonIndex(dayOfYear / daysInSeason);' in ledger and
      'int dayOfSeason = dayOfYear % daysInSeason + 1;' in ledger,
      "configured season/year and one-based day preserved")
check('string date = FormatDebtDisplayDate(ToDisplayDay(dueDay));' in ledger and
      'string text = FormatAbsDayAsGameDate(absDay);' in ledger, "display and raw dates have separate callers")
for path in ('afeconomy_strings.xml', 'CNs/afeconomy_strings-zh-CN.xml'):
    values = {n.get('id'): n.get('text', '') for n in ET.parse(ROOT / 'content/modules/AF.Module.Economy/ModuleData/Languages' / path).getroot().iter('string')}
    for suffix in ('DebtPromiseCounterparty', 'DebtPromiseNoNote', 'DebtPromisePlayer', 'DebtSeasonSpring',
                   'DebtSeasonSummer', 'DebtSeasonAutumn', 'DebtSeasonWinter', 'DebtCalendarDate'):
        check('AF_ECO_UI_' + suffix in values, path + '/' + suffix)
    check(set(re.findall(r'\{\w+\}', values['AF_ECO_UI_DebtCalendarDate'])) == {'{YEAR}', '{SEASON}', '{DAY}'},
          path + ' date placeholders')
print(f'PASS debt promise display static contract: {checks} checks; compiled/old-save replay NOT RUN')
