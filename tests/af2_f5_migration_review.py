"""Exact approved F5 owner-movement inverse, anchored to the verified product freeze.

This never refreshes historical review digests. The old whole-host/algorithm
guards still run after inverse, and every new owner is bound to committed code.
Live game acceptance is not inferred from this source proof.
"""
from pathlib import Path
import difflib
from functools import lru_cache
import importlib.util
import re
import subprocess
from output_isolation import current_source_path

ROOT = Path(__file__).resolve().parents[1]
BEFORE = '320c1aad10df86b6078c1731c0ff8966ebd7c3f5'
PRODUCT = '2287069b6884981373f4a93992d6991091a871d1'
PATHS = {
    'ShoutBehavior.cs': 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs',
    'MyBehavior.cs': 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs',
    'MyBehavior.PersonaGeneration.cs': 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaGeneration.cs',
    'MyBehavior.PromotedPersonaGeneration.cs': 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PromotedPersonaGeneration.cs',
}
OWNERS = (
    'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs',
    'src/modules/AF.Module.Conversation/Internal/Pipeline/FullInteractionPipeline.cs',
    'src/modules/AF.Module.Conversation/Internal/Pipeline/LegacyInteractionPipelineComposition.cs',
    'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs',
    'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeDetachedPostprocess.cs',
    'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs',
    'src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs',
    'src/modules/AF.Module.Llm/Protocol/JsonResponseTextCodec.cs',
)
PERSONA_SYMBOLS = (
    'StripJsonResponseEnvelope', 'ExtractJsonObjectPayloads', 'TryExtractLooseJsonStringProperty',
    'TryReadLooseJsonStringValue', 'SkipJsonWhitespace', 'GetJsonPropertyIgnoreCase',
    'GetJsonStringIgnoreCase', 'TrimToMaxChars', 'NormalizePersonaPromptSourceText',
    'TryParsePersonaJson', 'ExtractLoosePersonaJsonField',
    'AppendNpcPersonaGenerationRequirementsToSystemPrompt', 'NormalizePromotedSkillKey',
    'TryApplyPromotedHeroSkillJson',
)


@lru_cache(maxsize=None)
def committed(revision, path):
    return subprocess.check_output(['git', 'show', revision + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')


def verify_owners():
    for path in OWNERS:
        actual = current_source_path(ROOT, path).read_text(encoding='utf-8-sig')
        assert actual == committed(PRODUCT, path), 'Unreviewed F5 dependency: ' + path


def exact_inverse(path, source, verify=True):
    """Reverse only the approved freeze's complete, uniquely matched source spans."""
    if verify:
        verify_owners()
    path = str(path).replace(chr(92), '/')
    resolved = PATHS.get(path, path)
    before = committed(BEFORE, resolved)
    after = committed(PRODUCT, resolved)
    if path == 'MyBehavior.cs':
        # Other owners may legitimately evolve this shared host. Only the 14 F5d
        # text/parser adapters are inverted; all surrounding validation survives.
        spec = importlib.util.spec_from_file_location('f5_decl', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
        ex = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(ex)
        for symbol in PERSONA_SYMBOLS:
            match = re.search(r'(?:private|internal|public) static [^\n]+?\b' + symbol + r'\(', after)
            assert match, 'Missing frozen F5d symbol: ' + symbol
            signature = match.group()
            new = ex.declaration(after, signature)
            old = ex.declaration(before, signature)
            assert source.count(new) == 1, 'Unreviewed F5d declaration: ' + symbol
            source = source.replace(new, old, 1)
        return source
    for old, new in approved_hunks(resolved):
        assert new and source.count(new) == 1, 'Unreviewed F5 migration span: ' + path
        source = source.replace(new, old, 1)
    return source


@lru_cache(maxsize=None)
def approved_hunks(resolved):
    old_lines = committed(BEFORE, resolved).splitlines(keepends=True)
    new_lines = committed(PRODUCT, resolved).splitlines(keepends=True)
    groups = list(difflib.SequenceMatcher(a=old_lines, b=new_lines, autojunk=False).get_grouped_opcodes(3))
    return tuple((''.join(old_lines[group[0][1]:group[-1][2]]), ''.join(new_lines[group[0][3]:group[-1][4]])) for group in reversed(groups))
