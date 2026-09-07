"""Confirm Queue guard regressions are rejected by executed scenarios, not syntax checks."""
import argparse
from pathlib import Path
import subprocess
import sys

HERE=Path(__file__).resolve().parent


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    args=parser.parse_args()
    for mutation in ['ignore-generation','skip-dispatch-guard','lose-execution-context','unguarded-speech','off-thread-game-read','recapture-generation','recapture-session']:
        result=subprocess.run([sys.executable,str(HERE/'run_queue.py'),'--dotnet',args.dotnet,'--mutate',mutation,'--output-name','queue-mutant-'+mutation],capture_output=True,text=True,encoding='utf-8',errors='replace')
        output=result.stdout+result.stderr
        if result.returncode!=1 or 'System.Exception:' not in output or 'The build failed' in output:
            print(output);print('FAIL queue mutation not behaviorally rejected: '+mutation);return 1
        print('PASS queue mutation rejected: '+mutation)
    return 0

if __name__=='__main__':raise SystemExit(main())
