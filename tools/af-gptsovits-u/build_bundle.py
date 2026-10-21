"""Derive a fresh U payload from a verified portable L dependency set; never edit L/game.
The derivation replaces the inference engine/model, not just the package name.
"""
from pathlib import Path
import argparse,hashlib,json,shutil,time
from download_models import FILES,REVISION,sha
from runtime_dependencies import install_resampy


def ignore(folder,names):
 return {n for n in names if n in ('__pycache__','.git','.pytest_cache') or n.endswith(('.pyc','.log'))}

def main():
 p=argparse.ArgumentParser(description=__doc__)
 for name in ('portable-l','upstream-app','models','resampy-wheel','output'):p.add_argument('--'+name,required=True,type=Path)
 a=p.parse_args();old=a.portable_l.resolve(strict=True);upstream=a.upstream_app.resolve(strict=True);models=a.models.resolve(strict=True);out=a.output.resolve()
 if out.exists():raise ValueError('fresh_output_required')
 for original in (old,upstream,models):
  if out==original or original in out.parents or out in original.parents:raise ValueError('separate_output_required')
 meta=json.loads((old/'runtime.json').read_text(encoding='utf-8-sig'))
 if meta['package_id']!='AF-GPTSOVITS-L':raise ValueError('verified_portable_L_required')
 for rel,digest in meta['files'].items():
  path=(old/rel).resolve();path.relative_to(old)
  if sha(path)!=digest:raise ValueError('L_source_hash_mismatch:'+rel)
 for name,(size,digest) in FILES.items():
  if (models/name).stat().st_size!=size or sha(models/name)!=digest:raise ValueError('V5_source_hash_mismatch:'+name)
 if sha(upstream/'GPT_SoVITS/TTS_infer_pack/TTS.py')!=sha(old/'app/GPT_SoVITS/TTS_infer_pack/TTS.py'):raise ValueError('upstream_and_portable_source_mismatch')
 module=out/'AnimusForge';bundle=module/'AF-GPTSOVITS-U';bundle.mkdir(parents=True)
 print('Copying independent portable runtime...',flush=True);shutil.copytree(old/'runtime',bundle/'runtime',ignore=ignore)
 dependency_files=install_resampy(bundle/'runtime',a.resampy_wheel)
 print('Copying pinned inference source and offline language data...',flush=True)
 def app_ignore(folder,names):
  blocked=ignore(folder,names)
  if Path(folder).name=='pretrained_models':blocked|={'chinese-roberta-wwm-ext-large','v2Pro','sv','gsv-v5-pretrained'}
  return blocked
 shutil.copytree(old/'app',bundle/'app',ignore=app_ignore)
 pretrained=bundle/'app/GPT_SoVITS/pretrained_models';bert='chinese-roberta-wwm-ext-large'
 shutil.copytree(upstream/'GPT_SoVITS/pretrained_models'/bert,pretrained/bert,ignore=ignore)
 (pretrained/'gsv-v5-pretrained').mkdir()
 for name in FILES:shutil.copy2(models/name,pretrained/'gsv-v5-pretrained'/name)
 here=Path(__file__).resolve().parent
 for name in ('server.py','ultra_engine.py','check_runtime.py'):shutil.copy2(here/name,bundle/name)
 # Already normalized local evaluation voice assets: identical IDs/references, separate copies.
 shutil.copytree(old.parent/'VoicePacks',module/'VoicePacks',ignore=ignore)
 files=[bundle/n for n in ('server.py','ultra_engine.py','check_runtime.py','runtime/python.exe','runtime/python312.dll','runtime/python312._pth')]+dependency_files
 files += [p for p in pretrained.rglob('*') if p.suffix in ('.bin','.pth','.ckpt','.safetensors')]
 files += [bundle/'app'/n for n in ('GPT_SoVITS/TTS_infer_pack/TTS.py','GPT_SoVITS/module/models.py','GPT_SoVITS/module/models_v5.py','GPT_SoVITS/text/G2PWModel/g2pW.onnx')]
 files += [pretrained/bert/n for n in ('config.json','tokenizer.json','tokenizer_config.json','vocab.txt') if (pretrained/bert/n).is_file()]
 doc={'schema_version':1,'package_id':'AF-GPTSOVITS-U','edition':'U','package_version':'0.1.1','af_min_version':'1.5.4','af_runtime_api':1,'engine':'gpt-sovits-v5','python':'runtime/python.exe','entrypoint':'server.py','files':{p.relative_to(bundle).as_posix():sha(p) for p in files},'source_commit':meta['source_commit'],'model_repository':'lj1995/GPT-SoVITS','model_revision':REVISION,'status':'local_evaluation','redistribution_cleared':False,'capabilities':{'languages':['zh','en','ja','yue'],'text_language_selection':True,'sample_rate':48000,'batch_size':1,'weight_switch':False,'profile':'v5dev-32','sample_steps':32,'cfg_rate':1.30,'cuda_graph':False,'flash_attention_extension':False},'af_engine_support_required':'gpt-sovits-v5 (U edition only)'}
 (bundle/'runtime.json').write_text(json.dumps(doc,indent=2),encoding='utf-8')
 report={'status':'BUILT_NOT_TESTED_NOT_GAME_INSTALLED','source_L':str(old),'output':str(module),'payload_bytes':sum(p.stat().st_size for p in module.rglob('*') if p.is_file()),'models':doc['model_revision'],'protected_L_unchanged':all(sha(old/rel)==digest for rel,digest in meta['files'].items())}
 (out/'build-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report),flush=True)
if __name__=='__main__':main()
