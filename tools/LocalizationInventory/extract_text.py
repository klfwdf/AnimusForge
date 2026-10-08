"""Read-only inventory of compiled AF strings and mapped text resources."""
import argparse
import bisect
from collections import Counter, defaultdict
import csv
import gzip
import hashlib
import html
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

HAN = re.compile(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\U00020000-\U000323af]')
PLACEHOLDERS = re.compile(r'\{[^{}\r\n]+\}|\[(?:ACTION|AFEF|CMD|TAG)[^\]\r\n]*\]|\{=[^}]+\}')
XML_ATTRIBUTE = re.compile(r'([\w:.-]+)\s*=\s*([\"\'])(.*?)\2', re.DOTALL)
XML_TEXT = re.compile(r'>([^<]+)<', re.DOTALL)
TEXT_EXTENSIONS = {'.json', '.xml', '.resx', '.xaml', '.config', '.txt', '.csv', '.md', '.html', '.yml', '.yaml'}

def run(root, *args, env=None):
    return subprocess.check_output(args, cwd=root, env=env).decode('utf-8-sig')

def json_records(text):
    """Walk JSON with exact token offsets, including keys and repeated values."""
    # Newtonsoft.Json resources may contain literal newlines/control characters
    # in strings. Preserve those values without rewriting the original file.
    decoder = json.JSONDecoder(strict=False)
    cursor = 0
    records = []
    def whitespace():
        nonlocal cursor
        while cursor < len(text) and text[cursor].isspace():
            cursor += 1
    def string(pointer, kind):
        nonlocal cursor
        start = cursor
        value, end = decoder.raw_decode(text, cursor)
        if not isinstance(value, str):
            raise ValueError('Expected a JSON string')
        cursor = end
        records.append({'offset': start, 'length': end-start, 'text': value,
                        'raw': text[start:end], 'context': pointer, 'kind': kind})
        return value
    def value(pointer):
        nonlocal cursor
        whitespace()
        if text[cursor] == '{':
            cursor += 1
            whitespace()
            if text[cursor] == '}':
                cursor += 1
                return
            while True:
                whitespace()
                key = string(pointer, 'json.key')
                child = pointer + '/' + key.replace('~', '~0').replace('/', '~1')
                records[-1]['context'] = child
                whitespace()
                if text[cursor] != ':':
                    raise ValueError('Expected JSON colon')
                cursor += 1
                value(child)
                whitespace()
                separator = text[cursor]
                cursor += 1
                if separator == '}': break
                if separator != ',': raise ValueError('Expected JSON object separator')
        elif text[cursor] == '[':
            cursor += 1
            whitespace()
            if text[cursor] == ']':
                cursor += 1
                return
            index = 0
            while True:
                value(pointer + '/' + str(index))
                index += 1
                whitespace()
                separator = text[cursor]
                cursor += 1
                if separator == ']': break
                if separator != ',': raise ValueError('Expected JSON array separator')
        elif text[cursor] == '"':
            string(pointer, 'json.value')
        else:
            _, cursor = decoder.raw_decode(text, cursor)
    value('')
    whitespace()
    if cursor != len(text):
        raise ValueError('Trailing JSON data')
    return records

def resource_records(path, text):
    if path.suffix.lower() == '.json':
        return json_records(text)
    if path.suffix.lower() in {'.xml', '.html', '.resx', '.xaml', '.config'}:
        # Lexical XML inventory intentionally retains bindings, IDs and comments;
        # it does not mistake these technical strings for safe translations.
        records = []
        for pattern, kind in [(XML_ATTRIBUTE, 'xml.attribute'), (XML_TEXT, 'xml.text')]:
            for match in pattern.finditer(text):
                group = 3 if kind == 'xml.attribute' else 1
                raw = match.group(group)
                if raw.strip():
                    records.append({'offset': match.start(group), 'length': len(raw),
                                    'text': html.unescape(raw), 'raw': raw, 'kind': kind,
                                    'context': match.group(1) if group == 3 else 'text-node'})
        return sorted(records, key=lambda record: record['offset'])
    return [{'offset': 0, 'length': len(text), 'text': text, 'raw': text,
             'kind': 'resource.document', 'context': 'whole-document'}]

def worldbook_path(path):
    normalized = '/' + path.replace('\\', '/').lower()
    return '/knowledge/' in normalized or normalized.endswith('/knowledgerules.json')

