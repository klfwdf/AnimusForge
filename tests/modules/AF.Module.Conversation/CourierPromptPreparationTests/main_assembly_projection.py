"""Exact source inverse for the detached main-message extraction, not a refreshed hash."""
from pathlib import Path
import importlib.util, subprocess, re
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('message_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
BASELINE='d9d266c7'
PATH='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptMessages.cs'
HELPERS=['private static object CreateCourierChatMessage(', 'private static void AppendCourierRawUserSection(', 'private static void AppendCourierUserSection(', 'private static void AppendCourierPersistentMemoryRoleMessages(', 'private static bool TryConvertCourierMemoryMessageToChatMessage(', 'private static string BuildCourierMemoryMetadataPrefix(', 'private static string StripCourierPromptScopeLabel(', 'private static string StripCourierSpeakerPrefix(']
NAMES=['BuildCourierReplyMessages','BuildInboundNpcLetterMessages']
def projected_messages(owner_source=None, adapter_source=None):
    baseline=subprocess.check_output(['git','show',BASELINE+':'+PATH],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    current=adapter_source if adapter_source is not None else (ROOT/PATH).read_text(encoding='utf-8-sig')
    owner=owner_source if owner_source is not None else (ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs').read_text(encoding='utf-8-sig')
    expected=baseline
    for name in NAMES:
        marker='private static List<object> '+name+'('
        original=ex.declaration(baseline,marker)
        adapter=ex.declaration(current,marker)
        head=original[:original.index('\n\t\tstring system =')]
        assert adapter.startswith(head+'\n\t\treturn MainPromptMessageAssemblyOwner.'+name+'('), 'capture adapter drift '+name
        implementation=ex.declaration(owner,'internal static List<object> '+name+'(')
        body=implementation[implementation.index('\n\t\tstring system ='):]
        body=body.replace('system = JoinSystemSections(playerCustomRuleBlock, system);','system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);').replace('letterText ?? ""','session.LetterText ?? ""')
        assert head+body==original, 'pure message algorithm drift '+name
        # Frozen detached values must be forwarded exactly; no role/history/fact may be dropped.
        inbound=name==NAMES[1]
        arguments=('npcName, playerName, seed' if inbound else 'npcName, playerName, session.LetterText')+', extras, '+('fact' if inbound else 'deliveryFact')+', history, persistentMemoryRoleMessages, npcRoleContext, preprocessExcludedRuleBlock, recentFacts, '+('playerIdentity, playerRelationship' if inbound else 'senderIdentity, senderRelationship')+', currentLocationLine, currentDateFact'+(', targetChars, letterKind' if inbound else '')+', MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(string.Empty)'
        assert adapter==head+'\n\t\treturn MainPromptMessageAssemblyOwner.'+name+'('+arguments+');\n\t}', 'detached argument drift '+name
        expected=expected.replace(original,adapter,1)
    for marker in HELPERS:
        original=ex.declaration(baseline,marker)
        implementation=ex.declaration(owner,marker.replace('private static','internal static',1)).replace('internal static','private static',1)
        assert implementation==original, 'history grammar drift '+marker
        expected=expected.replace(original,'',1)
    expected=re.sub(r'(?m)^\t+$','',expected)
    assert current==expected, 'unreviewed adapter source outside extraction'
    return baseline

def production_builders():
    current=(ROOT/PATH).read_text(encoding='utf-8-sig')
    return '\n'.join(ex.declaration(current,'private static List<object> '+name+'(') for name in NAMES)
if __name__=='__main__':
    projected_messages()
    current=(ROOT/PATH).read_text(encoding='utf-8-sig')
    owner=(ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs').read_text(encoding='utf-8-sig')
    cases=[(owner.replace('AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);',';',1), None), (None,current.replace('session.LetterText, extras,','session.LetterText, null,',1)), (None,current+'\n// unrelated unreviewed source\n')]
    for altered_owner,altered_adapter in cases:
        try: projected_messages(altered_owner,altered_adapter)
        except AssertionError: pass
        else: raise AssertionError('source inverse accepted an unrelated or lost-history mutation')
    print('PASS exact Courier main composition inverse, capture argument and history grammar guards / 3 source mutation rejections')
