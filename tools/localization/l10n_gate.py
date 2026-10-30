"""Static localization gate for game-language prompts (pure Python, no build).

1. Every compatibility text table with Chinese values has an English edition that passes
   check_bilingual_tables (key parity, no Chinese, placeholder/protocol parity).
2. Every English edition is registered: content-map entry (role compatibility) and, for embedded
   tables, an EmbeddedResource with LogicalName '<base logical>.en.json' in AnimusForge.csproj.
3. Every fixed prompt pair (localizationRole bilingual-prompt) has both editions mapped.
4. No runtime reader still loads a fixed prompt file through a single-language Lazy
   (GetModuleDataFilePath("<X>.json") inside a Lazy initializer) when an X.en.json exists.
5. Runtime C# under src/extensions/PolicySystem has no Chinese string literals (comments excluded).

Exit code 0 when every check passes.
"""
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
HAN = re.compile('[\u3400-\u9fff\uf900-\ufaff]')
failures = []


def fail(message):
    failures.append(message)


def main():
    result = subprocess.run([sys.executable, str(ROOT / 'tools/localization/check_bilingual_tables.py'), '--require-all'],
                            capture_output=True, text=True, encoding='utf-8')
    tail = (result.stdout.strip().splitlines() or [''])[-1]
    if result.returncode != 0:
        fail('bilingual tables: ' + tail)
        for line in result.stdout.splitlines()[:20]:
            if line.startswith('FAIL'):
                fail('  ' + line)

    content_map = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8'))
    entries = content_map['entries']
    by_target = {e['target']: e for e in entries}
    project = (ROOT / 'AnimusForge.csproj').read_text(encoding='utf-8-sig')
    for entry in entries:
        role = entry.get('localizationRole')
        source = entry['source']
        if role == 'compatibility' and source.endswith('.json') and not source.endswith('.en.json'):
            english = source[:-5] + '.en.json'
            if not (ROOT / english).exists():
                continue
            en_target = entry['target'][:-5] + '.en.json'
            en_entry = by_target.get(en_target)
            if en_entry is None or en_entry.get('source') != english or en_entry.get('localizationRole') != 'compatibility':
                fail(f'content-map: {en_target} is not mapped as compatibility from {english}')
            if entry.get('logicalName'):
                logical = entry['logicalName'][:-5] + '.en.json'
                include = english.replace('/', '\\')
                if include not in project and english not in project:
                    fail(f'csproj: {english} is not an EmbeddedResource')
                elif logical not in project:
                    fail(f'csproj: {english} lacks LogicalName {logical}')
                if en_entry is not None and en_entry.get('logicalName') != logical:
                    fail(f'content-map: {en_target} logicalName should be {logical}')
        if role == 'bilingual-prompt' and not re.search(r'\.en\.(json|xml)$', entry['target']):
            en_target = re.sub(r'\.(json|xml)$', r'.en.\1', entry['target'])
            if by_target.get(en_target, {}).get('localizationRole') != 'bilingual-prompt':
                fail(f'content-map: fixed prompt {entry["target"]} has no bilingual-prompt English edition')

    english_fixed = {pathlib.PurePosixPath(e['target']).name.replace('.en.', '.') for e in entries
                     if e.get('localizationRole') == 'bilingual-prompt' and '.en.' in e['target']}
    single = re.compile(r'new Lazy<[^;]*?GetModuleDataFilePath\("([^"]+)"\)', re.S)
    runtime = []
    for root in ('src', 'extensions', 'PolicySystem', 'Vengeance', 'WarStats', 'AnimusForge.SiegeAftermathIntervention'):
        base = ROOT / root
        if base.exists():
            runtime += [p for p in base.rglob('*.cs') if not re.search(r'[\\/](bin|obj|tests|tools|artifacts|NuGet)[\\/]', str(p))]
    literal = re.compile(r'(?s)//[^\r\n]*|/\*.*?\*/|(?P<lit>(?:\$@|@\$|@)"(?:""|[^"])*"|\$?"(?:\\.|[^"\\\r\n])*"|\'(?:\\.|[^\'\\])\')')
    literal_hits = 0
    for path in runtime:
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        for match in single.finditer(text):
            if match.group(1) in english_fixed:
                fail(f'{path.relative_to(ROOT).as_posix()}: {match.group(1)} loaded by a single-language Lazy; use PromptLanguageFile')
        for match in literal.finditer(text):
            if match.group('lit') and HAN.search(match.group('lit')):
                literal_hits += 1
                if literal_hits <= 10:
                    line = text.count('\n', 0, match.start()) + 1
                    fail(f'{path.relative_to(ROOT).as_posix()}:{line}: Chinese string literal in runtime C#')

    for message in failures:
        print('FAIL', message)
    print(f'l10n gate: {tail}; runtime files={len(runtime)}; chinese literals={literal_hits}; failures={len(failures)}')
    return 1 if failures else 0


if __name__ == '__main__':
    sys.exit(main())
