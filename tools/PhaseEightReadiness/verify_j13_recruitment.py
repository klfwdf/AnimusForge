"""Check J13d1 recruitment extraction and retained host boundaries at a named revision.

Source parity is separate from the generator's forced-asynchronous behavior tests.
This does not prove live recruitment actions, original Campaign ordering or old saves.
"""
import argparse
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASELINE = 'a49642bf331741a395fd2b4225e683a8566950b0'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--revision')
args = parser.parse_args()

def source(path, revision=None):
    raw = subprocess.check_output(['git', 'show', revision + ':' + path], cwd=ROOT) if revision else (ROOT / path).read_bytes()
    return raw.decode('utf-8-sig').replace('\r\n', '\n')

def declaration(text, signature):
    start = text.index('\t' + signature)
    return text[start:text.index('\n\t}', start) + 3]

old = source('RewardSystemBehavior.cs', BASELINE)
host = source('RewardSystemBehavior.cs', args.revision)
owner = source('src/modules/AF.Module.Social/Recruitment/RecruitmentOwner.cs', args.revision)
signatures = [
    'private bool TryApplyHeroJoinPlayerPartyCore(',
    'private bool TryApplyNonHeroJoinPlayerPartyForExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, string latestReply, bool asCompanion,',
    'private static bool TryPromoteNonHeroToCompanion(',
]
for signature in signatures:
    prior = declaration(old, signature)
    current = declaration(owner, signature.replace('private static', 'internal static').replace('private bool', 'internal static bool'))
    current = current.replace('internal static bool', 'private static bool' if 'private static' in signature else 'private bool', 1)
    current = current.replace('RewardSystemBehavior host, Hero joiningHero', 'Hero joiningHero')
    current = current.replace('host.RememberHeroJoinOriginalClan(', 'RememberHeroJoinOriginalClan(')
    assert current == prior, 'recruitment algorithm changed: ' + signature
    wrapper = declaration(host, signature)
    assert 'return RecruitmentOwner.' in wrapper and len(wrapper.splitlines()) == 4, 'missing real owner routing'
    host = host.replace(wrapper, prior, 1)
assert host == old, 'unreviewed family/spouse/captivity/roster/Native-token/Agent/economy/cleanup host delta'

old = source('MyBehavior.cs', BASELINE)
host = source('MyBehavior.cs', args.revision)
new = source('MyBehavior.PromotedPersonaGeneration.cs', args.revision)
removed = []
for signature in ['public static async Task GeneratePromotedNonHeroCompanionProfileForExternalAsync(',
                  'private async Task GeneratePromotedNonHeroCompanionProfileAsync(',
                  'private async Task GeneratePromotedNonHeroCompanionSkillsAsync(']:
    method = declaration(old, signature)
    removed.append(method)
    old = old.replace(method + '\n\n', '', 1)
assert host == old, 'unreviewed ordinary persona/skill parser/host delta'
# Preserve every original player-facing prompt/fallback/route string. The removed
# stage diagnostic and redundant outer error log are explicitly excluded.
literals = re.compile(r'"(?:\\.|[^"\\])*"')
excluded = {'"promoted_companion_persona"', '"promoted_companion_persona_fallback"',
            '"promoted_companion_skills_start"', '"promoted_companion_skills"',
            '"[WARN] Promoted companion profile generation failed: "'}
for literal in set(literals.findall('\n'.join(removed))) - excluded:
    assert literal in new, 'original generation text/route missing: ' + literal
for guard in ['_npcPersonaGeneration.TryBegin(', '_npcPersonaGeneration.IsCurrent(',
              'ReferenceEquals(FindHeroById(', 'work.OriginalPersonality', 'work.OriginalBackground',
              'string.Equals(skillSource, BuildPromotedHeroSkillSummary(hero)', '_npcPersonaGeneration.Complete(']:
    assert guard in new, 'missing promoted lifecycle guard: ' + guard
assert new.count('RunMemorySummaryCompletionAsync(') == 6, 'capture/commit/failure must all dispatch'
print('PASS J13d1 recruitment exact algorithm/whole-host inverse; original prompt/routes/fallback text; promotion dispatcher/lease/source guards; not live acceptance')
