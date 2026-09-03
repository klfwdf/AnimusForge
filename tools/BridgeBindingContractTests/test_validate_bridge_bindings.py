from __future__ import annotations

import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "tools" / "BridgeBindingContractTests"

import sys

sys.path.insert(0, str(MODULE))
import validate_bridge_bindings as validator  # noqa: E402


class BridgeBindingManifestTests(unittest.TestCase):
    def load_manifest(self) -> dict:
        return json.loads((ROOT / "docs" / "phase8" / "bridge-binding-manifest.json").read_text(encoding="utf-8"))

    def load_catalog(self) -> dict[str, dict]:
        document = json.loads((ROOT / "docs" / "phase8" / "full-domain-readiness-catalog.json").read_text(encoding="utf-8"))
        return validator.load_catalog(document)

    def test_current_manifest_passes(self) -> None:
        result = validator.run(ROOT)
        self.assertEqual(result["state"], "PASS")
        self.assertEqual(result["bindings"], 16)
        self.assertEqual(result["wired"], 10)
        self.assertEqual(result["declaredOnly"], 6)
        self.assertEqual(result["configEnabled"], 10)

    def load_config(self) -> dict:
        return json.loads((ROOT / "AnimusForge" / "ModuleData" / "FeatureBridges.json").read_text(encoding="utf-8"))

    def assert_config_rejected(self, mutate) -> None:
        config = self.load_config()
        mutate(config)
        with self.assertRaises(validator.BridgeBindingFailure):
            validator.validate_feature_bridge_config(config)

    def test_runtime_config_accepts_explicit_disable_all(self) -> None:
        config = self.load_config()
        config["enabled"] = []
        result = validator.validate_feature_bridge_config(config)
        self.assertEqual(result["configEnabled"], 0)

    def test_runtime_config_rejects_unknown_field(self) -> None:
        self.assert_config_rejected(lambda config: config.update({"unexpected": True}))

    def test_runtime_config_rejects_duplicate_id(self) -> None:
        def mutate(config: dict) -> None:
            config["enabled"].append(config["enabled"][0])

        self.assert_config_rejected(mutate)

    def test_runtime_config_rejects_unwired_id(self) -> None:
        def mutate(config: dict) -> None:
            config["enabled"].append("bootstrap-host")

        self.assert_config_rejected(mutate)

    def test_runtime_config_rejects_noncanonical_id_case(self) -> None:
        def mutate(config: dict) -> None:
            config["enabled"][0] = config["enabled"][0].upper()

        self.assert_config_rejected(mutate)

    def test_runtime_config_rejects_blocked_id(self) -> None:
        def mutate(config: dict) -> None:
            config["enabled"].append("scene-duel")

        self.assert_config_rejected(mutate)

    def test_runtime_config_rejects_bad_version(self) -> None:
        self.assert_config_rejected(lambda config: config.update({"contractVersion": 99}))

    def test_runtime_config_rejects_duplicate_json_field(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "FeatureBridges.json"
            path.write_text(
                '{"schemaVersion":1,"schemaVersion":1,"contractVersion":1,"enabled":[]}',
                encoding="utf-8",
            )
            with self.assertRaises(validator.BridgeBindingFailure):
                validator.load_json(path, "feature bridge runtime config")

    def assert_manifest_rejected(self, mutate) -> None:
        manifest = self.load_manifest()
        mutate(manifest)
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "manifest.json"
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaises(validator.BridgeBindingFailure):
                validator.validate_manifest(manifest, self.load_catalog(), ROOT)

    def test_declared_only_cannot_claim_runtime_entry(self) -> None:
        def mutate(document: dict) -> None:
            document["bindings"][0]["runtimeBinding"]["state"] = "wired"
            document["bindings"][0]["runtimeBinding"]["entryPath"] = "SubModule.cs"
            document["bindings"][0]["runtimeBinding"]["symbol"] = "SubModule"
            document["bindings"][0]["runtimeBinding"]["frequency"] = "startup"

        self.assert_manifest_rejected(mutate)

    def test_absolute_path_is_rejected(self) -> None:
        def mutate(document: dict) -> None:
            document["bindings"][1]["entryPaths"][0] = "F:/outside.cs"

        self.assert_manifest_rejected(mutate)

    def test_terminal_ui_path_is_rejected(self) -> None:
        def mutate(document: dict) -> None:
            document["bindings"][1]["entryPaths"][0] = "AnimusForgeTerminalBehavior.cs"

        self.assert_manifest_rejected(mutate)

    def test_terminal_ui_variant_path_is_rejected(self) -> None:
        def mutate(document: dict) -> None:
            document["bindings"][1]["entryPaths"][0] = "TerminalWeeklyReportBrowserPopup.cs"

        self.assert_manifest_rejected(mutate)

    def test_unreviewed_bridge_cannot_be_wired(self) -> None:
        def mutate(document: dict) -> None:
            binding = next(item for item in document["bindings"] if item["id"] == "scene-duel")
            binding["runtimeBinding"] = {
                "state": "wired",
                "entryPath": "SceneTauntBehavior.cs",
                "symbol": "SceneTauntBehavior",
                "frequency": "event",
                "notes": "test",
            }

        self.assert_manifest_rejected(mutate)

    def test_missing_symbol_is_rejected(self) -> None:
        def mutate(document: dict) -> None:
            document["bindings"][2]["symbols"][0]["symbol"] = "DefinitelyMissingSymbol"

        self.assert_manifest_rejected(mutate)

    def test_gate_in_other_method_is_rejected(self) -> None:
        def mutate(document: dict) -> None:
            source_path = ROOT / "Refactor" / "Adapters" / "LegacyKnowledgeRagGateway.cs"
            original = source_path.read_text(encoding="utf-8")
            altered = original.replace(
                "if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.GatewayKnowledgeProfile))",
                "// gate moved out of the reviewed method\n        if (false)",
                1,
            ).replace(
                "    public Task<LlmGenerateResult> GenerateAsync(",
                "    private static bool FakeGate() => FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.GatewayKnowledgeProfile);\n\n    public Task<LlmGenerateResult> GenerateAsync(",
                1,
            )
            with tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                target = root / "Refactor" / "Adapters" / "LegacyKnowledgeRagGateway.cs"
                target.parent.mkdir(parents=True)
                target.write_text(altered, encoding="utf-8")
                # Copy the minimum project files needed by the validator and
                # replace only the reviewed source path through a temp project.
                for relative in ("docs/phase8/bridge-binding-manifest.json", "docs/phase8/full-domain-readiness-catalog.json", "AnimusForge/ModuleData/FeatureBridges.json"):
                    destination = root / relative
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    destination.write_text((ROOT / relative).read_text(encoding="utf-8"), encoding="utf-8")
                manifest = self.load_manifest()
                # The full validator requires all source files; this focused
                # parser assertion exercises the method-body contract directly.
                with self.assertRaises(validator.BridgeBindingFailure):
                    validator.validate_wired_method_contract(
                        "gateway-knowledge-profile", altered,
                        "GenerateAsync", "FeatureBridgeIds.GatewayKnowledgeProfile",
                    )

    def test_gate_after_side_effect_is_rejected(self) -> None:
        source = (ROOT / "Refactor" / "Adapters" / "LegacyKnowledgeRagGateway.cs").read_text(encoding="utf-8")
        altered = source.replace(
            "if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.GatewayKnowledgeProfile))",
            "if (false)",
            1,
        ).replace(
            "        return _configuredGateway.GenerateAsync(request, cancellationToken);",
            "        _configuredGateway.GenerateAsync(request, cancellationToken);\n        if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.GatewayKnowledgeProfile)) { return Task.FromResult(new LlmGenerateResult()); }\n        return Task.FromResult(new LlmGenerateResult());",
            1,
        )
        with self.assertRaises(validator.BridgeBindingFailure):
            validator.validate_wired_method_contract(
                "gateway-knowledge-profile", altered,
                "GenerateAsync", "FeatureBridgeIds.GatewayKnowledgeProfile",
            )

    def test_wrong_bridge_id_is_rejected(self) -> None:
        source = (ROOT / "Refactor" / "Adapters" / "LegacyKnowledgeRagGateway.cs").read_text(encoding="utf-8")
        altered = source.replace(
            "FeatureBridgeIds.GatewayKnowledgeProfile",
            "FeatureBridgeIds.ConversationGateway",
        )
        with self.assertRaises(validator.BridgeBindingFailure):
            validator.validate_wired_method_contract(
                "gateway-knowledge-profile", altered,
                "GenerateAsync", "FeatureBridgeIds.GatewayKnowledgeProfile",
            )

    def test_fake_call_without_method_declaration_is_rejected(self) -> None:
        with self.assertRaises(validator.BridgeBindingFailure):
            validator.validate_wired_method_contract(
                "gateway-knowledge-profile",
                "// GenerateAsync(FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.GatewayKnowledgeProfile));",
                "GenerateAsync", "FeatureBridgeIds.GatewayKnowledgeProfile",
            )

    def test_cached_siege_gate_requires_initializer(self) -> None:
        source = (ROOT / "AfGcczShoutBridge.cs").read_text(encoding="utf-8")
        altered = source.replace(
            "FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.ConversationSiege)",
            "FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.ConversationGateway)",
            1,
        )
        with self.assertRaises(validator.BridgeBindingFailure):
            validator.validate_wired_method_contract(
                "conversation-siege", altered,
                "IsActive", "FeatureBridgeIds.ConversationSiege",
                cached_gate="ConversationSiegeBridgeEnabled",
            )


if __name__ == "__main__":
    unittest.main()
