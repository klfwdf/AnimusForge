using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AnimusForge;

public sealed class CourierFoodConsumptionModel : MobilePartyFoodConsumptionModel
{
	private const string LogSource = "CourierFood";
	private const int MaxLoggedFallbackParties = 128;

	private readonly MobilePartyFoodConsumptionModel _inner;
	private static readonly HashSet<string> LoggedFallbackParties = new HashSet<string>(StringComparer.Ordinal);
	private static readonly object FallbackLogLock = new object();

	public CourierFoodConsumptionModel(MobilePartyFoodConsumptionModel inner)
	{
		_inner = inner ?? new DefaultMobilePartyFoodConsumptionModel();
	}

	public override int NumberOfMenOnMapToEatOneFood => _inner.NumberOfMenOnMapToEatOneFood;

	public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
	{
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return new ExplainedNumber(0f, includeDescription, null);
		}
		return _inner.CalculateDailyBaseFoodConsumptionf(party, includeDescription);
	}

	public override ExplainedNumber CalculateDailyFoodConsumptionf(MobileParty party, ExplainedNumber baseConsumption)
	{
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return new ExplainedNumber(0f, false, null);
		}
		try
		{
			return _inner.CalculateDailyFoodConsumptionf(party, baseConsumption);
		}
		catch (NullReferenceException exception)
		{
			LogNativeFoodFallback(party, exception);
			return baseConsumption;
		}
	}

	public override bool DoesPartyConsumeFood(MobileParty mobileParty)
	{
		if (CourierDeliveryBehavior.IsCourierParty(mobileParty))
		{
			return false;
		}
		return _inner.DoesPartyConsumeFood(mobileParty);
	}

	private void LogNativeFoodFallback(MobileParty party, NullReferenceException exception)
	{
		try
		{
			string partyId = party?.StringId ?? "<null>";
			bool shouldLog;
			lock (FallbackLogLock)
			{
				shouldLog = false;
				if (!LoggedFallbackParties.Contains(partyId) && LoggedFallbackParties.Count < MaxLoggedFallbackParties)
				{
					LoggedFallbackParties.Add(partyId);
					shouldLog = true;
				}
			}
			if (!shouldLog)
			{
				return;
			}
			Logger.Log(LogSource, "native_food_nre_fallback party=" + partyId + " inner=" + (_inner?.GetType().FullName ?? "<null>") + " detail=" + DescribeFoodStateFault(party) + " exception=" + exception.GetType().Name);
		}
		catch
		{
		}
	}

	private static string DescribeFoodStateFault(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return "party_null";
			}
			string memberFault = FindRosterCultureFault(party.MemberRoster, "member");
			if (!string.IsNullOrEmpty(memberFault))
			{
				return memberFault;
			}
			string prisonerFault = FindRosterCultureFault(party.PrisonRoster, "prisoner");
			if (!string.IsNullOrEmpty(prisonerFault))
			{
				return prisonerFault;
			}
			return "no_roster_culture_fault_found";
		}
		catch (Exception exception)
		{
			return "inspection_exception=" + exception.GetType().Name;
		}
	}

	private static string FindRosterCultureFault(TroopRoster roster, string label)
	{
		if (roster == null)
		{
			return label + "_roster_null";
		}
		for (int index = 0; index < roster.Count; index++)
		{
			CharacterObject character;
			try
			{
				character = roster.GetCharacterAtIndex(index);
			}
			catch (Exception exception)
			{
				return label + "[" + index + "]_read_exception=" + exception.GetType().Name;
			}
			if (character == null)
			{
				return label + "[" + index + "]=character_null";
			}
			try
			{
				if (character.Culture == null)
				{
					return label + "[" + index + "]=culture_null,id=" + (character.StringId ?? "<null>");
				}
			}
			catch (Exception exception)
			{
				return label + "[" + index + "]=culture_read_exception,id=" + (character.StringId ?? "<null>") + ",exception=" + exception.GetType().Name;
			}
		}
		return "";
	}
}