def classify(record):
    kind = record['kind']
    context = record.get('context', '').lower()
    path = record['path'].lower()
    if kind == 'csharp.comment': return 'comment_not_runtime'
    if kind == 'csharp.disabled': return 'conditional_code_review'
    if kind.endswith('.key') or kind == 'csharp.char': return 'logic_or_contract_review'
    if any(x in context for x in ['logger', 'console.', 'debug.', 'logerror', 'logwarn']):
        return 'diagnostic_review'
    if any(x in context for x in ['contains', 'equals', 'startswith', 'endswith', 'indexof', 'regex', 'replace', 'split']):
        return 'logic_or_contract_review'
    if kind == 'xml.attribute' and context in {'id', 'xml_path', 'name', 'sprite', 'brush', 'type', 'datasource'}:
        return 'resource_contract_review'
    if 'prompt' in path or 'prompt' in record.get('symbol', '').lower(): return 'prompt_review'
    return 'text_review'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--dotnet', default='dotnet')
    args = parser.parse_args()
    root = args.root.resolve()
    out = root / 'docs/localization/AF-ENGLISH/inventory'
    out.mkdir(parents=True, exist_ok=True)
    scratch = root / 'tools/LocalizationInventory/obj/inventory'
    scratch.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, DOTNET_CLI_HOME=str(scratch / 'dotnet-home'),
               NUGET_PACKAGES=str(scratch / 'nuget'), DOTNET_CLI_TELEMETRY_OPTOUT='1',
               DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false',
               DOTNET_NOLOGO='1')
    tracked = set(run(root, 'git', 'ls-files', '-z').strip('\0').split('\0'))
    baseline = run(root, 'git', 'rev-parse', 'HEAD').strip()
    project = root / 'AnimusForge.csproj'
    variants = defaultdict(set)
    embedded = set()
    projects = [project] + [root / p for p in sorted(tracked) if '/AF.Bootstrap/' in p and p.endswith('.csproj')]
    for project in projects:
        for api in ['1.3', '1.4']:
            output = run(root, args.dotnet, 'msbuild', str(project), '-nologo', '-getItem:Compile,EmbeddedResource',
                         f'-p:BannerlordApi={api}', env=env)
            items = json.loads(output)['Items']
            for item in items['Compile']:
                file = Path(item['FullPath']).resolve()
                if file.is_relative_to(root):
                    relative = file.relative_to(root).as_posix()
                    if relative in tracked:
                        variants[relative].add(api)
            for item in items.get('EmbeddedResource', []):
                file = Path(item['FullPath']).resolve()
                if file.is_relative_to(root):
                    relative = file.relative_to(root).as_posix()
                    if relative in tracked: embedded.add(relative)
    csharp = sorted(variants)
    input_manifest = scratch / 'csharp-paths.json'
    input_manifest.write_text(json.dumps(csharp, ensure_ascii=False), encoding='utf-8')
    tool = root / 'tools/LocalizationInventory/LocalizationInventory.csproj'
    subprocess.run([args.dotnet, 'build', str(tool), '-c', 'Release', '--nologo', '-v', 'quiet'],
                   cwd=root, env=env, check=True)
    raw_code = scratch / 'csharp.jsonl'
    with raw_code.open('wb') as stream:
        subprocess.run([args.dotnet, str(tool.parent / 'bin/Release/net8.0/LocalizationInventory.dll'),
                        str(root), str(input_manifest)], cwd=root, env=env, stdout=stream, check=True)
    content_map = json.loads((root / 'content/content-map.json').read_text(encoding='utf-8-sig'))
    mapped = {entry['source'] for entry in content_map['entries']}
    # Include module manifests and tracked resources alongside legacy module/extension
    # loaders as an explicit secondary surface; do not translate their keys blindly.
    secondary = {p for p in tracked if Path(p).suffix.lower() in TEXT_EXTENSIONS and
                 ((p.startswith(('AnimusForge/', 'content/', 'extensions/')) and
                   any(part in p.split('/') for part in ['ModuleData', 'CustomPrompts', 'GUI'])) or
                  p.endswith('/SubModule.xml'))}
    resource_paths = sorted(p for p in mapped | secondary | embedded if Path(p).suffix.lower() in TEXT_EXTENSIONS)
    excluded = []
    resources = []
    for path in resource_paths:
        if path not in tracked:
            raise ValueError(f'Mapped resource is not tracked: {path}')
        if worldbook_path(path):
            excluded.append({'path': path, 'sha256': hashlib.sha256((root / path).read_bytes()).hexdigest(),
                             'reason': 'deferred_worldbook_body', 'bytes': (root / path).stat().st_size})
        else:
            resources.append(path)
    manifest = []
    raw_han_lines = []
    counts = Counter()
    groups = {}
    all_output = out / 'occurrences.jsonl'
    han_output = out / 'han-review.jsonl'
    with all_output.open('w', encoding='utf-8', newline='\n') as all_stream, han_output.open('w', encoding='utf-8', newline='\n') as han_stream:
        def emit(record):
            text = record['text']
            record['contains_han'] = bool(HAN.search(text))
            record['category'] = classify(record)
            record['placeholders'] = sorted(set(PLACEHOLDERS.findall(text)))
            record['text_id'] = hashlib.sha256(text.encode('utf-8')).hexdigest()[:20]
            anchor = f'{record["path"]}:{record["kind"]}:{record["offset"]}:{record.get("context", "")}'
            record['occurrence_id'] = hashlib.sha256(anchor.encode('utf-8')).hexdigest()[:20]
            record['status'] = 'review_required_not_translated'
            all_stream.write(json.dumps(record, ensure_ascii=False) + '\n')
            counts['occurrences'] += 1
            counts['kind:' + record['kind']] += 1
            if record['contains_han']:
                han_stream.write(json.dumps(record, ensure_ascii=False) + '\n')
                counts['han_occurrences'] += 1
                counts['han_category:' + record['category']] += 1
                group = groups.setdefault(record['text_id'], {'text_id': record['text_id'], 'text': text, 'categories': set(), 'references': []})
                if group['text'] != text:
                    raise ValueError('Text ID collision')
                group['categories'].add(record['category'])
                group['references'].append(record['occurrence_id'])
        for line in raw_code.read_text(encoding='utf-8-sig').splitlines():
            record = json.loads(line)
            record['compiled_api_variants'] = sorted(variants[record['path']])
            emit(record)
        for path in resources:
            with (root / path).open(encoding='utf-8-sig', newline='') as stream:
                text = stream.read()
            starts = [0] + [match.end() for match in re.finditer('\n', text)]
            try:
                records = resource_records(Path(path), text)
            except (ValueError, IndexError) as exc:
                raise ValueError(f'Resource inventory failed for {path}: {exc}') from exc
            for record in records:
                index = bisect.bisect_right(starts, record['offset']) - 1
                record.update(path=path, line=index+1, column=record['offset']-starts[index]+1,
                              end_line=bisect.bisect_right(starts, record['offset']+record['length']),
                              offset_unit='unicode_codepoints',
                              surface='mapped' if path in mapped else 'secondary_loader_resource')
                emit(record)
    for path in csharp + resources:
        data = (root / path).read_bytes()
        text = data.decode('utf-8-sig')
        manifest.append({'path': path, 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data),
                         'surface': 'compiled_code' if path in variants else ('mapped_resource' if path in mapped else 'secondary_loader_resource')})
        for line, value in enumerate(text.splitlines(), 1):
            if HAN.search(value): raw_han_lines.append((path, line, value))
    with (out / 'han-source-lines.tsv').open('w', encoding='utf-8-sig', newline='') as stream:
        writer = csv.writer(stream, delimiter='\t', lineterminator='\n')
        writer.writerow(['path', 'line', 'raw_source_line'])
        writer.writerows(raw_han_lines)
    with (out / 'unique-han-texts.jsonl').open('w', encoding='utf-8', newline='\n') as stream:
        for key in sorted(groups):
            group = groups[key]
            group['categories'] = sorted(group['categories'])
            stream.write(json.dumps(group, ensure_ascii=False) + '\n')
    summary = {'schema_version': 1, 'source_revision': baseline, 'branch': 'AF-ENGLISH',
               'worldbook_policy': 'knowledge subtree and KnowledgeRules.json bodies deferred; UI/loader code included',
               'compiled_csharp_files': len(csharp), 'text_resource_files': len(resources),
               'excluded_worldbook_files': len(excluded), 'raw_han_source_lines': len(raw_han_lines),
               'unique_han_texts': len(groups), 'counts': dict(sorted(counts.items())),
               'limitations': ['Static extraction, not runtime reachability or language identification.',
                              'Disabled preprocessor trivia is inventoried as review-only, not tokenized as active code.',
                              'XML is lexically inventoried; bindings/identifiers/comments require review.',
                              'Raster image text, native binaries and player-created runtime content are not scanned.']}
    for name, value in [('manifest.json', manifest), ('excluded-worldbook.json', excluded), ('summary.json', summary)]:
        with (out / name).open('w', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(value, ensure_ascii=False, indent=2) + '\n')
    # Keep the local plain inventories, but publish deterministic compressed
    # evidence: the complete occurrence stream exceeds GitHub's blob limit.
    for name in ['occurrences.jsonl', 'han-review.jsonl', 'unique-han-texts.jsonl', 'han-source-lines.tsv']:
        with (out / name).open('rb') as source, (out / (name + '.gz')).open('wb') as destination:
            with gzip.GzipFile(filename='', fileobj=destination, mode='wb', mtime=0) as archive:
                shutil.copyfileobj(source, archive)
    print(json.dumps(summary, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    main()
