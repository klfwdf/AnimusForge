"""U portable synthesis/latency/VRAM/RAM evaluation; always closes its own worker."""
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
    import io,wave,psutil
    parser=argparse.ArgumentParser();parser.add_argument('--module',required=True);parser.add_argument('--output',required=True);parser.add_argument('--smoke',action='store_true');args=parser.parse_args()
    module=Path(args.module).resolve();output=Path(args.output).resolve()
    if output.exists():raise ValueError('fresh_output_required')
    output.mkdir(parents=True);root=module/'AF-GPTSOVITS-U';token=os.urandom(16).hex()
    with socket.socket() as s:s.bind(('127.0.0.1',0));port=s.getsockname()[1]
    endpoint='http://127.0.0.1:'+str(port);opener=build_opener(ProxyHandler({}))
    def call(route,data=None,authenticated=True):
        headers={'Content-Type':'application/json'}
        if authenticated:headers['X-AF-Instance']=token
        req=Request(endpoint+route,None if data is None else json.dumps(data,ensure_ascii=False).encode('utf-8'),headers)
        with opener.open(req,timeout=240) as reply:return reply.read()
    report={'status':'RUNNING','scope':'portable U V5 Dev; no game, no old save, no subjective quality certification','profile':'v5dev-32','cases':[],'memory_sampling':'PID-attributed Windows WDDM dedicated/shared and RSS at 100ms; no 8GB claim'}
    report['hardware']=subprocess.check_output(['nvidia-smi','--query-gpu=name,memory.total,driver_version','--format=csv,noheader'],text=True).strip()
    env=os.environ.copy();env.update(AF_TTS_INSTANCE=token,PYTHONUTF8='1',PYTHONDONTWRITEBYTECODE='1')
    memory=None;rss=[];stop=threading.Event()
    with (output/'worker-output.log').open('wb') as log:
        process=subprocess.Popen([str(root/'runtime/python.exe'),str(root/'server.py'),'--port',str(port),'--module-root',str(module),'--parent-pid',str(os.getpid())],cwd=root,env=env,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW)
        memory=PidMemory(process.pid);report['pid']=process.pid
        def sample_ram():
            p=psutil.Process(process.pid)
            while not stop.is_set():
                try:rss.append(p.memory_info().rss)
                except psutil.Error:break
                stop.wait(.1)
        ram=threading.Thread(target=sample_ram,daemon=True);ram.start();started=time.monotonic()
        try:
            while time.monotonic()-started<120:
                if process.poll() is not None:raise RuntimeError('worker_exited:'+str(process.returncode))
                try:
                    health=json.loads(call('/af/health'))
                    if health['state']=='failed':raise RuntimeError('initialization_failed:'+str(health.get('error')))
                    if health['state']=='ready':break
                except (URLError,TimeoutError):pass
                time.sleep(.5)
            else:raise RuntimeError('startup_timeout')
            assert health['package_id']=='AF-GPTSOVITS-U' and health['engine']=='gpt-sovits-v5'
            report['ready_seconds']=time.monotonic()-started;report['health']=dict(health);report['health'].pop('instance',None)
            try:call('/af/health',authenticated=False);raise AssertionError('unauthorized_request_accepted')
            except HTTPError as err:assert err.code==403
            voices={v['id']:v for v in json.loads((module/'VoicePacks/manifest.json').read_text(encoding='utf-8-sig'))['profiles']}
            text='你答应过要听我把话说完，可你每次都装作毫不在意。好啦，我没有真的生气，只是希望下一次，你能认真看着我的眼睛，告诉我你心里到底在想什么。'
            cases=[('female-sparkle',tone,'all_zh',text) for tone in ('neutral','happy','angry','sad')]
            cases += [('male-clear','angry','all_zh','先别急着替自己辩解。如果你真的在乎那些仍然等待消息的人，就应该把昨晚发生的事情一五一十地告诉我，而不是拿一句无可奉告，把所有责任都推给别人。'),('female-gentle','happy','all_zh','其实我只是想跟你多待一会儿。等雨停了，我们再一起慢慢走回去，好不好？'),('female-sparkle','neutral','en','You promised to listen. Before you make another excuse, look me in the eyes and tell me what really happened last night.'),('female-sparkle','happy','all_ja','そんなに緊張しないで。私はただ、あなたが最後まで話を聞いてくれるか、確かめたかっただけ。'),('female-sparkle','neutral','all_yue','你唔使咁紧张，我只系想同你慢慢倾清楚。等你听完我讲嘅嘢，我哋再一齐决定下一步点做，好唔好？')]
            if args.smoke:cases=cases[:1]
            for i,(vid,tone,language,text) in enumerate(cases,1):
                voice=voices[vid];reference=voice.get('emotion_variants',{}).get(tone,voice);path=(module/'VoicePacks'/reference['reference_file']).resolve()
                request={'text':text,'text_lang':language,'ref_audio_path':str(path),'prompt_text':reference['prompt_text'],'prompt_lang':reference['prompt_lang'],'speed_factor':1}
                start=time.monotonic();offset=len(memory.rows);audio=call('/tts',request)
                with wave.open(io.BytesIO(audio)) as w:
                    assert w.getframerate()==48000 and w.getnchannels()==1 and w.getsampwidth()==2
                    duration=w.getnframes()/w.getframerate()
                name=f'{i:02}-{vid}-{tone}-{language}.wav';(output/name).write_bytes(audio);elapsed=time.monotonic()-start
                row={'voice':vid,'tone':tone,'language':language,'text':text,'seconds':elapsed,'audio_seconds':duration,'rtf':elapsed/duration,'audio':name,'reference_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'sha256':hashlib.sha256(audio).hexdigest(),'peak_dedicated_mib':max((s['dedicated_bytes'] for s in memory.rows[offset:]),default=0)/1048576}
                report['cases'].append(row);print(json.dumps(row,ensure_ascii=False),flush=True)
            report['status']='SYNTHESIS_PASS'
        except Exception as err:report.update(status='FAILED',error=type(err).__name__+':'+str(err));raise
        finally:
            if process.poll() is None:process.terminate()
            process.wait(20);stop.set();ram.join(5);memory.close();report['process_exited']=process.poll() is not None
            report['sample_count']=len(memory.rows);report['sampler_error']=memory.error;report['peak_rss_mib']=max(rss,default=0)/1048576
            for field in ('dedicated_bytes','shared_bytes'):report['peak_'+field.replace('bytes','mib')]=max((v.get(field,0) for v in memory.rows),default=0)/1048576
            (output/'memory-samples.json').write_text(json.dumps(memory.rows),encoding='utf-8');(output/'result.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PASS portable U tests; peak dedicated MiB',report['peak_dedicated_mib'],flush=True)
if __name__=='__main__':main()
