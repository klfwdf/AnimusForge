"""AF U authenticated, bounded, single-job loopback server. No public control API."""
from pathlib import Path
import argparse, ctypes, hmac, io, json, logging, math, os, threading, time
from contextlib import redirect_stdout, redirect_stderr
from logging.handlers import RotatingFileHandler

LANGUAGES=('zh','en','ja','yue')
ENGINE='gpt-sovits-v5'

def normalize_output_language(value):
 aliases={'auto':'auto','zh':'all_zh','all_zh':'all_zh','en':'en','ja':'all_ja','all_ja':'all_ja','yue':'all_yue','all_yue':'all_yue'}
 if not isinstance(value,str) or value not in aliases:raise ValueError('text_language_invalid')
 return aliases[value]

def validate_request(data,module):
 if not isinstance(data,dict):raise ValueError('request_object_required')
 text=data.get('text');prompt=data.get('prompt_text');lang=data.get('prompt_lang')
 if not isinstance(text,str) or not text.strip() or len(text)>500:raise ValueError('text_invalid')
 if not isinstance(prompt,str) or not prompt.strip() or len(prompt)>1000:raise ValueError('reference_text_invalid')
 if any('\uac00'<=ch<='\ud7af' for ch in text+prompt):raise ValueError('korean_frontend_unavailable')
 if lang not in LANGUAGES:raise ValueError('reference_language_invalid')
 raw=data.get('ref_audio_path')
 if not isinstance(raw,str):raise ValueError('reference_invalid')
 reference=Path(raw).resolve(strict=True);voices=(Path(module)/'VoicePacks').resolve(strict=True);reference.relative_to(voices)
 if reference.suffix.lower()!='.wav' or not reference.is_file() or not 0<reference.stat().st_size<=8*1024*1024:raise ValueError('reference_invalid')
 speed=data.get('speed_factor',1.0)
 if isinstance(speed,bool):raise ValueError('speed_invalid')
 speed=float(speed)
 if not math.isfinite(speed) or not .5<=speed<=2:raise ValueError('speed_invalid')
 # Never accept client-supplied weights, paths outside VoicePacks, extra refs or sampling knobs.
 return dict(text=text,text_lang=normalize_output_language(data.get('text_lang','auto')),ref_audio_path=str(reference),prompt_lang=lang,prompt_text=prompt,speed_factor=speed,text_split_method='cut5',seed=-1)

def create_app(module,instance,state,logger):
 from fastapi import FastAPI,Request,HTTPException
 from fastapi.responses import Response
 from starlette.concurrency import run_in_threadpool
 import soundfile as sf
 app=FastAPI(docs_url=None,redoc_url=None,openapi_url=None);gate=threading.Lock()
 def auth(request):
  if not hmac.compare_digest(request.headers.get('X-AF-Instance',''),instance):raise HTTPException(403,'instance_mismatch')
 @app.get('/af/health')
 async def health(request:Request):
  auth(request)
  return dict(instance=instance,package_id='AF-GPTSOVITS-U',af_runtime_api=1,engine=ENGINE,state=state['state'],error=state.get('error'),capabilities=dict(audio='pcm16_mono_wav',sample_rate=48000,batch_size=1,weight_switch=False,text_language_selection=True,languages=list(LANGUAGES),profile='v5dev-32'),busy=gate.locked())
 @app.post('/tts')
 async def tts(request:Request):
  auth(request)
  if state['state']!='ready':raise HTTPException(503,'runtime_not_ready')
  raw=bytearray()
  async for part in request.stream():
   if len(raw)+len(part)>32768:raise HTTPException(413,'request_too_large')
   raw.extend(part)
  try:
   values=validate_request(json.loads(raw),module)
   info=sf.info(values['ref_audio_path'])
   if info.format!='WAV' or info.channels!=1 or not 3<=info.duration<=10:raise ValueError('reference_must_be_mono_WAV_3_to_10_seconds')
  except (ValueError,TypeError,KeyError,OSError,RuntimeError) as error:raise HTTPException(400,'invalid_synthesis_request') from error
  if not gate.acquire(False):raise HTTPException(429,'runtime_busy')
  started=time.monotonic()
  def generate():
   # Upstream diagnostics include transcripts: suppress them rather than log private dialogue.
   with redirect_stdout(io.StringIO()),redirect_stderr(io.StringIO()):return state['engine'].synthesize(values)
  try:
   rate,audio=await run_in_threadpool(generate)
   if len(audio)*2+128>32*1024*1024:raise HTTPException(413,'audio_too_large')
   buffer=io.BytesIO();sf.write(buffer,audio,rate,format='WAV',subtype='PCM_16')
   logger.info('synthesis_ok chars=%d elapsed=%.3f rate=%d',len(values['text']),time.monotonic()-started,rate)
   return Response(buffer.getvalue(),media_type='audio/wav')
  except HTTPException:raise
  except Exception as error:
   logger.error('synthesis_failed type=%s',type(error).__name__)
   if type(error).__name__=='OutOfMemoryError':
    state.update(state='failed',error='cuda_out_of_memory');raise HTTPException(503,'cuda_out_of_memory') from error
   raise HTTPException(500,'synthesis_failed') from error
  finally:gate.release()
 return app

