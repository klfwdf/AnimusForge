using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F4 RID: 1012
	public class HeroKnownInformationCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003F1B RID: 16155 RVA: 0x0011CAE0 File Offset: 0x0011ACE0
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnDailyTickHero));
			CampaignEvents.ConversationEnded.AddNonSerializedListener(this, new Action<IEnumerable<CharacterObject>>(this.ConversationEnded));
			CampaignEvents.OnAgentJoinedConversationEvent.AddNonSerializedListener(this, new Action<IAgent>(this.OnAgentJoinedConversation));
			CampaignEvents.OnPlayerMetHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnPlayerMetHero));
			CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, new Action<Hero, Hero, bool>(this.OnHeroesMarried));
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinishedEvent));
			CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, new Action(this.OnCharacterCreationIsOver));
			CampaignEvents.OnPlayerLearnsAboutHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnPlayerLearnsAboutHero));
			CampaignEvents.NearbyPartyAddedToPlayerMapEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnNearbyPartyAddedToPlayerMapEvent));
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuChanged));
			CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, new Action<IMission>(this.OnAfterMissionStarted));
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyAttachedAnotherParty));
			CampaignEvents.OnPlayerJoinedTournamentEvent.AddNonSerializedListener(this, new Action<Town, bool>(this.OnPlayerJoinedTournament));
			CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnMarriageOfferedToPlayer));
		}

		// Token: 0x06003F1C RID: 16156 RVA: 0x0011CC5D File Offset: 0x0011AE5D
		private void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
		{
			if (suitor.Clan == Clan.PlayerClan)
			{
				maiden.IsKnownToPlayer = true;
				return;
			}
			suitor.IsKnownToPlayer = true;
		}

		// Token: 0x06003F1D RID: 16157 RVA: 0x0011CC7C File Offset: 0x0011AE7C
		private void OnPlayerJoinedTournament(Town town, bool isParticipant)
		{
			foreach (CharacterObject characterObject in Campaign.Current.TournamentManager.GetTournamentGame(town).GetParticipantCharacters(town.Settlement, false))
			{
				if (characterObject.IsHero && !characterObject.HeroObject.IsKnownToPlayer)
				{
					characterObject.HeroObject.IsKnownToPlayer = true;
				}
			}
		}

		// Token: 0x06003F1E RID: 16158 RVA: 0x0011CD00 File Offset: 0x0011AF00
		private void OnNearbyPartyAddedToPlayerMapEvent(MobileParty mobileParty)
		{
			if (mobileParty.LeaderHero != null)
			{
				mobileParty.LeaderHero.IsKnownToPlayer = true;
			}
		}

		// Token: 0x06003F1F RID: 16159 RVA: 0x0011CD18 File Offset: 0x0011AF18
		private void OnPartyAttachedAnotherParty(MobileParty party)
		{
			if (party == MobileParty.MainParty)
			{
				if (party.AttachedTo.LeaderHero != null)
				{
					party.AttachedTo.LeaderHero.IsKnownToPlayer = true;
				}
				using (List<MobileParty>.Enumerator enumerator = party.AttachedTo.AttachedParties.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						MobileParty mobileParty = enumerator.Current;
						if (mobileParty.LeaderHero != null)
						{
							mobileParty.LeaderHero.IsKnownToPlayer = true;
						}
					}
					return;
				}
			}
			if ((party.AttachedTo == MobileParty.MainParty || party.AttachedTo == MobileParty.MainParty.AttachedTo) && party.LeaderHero != null)
			{
				party.LeaderHero.IsKnownToPlayer = true;
			}
		}

		// Token: 0x06003F20 RID: 16160 RVA: 0x0011CDD8 File Offset: 0x0011AFD8
		private void OnPartyAttachedToAnotherParty(MobileParty mobileParty)
		{
			if (mobileParty == MobileParty.MainParty)
			{
				if (mobileParty.AttachedTo.LeaderHero != null)
				{
					mobileParty.AttachedTo.LeaderHero.IsKnownToPlayer = true;
				}
				using (List<MobileParty>.Enumerator enumerator = mobileParty.AttachedTo.AttachedParties.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						MobileParty mobileParty2 = enumerator.Current;
						if (mobileParty2.LeaderHero != null)
						{
							mobileParty2.LeaderHero.IsKnownToPlayer = true;
						}
					}
					return;
				}
			}
			if ((mobileParty.AttachedTo == MobileParty.MainParty || mobileParty.AttachedTo == MobileParty.MainParty.AttachedTo) && mobileParty.LeaderHero != null)
			{
				mobileParty.LeaderHero.IsKnownToPlayer = true;
			}
		}

		// Token: 0x06003F21 RID: 16161 RVA: 0x0011CE98 File Offset: 0x0011B098
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (MapEvent.PlayerMapEvent == mapEvent)
			{
				foreach (PartyBase partyBase in mapEvent.InvolvedParties)
				{
					if (partyBase.LeaderHero != null)
					{
						partyBase.LeaderHero.IsKnownToPlayer = true;
					}
				}
			}
		}

		// Token: 0x06003F22 RID: 16162 RVA: 0x0011CEFC File Offset: 0x0011B0FC
		private void OnPlayerLearnsAboutHero(Hero hero)
		{
			this.UpdateHeroLocation(hero);
			if (hero.Clan != Clan.PlayerClan)
			{
				TextObject textObject = new TextObject("{=oSghSUxp}You've learned about {?IS_RULER}{RULER_NAME_AND_TITLE}{?}{HERO.NAME}{\\?}.", null);
				textObject.SetTextVariable("IS_RULER", hero.IsKingdomLeader ? 1 : 0);
				if (hero.IsKingdomLeader)
				{
					TextObject textObject2 = GameTexts.FindText("str_faction_ruler_name_with_title", hero.MapFaction.Culture.StringId);
					textObject2.SetCharacterProperties("RULER", hero.CharacterObject, false);
					textObject.SetTextVariable("RULER_NAME_AND_TITLE", textObject2);
				}
				else
				{
					textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
				}
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
			}
		}

		// Token: 0x06003F23 RID: 16163 RVA: 0x0011CFAA File Offset: 0x0011B1AA
		private void OnAfterMissionStarted(IMission mission)
		{
			if (CampaignMission.Current.Location != null)
			{
				this.LearnAboutLocationCharacters(CampaignMission.Current.Location);
			}
		}

		// Token: 0x06003F24 RID: 16164 RVA: 0x0011CFC8 File Offset: 0x0011B1C8
		private void OnGameMenuChanged(MenuCallbackArgs args)
		{
			foreach (Location location in Campaign.Current.GameMenuManager.MenuLocations)
			{
				this.LearnAboutLocationCharacters(location);
			}
		}

		// Token: 0x06003F25 RID: 16165 RVA: 0x0011D024 File Offset: 0x0011B224
		private void LearnAboutLocationCharacters(Location location)
		{
			foreach (LocationCharacter locationCharacter in location.GetCharacterList())
			{
				if (locationCharacter.Character.IsHero && locationCharacter.Character.HeroObject.CurrentSettlement == Settlement.CurrentSettlement)
				{
					locationCharacter.Character.HeroObject.IsKnownToPlayer = true;
				}
			}
		}

		// Token: 0x06003F26 RID: 16166 RVA: 0x0011D0A0 File Offset: 0x0011B2A0
		private void OnPlayerMetHero(Hero hero)
		{
			hero.IsKnownToPlayer = true;
		}

		// Token: 0x06003F27 RID: 16167 RVA: 0x0011D0A9 File Offset: 0x0011B2A9
		private void OnDailyTickHero(Hero hero)
		{
			this.UpdateHeroLocation(hero);
		}

		// Token: 0x06003F28 RID: 16168 RVA: 0x0011D0B4 File Offset: 0x0011B2B4
		private void OnAgentJoinedConversation(IAgent agent)
		{
			CharacterObject characterObject = (CharacterObject)agent.Character;
			if (characterObject.IsHero)
			{
				this.UpdateHeroLocation(characterObject.HeroObject);
				characterObject.HeroObject.IsKnownToPlayer = true;
			}
			MobileParty conversationParty = MobileParty.ConversationParty;
			Hero hero;
			if (conversationParty == null)
			{
				hero = null;
			}
			else
			{
				CaravanPartyComponent caravanPartyComponent = conversationParty.CaravanPartyComponent;
				hero = ((caravanPartyComponent != null) ? caravanPartyComponent.Owner : null);
			}
			Hero hero2 = hero;
			if (hero2 != null)
			{
				hero2.IsKnownToPlayer = true;
			}
		}

		// Token: 0x06003F29 RID: 16169 RVA: 0x0011D118 File Offset: 0x0011B318
		private void UpdateHeroLocation(Hero hero)
		{
			if (hero.IsKnownToPlayer)
			{
				if (hero.IsActive || hero.IsPrisoner)
				{
					Settlement closestSettlement = HeroHelper.GetClosestSettlement(hero);
					if (closestSettlement != null)
					{
						hero.UpdateLastKnownClosestSettlement(closestSettlement);
						return;
					}
				}
			}
			else
			{
				hero.UpdateLastKnownClosestSettlement(null);
			}
		}

		// Token: 0x06003F2A RID: 16170 RVA: 0x0011D158 File Offset: 0x0011B358
		private void OnCharacterCreationIsOver()
		{
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				this.UpdateHeroLocation(hero);
			}
		}

		// Token: 0x06003F2B RID: 16171 RVA: 0x0011D1AC File Offset: 0x0011B3AC
		private void OnGameLoadFinishedEvent()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.1.0", 0))
			{
				foreach (Hero hero in Clan.PlayerClan.Heroes)
				{
					hero.SetHasMet();
				}
				foreach (Hero hero2 in Hero.AllAliveHeroes)
				{
					if (hero2.LastKnownClosestSettlement == null)
					{
						this.UpdateHeroLocation(hero2);
					}
					if (hero2.HasMet)
					{
						hero2.IsKnownToPlayer = true;
					}
				}
			}
		}

		// Token: 0x06003F2C RID: 16172 RVA: 0x0011D27C File Offset: 0x0011B47C
		private void OnHeroesMarried(Hero hero1, Hero hero2, bool showNotification)
		{
			if (hero1 == Hero.MainHero)
			{
				hero2.SetHasMet();
			}
			if (hero2 == Hero.MainHero)
			{
				hero1.SetHasMet();
			}
		}

		// Token: 0x06003F2D RID: 16173 RVA: 0x0011D29A File Offset: 0x0011B49A
		private void OnHeroCreated(Hero hero, bool isBornNaturally)
		{
			if (hero.Clan == Clan.PlayerClan)
			{
				hero.SetHasMet();
			}
		}

		// Token: 0x06003F2E RID: 16174 RVA: 0x0011D2B0 File Offset: 0x0011B4B0
		private void ConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
		{
			foreach (CharacterObject characterObject in conversationCharacters)
			{
				if (characterObject.IsHero)
				{
					bool flag = true;
					CampaignEventDispatcher.Instance.CanPlayerMeetWithHeroAfterConversation(characterObject.HeroObject, ref flag);
					if (flag)
					{
						characterObject.HeroObject.SetHasMet();
					}
				}
			}
		}

		// Token: 0x06003F2F RID: 16175 RVA: 0x0011D31C File Offset: 0x0011B51C
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
