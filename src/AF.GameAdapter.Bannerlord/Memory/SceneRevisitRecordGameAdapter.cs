using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
namespace AnimusForge;
internal sealed class SceneRevisitRecordGameAdapter
{
 internal delegate void CapturePlace(out string placeName, out string spotName);
 private readonly SceneRevisitStateOwner _state;
 private readonly Func<int,Hero> _resolveHero;
 private readonly Func<string> _playerName;
 private readonly Func<NpcDataPacket,string> _historyName;
 private readonly Action<string,List<NpcDataPacket>> _recordFact;
 private readonly CapturePlace _capturePlace;
 internal SceneRevisitRecordGameAdapter(SceneRevisitStateOwner state, Func<int,Hero> resolveHero,
     Func<string> playerName, Func<NpcDataPacket,string> historyName,
     Action<string,List<NpcDataPacket>> recordFact, CapturePlace capturePlace)
 { _state=state??throw new ArgumentNullException(nameof(state)); _resolveHero=resolveHero; _playerName=playerName; _historyName=historyName; _recordFact=recordFact; _capturePlace=capturePlace; }
	internal static int GetCurrentCampaignDaySafe()
	{
		try
		{
			return (int)CampaignTime.Now.ToDays;
		}
		catch
		{
			return -1;
		}
	}

	internal static string GetCurrentSettlementIdSafe()
	{
		try
		{
			return (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	internal static string NormalizeSceneRevisitKeyToken(string text)
	{
		string text2 = (text ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = Regex.Replace(text2, "\\s+", " ");
		return text2.Trim();
	}

	internal static string BuildCurrentSceneRevisitKeySafe(CapturePlace capturePlace)
	{
		try
		{
			capturePlace(out var placeName, out var spotName);
			string[] value = new string[4]
			{
				NormalizeSceneRevisitKeyToken(GetCurrentSettlementIdSafe()),
				NormalizeSceneRevisitKeyToken(Mission.Current?.SceneName),
				NormalizeSceneRevisitKeyToken(placeName),
				NormalizeSceneRevisitKeyToken(spotName)
			};
			string text = string.Join("|", value.Where(x => !string.IsNullOrWhiteSpace(x)));
			return text.Trim();
		}
		catch
		{
			return "";
		}
	}

	internal static string BuildSceneHeroRevisitRecordKey(Hero hero, string sceneKey)
	{
		string text = (hero?.StringId ?? "").Trim();
		string text2 = (sceneKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return text + "@" + text2;
	}

	internal static string BuildSceneRevisitFactBody(string playerDisplayName, int elapsedDays)
	{
		string text = string.IsNullOrWhiteSpace(playerDisplayName) ? "玩家" : playerDisplayName.Trim();
		if (elapsedDays <= 0)
		{
			return "今天稍早时候刚与" + text + "见过面。";
		}
		return "距离你上次与" + text + "见面，已有" + elapsedDays + "天了。";
	}

	internal void TryInjectSceneFirstMeetingFactsBeforePlayerMessage(List<NpcDataPacket> nearbyData)
	{
		if (nearbyData == null || nearbyData.Count == 0)
		{
			return;
		}
		List<(NpcDataPacket Npc, string Fact)> list = new List<(NpcDataPacket, string)>();
		lock (_state.Gate)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (!(nearbyDatum?.IsHero ?? false))
				{
					continue;
				}
				Hero hero = _resolveHero(nearbyDatum.AgentIndex);
				string text = (hero?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(text) || _state.FirstMeetingShownThisSession.Contains(text))
				{
					continue;
				}
				string firstMeetingNpcFactTextForPromptIfNeeded = MyBehavior.GetFirstMeetingNpcFactTextForPromptIfNeeded(hero, persistToHistory: false);
				if (string.IsNullOrWhiteSpace(firstMeetingNpcFactTextForPromptIfNeeded))
				{
					continue;
				}
				list.Add((nearbyDatum, firstMeetingNpcFactTextForPromptIfNeeded.Trim()));
				_state.FirstMeetingShownThisSession.Add(text);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			// This fact is written in second person for one observer. Broadcasting it to every
			// nearby NPC can mix different notoriety results into the same prompt.
			_recordFact(list[i].Fact, new List<NpcDataPacket>(1) { list[i].Npc });
		}
	}

	internal void TryInjectSceneRevisitFactsBeforePlayerMessage(List<NpcDataPacket> nearbyData)
	{
		if (nearbyData == null || nearbyData.Count == 0)
		{
			return;
		}
		int currentCampaignDaySafe = GetCurrentCampaignDaySafe();
		if (currentCampaignDaySafe < 0)
		{
			return;
		}
		string text = BuildCurrentSceneRevisitKeySafe(_capturePlace);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string playerDisplayNameForShout = _playerName();
		if (string.IsNullOrWhiteSpace(playerDisplayNameForShout))
		{
			playerDisplayNameForShout = "玩家";
		}
		List<(Hero Hero, string SceneHistoryName, string RecordKey, int ElapsedDays)> list = new List<(Hero, string, string, int)>();
		lock (_state.Gate)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (!(nearbyDatum?.IsHero ?? false))
				{
					continue;
				}
				Hero hero = _resolveHero(nearbyDatum.AgentIndex);
				if (hero == null)
				{
					continue;
				}
				string text2 = BuildSceneHeroRevisitRecordKey(hero, text);
				if (string.IsNullOrWhiteSpace(text2) || !hashSet.Add(text2))
				{
					continue;
				}
				if (_state.HandledThisSession.Contains(text2))
				{
					continue;
				}
				if (_state.Days.TryGetValue(text2, out var value))
				{
					list.Add((hero, _historyName(nearbyDatum), text2, Math.Max(0, currentCampaignDaySafe - value)));
				}
				_state.Days[text2] = currentCampaignDaySafe;
				_state.HandledThisSession.Add(text2);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (var item in list)
		{
			string text3 = string.IsNullOrWhiteSpace(item.SceneHistoryName) ? (item.Hero.Name?.ToString() ?? "对方") : item.SceneHistoryName;
			string text4 = BuildSceneRevisitFactBody(playerDisplayNameForShout, item.ElapsedDays);
			string extraFact = "[AFEF NPC行为补充] " + text3 + text4;
			_recordFact(extraFact, nearbyData);
			MyBehavior.AppendExternalDialogueHistory(item.Hero, null, null, "[AFEF NPC行为补充] " + text4);
		}
	}
}
