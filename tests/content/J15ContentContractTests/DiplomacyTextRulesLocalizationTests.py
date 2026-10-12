"""Exact legacy text-rule migration plus package-selected decision descriptions."""
from pathlib import Path
import json,re,subprocess,sys
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/localization'))
from inventory_untranslated import LEX,decode,HAN
base=ROOT/'content/modules/AF.Module.Diplomacy/ModuleData'
en=json.loads((base/'DiplomacyPromptTexts.en.json').read_text(encoding='utf-8'))
zh=json.loads((base/'DiplomacyPromptTexts.json').read_text(encoding='utf-8'))
compat=json.loads((base/'DiplomacyPromptCompatibility.json').read_text(encoding='utf-8'))
path='src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyTextRules.cs'
source=(ROOT/path).read_text(encoding='utf-8-sig')
old=subprocess.check_output(['git','show','183542485:'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
seen=[]
ui_en={n.get("id"):n.get("text") for n in ET.parse(base/"Languages/afdip_ui_strings.xml").iter("string")}
ui_zh={n.get("id"):n.get("text") for n in ET.parse(base/"Languages/CNs/afdip_ui_strings-zh-CN.xml").iter("string")}
def restore(m):
    key=m[2]; seen.append(key)
    if m[1]=='localize':
        cn = zh.get(key, ui_zh.get(key))
        english = en.get(key, ui_en.get(key))
        assert compat[key] == cn, key
        if decode(m[3]): assert decode(m[3]) == english, key
        else: assert key in en and key in zh, key + ": empty fallback must resolve a real package asset"
        assert not HAN.search(english), key
    else: assert decode(m[3])==''
    return "'"+compat[key]+"'" if m[4] else json.dumps(compat[key],ensure_ascii=False)
restored=re.sub(r'WorldDiplomacyPromptContractRules.ContractText\((localize|null), "(AF_DIP_(?:Fact|Source)_\d+)", ("(?:\\.|[^"\\])*")\)(\[0\])?',restore,source)
names=['BuildBilateralStateLabel','DescribePeacePressure','DescribeStrengthBalance','DescribeWarProgress','DescribeOtherWarBurden','DescribeRulerRelation','DescribeRealmRelationProfile','DescribeWarDuration','DescribeWarPressure','DescribeBorderRelation']
for name in names:
    restored,count=re.subn(r'public static string '+name+r'\([^\n]*\)\n\t\t=> [^\n]+;\n\n\t','',restored)
    assert count==1,name
restored=restored.replace(', Func<string, string, string> localize)',')').replace(', Func<string, string, string> localize = null)',')')
restored=restored.replace(', localize)', ')')
separator=' + (localize == null ? "" : localize("FallbackDocumentTitleTargetSeparator", ": "))'
assert restored.count(separator)==1
restored=restored.replace(separator, '')
# Package separators and battle type projection are display/request projections;
# the no-resolver signatures still return original facts and identities.
list_separator='localize == null ? "、" : localize("PromptListSeparator", ", ")'
assert restored.count(list_separator)==1 and zh['PromptListSeparator']=='、' and en['PromptListSeparator']==', '
restored=restored.replace(list_separator, '"、"')
assert 'if (localize == null) return type;' in source
for key, english in (('BattleTypeSiege', 'Siege battle'), ('BattleTypeRaid', 'Raid battle'), ('BattleTypeField', 'Field battle')):
    assert compat[key] == zh[key] and en[key] == english
assert 'string.Equals(type, WorldDiplomacyPromptContractRules.ContractText(null, key, ""), StringComparison.Ordinal)' in source
assert restored.count('ProjectBattleTypeForPrompt(fact?.BattleType)')==1
restored=restored.replace('ProjectBattleTypeForPrompt(fact?.BattleType)', 'fact?.BattleType')
start=restored.index('    private static readonly string[] BattleTypePromptKeys')
end=restored.index('public static string BuildRoundCompressionPrompt',start)
assert 'return type;' in restored[start:end] and 'foreach (string key in BattleTypePromptKeys)' in restored[start:end]
restored=restored[:start]+restored[end:]
restored=restored.replace('\n\npublic static string BuildRoundCompressionPrompt', '\npublic static string BuildRoundCompressionPrompt',1)

def normalize(s):
    return LEX.sub(lambda m:json.dumps(decode(m[0]),ensure_ascii=False) if m.lastgroup=='string' else m[0],s)
assert normalize(restored)==normalize(old),'all regexes, thresholds, facts, formatting, unknown handling and char values preserved'
assert len(seen)==262
assert len([k for k in en if k.startswith('AF_DIP_Fact_')])==50
assert not any(HAN.search(decode(m[0]) if m.lastgroup=='string' else m[0][1:-1]) for m in LEX.finditer(source) if m.lastgroup in ('string','char'))
composer=(ROOT/'src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPromptComposer.cs').read_text(encoding='utf-8-sig')
masked=LEX.sub(lambda m:' '*len(m[0]),composer)
calls=0
for name in names:
    for m in re.finditer(r'WorldDiplomacyTextRules\.'+name+r'\(',composer):
        d=1;b=m.end()
        while d:d+=(masked[b]=='(')-(masked[b]==')');b+=1
        assert composer[m.start():b].endswith(', world.LocalizePrompt)'),name
        calls+=1
assert calls==17
for number in range(72,118):
    key=f'AF_DIP_Source_{number:03}'
    if number==96: continue # stable unknown target identity, intentionally raw
    assert key in zh and key in en and zh[key]==compat[key], key
assert zh['FallbackDocumentTitleTargetSeparator']=='' and en['FallbackDocumentTitleTargetSeparator']==': '
assert 'BuildFallbackDocumentTitle(document, "statement", DiplomacyPromptTexts.Get)' in composer

print('PASS diplomacy text rules: 262 original literals preserved, 50 translated descriptions, 17 prompt consumers, 10 original signatures, Han literals=0; C# NOT RUN')