def main():
 p=argparse.ArgumentParser();p.add_argument('--port',type=int,required=True);p.add_argument('--module-root',required=True);p.add_argument('--parent-pid',type=int,required=True);args=p.parse_args()
 root=Path(__file__).resolve().parent;module=Path(args.module_root).resolve();instance=os.environ.get('AF_TTS_INSTANCE','')
 if len(instance)!=32 or any(c not in '0123456789abcdefABCDEF' for c in instance) or not 1024<=args.port<=65535:raise ValueError('invalid_managed_launch')
 if root!=module/'AF-GPTSOVITS-U':raise ValueError('managed_package_location_mismatch')
 kernel=ctypes.WinDLL('kernel32',use_last_error=True);kernel.OpenProcess.restype=ctypes.c_void_p
 kernel.GetExitCodeProcess.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_ulong)];kernel.CloseHandle.argtypes=[ctypes.c_void_p]
 parent=kernel.OpenProcess(0x1000,False,args.parent_pid)
 if not parent:raise RuntimeError('parent_unavailable')
 def watch_parent():
  while True:
   code=ctypes.c_ulong()
   if not kernel.GetExitCodeProcess(parent,ctypes.byref(code)) or code.value!=259:os._exit(0)
   time.sleep(1)
 threading.Thread(target=watch_parent,daemon=True).start()
 cache=module/'Cache/TTS/U';logs=module/'Logs/TTS';cache.mkdir(parents=True,exist_ok=True);logs.mkdir(parents=True,exist_ok=True)
 logger=logging.getLogger('af_u');logger.setLevel(logging.INFO);logger.addHandler(RotatingFileHandler(logs/'af-gptsovits-u.log',maxBytes=2*1024*1024,backupCount=2,encoding='utf-8'))
 os.environ['TEMP']=os.environ['TMP']=str(cache)
 import ultra_engine
 ultra_engine.prepare(root/'app',cache)
 state={'state':'loading','engine':None,'error':None};app=create_app(module,instance,state,logger)
 def initialize():
  try:
   with redirect_stdout(io.StringIO()),redirect_stderr(io.StringIO()):state['engine']=ultra_engine.load(root/'app',cache)
   state['state']='ready';logger.info('runtime_ready engine=%s profile=v5dev-32',ENGINE)
  except Exception as error:
   state.update(state='failed',error=type(error).__name__);logger.error('initialization_failed type=%s',type(error).__name__)
 threading.Thread(target=initialize,daemon=True).start()
 import uvicorn
 uvicorn.run(app,host='127.0.0.1',port=args.port,access_log=False,log_level='warning',http='h11',ws='none',loop='asyncio',lifespan='off')
if __name__=='__main__':main()
