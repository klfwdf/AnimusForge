"""Check J13d1 recruitment extraction and retained host boundaries at a named revision.

Source parity is separate from the generator's forced-asynchronous behavior tests.
This does not prove live recruitment actions, original Campaign ordering or old saves.
"""
import argparse
import re
import subprocess
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import current_source_path
from af2_f5_migration_review import exact_inverse
BASELINE = 'a49642bf331741a395fd2b4225e683a8566950b0'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--revision')
parser.add_argument('--mutate', choices=['recruitment-null-guard', 'retained-family-guard', 'promotion-lease-guard'])
args = parser.parse_args()

def source(path, revision=None):
    raw = subprocess.check_output(['git', 'show', revision + ':' + path], cwd=ROOT) if revision else current_source_path(ROOT, path).read_bytes()
    return raw.decode('utf-8-sig').replace('\r\n', '\n')

def declaration(text, signature):
    start = text.index('\t' + signature)
    return text[start:text.index('\n\t}', start) + 3]

old = source('RewardSystemBehavior.cs', BASELINE)
host = source('RewardSystemBehavior.cs', args.revision)
owner = source('src/modules/AF.Module.Social/Recruitment/RecruitmentOwner.cs', args.revision)
if args.mutate == 'recruitment-null-guard':
    assert 'joiningHero == null' in owner
    owner = owner.replace('joiningHero == null', 'joiningHero != null', 1)
if args.mutate == 'retained-family-guard':
    signature = 'private static bool ShouldPreservePlayerFamilyIdentityForCompanionJoin('
    body = declaration(host, signature)
    assert 'return false;' in body
    host = host.replace(body, body.replace('return false;', 'return true;', 1), 1)
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
    header = prior.splitlines()[0]
    method_name = re.search(r'(\w+)\(', header).group(1)
    parameters = header[header.index('(') + 1:header.rindex(')')]
    arguments = []
    for parameter in parameters.split(','):
        words = parameter.strip().split()
        arguments.append(('out ' if words[0] == 'out' else '') + words[-1])
    if method_name == 'TryApplyHeroJoinPlayerPartyCore':
        arguments.insert(0, 'this')
    expected_wrapper = header + '\n\t{\n\t\treturn RecruitmentOwner.' + method_name + '(' + ', '.join(arguments) + ');\n\t}'
    assert wrapper == expected_wrapper, 'missing exact real owner argument routing: ' + method_name
    host = host.replace(wrapper, prior, 1)
# J13's original whole-host inverse also locked unrelated Economy projections and
# later Memory/Weekly work. Preserve the named recruitment closure instead: every
# baseline host method called directly by the exact owner must remain unchanged.
def retained_callees(prior_host, current_host, caller, excluded=()):
    checked = []
    for match in re.finditer(r"^\t(?:private|public|internal|protected) (?:static )?[^\n=;]+\([^\n]*", prior_host, re.MULTILINE):
        signature = match.group().lstrip("\t")
        name_match = re.search(r"(\w+)\(", signature)
        if not name_match:
            continue
        name = name_match.group(1)
        if name in excluded or not re.search(r"\b" + re.escape(name) + r"\s*\(", caller):
            continue
        assert declaration(current_host, signature) == declaration(prior_host, signature), 'retained recruitment/persona callee changed: ' + signature
        checked.append(signature)
    assert checked, 'retained callee coverage must not be empty'
    return checked

recruitment_callees = retained_callees(old, host, owner, ('TryApplyHeroJoinPlayerPartyCore', 'TryApplyNonHeroJoinPlayerPartyForExternal', 'TryPromoteNonHeroToCompanion'))

old = source('MyBehavior.cs', BASELINE)
host = source('MyBehavior.cs', args.revision)
if 'JsonResponseTextCodec.TrimToMaxChars' in host: host = exact_inverse('MyBehavior.cs', host)
new = source('MyBehavior.PromotedPersonaGeneration.cs', args.revision)
if 'NpcPersonaTextRules.BuildPromoted' in new: new = exact_inverse('MyBehavior.PromotedPersonaGeneration.cs', new)
if args.mutate == 'promotion-lease-guard':
    assert '_npcPersonaGeneration.IsCurrent(' in new
    new = new.replace('_npcPersonaGeneration.IsCurrent(', '_npcPersonaGeneration.HasEntry(')
removed = []
for signature in ['public static async Task GeneratePromotedNonHeroCompanionProfileForExternalAsync(',
                  'private async Task GeneratePromotedNonHeroCompanionProfileAsync(',
                  'private async Task GeneratePromotedNonHeroCompanionSkillsAsync(']:
    method = declaration(old, signature)
    removed.append(method)
    old = old.replace(method + '\n\n', '', 1)
persona_callees = retained_callees(old, host, '\n'.join(removed))
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
print('PASS J13d1 recruitment exact algorithm/retained-callee parity=' + str(len(recruitment_callees)) + '; persona-callee parity=' + str(len(persona_callees)) + '; original prompt/routes/fallback text; promotion dispatcher/lease/source guards; not live acceptance')
