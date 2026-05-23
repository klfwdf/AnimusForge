using System;
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

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D6 RID: 214
	public class HeirSelectionCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000A0B RID: 2571 RVA: 0x0004CA04 File Offset: 0x0004AC04
		public override void RegisterEvents()
		{
			CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnBeforeMainCharacterDied));
			CampaignEvents.OnBeforePlayerCharacterChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnBeforePlayerCharacterChanged));
			CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, MobileParty, bool>(this.OnPlayerCharacterChanged));
			CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeirSelectionOver));
		}

		// Token: 0x06000A0C RID: 2572 RVA: 0x0004CA6D File Offset: 0x0004AC6D
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000A0D RID: 2573 RVA: 0x0004CA70 File Offset: 0x0004AC70
		private void OnBeforePlayerCharacterChanged(Hero oldPlayer, Hero newPlayer)
		{
			foreach (ItemRosterElement itemRosterElement in MobileParty.MainParty.ItemRoster)
			{
				this._itemsThatWillBeInherited.Add(itemRosterElement);
			}
			for (int i = 0; i < 12; i++)
			{
				if (!oldPlayer.BattleEquipment[i].IsEmpty)
				{
					this._equipmentsThatWillBeInherited.AddToCounts(oldPlayer.BattleEquipment[i], 1);
				}
				if (!oldPlayer.CivilianEquipment[i].IsEmpty)
				{
					this._equipmentsThatWillBeInherited.AddToCounts(oldPlayer.CivilianEquipment[i], 1);
				}
			}
		}

		// Token: 0x06000A0E RID: 2574 RVA: 0x0004CB34 File Offset: 0x0004AD34
		private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
		{
			foreach (Alley alley in oldPlayer.OwnedAlleys.ToList<Alley>())
			{
				alley.SetOwner(newPlayer);
			}
			if (isMainPartyChanged)
			{
				newMainParty.ItemRoster.Add(this._itemsThatWillBeInherited);
			}
			newMainParty.ItemRoster.Add(this._equipmentsThatWillBeInherited);
			this._itemsThatWillBeInherited.Clear();
			this._equipmentsThatWillBeInherited.Clear();
		}

		// Token: 0x06000A0F RID: 2575 RVA: 0x0004CBC8 File Offset: 0x0004ADC8
		private void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			Dictionary<Hero, int> heirApparents = Hero.MainHero.Clan.GetHeirApparents();
			Hero.MainHero.AddDeathMark(killer, detail);
			if (heirApparents.Count == 0)
			{
				if (PlayerEncounter.Current != null && (PlayerEncounter.Battle == null || !PlayerEncounter.Battle.IsFinalized))
				{
					PlayerEncounter.Finish(true);
				}
				Dictionary<TroopRosterElement, int> dictionary = new Dictionary<TroopRosterElement, int>();
				foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.Party.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character != CharacterObject.PlayerCharacter)
					{
						dictionary.Add(troopRosterElement, troopRosterElement.Number);
					}
				}
				foreach (KeyValuePair<TroopRosterElement, int> keyValuePair in dictionary)
				{
					MobileParty.MainParty.Party.MemberRoster.RemoveTroop(keyValuePair.Key.Character, keyValuePair.Value, default(UniqueTroopDescriptor), 0);
				}
				CampaignEventDispatcher.Instance.OnGameOver();
				this.GameOverCleanup();
				this.ShowGameStatistics();
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
					PlayerEncounter.Finish(true);
				}
				CampaignEventDispatcher.Instance.OnHeirSelectionRequested(heirApparents);
			}
			if (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.ExitToLast();
			}
		}

		// Token: 0x06000A10 RID: 2576 RVA: 0x0004CD68 File Offset: 0x0004AF68
		private void OnHeirSelectionOver(Hero selectedHeir)
		{
			ApplyHeirSelectionAction.ApplyByDeath(selectedHeir);
		}

		// Token: 0x06000A11 RID: 2577 RVA: 0x0004CD70 File Offset: 0x0004AF70
		private void ShowGameStatistics()
		{
			object obj = new TextObject("{=oxb2FVz5}Clan Destroyed", null);
			TextObject textObject = new TextObject("{=T2GbF6lK}With no suitable heirs, the {CLAN_NAME} clan is no more. Your journey ends here.", null);
			textObject.SetTextVariable("CLAN_NAME", Clan.PlayerClan.Name);
			TextObject textObject2 = new TextObject("{=DM6luo3c}Continue", null);
			InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, false, textObject2.ToString(), "", delegate()
			{
				GameOverState gameState = Game.Current.GameStateManager.CreateState<GameOverState>(new object[] { GameOverState.GameOverReason.ClanDestroyed });
				Game.Current.GameStateManager.CleanAndPushState(gameState, 0);
			}, null, "", 0f, null, null, null), true, false);
		}

		// Token: 0x06000A12 RID: 2578 RVA: 0x0004CE08 File Offset: 0x0004B008
		private void GameOverCleanup()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, Hero.MainHero.Gold, true);
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

		// Token: 0x04000484 RID: 1156
		private readonly ItemRoster _itemsThatWillBeInherited = new ItemRoster();

		// Token: 0x04000485 RID: 1157
		private readonly ItemRoster _equipmentsThatWillBeInherited = new ItemRoster();
	}
}
