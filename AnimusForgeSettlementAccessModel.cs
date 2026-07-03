using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class AnimusForgeSettlementAccessModel : SettlementAccessModel
{
	private static readonly TextObject CastleRequestMeetingDisabledText = new TextObject("AnimusForge 已禁用城堡中的“请求与某人会面”。");

	private readonly SettlementAccessModel _inner;

	public AnimusForgeSettlementAccessModel(SettlementAccessModel inner)
	{
		_inner = inner ?? new DefaultSettlementAccessModel();
	}

	public override void CanMainHeroEnterSettlement(Settlement settlement, out AccessDetails accessDetails)
	{
		_inner.CanMainHeroEnterSettlement(settlement, out accessDetails);
	}

	public override void CanMainHeroEnterLordsHall(Settlement settlement, out AccessDetails accessDetails)
	{
		_inner.CanMainHeroEnterLordsHall(settlement, out accessDetails);
	}

	public override void CanMainHeroEnterDungeon(Settlement settlement, out AccessDetails accessDetails)
	{
		_inner.CanMainHeroEnterDungeon(settlement, out accessDetails);
	}

	public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId, out bool disableOption, out TextObject disabledText)
	{
		return _inner.CanMainHeroAccessLocation(settlement, locationId, out disableOption, out disabledText);
	}

	public override bool CanMainHeroDoSettlementAction(Settlement settlement, SettlementAction settlementAction, out bool disableOption, out TextObject disabledText)
	{
		return _inner.CanMainHeroDoSettlementAction(settlement, settlementAction, out disableOption, out disabledText);
	}

	public override bool IsRequestMeetingOptionAvailable(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		bool result = _inner.IsRequestMeetingOptionAvailable(settlement, out disableOption, out disabledText);
		if (result && !disableOption && ShouldDisableCastleRequestMeeting(settlement))
		{
			disableOption = true;
			disabledText = CastleRequestMeetingDisabledText;
			Logger.Log("SettlementAccess", "Disabled castle request meeting option. Settlement=" + (settlement?.StringId ?? "null"));
		}
		return result;
	}

	private static bool ShouldDisableCastleRequestMeeting(Settlement settlement)
	{
		try
		{
			return settlement?.IsCastle == true;
		}
		catch (Exception ex)
		{
			Logger.Log("SettlementAccess", "Failed to evaluate castle request meeting disable guard: " + ex.Message);
			return false;
		}
	}
}
