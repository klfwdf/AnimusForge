using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Restricts ordinary town-memory injection to eligible speakers in a live town mission.
/// </summary>
internal static class GcczTownRuleMemorySpeakerResolver
{
	internal static Settlement ResolveCurrentTownScene()
	{
		if (Mission.Current == null)
		{
			return null;
		}
		Settlement settlement = Settlement.CurrentSettlement
			?? Hero.MainHero?.CurrentSettlement
			?? MobileParty.MainParty?.CurrentSettlement;
		return settlement?.IsTown == true ? settlement : null;
	}

	internal static CharacterObject ResolveTargetCharacter(CharacterObject targetCharacter, int targetAgentIndex)
	{
		if (targetCharacter != null || targetAgentIndex < 0)
		{
			return targetCharacter;
		}
		try
		{
			return Mission.Current?.Agents?
				.FirstOrDefault(agent => agent != null && agent.Index == targetAgentIndex)?
				.Character as CharacterObject;
		}
		catch
		{
			return targetCharacter;
		}
	}

	internal static bool IsEligible(Settlement settlement, Hero hero, CharacterObject character)
	{
		if (settlement?.IsTown != true)
		{
			return false;
		}
		if (hero != null)
		{
			if (GcczTownRuleMemoryRulerAdapter.IsSameHero(hero, settlement.OwnerClan?.Leader))
			{
				return true;
			}
			return hero.IsNotable
				&& (ReferenceEquals(hero.CurrentSettlement, settlement)
					|| settlement.Notables?.Contains(hero) == true);
		}
		return IsOrdinaryCivilian(character);
	}

	private static bool IsOrdinaryCivilian(CharacterObject character)
	{
		if (character == null || character == CharacterObject.PlayerCharacter || character.HeroObject != null)
		{
			return false;
		}
		switch (character.Occupation)
		{
		case Occupation.Townsfolk:
		case Occupation.Villager:
		case Occupation.GoodsTrader:
		case Occupation.Artisan:
		case Occupation.Merchant:
		case Occupation.Weaponsmith:
		case Occupation.Armorer:
		case Occupation.HorseTrader:
		case Occupation.ShopWorker:
		case Occupation.Blacksmith:
		case Occupation.Tavernkeeper:
		case Occupation.TavernWench:
		case Occupation.TavernGameHost:
		case Occupation.Musician:
		case Occupation.Preacher:
		case Occupation.RansomBroker:
		case Occupation.NotAssigned:
			return true;
		default:
			return false;
		}
	}
}
