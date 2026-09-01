"""Synthetic fixture-only evidence tests; no Bannerlord, network, or real saves."""

from __future__ import annotations

import copy
import hashlib
import json
import shutil
import sys
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

import readiness


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
NOW = datetime(2026, 9, 1, 8, 0, tzinfo=timezone.utc)
COMMIT = "a" * 40
VERSION = "0.0.0-fixture"
STATE = readiness.SourceState(COMMIT, True, True)


class ReadinessTests(unittest.TestCase):
    def setUp(self) -> None:
        # All test writes and automatic cleanup stay under this owned tool path.
        self.temporary = tempfile.TemporaryDirectory(prefix=".fixture-", dir=HERE)
        self.root = Path(self.temporary.name).resolve()
        self.assertTrue(self.root.is_relative_to(HERE))
        self.addCleanup(self.temporary.cleanup)
        for relative in readiness.POLICY_FILES:
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(PROJECT / relative, target)
        domain_catalog = json.loads((PROJECT / readiness.DOMAIN_CATALOG).read_text(encoding="utf-8"))
        cleanup_catalog = json.loads((PROJECT / readiness.CLEANUP_CATALOG).read_text(encoding="utf-8"))
        referenced_paths = {
            path
            for item in domain_catalog["domains"] + domain_catalog["bridges"]
            for path in item["entryPaths"]
        } | {item["path"] for item in cleanup_catalog["candidates"]} | {domain_catalog["programSource"]}
        for relative in sorted(referenced_paths):
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            try:
                target.hardlink_to(PROJECT / relative)
            except OSError:
                shutil.copyfile(PROJECT / relative, target)
        (self.modules, self.bridges, self.composition_cases, self.domains,
         self.full_bridges, self.cleanup_inventory) = readiness.load_policy(readiness.EvidenceFiles(self.root, None))
        self.write("fixture.log", b"FIXTURE ONLY: not a game observation.\n")
        self.review_candidate = next(item for item in self.cleanup_inventory.values()
                                     if item["disposition"] == "REVIEW_REMOVAL")
        all_domain_reviewers = {maintainer for item in self.domains.values() for maintainer in item["maintainers"]}
        all_full_bridge_cases = {case for item in self.full_bridges.values() for case in item["requiredCases"]}
        self.records: dict[str, dict] = {}
        artifacts = []
        for artifact_id in sorted(readiness.ARTIFACT_IDS):
            path = "artifacts/" + artifact_id + ".fixture"
            self.write(path, ("FIXTURE ONLY: " + artifact_id).encode())
            api = artifact_id.removeprefix("implementation-") if artifact_id.startswith("implementation-") else "agnostic"
            artifacts.append({"id": artifact_id, "file": self.ref(path), "sourceCommit": COMMIT, "version": VERSION, "apiLine": api})
        self.artifacts = {item["id"]: item for item in artifacts}
        for module_id in self.modules:
            for layer in readiness.LAYERS:
                lines = ("1.3", "1.4") if layer in ("LIVE", "SAVE") else ("agnostic",)
                for api in lines:
                    record_id = module_id + "/" + layer + "/" + api
                    record = {
                        "schemaVersion": 1, "id": record_id, "mode": "fixture",
                        "moduleId": module_id, "layer": layer, "apiLine": api,
                        "kind": {"OFFLINE": "contract", "LIVE": "game-scenario", "SAVE": "save-roundtrip", "RELEASE": "package-validation"}[layer],
                        "result": "PASS", "sourceCommit": COMMIT,
                        "domainIds": sorted(self.domains), "cleanupCandidateIds": [],
                        "releaseVersion": VERSION,
                        "recordedAt": (NOW - timedelta(hours=1)).isoformat(),
                        "steps": ["FIXTURE ONLY: no game operation was performed."],
                        "expected": "FIXTURE ONLY: expected sentinel.",
                        "observed": "FIXTURE ONLY: observed sentinel.",
                        "caseIds": sorted((self.composition_cases if module_id == "af.foundation.runtime"
                                           else self.bridges.get(module_id, set())) | all_full_bridge_cases),
                        "artifactHashes": {key: item["file"]["sha256"] for key, item in self.artifacts.items()},
                        "attachments": [self.ref("fixture.log")],
                        "environment": {"hostInitialized": True, "hostContext": "Campaign", "gameBuildInfo": "v" + api + ".0.0", "saveIdentity": "FIXTURE-NOT-A-SAVE", "oldSaveLoaded": True, "roundTripVerified": True},
                        "ownerReview": {"decision": "ACCEPTED",
                                        "reviewers": sorted(set(self.modules[module_id]["maintainers"]) | all_domain_reviewers),
                                        "reviewedAt": NOW.isoformat(), "note": "FIXTURE ONLY: no actual owner attestation."},
                    }
                    self.records[record_id] = record
        self.cleanup_id = "af.foundation.runtime/OFFLINE/agnostic"
        self.rollback_id = "af.foundation.runtime/RELEASE/rollback-drill"
        self.records[self.cleanup_id]["kind"] = "cleanup-inventory"
        self.records[self.rollback_id] = copy.deepcopy(self.records["af.foundation.runtime/RELEASE/agnostic"])
        self.records[self.rollback_id]["id"] = self.rollback_id
        self.records[self.rollback_id]["kind"] = "rollback-drill"
        self.records[self.rollback_id]["rollbackTargetCommit"] = "b" * 40
        self.document = {
            "schemaVersion": 1, "mode": "fixture", "releaseVersion": VERSION,
            "source": {"commit": COMMIT, "files": [self.ref(path) for path in readiness.POLICY_FILES]},
            "artifacts": artifacts, "evidence": [],
            "cleanup": {"auditEvidenceId": self.cleanup_id, "candidates": []},
            "rollback": {"commit": "b" * 40, "evidenceId": self.rollback_id, "saveSideEffectsNotUndone": True},
        }

    def write(self, path: str, data: bytes) -> None:
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    def ref(self, path: str) -> dict:
        return {"root": "project", "path": path, "sha256": hashlib.sha256((self.root / path).read_bytes()).hexdigest()}

    def materialize(self) -> Path:
        self.document["evidence"] = []
        for index, record in enumerate(self.records.values()):
            path = f"evidence/{index}.json"
            self.write(path, json.dumps(record).encode())
            self.document["evidence"].append(self.ref(path))
        self.write("manifest.json", json.dumps(self.document).encode())
        return self.root / "manifest.json"

    def evaluate(self, state: readiness.SourceState = STATE) -> dict:
        return readiness.evaluate(self.root, self.materialize(), now=NOW, source_state=state)

    def blocked(self, code: str, state: readiness.SourceState = STATE) -> dict:
        result = self.evaluate(state)
        self.assertEqual("BLOCKED", result["status"])
        self.assertIn(code, {issue["code"] for issue in result["blockingIssues"]})
        self.assertFalse(any(result["authorization"].values()))
        return result

    def live(self) -> dict:
        return self.records["af.module.conversation/LIVE/1.3"]

    def candidate(self, disposition: str = "REVIEW_REMOVAL") -> dict:
        inventory_id = self.review_candidate["id"]
        replacement = [key for key in self.records if key.startswith("af.foundation.runtime/")]
        for record_id in replacement:
            self.records[record_id]["cleanupCandidateIds"] = sorted(
                set(self.records[record_id].get("cleanupCandidateIds", [])) | {inventory_id})
        self.records[self.rollback_id]["cleanupCandidateIds"] = sorted(
            set(self.records[self.rollback_id].get("cleanupCandidateIds", [])) | {inventory_id})
        candidate = {
            "inventoryCandidateId": inventory_id,
            "file": self.ref(self.review_candidate["path"]), "moduleId": "af.foundation.runtime",
            "auditEvidenceId": self.cleanup_id, "disposition": disposition,
            "rationale": "FIXTURE ONLY: reviewed synthetic candidate.",
            "activeCallers": [], "dynamicEntryPoints": [], "saveIdentityRequired": False,
            "replacementEvidenceIds": replacement,
            "rollback": {"commit": "b" * 40, "evidenceId": self.rollback_id, "saveSideEffectsNotUndone": True},
        }
        self.document["cleanup"]["candidates"] = [candidate]
        return candidate

    def test_complete_fixture_is_not_release_or_authorization(self) -> None:
        result = self.evaluate()
        self.assertEqual("FIXTURE-VALID", result["status"], result["blockingIssues"])
        self.assertEqual(49, result["acceptedEvidenceCount"])
        self.assertEqual(20, len(result["domains"]))
        self.assertTrue(all(item["layers"]["SAVE"] == "EVIDENCE-ACCEPTED" for item in result["domains"]))
        self.assertFalse(result["fullProjectReleaseReady"])
        self.assertFalse(any(result["authorization"].values()))

    def test_all_missing_is_blocked(self) -> None:
        self.records.clear()
        self.document.update(source={"commit": None, "files": []}, artifacts=[], cleanup={}, rollback={})
        result = self.blocked("MISSING_LIVE")
        self.assertEqual(8, len(result["modules"]))
        self.assertEqual(20, len(result["domains"]))
        self.assertTrue(all(item["layers"]["SAVE"] == "BLOCKED" for item in result["modules"]))
        self.assertTrue(all(item["layers"]["SAVE"] == "BLOCKED" for item in result["domains"]))

    def test_all_twenty_domains_require_explicit_evidence_coverage(self) -> None:
        missing = sorted(self.domains)[-1]
        for record in self.records.values():
            record["domainIds"].remove(missing)
        self.blocked("MISSING_DOMAIN_OFFLINE")

    def test_unknown_domain_id_is_rejected(self) -> None:
        self.live()["domainIds"].append("unknown-domain")
        result = self.evaluate()
        self.assertEqual("BLOCKED", result["status"])
        self.assertTrue(any(issue["code"].startswith("EVIDENCE_") for issue in result["blockingIssues"]))

    def test_domain_maintainer_review_is_required(self) -> None:
        record = next(iter(self.records.values()))
        maintainer = self.domains[record["domainIds"][0]]["maintainers"][0]
        record["ownerReview"]["reviewers"].remove(maintainer)
        self.blocked("EVIDENCE_0")

    def test_fixture_record_cannot_be_promoted_by_manifest_mode(self) -> None:
        self.document["mode"] = "real"
        self.blocked("EVIDENCE_0")

    def test_role_placeholder_owner_blocks_real_readiness(self) -> None:
        self.document["mode"] = "real"
        for record in self.records.values():
            record["mode"] = "real"
        self.blocked("UNASSIGNED_DOMAIN_OWNER")

    def test_dirty_source_blocks_acceptance(self) -> None:
        self.blocked("SOURCE_DIRTY", readiness.SourceState(COMMIT, False, True))

    def test_stale_source_commit(self) -> None:
        self.document["source"]["commit"] = "c" * 40
        self.blocked("SOURCE_COMMIT")

    def test_source_contract_hash_drift(self) -> None:
        path = readiness.CATALOG
        self.write(path, (self.root / path).read_bytes() + b"\n")
        self.blocked("SOURCE_BINDINGS")

    def test_missing_contract_binding(self) -> None:
        self.document["source"]["files"].pop()
        self.blocked("SOURCE_BINDINGS")

    def test_artifact_hash_tampering(self) -> None:
        self.write(self.document["artifacts"][0]["file"]["path"], b"tampered FIXTURE")
        self.blocked("ARTIFACTS")

    def test_artifact_source_mismatch(self) -> None:
        self.document["artifacts"][0]["sourceCommit"] = "c" * 40
        self.blocked("ARTIFACTS")

    def test_record_artifact_hash_mismatch(self) -> None:
        self.live()["artifactHashes"]["bootstrap"] = "0" * 64
        self.blocked("MISSING_LIVE")

    def test_evidence_file_hash_tampering(self) -> None:
        path = self.materialize()
        record_path = self.root / self.document["evidence"][0]["path"]
        record_path.write_bytes(record_path.read_bytes() + b"\n")
        result = readiness.evaluate(self.root, path, now=NOW, source_state=STATE)
        self.assertEqual("BLOCKED", result["status"])
        self.assertIn("EVIDENCE_0", {issue["code"] for issue in result["blockingIssues"]})

    def test_expired_or_future_or_naive_evidence(self) -> None:
        record = self.live()
        for timestamp in ((NOW - timedelta(days=15)).isoformat(), (NOW + timedelta(days=1)).isoformat(), "2026-09-01T07:00:00"):
            with self.subTest(timestamp=timestamp):
                record["recordedAt"] = timestamp
                self.blocked("MISSING_LIVE")

    def test_missing_live_host_is_not_stage_or_process_pass(self) -> None:
        record = self.live()
        for mutation in ({"hostInitialized": False, "gameRunning": True, "installedMatchesStage": True}, {"hostInitialized": True, "hostContext": "MainMenu"}):
            with self.subTest(mutation=mutation):
                record["environment"].update(mutation)
                self.blocked("MISSING_LIVE")

    def test_readiness_kind_cannot_satisfy_live(self) -> None:
        self.live()["kind"] = "readiness"
        self.blocked("MISSING_LIVE")

    def test_mismatched_runtime_api(self) -> None:
        self.live()["environment"]["gameBuildInfo"] = "v1.4.0.0"
        self.blocked("MISSING_LIVE")

    def test_invalid_runtime_build_format(self) -> None:
        self.live()["environment"]["gameBuildInfo"] = "v1.3.garbage"
        self.blocked("MISSING_LIVE")

    def test_save_roundtrip_is_required(self) -> None:
        self.records["af.module.conversation/SAVE/1.3"]["environment"]["roundTripVerified"] = False
        self.blocked("MISSING_SAVE")

    def test_both_runtime_api_lines_are_required(self) -> None:
        del self.records["af.module.conversation/LIVE/1.4"]
        self.blocked("MISSING_LIVE")

    def test_bridge_case_missing(self) -> None:
        self.records["af.bridge.conversation-siege/LIVE/1.3"]["caseIds"].remove("safe-mode")
        self.blocked("BRIDGE_MATRIX")

    def test_bridge_save_case_missing(self) -> None:
        self.records["af.bridge.conversation-siege/SAVE/1.4"]["caseIds"].remove("safe-mode")
        self.blocked("BRIDGE_MATRIX")

    def test_full_domain_bridge_cases_require_offline_live_and_save(self) -> None:
        case = "PACKAGE_ALLOWLIST"
        for record in self.records.values():
            if (record["layer"], record["apiLine"]) == ("SAVE", "1.3") and case in record["caseIds"]:
                record["caseIds"].remove(case)
        self.blocked("FULL_DOMAIN_BRIDGE_MATRIX")

    def test_full_composition_coverage_not_just_bridge_cases(self) -> None:
        self.records["af.foundation.runtime/LIVE/1.4"]["caseIds"].remove("partial-start-failure")
        self.blocked("COMPOSITION_MATRIX")

    def test_rollback_drill_cannot_replace_package_validation(self) -> None:
        for record in self.records.values():
            if record["layer"] == "RELEASE":
                record["kind"] = "rollback-drill"
        self.blocked("MISSING_RELEASE")

    def test_package_validation_cannot_replace_rollback_drill(self) -> None:
        self.records[self.rollback_id]["kind"] = "package-validation"
        self.blocked("ROLLBACK")

    def test_release_versions_must_match_across_artifacts(self) -> None:
        self.artifacts["implementation-1.4"]["version"] = "0.0.1-fixture"
        self.blocked("ARTIFACTS")

    def test_artifact_api_line_must_match_role(self) -> None:
        self.artifacts["implementation-1.4"]["apiLine"] = "1.3"
        self.blocked("ARTIFACTS")

    def test_invalid_release_version_and_record_mismatch(self) -> None:
        self.document["releaseVersion"] = "garbage"
        self.blocked("RELEASE_VERSION")
        self.document["releaseVersion"] = VERSION
        self.live()["releaseVersion"] = "0.0.1-fixture"
        self.blocked("MISSING_LIVE")

    def test_bridge_requires_both_owners(self) -> None:
        self.records["af.bridge.conversation-siege/LIVE/1.3"]["ownerReview"]["reviewers"] = ["conversation-owner"]
        self.blocked("MISSING_LIVE")

    def test_unreviewed_evidence_is_rejected(self) -> None:
        self.live()["ownerReview"]["decision"] = "PENDING"
        self.blocked("MISSING_LIVE")

    def test_missing_log_attachment(self) -> None:
        self.live()["attachments"] = []
        self.blocked("MISSING_LIVE")

    def test_candidate_review_still_does_not_authorize_removal(self) -> None:
        self.candidate()
        result = self.evaluate()
        self.assertEqual("FIXTURE-VALID", result["status"], result["blockingIssues"])
        self.assertFalse(result["authorization"]["delete"])

    def test_active_caller_dynamic_entry_and_save_type_block_removal(self) -> None:
        candidate = self.candidate()
        for name, value in (("activeCallers", ["SubModule.OnGameStart"]), ("dynamicEntryPoints", ["Harmony registration"]), ("saveIdentityRequired", True)):
            with self.subTest(name=name):
                candidate.update(activeCallers=[], dynamicEntryPoints=[], saveIdentityRequired=False)
                candidate[name] = value
                self.blocked("CLEANUP_REVIEW")

    def test_active_legacy_may_be_kept(self) -> None:
        candidate = self.candidate("KEEP")
        candidate.update(activeCallers=["fixture caller"], dynamicEntryPoints=["fixture registration"], saveIdentityRequired=True)
        self.assertEqual("FIXTURE-VALID", self.evaluate()["status"])

    def test_candidate_owner_or_replacement_incomplete(self) -> None:
        candidate = self.candidate()
        candidate["replacementEvidenceIds"].remove("af.foundation.runtime/RELEASE/agnostic")
        self.blocked("CLEANUP_REVIEW")
        candidate["moduleId"] = "af.module.conversation"
        self.blocked("CLEANUP_REVIEW")

    def test_candidate_replacement_and_rollback_bind_exact_inventory_entry(self) -> None:
        candidate = self.candidate()
        replacement_id = candidate["replacementEvidenceIds"][0]
        self.records[replacement_id]["cleanupCandidateIds"] = []
        self.blocked("CLEANUP_REVIEW")
        self.records[replacement_id]["cleanupCandidateIds"] = [candidate["inventoryCandidateId"]]
        self.records[self.rollback_id]["cleanupCandidateIds"] = []
        self.blocked("CLEANUP_REVIEW")

    def test_missing_cleanup_review_blocks_even_empty_candidates(self) -> None:
        self.document["cleanup"]["auditEvidenceId"] = None
        self.blocked("CLEANUP_REVIEW")

    def test_rollback_commit_and_save_warning_required(self) -> None:
        self.blocked("ROLLBACK", readiness.SourceState(COMMIT, True, False))
        self.document["rollback"]["saveSideEffectsNotUndone"] = False
        self.blocked("ROLLBACK")

    def test_current_head_is_not_a_rollback_point(self) -> None:
        self.blocked("ROLLBACK", readiness.SourceState(COMMIT, True, True, True))

    def test_rollback_record_must_bind_exact_target_commit(self) -> None:
        self.document["rollback"]["commit"] = "c" * 40
        self.blocked("ROLLBACK")

    def test_traversal_absolute_ads_and_unknown_root_rejected(self) -> None:
        reference = self.live()["attachments"][0]
        for path in ("../fixture.log", "/fixture.log", "C:/fixture.log", "fixture.log:stream", "artifacts\\..\\fixture.log", "./fixture.log"):
            with self.subTest(path=path):
                reference["path"] = path
                self.blocked("MISSING_LIVE")
        reference.update(path="fixture.log", root="unapproved")
        self.blocked("MISSING_LIVE")

    def test_explicit_artifact_root_is_required(self) -> None:
        artifact_root = self.root / "external-fixture"
        artifact_root.mkdir()
        (artifact_root / "fixture.log").write_bytes(b"FIXTURE external log")
        reference = {"root": "artifact", "path": "fixture.log", "sha256": hashlib.sha256(b"FIXTURE external log").hexdigest()}
        self.live()["attachments"] = [reference]
        self.blocked("MISSING_LIVE")
        result = readiness.evaluate(self.root, self.materialize(), artifact_root, now=NOW, source_state=STATE)
        self.assertEqual("FIXTURE-VALID", result["status"], result["blockingIssues"])

    def test_resolved_junction_or_symlink_escape_is_rejected(self) -> None:
        external = tempfile.TemporaryDirectory(prefix=".fixture-external-", dir=HERE)
        outside = Path(external.name).resolve()
        self.assertTrue(outside.is_relative_to(HERE))
        self.addCleanup(external.cleanup)
        payload = b"FIXTURE ONLY: outside the declared project root"
        (outside / "outside.log").write_bytes(payload)
        link = self.root / "escape-link"
        if sys.platform == "win32":
            import _winapi
            _winapi.CreateJunction(str(outside), str(link))
        else:
            link.symlink_to(outside, target_is_directory=True)
        self.live()["attachments"] = [{"root": "project", "path": "escape-link/outside.log", "sha256": hashlib.sha256(payload).hexdigest()}]
        self.blocked("MISSING_LIVE")

    def test_duplicate_json_keys_rejected(self) -> None:
        path = self.materialize()
        path.write_text('{"schemaVersion":1,"schemaVersion":1,"mode":"fixture"}', encoding="utf-8")
        result = readiness.evaluate(self.root, path, now=NOW, source_state=STATE)
        self.assertEqual("MANIFEST", result["blockingIssues"][0]["code"])

    def test_commands_in_input_are_never_executed(self) -> None:
        sentinel = self.root / "must-not-exist"
        self.live()["command"] = f"powershell Set-Content -LiteralPath '{sentinel}' -Value executed"
        self.assertEqual("FIXTURE-VALID", self.evaluate()["status"])
        self.assertFalse(sentinel.exists())

    def test_malformed_input_is_blocked_without_traceback(self) -> None:
        self.live()["moduleId"] = ["invalid"]
        self.blocked("MISSING_LIVE")
        self.document["source"] = []
        self.blocked("MANIFEST")

    def test_null_and_scalar_cleanup_candidate_are_blocked(self) -> None:
        for candidate in (None, "text", 1, []):
            with self.subTest(candidate=candidate):
                self.document["cleanup"]["candidates"] = [candidate]
                self.blocked("CLEANUP_REVIEW")

    def test_malformed_policy_catalog_is_blocked(self) -> None:
        document = json.loads((self.root / readiness.CATALOG).read_text(encoding="utf-8"))
        document["modules"][0]["id"] = 99
        self.write(readiness.CATALOG, json.dumps(document).encode())
        self.blocked("EXISTING_CONTRACTS")

    def test_full_domain_and_cleanup_catalogs_are_strict(self) -> None:
        document = json.loads((self.root / readiness.DOMAIN_CATALOG).read_text(encoding="utf-8"))
        document["domains"].pop()
        self.write(readiness.DOMAIN_CATALOG, json.dumps(document).encode())
        self.blocked("EXISTING_CONTRACTS")
        shutil.copyfile(PROJECT / readiness.DOMAIN_CATALOG, self.root / readiness.DOMAIN_CATALOG)
        cleanup = json.loads((self.root / readiness.CLEANUP_CATALOG).read_text(encoding="utf-8"))
        cleanup["candidates"][0]["disposition"] = "DELETE_NOW"
        self.write(readiness.CLEANUP_CATALOG, json.dumps(cleanup).encode())
        self.blocked("EXISTING_CONTRACTS")

    def test_domain_required_fields_fail_closed(self) -> None:
        original = json.loads((self.root / readiness.DOMAIN_CATALOG).read_text(encoding="utf-8"))
        mutations = (
            ("owner", ""), ("maintainers", []), ("entryPaths", []),
            ("ownerAssignmentState", "UNKNOWN"),
            ("promptAction", {"prompt": "APPLICABLE"}),
            ("persistence", {"responsibility": "missing key/type declarations"}),
            ("failureFallback", ""), ("defaultState", "UNKNOWN"),
            ("currentEvidence", {"offline": "VERIFY"}), ("blockingGates", []), ("bridgeIds", []),
        )
        for field, value in mutations:
            with self.subTest(field=field):
                document = copy.deepcopy(original)
                document["domains"][0][field] = value
                self.write(readiness.DOMAIN_CATALOG, json.dumps(document).encode())
                self.blocked("EXISTING_CONTRACTS")

    def test_size_limit_rejects_oversized_json(self) -> None:
        with self.assertRaises(readiness.InvalidEvidence):
            readiness.decode_json(b" " * (readiness.MAX_JSON_BYTES + 1))


if __name__ == "__main__":
    unittest.main(verbosity=2)
