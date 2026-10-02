import argparse, csv, hashlib, json, math, os, pathlib, shutil, statistics, subprocess, sys, threading, time
from datetime import datetime, timezone, timedelta
import xml.sax.saxutils as xml

HERE=pathlib.Path(__file__).resolve().parent
REPO=HERE.parent.parent
RERANKER=REPO/'src/modules/AF.Module.Knowledge/Semantic/OnnxCrossEncoderReranker.cs'
MODELSTORE=REPO/'src/AF.Persistence/AnimusForgeModelStore.cs'
ORT_VERSION='1.22.0'

def sha(path):
 h=hashlib.sha256()
 with pathlib.Path(path).open('rb') as f:
  for data in iter(lambda:f.read(4*1024*1024),b''):h.update(data)
 return h.hexdigest()
def save(path,data):
 pathlib.Path(path).write_text(json.dumps(data,ensure_ascii=False,indent=2,allow_nan=False),encoding='utf-8')
def safe_output(value):
 p=pathlib.Path(value).resolve()
 if not p.is_relative_to(REPO/'artifacts'):raise ValueError('Output must be below this repository artifacts directory.')
 return p

def dataset():
 docs=[
  '帝国的皇位继承引发了内战，北部帝国强调元老院的权威，南部帝国支持女皇，西部帝国拥护军队推举的统治者。',
  '瓦兰迪亚的贵族以封建领地和骑兵力量为基础，领主需要履行效忠义务，土地与封臣关系决定政治地位。',
  '巴旦尼亚的村落分布在森林与高地，弓箭手擅长利用树林，地方首领重视传统与部族之间的关系。',
  '库赛特草原上的骑射部队机动灵活，牧场与马匹供应影响军队补给，部族首领需要协调各自的利益。',
  '阿塞莱商队沿着南部商路往来，谷物、马匹和地方特产的价格随市场供需变化，战争会增加运输风险。',
  '斯特吉亚的战士熟悉寒冷的北方，盾墙和步兵在当地战争中占有重要地位，村庄依赖农耕与贸易。',
  '围城期间守军依靠城墙与驻军组织防御。粮食消耗、城墙受损和攻城器械都会影响围城局势。',
  '城市的繁荣与税收有关。安全度不足会影响秩序，忠诚度下降可能引发叛乱，驻军维护也需要支付费用。',
  '招募士兵需要支付费用，部队每天需要粮食和工资。补充新兵与训练老兵是不同的军队管理工作。',
  '俘虏可以被赎回或留在队伍中，但携带过多俘虏会影响行动。贵族俘虏涉及势力关系和战争谈判。',
  '债务应记录借款人、债权人、金额和约定。偿还债务是实际资产变动，口头承诺不等于已完成付款。',
  '商人出售货物，买卖双方需要明确物品数量和价款。展示物品和真正交付物品不是同一个行为。',
  '同伴可以加入队伍并承担职责。安排同伴执行任务时，需要确认人物身份、当前所在位置和可用状态。',
  '领地属于具体家族，转移城市或城堡会影响所有权。只有实际执行完成后，才能把转移写成既成事实。',
  '决斗是双方约定的对抗，提出挑战与真正开始战斗不同。胜负应依据发生的战斗结果，而不是对话声明。',
  '场景中的带路与跟随以当前人物为目标，传唤需要找到对应的人物。大地图行军与场景移动应分别处理。'
 ]
 short=[d[:30] for d in docs]
 medium=[d+'相关决定应根据当前局势、人物身份、双方关系与实际资源作出，传闻和承诺不能代替已经发生的事实。' for d in docs]
 long=[(d+'这段资料用于独立长文本检索测试，需要结合前后文判断相关性，不能把假设记作实际发生的事件。')*12 for d in docs]
 return [
  {'id':'short-b1','query':'商队运输货物时有什么风险？','documents':short[4:5]},
  {'id':'short-b4','query':'城镇忠诚度和税收有什么关系？','documents':[short[i] for i in [7,4,6,1]]},
  {'id':'short-b8','query':'北部帝国为什么与其他帝国发生内战？','documents':short[:8]},
  {'id':'medium-b8','query':'我想偿还借款，如何判断债务已经真正还清？','documents':[medium[i] for i in [10,11,7,4,12,13,9,14]]},
  {'id':'long-b8','query':'攻城时守军的粮食、驻军和城墙会怎样影响战斗？','documents':long[:8]},
  {'id':'long-b16','query':'怎样区分领地转移的口头承诺与真正完成的交付？','documents':long}
 ]

