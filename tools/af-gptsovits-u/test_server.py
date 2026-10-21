"""Production U handler checks with a fake audio engine, no GPU/model startup."""
import copy,concurrent.futures,io,json,logging,math,tempfile,threading,time,unittest,wave
from pathlib import Path
import numpy as np
import soundfile as sf
from fastapi.testclient import TestClient
from server import create_app,validate_request,normalize_output_language

class ServerChecks(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name);(self.root/'VoicePacks').mkdir();self.wav=self.root/'VoicePacks/ref.wav';sf.write(self.wav,np.zeros(5*16000),16000,subtype='PCM_16')
  self.calls=[];self.entered=threading.Event();self.release=threading.Event();self.delayed=False
  outer=self
  class Fake:
   def synthesize(self,values):
    outer.calls.append(values)
    if outer.delayed:outer.entered.set();outer.release.wait(5)
    return 48000,np.zeros(4800,dtype=np.int16)
  self.state={'state':'ready','engine':Fake(),'error':None};self.client=TestClient(create_app(self.root,'a'*32,self.state,logging.getLogger('test')));self.headers={'X-AF-Instance':'a'*32}
  self.request={'text':'你好，我想把话说清楚。','text_lang':'zh','ref_audio_path':str(self.wav),'prompt_lang':'zh','prompt_text':'你好。','speed_factor':1}
 def tearDown(self):self.release.set();self.client.close();self.tmp.cleanup()
 def post(self,v):return self.client.post('/tts',json=v,headers=self.headers)
 def test_identity_and_audio(self):
  self.assertEqual(self.client.get('/af/health').status_code,403)
  health=self.client.get('/af/health',headers=self.headers).json();self.assertEqual(health['engine'],'gpt-sovits-v5');self.assertEqual(health['package_id'],'AF-GPTSOVITS-U');self.assertEqual(health['capabilities']['sample_rate'],48000)
  self.assertEqual(self.client.post('/tts',json=self.request).status_code,403)
  answer=self.post(self.request);self.assertEqual(answer.status_code,200)
  with wave.open(io.BytesIO(answer.content)) as wav:self.assertEqual((wav.getframerate(),wav.getnchannels(),wav.getsampwidth()),(48000,1,2))
  self.assertEqual(self.calls[-1]['text_lang'],'all_zh')
 def test_validation_and_injection(self):
  for data in [[],None,{},dict(self.request,text=''),dict(self.request,text='x'*501),dict(self.request,prompt_text=' '),dict(self.request,prompt_text='x'*1001),dict(self.request,text_lang='ko'),dict(self.request,prompt_lang='ko'),dict(self.request,text='안녕'),dict(self.request,ref_audio_path=str(self.root/'outside.wav')),dict(self.request,speed_factor=True),dict(self.request,speed_factor=3)]:
   with self.subTest(data=str(data)[:50]):self.assertEqual(self.post(data).status_code,400)
  for value in [float('nan'),float('inf'),-float('inf')]:
   with self.assertRaises(ValueError):validate_request(dict(self.request,speed_factor=value),self.root)
  answer=self.post(dict(self.request,sample_steps=1,cfg_rate=0,aux_ref_audio_paths=['C:/outside.wav'],sovits_weights='untrusted.pth',seed=7));self.assertEqual(answer.status_code,200)
  self.assertNotIn('sample_steps',self.calls[-1]);self.assertNotIn('aux_ref_audio_paths',self.calls[-1]);self.assertNotIn('sovits_weights',self.calls[-1]);self.assertEqual(self.calls[-1]['seed'],-1)
  self.assertEqual(self.client.post('/tts',content=b'x'*32769,headers=self.headers).status_code,413)
  self.assertEqual(self.client.post('/tts',content=b'{bad',headers=self.headers).status_code,400)
  self.assertEqual(self.client.get('/docs',headers=self.headers).status_code,404)
 def test_languages(self):
  for src,dst in [('auto','auto'),('zh','all_zh'),('ja','all_ja'),('yue','all_yue'),('en','en')]:
   self.assertEqual(normalize_output_language(src),dst);self.assertEqual(self.post(dict(self.request,text_lang=src)).status_code,200)
 def test_audio_reference_duration(self):
  sf.write(self.wav,np.zeros(16000),16000,subtype='PCM_16');self.assertEqual(self.post(self.request).status_code,400)
 def test_state_and_gate(self):
  self.state['state']='loading';self.assertEqual(self.post(self.request).status_code,503);self.state['state']='ready';self.delayed=True
  with concurrent.futures.ThreadPoolExecutor(1) as pool:
   future=pool.submit(self.post,self.request);self.assertTrue(self.entered.wait(5));self.assertEqual(self.post(self.request).status_code,429);self.assertTrue(self.client.get('/af/health',headers=self.headers).json()['busy']);self.release.set();self.assertEqual(future.result().status_code,200)
  self.assertFalse(self.client.get('/af/health',headers=self.headers).json()['busy'])
 def test_engine_failure_releases_gate(self):
  class Bad:
   def synthesize(self,v):raise RuntimeError('PRIVATE_TEXT_MUST_NOT_LEAK')
  self.state['engine']=Bad();r=self.post(self.request);self.assertEqual(r.status_code,500);self.assertNotIn('PRIVATE',r.text);self.assertFalse(self.client.get('/af/health',headers=self.headers).json()['busy'])
if __name__=='__main__':unittest.main(verbosity=2)
