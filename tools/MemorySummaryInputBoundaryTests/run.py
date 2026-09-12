"""Exact-source regression for memory-summary input capture and stale-source rejection."""
import argparse
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--original', action='store_true')
args = parser.parse_args()

if args.original:
    source = subprocess.check_output(['git', 'show', '9040d184:MyBehavior.cs'], cwd=ROOT).decode('utf-8-sig')
    start = source.index('private async Task<MemorySummaryExecutionResult> ExecuteMemorySummaryJobAsync(')
    end = source.index('private static string BuildMemoryOverviewSummarySystemPrompt(', start)
    worker = source[start:end]
    assert 'Hero hero = FindHeroById(memoryId);' in worker
    assert 'BuildMemorySummaryUserPrompt(hero, draft)' in worker
    assert 'TryParseMemorySummaryResponse(apiCallResult.Content, hero, draft' in worker
    assert 'IsMemorySummaryInputCurrent' not in source
    print('FAIL baseline=9040d184: live input reads in Execute jobs; no per-source acceptance check')
    raise SystemExit(1)

if not (ROOT / 'MyBehavior.MemorySummaryInputs.cs').exists():
    print('FAIL: production memory-summary input boundary is missing')
    raise SystemExit(1)

# The runtime harness is added with the production boundary; never accept a missing harness.
harness = Path(__file__).with_name('runtime.py')
if not harness.exists():
    print('FAIL: runtime boundary verification is missing')
    raise SystemExit(1)
raise SystemExit(subprocess.call(['python', str(harness)], cwd=ROOT))
