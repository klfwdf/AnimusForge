"""Source delivery checks; fixtures stay in the ignored workspace-local directory."""
from __future__ import annotations

import hashlib
import ast
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
from contextlib import redirect_stdout, redirect_stderr

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location("source_delivery", ROOT / "scripts/validate_source_delivery.py")
delivery = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(delivery)


class SourceDeliveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        base = ROOT / "local/source-delivery-tests"
        base.mkdir(parents=True, exist_ok=True)
        cls.runs = Path(tempfile.mkdtemp(dir=base))

    def setUp(self):
        self.repo = Path(tempfile.mkdtemp(dir=self.runs))
        self.git("init", "-b", "main")
        self.git("config", "user.name", "Fixture")
        self.git("config", "user.email", "fixture@example.invalid")
        self.git("config", "core.hooksPath", ".unused-hooks")
        self.policy = {"schema": 1, "reviewedTools": ["ApprovedTool", "audit.py"], "reservedRoots": ["local"]}
        self.policy_path = self.repo / "policy.json"
        self.policy_path.write_text(json.dumps(self.policy), encoding="utf-8")
        self.write("tools/ApprovedTool/source.py", "print('ok')\n")
        self.anchor = self.commit()
        self.rules = {"schema": 1, "allowPush": True, "approvedRepository": "example/project",
                      "policySha256": hashlib.sha256(self.policy_path.read_bytes()).hexdigest(),
                      "anchors": {"refs/heads/main": self.anchor}, "excludedText": ["FixtureInternalMarker"]}
        self.rules_path = self.repo.parent / (self.repo.name + "-rules.json")
        self.save_rules()

    def git(self, *args):
        return subprocess.check_output(["git", "-C", str(self.repo), *args], stderr=subprocess.PIPE).decode().strip()

    def write(self, path, text):
        p = self.repo / path
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")

    def commit(self, message="Fixture change"):
        self.git("add", "--all")
        self.git("commit", "--allow-empty", "-m", message)
        return self.git("rev-parse", "HEAD")

    def save_rules(self):
        self.rules_path.write_text(json.dumps(self.rules), encoding="utf-8")

    def invoke(self, new=None, remote="https://github.com/example/project.git", raw=None):
        if raw is None:
            raw = f"refs/heads/main {new or self.git('rev-parse', 'HEAD')} refs/heads/main {self.anchor}\n"
        args = ["--repo", str(self.repo), "--policy", str(self.policy_path),
                "--local-rules", str(self.rules_path), "--pre-push", "--remote-url", remote]
        stdout, stderr = io.StringIO(), io.StringIO()
        with patch("sys.stdin", io.StringIO(raw)), redirect_stdout(stdout), redirect_stderr(stderr):
            code = delivery.main(args)
        return subprocess.CompletedProcess(args, code, stdout.getvalue(), stderr.getvalue())

    def test_reviewed_paths(self):
        self.assertEqual(delivery.path_violations(["src/Runtime.cs", "tools/ApprovedTool/x.cs", "tools/audit.py"], self.policy), 0)

    def test_unreviewed_and_case_variants_rejected(self):
        for path in ["tools/Unreviewed/a.cs", "Tools/Unreviewed/a.cs", "local/file", "LOCAL/file", "../file", "/file", "tools\\ApprovedTool\\x.cs"]:
            self.assertGreater(delivery.path_violations([path], self.policy), 0, path)

    def test_policy_rejects_invalid_entries(self):
        for value in ["../x", "/x", "x/y", "x\\y", "", "*"]:
            self.policy_path.write_text(json.dumps({**self.policy, "reviewedTools": [value]}), encoding="utf-8")
            with self.assertRaises(ValueError):
                delivery.load_policy(self.policy_path)

    def test_policy_rejects_duplicate_entries_and_bad_schema(self):
        for policy in [{**self.policy, "reviewedTools": ["A", "a"]}, {**self.policy, "schema": 9}]:
            self.policy_path.write_text(json.dumps(policy), encoding="utf-8")
            with self.assertRaises(ValueError):
                delivery.load_policy(self.policy_path)

    def test_url_normalization(self):
        for url in ["https://github.com/example/project.git", "git@github.com:example/project.git", "ssh://git@github.com/example/project.git"]:
            self.assertEqual(delivery.repository_name(url), "example/project")
        for url in ["https://user:secret@github.com/example/project.git", "file:///tmp/repo", "https://other.example/example/project"]:
            with self.assertRaises(ValueError):
                delivery.repository_name(url)

    def test_approved_anchor_and_new_commit_pass(self):
        self.assertEqual(self.invoke().returncode, 0)
        self.write("tools/ApprovedTool/new.py", "ok\n")
        self.assertEqual(self.invoke(self.commit()).returncode, 0)

    def test_unknown_tool_is_blocked_without_echoing_path(self):
        self.write("tools/UnreviewedTool/x.cs", "fixture\n")
        result = self.invoke(self.commit())
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn("UnreviewedTool", result.stdout + result.stderr)

    def test_removed_intermediate_tool_is_still_blocked(self):
        self.write("tools/UnreviewedTool/x.cs", "fixture\n")
        self.commit()
        self.git("rm", "--cached", "tools/UnreviewedTool/x.cs")
        self.git("commit", "-m", "Fixture removal")
        self.assertNotEqual(self.invoke().returncode, 0)

    def test_content_and_message_are_checked(self):
        self.write("README.md", "FixtureInternalMarker\n")
        result = self.invoke(self.commit())
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn("FixtureInternalMarker", result.stdout + result.stderr)

    def test_commit_message_is_checked(self):
        self.assertNotEqual(self.invoke(self.commit("FixtureInternalMarker")).returncode, 0)

    def test_utf16_payload_is_checked(self):
        (self.repo / "payload.bin").write_bytes("FixtureInternalMarker".encode("utf-16-le"))
        self.assertNotEqual(self.invoke(self.commit()).returncode, 0)

    def test_malformed_input_is_rejected(self):
        for raw in ["invalid\n", "refs/heads/main bad refs/heads/main bad\n", f"refs/heads/main {'0'*40} refs/heads/main {self.anchor}\n"]:
            self.assertNotEqual(self.invoke(raw=raw).returncode, 0)

    def test_empty_push_is_noop(self):
        self.assertEqual(self.invoke(raw="").returncode, 0)

    def test_unapproved_remote_missing_rules_and_paused_rules_fail_closed(self):
        self.assertNotEqual(self.invoke(remote="https://github.com/other/project.git").returncode, 0)
        self.rules["allowPush"] = False
        self.save_rules()
        self.assertNotEqual(self.invoke().returncode, 0)
        self.rules_path = self.repo / "missing.json"
        self.assertNotEqual(self.invoke().returncode, 0)

    def test_unreviewed_policy_change_is_rejected(self):
        self.policy_path.write_text(json.dumps({**self.policy, "reviewedTools": ["Anything"]}), encoding="utf-8")
        self.assertNotEqual(self.invoke().returncode, 0)

    def test_unrelated_root_history_is_rejected(self):
        self.git("checkout", "--orphan", "unrelated")
        self.assertNotEqual(self.invoke(self.commit("Independent fixture root")).returncode, 0)

    def test_unknown_anchor_is_rejected(self):
        self.rules["anchors"] = {"refs/heads/main": "a" * 40}
        self.save_rules()
        self.assertNotEqual(self.invoke().returncode, 0)

    def test_invalid_local_schema_is_rejected(self):
        for update in [{"schema": 9}, {"anchors": {}}, {"excludedText": []}, {"excludedText": [""]}]:
            original = dict(self.rules)
            self.rules.update(update)
            self.save_rules()
            self.assertNotEqual(self.invoke().returncode, 0)
            self.rules = original

    def test_content_scan_crosses_chunk_boundary(self):
        payload = b"x" * (1024 * 1024 - 3) + b"FixtureInternalMarker" + b"y" * 32
        (self.repo / "boundary.bin").write_bytes(payload)
        self.assertNotEqual(self.invoke(self.commit()).returncode, 0)

    def test_reference_cli(self):
        result = subprocess.run([os.sys.executable, "-B", str(ROOT / "scripts/validate_source_delivery.py"),
                                 "--repo", str(self.repo), "--policy", str(self.policy_path), "--ref", self.anchor], capture_output=True)
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_source_overlay_declares_reviewed_auxiliary_sources(self):
        tree = ast.parse((ROOT / "tools/package_policy_system_source_overlay.py").read_text(encoding="utf-8-sig"))
        paths = [node.args[0].value for node in ast.walk(tree)
                 if isinstance(node, ast.Call) and isinstance(node.func, ast.Name)
                 and node.func.id in ("add_tree", "add_file") and node.args
                 and isinstance(node.args[0], ast.Constant) and isinstance(node.args[0].value, str)
                 and node.args[0].value.startswith("tools/")]
        policy = delivery.load_policy(ROOT / "scripts/source_delivery.json")
        self.assertTrue(paths)
        self.assertEqual(delivery.path_violations(paths, policy), 0)

    def test_ignore_rules_match_reviewed_manifest(self):
        expected = set(delivery.load_policy(ROOT / "scripts/source_delivery.json")["reviewedTools"])
        for name in (".gitignore", ".zcodeignore"):
            lines = (ROOT / name).read_text(encoding="utf-8-sig").splitlines()
            actual = {line[len("!/tools/"):].rstrip("/") for line in lines if line.startswith("!/tools/")}
            self.assertEqual(actual, expected)
            self.assertIn("/tools/*", lines)

    def test_actual_git_hook_accepts_reviewed_and_rejects_unreviewed(self):
        self.write("scripts/validate_source_delivery.py", (ROOT / "scripts/validate_source_delivery.py").read_text(encoding="utf-8"))
        self.write("scripts/source_delivery.json", self.policy_path.read_text(encoding="utf-8"))
        self.write("local/source-delivery-policy.json", self.rules_path.read_text(encoding="utf-8"))
        self.write(".gitignore", "/local/\n")
        self.git("config", "core.hooksPath", str(ROOT / ".githooks"))
        protocol = self.repo.parent / (self.repo.name + "-push.txt")
        def call(oid):
            protocol.write_text(f"refs/heads/main {oid} refs/heads/main {self.anchor}\n", encoding="utf-8")
            return subprocess.run(["git", "-C", str(self.repo), "hook", "run", "--to-stdin=" + str(protocol),
                                   "pre-push", "--", "origin", "https://github.com/example/project.git"], capture_output=True)
        self.assertEqual(call(self.anchor).returncode, 0)
        self.write("tools/UnreviewedTool/a.cs", "fixture\n")
        self.assertNotEqual(call(self.commit()).returncode, 0)


if __name__ == "__main__":
    unittest.main()
