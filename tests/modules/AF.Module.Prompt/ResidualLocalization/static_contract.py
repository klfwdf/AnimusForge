from pathlib import Path
import json,re
R=Path(__file__).resolve().parents[4]; B=R/'content/modules/AF.Module.Prompt/ModuleData'
def load(path):
 def unique(items):
  result={}
  for k,v in items:
   assert k not in result,(path,k)
   result[k]=v
  return result
 return json.loads(path.read_text('utf-8-sig'),object_pairs_hook=unique)
c=load(B/'PromptComposerTexts.zh-CN.json');e=load(B/'PromptComposerTexts.en.json');raw=load(B/'PromptCompatibility.json')
assert c.keys()==e.keys()
for k in c:
 if k.startswith('residual_'):
  assert re.findall(r'\{[A-Za-z_]+\}',c[k])==re.findall(r'\{[A-Za-z_]+\}',e[k]),k
for p in (R/'src/modules/AF.Module.Prompt').rglob('*.cs'):
 for k in re.findall(r'PromptComposerTexts.(?:Get|Format)\("(residual_[^"]+)"',p.read_text('utf-8-sig')):assert k in c and k in e,(p,k)
scene=(R/'src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs').read_text('utf-8-sig')
assert 'promptBody + PromptComposerTexts.Get("npc_initiated_fact_supplement")' in scene
assert 'PlayerCraftedAfefInspectionSuffix => PromptComposerTexts.Raw("history_afef_inspection_suffix")' in scene
history=(R/'src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs').read_text('utf-8-sig')
assert 'MemorySystemFactProjection.ProjectNpcFact(factLine)' in history
assert 'isCurrent ? PromptComposerTexts.Raw("history_section_scope_current") : PromptComposerTexts.Raw("history_section_scope_historical")' in history
assert '【当下行为】' not in history and '【过往行为】' not in history, 'scope markers must stay in the invariant corpus'
assert raw['history_section_scope_current'] == '【当下行为】' and raw['history_section_scope_historical'] == raw['main_history_raw_019'] == '【过往行为】', 'past-fact marker must match every other consumer'
config=(R/'src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs').read_text('utf-8-sig')
assert 'PromptComposerTexts.Format("residual_player_fixed_id", ("ID", playerId))' in config
assert 'return PromptComposerTexts.Get("residual_player_colon") + playerText;' in config
assert c['residual_player_colon']=='玩家: ' and e['residual_player_colon']=='Player: '
assert 'stringBuilder.Append(PromptComposerTexts.Get("residual_player_colon"))' in config
assert 'distance.ToString("0.0")' in config and 'num.ToString("0.00")' in config
retrieval_raw=load(B/"PromptRetrievalCompatibility.json")
assert retrieval_raw["Raw_d4b710cc137d"]=="【玩家当前地点ID】"
assert 'text.IndexOf(PromptRetrievalTexts.Raw("Raw_d4b710cc137d"), StringComparison.OrdinalIgnoreCase)' in config
assert 'text.IndexOf(PromptComposerTexts.Get("residual_location_header"), StringComparison.OrdinalIgnoreCase)' in config
for marker in ['WORLDMAP_ORDER','hero','settlement','DIPLOMACY','KINGDOM_AGENDA']:
 assert any(marker in v for k,v in c.items() if k.startswith('residual_'))
for file in ['RuleBehaviorPrompts.json','RuleBehaviorPrompts.en.json']:
 d=load(B/file);tags=[y['Tag'] for x in d['RulePrompts'] for y in x.get('PostprocessRules',[])]
 assert '[ATT:TROOP_INDEX:COUNT]' in tags and '[ATP:PRISONER_INDEX:COUNT]' in tags
assert raw['afef_player_action']=='[AFEF玩家行为补充]'
assert raw['rule_section_prefix']=='【附加规则:'
print('PASS residual paired resource/reference/formatting and exact protocol-preservation static contracts')
