"""Reviewed localization inverses only; never a replacement behavioral implementation."""
import json
import re
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]

def restore_conversation_resources(source):
    base = ROOT / 'content/modules/AF.Module.Conversation/ModuleData'
    raw = json.loads((base / 'ConversationRuntimeCompatibility.json').read_text(encoding='utf-8'))
    cn = json.loads((base / 'ConversationRuntimePrompts.json').read_text(encoding='utf-8'))
    en = json.loads((base / 'ConversationRuntimePrompts.en.json').read_text(encoding='utf-8'))
    def restore(match):
        method, key = match.groups()
        values = raw if method == 'Raw' else cn
        assert key in values, key
        if method == 'Prompt': assert key in en, key
        return json.dumps(values[key], ensure_ascii=False)
    return re.sub(r'ConversationRuntimeTexts\.(Raw|Prompt)\("([^"\n]+)"\)', restore, source)

def restore_llm_resources(source):
    values = json.loads((ROOT / 'content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json').read_text(encoding='utf-8'))
    def restore(match):
        key = match[1]
        assert key in values, key
        return json.dumps(values[key], ensure_ascii=False)
    return re.sub(r'LlmSourceTexts\.Get\("([^"\n]+)"\)', restore, source)


def restore_scene_receipts(source, system=False):
    """Invert only reviewed receipt/layout/source-scope changes; original oracle remains intact."""
    var = 'aiResponse' if system else 'content'
    declaration = 'var diplomacyReceipts = new List<string>();'
    assert source.count(declaration) == 1
    source = source.replace(declaration, '', 1)
    dispatch = 'ref ' + var + ', diplomacyReceipts)'
    assert source.count(dispatch) == 1
    source = source.replace(dispatch, 'ref ' + var + ')', 1)
    visible = 'SanitizeSceneSpeechText(DiplomacyConversationBridge.ComposeReply(' + var + ', diplomacyReceipts, forUi: true))'
    assert source.count(visible) == 1
    source = source.replace(visible, var if system else 'SanitizeSceneSpeechText(content)', 1)
    raw = 'DiplomacyConversationBridge.ComposeReply(' + var + ', diplomacyReceipts)'
    assert source.count(raw) == (3 if system else 2)
    source = source.replace(raw, var)
    source = source.replace('(!string.IsNullOrWhiteSpace(' + var + ') || diplomacyReceipts.Count > 0)', '!string.IsNullOrWhiteSpace(' + var + ')')
    source = source.replace('(string.IsNullOrWhiteSpace(' + var + ') && diplomacyReceipts.Count == 0)', 'string.IsNullOrWhiteSpace(' + var + ')')
    source = source.replace('(string.IsNullOrWhiteSpace(SanitizeSceneSpeechText(' + var + ')) && diplomacyReceipts.Count == 0)', 'string.IsNullOrWhiteSpace(SanitizeSceneSpeechText(' + var + '))')
    assert 'diplomacyReceipts' not in source
    # Recorded source is independent of UI text and does not execute an action.
    scope = re.compile(r'using var diplomacySource = DiplomacyDialogueSourceScope.Begin\([^;]+;' if system else r'using var diplomacySource = item\.DiplomacyOrigin == null \? null : new DiplomacyDialogueSourceScope\(item\.DiplomacyOrigin\);')
    assert len(scope.findall(source)) == 1
    source = scope.sub('', source)
    # Reviewed bilingual failure-color gate: original Chinese match is retained.
    source, color_count = re.subn(r'\((notification\d*)\.IndexOf\("失败", StringComparison\.OrdinalIgnoreCase\) >= 0 \|\| \1\.IndexOf\("fail", StringComparison\.OrdinalIgnoreCase\) >= 0\)', r'\1.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0', source)
    assert color_count == 1
    return source
