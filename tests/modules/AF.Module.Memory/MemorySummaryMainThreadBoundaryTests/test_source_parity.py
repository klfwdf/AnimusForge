"""Read-only guards for the scoped B1 inverse; never edit production to test rejection."""
from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path
from af2_terminal_migration_review import historical_fixture
import importlib.util
import json
import subprocess
import unittest
from unittest.mock import patch

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
spec = importlib.util.spec_from_file_location("b1_inverse_under_test", HERE / "source_parity.py")
inverse = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inverse)
from f3_migration_projection import read_current
SOURCE = read_current("MyBehavior.cs")
REVIEW = json.loads((HERE / "source-review-b1.json").read_text(encoding="utf-8"))
BASELINE = subprocess.check_output(
    ["git", "show", REVIEW["baseline"] + ":MyBehavior.cs"], cwd=ROOT
).decode("utf-8-sig").replace("\r\n", "\n")


class InverseGuards(unittest.TestCase):
    def setUp(self):
        # All immutable physical/upstream and positive B1 checks complete before
        # any negative assertion or target-only mock can be installed.
        context = inverse.bound_b1_review_input()
        self.bound = context.__enter__()
        self.addCleanup(context.__exit__, None, None, None)
        self.source = self.bound.source
        self.upstream_stages = self.bound.upstream_stages

    def reject(self, source, message):
        with self.assertRaisesRegex(AssertionError, message):
            self.bound.verify(source)

    @historical_fixture
    def test_exact_whole_baseline(self):
        self.assertEqual(BASELINE, self.bound.verify(self.source))

    @historical_fixture
    def test_queue_normalization_body_is_exact_old_semantics(self):
        spec = importlib.util.spec_from_file_location("normalizer_extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
        extractor = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(extractor)
        for model, suffix, day in [("MemorySummaryJob", "MemorySummaryQueue", "GameDayIndex"),
                                   ("MajorActionSummaryJob", "MajorActionSummaryQueue", "TriggerGameDayIndex")]:
            signature = "private static List<" + model + "> "
            prior = extractor.declaration(BASELINE, signature + "Sanitize" + suffix + "(")
            actual = extractor.declaration(self.source, signature + "Normalize" + suffix + "(")
            restored = actual.replace("Normalize" + suffix, "Sanitize" + suffix, 1).replace(
                "return list;", "return list.OrderBy((" + model + " x) => x." + day +
                ").ThenBy((" + model + " x) => x.HeroName).ToList();", 1)
            self.assertEqual(prior, restored)  # No mutation/filter/dedupe body rewrite hidden by wrapper inverse.

    @historical_fixture
    def test_raw_input_four_declaration_inverse(self):
        review = REVIEW["rawSourceFingerprintReview"]
        text = self.bound.fixture_input(review["inputPath"])
        baseline = subprocess.check_output(["git", "show", review["baseline"] + ":" + review["inputPath"]], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
        spec = importlib.util.spec_from_file_location("raw_input_extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
        extractor = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(extractor)
        for item in review["inputDeclarations"]:
            current = extractor.declaration(text, item["signature"])
            old = extractor.declaration(baseline, item["signature"])
            self.assertEqual(inverse._sha256(current), item["sha256"])
            self.assertEqual(inverse._sha256(old), item["baselineSha256"])
            text = text.replace(current, old, 1)
        self.assertEqual(text, baseline)  # Includes unchanged generic JSON/editor/plan hash and async/parse/release bodies.

    @historical_fixture
    def test_single_draft_line_and_bind_are_exact_previous_bodies(self):
        spec = importlib.util.spec_from_file_location("draft_entry_extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
        extractor = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(extractor)
        old = subprocess.check_output(["git", "show", "40b92e67:MyBehavior.cs"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
        old_drafts = extractor.declaration(old, "private static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(")
        line = extractor.declaration(self.source, "private static DailyMemoryLine SanitizeDailyMemoryDraftLine(")
        bind = extractor.declaration(self.source, "private static void BindDailyMemoryDraftWeeklyTrigger(")
        entry = extractor.declaration(self.source, "private static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(")
        select = old_drafts[old_drafts.index("x.GameDayIndex = draft.GameDayIndex;"):old_drafts.index("return x;")]
        def strip_indent(text):
            return "\n".join(part.lstrip("\t") for part in text.splitlines())
        self.assertIn(strip_indent(select).strip(), strip_indent(line))
        self.assertIn('trigger.MemoryId = memoryId;', bind)
        self.assertIn('trigger.GameDayIndex = gameDayIndex;', bind)
        self.assertIn('trigger.GameDate = string.IsNullOrWhiteSpace(trigger.GameDate) ? gameDate : trigger.GameDate;', bind)
        self.assertIn("BindDailyMemoryDraftWeeklyTrigger(trigger, text, draft.GameDayIndex, draft.GameDate);", entry)
        self.assertIn("SanitizeDailyMemoryDraftLine(sourceLine, draft);", entry)
        self.assertNotIn(").Where((DailyMemoryLine x)", entry)

    @historical_fixture
    def test_inner_structure_fix_has_narrow_inverse(self):
        review = REVIEW["innerStructureReview"]
        baseline = subprocess.check_output(["git", "show", review["baseline"] + ":" + review["path"]], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
        for edit in review["exactEdits"]:
            self.assertEqual(baseline.count(edit["before"]), 1)
            baseline = baseline.replace(edit["before"], edit["after"], 1)
        self.assertEqual(baseline, self.bound.fixture_input(review["path"]))

    @historical_fixture
    def test_dispatcher_dependency_direction_and_host_shape(self):
        runtime = (ROOT / "src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs").read_text(encoding="utf-8-sig")
        host = (current_source_path(ROOT, "MyBehavior.MemorySummaryMainThread.cs")).read_text(encoding="utf-8-sig")
        self.assertNotIn("TaleWorlds", runtime)
        self.assertNotIn("MyBehavior", runtime)
        self.assertNotIn("ConcurrentQueue", host)
        self.assertNotIn("TaskCompletionSource", host)
        self.assertNotIn("HasMemorySummaryMainThreadAllowance", host)
        self.assertIn("MemorySummaryDispatch.Submit(generation, operation)", host)
        self.assertIn("MemorySummaryDispatch.SubmitCompletion(generation, operation)", host)

    @historical_fixture
    def test_planner_only_changes_elapsed_owner_read(self):
        old = subprocess.check_output(["git", "show", "9617f96a:MyBehavior.MemorySummaryPlanning.cs"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
        self.assertEqual(old.count("_memorySummaryMainThreadElapsedTicks"), 2)
        self.assertEqual(old.replace("_memorySummaryMainThreadElapsedTicks", "MemorySummaryDispatchElapsedTicks"),
                         self.bound.fixture_input("MyBehavior.MemorySummaryPlanning.cs"))

    @historical_fixture
    def test_changed_accepted_body(self):
        self.reject(self.source.replace("_eventSourceMaterialIndexBinding.Build(source);",
                                   "_eventSourceMaterialIndexBinding.Build(null);", 1),
                    "Unreviewed B1 declaration")

    @historical_fixture
    def test_added_composition_span_drift(self):
        self.reject(self.source.replace("item => item.Day, item => item.StableKey",
                                   "item => 0, item => item.StableKey", 1),
                    "Unreviewed B1 added source span")

    @historical_fixture
    def test_duplicate_composition_span(self):
        self.reject(self.source + REVIEW["addedSourceSpans"][0]["text"],
                    "Unreviewed B1 added source span")

    @historical_fixture
    def test_unlisted_surrounding_change(self):
        self.reject(self.source + "\n// unreviewed extra source\n",
                    "Unreviewed B1 surrounding source changes")

    @historical_fixture
    def test_deleted_original_cannot_return(self):
        extractor = importlib.util.spec_from_file_location(
            "guard_extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
        module = importlib.util.module_from_spec(extractor)
        extractor.loader.exec_module(module)
        body = module.declaration(BASELINE, "private bool HasPastDailyMemoryDrafts(")
        self.reject(self.source.replace("public override void SyncData(IDataStore dataStore)",
                                   body + "\npublic override void SyncData(IDataStore dataStore)", 1),
                    "Deleted B1 declaration unexpectedly restored")

    @historical_fixture
    def test_added_campaign_scope_cannot_drift(self):
        self.reject(self.source.replace("_campaignMemoryMaintenanceCycleActive = true;",
                                   "_campaignMemoryMaintenanceCycleActive = false;", 1),
                    "Unreviewed B1 added source span")

    @historical_fixture
    def test_whole_components_and_test_inputs_are_locked(self):
        original = Path.read_text
        targets = list(REVIEW["productionDependencies"]) + [
            REVIEW["evidence"]["materials"]["runner"],
            REVIEW["evidence"]["sealing"]["harness"],
            "tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/CapturedHarness.cs.txt",
        ]
        for target in targets:
            with self.subTest(path=target):
                if target in {
                    "MyBehavior.MemorySummaryInput.cs",
                    "src/modules/AF.Module.Memory/Summary/MemorySourceFingerprintWriter.cs",
                    "MyBehavior.MemorySummaryMainThread.cs",
                    "MyBehavior.MemorySummaryPlanning.cs",
                }:
                    # The bound context has already verified every upstream layer.
                    # Mutate only its original B1 dependency input, not physical reads.
                    with self.bound.dependency_mutation(target):
                        self.reject(self.source, "Unreviewed B1 (production dependency|evidence)")
                    continue
                path = current_source_path(ROOT, target)
                def changed(file, *args, **kwargs):
                    text = original(file, *args, **kwargs)
                    return text + "\n// unreviewed dependency\n" if file == path else text
                with patch.object(Path, "read_text", changed):
                    self.reject(self.source, "Unreviewed B1 (production dependency|evidence)")

    @historical_fixture
    def test_obsolete_partial_cannot_reenter(self):
        original = Path.exists
        removed = ROOT / "MyBehavior.EventSourceMaterialIndex.cs"
        with patch.object(Path, "exists", lambda p: True if p == removed else original(p)):
            self.reject(self.source, "Obsolete B1 production file restored")


class CurrentScopeGuards(InverseGuards):
    """Explicit current Memory-only successor; original historical tests remain unchanged."""
    RECORDS = 'src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs'
    def setUp(self):
        # This independently scoped current-source suite retains its own producer
        # and verifier; it is not a substitute for the historical B1 target.
        self.source = SOURCE


    def extract(self,text,signature):
        spec=importlib.util.spec_from_file_location('finite_memory_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        return module.declaration(text,signature)

    def reject(self,source,message):
        with self.assertRaisesRegex(AssertionError,message):
            inverse.verify_current_memory_source(source)

    @historical_fixture
    def test_exact_whole_baseline(self):
        self.assertTrue(inverse.verify_current_memory_source(SOURCE))
        # Deliberately not an assertion of whole-host inverse equivalence.

    @historical_fixture
    def test_queue_normalization_body_is_exact_old_semantics(self):
        owner=(ROOT/self.RECORDS).read_text(encoding='utf-8-sig')
        for model,suffix,day in [('MemorySummaryJob','MemorySummaryQueue','GameDayIndex'),('MajorActionSummaryJob','MajorActionSummaryQueue','TriggerGameDayIndex')]:
            current=self.extract(owner,'internal static List<'+model+'> Normalize'+suffix+'(')
            old=self.extract(BASELINE,'private static List<'+model+'> Sanitize'+suffix+'(')
            restored=current.replace('internal static','private static',1).replace('Normalize'+suffix,'Sanitize'+suffix,1).replace('return list;','return list.OrderBy(('+model+' x) => x.'+day+').ThenBy(('+model+' x) => x.HeroName).ToList();',1)
            self.assertEqual(old,restored)
            host=self.extract(SOURCE,'private static List<'+model+'> Sanitize'+suffix+'(')
            self.assertIn('MemoryRecordRules.Sanitize'+suffix+'(',host)

    @historical_fixture
    def test_raw_input_four_declaration_inverse(self):
        review=REVIEW['rawSourceFingerprintReview']
        current=read_current(review['inputPath'])
        accepted=inverse.current_scope_baseline(review['inputPath'])
        old=subprocess.check_output(['git','show',review['baseline']+':'+review['inputPath']],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        for item in review['inputDeclarations']:
            self.assertEqual(inverse._sha256(self.extract(old,item['signature'])),item['baselineSha256'])
            self.assertEqual(self.extract(current,item['signature']),self.extract(accepted,item['signature']))
        # Current DTO/copy/framing parity is executed by Budget's real 65,808-check pair.
        self.assertIn('ComputeMemorySummarySourceFingerprint(source)',current)

    @historical_fixture
    def test_single_draft_line_and_bind_are_exact_previous_bodies(self):
        owner=(ROOT/self.RECORDS).read_text(encoding='utf-8-sig')
        old=subprocess.check_output(['git','show','40b92e67:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        drafts=self.extract(old,'private static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(')
        line=self.extract(owner,'internal static DailyMemoryLine SanitizeDailyMemoryDraftLine(')
        old_lines=drafts[drafts.index('x.GameDayIndex = draft.GameDayIndex;'):drafts.index('return x;')]
        self.assertEqual(line.count('MyBehavior.IsValidMemoryCommitMarker('),1)
        line=line.replace('MyBehavior.IsValidMemoryCommitMarker(', 'IsValidMemoryCommitMarker(',1)
        # Preserve ordered old normalization operations; approved commit-marker checks were added.
        at=0
        for operation in (part.strip() for part in old_lines.splitlines()):
            if operation:
                found=line.find(operation,at)
                self.assertGreaterEqual(found,0,operation)
                at=found+len(operation)
        bind=self.extract(owner,'internal static void BindDailyMemoryDraftWeeklyTrigger(')
        entry=self.extract(owner,'internal static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(')
        for operation in ['trigger.MemoryId = memoryId;','trigger.GameDayIndex = gameDayIndex;','trigger.GameDate = string.IsNullOrWhiteSpace(trigger.GameDate) ? gameDate : trigger.GameDate;']:
            self.assertIn(operation,bind)
        self.assertIn('BindDailyMemoryDraftWeeklyTrigger(trigger, text, draft.GameDayIndex, draft.GameDate);',entry)
        self.assertIn('SanitizeDailyMemoryDraftLine(sourceLine, draft);',entry)
        self.assertIn('TWParallel.IsMainThread() ? sourceEntry : sourceEntry?.CopyForSummary()',entry)

    @historical_fixture
    def test_inner_structure_fix_has_narrow_inverse(self):
        path=REVIEW['innerStructureReview']['path']
        live=read_current(path)
        self.assertEqual(live,inverse.current_scope_baseline(path))
        self.assertIn('_lineStructureProbe.MoveNext();',live)
        self.assertIn('_triggerStructureProbe.MoveNext();',live)
        # Source guards below reject modified components. Coupled sealing runtime remains deferred.

    @historical_fixture
    def test_planner_only_changes_elapsed_owner_read(self):
        path='MyBehavior.MemorySummaryPlanning.cs'
        live=read_current(path)
        self.assertEqual(live,inverse.current_scope_baseline(path))
        self.assertEqual(live.count('MemorySummaryDispatchElapsedTicks * 1000.0 / Stopwatch.Frequency'),2)
        self.assertNotIn('_memorySummaryMainThreadElapsedTicks',live)
        self.assertIn('MemorySummaryRunOwner.Lease run',live)

    @historical_fixture
    def test_duplicate_composition_span(self):
        import re
        span=re.search(r'private readonly AnimusForge\.Refactor\.Runtime\.EventSourceMaterialIndex<EventSourceMaterialEntry> _eventSourceMaterialIndexBinding\s*=\s*[^;]+;',SOURCE).group()
        self.reject(SOURCE+'\n'+span,'Unreviewed B1 added source span')

    @historical_fixture
    def test_added_campaign_scope_cannot_drift(self):
        anchor='_campaignMemoryMaintenanceCycleActive = true;'
        self.assertEqual(SOURCE.count(anchor),1)
        self.reject(SOURCE.replace(anchor,'_campaignMemoryMaintenanceCycleActive = false;',1),'Unreviewed B1 declaration')

    @historical_fixture
    def test_whole_components_and_test_inputs_are_locked(self):
        read=Path.read_text
        # Do not load or run the diplomacy-coupled Captured/Sealing fixture groups here.
        targets=inverse.current_scope_dependencies(REVIEW)+[REVIEW['evidence']['materials']['runner']]
        for target in targets:
            with self.subTest(path=target):
                if target in {'MyBehavior.MemorySealing.cs', 'MyBehavior.MemoryMaintenanceBudget.cs', 'MyBehavior.MemorySummaryInput.cs'}:
                    # Positive physical/F3/finite guards finish before assertRaises;
                    # rejection must come from the designated finite B1 target.
                    with inverse.bound_finite_review_input(SOURCE) as finite:
                        with finite.dependency_mutation(target):
                            with self.assertRaisesRegex(AssertionError, 'Unreviewed B1 (production dependency|evidence)'):
                                finite.verify(SOURCE)
                    continue
                path=current_source_path(ROOT, target)
                def changed(file,*args,**kwargs):
                    text=read(file,*args,**kwargs)
                    return text+'\n// unreviewed dependency\n' if file==path else text
                with patch.object(Path,'read_text',changed):
                    self.reject(SOURCE,'Unreviewed B1 (production dependency|evidence)')


if __name__ == '__main__':
    import sys
    if '--finite' in sys.argv:
        sys.argv.remove('--finite')
        result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(CurrentScopeGuards))
        print('B1_FINITE_SCOPE historical_full_inverse=NOT_RUN captured_sealing_runtime=DEFERRED')
        raise SystemExit(0 if result.wasSuccessful() else 1)
    # No skips or weakened assertions: default still runs the original historical suite.
    unittest.main(defaultTest='InverseGuards',verbosity=2)
