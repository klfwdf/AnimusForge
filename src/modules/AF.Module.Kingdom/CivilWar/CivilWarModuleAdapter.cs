using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

internal sealed class CivilWarModuleAdapter : ICivilWarModulePort
{
	private static readonly Regex TagPattern = new Regex("^\\[A:CIVIL_FACTION:(JOIN:(CROWN|OPPOSITION)|RECRUIT|DETONATE)\\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private readonly KingdomCivilWarOwner _owner = new KingdomCivilWarOwner();

	public void Load(string json)
	{
		try
		{
			_owner.Replace(string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<KingdomCivilWarStorage>(json));
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[WARN] Civil war save rejected: " + ex.Message);
			_owner.Replace(null);
		}
	}

	public string Save()
	{
		try { return JsonConvert.SerializeObject(_owner.Storage) ?? ""; }
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[ERROR] Serialize civil war failed: " + ex.Message);
			return "";
		}
	}

	public void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> recentEvents)
	{
		_owner.AdvanceWeek(kingdom, weekIndex, stability, (target, delta) => adjustStability?.Invoke(target, delta), recentEvents);
	}

	public bool HasTrackedKingdom(Kingdom kingdom) => _owner.HasTrackedKingdom(kingdom);

	public int GetSettlementLoyaltyDelta(Settlement settlement) => _owner.GetOppositionLoyaltyDelta(settlement);

	public bool BlocksNewOffensiveWar(Kingdom kingdom) => _owner.IsInOpenCivilWar(kingdom?.StringId);

	public void ApplyPrestigeDelta(string kingdomId, int delta, string reason)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || delta == 0) return;
		WorldDiplomacyBehavior.ApplyExternalPrestigeDelta(kingdomId, delta, reason ?? "内战");
	}

	public IReadOnlyList<CivilWarPanelKingdom> GetPanelKingdoms(int pageIndex, int pageSize, out int pageCount)
	{
		List<KingdomCivilWarKingdomState> states = _owner.ListForPanel().ToList();
		pageSize = Math.Max(1, pageSize);
		pageCount = Math.Max(1, (states.Count + pageSize - 1) / pageSize);
		int page = Math.Max(0, Math.Min(pageIndex, pageCount - 1));
		return states.Skip(page * pageSize).Take(pageSize).Select(KingdomCivilWarOwner.ToPanel).ToList();
	}

	public List<PostprocessRuleEntry> BuildPostprocessRules()
	{
		return new List<PostprocessRuleEntry>
		{
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:JOIN:CROWN]", Description = "玩家明确决定自己的家族加入当前王国已成形的王室派，且当前NPC明确确认时输出。询问、犹豫或派系未成形时禁止。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:JOIN:OPPOSITION]", Description = "玩家明确决定自己的家族加入当前王国已成形的反对派，且当前NPC明确确认时输出。询问、犹豫或派系未成形时禁止。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:RECRUIT]", Description = "玩家已有明确派系，并明确说服当前NPC的整个家族加入该派系，双方都无条件同意时输出。拒绝、身份不明或只是本人入队时禁止。" },
			new PostprocessRuleEntry { Tag = "[A:CIVIL_FACTION:DETONATE]", Description = "玩家明确要求立即引爆当前王国的内战，且当前NPC明确同意执行时输出。讨论、威胁或劝阻时禁止。" }
		};
	}

	public bool TryApplyTag(Hero speaker, string tag, out string message)
	{
		message = "";
		Match match = TagPattern.Match((tag ?? "").Trim());
		if (!match.Success || speaker == null) return false;
		Kingdom kingdom = speaker.Clan?.Kingdom ?? Clan.PlayerClan?.Kingdom;
		string action = match.Groups[1].Value ?? "";
		if (action.StartsWith("JOIN:CROWN", StringComparison.OrdinalIgnoreCase)) return _owner.TryJoinPlayer(kingdom, KingdomCivilWarSide.Crown, out message);
		if (action.StartsWith("JOIN:OPPOSITION", StringComparison.OrdinalIgnoreCase)) return _owner.TryJoinPlayer(kingdom, KingdomCivilWarSide.Opposition, out message);
		if (action.Equals("RECRUIT", StringComparison.OrdinalIgnoreCase)) return _owner.TryRecruitClan(Hero.MainHero, speaker.Clan, kingdom, out message);
		if (action.Equals("DETONATE", StringComparison.OrdinalIgnoreCase))
		{
			int week = Math.Max(1, (int)CampaignTime.Now.ToDays / 7);
			return _owner.TryDetonate(kingdom, week, true, (target, delta) =>
			{
				MyBehavior.TryAdjustKingdomStabilityForExternal(target, delta, "civil_war", out _, out _);
				return 0;
			}, out message);
		}
		return false;
	}
}
