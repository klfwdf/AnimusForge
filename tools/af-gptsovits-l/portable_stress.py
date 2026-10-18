"""Real portable-process smoke/stress; Windows PID-attributed WDDM counters.

No game files are touched. WDDM Dedicated Usage is sampled at 100ms; it is
reported separately from shared GPU memory and is not a hard allocation cap.
"""
import argparse, ctypes as c, hashlib, json, os, socket, subprocess, threading, time
from pathlib import Path
from urllib.request import Request, build_opener, ProxyHandler
from urllib.error import HTTPError, URLError


class PidMemory:
    def __init__(self, pid):
        self.pid, self.rows, self.stop, self.error = pid, [], threading.Event(), None
        self.thread = threading.Thread(target=self.run, daemon=True)
        self.thread.start()

    def run(self):
        class Value(c.Structure):
            _fields_ = [("status", c.c_ulong), ("value", c.c_longlong)]
        class Item(c.Structure):
            _fields_ = [("name", c.c_wchar_p), ("value", Value)]
        pdh = c.WinDLL("pdh")
        handle = c.c_void_p
        pdh.PdhOpenQueryW.argtypes = [c.c_wchar_p, c.c_size_t, c.POINTER(handle)]
        pdh.PdhAddEnglishCounterW.argtypes = [handle, c.c_wchar_p, c.c_size_t, c.POINTER(handle)]
        pdh.PdhCollectQueryData.argtypes = [handle]
        pdh.PdhCloseQuery.argtypes = [handle]
        pdh.PdhGetFormattedCounterArrayW.argtypes = [handle,c.c_ulong,c.POINTER(c.c_ulong),c.POINTER(c.c_ulong),handle]
        query = handle()
        try:
            if pdh.PdhOpenQueryW(None, 0, c.byref(query)): raise RuntimeError("pdh_open_failed")
            counters = {}
            for field, name in (("dedicated_bytes", "Dedicated Usage"), ("shared_bytes", "Shared Usage")):
                counter = handle()
                if pdh.PdhAddEnglishCounterW(query, "\\GPU Process Memory(*)\\"+name, 0, c.byref(counter)):
                    raise RuntimeError("pdh_counter_unavailable")
                counters[field] = counter
            prefix = "pid_"+str(self.pid)+"_"
            while not self.stop.is_set():
                if pdh.PdhCollectQueryData(query): raise RuntimeError("pdh_collect_failed")
                row = {"time":time.time(),"pid":self.pid}
                for field, counter in counters.items():
                    size, count = c.c_ulong(), c.c_ulong()
                    pdh.PdhGetFormattedCounterArrayW(counter,0x400,c.byref(size),c.byref(count),None)
                    if not size.value: continue
                    buffer = c.create_string_buffer(size.value)
                    if pdh.PdhGetFormattedCounterArrayW(counter,0x400,c.byref(size),c.byref(count),buffer): continue
                    items = c.cast(buffer,c.POINTER(Item))
                    matches = [items[i].value.value for i in range(count.value)
                               if items[i].name.startswith(prefix) and items[i].value.status in (0,1)]
                    if matches: row[field] = sum(matches)
                if "dedicated_bytes" in row: self.rows.append(row)
                self.stop.wait(.1)
        except Exception as error:
            self.error = str(error)
        finally:
            if query: pdh.PdhCloseQuery(query)

    def close(self):
        self.stop.set(); self.thread.join(5)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--module", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    module, output = Path(args.module).resolve(), Path(args.output).resolve()
    if output.exists(): raise RuntimeError("fresh_output_required")
    output.mkdir(parents=True)
    root = module / "AF-GPTSOVITS-L"
    token = os.urandom(16).hex()
    with socket.socket() as listener:
        listener.bind(("127.0.0.1",0)); port = listener.getsockname()[1]
    endpoint = "http://127.0.0.1:"+str(port)
    environment = os.environ.copy()
    environment.update(AF_TTS_INSTANCE=token,PYTHONUTF8="1",PYTHONDONTWRITEBYTECODE="1",HF_HUB_OFFLINE="1")
    report = {"scope":"real portable L server; not game playback or 8GB hardware acceptance",
              "memory_source":"Windows PDH GPU Process Memory; sum PID physical-adapter instances, 100ms sampled",
              "gpu_target_mib":2048,"cases":[],"status":"RUNNING"}
    opener = build_opener(ProxyHandler({}))
    def call(route, data=None):
        body = None if data is None else json.dumps(data,ensure_ascii=False).encode("utf-8")
        request = Request(endpoint+route, body, {"X-AF-Instance":token,"Content-Type":"application/json"})
        with opener.open(request,timeout=150) as response: return response.read()
    with (output/"worker-output.log").open("wb") as log:
        process = subprocess.Popen([str(root/"runtime/python.exe"),str(root/"server.py"),"--port",str(port),"--module-root",str(module),"--parent-pid",str(os.getpid())],cwd=root,env=environment,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW)
        memory = PidMemory(process.pid)
        report["pid"] = process.pid
        started = time.monotonic()
        try:
            while time.monotonic()-started < 120:
                if process.poll() is not None: raise RuntimeError("worker_exited_"+str(process.returncode))
                try:
                    health = json.loads(call("/af/health"))
                    if health["state"] == "ready": break
                    if health["state"] == "failed": raise RuntimeError("model_load_failed")
                except (URLError, TimeoutError): pass
                time.sleep(.5)
            else: raise RuntimeError("startup_timeout")
            report["ready_seconds"] = time.monotonic()-started
            report["capabilities"] = health["capabilities"]
            voices = json.loads((module/"VoicePacks/manifest.json").read_text(encoding="utf-8-sig"))["profiles"]
            cases = [(v,"neutral","你答应过要把事情说清楚，所以这一次，请先听我说完，好吗？") for v in voices]
            sparkle = next(v for v in voices if v["id"] == "female-sparkle")
            for tone in ("happy","angry","sad"):
                cases.append((sparkle,tone,{"happy":"哎呀，原来你一直记着我的话呀，这下我可真的要高兴起来了。","angry":"我已经解释过很多次了，你能不能别再假装什么都不知道？","sad":"我还以为这一次你会留下来，至少让我把没说完的话说完。"}[tone]))
            cases += [(voices[0],"angry","Before you accuse me of breaking my promise, remember that I stayed here through the entire night, listened to every objection, and asked for only one honest answer; if that still means nothing to you, please say so clearly instead of pretending that this conversation never happened."),
                      (sparkle,"happy","你总说我把每一件小事都想得太复杂，可是，如果连我们为什么争吵、为什么彼此误会都不愿意认真弄明白，那么就算今天勉强笑着把话题带过去，明天遇到同样的事情，我们不还是会站在这里，把那些伤人的话重新说一遍吗？所以，先别急着离开，坐下来听我说完，哪怕你最后仍然不同意我的想法，至少也应该让我知道，你确实听懂了我想告诉你的事情。"),
                      (voices[0],"neutral","こんにちは。最後まで話を聞いてください。")]
            for index,(voice,tone,text) in enumerate(cases,1):
                reference = voice.get("emotion_variants",{}).get(tone,voice)
                path = (module/"VoicePacks"/reference["reference_file"]).resolve()
                start = time.time(); before = len(memory.rows)
                audio = call("/tts",{"text":text,"ref_audio_path":str(path),"prompt_lang":reference.get("prompt_lang",voice["prompt_lang"]),"prompt_text":reference["prompt_text"],"speed_factor":1})
                if not audio.startswith(b"RIFF") or audio[8:12] != b"WAVE": raise RuntimeError("not_wave")
                filename = f"{index:02}-{voice['id']}-{tone}.wav"
                (output/filename).write_bytes(audio)
                samples = memory.rows[before:]
                row = {"voice":voice["id"],"tone":tone,"characters":len(text),"seconds":time.time()-start,
                       "bytes":len(audio),"audio":filename,"reference_sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
                       "sampled_dedicated_peak_mib":max((s["dedicated_bytes"] for s in samples),default=0)/1048576}
                report["cases"].append(row)
                print(json.dumps(row,ensure_ascii=False),flush=True)
            report["status"] = "SYNTHESIS_PASS"
        except Exception as error:
            report.update(status="FAILED",error=type(error).__name__+":"+str(error))
            raise
        finally:
            process.terminate(); process.wait(15); time.sleep(.3); memory.close()
            report["process_exited"] = process.poll() is not None
            report["sampler_error"] = memory.error
            report["sample_count"] = len(memory.rows)
            for field in ("dedicated_bytes","shared_bytes"):
                report["sampled_peak_"+field.replace("bytes","mib")] = max((s.get(field,0) for s in memory.rows),default=0)/1048576
            (output/"memory-samples.json").write_text(json.dumps(memory.rows),encoding="utf-8")
            (output/"result.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False,indent=2),flush=True)


if __name__ == "__main__": main()
