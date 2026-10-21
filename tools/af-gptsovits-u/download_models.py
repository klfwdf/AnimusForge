"""Download only pinned official V5 inference weights into a fresh local cache."""
from pathlib import Path
import argparse, hashlib, json, os, time
from urllib.request import urlopen
REVISION = '8f8857563c040d542de108a2c18b0425abd6c9c5'
FILES = {
 's2Gv5dev.pth': (766385517, '16c11fc588a9229a479316a11fc0f518087e79773f5271666a90b39d50460407'),
 'vocoder.pth': (57761163, 'b7543c83222aa2105bb4efb56845086bc5f2eeba61243d723ffa062052b3221f'),
}
def sha(path):
 h=hashlib.sha256()
 with Path(path).open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True,type=Path);a=p.parse_args();root=a.output.resolve();root.mkdir(parents=True,exist_ok=True)
 for name,(size,digest) in FILES.items():
  target=root/name
  if target.exists():
   if target.stat().st_size==size and sha(target)==digest: print('Verified existing',name,flush=True);continue
   raise ValueError('existing_model_mismatch:'+name)
  partial=root/(name+'.partial')
  if partial.exists():raise ValueError('partial_exists_inspect_before_retry:'+name)
  url=f'https://huggingface.co/lj1995/GPT-SoVITS/resolve/{REVISION}/gsv-v5-pretrained/{name}?download=true'
  print('Downloading pinned official',name,size,flush=True);total=0;h=hashlib.sha256();tick=time.monotonic()
  with urlopen(url,timeout=90) as source,partial.open('xb') as out:
   for b in iter(lambda:source.read(4*1024*1024),b''):
    total+=len(b)
    if total>size:raise ValueError('download_size_exceeded')
    out.write(b);h.update(b)
    if time.monotonic()-tick>15:print(name,total,'/',size,flush=True);tick=time.monotonic()
  if total!=size or h.hexdigest()!=digest:raise ValueError('official_weight_integrity_failed:'+name)
  os.rename(partial,target);print('PASS official size/SHA256',name,flush=True)
 (root/'model-source.json').write_text(json.dumps({'repository':'https://huggingface.co/lj1995/GPT-SoVITS','revision':REVISION,'files':{n:{'bytes':s,'sha256':h} for n,(s,h) in FILES.items()}},indent=2),encoding='utf-8')
if __name__=='__main__':main()
