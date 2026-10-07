using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading;
namespace AnimusForge;
internal readonly struct SceneHistoryScalarRecord {
 internal readonly bool Exists;internal readonly long EventSequence;internal readonly string Role,Content,TargetName,SpeakerName;
 internal SceneHistoryScalarRecord(ConversationMessage m){Exists=m!=null;EventSequence=m?.EventSequence??0;Role=m?.Role;Content=m?.Content;TargetName=m?.TargetName;SpeakerName=m?.SpeakerName;}
}
// Sole scene-history authority. Uses the existing adapter gate to preserve atomic multi-write ordering.
internal sealed class SceneConversationHistoryOwner {
 private readonly object _gate;private readonly Dictionary<int,List<ConversationMessage>> _npcHistory=new();private readonly List<ConversationMessage> _publicHistory=new();
 internal SceneConversationHistoryOwner(object gate){_gate=gate??throw new ArgumentNullException(nameof(gate));}
 internal void Reset(){lock(_gate){_npcHistory.Clear();_publicHistory.Clear();}}
 internal void AppendPublic(ConversationMessage message){lock(_gate){_publicHistory.Add(message);}}
 internal string CaptureIllustrationDialogue()
 {
  lock (_gate)
  {
   int start = 0, rounds = 0;
   // Only requested on click. Keep complete replies/fact lines, including every speaker.
   for (int i = _publicHistory.Count - 1; i >= 0; i--)
    if (string.Equals(_publicHistory[i]?.Role, "user", StringComparison.OrdinalIgnoreCase))
    { start = i; if (++rounds == 2) break; }
   var output = new StringBuilder();
   for (int i = start; i < _publicHistory.Count; i++)
   {
    ConversationMessage line = _publicHistory[i];
    if (line == null || string.IsNullOrWhiteSpace(line.Content)) continue;
    string speaker = string.Equals(line.Role, "user", StringComparison.OrdinalIgnoreCase) ? "玩家" : line.SpeakerName ?? line.Role;
    output.Append(speaker);
    if (!string.IsNullOrWhiteSpace(line.TargetName)) output.Append(" → ").Append(line.TargetName);
    output.Append("：").AppendLine(line.Content);
   }
   return output.ToString();
  }
 }
 internal void AppendNpc(int index,ConversationMessage message){lock(_gate){if(!_npcHistory.ContainsKey(index))_npcHistory[index]=new List<ConversationMessage>();_npcHistory[index].Add(message);}}
 internal void AppendPair(int index,ConversationMessage pub,ConversationMessage priv){lock(_gate){_publicHistory.Add(pub);if(!_npcHistory.TryGetValue(index,out var list)||list==null)_npcHistory[index]=list=new();list.Add(priv);}}
 internal void PruneNpcFacts(int index){lock(_gate){SceneHistoryProjectionOwner.RemoveExpiredSingleUseSceneNpcFacts(_npcHistory[index]);}}
 internal void PrunePublicFacts(){lock(_gate){SceneHistoryProjectionOwner.RemoveExpiredSingleUseSceneNpcFacts(_publicHistory);}}
 internal void RollbackPlayerEvent(long sequence){lock(_gate){_publicHistory.RemoveAll(m=>m!=null&&m.EventSequence==sequence&&string.Equals(m.Role,"user",StringComparison.OrdinalIgnoreCase));foreach(var list in _npcHistory.Values)list?.RemoveAll(m=>m!=null&&m.EventSequence==sequence&&string.Equals(m.Role,"user",StringComparison.OrdinalIgnoreCase));}}
 internal List<ConversationMessage> CapturePublic(){lock(_gate){return _publicHistory.Select(Clone).ToList();}}
 internal List<string> CaptureVisiblePublicLines(int viewer, string heroId, string npcName, bool npcAddress, string playerName, int historyLimit, int auxiliaryLimit = -1)
 {
     lock (_gate)
     {
         var lines = BuildVisibleSceneHistoryLines(_publicHistory, viewer, npcName, npcAddress, heroId, playerName, historyLimit);
         if (auxiliaryLimit < 0 || lines == null || lines.Count <= auxiliaryLimit) return lines;
         return lines.Skip(Math.Max(0, lines.Count - auxiliaryLimit)).ToList();
     }
 }
 internal long PublicFingerprint(){lock(_gate){int count=_publicHistory.Count;long last=count>0?(_publicHistory[count-1]?.EventSequence??0):0;return(last<<16)^count;}}
 internal int PublicCount {get{lock(_gate){return _publicHistory.Count;}}}
 internal SceneHistoryScalarRecord ReadPublicScalar(int index){lock(_gate){return new SceneHistoryScalarRecord(_publicHistory[index]);}}
 internal void AppendDiagnostics(StringBuilder output){lock(_gate){int entries=0;foreach(var list in _npcHistory.Values)entries+=list?.Count??0;output.Append(" scenePublicEntries=").Append(_publicHistory.Count).Append(" sceneNpcBuckets=").Append(_npcHistory.Count).Append(" sceneNpcEntries=").Append(entries);}}
 internal bool CapturePrivateFactForPromotion(int agent,string shared,out int index,out ConversationMessage candidate){lock(_gate){index=-1;candidate=null;if(!_npcHistory.TryGetValue(agent,out var list)||list==null)return false;for(int i=list.Count-1;i>=0;i--){var m=list[i];if(m!=null&&string.Equals(m.Role??"","system",StringComparison.OrdinalIgnoreCase)&&string.Equals(m.Content??"",shared,StringComparison.Ordinal)){index=i;candidate=Clone(m);return true;}}return false;}}
 internal bool ApplyPrivateFactPromotion(int agent,int index,string shared,ConversationMessage replacement){lock(_gate){if(!_npcHistory.TryGetValue(agent,out var list)||list==null||index<0||index>=list.Count)return false;var m=list[index];if(m==null||!string.Equals(m.Role??"","system",StringComparison.OrdinalIgnoreCase)||!string.Equals(m.Content??"",shared,StringComparison.Ordinal))return false;m.Content=replacement.Content;m.TargetAgentIndex=replacement.TargetAgentIndex;m.TargetHeroId=replacement.TargetHeroId;m.SpeakerHeroId=replacement.SpeakerHeroId;m.VisibleAgentIndices=replacement.VisibleAgentIndices;m.VisibleHeroIds=replacement.VisibleHeroIds;return true;}}
 internal string LatestNpcUtterance(int agent){lock(_gate){if(_npcHistory.TryGetValue(agent,out var list)&&list!=null&&list.Count>0){string text=SceneHistoryProjectionOwner.GetLatestSceneNpcUtteranceFromHistory(list,agent);if(!string.IsNullOrWhiteSpace(text))return text;}return SceneHistoryProjectionOwner.GetLatestSceneNpcUtteranceFromHistory(_publicHistory,agent);}}
 private static ConversationMessage Clone(ConversationMessage m)=>m==null?null:new ConversationMessage{EventSequence=m.EventSequence,GameDayIndex=m.GameDayIndex,GameDate=m.GameDate,GameHour=m.GameHour,Scene=m.Scene,Role=m.Role,Content=m.Content,SpeakerName=m.SpeakerName,SpeakerAgentIndex=m.SpeakerAgentIndex,SpeakerHeroId=m.SpeakerHeroId,TargetAgentIndex=m.TargetAgentIndex,TargetName=m.TargetName,TargetHeroId=m.TargetHeroId,PlayerDistanceMeters=m.PlayerDistanceMeters,VisibleAgentIndices=m.VisibleAgentIndices==null?null:new(m.VisibleAgentIndices),VisibleHeroIds=m.VisibleHeroIds==null?null:new(m.VisibleHeroIds)};
internal List<ConversationMessage> CaptureNpc(int npcAgentIndex, string viewerHeroId, ConversationSpeechTextOptions options)
	{
		lock (_gate)
		{
			List<ConversationMessage> list = null;

			if (!string.IsNullOrWhiteSpace(viewerHeroId))
			{
				List<ConversationMessage> merged = new List<ConversationMessage>();
				HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
				void addVisible(IEnumerable<ConversationMessage> source)
				{
					if (source == null)
					{
						return;
					}
					foreach (ConversationMessage msg in source)
					{
						if (msg == null || !SceneHistoryProjectionOwner.IsSceneHistoryVisibleToAgentOrHero(msg, npcAgentIndex, viewerHeroId))
						{
							continue;
						}
						string key = SceneHistoryProjectionOwner.BuildSceneHistoryMergeKey(msg, options);
						if (!seen.Add(key))
						{
							continue;
						}
						merged.Add(msg);
					}
				}
				if (_npcHistory != null)
				{
					if (_npcHistory.TryGetValue(npcAgentIndex, out list) && list != null)
					{
						addVisible(list);
					}
				}
				addVisible(_publicHistory);
				if (_npcHistory != null)
				{
					foreach (KeyValuePair<int, List<ConversationMessage>> pair in _npcHistory)
					{
						if (pair.Key == npcAgentIndex || pair.Value == null || !SceneHistoryProjectionOwner.DoesSceneHistoryBucketRelateToHero(pair.Value, viewerHeroId))
						{
							continue;
						}
						addVisible(pair.Value);
					}
				}
				if (merged.Count > 0)
				{
					return SceneHistoryProjectionOwner.SortConversationMessagesByEventSequence(merged).Select(Clone).ToList();
				}
			}
			if (_npcHistory != null && _npcHistory.TryGetValue(npcAgentIndex, out list) && list != null && list.Count > 0)
			{
				return list.Where((ConversationMessage msg) => msg != null && SceneHistoryProjectionOwner.IsSceneHistoryVisibleToAgentOrHero(msg, npcAgentIndex, viewerHeroId)).Select(Clone).ToList();
			}
			if (_publicHistory != null && _publicHistory.Count > 0)
			{
				return _publicHistory.Where((ConversationMessage msg) => msg != null && SceneHistoryProjectionOwner.IsSceneHistoryVisibleToAgentOrHero(msg, npcAgentIndex, viewerHeroId)).Select(Clone).ToList();
			}
		}
		return new List<ConversationMessage>();
	}
internal static List<string> BuildVisibleSceneHistoryLines(List<ConversationMessage> history, int viewerAgentIndex, string targetNpcName, bool useNpcNameAddress, string viewerHeroId, string playerName, int historyLineLimit)
	{
		if (history == null || history.Count == 0)
		{
			return null;
		}

		int conversationCount = 0;
		List<string> list = new List<string>();
		for (int num = history.Count - 1; num >= 0; num--)
		{
			ConversationMessage msg = history[num];
			if (SceneHistoryProjectionOwner.IsSceneHistoryVisibleToAgentOrHero(msg, viewerAgentIndex, viewerHeroId) && HistorySectionProjectionOwner.TryRenderSceneHistoryLine(msg, null, out var line, viewerAgentIndex, targetNpcName, useNpcNameAddress, true, playerName))
			{
				bool isFact = SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(line, out var _);
				if (isFact || conversationCount < historyLineLimit)
				{
					list.Add(line);
					if (!isFact)
					{
						conversationCount++;
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		list.Reverse();
		return list;
	}
}
