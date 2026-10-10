"""Terminal display fallback coverage; routing and setting delegates are not translated."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/localization'))
from inventory_untranslated import decode, HAN
from resource_catalog import registered_pairs

source=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs').read_text(encoding='utf-8-sig')
helper=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsLocalization.cs').read_text(encoding='utf-8-sig')
mapped=set(re.findall(r'"(\w+)"',helper.split('EnglishTitles',1)[0]))
en={n.get('id'):n.get('text','') for e,z in registered_pairs(ROOT) for n in ET.parse(e).getroot().iter('string')}
zh={n.get('id'):n.get('text','') for e,z in registered_pairs(ROOT) for n in ET.parse(z).getroot().iter('string')}
string=r'"(?:\\.|[^"\\])*"'
group=r'(?:'+string+r'|DuelSettings\.NewsSettingsGroup)'
category=r'(?:'+string+r'|TerminalCategories\.\w+)'
pattern=re.compile(r'new TerminalSettingDef\(('+string+r'),\s*('+category+r'),\s*('+group+r'),\s*('+string+r'),\s*('+string+r')')
rows=list(pattern.finditer(source))
assert len(rows)==source.count('new TerminalSettingDef('), 'unreviewed registry definition syntax'
ids=[decode(m[1]) for m in rows]
assert len(set(ids))==len(ids) and len(ids)>=250
checks=2
for m,identity in zip(rows,ids):
    assert identity in mapped,identity
    checks+=1
    for pos,part in ((3,'Group'),(4,'Title'),(5,'Hint')):
        if m[pos]=='DuelSettings.NewsSettingsGroup':continue
        value=decode(m[pos])
        assert not HAN.search(value), identity+'/'+part+' still hardcodes Chinese'
        checks+=1
        if not value:continue
        key='AF_TERMINAL_UI_'+identity+'_'+part
        assert key in en and key in zh,key
        assert value==en[key],key+' fallback drift'
        checks+=2
for expr in ('TerminalSettingsLocalization.Group(Id, groupName ?? "")',
             'TerminalSettingsLocalization.Title(Id, displayName ?? "")',
             'TerminalSettingsLocalization.Hint(Id, hintText ?? "")'):
    assert expr in source,expr
    checks+=1
assert 'new TextObject($"{{=AF_TERMINAL_UI_{id}_{part}}}{englishFallback}").ToString()' in helper
print(f'PASS terminal display fallback contract: definitions={len(rows)} checks={checks+1}; category aliases remain separately tracked')
