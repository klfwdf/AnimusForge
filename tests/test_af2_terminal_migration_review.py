"""Positive and drift-negative controls for the fixed historical review inverse."""
import unittest
from pathlib import Path

import af2_terminal_migration_review as review


class TerminalInverseTests(unittest.TestCase):
    def test_actual_freeze_and_every_exact_inverse(self):
        review.verify_bindings()
        for relative, row in review.packet()["paths"].items():
            with self.subTest(path=relative):
                source = (review.ROOT / relative).read_text(encoding="utf-8-sig")
                restored = review.restore(relative, source)
                self.assertEqual(row["beforeSha256"], review.digest(restored))

    def test_body_neighbor_and_extra_changes_are_rejected(self):
        relative, row = next((path, value) for path, value in review.packet()["paths"].items()
                             if any("return " in edit["after"] for edit in value["edits"]))
        source = review.restore_j17(relative, (review.ROOT / relative).read_text(encoding="utf-8-sig"), require_current=True)
        after = next(edit["after"] for edit in row["edits"] if "return " in edit["after"])
        self.assertIn(after, source)
        mutants = (
            source.replace(after, after.replace("return ", "throw ", 1), 1),
            "// unexpected neighboring declaration\n" + source,
            source + "\n// unexpected extra declaration\n",
        )
        for mutant in mutants:
            with self.assertRaisesRegex(AssertionError, "Unreviewed terminal source"):
                review.restore(relative, mutant)

    def test_new_owner_drift_is_rejected_before_projection(self):
        bindings = review.packet()["bindings"]
        relative = next(path for path in bindings if path not in review.packet()["paths"] and path.endswith(".cs"))
        target = (review.ROOT / relative).resolve()
        read = Path.read_text

        def mutated_read(path, *args, **kwargs):
            source = read(path, *args, **kwargs)
            return source + "\n// unexpected owner drift\n" if Path(path).resolve() == target else source

        with self.assertRaisesRegex(AssertionError, "Unreviewed terminal dependency"):
            review.verify_bindings(mutated_read)

    def test_scoped_projection_is_reentrant_and_restores_reads(self):
        relative, row = next(iter(review.packet()["paths"].items()))
        path = review.ROOT / relative
        source = path.read_text(encoding="utf-8-sig")
        with review.projection_reads():
            with review.projection_reads():
                self.assertEqual(row["beforeSha256"], review.digest(path.read_text(encoding="utf-8-sig")))
        self.assertEqual(source, path.read_text(encoding="utf-8-sig"))


class J17InverseAlgorithmTests(unittest.TestCase):
    def sample(self):
        path = "src/Exact.cs"
        freeze = "class Exact { int Value() { return 1; } }\n"
        current = "class Exact { int Value() { return Named.Read(); } }\n"
        row = dict(freezeSha256=review.digest(freeze), currentSha256=review.digest(current),
                   edits=[dict(before="return 1;", after="return Named.Read();", symbols=["Exact.Value"])])
        return path, freeze, current, row

    def scoped(self, path, row):
        from contextlib import ExitStack
        from unittest.mock import patch
        stack = ExitStack()
        stack.enter_context(patch.object(review, "packet", return_value={"bindings": {path: row["freezeSha256"]}}))
        stack.enter_context(patch.object(review, "j17_packet", return_value={"paths": {path: row}}))
        return stack

    def test_j17_exact_two_stage_input(self):
        path, freeze, current, row = self.sample()
        with self.scoped(path, row):
            self.assertEqual(freeze, review.restore_j17(path, current, require_current=True))
            self.assertEqual(freeze, review.restore_j17(path, freeze))
            self.assertEqual("unbound", review.restore_j17("src/Other.cs", "unbound"))

    def test_j17_body_neighbor_extra_and_replaced_freeze_rejected(self):
        path, freeze, current, row = self.sample()
        with self.scoped(path, row):
            for value in [current.replace("Named.Read()", "Named.Wrong()"), "// neighbor\n" + current, current + "// extra\n", freeze]:
                with self.subTest(value=value), self.assertRaisesRegex(AssertionError, "Unreviewed terminal source J17"):
                    review.restore_j17(path, value, require_current=True)

    def test_j17_unique_context_and_symbol_required(self):
        path, freeze, current, row = self.sample()
        row["edits"][0]["symbols"] = []
        with self.scoped(path, row), self.assertRaisesRegex(AssertionError, "named symbols"):
            review.restore_j17(path, current)
        row["edits"][0]["symbols"] = ["Exact.Value"]
        row["edits"][0]["after"] = "not present"
        with self.scoped(path, row), self.assertRaisesRegex(AssertionError, "Unreviewed terminal context"):
            review.restore_j17(path, current)

    def test_j17_incomplete_inverse_and_original_binding_rejected(self):
        path, freeze, current, row = self.sample()
        row["edits"][0]["before"] = "return 2;"
        with self.scoped(path, row), self.assertRaisesRegex(AssertionError, "Incomplete J17 terminal inverse"):
            review.restore_j17(path, current)
        row["edits"][0]["before"] = "return 1;"
        from unittest.mock import patch
        with self.scoped(path, row), patch.object(review, "packet", return_value={"bindings": {path: "wrong"}}), self.assertRaisesRegex(AssertionError, "original packet"):
            review.restore_j17(path, current)


