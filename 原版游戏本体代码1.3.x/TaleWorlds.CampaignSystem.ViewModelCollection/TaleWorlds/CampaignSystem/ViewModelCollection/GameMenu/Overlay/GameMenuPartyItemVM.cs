using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000BA RID: 186
	public class GameMenuPartyItemVM : ViewModel
	{
		// Token: 0x0600122B RID: 4651 RVA: 0x0004923C File Offset: 0x0004743C
		public GameMenuPartyItemVM()
		{
			this.Visual = new CharacterImageIdentifierVM(null);
			this.RegisterEvents();
		}

		// Token: 0x0600122C RID: 4652 RVA: 0x00049268 File Offset: 0x00047468
		public GameMenuPartyItemVM(Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem, Settlement settlement)
		{
			this._onSetAsContextMenuActiveItem = onSetAsContextMenuActiveItem;
			this.Settlement = settlement;
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			this.SettlementPath = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
			this.Visual = new CharacterImageIdentifierVM(null);
			this.NameText = settlement.Name.ToString();
			this.PartySize = -1;
			this.PartyWoundedSize = -1;
			this.PartySizeLbl = "";
			this.IsPlayer = false;
			this.IsAlly = false;
			this.IsEnemy = false;
			this.Quests = new MBBindingList<QuestMarkerVM>();
			this.RefreshProperties();
			this.RegisterEvents();
		}

		// Token: 0x0600122D RID: 4653 RVA: 0x00049324 File Offset: 0x00047524
		public GameMenuPartyItemVM(Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem, PartyBase item, bool canShowQuest)
		{
			this._onSetAsContextMenuActiveItem = onSetAsContextMenuActiveItem;
			this.Party = item;
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(this.Party);
			if (visualPartyLeader != null)
			{
				CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(visualPartyLeader, false);
				this.Visual = new CharacterImageIdentifierVM(characterCode);
			}
			else
			{
				this.Visual = new CharacterImageIdentifierVM(null);
			}
			this.Quests = new MBBindingList<QuestMarkerVM>();
			this._canShowQuest = canShowQuest;
			this.RefreshProperties();
			this.RegisterEvents();
		}

		// Token: 0x0600122E RID: 4654 RVA: 0x000493A4 File Offset: 0x000475A4
		public GameMenuPartyItemVM(Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem, CharacterObject character, bool useCivilianEquipment)
		{
			this._onSetAsContextMenuActiveItem = onSetAsContextMenuActiveItem;
			this.Character = character;
			this._useCivilianEquipment = useCivilianEquipment;
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(character, useCivilianEquipment);
			this.Visual = new CharacterImageIdentifierVM(characterCode);
			Hero heroObject = this.Character.HeroObject;
			this.Banner_9 = (((heroObject != null && heroObject.IsLord) || (this.Character.IsHero && this.Character.HeroObject.Clan == Clan.PlayerClan && character.HeroObject.IsLord)) ? new BannerImageIdentifierVM(this.Character.HeroObject.ClanBanner, true) : new BannerImageIdentifierVM(null, false));
			this.NameText = this.Character.Name.ToString();
			this.PartySize = -1;
			this.PartyWoundedSize = -1;
			this.PartySizeLbl = "";
			this.IsPlayer = character.IsPlayerCharacter;
			this.Quests = new MBBindingList<QuestMarkerVM>();
			this.RefreshProperties();
			this.RegisterEvents();
		}

		// Token: 0x0600122F RID: 4655 RVA: 0x000494B8 File Offset: 0x000476B8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.RefreshProperties();
		}

		// Token: 0x06001230 RID: 4656 RVA: 0x000494C6 File Offset: 0x000476C6
		public void ExecuteSetAsContextMenuItem()
		{
			Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem = this._onSetAsContextMenuActiveItem;
			if (onSetAsContextMenuActiveItem == null)
			{
				return;
			}
			onSetAsContextMenuActiveItem(this);
		}

		// Token: 0x06001231 RID: 4657 RVA: 0x000494DC File Offset: 0x000476DC
		public void ExecuteOpenEncyclopedia()
		{
			string encyclopediaPageLink = this.GetEncyclopediaPageLink();
			if (!string.IsNullOrEmpty(encyclopediaPageLink))
			{
				Campaign.Current.EncyclopediaManager.GoToLink(encyclopediaPageLink);
			}
		}

		// Token: 0x06001232 RID: 4658 RVA: 0x00049508 File Offset: 0x00047708
		public void ExecuteCloseTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001233 RID: 4659 RVA: 0x00049510 File Offset: 0x00047710
		public void ExecuteOpenTooltip()
		{
			PartyBase party = this.Party;
			if (((party != null) ? party.MobileParty : null) != null)
			{
				InformationManager.ShowTooltip(typeof(MobileParty), new object[]
				{
					this.Party.MobileParty,
					true,
					false
				});
				return;
			}
			if (this.Settlement != null)
			{
				InformationManager.ShowTooltip(typeof(Settlement), new object[] { this.Settlement });
				return;
			}
			InformationManager.ShowTooltip(typeof(Hero), new object[]
			{
				this.Character.HeroObject,
				true
			});
		}

		// Token: 0x06001234 RID: 4660 RVA: 0x000495BC File Offset: 0x000477BC
		public void RefreshProperties()
		{
			this.EncyclopediaCursorEffect = ((!string.IsNullOrEmpty(this.GetEncyclopediaPageLink())) ? "RightClickLink" : null);
			if (this.Party != null)
			{
				this.RefreshCounts();
				this.Relation = HeroVM.GetRelation(this.Party.LeaderHero);
				this.LocationText = " ";
				TextObject name = this.Party.Name;
				if (this.Party.IsMobile)
				{
					name = this.Party.MobileParty.Name;
					float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
					if (this.Party.MobileParty.Position.DistanceSquared(MobileParty.MainParty.Position) > getEncounterJoiningRadius * getEncounterJoiningRadius)
					{
						if (this.Party.MobileParty.MapEvent == null)
						{
							GameTexts.SetVariable("LEFT", GameTexts.FindText("str_distance_to_army_leader", null));
							float num = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(this.Party.MobileParty, MobileParty.MainParty, this.Party.MobileParty.NavigationCapability);
							GameTexts.SetVariable("RIGHT", CampaignUIHelper.GetPartyDistanceByTimeText((float)((int)num), this.Party.MobileParty.Speed));
							this.LocationText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
						}
						else
						{
							TextObject variable = GameTexts.FindText("str_at_map_event", null);
							TextObject textObject = new TextObject("{=zawBaxl5}Distance : {DISTANCE}", null);
							textObject.SetTextVariable("DISTANCE", variable);
							this.LocationText = textObject.ToString();
						}
					}
					this.DescriptionText = this.GetPartyDescriptionTextFromValues();
					this.IsMergedWithArmy = true;
					if (this.Party.MobileParty.Army != null)
					{
						this.IsMergedWithArmy = this.Party.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(this.Party.MobileParty);
					}
				}
				this.NameText = name.ToString();
				this.ProfessionText = " ";
				this.HasShips = this.Party.Ships.Count > 0;
			}
			else if (this.Character != null)
			{
				this.Relation = HeroVM.GetRelation(this.Character.HeroObject);
				Hero heroObject = this.Character.HeroObject;
				this.IsCharacterInPrison = heroObject != null && heroObject.IsPrisoner;
				GameTexts.SetVariable("PROFESSION", HeroHelper.GetCharacterTypeName(this.Character.HeroObject));
				string variableName = "LOCATION";
				Hero heroObject2 = this.Character.HeroObject;
				GameTexts.SetVariable(variableName, (((heroObject2 != null) ? heroObject2.CurrentSettlement : null) != null) ? this.Character.HeroObject.CurrentSettlement.Name.ToString() : "");
				Hero heroObject3 = this.Character.HeroObject;
				this.DescriptionText = ((heroObject3 != null && !heroObject3.IsSpecial) ? GameTexts.FindText("str_character_in_town", null).ToString() : string.Empty);
				string variableName2 = "LOCATION";
				LocationComplex locationComplex = LocationComplex.Current;
				TextObject textObject2;
				if (locationComplex == null)
				{
					textObject2 = null;
				}
				else
				{
					Location locationOfCharacter = locationComplex.GetLocationOfCharacter(this.Character.HeroObject);
					textObject2 = ((locationOfCharacter != null) ? locationOfCharacter.Name : null);
				}
				GameTexts.SetVariable(variableName2, textObject2 ?? TextObject.GetEmpty());
				this.LocationText = GameTexts.FindText("str_location_colon", null).ToString();
				GameTexts.SetVariable("PROFESSION", HeroHelper.GetCharacterTypeName(this.Character.HeroObject));
				this.ProfessionText = GameTexts.FindText("str_profession_colon", null).ToString();
				if (this.Character.IsHero && this.Character.HeroObject.IsNotable)
				{
					GameTexts.SetVariable("POWER", Campaign.Current.Models.NotablePowerModel.GetPowerRankName(this.Character.HeroObject).ToString());
					this.PowerText = GameTexts.FindText("str_power_colon", null).ToString();
				}
				this.NameText = this.Character.Name.ToString();
				this.HasShips = false;
			}
			this.RefreshQuestStatus();
			this.RefreshRelationStatus();
		}

		// Token: 0x06001235 RID: 4661 RVA: 0x000499A4 File Offset: 0x00047BA4
		public void RefreshQuestStatus()
		{
			this.Quests.Clear();
			PartyBase party = this.Party;
			Hero hero;
			if ((hero = ((party != null) ? party.LeaderHero : null)) == null)
			{
				CharacterObject character = this.Character;
				hero = ((character != null) ? character.HeroObject : null);
			}
			Hero hero2 = hero;
			if (hero2 != null)
			{
				GameMenuPartyItemVM.<>c__DisplayClass16_0 CS$<>8__locals1 = new GameMenuPartyItemVM.<>c__DisplayClass16_0();
				CS$<>8__locals1.questTypes = CampaignUIHelper.GetQuestStateOfHero(hero2);
				int k;
				int i;
				for (i = 0; i < CS$<>8__locals1.questTypes.Count; i = k + 1)
				{
					if (!this.Quests.Any((QuestMarkerVM q) => q.QuestMarkerType == (int)CS$<>8__locals1.questTypes[i].Item1))
					{
						this.Quests.Add(new QuestMarkerVM(CS$<>8__locals1.questTypes[i].Item1, CS$<>8__locals1.questTypes[i].Item2, CS$<>8__locals1.questTypes[i].Item3));
					}
					k = i;
				}
			}
			else
			{
				PartyBase party2 = this.Party;
				if (((party2 != null) ? party2.MobileParty : null) != null)
				{
					List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(this.Party.MobileParty);
					for (int j = 0; j < questsRelatedToParty.Count; j++)
					{
						TextObject questHintText = ((questsRelatedToParty[j].JournalEntries.Count > 0) ? questsRelatedToParty[j].JournalEntries[0].LogText : TextObject.GetEmpty());
						CampaignUIHelper.IssueQuestFlags issueQuestFlag;
						if (hero2 != null && questsRelatedToParty[j].QuestGiver == hero2)
						{
							issueQuestFlag = (questsRelatedToParty[j].IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest : CampaignUIHelper.IssueQuestFlags.ActiveIssue);
						}
						else
						{
							issueQuestFlag = (questsRelatedToParty[j].IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest : CampaignUIHelper.IssueQuestFlags.TrackedIssue);
						}
						this.Quests.Add(new QuestMarkerVM(issueQuestFlag, questsRelatedToParty[j].Title, questHintText));
					}
				}
			}
			this.Quests.Sort(new GameMenuPartyItemVM.QuestMarkerComparer());
		}

		// Token: 0x06001236 RID: 4662 RVA: 0x00049BB4 File Offset: 0x00047DB4
		private void RefreshRelationStatus()
		{
			this.IsEnemy = false;
			this.IsAlly = false;
			this.IsNeutral = false;
			IFaction faction = null;
			bool flag = false;
			if (this.Character != null)
			{
				this.IsPlayer = this.Character.IsPlayerCharacter;
				flag = this.Character.IsHero && this.Character.HeroObject.IsNotable;
				IFaction faction2;
				if (!this.IsPlayer)
				{
					CharacterObject character = this.Character;
					faction2 = ((character != null) ? character.HeroObject.MapFaction : null);
				}
				else
				{
					faction2 = null;
				}
				faction = faction2;
			}
			else if (this.Party != null)
			{
				bool isPlayer;
				if (this.Party.IsMobile)
				{
					MobileParty mobileParty = this.Party.MobileParty;
					isPlayer = mobileParty != null && mobileParty.IsMainParty;
				}
				else
				{
					isPlayer = false;
				}
				this.IsPlayer = isPlayer;
				flag = false;
				IFaction faction3;
				if (!this.IsPlayer)
				{
					PartyBase party = this.Party;
					if (party == null)
					{
						faction3 = null;
					}
					else
					{
						MobileParty mobileParty2 = party.MobileParty;
						faction3 = ((mobileParty2 != null) ? mobileParty2.MapFaction : null);
					}
				}
				else
				{
					faction3 = null;
				}
				faction = faction3;
			}
			if (this.IsPlayer || faction == null || flag)
			{
				if (!this.IsPlayer)
				{
					this.IsNeutral = true;
				}
				return;
			}
			if (FactionManager.IsAtWarAgainstFaction(faction, Hero.MainHero.MapFaction))
			{
				this.IsEnemy = true;
				return;
			}
			if (DiplomacyHelper.IsSameFactionAndNotEliminated(faction, Hero.MainHero.MapFaction))
			{
				this.IsAlly = true;
				return;
			}
			this.IsNeutral = true;
		}

		// Token: 0x06001237 RID: 4663 RVA: 0x00049CF4 File Offset: 0x00047EF4
		public void RefreshVisual()
		{
			if (this.Visual.IsEmpty)
			{
				if (this.Character != null)
				{
					CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(this.Character, this._useCivilianEquipment);
					this.Visual = new CharacterImageIdentifierVM(characterCode);
					return;
				}
				if (this.Party != null)
				{
					CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(this.Party);
					if (visualPartyLeader != null)
					{
						CharacterCode characterCode2 = CampaignUIHelper.GetCharacterCode(visualPartyLeader, false);
						this.Visual = new CharacterImageIdentifierVM(characterCode2);
						return;
					}
					this.Visual = new CharacterImageIdentifierVM(null);
				}
			}
		}

		// Token: 0x06001238 RID: 4664 RVA: 0x00049D70 File Offset: 0x00047F70
		public void RefreshCounts()
		{
			if (this.PartySize != this.Party.NumberOfHealthyMembers || this.PartyWoundedSize != this.Party.NumberOfAllMembers - this.Party.NumberOfHealthyMembers)
			{
				this.PartyWoundedSize = this.Party.NumberOfAllMembers - this.Party.NumberOfHealthyMembers;
				this.PartySize = this.Party.NumberOfHealthyMembers;
				MobileParty mobileParty = this.Party.MobileParty;
				this.PartySizeLbl = ((mobileParty != null && mobileParty.IsInfoHidden) ? "?" : this.Party.NumberOfHealthyMembers.ToString());
			}
			MBReadOnlyList<Ship> ships = this.Party.Ships;
			this.ShipCount = ((ships != null) ? ships.Count : 0);
		}

		// Token: 0x06001239 RID: 4665 RVA: 0x00049E34 File Offset: 0x00048034
		public string GetPartyDescriptionTextFromValues()
		{
			GameTexts.SetVariable("newline", "\n");
			string content = ((this.Party.MobileParty.CurrentSettlement != null && this.Party.MobileParty.MapEvent == null) ? "" : CampaignUIHelper.GetMobilePartyBehaviorText(this.Party.MobileParty));
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_food", null).ToString());
			GameTexts.SetVariable("RIGHT", this.Party.MobileParty.Food);
			string content2 = GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString();
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_map_tooltip_speed", null).ToString());
			GameTexts.SetVariable("RIGHT", this.Party.MobileParty.Speed.ToString("F"));
			string content3 = GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString();
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_view_distance", null).ToString());
			GameTexts.SetVariable("RIGHT", this.Party.MobileParty.SeeingRange);
			string content4 = GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString();
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", content2);
			string content5 = GameTexts.FindText("str_string_newline_string", null).ToString();
			GameTexts.SetVariable("STR1", content5);
			GameTexts.SetVariable("STR2", content3);
			content5 = GameTexts.FindText("str_string_newline_string", null).ToString();
			GameTexts.SetVariable("STR1", content5);
			GameTexts.SetVariable("STR2", content4);
			return GameTexts.FindText("str_string_newline_string", null).ToString();
		}

		// Token: 0x0600123A RID: 4666 RVA: 0x00049FE1 File Offset: 0x000481E1
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.UnregisterEvents();
		}

		// Token: 0x0600123B RID: 4667 RVA: 0x00049FEF File Offset: 0x000481EF
		private void RegisterEvents()
		{
			CampaignEvents.OnPlayerBodyPropertiesChangedEvent.AddNonSerializedListener(this, new Action(this.OnPlayerCharacterChangedEvent));
		}

		// Token: 0x0600123C RID: 4668 RVA: 0x0004A008 File Offset: 0x00048208
		private void OnPlayerCharacterChangedEvent()
		{
			CharacterObject characterObject = ((this.Party != null) ? PartyBaseHelper.GetVisualPartyLeader(this.Party) : this.Character);
			if (characterObject == CharacterObject.PlayerCharacter)
			{
				CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(characterObject, false);
				this.Visual = new CharacterImageIdentifierVM(characterCode);
			}
		}

		// Token: 0x0600123D RID: 4669 RVA: 0x0004A04D File Offset: 0x0004824D
		private void UnregisterEvents()
		{
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x0600123E RID: 4670 RVA: 0x0004A05C File Offset: 0x0004825C
		private string GetEncyclopediaPageLink()
		{
			PartyBase party = this.Party;
			if (party == null || !party.MobileParty.IsCaravan)
			{
				PartyBase party2 = this.Party;
				if (party2 == null || !party2.MobileParty.IsGarrison)
				{
					PartyBase party3 = this.Party;
					if (party3 == null || !party3.MobileParty.IsMilitia)
					{
						PartyBase party4 = this.Party;
						if (party4 == null || !party4.MobileParty.IsVillager)
						{
							if (this.Character != null)
							{
								return this.Character.EncyclopediaLink;
							}
							if (this.Party != null)
							{
								if (this.Party.LeaderHero != null)
								{
									return this.Party.LeaderHero.EncyclopediaLink;
								}
								if (this.Party.Owner != null)
								{
									return this.Party.Owner.EncyclopediaLink;
								}
								CharacterObject visualPartyLeader = CampaignUIHelper.GetVisualPartyLeader(this.Party);
								if (visualPartyLeader != null)
								{
									return visualPartyLeader.EncyclopediaLink;
								}
							}
							return null;
						}
					}
				}
			}
			return null;
		}

		// Token: 0x170005F3 RID: 1523
		// (get) Token: 0x0600123F RID: 4671 RVA: 0x0004A13E File Offset: 0x0004833E
		// (set) Token: 0x06001240 RID: 4672 RVA: 0x0004A146 File Offset: 0x00048346
		[DataSourceProperty]
		public int Relation
		{
			get
			{
				return this._relation;
			}
			set
			{
				if (value != this._relation)
				{
					this._relation = value;
					base.OnPropertyChangedWithValue(value, "Relation");
				}
			}
		}

		// Token: 0x170005F4 RID: 1524
		// (get) Token: 0x06001241 RID: 4673 RVA: 0x0004A164 File Offset: 0x00048364
		// (set) Token: 0x06001242 RID: 4674 RVA: 0x0004A16C File Offset: 0x0004836C
		[DataSourceProperty]
		public MBBindingList<QuestMarkerVM> Quests
		{
			get
			{
				return this._quests;
			}
			set
			{
				if (value != this._quests)
				{
					this._quests = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestMarkerVM>>(value, "Quests");
				}
			}
		}

		// Token: 0x170005F5 RID: 1525
		// (get) Token: 0x06001243 RID: 4675 RVA: 0x0004A18A File Offset: 0x0004838A
		// (set) Token: 0x06001244 RID: 4676 RVA: 0x0004A192 File Offset: 0x00048392
		[DataSourceProperty]
		public bool IsHighlightEnabled
		{
			get
			{
				return this._isHighlightEnabled;
			}
			set
			{
				if (value != this._isHighlightEnabled)
				{
					this._isHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsHighlightEnabled");
				}
			}
		}

		// Token: 0x170005F6 RID: 1526
		// (get) Token: 0x06001245 RID: 4677 RVA: 0x0004A1B0 File Offset: 0x000483B0
		// (set) Token: 0x06001246 RID: 4678 RVA: 0x0004A1B8 File Offset: 0x000483B8
		[DataSourceProperty]
		public bool IsCharacterInPrison
		{
			get
			{
				return this._isCharacterInPrison;
			}
			set
			{
				if (value != this._isCharacterInPrison)
				{
					this._isCharacterInPrison = value;
					base.OnPropertyChangedWithValue(value, "IsCharacterInPrison");
				}
			}
		}

		// Token: 0x170005F7 RID: 1527
		// (get) Token: 0x06001247 RID: 4679 RVA: 0x0004A1D6 File Offset: 0x000483D6
		// (set) Token: 0x06001248 RID: 4680 RVA: 0x0004A1DE File Offset: 0x000483DE
		[DataSourceProperty]
		public bool HasShips
		{
			get
			{
				return this._hasShips;
			}
			set
			{
				if (value != this._hasShips)
				{
					this._hasShips = value;
					base.OnPropertyChangedWithValue(value, "HasShips");
				}
			}
		}

		// Token: 0x170005F8 RID: 1528
		// (get) Token: 0x06001249 RID: 4681 RVA: 0x0004A1FC File Offset: 0x000483FC
		// (set) Token: 0x0600124A RID: 4682 RVA: 0x0004A204 File Offset: 0x00048404
		[DataSourceProperty]
		public bool IsIdle
		{
			get
			{
				return this._isIdle;
			}
			set
			{
				if (value != this._isIdle)
				{
					this._isIdle = value;
					base.OnPropertyChangedWithValue(value, "IsIdle");
				}
			}
		}

		// Token: 0x170005F9 RID: 1529
		// (get) Token: 0x0600124B RID: 4683 RVA: 0x0004A222 File Offset: 0x00048422
		// (set) Token: 0x0600124C RID: 4684 RVA: 0x0004A22A File Offset: 0x0004842A
		[DataSourceProperty]
		public bool IsPlayer
		{
			get
			{
				return this._isPlayer;
			}
			set
			{
				if (value != this._isPlayer)
				{
					this._isPlayer = value;
					base.OnPropertyChanged("IsPlayerParty");
				}
			}
		}

		// Token: 0x170005FA RID: 1530
		// (get) Token: 0x0600124D RID: 4685 RVA: 0x0004A247 File Offset: 0x00048447
		// (set) Token: 0x0600124E RID: 4686 RVA: 0x0004A24F File Offset: 0x0004844F
		[DataSourceProperty]
		public bool IsEnemy
		{
			get
			{
				return this._isEnemy;
			}
			set
			{
				if (value != this._isEnemy)
				{
					this._isEnemy = value;
					base.OnPropertyChangedWithValue(value, "IsEnemy");
				}
			}
		}

		// Token: 0x170005FB RID: 1531
		// (get) Token: 0x0600124F RID: 4687 RVA: 0x0004A26D File Offset: 0x0004846D
		// (set) Token: 0x06001250 RID: 4688 RVA: 0x0004A275 File Offset: 0x00048475
		[DataSourceProperty]
		public bool IsAlly
		{
			get
			{
				return this._isAlly;
			}
			set
			{
				if (value != this._isAlly)
				{
					this._isAlly = value;
					base.OnPropertyChangedWithValue(value, "IsAlly");
				}
			}
		}

		// Token: 0x170005FC RID: 1532
		// (get) Token: 0x06001251 RID: 4689 RVA: 0x0004A293 File Offset: 0x00048493
		// (set) Token: 0x06001252 RID: 4690 RVA: 0x0004A29B File Offset: 0x0004849B
		[DataSourceProperty]
		public bool IsNeutral
		{
			get
			{
				return this._isNeutral;
			}
			set
			{
				if (value != this._isNeutral)
				{
					this._isNeutral = value;
					base.OnPropertyChangedWithValue(value, "IsNeutral");
				}
			}
		}

		// Token: 0x170005FD RID: 1533
		// (get) Token: 0x06001253 RID: 4691 RVA: 0x0004A2B9 File Offset: 0x000484B9
		// (set) Token: 0x06001254 RID: 4692 RVA: 0x0004A2C1 File Offset: 0x000484C1
		[DataSourceProperty]
		public bool IsMergedWithArmy
		{
			get
			{
				return this._isMergedWithArmy;
			}
			set
			{
				if (value != this._isMergedWithArmy)
				{
					this._isMergedWithArmy = value;
					base.OnPropertyChangedWithValue(value, "IsMergedWithArmy");
				}
			}
		}

		// Token: 0x170005FE RID: 1534
		// (get) Token: 0x06001255 RID: 4693 RVA: 0x0004A2DF File Offset: 0x000484DF
		// (set) Token: 0x06001256 RID: 4694 RVA: 0x0004A2E7 File Offset: 0x000484E7
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x170005FF RID: 1535
		// (get) Token: 0x06001257 RID: 4695 RVA: 0x0004A30A File Offset: 0x0004850A
		// (set) Token: 0x06001258 RID: 4696 RVA: 0x0004A312 File Offset: 0x00048512
		[DataSourceProperty]
		public string SettlementPath
		{
			get
			{
				return this._settlementPath;
			}
			set
			{
				if (value != this._settlementPath)
				{
					this._settlementPath = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementPath");
				}
			}
		}

		// Token: 0x17000600 RID: 1536
		// (get) Token: 0x06001259 RID: 4697 RVA: 0x0004A335 File Offset: 0x00048535
		// (set) Token: 0x0600125A RID: 4698 RVA: 0x0004A33D File Offset: 0x0004853D
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._locationText;
			}
			set
			{
				if (value != this._locationText)
				{
					this._locationText = value;
					base.OnPropertyChangedWithValue<string>(value, "LocationText");
				}
			}
		}

		// Token: 0x17000601 RID: 1537
		// (get) Token: 0x0600125B RID: 4699 RVA: 0x0004A360 File Offset: 0x00048560
		// (set) Token: 0x0600125C RID: 4700 RVA: 0x0004A368 File Offset: 0x00048568
		[DataSourceProperty]
		public string PowerText
		{
			get
			{
				return this._powerText;
			}
			set
			{
				if (value != this._powerText)
				{
					this._powerText = value;
					base.OnPropertyChangedWithValue<string>(value, "PowerText");
				}
			}
		}

		// Token: 0x17000602 RID: 1538
		// (get) Token: 0x0600125D RID: 4701 RVA: 0x0004A38B File Offset: 0x0004858B
		// (set) Token: 0x0600125E RID: 4702 RVA: 0x0004A393 File Offset: 0x00048593
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x17000603 RID: 1539
		// (get) Token: 0x0600125F RID: 4703 RVA: 0x0004A3B6 File Offset: 0x000485B6
		// (set) Token: 0x06001260 RID: 4704 RVA: 0x0004A3BE File Offset: 0x000485BE
		[DataSourceProperty]
		public string ProfessionText
		{
			get
			{
				return this._professionText;
			}
			set
			{
				if (value != this._professionText)
				{
					this._professionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProfessionText");
				}
			}
		}

		// Token: 0x17000604 RID: 1540
		// (get) Token: 0x06001261 RID: 4705 RVA: 0x0004A3E1 File Offset: 0x000485E1
		// (set) Token: 0x06001262 RID: 4706 RVA: 0x0004A3E9 File Offset: 0x000485E9
		[DataSourceProperty]
		public string EncyclopediaCursorEffect
		{
			get
			{
				return this._encyclopediaCursorEffect;
			}
			set
			{
				if (value != this._encyclopediaCursorEffect)
				{
					this._encyclopediaCursorEffect = value;
					base.OnPropertyChangedWithValue<string>(value, "EncyclopediaCursorEffect");
				}
			}
		}

		// Token: 0x17000605 RID: 1541
		// (get) Token: 0x06001263 RID: 4707 RVA: 0x0004A40C File Offset: 0x0004860C
		// (set) Token: 0x06001264 RID: 4708 RVA: 0x0004A414 File Offset: 0x00048614
		[DataSourceProperty]
		public CharacterImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x17000606 RID: 1542
		// (get) Token: 0x06001265 RID: 4709 RVA: 0x0004A432 File Offset: 0x00048632
		// (set) Token: 0x06001266 RID: 4710 RVA: 0x0004A43A File Offset: 0x0004863A
		[DataSourceProperty]
		public BannerImageIdentifierVM Banner_9
		{
			get
			{
				return this._banner_9;
			}
			set
			{
				if (value != this._banner_9)
				{
					this._banner_9 = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner_9");
				}
			}
		}

		// Token: 0x17000607 RID: 1543
		// (get) Token: 0x06001267 RID: 4711 RVA: 0x0004A458 File Offset: 0x00048658
		// (set) Token: 0x06001268 RID: 4712 RVA: 0x0004A460 File Offset: 0x00048660
		[DataSourceProperty]
		public int PartySize
		{
			get
			{
				return this._partySize;
			}
			set
			{
				if (value != this._partySize)
				{
					this._partySize = value;
					base.OnPropertyChangedWithValue(value, "PartySize");
				}
			}
		}

		// Token: 0x17000608 RID: 1544
		// (get) Token: 0x06001269 RID: 4713 RVA: 0x0004A47E File Offset: 0x0004867E
		// (set) Token: 0x0600126A RID: 4714 RVA: 0x0004A486 File Offset: 0x00048686
		[DataSourceProperty]
		public int PartyWoundedSize
		{
			get
			{
				return this._partyWoundedSize;
			}
			set
			{
				if (value != this._partySize)
				{
					this._partyWoundedSize = value;
					base.OnPropertyChangedWithValue(value, "PartyWoundedSize");
				}
			}
		}

		// Token: 0x17000609 RID: 1545
		// (get) Token: 0x0600126B RID: 4715 RVA: 0x0004A4A4 File Offset: 0x000486A4
		// (set) Token: 0x0600126C RID: 4716 RVA: 0x0004A4AC File Offset: 0x000486AC
		[DataSourceProperty]
		public int ShipCount
		{
			get
			{
				return this._shipCount;
			}
			set
			{
				if (value != this._shipCount)
				{
					this._shipCount = value;
					base.OnPropertyChangedWithValue(value, "ShipCount");
				}
			}
		}

		// Token: 0x1700060A RID: 1546
		// (get) Token: 0x0600126D RID: 4717 RVA: 0x0004A4CA File Offset: 0x000486CA
		// (set) Token: 0x0600126E RID: 4718 RVA: 0x0004A4D2 File Offset: 0x000486D2
		[DataSourceProperty]
		public string PartySizeLbl
		{
			get
			{
				return this._partySizeLbl;
			}
			set
			{
				if (value != this._partySizeLbl)
				{
					this._partySizeLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "PartySizeLbl");
				}
			}
		}

		// Token: 0x1700060B RID: 1547
		// (get) Token: 0x0600126F RID: 4719 RVA: 0x0004A4F5 File Offset: 0x000486F5
		// (set) Token: 0x06001270 RID: 4720 RVA: 0x0004A4FD File Offset: 0x000486FD
		[DataSourceProperty]
		public bool IsLeader
		{
			get
			{
				return this._isLeader;
			}
			set
			{
				if (value != this._isLeader)
				{
					this._isLeader = value;
					base.OnPropertyChangedWithValue(value, "IsLeader");
				}
			}
		}

		// Token: 0x0400084D RID: 2125
		public CharacterObject Character;

		// Token: 0x0400084E RID: 2126
		public PartyBase Party;

		// Token: 0x0400084F RID: 2127
		public Settlement Settlement;

		// Token: 0x04000850 RID: 2128
		private readonly bool _canShowQuest = true;

		// Token: 0x04000851 RID: 2129
		private readonly bool _useCivilianEquipment;

		// Token: 0x04000852 RID: 2130
		private readonly Action<GameMenuPartyItemVM> _onSetAsContextMenuActiveItem;

		// Token: 0x04000853 RID: 2131
		private MBBindingList<QuestMarkerVM> _quests;

		// Token: 0x04000854 RID: 2132
		private int _partySize;

		// Token: 0x04000855 RID: 2133
		private int _partyWoundedSize;

		// Token: 0x04000856 RID: 2134
		private int _shipCount;

		// Token: 0x04000857 RID: 2135
		private int _relation = -101;

		// Token: 0x04000858 RID: 2136
		private CharacterImageIdentifierVM _visual;

		// Token: 0x04000859 RID: 2137
		private BannerImageIdentifierVM _banner_9;

		// Token: 0x0400085A RID: 2138
		private string _settlementPath;

		// Token: 0x0400085B RID: 2139
		private string _partySizeLbl;

		// Token: 0x0400085C RID: 2140
		private string _nameText;

		// Token: 0x0400085D RID: 2141
		private string _locationText;

		// Token: 0x0400085E RID: 2142
		private string _descriptionText;

		// Token: 0x0400085F RID: 2143
		private string _professionText;

		// Token: 0x04000860 RID: 2144
		private string _powerText;

		// Token: 0x04000861 RID: 2145
		private string _encyclopediaCursorEffect;

		// Token: 0x04000862 RID: 2146
		private bool _isIdle;

		// Token: 0x04000863 RID: 2147
		private bool _isPlayer;

		// Token: 0x04000864 RID: 2148
		private bool _isEnemy;

		// Token: 0x04000865 RID: 2149
		private bool _isAlly;

		// Token: 0x04000866 RID: 2150
		private bool _isNeutral;

		// Token: 0x04000867 RID: 2151
		private bool _isHighlightEnabled;

		// Token: 0x04000868 RID: 2152
		private bool _isLeader;

		// Token: 0x04000869 RID: 2153
		private bool _isMergedWithArmy;

		// Token: 0x0400086A RID: 2154
		private bool _isCharacterInPrison;

		// Token: 0x0400086B RID: 2155
		private bool _hasShips;

		// Token: 0x0200022E RID: 558
		private class QuestMarkerComparer : IComparer<QuestMarkerVM>
		{
			// Token: 0x060024A0 RID: 9376 RVA: 0x0007FDE8 File Offset: 0x0007DFE8
			public int Compare(QuestMarkerVM x, QuestMarkerVM y)
			{
				return x.QuestMarkerType.CompareTo(y.QuestMarkerType);
			}
		}
	}
}
