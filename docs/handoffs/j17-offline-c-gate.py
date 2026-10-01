"""Final C runner with explicit current candidate and credential-free synthetic context."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import uuid

root = Path(__file__).resolve().parents[2]
ap = argparse.ArgumentParser()
ap.add_argument('--output-parent',type=Path)
ap.add_argument('--candidate', type=Path, required=True)
ap.add_argument('--ids', type=Path)
args = ap.parse_args()
candidate = args.candidate.resolve(strict=True)
candidate.relative_to(root)
assert 'single_module_stage' not in str(candidate).lower()
marker = candidate.with_suffix('.build.json')
metadata = json.loads(marker.read_text(encoding='utf-8-sig'))
actual_hash = hashlib.sha256(candidate.read_bytes()).hexdigest().upper()
assert metadata['Sha256'] == actual_hash and metadata['BannerlordApi'] == '1.4'
approved = Path('E:/tmp/af-j17-20260930')
assert approved.is_dir() and not approved.is_symlink()
parent=args.output_parent or root / 'artifacts/j17b/session-20261001/p6-integration'
parent=parent.resolve();parent.relative_to(root/'artifacts')
out = parent / ('c-' + uuid.uuid4().hex[:12])
home = out.parent / ('c-home-' + uuid.uuid4().hex[:12])
home.mkdir(parents=True, exist_ok=False)
sdk = root / 'local/dotnet/8.0.425'
game = Path('D:/steam/steamapps/common/Mount & Blade II Bannerlord')
env = {key: os.environ[key] for key in ('SystemRoot','WINDIR','COMSPEC','PATHEXT','PROCESSOR_ARCHITECTURE',
    'ProgramFiles','ProgramFiles(x86)','ProgramW6432','ProgramData','ALLUSERSPROFILE') if key in os.environ}
env.update({
    'PATH':str(sdk)+os.pathsep+'D:/酒馆/Git/cmd'+os.pathsep+'C:/Windows/System32'+os.pathsep+'C:/Program Files/PowerShell/7-preview',
    'AF_DOTNET8':str(sdk/'dotnet.exe'),'AF_DOTNET10':'C:/Program Files/dotnet/dotnet.exe',
    'DOTNET_ROOT':str(sdk),'DOTNET_NOLOGO':'1','DOTNET_CLI_TELEMETRY_OPTOUT':'1',
    'USERPROFILE':str(home),'HOME':str(home),'APPDATA':str(home/'appdata'),'LOCALAPPDATA':str(home/'localappdata'),
    'HOMEDRIVE':home.drive,'HOMEPATH':str(home)[len(home.drive):],
    'AF_TEST_TEMP_ROOT':str(approved),'AF_PWSH':'C:/Program Files/PowerShell/7-preview/pwsh.exe',
    'AF_BANNERLORD_ROOT':str(game),'AF_WORKSHOP_DIR':'D:/steam/steamapps/workshop/content/261550',
    'AF_REPLAY_14_REFS':str(root/'local/bannerlord-refs/1.4.7.117484'),
    'AF_REPLAY_CANDIDATE_DLL':str(candidate),'AF_REPLAY_STAGE_BIN':str(game/'Modules/AnimusForge/bin/Win64_Shipping_Client'),
    'AF_REPLAY_HARMONY_MODULE_PATH':str(game/'Modules/Bannerlord.Harmony'),
    'AF_REPLAY_MCM_MODULE_PATH':str(game/'Modules/Bannerlord.MBOptionScreen'),
    'AF_REPLAY_UIEXTENDER_MODULE_PATH':str(game/'Modules/Bannerlord.UIExtenderEx'),
})
head = subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
command = [sys.executable,'-X','utf8','-B',str(root/'tests/run_all.py'),'--jobs','1','--out',str(out)]
if args.ids: command += ['--ids',str(args.ids.resolve(strict=True))]
print(out,flush=True)
done = subprocess.run(command,cwd=root,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
assert out.is_dir(), done.stdout[-1000:]
(out/'aggregate.log').write_text(done.stdout+'\n--- stderr ---\n'+done.stderr,encoding='utf-8')
(out/'candidate.json').write_text(json.dumps({'head':head,'candidate':str(candidate),'candidateSha256':actual_hash,
    'exit':done.returncode,'command':command,'syntheticTempParent':str(approved),'jobs':1},indent=2),encoding='utf-8')
print(done.stdout,flush=True)
print('aggregate exit',done.returncode,flush=True)
sys.exit(done.returncode)
