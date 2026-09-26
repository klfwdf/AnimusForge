#!/usr/bin/env python3
"""Validate the current AF skill package, not historical prose or runtime implementation."""
import argparse
from pathlib import Path
import re
import sys
from urllib.parse import unquote, urlsplit

try:
    import yaml
except ImportError:
    print('NOT-RUN: YAML validation requires PyYAML; no dependency was installed.', file=sys.stderr)
    sys.exit(2)

REQUIRED = [
    'SKILL.md', 'agents/openai.yaml',
    *['references/' + name + '.md' for name in (
        'routing-and-identity', 'mod-development', 'host-compatibility', 'ledger-and-handoff',
        'repository-structure', 'plugin-architecture', 'module-and-bridge-workflow',
        'bannerlord-compatibility', 'interaction-pipeline', 'persistence-and-user-data',
        'runtime-safety', 'validation', 'known-debt', 'refactor-review-checklist')],
    'assets/module/module.yaml', 'assets/module/README.template.md',
    'assets/bridge/module.yaml', 'assets/bridge/README.template.md',
    'scripts/suggest-reference-route.sh', 'scripts/install-af-skill.sh',
    'scripts/verify-af-skill.sh', 'scripts/verify-af-skill.py', 'scripts/test-af-skill.py',
]


def markdown_body(text):
    # Ignore examples inside fenced code; local links outside examples are checked.
    return re.sub(r'^(`{3,}|~{3,})[^\n]*\n.*?^\1[ \t]*$', '', text, flags=re.M | re.S)


def anchors(text):
    text = markdown_body(text)
    result = set(re.findall(r'<a\s+id=["\']([^"\']+)["\']', text))
    seen = {}
    for heading in re.findall(r'^#{1,6}\s+(.+?)\s*#*$', text, re.M):
        slug = re.sub(r'[^\w\- ]', '', heading.lower()).replace(' ', '-')
        count = seen.get(slug, 0)
        seen[slug] = count + 1
        result.add(slug + (f'-{count}' if count else ''))
    return result


def validate(root):
    errors = []

    def check(condition, message):
        if not condition:
            errors.append(message)

    def read_yaml(path):
        try:
            data = yaml.safe_load(path.read_text(encoding='utf-8-sig'))
            if not isinstance(data, dict):
                raise ValueError('Expected a YAML mapping')
            return data
        except (OSError, ValueError, yaml.YAMLError) as exc:
            errors.append(f'{path}: {exc}')
            return {}

    for relative in REQUIRED:
        check((root/relative).is_file(), f'Missing {relative}')

    skill = root/'SKILL.md'
    if skill.is_file():
        text = skill.read_text(encoding='utf-8-sig')
        match = re.match(r'\A---\r?\n(.*?)\r?\n---(?:\r?\n|$)', text, re.S)
        check(match is not None, 'SKILL.md must start with YAML frontmatter')
        if match:
            try:
                data = yaml.safe_load(match.group(1))
                if not isinstance(data, dict):
                    raise ValueError('Expected a frontmatter mapping')
                check(not set(data) - {'name', 'description', 'license', 'allowed-tools', 'metadata'},
                      'Unsupported SKILL.md frontmatter field')
                check(data.get('name') == 'animusforge-maintainer', 'Incorrect skill identifier')
                check(isinstance(data.get('description'), str) and bool(data['description'].strip()),
                      'Missing skill description')
                metadata = data.get('metadata')
                version = metadata.get('version') if isinstance(metadata, dict) else None
                check(isinstance(version, str) and bool(re.fullmatch(r'(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)', version)),
                      'metadata.version must be a major.minor.patch string')
                check(re.findall(r'^Skill version: `([^`]+)`', text, re.M) == [version],
                      'Displayed skill version differs from metadata.version')
            except (ValueError, yaml.YAMLError) as exc:
                errors.append(f'SKILL.md frontmatter: {exc}')

    agent = read_yaml(root/'agents/openai.yaml')
    interface = agent.get('interface')
    policy = agent.get('policy')
    check(isinstance(interface, dict), 'Missing interface metadata')
    if isinstance(interface, dict):
        for key in ('display_name', 'short_description', 'default_prompt'):
            check(isinstance(interface.get(key), str) and bool(interface[key].strip()), f'Missing interface.{key}')
        check('$animusforge-maintainer' in str(interface.get('default_prompt', '')), 'Missing skill invocation in UI prompt')
    check(isinstance(policy, dict) and policy.get('allow_implicit_invocation') is True,
          'Preserve normal implicit invocation policy')

    for kind in ('module', 'bridge'):
        data = read_yaml(root/f'assets/{kind}/module.yaml')
        check(data.get('kind') == kind, f'Incorrect {kind} template kind')
        check(isinstance(data.get('id'), str) and data['id'].startswith('af.'+kind+'.'), f'Incorrect {kind} template ID')
        check(isinstance(data.get('owner'), dict), f'Missing {kind} template owner')
        compatibility = data.get('compatibility')
        check(isinstance(compatibility, dict) and compatibility.get('bannerlord') == ['1.3', '1.4'],
              f'{kind} template must retain both Bannerlord API lines')

    for path in sorted(root.rglob('*.md')):
        text = markdown_body(path.read_text(encoding='utf-8-sig'))
        # Inline links and reference-style definitions, resolved from the containing file.
        links = re.findall(r'\]\(<?([^\s)>]+)>?(?:\s+[^)]*)?\)', text)
        links += re.findall(r'^\s*\[[^\]]+\]:\s*<?([^\s>]+)>?', text, re.M)
        for link in links:
            parsed = urlsplit(link)
            if parsed.scheme or parsed.netloc:
                continue
            target = (path.parent/unquote(parsed.path)).resolve() if parsed.path else path
            check(target.exists(), f'{path.relative_to(root)}: broken link {link}')
            if target.is_file() and parsed.fragment and target.suffix.lower() == '.md':
                check(unquote(parsed.fragment) in anchors(target.read_text(encoding='utf-8-sig')),
                      f'{path.relative_to(root)}: broken anchor {link}')

    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', nargs='?', type=Path, default=Path(__file__).resolve().parent.parent)
    root = parser.parse_args().root.resolve()
    if not root.is_dir():
        print(f'ERROR: AF skill directory does not exist: {root}', file=sys.stderr)
        return 2
    try:
        errors = validate(root)
    except (OSError, UnicodeError, ValueError) as exc:
        print(f'ERROR: Cannot validate skill: {exc}', file=sys.stderr)
        return 1
    for error in errors:
        print('ERROR: '+error, file=sys.stderr)
    if errors:
        print(f'AF skill validation failed with {len(errors)} error(s).', file=sys.stderr)
        return 1
    print(f'AF skill structure, metadata, templates and local links are valid at {root}.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
