"""Exact legacy text-rule migration plus package-selected decision descriptions."""
from pathlib import Path
import json,re,subprocess,sys
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
def restore(m):
    key=m[2]; seen.append(key)
    if m[1]=='localize':
        assert compat[key]==zh[key] and decode(m[3])==en[key]
        assert not HAN.search(en[key])
    else: assert decode(m[3])==''
    return "'"+compat[key]+"'" if m[4] else json.dumps(compat[key],ensure_ascii=False)
restored=re.sub(r'WorldDiplomacyPromptContractRules.ContractText\((localize|null), "(AF_DIP_(?:Fact|Source)_\d+)", ("(?:\\.|[^"\\])*")\)(\[0\])?',restore,source)
names=['BuildBilateralStateLabel','DescribePeacePressure','DescribeStrengthBalance','DescribeWarProgress','DescribeOtherWarBurden','DescribeRulerRelation','DescribeRealmRelationProfile','DescribeWarDuration','DescribeWarPressure','DescribeBorderRelation']
for name in names:
    restored,count=re.subn(r'public static string '+name+r'\([^\n]*\)\n\t\t=> [^\n]+;\n\n\t','',restored)
    assert count==1,name
restored=restored.replace(', Func<string, string, string> localize)',')')
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
print('PASS diplomacy text rules: 262 original literals preserved, 50 translated descriptions, 17 prompt consumers, 10 original signatures, Han literals=0; C# NOT RUN')
