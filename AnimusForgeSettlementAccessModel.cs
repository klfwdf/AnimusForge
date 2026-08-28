using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class AnimusForgeSettlementAccessModel : SettlementAccessModel
{
	private static readonly bool HostileCastleNativeMeetingEnabled = true;

	private static readonly TextObject CastleRequestMeetingDisabledText = new TextObject("AnimusForge 已禁用城堡中的“请求与某人会面”。");

	private static string _lastHostileCastleGuardFailureSettlementId;

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
			if (!CanSafelyEnableHostileCastleRequestMeeting(settlement))
			{
				disableOption = true;
				disabledText = CastleRequestMeetingDisabledText;
				Logger.Log("SettlementAccess", "Disabled castle request meeting option. Settlement=" + (settlement?.StringId ?? "null"));
			}
		}
		return result;
	}

	private static bool CanSafelyEnableHostileCastleRequestMeeting(Settlement settlement)
	{
		if (!HostileCastleNativeMeetingEnabled || !IsHostileCastleForMainHero(settlement))
		{
			return false;
		}
		try
		{
			bool guardArmed = LordEncounterBehavior.IsNativeSettlementRequestMeetingContext();
			if (!guardArmed)
			{
				string settlementId = settlement?.StringId ?? "null";
				if (!string.Equals(_lastHostileCastleGuardFailureSettlementId, settlementId, StringComparison.Ordinal))
				{
					_lastHostileCastleGuardFailureSettlementId = settlementId;
					Logger.Log("SettlementAccess", "Hostile castle request meeting failed closed because the native meeting guard could not be armed. Settlement=" + settlementId);
				}
			}
			else
			{
				_lastHostileCastleGuardFailureSettlementId = null;
			}
			return guardArmed;
		}
		catch (Exception ex)
		{
			Logger.Log("SettlementAccess", "Hostile castle request meeting failed closed: " + ex.Message);
			return false;
		}
	}

	private static bool IsHostileCastleForMainHero(Settlement settlement)
	{
		try
		{
			if (settlement?.IsCastle != true)
			{
				return false;
			}
			IFaction playerFaction = Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.MapFaction;
			IFaction settlementFaction = settlement.MapFaction ?? settlement.OwnerClan?.MapFaction;
			return playerFaction != null
				&& settlementFaction != null
				&& FactionManager.IsAtWarAgainstFaction(settlementFaction, playerFaction);
		}
		catch (Exception ex)
		{
			Logger.Log("SettlementAccess", "Failed to evaluate hostile castle request meeting guard: " + ex.Message);
			return false;
		}
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
