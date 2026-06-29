using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

internal static class EncounterConversationTargetResolver
{
	internal static Hero TryResolveLordFromArgumentsThenEncounterLeader(object instance, object[] args)
	{
		Hero hero = TryResolveLordFromArguments(args);
		if (IsUsableLordTarget(hero))
		{
			return hero;
		}
		hero = TryResolveHeroFromObject(instance);
		if (IsUsableLordTarget(hero))
		{
			return hero;
		}
		hero = TryResolveEncounterLeader();
		return IsUsableLordTarget(hero) ? hero : null;
	}

	private static Hero TryResolveLordFromArguments(object[] args)
	{
		if (args == null)
		{
			return null;
		}
		foreach (object arg in args)
		{
			Hero hero = TryResolveHeroFromObject(arg);
			if (IsUsableLordTarget(hero))
			{
				return hero;
			}
		}
		return null;
	}

	internal static Hero TryResolveHeroFromObject(object value)
	{
		return TryResolveHeroFromObject(value, 0);
	}

	private static Hero TryResolveHeroFromObject(object value, int depth)
	{
		if (value == null || depth > 4)
		{
			return null;
		}
		if (value is Hero hero)
		{
			return hero;
		}
		if (value is CharacterObject characterObject)
		{
			return characterObject.HeroObject;
		}
		if (value is ConversationCharacterData conversationCharacterData)
		{
			return conversationCharacterData.Character?.HeroObject;
		}
		Type type = value.GetType();
		object resolved = TryGetPropertyValue(value, type, "LeaderHero");
		if (resolved is Hero leaderHero)
		{
			return leaderHero;
		}
		resolved = TryGetPropertyValue(value, type, "HeroObject");
		if (resolved is Hero heroObject)
		{
			return heroObject;
		}
		resolved = TryGetPropertyValue(value, type, "Character");
		if (resolved is CharacterObject reflectedCharacter)
		{
			return reflectedCharacter.HeroObject;
		}
		Hero nestedHero = TryResolveHeroFromObject(TryGetPropertyValue(value, type, "Party"), depth + 1);
		if (nestedHero != null)
		{
			return nestedHero;
		}
		nestedHero = TryResolveHeroFromObject(TryGetPropertyValue(value, type, "MobileParty"), depth + 1);
		if (nestedHero != null)
		{
			return nestedHero;
		}
		return null;
	}

	private static object TryGetPropertyValue(object value, Type type, string propertyName)
	{
		try
		{
			return AccessTools.Property(type, propertyName)?.GetValue(value);
		}
		catch
		{
			return null;
		}
	}

	private static Hero TryResolveEncounterLeader()
	{
		try
		{
			PartyBase encounteredParty = PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
			return encounteredParty?.LeaderHero;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsUsableLordTarget(Hero hero)
	{
		return hero != null && hero != Hero.MainHero && hero.IsLord;
	}
}