def protected(module):
 paths=[RERANKER,MODELSTORE,REPO/'AnimusForge.csproj']
 paths+=list((REPO/'scripts/build').glob('*.ps1'))
 paths+=list((module/'ONNX/reranker').glob('*'))
 paths+=list((module/'bin/Win64_Shipping_Client').glob('*onnx*.dll'))
 paths+=[module/'SubModule.xml']
 return {str(p):{'sha256':sha(p),'bytes':p.stat().st_size} for p in paths if p.is_file()}

def build(root,module):
 gen=root/'generated';gen.mkdir(exist_ok=True)
 original=RERANKER.read_bytes();text=original.decode('utf-8-sig')
 needle='sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;'
 if text.count(needle)!=1:raise ValueError('Session-options hook anchor changed; abort rather than rewrite production.')
 insertion = ('\r\n' if '\r\n' in text else '\n')+'\t\t\t\tBenchmarkHooks.Configure(sessionOptions);'
 patched=text.replace(needle,needle+insertion)
 if patched.replace(needle+insertion,needle)!=text:raise AssertionError('More than a hook changed.')
 (gen/'OnnxCrossEncoderReranker.original.cs').write_bytes(original)
 (gen/'OnnxCrossEncoderReranker.benchmark.cs').write_bytes((b'\xef\xbb\xbf' if original.startswith(b'\xef\xbb\xbf') else b'') + patched.encode('utf-8'))
 (gen/'AnimusForgeModelStore.cs').write_bytes(MODELSTORE.read_bytes())
 save(root/'source-derivation.json',{'source':str(RERANKER),'sourceSha256':sha(RERANKER),'generatedSha256':sha(gen/'OnnxCrossEncoderReranker.benchmark.cs'),'onlyChange':'Insert BenchmarkHooks.Configure(sessionOptions) after existing graph-optimization assignment','exactReversionEqualsOriginalText':True,'unusedEngineNamespaceAdapter':'Empty namespace only; no TaleWorlds API is used by this source', 'modelStoreSha256':sha(MODELSTORE)})
 env=os.environ.copy();env.update({'NUGET_PACKAGES':str(root/'deps/nuget'),'DOTNET_CLI_HOME':str(root/'deps/dotnet-home'),'DOTNET_NOLOGO':'1','DOTNET_CLI_TELEMETRY_OPTOUT':'1','DOTNET_SKIP_FIRST_TIME_EXPERIENCE':'1','NUGET_HTTP_CACHE_PATH':str(root/'deps/nuget-http-cache')})
 native=root/'deps/native/ort-1.22.0'
 for flavor in ['legacy','matched']:
  app=gen/flavor;app.mkdir(exist_ok=True)
  runner=(HERE/'Runner.cs').read_text(encoding='utf-8-sig')
  if flavor=='legacy':
   start=runner.index('                var cuda = new OrtCUDAProviderOptions();')
   end=runner.index('                cuda.Dispose();',start)+len('                cuda.Dispose();')
   runner=runner[:start]+'                throw new InvalidOperationException("Legacy CPU baseline cannot use CUDA.");'+runner[end:]
  (app/'Runner.cs').write_text(runner,encoding='utf-8')
  references='''<PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net472" Version="1.0.3" PrivateAssets="all" /><PackageReference Include="Newtonsoft.Json" Version="13.0.3" />'''
  if flavor=='matched':references+=f'<PackageReference Include="Microsoft.ML.OnnxRuntime.Managed" Version="{ORT_VERSION}" />'
  else:
   b=module/'bin/Win64_Shipping_Client'
   for name in ['Microsoft.ML.OnnxRuntime','System.Memory','System.Buffers','System.Runtime.CompilerServices.Unsafe']:
    references+=f'<Reference Include="{name}"><HintPath>{xml.escape(str(b/(name+".dll")))}</HintPath><Private>true</Private></Reference>'
   references+='<PackageReference Include="System.Numerics.Vectors" Version="4.5.0" />'
  project=f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><PlatformTarget>x64</PlatformTarget><LangVersion>10.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects><GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType></PropertyGroup><ItemGroup>{references}<Compile Include="Runner.cs"/><Compile Include="../OnnxCrossEncoderReranker.benchmark.cs"/><Compile Include="../AnimusForgeModelStore.cs"/></ItemGroup></Project>'''
  csproj=app/'Benchmark.csproj';csproj.write_text(project,encoding='utf-8')
  log=root/f'build-{flavor}.log'
  with log.open('w',encoding='utf-8') as f:
   ret=subprocess.run(['dotnet','build',str(csproj),'-c','Release','--nologo','-v','minimal','-p:UseSharedCompilation=false','--disable-build-servers'],env=env,stdout=f,stderr=subprocess.STDOUT)
  if ret.returncode:raise RuntimeError(log.read_text(encoding='utf-8'))
  bindir=app/'bin/Release/net472'
  files=(module/'bin/Win64_Shipping_Client').glob('onnx*.dll') if flavor=='legacy' else native.glob('*.dll')
  for f in files:shutil.copy2(f,bindir/f.name)
 return gen

class Telemetry:
 def __init__(self,path):self.path=path;self.rows=[];self.proc=None;self.thread=None
 def start(self):
  self.proc=subprocess.Popen(['nvidia-smi','--query-gpu=timestamp,index,name,memory.used,utilization.gpu,power.draw','--format=csv,noheader,nounits','--loop-ms=200'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8')
  def read():
   for line in self.proc.stdout:
    try:
     fields=next(csv.reader([line]));self.rows.append({'wallUnix':time.time(),'gpuTimestamp':fields[0].strip(),'index':int(fields[1]),'name':fields[2].strip(),'memoryMiB':float(fields[3]),'utilizationPct':float(fields[4]),'powerW':None if 'N/A' in fields[5] else float(fields[5])})
    except (ValueError,IndexError):pass
  self.thread=threading.Thread(target=read,daemon=True);self.thread.start();time.sleep(1)
 def stop(self,start,end):
  time.sleep(.4);self.proc.terminate();self.proc.wait(timeout=10);self.thread.join(timeout=5)
  save(self.path,self.rows)
  baseline=[r['memoryMiB'] for r in self.rows if r['index']==0 and r['wallUnix']<start]
  during=[r for r in self.rows if r['index']==0 and start<=r['wallUnix']<=end]
  if not baseline or not during:raise RuntimeError('GPU telemetry missing.')
  base=statistics.median(baseline);peak=max(r['memoryMiB'] for r in during)
  return {'deviceWide':True,'sampleIntervalMs':200,'baselineMiB':base,'peakMiB':peak,'peakDeltaMiB':peak-base,'maxUtilizationPct':max(r['utilizationPct'] for r in during),'samples':len(during)}

def run_one(root,module,flavor,backend,round_index,repetitions,profile=False):
 name=f'{flavor}-{backend}-'+('profile' if profile else f'round{round_index}')
 exe=root/f'generated/{flavor}/bin/Release/net472/Benchmark.exe'
 result=root/'runs'/f'{name}.json';result.parent.mkdir(exist_ok=True)
 env=os.environ.copy();env['PATH']=str(root/'deps/native/cuda-12.8')+os.pathsep+str(exe.parent)+os.pathsep+env['PATH']
 command=[str(exe),'--module-root',str(module),'--backend',backend,'--dataset',str(root/'dataset.json'),'--result',str(result),'--repetitions',str(repetitions)]
 if profile:
  p=root/'profiles';p.mkdir(exist_ok=True);command+=['--profile-prefix',str(p/name)]
 telemetry=Telemetry(root/'runs'/f'{name}-telemetry.json');telemetry.start();start=time.time()
 print('RUN',name,flush=True)
 try:
  with (root/'runs'/f'{name}.log').open('w',encoding='utf-8') as f:
   ret=subprocess.run(command,env=env,stdout=f,stderr=subprocess.STDOUT,timeout=600)
 finally:end=time.time();gpu=telemetry.stop(start,end)
 data=json.loads(result.read_text(encoding='utf-8-sig')) if result.exists() else {'ok':False,'error':'No result file'}
 data.update({'command':command,'name':name,'flavor':flavor,'telemetry':gpu,'processWallMs':(end-start)*1000,'exitCode':ret.returncode,'profilePass':profile});save(result,data)
 if ret.returncode or not data.get('ok'):raise RuntimeError(name+' failed: '+str(data.get('error')))
 print('OK',name,'init_ms',round(data['initializationMs'],2),'GPU_peak_delta_MiB',gpu['peakDeltaMiB'],flush=True)
 return data

def quantile(values,q):
 s=sorted(values);pos=(len(s)-1)*q;lo=int(pos);hi=min(lo+1,len(s)-1);return s[lo]+(s[hi]-s[lo])*(pos-lo)
def summary(values):return {'n':len(values),'meanMs':statistics.mean(values),'p50Ms':statistics.median(values),'p95Ms':quantile(values,.95),'minMs':min(values),'maxMs':max(values)}
def compare_scores(a,b,label):
 comparisons=[]
 if len(a['scenarios'])!=len(b['scenarios']):raise AssertionError('Scenario count mismatch')
 for sa,sb in zip(a['scenarios'],b['scenarios']):
  if sa['id']!=sb['id']:raise AssertionError('Scenario mismatch')
  x=sa['cacheMiss'][0]['scores'];y=sb['cacheMiss'][0]['scores']
  if len(x)!=len(y) or not x:raise AssertionError('Score count mismatch')
  orderx=sorted(range(len(x)),key=lambda i:(-x[i],i));ordery=sorted(range(len(y)),key=lambda i:(-y[i],i))
  k=min(2,len(x));err=[abs(u-v) for u,v in zip(x,y)]
  comparisons.append({'id':sa['id'],'maxAbsError':max(err),'meanAbsError':statistics.mean(err),'withinAbsTolerance1e-5':max(err)<=1e-5,'sameTop1':orderx[0]==ordery[0],'sameTop2Set':set(orderx[:k])==set(ordery[:k]),'sameFullOrder':orderx==ordery,'cpuOrder':orderx,'otherOrder':ordery,'scoreCount':len(x)})
 return {'label':label,'cases':comparisons,'maxAbsError':max(c['maxAbsError'] for c in comparisons),'allWithinAbsTolerance1e-5':all(c['withinAbsTolerance1e-5'] for c in comparisons),'allSameTop1':all(c['sameTop1'] for c in comparisons),'allSameTop2Set':all(c['sameTop2Set'] for c in comparisons),'allSameFullOrder':all(c['sameFullOrder'] for c in comparisons)}

def aggregate(root,runs,profile,before,after,environment):
 groups={}
 for flavor,backend in [('legacy','cpu'),('matched','cpu'),('matched','cuda')]:
  key=flavor+'-'+backend;subset=[r for r in runs if r['flavor']==flavor and r['backend']==backend]
  cases=[]
  for i,item in enumerate(subset[0]['scenarios']):
   selected=[r['scenarios'][i] for r in subset]
   cases.append({'id':item['id'],'batchSize':item['batchSize'],'tokenLengths':item['tokenLengths'],'cacheMiss':summary([p['ms'] for s in selected for p in s['cacheMiss']]),'cacheHit':summary([p['ms'] for s in selected for p in s['cacheHit']]),'firstShapeMiss':summary([s['firstShapeMiss']['ms'] for s in selected])})
  groups[key]={'cases':cases,'initialization':summary([r['initializationMs'] for r in subset]),'gpuTelemetry':[r['telemetry'] for r in subset]}
 representative={k:next(r for r in runs if r['flavor']+'-'+r['backend']==k) for k in groups}
 consistency=[compare_scores(representative['legacy-cpu'],representative['matched-cpu'],'deployed CPU 1.18 vs matched CPU 1.22'),compare_scores(representative['matched-cpu'],representative['matched-cuda'],'matched CPU 1.22 vs CUDA 1.22')]
 for comparison,left,right in [(consistency[0],'legacy-cpu','matched-cpu'),(consistency[1],'matched-cpu','matched-cuda')]:
  checks=[]
  left_runs=[r for r in runs if r['flavor']+'-'+r['backend']==left]
  right_runs=[r for r in runs if r['flavor']+'-'+r['backend']==right]
  for index,(a,b) in enumerate(zip(left_runs,right_runs)):
   checks.append(compare_scores(a,b,f'round{index+1}: {left} vs {right}'))
  comparison['roundChecks']=checks
  comparison['maxAbsError']=max(c['maxAbsError'] for c in checks)
  for key in ['allWithinAbsTolerance1e-5','allSameTop1','allSameTop2Set','allSameFullOrder']:
   comparison[key]=all(c[key] for c in checks)

 stability=[]
 for r in runs:
  for s in r['scenarios']:
   ref=s['cacheMiss'][0]['scores']
   error=max(abs(a-b) for phase in ['cacheMiss','cacheHit'] for call in s[phase] for a,b in zip(ref,call['scores']))
   stability.append({'run':r['name'],'case':s['id'],'maxAbsErrorAcrossCalls':error})
 events=json.loads(pathlib.Path(profile['profilePath']).read_text(encoding='utf-8'))
 counts={};durations={};ops={};cpu_input_types=set()
 for event in events:
  provider=event.get('args',{}).get('provider')
  if provider:
   counts[provider]=counts.get(provider,0)+1;durations[provider]=durations.get(provider,0)+event.get('dur',0)
   if provider=='CPUExecutionProvider':
    for item in event.get('args',{}).get('input_type_shape',[]):cpu_input_types.update(item.keys())
   name=event.get('args',{}).get('op_name','unknown');ops.setdefault(provider,{})[name]=ops.setdefault(provider,{}).get(name,0)+1
 save(root/'provider-profile-summary.json',{'eventCounts':counts,'summedKernelDurationUs':durations,'operatorCounts':ops,'cpuInputTypes':sorted(cpu_input_types),'note':'Separate diagnostic pass; timings may overlap and are not wall-clock speedup.'})
 if not counts.get('CUDAExecutionProvider') or not ops.get('CUDAExecutionProvider',{}).get('MatMul'):raise RuntimeError('No CUDA provider kernel event was observed; do not claim GPU execution.')
 result={'environment':environment,'groups':groups,'consistency':consistency,'withinRunStability':stability,'providerProfile':{'eventCounts':counts,'operatorCounts':ops,'cpuInputTypes':sorted(cpu_input_types)},'protectedHashesUnchanged':before==after,'protectedBefore':before,'protectedAfter':after,'limitations':['实际测试显卡：'+environment['gpu']+'；不外推其他显卡的倍率','Offline single-worker synthetic workloads; no in-game FPS, concurrent requests or real player corpus measured','GPU memory telemetry is device-wide at 200ms sampling, not exact per-process or sub-sample peak','CPU defaults, game not running; CPU pool contention under game load not measured','Synthetic long-text fixtures deliberately reach 512 tokens; their multi-second CPU latency is a stress case, not typical conversation latency','Initialization is fresh process with OS file cache uncontrolled, not guaranteed disk-cold','Matched ORT version is 1.22.0; production CPU baseline uses deployed 1.18 binary','Absolute score tolerance 1e-5 is a benchmark diagnostic, not a gameplay acceptance rule']}
 save(root/'summary.json',result)
 lines=['# 独立重排序模型 CPU / CUDA 对照报告','',f"- 时间（北京时间）：{environment['localDateTime']}",f"- GPU：{environment['gpu']}",f"- CPU：{environment.get('cpu','not captured')}；内存：{environment.get('physicalRamGiB','not captured')} GiB",'- 状态：独立离线实测；未做游戏内验收。','- 正式模组、模型、检索规则、一键脚本和游戏模块未修改。','- 测试复用生产分词、512 上限、padding、Sigmoid、缓存、批量及单条回退实现；生成副本仅插入 Session 配置 hook。','- CUDA 使用原模型 FP32，关闭 TF32；不使用 FP16、量化或改变候选规则。','- 单次耗时包含分词、张量准备、推理和返回分数，不是纯 kernel 时间；logger 是独立测试适配器，不包含游戏日志和 UI 成本。','','## 热态缓存未命中：P50 / P95（毫秒）','', '| 场景 | batch / padded tokens | 部署 CPU 1.18 | 对照 CPU 1.22 | CUDA 1.22 | CUDA 对同版 CPU 提速 |','|---|---|---:|---:|---:|---:|']
 for i,c in enumerate(groups['legacy-cpu']['cases']):
  new=groups['matched-cpu']['cases'][i];gpu=groups['matched-cuda']['cases'][i]
  cell=lambda s:f"{s['cacheMiss']['p50Ms']:.2f} / {s['cacheMiss']['p95Ms']:.2f}"
  lines.append(f"| {c['id']} | {c['batchSize']} / {max(c['tokenLengths'])} | {cell(c)} | {cell(new)} | {cell(gpu)} | {new['cacheMiss']['p50Ms']/gpu['cacheMiss']['p50Ms']:.2f}x |")
 lines+=['','每格为 P50 / P95；样本来自独立进程重复轮次。升级 ORT 的收益与 CUDA 的收益分列，不能混为显卡提速。','','## 启动、缓存命中和显存','', '| 组别 | 初始化 P50 ms | 首次短 batch 推理 P50 ms | 缓存命中 P50 范围 ms | 设备峰值 MiB | 基线增量峰值 MiB |','|---|---:|---:|---:|---:|---:|']
 for k,g in groups.items():
  hits=[c['cacheHit']['p50Ms'] for c in g['cases']]
  lines.append(f"| {k} | {g['initialization']['p50Ms']:.2f} | {g['cases'][0]['firstShapeMiss']['p50Ms']:.2f} | {min(hits):.4f}–{max(hits):.4f} | {max(t['peakMiB'] for t in g['gpuTelemetry']):.0f} | {max(t['peakDeltaMiB'] for t in g['gpuTelemetry']):.0f} |")
 lines+=['','显存来自 nvidia-smi 全设备采样；基线增量是估计，不是精确进程显存。200ms 采样可能漏掉瞬时峰值。','','## 分数与排序一致性','']
 for c in consistency:
  lines += [f"### {c['label']}",f"- 最大绝对分数差：{c['maxAbsError']:.9g}",f"- 全部满足绝对差 ≤ 1e-5：{c['allWithinAbsTolerance1e-5']}",f"- Top1 / Top2 集合 / 完整顺序一致：{c['allSameTop1']} / {c['allSameTop2Set']} / {c['allSameFullOrder']}",'']
 lines+=['## CUDA 真实执行证据','', '单独 profiling 进程的 provider kernel 事件计数：`'+json.dumps(counts)+'`。详细算子分配见 provider-profile-summary.json。CPU 上少量 shape 等算子不等于整体回退，但 CUDA 未支持的计算必须如实列出。','','## 边界与未验证','']
 lines += ['- '+x for x in result['limitations']]
 lines += ['',f"- 保护文件 SHA256 前后完全一致：{before==after}",'- 原始结果、分数、排序和采样分别在 runs/、summary.json、profiles/ 中。','- 独立工具可在 RTX 5060 机器上用相同命令复测；不能从本机结果推断准确倍率。','', '## 初步建议','', f"本机 CUDA 全场景热态 P50 较同版 CPU 更低：{all(c['cacheMiss']['p50Ms']>g['cacheMiss']['p50Ms'] for c,g in zip(groups['matched-cpu']['cases'],groups['matched-cuda']['cases']))}；数值容差 / Top2 集合保持一致：{consistency[1]['allWithinAbsTolerance1e-5']} / {consistency[1]['allSameTop2Set']}。可据此继续评估可选 CUDA 重排序后端，但不建议未经实机验证就默认开启或全部 GPU 化。显存采样增量峰值为 {max(t['peakDeltaMiB'] for t in groups['matched-cuda']['gpuTelemetry'])/1024:.2f} GiB，初始化和首次调用另有代价。目标显卡复测与单独授权接入后，仍需测试游戏显存压力、并发、失败回退及帧时间；向量模型保留 CPU。" ]
 (root/'REPORT.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
 return result

def main():
 p=argparse.ArgumentParser();p.add_argument('--module-root',required=True);p.add_argument('--output',required=True);p.add_argument('--rounds',type=int,default=3);p.add_argument('--repetitions',type=int,default=10);p.add_argument('--build-only',action='store_true');p.add_argument('--skip-build',action='store_true');p.add_argument('--resume',action='store_true');a=p.parse_args()
 if a.rounds<=0 or a.repetitions<=0:raise ValueError('rounds and repetitions must be positive.')
 root=safe_output(a.output);root.mkdir(parents=True,exist_ok=True);module=pathlib.Path(a.module_root).resolve()
 if module.is_relative_to(root) or root.is_relative_to(module):raise ValueError('Output and game module must be disjoint.')
 before=protected(module)
 if a.resume:
  previous=json.loads((root/'protected-before.json').read_text(encoding='utf-8'))
  if previous!=before:raise ValueError('Cannot resume: original source/model/runtime protection manifest changed.')
  old_environment=json.loads((root/'environment.json').read_text(encoding='utf-8'))
  if old_environment['datasetSha256']!=sha(root/'dataset.json') or json.loads((root/'dataset.json').read_text(encoding='utf-8'))!=dataset():raise ValueError('Cannot resume: dataset changed.')
  current_gpu=subprocess.check_output(['nvidia-smi','--query-gpu=name,driver_version,memory.total','--format=csv,noheader'],text=True).strip()
  if current_gpu!=old_environment['gpu']:raise ValueError('Cannot resume: GPU or driver differs; use a new output directory.')
 save(root/'protected-before.json',before);save(root/'dataset.json',dataset())
 environment={'gitHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=REPO,text=True).strip(),'gpu':subprocess.check_output(['nvidia-smi','--query-gpu=name,driver_version,memory.total','--format=csv,noheader'],text=True).strip(),'localDateTime':datetime.now(timezone(timedelta(hours=8))).isoformat(),'datasetSha256':sha(root/'dataset.json'),'rounds':a.rounds,'repetitionsPerCasePerRound':a.repetitions,'cudaTf32':False,'graphOptimization':'ORT_ENABLE_EXTENDED','cpuThreadSettings':'ORT defaults, same as production','moduleRoot':str(module)}
 save(root/'environment.json',environment)
 if not a.skip_build:build(root,module)
 else:
  if (root/'generated/OnnxCrossEncoderReranker.original.cs').read_bytes()!=RERANKER.read_bytes() or (root/'generated/AnimusForgeModelStore.cs').read_bytes()!=MODELSTORE.read_bytes():raise ValueError('Cannot reuse build: source snapshot differs.')
 if a.build_only:return
 runs=[]
 variants=[('legacy','cpu'),('matched','cpu'),('matched','cuda')]
 try:
  for r in range(a.rounds):
   # Rotate run order across rounds to reduce order / thermal bias.
   for flavor,backend in variants[r%3:]+variants[:r%3]:
    prior=root/'runs'/f'{flavor}-{backend}-round{r+1}.json'
    old=json.loads(prior.read_text(encoding='utf-8')) if a.resume and prior.exists() else None
    if old and old.get('ok') and old.get('exitCode')==0 and all(len(case['cacheMiss'])==a.repetitions and len(case['cacheHit'])==a.repetitions for case in old['scenarios']):
     print('RESUME',old['name'],flush=True);runs.append(old)
    else:runs.append(run_one(root,module,flavor,backend,r+1,a.repetitions))
  profile=run_one(root,module,'matched','cuda',0,1,True)
 finally:
  after=protected(module);save(root/'protected-after.json',after)
  if before!=after:raise RuntimeError('Protected source/model/dependency hashes changed during benchmark; do not silently accept.')
 result=aggregate(root,runs,profile,before,after,environment)
 print('REPORT',root/'REPORT.md',flush=True)
 print('CONSISTENCY',json.dumps(result['consistency'],ensure_ascii=False),flush=True)
if __name__=='__main__':main()
