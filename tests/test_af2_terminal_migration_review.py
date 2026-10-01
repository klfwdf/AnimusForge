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
        source = (review.ROOT / relative).read_text(encoding="utf-8-sig")
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


if __name__ == "__main__":
    unittest.main()
