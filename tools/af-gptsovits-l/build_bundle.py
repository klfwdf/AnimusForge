"""Build a fresh local-evaluation L payload. Never writes the game or source install."""
import argparse
from pathlib import Path
import copy,hashlib,importlib,json,os,shutil,sys,time
import lite_engine

parser=argparse.ArgumentParser()
for name in ("installation","imports","voices","output"):parser.add_argument("--"+name,required=True)
args=parser.parse_args()
installation=Path(args.installation).resolve();destination=Path(args.output).resolve()
if destination.exists():raise RuntimeError("fresh_output_required")
module=destination/"AnimusForge";bundle=module/"AF-GPTSOVITS-L";runtime=bundle/"runtime";app=bundle/"app"
bundle.mkdir(parents=True)
source_app=installation/"app";site=installation/".venv312/Lib/site-packages"
base=installation/"python/cpython-3.12.15-windows-x86_64-none"
report={"status":"LOCAL_EVALUATION_NOT_GAME_DEPLOYED","source_installation":str(installation),"started":time.time(),"source_commit":"a3035089c65ca63eb7777a4b0dafd198ebe36cda","language_frontend_checks":{}}

def sha(path):
    h=hashlib.sha256()
    with Path(path).open("rb") as f:
        for chunk in iter(lambda:f.read(4*1024*1024),b""):h.update(chunk)
    return h.hexdigest()

def ignore(folder,names):
    excluded={n for n in names if n in ("__pycache__",".git",".github","tests","test") or n.endswith((".pyc",".pdb",".lib"))}
    if Path(folder).resolve()==(source_app/"GPT_SoVITS/pretrained_models/chinese-roberta-wwm-ext-large").resolve():
        excluded.update(n for n in names if n in ("pytorch_model.bin","model.safetensors"))
    return excluded

print("Checking offline language frontends and API dependency imports",flush=True)
lite_engine.prepare(source_app,destination/"build-cache")
from text.cleaner import clean_text
for language,text in {"zh":"你好，请把话说完。","en":"Please stay and listen.","ja":"こんにちは。話を聞いてください。","ko":"안녕하세요. 끝까지 들어 주세요.","yue":"你好，我想同你傾下計。"}.items():
    try:
        value=clean_text(text,language,"v2Pro")
        report["language_frontend_checks"][language]={"success":True,"phonemes":len(value[0])}
    except Exception as error:
        report["language_frontend_checks"][language]={"success":False,"error":type(error).__name__+":"+str(error)}
        print("Frontend check failed:",language,type(error).__name__,flush=True)
for name in ("fastapi","starlette","uvicorn","uvicorn.protocols.http.h11_impl","uvicorn.lifespan.off","uvicorn.loops.asyncio","soundfile","resampy"):
    importlib.import_module(name)
traced=json.loads(Path(args.imports).read_text(encoding="utf-8"))
traced.update({k:str(Path(v.__file__).resolve()) for k,v in tuple(sys.modules.items()) if getattr(v,"__file__",None)})
roots=set()
for filename in traced.values():
    try:roots.add(Path(filename).resolve().relative_to(site).parts[0])
    except (ValueError,IndexError):pass
for name in ("torchgen","pyopenjtalk","pyopenjtalk_plus","jamo","g2pk2","g2pkk","ko_pron","pyjyutping","wordsegment","inflect","distance","anyascii","unidecode","sentencepiece","pkg_resources","setuptools","_distutils_hack"):
    if (site/name).exists():roots.add(name)
roots.update(p.name for p in site.iterdir() if p.name.endswith((".dist-info",".libs")))
roots.discard("__pycache__")
print("Copying portable Python and",len(roots),"inference dependency roots",flush=True)
runtime.mkdir()
for item in base.iterdir():
    if item.name in ("Scripts","tcl","libs","include","BUILD"):continue
    if item.is_dir():
        shutil.copytree(item,runtime/item.name,ignore=lambda folder,names:{n for n in names if n in ("site-packages","__pycache__","test","tests","idlelib","tkinter","ensurepip")})
    else:shutil.copy2(item,runtime/item.name)
(runtime/"Lib/site-packages").mkdir(exist_ok=True)
for name in sorted(roots):
    source=site/name;target=runtime/"Lib/site-packages"/name
    if source.is_dir():shutil.copytree(source,target,ignore=ignore)
    elif source.is_file():shutil.copy2(source,target)
(runtime/"python312._pth").write_text(".\n..\nLib\nLib/site-packages\nDLLs\nimport site\n",encoding="utf-8")
print("Copying required upstream inference source/model resources",flush=True)
app.mkdir()
for name in ("GPT_SoVITS","tools","runtime"):
    shutil.copytree(source_app/name,app/name,ignore=ignore)
for name in ("config.py","LICENSE","LICENSE.txt","LICENSE.md"):
    if (source_app/name).is_file():shutil.copy2(source_app/name,app/name)
