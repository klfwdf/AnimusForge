"""Run the production HTTP handler with controlled engine/audio leaves; no model or web server.
Usage: python test_language.py <fresh-fixture-directory>
"""
import ast, asyncio, io, json, logging, sys, threading, time
from pathlib import Path
import server
root=Path(sys.argv[1]).resolve()
root.mkdir(parents=True,exist_ok=False)
voice=root/'VoicePacks'/'reference.wav';voice.parent.mkdir();voice.write_bytes(b'fixture')
class HTTPException(Exception):
    def __init__(self,status,detail):self.status_code=status;self.detail=detail
class Request:
    def __init__(self,data,authorized=True):self.data=data;self.headers={'X-AF-Instance':'fixture'} if authorized else {}
    async def body(self):return json.dumps(self.data,ensure_ascii=False).encode()
class Response:
    def __init__(self,content,media_type):self.content=content;self.media_type=media_type
class Engine:
    def __init__(self):self.calls=[]
    def synthesize(self,data):self.calls.append(data);return 32000,[0]
class AudioLeaf:
    @staticmethod
    def write(output,*args,**kwargs):output.write(b'RIFF-fixture')
async def worker(fn,*args):return fn(*args)
engine=Engine();gate=threading.Lock()
ns=dict(module=root,instance='fixture',state={'state':'ready','engine':engine},gate=gate,
        normalize_output_language=server.normalize_output_language,Request=Request,HTTPException=HTTPException,
        Response=Response,Path=Path,json=json,io=io,sf=AudioLeaf,run_in_threadpool=worker,time=time,logger=logging.getLogger('fixture'))
main=next(n for n in ast.parse(Path(server.__file__).read_text(encoding='utf-8')).body if isinstance(n,ast.FunctionDef) and n.name=='main')
methods=[n for n in main.body if isinstance(n,(ast.FunctionDef,ast.AsyncFunctionDef)) and n.name in ('authenticate','health','tts')]
for n in methods:n.decorator_list=[]
exec(compile(ast.fix_missing_locations(ast.Module(body=methods,type_ignores=[])),str(server.__file__),'exec'),ns)
async def run():
    checks=0
    def check(value):
        nonlocal checks
        assert value;checks+=1
    base={'text':'你好','ref_audio_path':str(voice),'prompt_lang':'zh','prompt_text':'原始参考台词','speed_factor':1}
    for choice,expected in [(None,'auto'),('auto','auto'),('zh','all_zh'),('all_zh','all_zh'),('yue','all_yue'),('all_yue','all_yue'),('en','en'),('ja','all_ja'),('all_ja','all_ja')]:
        data=dict(base)
        if choice is not None:data['text_lang']=choice
        reply=await ns['tts'](Request(data))
        check(reply.media_type=='audio/wav' and engine.calls[-1]['text_lang']==expected)
        check(engine.calls[-1]['prompt_lang']=='zh' and engine.calls[-1]['prompt_text']==base['prompt_text'])
        check(not gate.locked())
    for invalid in [None,'','中文','ko','fr',[],{},True,'ja;en']:
        calls=len(engine.calls)
        try:await ns['tts'](Request(dict(base,text_lang=invalid)));raise AssertionError('invalid accepted')
        except HTTPException as e:check(e.status_code==400 and len(engine.calls)==calls and not gate.locked())
    health=await ns['health'](Request(base));check(health['capabilities']['text_language_selection'] is True)
    try:await ns['tts'](Request(base,False));raise AssertionError('auth bypass')
    except HTTPException as e:check(e.status_code==403)
    gate.acquire()
    try:
        try:await ns['tts'](Request(base));raise AssertionError('concurrency bypass')
        except HTTPException as e:check(e.status_code==429)
    finally:gate.release()
    print('PASS',checks,'actual L handler assertions (model/audio leaves substituted)')
asyncio.run(run())
