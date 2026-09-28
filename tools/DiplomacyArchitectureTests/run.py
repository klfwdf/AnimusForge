"""Check actual MSBuild members, deletion parity and executable diplomacy dependency boundaries."""
from pathlib import Path
import argparse,importlib.util,json,subprocess,os
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
BASELINE='19e9bb22'
def read(p):return (ROOT/p).read_text(encoding='utf-8-sig')
def old(p):return subprocess.check_output(['git','show',BASELINE+':'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def load(name,p):
 s=importlib.util.spec_from_file_location(name,ROOT/p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def main():
 p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');p.add_argument('--baseline-dll');p.add_argument('--candidate-dll');a=p.parse_args()
 retired=load('retired','tools/DiplomacyArchitectureTests/retired.py')
 declaration=load('decl','tools/ChannelCutoverBoundaryTests/run.py').declaration
 prior=old(retired.HOST)
 current=read(retired.HOST)
 prior_tick=declaration(prior,'public void OnEngineTick(')
 current_tick=declaration(current,'public void OnEngineTick(')
 assert 'WorldDiplomacyTickApplication.Run(ref source)' in current_tick and 'ProcessCompletedJobs()' not in current_tick, 'Tick predecessor still owns ordering'
 prior=prior.replace(prior_tick,current_tick)
 # These production bodies have moved to a compiled, replayed Application owner.
 # The Roslyn gate below requires each predecessor to be a single forwarder.
 for signature in ('private void ProcessAnalyzedDocument(', 'private void ProcessAnalyzedMultiActionDocument(',
                   'private void FinalizePublishedDocumentAfterAnalysis(', 'private bool TryIncludeResultSettlementTarget('):
  before=declaration(prior,signature);after=declaration(current,signature)
  assert 'WorldDiplomacyDocumentExecutionApplication.' in after, signature
  prior=prior.replace(before,after)
 for signature in ('private void StartDocumentPropagation(', 'private void ReconcileAnalyzedPlayerDeclarationWithReachedCourts('):
  before=declaration(prior,signature);after=declaration(current,signature)
  assert 'WorldDiplomacyPublicationRoutingApplication.' in after, signature
  prior=prior.replace(before,after)
 for signature in ('private void TryScheduleMandatoryCourtResponse(',):
  before=declaration(prior,signature);after=declaration(current,signature)
  assert 'WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(' in after, signature
  prior=prior.replace(before,after)
 query='internal static WorldDiplomacyTimelineRevisionResult QueryWorldMessageTimelineRevision('
 snapshot='internal static bool TryGetTimelineRevisionSnapshot('
 timeline_query='internal static WorldDiplomacyTimelineDocumentsResult QueryTimelineDocuments('
 timeline_state='internal static bool TryGetTimelineState('
 mark_read='internal static bool TryMarkDocumentReadForCommand('
 discussion='private bool CanDiscussWorldDiplomacy('
 discussion_wrapper='public static bool CanDiscussWorldDiplomacyForExternal('
 discussion_snapshot='internal static bool TryCaptureDiscussionCandidate('
 proactive='private bool TryBuildProactiveDiscussion('
 proactive_wrapper='public static bool TryBuildProactiveDiscussionForExternal('
 proactive_speaker='internal static bool TryCaptureProactiveSpeaker('
 proactive_documents='internal static bool TryCaptureProactiveDocuments('
 prior=prior.replace('\t'+declaration(prior,query)+'\n\n','')
 prior=prior.replace('    '+declaration(prior,timeline_query)+'\n\n','')
 prior=prior.replace('\t'+declaration(prior,mark_read)+'\n','')
 prior=prior.replace('\t'+declaration(prior,discussion)+'\n','')
 prior=prior.replace('\t'+declaration(prior,discussion_wrapper)+'\n\n','')
 prior=prior.replace('\t'+declaration(prior,proactive)+'\n','')
 prior=prior.replace('\t'+declaration(prior,proactive_wrapper)+'\n\n','')
 current=current.replace('\t'+declaration(current,snapshot)+'\n\n','')
 current=current.replace('    '+declaration(current,timeline_state)+'\n\n','')
 current=current.replace('\tpublic static bool CanDiscussWorldDiplomacyForExternal(Hero hero) =>\n\t\tDiplomacyModuleServices.World.CanDiscuss(hero?.StringId);\n\n','')
 current=current.replace('\t'+declaration(current,discussion_snapshot)+'\n\n','')
 current=current.replace('\tinternal static bool HasKnownDocumentForDiscussion(Hero hero, string kingdomId) =>\n\t\tResolveInstance()?.GetKnownDocumentIdsForHero(hero, kingdomId).Count > 0;\n\n','')
 current=current.replace('\t'+declaration(current,proactive_wrapper)+'\n\n','')
 current=current.replace('\t'+declaration(current,proactive_speaker)+'\n\n','')
 current=current.replace('\t'+declaration(current,proactive_documents)+'\n\n','')
 current=current.replace('\tinternal static string GetPlayerKingdomNameForProactive() => KingdomName(Clan.PlayerClan?.Kingdom);\n\tinternal static string FormatDateForProactive(int day) => FormatCampaignDate(day);\n\n','')
 assert retired.remove_retired(prior,declaration)==current,'Active behavior body changed beyond retired private declarations and verified R1 revision route'
 print('PASS 17 private method deletions; tick is an Application forwarder; other surviving host text unchanged')
 out=HERE/'.generated';out.mkdir(exist_ok=True)
 # Evaluation only: no game startup, restore, Stage or deployment.
 result=subprocess.run([a.dotnet,'msbuild',str(ROOT/'AnimusForge.csproj'),'-getItem:Compile'],cwd=ROOT,capture_output=True,encoding='utf-8',check=True)
 items=json.loads(result.stdout)['Items']['Compile']
 paths=[Path(item['FullPath']) for item in items]
 payload={'root':str(ROOT),'paths':[str(path) for path in paths], 'retired':[s[s.rfind(' ')+1:].rstrip('(') for s in retired.RETIRED]}
 payload.update(baseline=a.baseline_dll or '',candidate=a.candidate_dll or '')
 manifest=out/'sources.json';manifest.write_text(json.dumps(payload),encoding='utf-8')
 # The baseline host is for a reference-use audit, never compiled into production.
 (out/'prior-host.cs.txt').write_text(prior,encoding='utf-8')
 code=subprocess.call([a.dotnet,'run','--project',str(HERE/'DiplomacyArchitectureTests.csproj'),'-c','Release','--',str(manifest)],cwd=ROOT)
 raise SystemExit(code)
if __name__=='__main__':main()
