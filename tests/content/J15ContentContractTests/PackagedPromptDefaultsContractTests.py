"""Packaged/default/user-override wiring; no model request or disk mutation."""
from pathlib import Path
import json
import re
import sys

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/localization'))
from inventory_untranslated import decode,HAN
settings=(ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs').read_text(encoding='utf-8-sig')
defaults=(ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.PackagedPromptDefaults.cs').read_text(encoding='utf-8-sig')
mapping=json.loads((ROOT/'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
checks=0
def check(ok,label):
    global checks
    checks+=1
    if not ok:raise AssertionError(label)
specs={'KingdomRebellionSystemPrompt':'AF.Module.Kingdom','WeeklyReportWritingRequirements':'AF.Module.Weekly',
       'DailyMemoryCompressionWritingRequirements':'AF.Module.Memory','MemoryOverviewCompressionWritingRequirements':'AF.Module.Memory',
       'MajorActionCompressionWritingRequirements':'AF.Module.Memory'}
calls=dict((m[1],decode(m[2])) for m in re.finditer(r'ReadPackagedPromptDefault\((\w+)JsonFileName, ("(?:\\.|[^"\\])*")\)',defaults))
check(set(calls)==set(specs),'exact intended defaults migrated')
for name,module in specs.items():
    check('private const string Default'+name+' =' not in settings,name+' no longer hardcodes a Chinese default')
    check(f'private static string Default{name} => Packaged{name}Default.Value;' in defaults,name+' cached access')
    check(f'private static readonly Lazy<string> Packaged{name}Default' in defaults,name+' one initialization')
    check(f'{name} = Default{name},' in settings,name+' remains in default-store construction')
    asset=f'content/modules/{module}/CustomPrompts/{name}.json'
    cn=json.loads((ROOT/asset).read_text(encoding='utf-8-sig'))
    en=json.loads((ROOT/asset.replace('.json','.en.json')).read_text(encoding='utf-8-sig'))
    check(calls[name]==en['Text'] and not HAN.search(calls[name]),name+' complete English fallback matches asset')
    check(any(e['source']==asset and e['target']=='ModuleData/Defaults/CustomPrompts/'+name+'.json' for e in mapping),name+' canonical package target')
    for token in re.findall(r'\[(?:REPORT|SHORT)\]|\bSUMMARY\b',cn['Text']):
        check(token in calls[name],name+' output protocol '+token)
check(defaults.count('LazyThreadSafetyMode.ExecutionAndPublication')==5,'thread-safe one-time caches')
check('TryReadCustomPromptTextJsonFile(path, null, englishFallback, out string text)' in defaults,
      'existing strict UTF-8/size/reparse/schema reader reused')
check('return text;' in defaults and 'return englishFallback;' in defaults,'empty valid defaults preserved; missing/corrupt data has full fallback')
check('GetPackagedCustomPromptTextStoreDirectory()' in defaults and 'GetCustomPromptTextStoreDirectory()' not in defaults,
      'packaged default cache never captures user overrides')
start=settings.index('private static bool TryReadLayeredCustomPromptTextJsonFile(')
end=settings.index('private static bool IsCustomPromptTextFileTooLarge',start)
layer=settings[start:end]
check(layer.index('TryReadCustomPromptTextJsonFile(GetCustomPromptUserReadPath(overridePath)')<layer.index('TryReadCustomPromptTextJsonFile(packagedPath'),
      'user overrides still have priority')
check('File.Write' not in defaults and 'ChangeLanguage' not in defaults and 'GetLanguage' not in defaults,
      'no implicit migration/write/language detection')
source_defaults=json.loads((ROOT/'content/foundation/AF.Foundation.Localization/ModuleData/McmPromptSourceCompatibility.json').read_text(encoding='utf-8'))
active_cn=json.loads((ROOT/'content/foundation/AF.Foundation.Localization/ModuleData/McmPromptDefaults.zh-CN.json').read_text(encoding='utf-8'))
check(active_cn['DefaultPlayerCustomPromptRule']==source_defaults['DefaultPlayerCustomPromptRule']
      and 'SourceDefaultPlayerCustomPromptRule' in settings
      and 'McmPromptDefaults.Get("DefaultPlayerCustomPromptRule")' in settings,
      'distinct player-rule source default preserved and connected to content package')
print(f'PASS packaged prompt defaults static contract: {checks} checks; current product compilation NOT RUN')
