"""Require deliberately broken candidates to fail behavioral comparison, not compilation."""
import argparse
from pathlib import Path
import subprocess
import sys

HERE = Path(__file__).resolve().parent


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    args=parser.parse_args()
    for mutation in ['drop-reward','relay-as-direct','drop-rule-hits','skip-normalize','allow-recompletion']:
        result=subprocess.run([sys.executable,str(HERE/'run.py'),'--dotnet',args.dotnet,'--mutate',mutation,'--output-name','mutant-'+mutation],capture_output=True,text=True,encoding='utf-8',errors='replace')
        output=result.stdout+result.stderr
        expected='second completion was not rejected' if mutation=='allow-recompletion' else 'mismatch'
        if result.returncode != 1 or expected not in output or 'The build failed' in output:
            print(output); print('FAIL mutation not detected by behavioral assertions: '+mutation); return 1
        print('PASS mutation rejected: '+mutation)
    return 0

if __name__=='__main__': raise SystemExit(main())
