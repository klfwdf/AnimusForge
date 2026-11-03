"""Validate English editions of runtime text tables (BilingualTextTable).

For every compatibility table X.json registered in content/content-map.json that has an X.en.json:
- the English file has exactly the same keys;
- each English value is "=" (language-invariant source) or contains no Han characters;
- {NAME}/{0} placeholders, AFEF/ACTION protocol markers and leading/trailing whitespace shape match.
With --require-all, every compatibility table must have an English edition.

Exit code 0 when all checks pass. Pure Python, no build.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
HAN = re.compile('[\u3400-\u9fff\uf900-\ufaff]')
# Fullwidth/CJK punctuation alone (e.g. a closing '”。') still marks a Chinese-only source fragment.
CJK = re.compile('[\u3000-\u303f\u3400-\u9fff\uf900-\ufaff\uff00-\uffef\u201c\u201d\u2018\u2019]')
PLACEHOLDER = re.compile(r'\{[A-Za-z_][A-Za-z0-9_]*\}|\{[0-9]+(?::[^}]*)?\}')
PROTOCOL = re.compile(r'\[(?:ACTION|RELAY|AD|ADP|AFEF)(?:[: ]|(?=[\u3400-\u9fff]))[^\]\r\n]*\]')
SAME = '='
FULLWIDTH_SEPARATORS = '：，；、。！？）」』'


def flatten(value, prefix=''):
    if isinstance(value, dict):
        for key, item in value.items():
            yield from flatten(item, prefix + key + '.')
    elif isinstance(value, str):
        yield prefix[:-1], value


# Canonical ASCII AFEF envelopes are equivalent to the legacy Chinese ones (history parsers accept both).
AFEF_EQUIVALENT = {
    '[AFEF NPC行为补充]': '[AFEF NPC_ACTION]',
    '[AFEF玩家行为补充]': '[AFEF PLAYER_ACTION]',
    # PrimaryChatMessagePolicy projects the legacy civil-war marker to plain "[Civil war fact]".
    '[AFEF内战事实]': None,
}


# Tables whose readers never compose new text from them; their Chinese values are identities only.
LANGUAGE_INVARIANT = {
    # Exact previous MCM defaults, used only to recognize and migrate old saved settings (DuelSettings).
    'LegacySettingsPrompts': 'legacy MCM default identities',
    'McmPromptSourceCompatibility': 'legacy MCM default identities and persisted option values',
    # LegacyValue is a routing id; the display text is a localized {=AF_UI_Terminal_*} string.
    'TerminalCategories': 'terminal routing ids',
    # Source patterns that UI projection matches to rewrite stored status lines; never composed into prompts.
    'EconomyDisplaySources': 'UI projection match patterns',
    # One embedded corpus matches both historical languages; it never renders new text.
    # DiplomacyReplyTextRules builds whole-line matchers; sponsor rules parse proven AFEF only.
    'DiplomacyReceiptCompatibility': 'legacy CN/EN receipt identities and parsing patterns',
}


def tokens(pattern, text):
    return sorted(set(AFEF_EQUIVALENT.get(t, t) for t in pattern.findall(text)) - {None})


def main(argv):
    require_all = '--require-all' in argv
    only = [a for a in argv if not a.startswith('--')]
    content_map = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8'))
    failures = []
    checked = missing = 0
    for entry in content_map['entries']:
        if entry.get('localizationRole') != 'compatibility' or not entry['source'].endswith('.json'):
            continue
        source = ROOT / entry['source']
        if source.name.endswith('.en.json'):
            continue
        if only and source.stem not in only:
            continue
        if source.stem in LANGUAGE_INVARIANT:
            continue
        english = source.with_name(source.stem + '.en.json')
        chinese_data = json.loads(source.read_text(encoding='utf-8'))
        if not isinstance(chinese_data, dict) or not any(HAN.search(v) for _, v in flatten(chinese_data)):
            continue
        if not english.exists():
            missing += 1
            if require_all:
                failures.append(f'{entry["source"]}: missing English edition')
            continue
        checked += 1
        english_data = json.loads(english.read_text(encoding='utf-8'))
        zh = dict(flatten(chinese_data))
        en = dict(flatten(english_data))
        name = english.relative_to(ROOT).as_posix()
        for key in sorted(set(zh) - set(en)):
            failures.append(f'{name}: missing key {key}')
        for key in sorted(set(en) - set(zh)):
            failures.append(f'{name}: extra key {key}')
        for key in sorted(set(zh) & set(en)):
            value, source_value = en[key], zh[key]
            if value == SAME:
                continue
            if not CJK.search(source_value) and value != source_value:
                failures.append(f'{name}: {key} has no Chinese source; keep it "=" (source: {source_value!r})')
                continue
            if HAN.search(value):
                failures.append(f'{name}: {key} English value contains Chinese: {value!r}')
            if tokens(PLACEHOLDER, value) != tokens(PLACEHOLDER, source_value):
                failures.append(f'{name}: {key} placeholders differ: {tokens(PLACEHOLDER, source_value)} -> {tokens(PLACEHOLDER, value)}')
            if tokens(PROTOCOL, value) != tokens(PROTOCOL, source_value):
                failures.append(f'{name}: {key} protocol markers differ')
            # Fullwidth punctuation carries its own spacing; its English counterpart ("：" -> ": ") may end with a space.
            trailing_ok = source_value[-1:].isspace() == value[-1:].isspace() or (
                value[-1:] == ' ' and source_value[-1:] in FULLWIDTH_SEPARATORS)
            if source_value[:1].isspace() != value[:1].isspace() or not trailing_ok:
                failures.append(f'{name}: {key} leading/trailing whitespace shape differs: {source_value!r} -> {value!r}')
    for failure in failures[:200]:
        print('FAIL', failure)
    print(f'bilingual tables checked={checked} missing_english={missing} failures={len(failures)}')
    return 1 if failures else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
