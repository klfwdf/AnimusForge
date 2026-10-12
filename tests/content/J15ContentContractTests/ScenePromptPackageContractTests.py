"""Package data and production route checks. Pure Python; does not run C#."""
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
MODULE = ROOT / 'extensions/AnimusForge.XihaiAction/src'
CONTENT = ROOT / 'content/modules/AnimusForge.XihaiAction/ModuleData'
checks = 0

def check(value, label):
    global checks
    checks += 1
    assert value, label

def read(relative):
    return (MODULE / relative).read_text(encoding='utf-8-sig')

def entries(path):
    nodes = ET.parse(path).getroot()
    values = {n.attrib['id']: n.attrib['text'] for n in nodes.iter('string')}
    check(len(values) == len(list(nodes.iter('string'))), str(path) + ' unique keys')
    return nodes.attrib, values

packs = {}
for lang, suffix in [('zh-CN', ''), ('en', '.en')]:
    attributes, values = entries(CONTENT / ('SceneActionPromptTexts' + suffix + '.xml'))
    packs[lang] = values
    check(attributes['language'] == lang, 'package language explicit')
    for section in ('catalog', 'speech'):
        source = CONTENT / 'Languages' / (f'afscene_{section}_strings.xml' if lang == 'en'
                   else f'CNs/afscene_{section}_strings-zh-CN.xml')
        _, originals = entries(source)
        neutralized = {'AF_SCENE_SPEECH_Prompt_Writing_10', 'AF_SCENE_SPEECH_Prompt_Combined_20'} if lang == 'zh-CN' else set()
        check(all(values[k] == v for k, v in originals.items() if k not in neutralized), lang + '/' + section + ' exact source values except two approved neutral-language rules')
    check(len(values) == 221, '215 existing entries plus six runtime instructions')
check(packs['en'].keys() == packs['zh-CN'].keys(), 'same keys in both editions')
check(not re.search(r'[\u3400-\u9fff]', ''.join(packs['en'].values())), 'English package no Han')
for key in packs['en']:
    check(set(re.findall(r'\{[A-Z_]+\}', packs['en'][key])) ==
          set(re.findall(r'\{[A-Z_]+\}', packs['zh-CN'][key])), key + ' placeholder parity')

original = subprocess.check_output(['git', 'show', 'ead5c8841:extensions/AnimusForge.XihaiAction/src/Runtime/SceneActionsRuntimeHost.cs'], cwd=ROOT).decode('utf-8')
start = original.index('            return "【自然语言动作】')
native = ''.join(re.findall(r'"([^"\n]*)"', original[start:original.index(';', start)]))
check(packs['zh-CN']['AF_SCENE_PACKAGE_NativeConversationActionInstruction'] == native, 'native Chinese instruction exact migration')

package = read('CoreProject/SceneActionPromptPackage.cs')
check('TaleWorlds' not in package and 'TextObject' not in package, 'Core no engine language dependency')
check('private static volatile Corpus _corpus' in package and package.count('if (_corpus != null) return;') == 2,
      'published once, subsequent loads perform no IO')
check('XmlReader.Create(filePath' in package and 'DtdProcessing.Prohibit' in package, 'actual package file read')
for name, prefix in [('SceneActionCatalogText', 'AF_SCENE_CATALOG_'), ('BattleSpeechText', 'AF_SCENE_SPEECH_')]:
    source = read('CoreProject/' + name + '.cs')
    check('SceneActionPromptPackage.ReadSection("' + prefix + '")' in source and
          'useChinese == SceneActionPromptPackage.UseChinese' in source, name + ' selected corpus consumer')
for host in ('SceneActionsRuntimeHost', 'BattleSpeechRuntimeHost'):
    source = read('Runtime/' + host + '.cs')
    check('SceneActionPromptPackage.Load(' in source and '"SceneActionPromptTexts.xml"' in source, host + ' initialize package')
adapter = read('Runtime/AfCompatV130.cs')
ui = read('Runtime/SceneActionsText.cs')
check('bool useChinese = SceneActionPromptPackage.UseChinese;' in adapter, 'classifier package capture')
check('SceneActionPromptPackage.UseChinese' in ui and 'AF_RUNTIME_SceneActions_Language' not in ui + adapter,
      'speech session package capture without UI marker')
controller = (ROOT / 'src/AF.GameAdapter.Bannerlord/SceneActions/SceneActionDirectiveController.cs').read_text(encoding='utf-8-sig')
check('SceneActionPromptPackage.Load(AnimusForgeModulePaths.GetModuleDataFilePath("SceneActionPromptTexts.xml"))' in controller,
      'unified postprocess loads same corpus')
core = read('CoreProject/BattleSpeechFrameworkV2.cs')
check('text.Any(char.IsLetter)' in core and '"non_chinese"' not in core and '"non_english"' not in core,
      'speech acceptance is independent of package language')
for text in ('守住阵线！', 'Hold the line!', 'Держите строй!', 'Tenez la ligne !', '混合 mixed speech'):
    check(any(c.isalpha() for c in text), 'natural-letter simulation: ' + text)
for text in ('12345', '!?', ''):
    check(not any(c.isalpha() for c in text), 'non-speech-letter simulation: ' + text)
check('SpeechProtocolOnlyPattern.IsMatch(text)' in core and 'SpeechProtocolOnlyPattern.IsMatch(reply)' in core,
      'protocol rejection unchanged at both boundaries')
check('@"[^。！？.!?；;]+[。！？.!?；;]"' in core, 'both punctuation sets independent of corpus')
check(packs['zh-CN']['AF_SCENE_SPEECH_Prompt_Writing_10'] == '- 正文使用自然、单行连续口语，建议' and packs['zh-CN']['AF_SCENE_SPEECH_Prompt_Combined_20'] == '- 字段标记保持英文大写。', 'CN package no forced output language')
check('BattleSpeechText.ContainsTaskInstruction(' in read('Runtime/BattleSpeechRuntimeHost.cs'), 'speech scope recognizes actual resource instructions')
check('string battleSpeechPreamble =' in adapter and 'SceneActionPromptPackage.Text("BattleSpeechPreamble")' in adapter, 'specialized preamble consumes package')
for values in packs.values():
    for header, body in [('Prompt_Single_03', 'Prompt_Single_04'), ('Prompt_Combined_12', 'Prompt_Combined_13')]:
        marker = values['AF_SCENE_SPEECH_' + header] + values['AF_SCENE_SPEECH_' + body]
        check(marker in 'context\n' + marker + 'rules', 'actual single/combined instruction marker simulation')
        check(marker not in 'ordinary conversation', 'ordinary text excluded')
print(f'PASS scene package prompt contract: {checks} static/data/simulation checks; compiled/runtime/package NOT RUN')
