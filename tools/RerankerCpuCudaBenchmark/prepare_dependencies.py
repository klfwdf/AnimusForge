import argparse, concurrent.futures, hashlib, json, pathlib, requests, zipfile
PINS={'nvidia-cublas-cu12':'12.8.4.1','nvidia-cudnn-cu12':'9.8.0.87','nvidia-cuda-runtime-cu12':'12.8.90','nvidia-cufft-cu12':'11.3.3.83'}
def file_hash(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
 return h.hexdigest()
def get_one(spec,root):
 name,url,expected=spec
 dest=root/'downloads'/name;dest.parent.mkdir(parents=True,exist_ok=True)
 if not dest.exists() or (expected and file_hash(dest)!=expected):
  part=dest.with_suffix(dest.suffix+'.partial');print('DOWNLOAD',name,flush=True)
  with requests.get(url,stream=True,timeout=(30,180)) as r:
   r.raise_for_status()
   with part.open('wb') as f:
    for b in r.iter_content(4*1024*1024):f.write(b)
  if expected and file_hash(part)!=expected:raise ValueError('SHA mismatch '+name)
  part.replace(dest)
 digest=file_hash(dest)
 target=root/'native'/('ort-1.22.0' if name.endswith('.nupkg') else 'cuda-12.8')
 target.mkdir(parents=True,exist_ok=True)
 extracted=[]
 with zipfile.ZipFile(dest) as z:
  for item in z.infolist():
   if not item.filename.lower().endswith('.dll'):continue
   if name.endswith('.nupkg') and not item.filename.startswith('runtimes/win-x64/native/'):continue
   path=target/pathlib.PurePosixPath(item.filename).name
   if path.exists():
    if path.stat().st_size!=item.file_size:raise ValueError('size collision '+str(path))
   else:
    with z.open(item) as src,path.open('wb') as out:
     while b:=src.read(4*1024*1024):out.write(b)
   extracted.append(str(path))
 print('READY',name,len(extracted),'DLLs',flush=True)
 return {'package':name,'url':url,'sha256':digest,'bytes':dest.stat().st_size,'dlls':extracted}
def main():
 a=argparse.ArgumentParser();a.add_argument('--output',required=True);o=a.parse_args()
 root=pathlib.Path(o.output).resolve()
 repo=pathlib.Path(__file__).resolve().parent.parent.parent
 if not root.is_relative_to(repo/'artifacts'):raise ValueError('Dependency output must stay below repository artifacts.')
 root.mkdir(parents=True,exist_ok=True)
 specs=[]
 for p,v in PINS.items():
  r=requests.get(f'https://pypi.org/pypi/{p}/{v}/json',timeout=30);r.raise_for_status()
  fs=[f for f in r.json()['urls'] if f['filename'].endswith('win_amd64.whl')]
  if len(fs)!=1:raise ValueError(p+' ambiguous Windows wheel')
  f=fs[0];specs.append((f['filename'],f['url'],f['digests']['sha256']))
 name='microsoft.ml.onnxruntime.gpu.windows.1.22.0.nupkg'
 specs.append((name,f'https://api.nuget.org/v3-flatcontainer/microsoft.ml.onnxruntime.gpu.windows/1.22.0/{name}',None))
 with concurrent.futures.ThreadPoolExecutor(max_workers=3) as ex:
  receipts=list(ex.map(lambda s:get_one(s,root),specs))
 (root/'dependency-receipt.json').write_text(json.dumps(receipts,ensure_ascii=False,indent=2),encoding='utf-8')
if __name__=='__main__':main()