if (installation/"cache/nltk").exists():shutil.copytree(installation/"cache/nltk",app/"offline-data/nltk",ignore=ignore)
print("Exporting the exact BERT feature layer without unused later layers",flush=True)
import torch
from transformers import AutoModelForMaskedLM
model=AutoModelForMaskedLM.from_pretrained(source_app/"GPT_SoVITS/pretrained_models/chinese-roberta-wwm-ext-large",local_files_only=True,torch_dtype=torch.float16)
model.bert.encoder.layer=torch.nn.ModuleList(list(model.bert.encoder.layer)[:-2])
model.config.num_hidden_layers=len(model.bert.encoder.layer);model.config.af_target_hidden_layer=model.config.num_hidden_layers
model.save_pretrained(app/"GPT_SoVITS/pretrained_models/chinese-roberta-wwm-ext-large",safe_serialization=True)
del model
for name in ("server.py","lite_engine.py"):shutil.copy2(Path(__file__).parent/name,bundle/name)
print("Building gender/custom voice folders with stable IDs",flush=True)
source_manifest=Path(args.voices).resolve();source_document=json.loads(source_manifest.read_text(encoding="utf-8-sig"))
voice_root=module/"VoicePacks";voice_root.mkdir()
english={"male-clear":"Jing Yuan","male-gentle":"Dan Heng","male-steady":"Welt","male-deep":"Gepard","male-senior":"Sampo","female-clear":"March 7th","female-gentle":"Firefly","female-steady":"Himeko","female-firm":"Serval","female-senior":"Bronya","female-sparkle":"Sparkle"}
codes=dict(zip(("male-clear","male-gentle","male-steady","male-deep","male-senior","female-clear","female-gentle","female-steady","female-firm","female-senior","female-sparkle"),
               ("AF-M001","AF-M002","AF-M003","AF-M004","AF-M005","AF-F001","AF-F002","AF-F003","AF-F004","AF-F005","AF-C001")))
counts={"male":0,"female":0,"custom":0};profiles=[]
for original in source_document["profiles"]:
    profile=copy.deepcopy(original);category=profile["category"];counts[category]+=1
    # Default-pack codes are permanent, never derived from current list order.
    alias=codes[profile["id"]]
    directory=Path(category.title())/alias;target=voice_root/directory;target.mkdir(parents=True)
    profile["label"]=english.get(profile["id"],profile["id"]);profile["style_target"]=alias
    profile["short_alias"]=alias;profile["usage"]="local_evaluation_redistribution_not_cleared"
    def recording(entry,name):
        derived=entry.get("inference_reference",entry)
        source=(source_manifest.parent/derived["reference_file"]).resolve()
        source.relative_to(source_manifest.parent)
        if sha(source).lower()!=derived["reference_sha256"].lower():raise RuntimeError("reference_hash_mismatch")
        out=target/name;out.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,out)
        entry.pop("inference_reference",None)
        entry["source_recording_sha256"]=entry["reference_sha256"]
        entry["reference_file"]=name;entry["reference_sha256"]=sha(out)
    recording(profile,"reference-16k.wav")
    for mood,reference in profile.get("emotion_variants",{}).items():recording(reference,"tones/"+mood+"-16k.wav")
    aliases={k:v for k,v in source_document.get("aliases",{}).items() if v==profile["id"]}
    (target/"manifest.json").write_text(json.dumps({"schema_version":1,"profiles":[profile],"aliases":aliases},ensure_ascii=False,indent=2),encoding="utf-8")
    flat=copy.deepcopy(profile)
    flat["reference_file"]=(directory/flat["reference_file"]).as_posix()
    for reference in flat.get("emotion_variants",{}).values():reference["reference_file"]=(directory/reference["reference_file"]).as_posix()
    profiles.append(flat)
(voice_root/"manifest.json").write_text(json.dumps({"schema_version":1,"profiles":profiles,"aliases":source_document.get("aliases",{}),"usage":"local_evaluation_redistribution_not_cleared"},ensure_ascii=False,indent=2),encoding="utf-8")
print("Hashing managed entrypoints and required models",flush=True)
files=[bundle/"server.py",bundle/"lite_engine.py",runtime/"python.exe",runtime/"python312._pth",runtime/"python312.dll"]
files+=list((app/"GPT_SoVITS/pretrained_models").rglob("*.bin"))+list((app/"GPT_SoVITS/pretrained_models").rglob("*.pth"))+list((app/"GPT_SoVITS/pretrained_models").rglob("*.ckpt"))+list((app/"GPT_SoVITS/pretrained_models").rglob("*.safetensors"))
files+=[app/"GPT_SoVITS/TTS_infer_pack/TTS.py",app/"GPT_SoVITS/text/G2PWModel/g2pW.onnx"]
files=[p for p in files if p.is_file()]
manifest={"schema_version":1,"package_id":"AF-GPTSOVITS-L","edition":"L","package_version":"0.1.0","af_min_version":"1.5.4","af_runtime_api":1,"engine":"gpt-sovits-v2pro","python":"runtime/python.exe","entrypoint":"server.py","files":{p.relative_to(bundle).as_posix():sha(p) for p in files},"source_commit":report["source_commit"],"status":"local_evaluation","gpu_target_mib":2048,"gpu_target_verified":False,"redistribution_cleared":False,
          "capabilities":{"text_language_selection":True,"languages":["zh","en","ja","yue"],"unavailable_languages":{"ko":"windows_korean_frontend_unavailable"},"batch_size":1,"weight_switch":False}}
(bundle/"runtime.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
report["dependency_roots"]=sorted(roots);report["voices"]=counts;report["payload_bytes"]=sum(p.stat().st_size for p in module.rglob("*") if p.is_file());report["finished"]=time.time()
(destination/"build-report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps({"payload":str(module),"bytes":report["payload_bytes"],"voices":counts,"frontend_checks":report["language_frontend_checks"]},ensure_ascii=False),flush=True)
