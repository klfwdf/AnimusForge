from __future__ import annotations

import contextlib
import io
import json
import re
import subprocess
import sys
import unittest
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import PersistenceIdentityAudit as audit  # noqa: E402


class PersistenceIdentityAuditTests(unittest.TestCase):
    def test_compile_pruning_only_uses_unconditional_preinclude_removals(self) -> None:
        project='<Project><ItemGroup><Compile Remove="artifacts/**/*.cs;tests/**/*.cs"/><Compile Remove="conditional/**/*.cs" Condition="condition"/><Compile Include="tests/Linked.cs"/><Compile Remove="later/**/*.cs"/></ItemGroup></Project>'
        self.assertEqual(audit.compile_glob_pruning_patterns(project), ['artifacts/**/*.cs', 'tests/**/*.cs'])
        for unsafe in ['<Project><Import Project="unknown"/></Project>', '<Project><Choose/></Project>',
                       '<Project><ItemGroup><Compile Remove="$(Unknown)/**/*.cs"/></ItemGroup></Project>']:
            with self.assertRaisesRegex(ValueError, 'cannot prove'):
                audit.compile_glob_pruning_patterns(unsafe)

    def test_actual_msbuild_pruning_preserves_explicit_linked_compile(self) -> None:
        sys.path.insert(0, str(ROOT/'tests'))
        from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
        out=new_run_root(ROOT,'identity-pruning-equivalence',None)
        dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
        for key in ('DOTNET_CLI_HOME','USERPROFILE','APPDATA','LOCALAPPDATA','TEMP','TMP'):
            Path(env[key]).mkdir(parents=True,exist_ok=True)
        env['PYTHONDONTWRITEBYTECODE']='1'
        for relative in ['Owner.cs','tests/Fake.cs','tests/Linked.cs','artifacts/Fake.cs','extensions/Linked/Owner.cs']:
            file=out/relative;file.parent.mkdir(parents=True,exist_ok=True);file.write_text('class Sample {}',encoding='utf-8')
        project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Remove="tests/**/*.cs;artifacts/**/*.cs;extensions/**/*.cs"/><Compile Include="tests/Linked.cs;extensions/Linked/Owner.cs"/></ItemGroup></Project>'
        file=out/'Input.csproj';file.write_text(project,encoding='utf-8')
        command=[str(dotnet),'msbuild',str(file),'-getItem:Compile','-nologo']
        before=subprocess.run(command,cwd=out,env=env,capture_output=True,text=True,check=True,timeout=30)
        optimized_env=env.copy()
        optimized_env['DefaultItemExcludes']=env.get('DefaultItemExcludes','')+';'+ ';'.join(re.sub(r'[\\/]\*\*[\\/]\*\.cs$', '/**', pattern) for pattern in audit.compile_glob_pruning_patterns(project))
        after=subprocess.run(command+['-getProperty:DefaultItemExcludes'],cwd=out,env=optimized_env,capture_output=True,text=True,check=True,timeout=30)
        self.assertIn('bin', json.loads(after.stdout)['Properties']['DefaultItemExcludes'])
        self.assertIn('obj', json.loads(after.stdout)['Properties']['DefaultItemExcludes'])
        paths=lambda result: sorted(x['Identity'].replace('\\','/') for x in json.loads(result.stdout)['Items']['Compile'])
        self.assertEqual(paths(before),['Owner.cs','extensions/Linked/Owner.cs','tests/Linked.cs'])
        self.assertEqual(paths(after),paths(before))
        (out/'before.json').write_text(before.stdout,encoding='utf-8');(out/'after.json').write_text(after.stdout,encoding='utf-8')

    def test_civilwar_review_accepts_only_exact_authorized_key_delta(self) -> None:
        old={('unchanged','int'),('_af_kingdom_civil_war_v1','string')}
        new={('unchanged','int'),('_af_kingdom_civil_war_v2','string')}
        self.assertTrue(audit.reviewed_civilwar_v2(new,old))
        for candidate in [old,new|{('extra','string')},new-{('unchanged','int')},
                          {('unchanged','int'),('_af_kingdom_civil_war_v2','int')},
                          new|{('_af_kingdom_civil_war_v1','string')}]:
            self.assertFalse(audit.reviewed_civilwar_v2(candidate,old))

    def test_local_sync_alias_preserves_type_and_detects_drift(self) -> None:
        field = 'private Dictionary<string, int> _love = null;\ndata.SyncData("love", ref _love);'
        local = 'Dictionary<string, int> forSync = _owner.Values;\ndata.SyncData("love", ref forSync);'
        self.assertEqual(audit.sync_bindings(field), audit.sync_bindings(local))
        self.assertEqual(audit.sync_bindings(local), {("love", "Dictionary<string, int>")})
        self.assertNotEqual(audit.sync_bindings(field), audit.sync_bindings(local.replace('int>', 'string>')))
        self.assertEqual(audit.sync_bindings('return forSync;\ndata.SyncData("love", ref forSync);'), {("love", "UNRESOLVED")})

    def test_semantic_refs_preserve_load_drift_and_method_scope(self) -> None:
        source = 'class Owner { internal int Count; }\nclass Adapter {\n static void Save(Store data, Owner owner, string folder) {\n  int local = owner.Count; data.SyncData("count", ref local); data.SyncData("voice", ref folder);\n }\n static void Load(Store data, Owner owner, int folder) {\n  data.SyncData("count", ref owner.Count); data.SyncData("number", ref folder);\n }\n}\nclass Store { internal void SyncData<T>(string key, ref T value) {} }'
        capture = lambda text: audit.current_sync([(Path("typed-scope.cs"), text)])
        expected = {("count", "int"), ("voice", "string"), ("number", "int")}
        self.assertEqual(capture(source), expected)
        self.assertEqual(capture(source.replace("internal int Count", "internal bool Count")),
                         expected | {("count", "bool")})
        self.assertEqual(capture(source.replace("ref owner.Count", "ref owner.Missing")),
                         expected | {("count", "UNRESOLVED")})
        wrong_key = source.replace('data.SyncData("count", ref owner.Count)',
                                   'data.SyncData("owner.Count", ref owner.Count)')
        self.assertEqual(capture(wrong_key), expected | {("owner.Count", "int")})

    def test_batch_parser_reads_multiple_blobs_and_missing(self) -> None:
        payload = b"abc"
        data = b"a" * 40 + b" blob 3\n" + payload + b"\n" + b"b" * 40 + b" missing\n"
        result = audit.parse_batch_cat_file(data, expected_objects=2)
        self.assertEqual(result["a" * 40], "abc")

    def test_batch_parser_rejects_truncated_blob(self) -> None:
        with self.assertRaises(ValueError):
            audit.parse_batch_cat_file(b"a" * 40 + b" blob 4\nabc\n")

    def test_current_snapshot_is_reused(self) -> None:
        snapshot = [(Path("one.cs"), "class One : CampaignBehaviorBase {}")]
        with mock.patch.object(audit, "current_source_snapshot", side_effect=AssertionError("re-enumerated")):
            self.assertEqual(audit.current_sync(snapshot), set())
            self.assertEqual(audit.current_behaviors(snapshot), {"One"})

    def test_json_output_is_stdout_only_and_progress_is_stderr(self) -> None:
        process = subprocess.run(
            [sys.executable, str(ROOT / "tools" / "PersistenceIdentityAudit.py"), "--json"],
            cwd=ROOT, capture_output=True, text=True, timeout=180,
        )
        self.assertIn('"status"', process.stdout)
        self.assertNotIn("current source enumeration", process.stdout)
        self.assertIn("current source enumeration", process.stderr)
        quiet = subprocess.run(
            [sys.executable, str(ROOT / "tools" / "PersistenceIdentityAudit.py"), "--json", "--quiet"],
            cwd=ROOT, capture_output=True, text=True, timeout=180,
        )
        self.assertNotIn("current source enumeration", quiet.stderr)

    def test_fail_closed_on_baseline_error(self) -> None:
        with mock.patch.object(audit, "current_source_snapshot", return_value=[]), \
             mock.patch.object(audit, "baseline_source_snapshot", side_effect=RuntimeError("baseline unavailable")), \
             mock.patch.object(sys, "argv", ["PersistenceIdentityAudit.py", "--json", "--quiet"]):
            stdout = io.StringIO()
            stderr = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                with contextlib.redirect_stderr(stderr):
                    self.assertEqual(audit.main(), 1)
            self.assertEqual(json.loads(stdout.getvalue())["status"], "FAIL")
            self.assertIn("baseline unavailable", stderr.getvalue())


if __name__ == "__main__":
    unittest.main(verbosity=2)
