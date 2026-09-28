"""Require three original gate ownership races to reproduce against immutable d40808b3."""
import argparse
from pathlib import Path
import subprocess
import sys

HERE=Path(__file__).resolve().parent


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    args=ap.parse_args()
    expected={
      'late-task':'retired A completion decremented/completed newer B gate',
      'late-waiter':'retired A waiter cleared newer B waiting/processing state',
      'late-timeout':'retired A timeout force-cleared newer B gate',
    }
    for case,message in expected.items():
        result=subprocess.run([sys.executable,str(HERE/'run_gate.py'),'--dotnet',args.dotnet,'--source-ref','d40808b3','--case',case,'--output-name','gate-red-'+case],capture_output=True,text=True,encoding='utf-8',errors='replace')
        output=result.stdout+result.stderr
        if result.returncode!=1 or message not in output or 'The build failed' in output:
            print(output);print('FAIL original gate race did not reproduce behaviorally: '+case);return 1
        print('PASS original gate regression reproduced: '+case)
    return 0

if __name__=='__main__':raise SystemExit(main())
