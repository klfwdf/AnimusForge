using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.ViewModelCollection;

public static class NavalUIHelper
{
	public static float GetHealthPercent(this Ship ship)
	{
		if (ship.MaxHitPoints == 0f)
		{
			return 0f;
		}
		return ship.HitPoints / ship.MaxHitPoints * 100f;
	}

	private static Tuple<bool, TextObject> GetIsStringApplicableForShipName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=aw2fR5fK}Ship name cannot be empty"));
		}
		if ((name.Length < 3 && !name.Any((char c) => Common.IsCharAsian(c))) || name.Length > 50)
		{
			TextObject textObject = new TextObject("{=cSLiAJUw}Ship name should be between {MIN} and {MAX} characters");
			textObject.SetTextVariable("MIN", 3);
			textObject.SetTextVariable("MAX", 50);
			return new Tuple<bool, TextObject>(item1: false, textObject);
		}
		if (!name.All((char x) => (char.IsLetterOrDigit(x) || char.IsWhiteSpace(x) || char.IsPunctuation(x)) && x != '{' && x != '}'))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=t9bmsau2}Ship name cannot contain special characters"));
		}
		if (name.StartsWith(" ") || name.EndsWith(" "))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=ol9uYvPl}Ship name cannot start or end with a white space"));
		}
		if (name.Contains("  "))
		{
			return new Tuple<bool, TextObject>(item1: false, new TextObject("{=bX4OPIPP}Ship name cannot contain consecutive white spaces"));
		}
		return new Tuple<bool, TextObject>(item1: true, TextObject.GetEmpty());
	}

	public static Tuple<bool, string> IsStringApplicableForShipName(string name)
	{
		Tuple<bool, TextObject> isStringApplicableForShipName = GetIsStringApplicableForShipName(name);
		return new Tuple<bool, string>(isStringApplicableForShipName.Item1, isStringApplicableForShipName.Item2.ToString());
	}

	public static Ship GetFlagship(PartyBase party)
	{
		return party.FlagShip;
	}

	public static List<TooltipProperty> GetShipyardTooltip(Town town)
	{
		if (town == null)
		{
			return new List<TooltipProperty>();
		}
		List<TooltipProperty> list = new List<TooltipProperty>();
		Building shipyard = town.GetShipyard();
		list.Add(new TooltipProperty(string.Empty, new TextObject("{=4vkUyYui}Shipyard{newline}Level {LEVEL}").SetTextVariable("LEVEL", shipyard.CurrentLevel).ToString(), 0));
		return list;
	}

	public static string GetTownCoastalPatrolTooltip(Town town)
	{
		TextObject textObject = GameTexts.FindText("str_string_newline_string");
		textObject.SetTextVariable("newline", "\n");
		textObject.SetTextVariable("STR1", GameTexts.FindText("str_coastal_patrol"));
		INavalPatrolPartiesCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<INavalPatrolPartiesCampaignBehavior>();
		if (CampaignUIHelper.IsSettlementInformationHidden(town.Settlement, out var _))
		{
			textObject.SetTextVariable("STR2", GameTexts.FindText("str_missing_info_indicator").ToString());
		}
		else if (campaignBehavior.GetNavalPatrolParty(town.Settlement) != null)
		{
			textObject.SetTextVariable("STR2", campaignBehavior.GetNavalPatrolParty(town.Settlement).GetBehaviorText().ToString());
		}
		else
		{
			textObject.SetTextVariable("STR2", campaignBehavior.GetSettlementPatrolStatus(town.Settlement).ToString());
		}
		return textObject.ToString();
	}

	public static string GetPrefabIdOfShipHull(ShipHull shipHull)
	{
		return MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId)?.Prefab ?? string.Empty;
	}
}
