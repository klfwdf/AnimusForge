using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x0200001A RID: 26
	public class PartyNameplateVM : NameplateVM
	{
		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000268 RID: 616 RVA: 0x0000A5B3 File Offset: 0x000087B3
		// (set) Token: 0x06000269 RID: 617 RVA: 0x0000A5BB File Offset: 0x000087BB
		public MobileParty Party { get; private set; }

		// Token: 0x0600026A RID: 618 RVA: 0x0000A5C4 File Offset: 0x000087C4
		public PartyNameplateVM()
		{
			this.Quests = new MBBindingList<QuestMarkerVM>();
		}

		// Token: 0x0600026B RID: 619 RVA: 0x0000A5EC File Offset: 0x000087EC
		public void InitializeWith(MobileParty party, Camera mapCamera)
		{
			this._mapCamera = mapCamera;
			this.Party = party;
			this._isPartyBannerDirty = true;
			this.Quests.Clear();
			this.RegisterEvents();
		}

		// Token: 0x0600026C RID: 620 RVA: 0x0000A614 File Offset: 0x00008814
		public virtual void Clear()
		{
			this._mapCamera = null;
			this.Party = null;
			this._isPartyBannerDirty = false;
			this._latestNameTextObject = null;
			this._previousQuestsBind = CampaignUIHelper.IssueQuestFlags.None;
			this.Quests.Clear();
			this.OnFinalize();
			this.UnregisterEvents();
		}

		// Token: 0x0600026D RID: 621 RVA: 0x0000A650 File Offset: 0x00008850
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.RefreshDynamicProperties(true);
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0000A660 File Offset: 0x00008860
		public void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangeKingdom));
			CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnClanLeaderChanged));
			CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, MobileParty, TeleportHeroAction.TeleportationDetail>(this.OnHeroTeleportationRequested));
			if (Game.Current != null)
			{
				Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged));
			}
		}

		// Token: 0x0600026F RID: 623 RVA: 0x0000A6EC File Offset: 0x000088EC
		public void UnregisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.ClearListeners(this);
			CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
			CampaignEvents.OnClanLeaderChangedEvent.ClearListeners(this);
			CampaignEvents.OnHeroTeleportationRequestedEvent.ClearListeners(this);
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged));
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000A740 File Offset: 0x00008940
		private void AddQuestBindFlagsForParty(MobileParty party)
		{
			if (party != MobileParty.MainParty && party != this.Party)
			{
				Hero leaderHero = party.LeaderHero;
				if (((leaderHero != null) ? leaderHero.Issue : null) != null && (this._questsBind & CampaignUIHelper.IssueQuestFlags.TrackedIssue) == CampaignUIHelper.IssueQuestFlags.None && ((this._questsBind & CampaignUIHelper.IssueQuestFlags.AvailableIssue) == CampaignUIHelper.IssueQuestFlags.None || (this._questsBind & CampaignUIHelper.IssueQuestFlags.ActiveIssue) == CampaignUIHelper.IssueQuestFlags.None))
				{
					this._questsBind |= CampaignUIHelper.GetIssueType(party.LeaderHero.Issue);
				}
				if (((this._questsBind & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) == CampaignUIHelper.IssueQuestFlags.None && (this._questsBind & CampaignUIHelper.IssueQuestFlags.ActiveIssue) == CampaignUIHelper.IssueQuestFlags.None) || (this._questsBind & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) == CampaignUIHelper.IssueQuestFlags.None)
				{
					List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(party);
					for (int i = 0; i < questsRelatedToParty.Count; i++)
					{
						QuestBase questBase = questsRelatedToParty[i];
						if (party.LeaderHero != null && questBase.QuestGiver == party.LeaderHero)
						{
							if (questBase.IsSpecialQuest && (this._questsBind & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) == CampaignUIHelper.IssueQuestFlags.None)
							{
								this._questsBind |= CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest;
							}
							else if (!questBase.IsSpecialQuest && (this._questsBind & CampaignUIHelper.IssueQuestFlags.ActiveIssue) == CampaignUIHelper.IssueQuestFlags.None)
							{
								this._questsBind |= CampaignUIHelper.IssueQuestFlags.ActiveIssue;
							}
						}
						else if (questBase.IsSpecialQuest && (this._questsBind & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) == CampaignUIHelper.IssueQuestFlags.None)
						{
							this._questsBind |= CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest;
						}
						else if (!questBase.IsSpecialQuest && (this._questsBind & CampaignUIHelper.IssueQuestFlags.TrackedIssue) == CampaignUIHelper.IssueQuestFlags.None)
						{
							this._questsBind |= CampaignUIHelper.IssueQuestFlags.TrackedIssue;
						}
					}
				}
			}
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0000A89C File Offset: 0x00008A9C
		public override void RefreshDynamicProperties(bool forceUpdate)
		{
			base.RefreshDynamicProperties(forceUpdate);
			if (this._isVisibleOnMapBind || forceUpdate)
			{
				MobileParty party = this.Party;
				IssueBase issueBase;
				if (party == null)
				{
					issueBase = null;
				}
				else
				{
					Hero leaderHero = party.LeaderHero;
					issueBase = ((leaderHero != null) ? leaderHero.Issue : null);
				}
				IssueBase issueBase2 = issueBase;
				this._questsBind = CampaignUIHelper.IssueQuestFlags.None;
				if (this.Party != MobileParty.MainParty)
				{
					if (issueBase2 != null)
					{
						this._questsBind |= CampaignUIHelper.GetIssueType(issueBase2);
					}
					List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(this.Party);
					for (int i = 0; i < questsRelatedToParty.Count; i++)
					{
						QuestBase questBase = questsRelatedToParty[i];
						if (questBase.QuestGiver != null && questBase.QuestGiver == this.Party.LeaderHero)
						{
							this._questsBind |= (questBase.IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest : CampaignUIHelper.IssueQuestFlags.ActiveIssue);
						}
						else
						{
							this._questsBind |= (questBase.IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest : CampaignUIHelper.IssueQuestFlags.TrackedIssue);
						}
					}
				}
			}
			this._isInArmyBind = this.Party.Army != null && this.Party.AttachedTo != null;
			this._isArmyBind = this.Party.Army != null && this.Party.Army.LeaderParty == this.Party;
			MobileParty party2 = this.Party;
			this._isInSettlementBind = ((party2 != null) ? party2.CurrentSettlement : null) != null;
			if (this._isArmyBind && (this._isVisibleOnMapBind || forceUpdate))
			{
				this.AddQuestBindFlagsForParty(this.Party.Army.LeaderParty);
				for (int j = 0; j < this.Party.Army.LeaderParty.AttachedParties.Count; j++)
				{
					MobileParty party3 = this.Party.Army.LeaderParty.AttachedParties[j];
					this.AddQuestBindFlagsForParty(party3);
				}
			}
			if (this._isArmyBind || !this._isInArmy || forceUpdate)
			{
				int partyHealthyCount = SandBoxUIHelper.GetPartyHealthyCount(this.Party);
				if (partyHealthyCount != this._latestTotalCount)
				{
					this._latestTotalCount = partyHealthyCount;
					this._countBind = (this.Party.IsInfoHidden ? "?" : partyHealthyCount.ToString());
				}
				int allWoundedMembersAmount = SandBoxUIHelper.GetAllWoundedMembersAmount(this.Party);
				int allPrisonerMembersAmount = SandBoxUIHelper.GetAllPrisonerMembersAmount(this.Party);
				if (this._latestWoundedAmount != allWoundedMembersAmount || this._latestPrisonerAmount != allPrisonerMembersAmount)
				{
					if (this._latestWoundedAmount != allWoundedMembersAmount)
					{
						this._woundedBind = ((allWoundedMembersAmount == 0) ? "" : (this.Party.IsInfoHidden ? "?" : SandBoxUIHelper.GetPartyWoundedText(allWoundedMembersAmount)));
						this._latestWoundedAmount = allWoundedMembersAmount;
					}
					if (this._latestPrisonerAmount != allPrisonerMembersAmount)
					{
						this._prisonerBind = ((allPrisonerMembersAmount == 0) ? "" : (this.Party.IsInfoHidden ? "?" : SandBoxUIHelper.GetPartyPrisonerText(allPrisonerMembersAmount)));
						this._latestPrisonerAmount = allPrisonerMembersAmount;
					}
					this._extraInfoTextBind = this._woundedBind + this._prisonerBind;
				}
			}
			if (!this.Party.IsMainParty)
			{
				Army army = this.Party.Army;
				if (army == null || !army.LeaderParty.AttachedParties.Contains(MobileParty.MainParty) || !this.Party.Army.LeaderParty.AttachedParties.Contains(this.Party))
				{
					Hero mainHero = Hero.MainHero;
					if (((mainHero != null) ? mainHero.MapFaction : null) != null)
					{
						IFaction mapFaction = this.Party.MapFaction;
						Hero mainHero2 = Hero.MainHero;
						if (FactionManager.IsAtWarAgainstFaction(mapFaction, (mainHero2 != null) ? mainHero2.MapFaction : null))
						{
							this._factionColorBind = ((this.Party.Army != null && this.Party.Army.LeaderParty == this.Party) ? PartyNameplateVM.NegativeArmyIndicator : PartyNameplateVM.NegativeIndicator);
							goto IL_46E;
						}
					}
					if (DiplomacyHelper.IsSameFactionAndNotEliminated(this.Party.MapFaction, Hero.MainHero.MapFaction))
					{
						this._factionColorBind = ((this.Party.Army != null && this.Party.Army.LeaderParty == this.Party) ? PartyNameplateVM.PositiveArmyIndicator : PartyNameplateVM.PositiveIndicator);
						goto IL_46E;
					}
					this._factionColorBind = ((this.Party.Army != null && this.Party.Army.LeaderParty == this.Party) ? PartyNameplateVM.NeutralArmyIndicator : PartyNameplateVM.NeutralIndicator);
					goto IL_46E;
				}
			}
			this._factionColorBind = ((this.Party.Army != null && this.Party.Army.LeaderParty == this.Party) ? PartyNameplateVM.MainPartyArmyIndicator : PartyNameplateVM.MainPartyIndicator);
			IL_46E:
			if (this._isPartyBannerDirty || forceUpdate)
			{
				this.PartyBanner = new BannerImageIdentifierVM(this.Party.Banner, true);
				this._isPartyBannerDirty = false;
			}
			if (this._isVisibleOnMapBind && (this._isInArmyBind || this._isInSettlementBind || (!this.Party.IsMainParty && this.Party.IsInRaftState)))
			{
				this._isVisibleOnMapBind = false;
			}
			Army army2 = this.Party.Army;
			TextObject textObject;
			if (army2 != null && army2.DoesLeaderPartyAndAttachedPartiesContain(this.Party))
			{
				textObject = this.Party.ArmyName;
			}
			else if (this.Party.LeaderHero != null)
			{
				textObject = this.Party.LeaderHero.Name;
			}
			else
			{
				textObject = this.Party.Name;
			}
			this._isDisorganizedBind = this.Party.IsDisorganized;
			if (this._latestNameTextObject == null || forceUpdate || !this._latestNameTextObject.Equals(textObject))
			{
				this._latestNameTextObject = textObject;
				this._fullNameBind = this._latestNameTextObject.ToString();
			}
			if (this.Party.IsActive && !this._cachedSpeed.ApproximatelyEqualsTo(this.Party.Speed, 0.01f))
			{
				this._cachedSpeed = this.Party.Speed;
				this._movementSpeedTextBind = this._cachedSpeed.ToString("F1");
			}
			this._isCurrentlyAtSeaBind = this.Party.IsCurrentlyAtSea;
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000AE7C File Offset: 0x0000907C
		public override void RefreshPosition()
		{
			base.RefreshPosition();
			Vec3 vec = (this.Party.Position + this.Party.EventPositionAdder).AsVec3();
			Vec3 worldSpacePosition = vec + new Vec3(0f, 0f, 0.8f, -1f);
			this._latestX = 0f;
			this._latestY = 0f;
			this._latestW = 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, vec, ref this._latestX, ref this._latestY, ref this._latestW);
			this._partyPositionBind = new Vec2(this._latestX, this._latestY);
			MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, worldSpacePosition, ref this._latestX, ref this._latestY, ref this._latestW);
			this._headPositionBind = new Vec2(this._latestX, this._latestY);
			base.DistanceToCamera = vec.Distance(this._mapCamera.Position);
		}

		// Token: 0x06000273 RID: 627 RVA: 0x0000AF78 File Offset: 0x00009178
		public override void RefreshTutorialStatus(string newTutorialHighlightElementID)
		{
			base.RefreshTutorialStatus(newTutorialHighlightElementID);
			MobileParty party = this.Party;
			bool flag;
			if (party == null)
			{
				flag = null != null;
			}
			else
			{
				PartyBase party2 = party.Party;
				flag = ((party2 != null) ? party2.Id : null) != null;
			}
			if (!flag)
			{
				Debug.FailedAssert("Mobile party id is null when refreshing tutorial status", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\PartyNameplateVM.cs", "RefreshTutorialStatus", 344);
				return;
			}
			this._bindIsTargetedByTutorial = this.Party.Party.Id == newTutorialHighlightElementID;
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0000AFE8 File Offset: 0x000091E8
		public void DetermineIsVisibleOnMap()
		{
			this._isVisibleOnMapBind = this._latestW < 100f && this._latestW > 0f && this._mapCamera.Position.z < 200f;
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000B024 File Offset: 0x00009224
		private bool IsInsideWindow()
		{
			return this._latestX <= Screen.RealScreenResolutionWidth && this._latestY <= Screen.RealScreenResolutionHeight && this._latestX + 100f >= 0f && this._latestY + 30f >= 0f;
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0000B078 File Offset: 0x00009278
		public virtual void RefreshBinding()
		{
			base.Position = this._partyPositionBind;
			this.HeadPosition = this._headPositionBind;
			base.IsVisibleOnMap = this._isVisibleOnMapBind;
			this.IsInSettlement = this._isInSettlementBind;
			base.FactionColor = this._factionColorBind;
			this.IsHigh = this._isHighBind;
			this.Count = this._countBind;
			this.Prisoner = this._prisonerBind;
			this.Wounded = this._woundedBind;
			this.IsBehind = this._isBehindBind;
			this.FullName = this._fullNameBind;
			base.IsTargetedByTutorial = this._bindIsTargetedByTutorial;
			this.IsInArmy = this._isInArmyBind;
			this.IsArmy = this._isArmyBind;
			this.ExtraInfoText = this._extraInfoTextBind;
			this.IsDisorganized = this._isDisorganizedBind;
			this.MovementSpeedText = this._movementSpeedTextBind;
			this.IsCurrentlyAtSea = this._isCurrentlyAtSeaBind;
			if (this._previousQuestsBind != this._questsBind)
			{
				this.Quests.Clear();
				for (int i = 0; i < CampaignUIHelper.IssueQuestFlagsValues.Length; i++)
				{
					CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlagsValues[i];
					if (issueQuestFlags != CampaignUIHelper.IssueQuestFlags.None && (this._questsBind & issueQuestFlags) != CampaignUIHelper.IssueQuestFlags.None)
					{
						this.Quests.Add(new QuestMarkerVM(issueQuestFlags, null, null));
					}
				}
				this._previousQuestsBind = this._questsBind;
			}
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0000B1BC File Offset: 0x000093BC
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			bool flag = this.Party.HomeSettlement != null && (this.Party.HomeSettlement.IsVillage ? settlement.BoundVillages.Contains(this.Party.HomeSettlement.Village) : (this.Party.HomeSettlement == settlement));
			if ((this.Party.IsCaravan || this.Party.IsVillager) && flag)
			{
				this._isPartyBannerDirty = true;
			}
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000B23D File Offset: 0x0000943D
		private void OnClanChangeKingdom(Clan arg1, Kingdom arg2, Kingdom arg3, ChangeKingdomAction.ChangeKingdomActionDetail arg4, bool showNotification)
		{
			Hero leaderHero = this.Party.LeaderHero;
			if (((leaderHero != null) ? leaderHero.Clan : null) == arg1)
			{
				this._isPartyBannerDirty = true;
			}
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0000B260 File Offset: 0x00009460
		private void OnClanLeaderChanged(Hero arg1, Hero arg2)
		{
			if (arg2.MapFaction == this.Party.MapFaction)
			{
				this._isPartyBannerDirty = true;
			}
		}

		// Token: 0x0600027A RID: 634 RVA: 0x0000B27C File Offset: 0x0000947C
		private void OnHeroTeleportationRequested(Hero arg1, Settlement arg2, MobileParty arg3, TeleportHeroAction.TeleportationDetail arg4)
		{
			if (arg1.MapFaction == this.Party.MapFaction)
			{
				this._isPartyBannerDirty = true;
			}
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x0600027B RID: 635 RVA: 0x0000B298 File Offset: 0x00009498
		// (set) Token: 0x0600027C RID: 636 RVA: 0x0000B2A0 File Offset: 0x000094A0
		public Vec2 HeadPosition
		{
			get
			{
				return this._headPosition;
			}
			set
			{
				if (value != this._headPosition)
				{
					this._headPosition = value;
					base.OnPropertyChangedWithValue(value, "HeadPosition");
				}
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x0600027D RID: 637 RVA: 0x0000B2C3 File Offset: 0x000094C3
		// (set) Token: 0x0600027E RID: 638 RVA: 0x0000B2CB File Offset: 0x000094CB
		public string Count
		{
			get
			{
				return this._count;
			}
			set
			{
				if (value != this._count)
				{
					this._count = value;
					base.OnPropertyChangedWithValue<string>(value, "Count");
				}
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x0600027F RID: 639 RVA: 0x0000B2EE File Offset: 0x000094EE
		// (set) Token: 0x06000280 RID: 640 RVA: 0x0000B2F6 File Offset: 0x000094F6
		public string Prisoner
		{
			get
			{
				return this._prisoner;
			}
			set
			{
				if (value != this._prisoner)
				{
					this._prisoner = value;
					base.OnPropertyChangedWithValue<string>(value, "Prisoner");
				}
			}
		}

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x06000281 RID: 641 RVA: 0x0000B319 File Offset: 0x00009519
		// (set) Token: 0x06000282 RID: 642 RVA: 0x0000B321 File Offset: 0x00009521
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

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x06000283 RID: 643 RVA: 0x0000B33F File Offset: 0x0000953F
		// (set) Token: 0x06000284 RID: 644 RVA: 0x0000B347 File Offset: 0x00009547
		public string Wounded
		{
			get
			{
				return this._wounded;
			}
			set
			{
				if (value != this._wounded)
				{
					this._wounded = value;
					base.OnPropertyChangedWithValue<string>(value, "Wounded");
				}
			}
		}

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x06000285 RID: 645 RVA: 0x0000B36A File Offset: 0x0000956A
		// (set) Token: 0x06000286 RID: 646 RVA: 0x0000B372 File Offset: 0x00009572
		public string ExtraInfoText
		{
			get
			{
				return this._extraInfoText;
			}
			set
			{
				if (value != this._extraInfoText)
				{
					this._extraInfoText = value;
					base.OnPropertyChangedWithValue<string>(value, "ExtraInfoText");
				}
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x06000287 RID: 647 RVA: 0x0000B395 File Offset: 0x00009595
		// (set) Token: 0x06000288 RID: 648 RVA: 0x0000B39D File Offset: 0x0000959D
		public string MovementSpeedText
		{
			get
			{
				return this._movementSpeedText;
			}
			set
			{
				if (value != this._movementSpeedText)
				{
					this._movementSpeedText = value;
					base.OnPropertyChangedWithValue<string>(value, "MovementSpeedText");
				}
			}
		}

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000289 RID: 649 RVA: 0x0000B3C0 File Offset: 0x000095C0
		// (set) Token: 0x0600028A RID: 650 RVA: 0x0000B3C8 File Offset: 0x000095C8
		public string FullName
		{
			get
			{
				return this._fullName;
			}
			set
			{
				if (value != this._fullName)
				{
					this._fullName = value;
					base.OnPropertyChangedWithValue<string>(value, "FullName");
				}
			}
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x0600028B RID: 651 RVA: 0x0000B3EB File Offset: 0x000095EB
		// (set) Token: 0x0600028C RID: 652 RVA: 0x0000B3F3 File Offset: 0x000095F3
		public bool IsInArmy
		{
			get
			{
				return this._isInArmy;
			}
			set
			{
				if (value != this._isInArmy)
				{
					this._isInArmy = value;
					base.OnPropertyChangedWithValue(value, "IsInArmy");
				}
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x0600028D RID: 653 RVA: 0x0000B411 File Offset: 0x00009611
		// (set) Token: 0x0600028E RID: 654 RVA: 0x0000B419 File Offset: 0x00009619
		public bool IsInSettlement
		{
			get
			{
				return this._isInSettlement;
			}
			set
			{
				if (value != this._isInSettlement)
				{
					this._isInSettlement = value;
					base.OnPropertyChangedWithValue(value, "IsInSettlement");
				}
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x0600028F RID: 655 RVA: 0x0000B437 File Offset: 0x00009637
		// (set) Token: 0x06000290 RID: 656 RVA: 0x0000B43F File Offset: 0x0000963F
		public bool IsDisorganized
		{
			get
			{
				return this._isDisorganized;
			}
			set
			{
				if (value != this._isDisorganized)
				{
					this._isDisorganized = value;
					base.OnPropertyChangedWithValue(value, "IsDisorganized");
				}
			}
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000291 RID: 657 RVA: 0x0000B45D File Offset: 0x0000965D
		// (set) Token: 0x06000292 RID: 658 RVA: 0x0000B465 File Offset: 0x00009665
		public bool IsCurrentlyAtSea
		{
			get
			{
				return this._isCurrentlyAtSea;
			}
			set
			{
				if (value != this._isCurrentlyAtSea)
				{
					this._isCurrentlyAtSea = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentlyAtSea");
				}
			}
		}

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000293 RID: 659 RVA: 0x0000B483 File Offset: 0x00009683
		// (set) Token: 0x06000294 RID: 660 RVA: 0x0000B48B File Offset: 0x0000968B
		public bool IsArmy
		{
			get
			{
				return this._isArmy;
			}
			set
			{
				if (value != this._isArmy)
				{
					this._isArmy = value;
					base.OnPropertyChangedWithValue(value, "IsArmy");
				}
			}
		}

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000295 RID: 661 RVA: 0x0000B4A9 File Offset: 0x000096A9
		// (set) Token: 0x06000296 RID: 662 RVA: 0x0000B4B1 File Offset: 0x000096B1
		public bool IsBehind
		{
			get
			{
				return this._isBehind;
			}
			set
			{
				if (value != this._isBehind)
				{
					this._isBehind = value;
					base.OnPropertyChangedWithValue(value, "IsBehind");
				}
			}
		}

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x06000297 RID: 663 RVA: 0x0000B4CF File Offset: 0x000096CF
		// (set) Token: 0x06000298 RID: 664 RVA: 0x0000B4D7 File Offset: 0x000096D7
		public bool IsHigh
		{
			get
			{
				return this._isHigh;
			}
			set
			{
				if (value != this._isHigh)
				{
					this._isHigh = value;
					base.OnPropertyChangedWithValue(value, "IsHigh");
				}
			}
		}

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x06000299 RID: 665 RVA: 0x0000B4F5 File Offset: 0x000096F5
		// (set) Token: 0x0600029A RID: 666 RVA: 0x0000B507 File Offset: 0x00009707
		public bool ShouldShowFullName
		{
			get
			{
				return this._shouldShowFullName || base.IsTargetedByTutorial;
			}
			set
			{
				if (value != this._shouldShowFullName)
				{
					this._shouldShowFullName = value;
					base.OnPropertyChangedWithValue(value, "ShouldShowFullName");
				}
			}
		}

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x0600029B RID: 667 RVA: 0x0000B525 File Offset: 0x00009725
		// (set) Token: 0x0600029C RID: 668 RVA: 0x0000B52D File Offset: 0x0000972D
		public BannerImageIdentifierVM PartyBanner
		{
			get
			{
				return this._partyBanner;
			}
			set
			{
				if (value != this._partyBanner)
				{
					this._partyBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "PartyBanner");
				}
			}
		}

		// Token: 0x04000115 RID: 277
		public static string PositiveIndicator = Color.FromUint(4285650500U).ToString();

		// Token: 0x04000116 RID: 278
		public static string PositiveArmyIndicator = Color.FromUint(4288804731U).ToString();

		// Token: 0x04000117 RID: 279
		public static string NegativeIndicator = Color.FromUint(4292232774U).ToString();

		// Token: 0x04000118 RID: 280
		public static string NegativeArmyIndicator = Color.FromUint(4294931829U).ToString();

		// Token: 0x04000119 RID: 281
		public static string NeutralIndicator = Color.FromUint(4291877096U).ToString();

		// Token: 0x0400011A RID: 282
		public static string NeutralArmyIndicator = Color.FromUint(4294573055U).ToString();

		// Token: 0x0400011B RID: 283
		public static string MainPartyIndicator = Color.FromUint(4287421380U).ToString();

		// Token: 0x0400011C RID: 284
		public static string MainPartyArmyIndicator = Color.FromUint(4289593317U).ToString();

		// Token: 0x0400011E RID: 286
		protected float _latestX;

		// Token: 0x0400011F RID: 287
		protected float _latestY;

		// Token: 0x04000120 RID: 288
		protected float _latestW;

		// Token: 0x04000121 RID: 289
		protected float _cachedSpeed;

		// Token: 0x04000122 RID: 290
		protected Camera _mapCamera;

		// Token: 0x04000123 RID: 291
		protected int _latestPrisonerAmount = -1;

		// Token: 0x04000124 RID: 292
		protected int _latestWoundedAmount = -1;

		// Token: 0x04000125 RID: 293
		protected int _latestTotalCount = -1;

		// Token: 0x04000126 RID: 294
		protected bool _isPartyBannerDirty;

		// Token: 0x04000127 RID: 295
		protected TextObject _latestNameTextObject;

		// Token: 0x04000128 RID: 296
		protected CampaignUIHelper.IssueQuestFlags _previousQuestsBind;

		// Token: 0x04000129 RID: 297
		protected CampaignUIHelper.IssueQuestFlags _questsBind;

		// Token: 0x0400012A RID: 298
		protected Vec2 _partyPositionBind;

		// Token: 0x0400012B RID: 299
		protected Vec2 _headPositionBind;

		// Token: 0x0400012C RID: 300
		protected bool _isHighBind;

		// Token: 0x0400012D RID: 301
		protected bool _isBehindBind;

		// Token: 0x0400012E RID: 302
		protected bool _isInArmyBind;

		// Token: 0x0400012F RID: 303
		protected bool _isInSettlementBind;

		// Token: 0x04000130 RID: 304
		protected bool _isVisibleOnMapBind;

		// Token: 0x04000131 RID: 305
		protected bool _isArmyBind;

		// Token: 0x04000132 RID: 306
		protected bool _isDisorganizedBind;

		// Token: 0x04000133 RID: 307
		protected bool _isCurrentlyAtSeaBind;

		// Token: 0x04000134 RID: 308
		protected string _factionColorBind;

		// Token: 0x04000135 RID: 309
		protected string _countBind;

		// Token: 0x04000136 RID: 310
		protected string _woundedBind;

		// Token: 0x04000137 RID: 311
		protected string _prisonerBind;

		// Token: 0x04000138 RID: 312
		protected string _extraInfoTextBind;

		// Token: 0x04000139 RID: 313
		protected string _fullNameBind;

		// Token: 0x0400013A RID: 314
		protected string _movementSpeedTextBind;

		// Token: 0x0400013B RID: 315
		private string _count;

		// Token: 0x0400013C RID: 316
		private string _wounded;

		// Token: 0x0400013D RID: 317
		private string _prisoner;

		// Token: 0x0400013E RID: 318
		private MBBindingList<QuestMarkerVM> _quests;

		// Token: 0x0400013F RID: 319
		private string _fullName;

		// Token: 0x04000140 RID: 320
		private string _extraInfoText;

		// Token: 0x04000141 RID: 321
		private string _movementSpeedText;

		// Token: 0x04000142 RID: 322
		private bool _isBehind;

		// Token: 0x04000143 RID: 323
		private bool _isHigh;

		// Token: 0x04000144 RID: 324
		private bool _shouldShowFullName;

		// Token: 0x04000145 RID: 325
		private bool _isInArmy;

		// Token: 0x04000146 RID: 326
		private bool _isArmy;

		// Token: 0x04000147 RID: 327
		private bool _isInSettlement;

		// Token: 0x04000148 RID: 328
		private bool _isDisorganized;

		// Token: 0x04000149 RID: 329
		private bool _isCurrentlyAtSea;

		// Token: 0x0400014A RID: 330
		private BannerImageIdentifierVM _partyBanner;

		// Token: 0x0400014B RID: 331
		private Vec2 _headPosition;
	}
}
