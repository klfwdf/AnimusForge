"""Independent AF U quality profile: official V5 Dev / native 48kHz / 32 steps.
No changes to the source installation, no L truncation/quantization, no training.
"""
from collections import OrderedDict
from pathlib import Path
import copy, hashlib, os, sys


def prepare(app, cache):
 app,cache=Path(app).resolve(),Path(cache).resolve();cache.mkdir(parents=True,exist_ok=True)
 os.environ.update(PYTHONDONTWRITEBYTECODE='1',HF_HUB_OFFLINE='1',TRANSFORMERS_OFFLINE='1',HF_HUB_DISABLE_TELEMETRY='1',HF_HOME=str(cache/'huggingface'),NLTK_DATA=str(app/'offline-data/nltk'),OMP_NUM_THREADS='4',MKL_NUM_THREADS='4',language='en_US')
 for key,leaf in [('MPLCONFIGDIR','matplotlib'),('NUMBA_CACHE_DIR','numba'),('CUDA_CACHE_PATH','cuda'),('TORCHINDUCTOR_CACHE_DIR','inductor'),('TORCH_HOME','torch'),('XDG_CACHE_HOME','xdg')]:
  path=cache/leaf;path.mkdir(exist_ok=True);os.environ[key]=str(path)
 os.chdir(app);sys.path[:0]=[str(app),str(app/'GPT_SoVITS')]


def transfer(value, device):
 import torch
 if torch.is_tensor(value):return value.detach().to(device).clone()
 if isinstance(value,dict):return {k:transfer(v,device) for k,v in value.items()}
 if isinstance(value,list):return [transfer(v,device) for v in value]
 if isinstance(value,tuple):return tuple(transfer(v,device) for v in value)
 return value


def tensor_bytes(value):
 import torch
 if torch.is_tensor(value):return value.numel()*value.element_size()
 if isinstance(value,dict):return sum(tensor_bytes(v) for v in value.values())
 if isinstance(value,(list,tuple)):return sum(tensor_bytes(v) for v in value)
 return 0


def load(app, cache):
 prepare(app,cache)
 import torch
 from transformers import AutoModelForMaskedLM,AutoTokenizer
 from TTS_infer_pack.TTS import TTS,TTS_Config,Text2SemanticLightningModule
 import torch.nn.functional as F
 if not torch.cuda.is_available():raise RuntimeError('cuda_unavailable')
 if torch.cuda.mem_get_info()[0]<6*1024**3:raise RuntimeError('U_requires_6GiB_free_VRAM_before_start')
 torch.set_num_threads(4)
 class UltraTTS(TTS):
  def init_bert_weights(self,path):
   self.bert_tokenizer=AutoTokenizer.from_pretrained(path,local_files_only=True)
   self.bert_model=AutoModelForMaskedLM.from_pretrained(path,local_files_only=True,torch_dtype=torch.float16).eval().to(self.configs.device)
  def init_t2s_weights(self,path):
   self.configs.t2s_weights_path=path;self.configs.hz=50;self.configs.save_configs()
   checkpoint=torch.load(path,map_location='cpu',weights_only=False);config=checkpoint['config'];self.configs.max_sec=config['data']['max_sec']
   model=Text2SemanticLightningModule(config,'unused',is_train=False);model.load_state_dict(checkpoint['weight'])
   self.t2s_model=model.eval().half().to(self.configs.device)
   codebook=self.t2s_model.model.ar_audio_embedding.weight.detach();mute=codebook[self.configs.mute_tokens[self.configs.version]].unsqueeze(0)
   self.configs.mute_emb_sim_matrix=F.cosine_similarity(mute.float(),codebook.float(),dim=-1)
 base=Path(app)/'GPT_SoVITS/pretrained_models'
 config=TTS_Config({'custom':{'device':'cuda','is_half':True,'version':'v5dev','t2s_weights_path':str(base/'s1v3.ckpt'),'vits_weights_path':str(base/'gsv-v5-pretrained/s2Gv5dev.pth'),'bert_base_path':str(base/'chinese-roberta-wwm-ext-large'),'cnhuhbert_base_path':str(base/'chinese-hubert-base')}})
 config.configs_path=str(Path(cache)/'tts-infer-U.yaml');pipeline=UltraTTS(config)
 if pipeline.configs.version!='v5dev' or pipeline.vocoder_configs['sr']!=48000:raise RuntimeError('U_engine_identity_mismatch')
 torch.cuda.empty_cache();return Engine(pipeline)


class Engine:
 def __init__(self,pipeline):
  self.pipeline=pipeline;self.empty_prompt=copy.deepcopy(pipeline.prompt_cache);self.cache=OrderedDict();self.cache_bytes=0;self.active_key=None
 def synthesize(self,request):
  import numpy as np
  import torch
  path=Path(request['ref_audio_path']).resolve();key=(hashlib.sha256(path.read_bytes()).hexdigest(),request['prompt_lang'],request['prompt_text'])
  if key!=self.active_key:
   self.pipeline.prompt_cache=transfer(self.cache.pop(key),'cuda') if key in self.cache else copy.deepcopy(self.empty_prompt)
   if self.pipeline.prompt_cache.get('ref_audio_path') is not None:self.pipeline.prompt_cache['ref_audio_path']=str(path)
   # Recalculate after a hit popped its entry; bounds cover only retained CPU tensors.
   self.cache_bytes=sum(tensor_bytes(v) for v in self.cache.values())
  values=dict(request);values.update(ref_audio_path=str(path),media_type='wav',streaming_mode=False,return_fragment=False,batch_size=1,parallel_infer=True,use_cuda_graph=False,use_flash_attention=False,super_sampling=False,sample_steps=32,cfg_rate=1.30,top_k=15,top_p=1.0,temperature=1.0,repetition_penalty=1.35)
  try:
   with torch.inference_mode():rate,audio=next(self.pipeline.run(values))
   audio=np.asarray(audio)
   if rate!=48000 or audio.ndim!=1 or not len(audio) or audio.dtype!=np.int16:raise RuntimeError('invalid_U_audio')
   packet=transfer(self.pipeline.prompt_cache,'cpu');size=tensor_bytes(packet)
   if key in self.cache:self.cache_bytes-=tensor_bytes(self.cache.pop(key))
   if size<=128*1024*1024:
    self.cache[key]=packet;self.cache_bytes+=size
    while len(self.cache)>44 or self.cache_bytes>128*1024*1024:
     _,removed=self.cache.popitem(last=False);self.cache_bytes-=tensor_bytes(removed)
   self.active_key=key;return int(rate),audio
  except Exception:
   self.active_key=None;self.pipeline.prompt_cache=copy.deepcopy(self.empty_prompt)
   raise