class J17PacketBindingTests(unittest.TestCase):
    def values(self):
        import hashlib
        old = b"immutable old packet"
        original = {"freeze": "freeze", "bindings": {"src/Old.cs": "old"}}
        value = {"schemaVersion": 1, "purpose": "approved-j17-current-to-terminal-freeze-inverse",
                 "freeze": "freeze", "oldPacketSha256": hashlib.sha256(old).hexdigest(),
                 "paths": {}, "approvedPaths": ["src/NewOwner.cs"],
                 "requiredOwnerPaths": ["src/NewOwner.cs"],
                 "currentOwnerBindings": {"src/NewOwner.cs": review.digest("actual owner")}}
        return old, original, value

    def loaded(self, value, old, original):
        import json
        from unittest.mock import patch
        with patch.object(review, "packet", return_value=original), patch.object(Path, "read_bytes", return_value=old), patch.object(Path, "read_text", return_value=json.dumps(value)):
            return review.j17_packet.__wrapped__()

    def test_packet_guards_reject_schema_purpose_freeze_old_and_unapproved_paths(self):
        import copy
        old, original, value = self.values()
        self.assertEqual(value, self.loaded(value, old, original))
        for field, invalid in [("schemaVersion",2),("purpose","wrong"),("freeze","wrong"),("oldPacketSha256","wrong"),("paths",{"src/Outside.cs":{}}),("approvedPaths",[])]:
            changed=copy.deepcopy(value);changed[field]=invalid
            with self.subTest(field=field), self.assertRaises(AssertionError):
                self.loaded(changed, old, original)

    def test_required_migrated_owner_cannot_be_unbound(self):
        old, original, value = self.values()
        value["currentOwnerBindings"]={}
        with self.assertRaisesRegex(AssertionError,"Unbound J17 migrated owner"):
            self.loaded(value,old,original)

    def test_independent_paths_require_approval_and_physical_current_binding(self):
        import copy
        old, original, value = self.values()
        value["independentLayers"]={"F5":{"paths":{"src/NewOwner.cs":{}}}}
        self.assertEqual(value,self.loaded(value,old,original))
        changed=copy.deepcopy(value)
        changed["independentLayers"]["F5"]["paths"]={"src/Outside.cs":{}}
        with self.assertRaisesRegex(AssertionError,"Unapproved independent path"):
            self.loaded(changed,old,original)
        changed=copy.deepcopy(value)
        changed["approvedPaths"].append("src/Unbound.cs")
        changed["independentLayers"]["F5"]["paths"]={"src/Unbound.cs":{}}
        with self.assertRaisesRegex(AssertionError,"Unbound independent current input"):
            self.loaded(changed,old,original)

    def test_actual_new_owner_drift_rejected_before_projection(self):
        from unittest.mock import patch
        old, original, value = self.values()
        original["bindings"]={}
        with patch.object(review,"packet",return_value=original),patch.object(review,"j17_packet",return_value=value):
            review.verify_bindings(lambda path,**kwargs:"actual owner")
            for mutant in ["wrong owner", "actual owner\n// neighboring declaration", "actual owner\n// extra declaration"]:
                with self.subTest(mutant=mutant), self.assertRaisesRegex(AssertionError,"Unreviewed J17 current owner"):
                    review.verify_bindings(lambda path,**kwargs:mutant)

