import argparse
from pathlib import Path
import hashlib,json,os,subprocess,sys,threading,time
parser=argparse.ArgumentParser()
for name in ("app","manifest","output"):parser.add_argument("--"+name,required=True)
parser.add_argument("--mode",choices=["baseline","lite","bert"],required=True)
args=parser.parse_args();output=Path(args.output).resolve();output.mkdir(parents=True,exist_ok=True)
import lite_engine
if args.mode=="bert":
    result=lite_engine.verify_bert(args.app,output/"cache")
    (output/"bert-equivalence.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(json.dumps(result));sys.exit(0)
samples=[];stop=threading.Event()
def gpu_used():return int(subprocess.check_output(["nvidia-smi","--query-gpu=memory.used","--format=csv,noheader,nounits"],creationflags=0x08000000,text=True).strip().splitlines()[0])
baseline=gpu_used()
def sample():
    while not stop.is_set():
        try:samples.append({"monotonic":time.monotonic(),"whole_gpu_mib":gpu_used()})
        except Exception:pass
        stop.wait(.25)
threading.Thread(target=sample,daemon=True).start()
result={"mode":args.mode,"gpu_baseline_mib":baseline,"results":[],"metrics_scope":"whole-card sampling and PyTorch allocator; increment is an estimate, not process attribution"}
try:
    started=time.monotonic();engine=lite_engine.load(args.app,output/"cache",optimized=args.mode=="lite")
    import torch,soundfile as sf,numpy as np
    result["load_seconds"]=time.monotonic()-started
    result["load_max_allocated_mib"]=torch.cuda.max_memory_allocated()/2**20
    result["load_max_reserved_mib"]=torch.cuda.max_memory_reserved()/2**20
    manifest=Path(args.manifest).resolve();catalog=json.loads(manifest.read_text(encoding="utf-8-sig"))
    male=next(p for p in catalog["profiles"] if p.get("category")=="male")
    custom=next(p for p in catalog["profiles"] if p.get("category")=="custom")
    texts=[(male,"angry","你答应过要听我说完，怎么现在又装作什么都不知道呢？"),(custom,"happy","别这么紧张嘛，我只是想看看，你究竟会不会认真听我说话。"),(custom,"neutral","You promised to listen, so please stay a little longer and tell me what really happened."),(male,"angry","如果你真的愿意相信我，就别在所有人面前装作什么都没发生；我当然知道你有自己的打算，可我已经等了这么久，难道连一句坦白的解释都不值得听到吗？")]
    for index,(voice,tone,text) in enumerate(texts):
        reference=voice.get("emotion_variants",{}).get(tone,voice);infer=reference.get("inference_reference",reference)
        request={"text":text,"text_lang":"auto","ref_audio_path":str(manifest.parent/infer["reference_file"]),"prompt_lang":reference.get("prompt_lang",voice["prompt_lang"]),"prompt_text":reference.get("prompt_text",voice["prompt_text"]),"speed_factor":1.0,"text_split_method":"cut5","seed":12345}
        torch.cuda.reset_peak_memory_stats();started=time.monotonic();rate,audio=engine.synthesize(request);torch.cuda.synchronize();seconds=time.monotonic()-started
        assert audio.size and np.max(np.abs(audio.astype(np.float32)))>0
        sf.write(output/f"{index+1:02d}-{args.mode}.wav",audio,rate,subtype="PCM_16")
        row={"voice":voice["id"],"tone":tone,"text":text,"seconds":seconds,"audio_seconds":audio.size/rate,"sample_rate":rate,"audio_sha256":hashlib.sha256(audio.tobytes()).hexdigest(),"max_allocated_mib":torch.cuda.max_memory_allocated()/2**20,"max_reserved_mib":torch.cuda.max_memory_reserved()/2**20}
        result["results"].append(row);print(json.dumps(row,ensure_ascii=False),flush=True)
    result["cache_bytes"]=engine.cache_bytes
    modules={name:str(Path(module.__file__).resolve()) for name,module in tuple(sys.modules.items()) if getattr(module,"__file__",None) and "site-packages" in str(module.__file__).lower()}
    (output/"imported-modules.json").write_text(json.dumps(modules,indent=2),encoding="utf-8")
finally:
    stop.set();time.sleep(.3);result["samples"]=samples
    result["whole_gpu_peak_mib"]=max([baseline]+[v["whole_gpu_mib"] for v in samples]);result["incremental_peak_estimate_mib"]=result["whole_gpu_peak_mib"]-baseline
    (output/"benchmark.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
