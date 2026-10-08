from pathlib import Path
import argparse, os, subprocess, sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root
parser = argparse.ArgumentParser(); parser.add_argument('--run-root', type=Path); args = parser.parse_args()
out = new_run_root(ROOT, 'action-protocol-mutations', args.run_root)
if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')
expected = {
    'skip-overflow': 'ASSERT overflow-rejected-before-owner',
    'skip-disallowed': 'ASSERT raw-disallowed-rejected',
    'ignore-order': 'ASSERT channel-exact-NativeConversation',
    'ignore-parameters': 'ASSERT parameter-plan-required',
    'allow-action-star': 'ASSERT no-unbounded-action-family',
}
for mutation, signal in expected.items():
    result = subprocess.run([sys.executable, '-B', str(HERE/'run.py'), '--mutate', mutation, '--run-root', str(out / mutation)], cwd=HERE.parents[4], env=os.environ.copy(), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=150)
    output = result.stdout + result.stderr
    if result.returncode == 0 or signal not in output or 'error CS' in output:
        print(output)
        raise SystemExit('invalid mutation: ' + mutation)
    print('PASS compiled mutation ' + mutation + ' -> ' + signal)
