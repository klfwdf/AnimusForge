"""Exact inverse of the J13 Persona extraction against its immediate verified parent.

The old whole-repository inverse remains available separately; no unrelated J13a/b
changes are excused by updating its hashes. This proof checks complete changed host
files and the unchanged reservation owner, plus named promoted/unnamed consumers.
"""
from pathlib import Path
import subprocess
import argparse

ROOT = Path(__file__).resolve().parents[2]
BASELINE = '89b38557c9b2f01b77e145dffa7fabfcc953bcc9'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--revision', help='Verify the recorded J13c candidate after later packages change the host.')
args = parser.parse_args()

def read(path):
    if args.revision:
        return subprocess.check_output(['git', 'show', args.revision + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    return (ROOT / path).read_bytes().decode('utf-8-sig').replace('\r\n', '\n')

def prior(path):
    return subprocess.check_output(['git', 'show', BASELINE + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')

def undo(source, new, old):
    assert source.count(new) == 1, 'missing/duplicate J13 inverse anchor: ' + new[:80]
    return source.replace(new, old, 1)

helper = read('MyBehavior.PersonaGeneration.cs')
helper = undo(helper,
    '            NpcPersonaProfilePolicy.CompleteGeneratedFields(ref genP, ref genB);',
    '            if (string.IsNullOrWhiteSpace(genP) && !string.IsNullOrWhiteSpace(genB)) genP = genB;\n            else if (string.IsNullOrWhiteSpace(genB) && !string.IsNullOrWhiteSpace(genP)) genB = genP;')
helper = undo(helper,
    '                    if (!NpcPersonaProfilePolicy.TryMerge(overwriteExisting, work.OriginalPersonality, work.OriginalBackground,\n                        curP, curB, genP, genB, out string nextPersonality, out string nextBackground))',
    '                    if (overwriteExisting && (!string.Equals(curP, work.OriginalPersonality, StringComparison.Ordinal)\n                        || !string.Equals(curB, work.OriginalBackground, StringComparison.Ordinal)))')
helper = undo(helper, '                    profile.Personality = nextPersonality;',
    '                    profile.Personality = (overwriteExisting || string.IsNullOrWhiteSpace(curP)) ? genP : curP.Trim();')
helper = undo(helper, '                    profile.Background = nextBackground;',
    '                    profile.Background = (overwriteExisting || string.IsNullOrWhiteSpace(curB)) ? genB : curB.Trim();')
assert helper == prior('MyBehavior.PersonaGeneration.cs'), 'unreviewed generation/voice/prompt/lease/dispatcher delta'
readiness = undo(read('MyBehavior.PersonaReadiness.cs'), 'using AnimusForge.Refactor.Runtime;\n', '')
readiness = undo(readiness, '        bool needsGeneration = NpcPersonaProfilePolicy.NeedsGeneration(id, personality, background);',
    '        bool needsGeneration = !string.IsNullOrWhiteSpace(id)\n            && (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background));')
assert readiness == prior('MyBehavior.PersonaReadiness.cs'), 'unreviewed readiness owner/thread/availability delta'
host = undo(read('MyBehavior.cs'), '\t\treturn NpcPersonaProfilePolicy.NormalizeGenerated(text);',
    '\t\tif (string.IsNullOrWhiteSpace(text))\n\t\t{\n\t\t\treturn "";\n\t\t}\n\t\treturn (text ?? "").Replace("\\r\\n", "\\n").Replace(\'\\r\', \'\\n\').Trim();')
assert host == prior('MyBehavior.cs'), 'unreviewed host/parser/promoted/skills/unnamed/editor delta'
assert read('src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs') == prior('src/modules/AF.Module.Conversation/Internal/NpcPersonaGenerationOwner.cs'), 'reservation/cooldown algorithm drifted'
assert read('RewardSystemBehavior.cs') == prior('RewardSystemBehavior.cs'), 'promotion caller changed'
print('PASS J13 Persona exact host/helper/readiness inverse; original reservation; promoted/profile/skills/unnamed/editor consumers unchanged; not live acceptance')
