using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.CampaignBehaviors;

public class HeirSelectionCampaignBehavior : CampaignBehaviorBase
{
	private readonly ItemRoster _itemsThatWillBeInherited = new ItemRoster();

	private readonly ItemRoster _equipmentsThatWillBeInherited = new ItemRoster();

	public override void RegisterEvents()
	{
		CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener(this, OnBeforeMainCharacterDied);
		CampaignEvents.OnBeforePlayerCharacterChangedEvent.AddNonSerializedListener(this, OnBeforePlayerCharacterChanged);
		CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
		CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, OnHeirSelectionOver);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnBeforePlayerCharacterChanged(Hero oldPlayer, Hero newPlayer)
	{
		foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
		{
			_itemsThatWillBeInherited.Add(item);
		}
		for (int i = 0; i < 12; i++)
		{
			if (!oldPlayer.BattleEquipment[i].IsEmpty)
			{
				_equipmentsThatWillBeInherited.AddToCounts(oldPlayer.BattleEquipment[i], 1);
			}
			if (!oldPlayer.CivilianEquipment[i].IsEmpty)
			{
				_equipmentsThatWillBeInherited.AddToCounts(oldPlayer.CivilianEquipment[i], 1);
			}
		}
	}

	private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		foreach (Alley item in oldPlayer.OwnedAlleys.ToList())
		{
			item.SetOwner(newPlayer);
		}
		if (isMainPartyChanged)
		{
			newMainParty.ItemRoster.Add(_itemsThatWillBeInherited);
		}
		newMainParty.ItemRoster.Add(_equipmentsThatWillBeInherited);
		_itemsThatWillBeInherited.Clear();
		_equipmentsThatWillBeInherited.Clear();
	}

	private void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		Dictionary<Hero, int> heirApparents = Hero.MainHero.Clan.GetHeirApparents();
		Hero.MainHero.AddDeathMark(killer, detail);
		if (heirApparents.Count == 0)
		{
			if (PlayerEncounter.Current != null && (PlayerEncounter.Battle == null || !PlayerEncounter.Battle.IsFinalized))
			{
				PlayerEncounter.Finish();
			}
			Dictionary<TroopRosterElement, int> dictionary = new Dictionary<TroopRosterElement, int>();
			foreach (TroopRosterElement item in MobileParty.MainParty.Party.MemberRoster.GetTroopRoster())
			{
				if (item.Character != CharacterObject.PlayerCharacter)
				{
					dictionary.Add(item, item.Number);
				}
			}
			foreach (KeyValuePair<TroopRosterElement, int> item2 in dictionary)
			{
				MobileParty.MainParty.Party.MemberRoster.RemoveTroop(item2.Key.Character, item2.Value);
			}
			CampaignEventDispatcher.Instance.OnGameOver();
			GameOverCleanup();
			ShowGameStatistics();
			Campaign.Current.OnGameOver();
		}
		else
		{
			if (Hero.MainHero.IsPrisoner)
			{
				EndCaptivityAction.ApplyByDeath(Hero.MainHero);
			}
			if (PlayerEncounter.Current != null && (PlayerEncounter.Battle == null || !PlayerEncounter.Battle.IsFinalized))
			{
				PlayerEncounter.Finish();
			}
			CampaignEventDispatcher.Instance.OnHeirSelectionRequested(heirApparents);
		}
		if (Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
	}

	private void OnHeirSelectionOver(Hero selectedHeir)
	{
		ApplyHeirSelectionAction.ApplyByDeath(selectedHeir);
	}

	private void ShowGameStatistics()
	{
		TextObject textObject = new TextObject("{=oxb2FVz5}Clan Destroyed");
		TextObject textObject2 = new TextObject("{=T2GbF6lK}With no suitable heirs, the {CLAN_NAME} clan is no more. Your journey ends here.");
		textObject2.SetTextVariable("CLAN_NAME", Clan.PlayerClan.Name);
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: "", affirmativeAction: delegate
		{
			GameOverState gameState = Game.Current.GameStateManager.CreateState<GameOverState>(new object[1] { GameOverState.GameOverReason.ClanDestroyed });
			Game.Current.GameStateManager.CleanAndPushState(gameState);
		}, negativeAction: null), pauseGameActiveState: true);
	}

	private void GameOverCleanup()
	{
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, Hero.MainHero.Gold, disableNotification: true);
		Campaign.Current.MainParty.Party.ItemRoster.Clear();
		Campaign.Current.MainParty.Party.MemberRoster.Clear();
		Campaign.Current.MainParty.Party.PrisonRoster.Clear();
		Campaign.Current.MainParty.IsVisible = false;
		Campaign.Current.CameraFollowParty = null;
		Campaign.Current.MainParty.IsActive = false;
		PartyBase.MainParty.SetVisualAsDirty();
		if (Hero.MainHero.MapFaction.IsKingdomFaction && Clan.PlayerClan.Kingdom.Leader == Hero.MainHero)
		{
			DestroyKingdomAction.ApplyByKingdomLeaderDeath(Clan.PlayerClan.Kingdom);
		}
	}
}
