"""Link the actual single Memory state owner and its host projections into replay fixtures.
Only engine facts and test fault/counter hooks remain fixture seams.
"""
import re, hashlib
from pathlib import Path
FILES=['Summary/MemoryBusinessStateOwner.IdentityStores.cs','Summary/MemoryRecoveryStateOwner.cs','Recovery/InteractionMemoryRecoveryLedger.cs','Records/DialogueHistoryLedger.cs','Summary/MemoryBusinessStateOwner.cs','Summary/MemoryBusinessStateOwner.Identity.cs','Summary/MemoryBusinessStateOwner.Queues.cs','Summary/MemorySummaryPlanningOwner.cs','Summary/MemorySealingOwner.cs','Summary/MemorySourceFingerprintRules.cs','Summary/MemorySourceFingerprintWriter.cs','Summary/CooperativeMemoryQueueSort.cs','Summary/MemoryMaintenanceWorkBudget.cs','Records/NpcActionLedger.cs']
FIELDS=['_dailyMemoryDrafts','_compressedMemoryBlocks','_memoryOverviewStates','_npcMajorActionSummaries','_pendingWeeklyMemoryMaterialTriggers','_memorySummaryQueue','_memoryOverviewQueue','_npcMajorActionSummaryQueue','_dirtyMemoryOverviewIds','_pendingMemoryOverviewCandidateScanIds','_pendingMemoryOverviewCandidateScanIdSet','_npcMajorActions','_memoryOverviewStateStorage','_npcMajorActionSummaryStorage']
MAP={'_dailyMemoryDraftSealOwnerKeys':'OwnerKeys','_dailyMemoryDraftSealOwnerIndex':'OwnerIndex','_dailyMemoryDraftSealDraftIndex':'DraftIndex','_dailyMemoryDraftSealTargetDay':'TargetDay','_dailyMemoryDraftSealQueuedMajor':'QueuedMajor','_dailyMemoryDraftSealQueued':'Queued','_dailyMemoryDrafts':'_state.Drafts','_memorySummaryQueue':'_state.DailyQueue','_npcMajorActionSummaryQueue':'_state.MajorQueue','_campaignMemoryMaintenanceBudget':'_port.SharedBudget()','ResetDailyMemoryDraftSealSliceState':'Reset'}
RULES=['NormalizeMemoryHeroId','SanitizeDailyMemoryDraftLine']
def owner_anchor(s):
 for a,b in sorted(MAP.items(),key=lambda x:-len(x[0])):s=s.replace(a,b)
 for a in RULES:s=re.sub(r'(?<![\w.])'+a+r'\(', 'MemoryRecordRules.'+a+'(',s)
 return s

def include(root,files,manifest,ex):
 source=(root/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
 projections=[]
 if not any('class DialogueDay' in v for v in files.values()):
  projections.append(ex.declaration(source,'internal class DialogueDay'))
 for field in FIELDS:
  m=re.search(r'^\s*private [^\n]+ '+field+r' \{ get => _memoryBusinessState[^\n]+',source,re.M)
  if not m:continue
  prop=m.group().strip();found=False
  for name in list(files):
   if name not in ['Fixture.cs','Program.cs','Product.cs','Sealing.cs','Business.cs','Terminal.cs']:continue
   pattern=r'(?m)^\s*(?:private |readonly )?(?:List<[^;\n]+>|Dictionary<[^;\n]+>|HashSet<[^;\n]+>|Queue<[^;\n]+>) '+field+r'\s*=.*?;'
   files[name],n=re.subn(pattern,'\n '+prop,files[name]);found|=n>0
  if not found:projections.append(prop)
 if not any(re.search(r'private const int DailyMaintenanceMaxJobsPerTick',v) for v in files.values()):
  projections.append(re.search(r'private const int DailyMaintenanceMaxJobsPerTick = [^;]+;',source).group())
 if not any(re.search(r'\b(?:bool|void) CancelUnavailableHeroCompressionWorkById\(',v) for v in files.values()):
  projections.append(ex.declaration(source,'private bool CancelUnavailableHeroCompressionWorkById('))
 factory=ex.declaration(source,'private MemoryBusinessStateOwner MemoryQueueState').replace('CurrentDay = () => (int)CampaignTime.Now.ToDays','CurrentDay = GetCurrentGameDayIndexSafe')
 owner='private readonly MemoryBusinessStateOwner _memoryBusinessState = new MemoryBusinessStateOwner();'
 files['StateProjections.cs']='using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;namespace AnimusForge {public partial class MyBehavior { '+owner+'\n'+factory+'\n'+'\n'.join(projections)+' }}'
 for contract in ['InteractionContracts.cs','LlmContracts.cs']:
  path=root/'src/AF.Contracts/Internal'/contract
  if contract not in files:files[contract]=path.read_text(encoding='utf-8-sig')
 codec=root/'src/modules/AF.Module.Llm/Protocol/JsonResponseTextCodec.cs'
 if codec.exists():files[codec.name]=codec.read_text(encoding='utf-8-sig')
 for rel in FILES:
  path='src/modules/AF.Module.Memory/'+rel;text=(root/path).read_text(encoding='utf-8-sig')
  if Path(path).name not in files and not (Path(path).stem in ['MemorySealingOwner','MemoryMaintenanceWorkBudget','CooperativeMemoryQueueSort','InteractionMemoryRecoveryLedger'] and any('class '+Path(path).stem in v for v in files.values())):files[Path(path).name]=text
  manifest.append(dict(file=path,sha256=hashlib.sha256(text.encode()).hexdigest(),whole_component=True))

def enable_expression_declarations(ex):
 original=ex.declaration
 def declaration(text,signature,optional=False):
  start=text.find(signature)
  if start<0:return original(text,signature,optional)
  opening=text.find('{',start);arrow=text.find('=>',start);endline=text.find('\n',start)
  if arrow>=0 and (opening<0 or arrow<opening):
   # These actual host expressions contain no statement-bodied lambdas.
   end=text.index(';',arrow)+1
   return text[start:end]
  return original(text,signature,optional)
 ex.declaration=declaration

def statement_body(body):
 if '{' not in body and '=>' in body:
  head,expr=body.rsplit('=>',1)
  return head+'{ '+('' if ' void ' in head else 'return ')+expr.strip()+' }'
 return body
