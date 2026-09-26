#!/usr/bin/env python3
"""Hermetic AF skill CLI checks; fixtures are retained for inspection, never installed."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--skill', type=Path, default=Path(__file__).resolve().parent.parent)
parser.add_argument('--bash', default=os.environ.get('AF_BASH', 'bash'))
parser.add_argument('--output', type=Path, help='Parent for a new retained fixture directory')
args = parser.parse_args()
ROOT = args.skill.resolve()
if args.output:
    args.output.mkdir(parents=True, exist_ok=True)
OUT = Path(tempfile.mkdtemp(prefix='af-skill-tests-', dir=args.output)).resolve()
ENV = {**os.environ, 'AF_PYTHON': sys.executable, 'PYTHONUTF8': '1',
       'CLAUDE_CONFIG_DIR': (OUT/'claude').as_posix(), 'CODEX_HOME': (OUT/'codex').as_posix()}
PROJECT = OUT/'af-project'
PROJECT.mkdir()
(PROJECT/'AnimusForge').mkdir()
(PROJECT/'AnimusForge.csproj').write_text('<Project />', encoding='utf-8')
(PROJECT/'AnimusForge/SubModule.xml').write_text('<Module />', encoding='utf-8')
OTHER = OUT/'other'
OTHER.mkdir()


def run(script, *argv, root=ROOT):
    return subprocess.run([args.bash, (root/'scripts'/script).as_posix(), *map(str, argv)],
                          env=ENV, capture_output=True, encoding='utf-8', errors='replace', timeout=45)


class SkillTools(unittest.TestCase):
    def assert_exit(self, result, code=0):
        self.assertEqual(result.returncode, code, result.stdout + result.stderr)

    def test_source_validation(self):
        self.assert_exit(run('verify-af-skill.sh', ROOT.as_posix()))

    def test_shell_syntax(self):
        for script in ROOT.joinpath('scripts').glob('*.sh'):
            with self.subTest(script=script.name):
                self.assert_exit(subprocess.run([args.bash, '-n', script.as_posix()],
                                               capture_output=True, text=True, timeout=15))

    def test_relative_cli_entrypoints(self):
        commands = [
            ['scripts/verify-af-skill.sh', '.'],
            ['scripts/suggest-reference-route.sh', PROJECT.as_posix(), 'REFACTOR'],
            ['scripts/install-af-skill.sh', '--host', 'claude', '--source', '.', '--dry-run'],
        ]
        for argv in commands:
            with self.subTest(script=argv[0]):
                # A non-login Git Bash launched by a Windows host may lack coreutils in PATH.
                result = subprocess.run([args.bash, *argv], cwd=ROOT, env={**ENV, 'PATH': ''}, capture_output=True,
                                        encoding='utf-8', errors='replace', timeout=45)
                self.assert_exit(result)
                if 'suggest' in argv[0]:
                    self.assertIn('module-and-bridge-workflow.md', result.stdout)

    def test_five_routes(self):
        cases = [
            ('修复普通 Bug', ['mod-development.md'], ['known-debt.md', 'plugin-architecture.md']),
            ('内部职责抽取重构', ['module-and-bridge-workflow.md', 'plugin-architecture.md'], ['known-debt.md']),
            ('public API contract', ['plugin-architecture.md'], ['known-debt.md']),
            ('双版本兼容 1.3/1.4', ['bannerlord-compatibility.md'], ['known-debt.md']),
            ('文档修改', ['validation.md'], ['known-debt.md', 'module-and-bridge-workflow.md']),
        ]
        for summary, wanted, forbidden in cases:
            with self.subTest(summary=summary):
                result = run('suggest-reference-route.sh', PROJECT.as_posix(), summary)
                self.assert_exit(result)
                for ref in wanted:
                    self.assertIn(ref, result.stdout)
                for ref in forbidden:
                    self.assertNotIn(ref, result.stdout)

    def test_no_mandatory_ledger_route(self):
        result = run('suggest-reference-route.sh', PROJECT.as_posix(), '修复普通 Bug')
        self.assert_exit(result)
        self.assertNotIn('ledger-and-handoff.md', result.stdout)

    def test_identity_and_invalid_path(self):
        for summary in ('普通 Bannerlord Mod', 'Minecraft Forge AF'):
            result = run('suggest-reference-route.sh', OTHER.as_posix(), summary)
            self.assert_exit(result)
            self.assertIn('unconfirmed', result.stdout)
            self.assertNotIn('mod-development.md', result.stdout)
        self.assert_exit(run('suggest-reference-route.sh', (OUT/'missing').as_posix()), 2)
        self.assert_exit(run('verify-af-skill.sh', (OUT/'missing').as_posix()), 2)

    def test_explicit_review_and_debt(self):
        for summary, ref in [('重构复核', 'refactor-review-checklist.md'), ('技术债盘点', 'known-debt.md'),
                             ('交接进度', 'ledger-and-handoff.md')]:
            result = run('suggest-reference-route.sh', PROJECT.as_posix(), summary)
            self.assert_exit(result)
            self.assertIn(ref, result.stdout)

    def fixture(self, name):
        target = OUT/name
        shutil.copytree(ROOT, target)
        return target

    def test_sibling_links_and_anchors(self):
        root = self.fixture('sibling-links')
        probe = root/'references/link-probe.md'
        probe.write_text('# Probe\n\n[ok](plugin-architecture.md)\n[anchor](#probe)\n', encoding='utf-8')
        self.assert_exit(run('verify-af-skill.sh', root.as_posix(), root=root))
        probe.write_text('# Probe\n\n[bad](missing-sibling.md)\n', encoding='utf-8')
        result = run('verify-af-skill.sh', root.as_posix(), root=root)
        self.assert_exit(result, 1)
        self.assertIn('missing-sibling.md', result.stderr)
        probe.write_text('# Probe\n\n[bad](#missing-anchor)\n', encoding='utf-8')
        result = run('verify-af-skill.sh', root.as_posix(), root=root)
        self.assert_exit(result, 1)
        self.assertIn('missing-anchor', result.stderr)

    def test_no_magic_prose_requirement(self):
        root = self.fixture('prose-change')
        entry = root/'SKILL.md'
        data = entry.read_text(encoding='utf-8').split('---', 2)
        entry.write_text('---'+data[1]+'---\n# Alternate wording\n\nSkill version: `0.2.0`\n', encoding='utf-8')
        self.assert_exit(run('verify-af-skill.sh', root.as_posix(), root=root))

    def test_bad_metadata_and_template(self):
        root = self.fixture('bad-version')
        p = root/'SKILL.md'
        p.write_text(p.read_text(encoding='utf-8').replace('version: "0.2.0"', 'version: "invalid"'), encoding='utf-8')
        self.assert_exit(run('verify-af-skill.sh', root.as_posix(), root=root), 1)
        root = self.fixture('bad-template')
        p = root/'assets/module/module.yaml'
        p.write_text(p.read_text(encoding='utf-8').replace('["1.3", "1.4"]', '["1.4"]'), encoding='utf-8')
        self.assert_exit(run('verify-af-skill.sh', root.as_posix(), root=root), 1)
        root = self.fixture('bad-yaml')
        (root/'agents/openai.yaml').write_text('interface: [\n', encoding='utf-8')
        self.assert_exit(run('verify-af-skill.sh', root.as_posix(), root=root), 1)

    def test_install_dry_run_and_refusal(self):
        result = run('install-af-skill.sh', '--host', 'both', '--mode', 'copy', '--source', ROOT.as_posix(), '--dry-run')
        self.assert_exit(result)
        self.assertIn('DRY-RUN:', result.stdout)
        self.assertFalse((OUT/'claude').exists())
        self.assertFalse((OUT/'codex').exists())
        destination = OUT/'codex/skills/animusforge-maintainer'
        destination.mkdir(parents=True)
        sentinel = destination/'existing.txt'
        sentinel.write_text('keep', encoding='utf-8')
        self.assert_exit(run('install-af-skill.sh', '--host', 'codex', '--source', ROOT.as_posix(), '--dry-run'), 3)
        self.assertEqual(sentinel.read_text(encoding='utf-8'), 'keep')
        self.assert_exit(run('install-af-skill.sh', '--host', 'unknown', '--dry-run'), 2)
        self.assert_exit(run('install-af-skill.sh', '--bad-option'), 2)


if __name__ == '__main__':
    print(f'Retained fixtures: {OUT}', flush=True)
    unittest.main(argv=[sys.argv[0]], verbosity=2)