class IndependentLayerTests(unittest.TestCase):
    def sample(self):
        path = "src/Input.cs"
        current = "class Input { int Value() { return Named.Read(); } }\n"
        target = "class Input { int Value() { return 1; } }\n"
        row = {"sourceSha256": review.digest(current), "targetSha256": review.digest(target),
               "edits": [{"after": "return Named.Read();", "before": "return 1;", "symbols": ["Input.Value"]}]}
        data = {"independentLayers": {"F5": {"paths": {path: row}}}}
        return path, current, target, row, data

    def test_exact_named_inverse_and_unchanged_prior_composition(self):
        from unittest.mock import patch
        path, current, target, row, data = self.sample()
        with patch.object(review, "j17_packet", return_value=data):
            self.assertEqual(target, review.restore_independent_layer("F5", path, current))
            self.assertEqual(target, review.restore_independent_layer("F5", path, target))
            self.assertEqual("unrelated", review.restore_independent_layer("F5", "src/Other.cs", "unrelated"))

    def test_body_neighbor_extra_and_wrong_layer_rejected(self):
        from unittest.mock import patch
        path, current, target, row, data = self.sample()
        with patch.object(review, "j17_packet", return_value=data):
            for mutant in [current.replace("Named.Read", "Named.Wrong"), "// neighbor\n"+current, current+"// extra\n"]:
                with self.subTest(mutant=mutant), self.assertRaisesRegex(AssertionError, "Unreviewed independent source"):
                    review.restore_independent_layer("F5", path, mutant)
            with self.assertRaises(KeyError):
                review.restore_independent_layer("F3", path, current)

    def test_duplicate_context_and_incomplete_inverse_rejected(self):
        from unittest.mock import patch
        path, current, target, row, data = self.sample()
        duplicate=current+current
        row["sourceSha256"]=review.digest(duplicate)
        with patch.object(review,"j17_packet",return_value=data), self.assertRaisesRegex(AssertionError,"Unreviewed independent context"):
            review.restore_independent_layer("F5",path,duplicate)
        row["sourceSha256"]=review.digest(current)
        row["edits"][0]["before"]="return 2;"
        with patch.object(review,"j17_packet",return_value=data), self.assertRaisesRegex(AssertionError,"Incomplete independent inverse"):
            review.restore_independent_layer("F5",path,current)

    def test_scoped_reentry_and_read_restore_after_exception(self):
        from unittest.mock import patch
        from contextlib import nullcontext
        path,current,target,row,data=self.sample()
        real_read=Path.read_text
        with patch.object(review,"j17_packet",return_value=data), patch.object(review,"projection_reads",side_effect=lambda:nullcontext()), patch.object(Path,"read_text",return_value=current):
            parent_read=Path.read_text
            with self.assertRaisesRegex(RuntimeError,"expected exit"):
                with review.independent_projection_reads("F5"):
                    with review.independent_projection_reads("F5"):
                        self.assertEqual(target,(review.ROOT/path).read_text())
                    self.assertEqual(("F5",),review._independent_active.get())
                    raise RuntimeError("expected exit")
            self.assertIs(parent_read,Path.read_text)
            self.assertEqual(current,(review.ROOT/path).read_text())
            self.assertEqual((),review._independent_active.get())
        self.assertIs(real_read,Path.read_text)


if __name__ == "__main__":
    unittest.main()
