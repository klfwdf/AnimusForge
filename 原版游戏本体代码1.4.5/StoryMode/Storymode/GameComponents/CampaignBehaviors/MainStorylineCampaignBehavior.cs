using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StoryMode.Quests.PlayerClanQuests;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class MainStorylineCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan && newKingdom != null && (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom))
		{
			Clan.PlayerClan.IsNoble = true;
		}
	}

	private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		if ((hero == StoryModeHeroes.Radagos && StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest)) && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RebuildPlayerClanQuest)) && causeOfDeath == KillCharacterAction.KillCharacterActionDetail.Executed) || causeOfDeath == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
		{
			result = true;
		}
		else if (hero.IsSpecial && hero != StoryModeHeroes.RadagosHenchman && !StoryModeManager.Current.MainStoryLine.IsCompleted)
		{
			result = false;
		}
	}

	private void OnHeroComesOfAge(Hero hero)
	{
		if (hero == StoryModeHeroes.LittleBrother || (hero == StoryModeHeroes.LittleSister && !ModuleHelper.IsModuleActive("NavalDLC")))
		{
			StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(hero);
		}
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion)
		{
			return;
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.13.105456"))
		{
			if (Clan.PlayerClan.Kingdom != null && !Clan.PlayerClan.IsUnderMercenaryService && !Clan.PlayerClan.IsNoble)
			{
				Clan.PlayerClan.IsNoble = true;
			}
			bool flag = StoryModeManager.Current.MainStoryLine.FamilyRescued && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest));
			HandlePlayerSiblingsStatesOnLoad(StoryModeHeroes.LittleSister, flag);
			HandlePlayerSiblingsStatesOnLoad(StoryModeHeroes.LittleBrother, flag);
			if (flag)
			{
				CheckStoryModeHeroStateAndUpdateIfNeeded(StoryModeHeroes.ElderBrother);
				CheckAndUpdateGovernorStatusOfStoryModeHero(StoryModeHeroes.ElderBrother);
			}
		}
		if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0"))
		{
			FirstPhase instance = FirstPhase.Instance;
			if (instance != null && instance.AllPiecesCollected)
			{
				ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>("dragon_banner");
				bool flag2 = false;
				foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
				{
					if (item.EquipmentElement.Item == itemObject)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					StoryModeManager.Current.MainStoryLine.FirstPhase?.MergeDragonBanner();
				}
			}
		}
		if (!MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.9.35367")))
		{
			return;
		}
		List<EquipmentElement> list = new List<EquipmentElement>();
		foreach (ItemRosterElement item2 in MobileParty.MainParty.ItemRoster)
		{
			string text = item2.EquipmentElement.Item?.StringId;
			if (!item2.EquipmentElement.IsQuestItem)
			{
				switch (text)
				{
				case "dragon_banner_center":
				case "dragon_banner_dragonhead":
				case "dragon_banner_handle":
				case "dragon_banner":
					list.Add(item2.EquipmentElement);
					break;
				}
			}
		}
		if (!list.Any())
		{
			return;
		}
		foreach (EquipmentElement item3 in list)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(item3, -1);
			MobileParty.MainParty.ItemRoster.AddToCounts(new EquipmentElement(item3.Item, null, null, isQuestItem: true), 1);
		}
	}

	private void HandlePlayerSiblingsStatesOnLoad(Hero hero, bool isPlayerFamilyRescued)
	{
		if (!hero.IsAlive || (hero != StoryModeHeroes.LittleBrother && (hero != StoryModeHeroes.LittleSister || ModuleHelper.IsModuleActive("NavalDLC"))))
		{
			return;
		}
		AgingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AgingCampaignBehavior>();
		FieldInfo field = typeof(AgingCampaignBehavior).GetField("_heroesYoungerThanHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic);
		Dictionary<Hero, int> dictionary = ((campaignBehavior != null) ? ((Dictionary<Hero, int>)field.GetValue(campaignBehavior)) : null);
		if (hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			if (!hero.IsDisabled && !hero.IsNotSpawned)
			{
				if (isPlayerFamilyRescued)
				{
					hero.ChangeState(Hero.CharacterStates.NotSpawned);
				}
				else
				{
					DisableHeroAction.Apply(hero);
				}
			}
			if (!hero.IsDisabled && dictionary != null && !dictionary.ContainsKey(hero))
			{
				dictionary.Add(hero, (int)hero.Age);
				field.SetValue(campaignBehavior, dictionary);
			}
		}
		else if (isPlayerFamilyRescued)
		{
			if (dictionary != null && dictionary.ContainsKey(hero))
			{
				dictionary.Remove(hero);
			}
			CheckPlayerSiblingsEducationStages(hero);
			CheckStoryModeHeroStateAndUpdateIfNeeded(hero);
			StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(hero);
		}
		else if (!hero.IsDisabled)
		{
			DisableHeroAction.Apply(hero);
			if (hero.GovernorOf != null)
			{
				ChangeGovernorAction.RemoveGovernorOf(hero);
			}
		}
		CheckAndUpdateGovernorStatusOfStoryModeHero(hero);
	}

	private void CheckPlayerSiblingsEducationStages(Hero hero)
	{
		EducationCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<EducationCampaignBehavior>();
		if (campaignBehavior != null)
		{
			Type typeFromHandle = typeof(EducationCampaignBehavior);
			if (((Dictionary<Hero, short>)typeFromHandle.GetField("_previousEducations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(campaignBehavior)).ContainsKey(hero) || !IsHeroAttributesInitialized(hero))
			{
				typeFromHandle.GetMethod("OnHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(campaignBehavior, new object[1] { hero });
			}
		}
	}

	private void CheckStoryModeHeroStateAndUpdateIfNeeded(Hero hero)
	{
		if (hero.IsNotSpawned || hero.IsDisabled)
		{
			Settlement settlementToSpawnForPlayerRelative = GetSettlementToSpawnForPlayerRelative(hero);
			if (hero.BornSettlement == null)
			{
				hero.BornSettlement = settlementToSpawnForPlayerRelative;
			}
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, settlementToSpawnForPlayerRelative);
			if (!hero.IsActive)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
			}
		}
		if (hero.Clan == null)
		{
			hero.Clan = Clan.PlayerClan;
			if (!hero.IsFugitive)
			{
				MakeHeroFugitiveAction.Apply(hero);
			}
		}
	}

	private static void CheckAndUpdateGovernorStatusOfStoryModeHero(Hero hero)
	{
		if (hero.GovernorOf != null && hero.CurrentSettlement != hero.GovernorOf.Settlement)
		{
			ChangeGovernorAction.RemoveGovernorOf(hero);
		}
	}

	private bool IsHeroAttributesInitialized(Hero hero)
	{
		foreach (CharacterAttribute item in Attributes.All)
		{
			if (hero.GetAttributeValue(item) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private Settlement GetSettlementToSpawnForPlayerRelative(Hero hero)
	{
		if (hero.GovernorOf != null)
		{
			return hero.GovernorOf.Settlement;
		}
		if (!hero.HomeSettlement.OwnerClan.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			return hero.HomeSettlement;
		}
		if (!Clan.PlayerClan.MapFaction.Settlements.IsEmpty())
		{
			return Clan.PlayerClan.MapFaction.Settlements.GetRandomElement();
		}
		foreach (Settlement item in Settlement.All)
		{
			if (!item.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				return item;
			}
		}
		return Village.All.GetRandomElement().Settlement;
	}
}
