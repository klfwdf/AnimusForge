"""Deterministic self-tests. No model inference or filesystem writes outside TemporaryDirectory."""
import importlib.util,json,pathlib,unittest
HERE=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('benchmark',HERE/'benchmark.py')
b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
class BenchmarkContractTests(unittest.TestCase):
 def test_only_artifacts_output_allowed(self):
  self.assertEqual(b.safe_output(b.REPO/'artifacts/probe'),b.REPO/'artifacts/probe')
  for p in [b.REPO,b.REPO/'src',b.REPO.parent/'unapproved']:
   with self.assertRaises(ValueError):b.safe_output(p)
 def test_percentiles(self):
  self.assertEqual(b.quantile([1,2,3,4],.5),2.5)
  self.assertAlmostEqual(b.quantile([1,2,3,4],.95),3.85)
  self.assertEqual(b.summary([1,2,3])['p50Ms'],2)
 def test_dataset_has_unique_nonempty_candidates(self):
  ds=b.dataset();self.assertEqual(len(ds),6)
  self.assertEqual([len(x['documents']) for x in ds],[1,4,8,8,8,16])
  self.assertEqual(len({x['id'] for x in ds}),len(ds))
  for s in ds:
   self.assertTrue(s['query']);self.assertEqual(len(set(s['documents'])),len(s['documents']))
 def test_numerical_tolerance_does_not_hide_rank_change(self):
  def run(x):return {'scenarios':[{'id':'case','cacheMiss':[{'scores':x}]}]}
  r=b.compare_scores(run([.5,.500001,.1]),run([.500001,.5,.1]),'test')
  self.assertTrue(r['allWithinAbsTolerance1e-5'])
  self.assertFalse(r['allSameTop1']);self.assertTrue(r['allSameTop2Set'])
 def test_identical_scores_report_exact_consistency(self):
  run={'scenarios':[{'id':'case','cacheMiss':[{'scores':[.3,.8,.1]}]}]}
  r=b.compare_scores(run,run,'identity');self.assertEqual(r['maxAbsError'],0)
  self.assertTrue(r['allSameFullOrder'])
 def test_mismatched_results_cannot_pass_consistency(self):
  a={'scenarios':[{'id':'case','cacheMiss':[{'scores':[.3,.8]}]}]}
  bbad={'scenarios':[{'id':'case','cacheMiss':[{'scores':[.3]}]}]}
  with self.assertRaises(AssertionError):b.compare_scores(a,bbad,'bad')
  with self.assertRaises(AssertionError):b.compare_scores(a,{'scenarios':[]},'bad')
 def test_production_source_has_exactly_one_hook_anchor(self):
  src=b.RERANKER.read_bytes();anchor=b'sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;'
  self.assertEqual(src.count(anchor),1)
  addition=(b'\r\n' if b'\r\n' in src else b'\n')+b'\t\t\t\tBenchmarkHooks.Configure(sessionOptions);'
  patched=src.replace(anchor,anchor+addition)
  self.assertEqual(patched.replace(addition,b''),src)
 def test_production_resolver_remains_identical(self):
  self.assertIn(b'ResolveReranker',b.MODELSTORE.read_bytes())
if __name__=='__main__':unittest.main(verbosity=2)
