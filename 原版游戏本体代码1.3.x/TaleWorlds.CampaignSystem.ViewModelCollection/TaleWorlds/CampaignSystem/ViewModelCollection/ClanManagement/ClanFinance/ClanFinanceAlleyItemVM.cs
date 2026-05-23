using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance
{
	// Token: 0x02000132 RID: 306
	public class ClanFinanceAlleyItemVM : ClanFinanceIncomeItemBaseVM
	{
		// Token: 0x06001C7E RID: 7294 RVA: 0x000691E8 File Offset: 0x000673E8
		public ClanFinanceAlleyItemVM(Alley alley, Action<ClanCardSelectionInfo> openCardSelectionPopup, Action<ClanFinanceAlleyItemVM> onSelection, Action onRefresh)
			: base(null, onRefresh)
		{
			this.Alley = alley;
			this._alleyModel = Campaign.Current.Models.AlleyModel;
			this._alleyBehavior = Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>();
			this._onSelection = new Action<ClanFinanceIncomeItemBaseVM>(this.tempOnSelection);
			this._onSelectionT = onSelection;
			this._openCardSelectionPopup = openCardSelectionPopup;
			this.ManageAlleyHint = new HintViewModel();
			this._alleyOwner = this._alleyBehavior.GetAssignedClanMemberOfAlley(this.Alley);
			if (this._alleyOwner == null)
			{
				this._alleyOwner = this.Alley.Owner;
			}
			this.OwnerVisual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(this._alleyOwner.CharacterObject));
			Settlement settlement = this.Alley.Settlement;
			base.ImageName = ((((settlement != null) ? settlement.SettlementComponent : null) != null) ? this.Alley.Settlement.SettlementComponent.WaitMeshName : "");
			this.RefreshValues();
		}

		// Token: 0x06001C7F RID: 7295 RVA: 0x000692E4 File Offset: 0x000674E4
		public override void RefreshValues()
		{
			base.RefreshValues();
			base.Name = this.Alley.Name.ToString();
			base.Location = this.Alley.Settlement.Name.ToString();
			base.Income = this._alleyModel.GetDailyIncomeOfAlley(this.Alley);
			this.IncomeText = GameTexts.FindText("str_plus_with_number", null).SetTextVariable("NUMBER", base.Income).ToString();
			this.ManageAlleyHint.HintText = new TextObject("{=dQBArrqh}Manage Alley", null);
			this.PopulateStatsList();
		}

		// Token: 0x06001C80 RID: 7296 RVA: 0x00069381 File Offset: 0x00067581
		private void tempOnSelection(ClanFinanceIncomeItemBaseVM item)
		{
			this._onSelectionT(this);
		}

		// Token: 0x06001C81 RID: 7297 RVA: 0x00069390 File Offset: 0x00067590
		protected override void PopulateStatsList()
		{
			base.PopulateStatsList();
			base.ItemProperties.Clear();
			string variable = GameTexts.FindText("str_plus_with_number", null).SetTextVariable("NUMBER", this._alleyModel.GetDailyCrimeRatingOfAlley, 2).ToString();
			string value = new TextObject("{=LuC5ZZMu}{CRIMINAL_RATING} ({INCREASE}){CRIME_ICON}", null).SetTextVariable("CRIMINAL_RATING", this.Alley.Settlement.MapFaction.MainHeroCrimeRating, 2).SetTextVariable("INCREASE", variable).SetTextVariable("CRIME_ICON", "{=!}<img src=\"SPGeneral\\MapOverlay\\Settlement\\icon_crime\" extend=\"16\">")
				.ToString();
			this.IncomeTextWithVisual = new TextObject("{=ePmSvu1s}{AMOUNT}{GOLD_ICON}", null).SetTextVariable("AMOUNT", base.Income).ToString();
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=FkhJz0po}Location", null).ToString(), this.Alley.Settlement.Name.ToString(), false, null));
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=5k4dxUEJ}Troops", null).ToString(), this._alleyBehavior.GetPlayerOwnedAlleyTroopCount(this.Alley).ToString(), false, null));
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=QPoA6vvx}Income", null).ToString(), this.IncomeTextWithVisual, false, null));
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=r0WIRUHo}Criminal Rating", null).ToString(), value, false, null));
			string statusText = this.GetStatusText();
			if (!string.IsNullOrEmpty(statusText))
			{
				base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=DXczLzml}Status", null).ToString(), statusText, false, null));
			}
		}

		// Token: 0x06001C82 RID: 7298 RVA: 0x00069538 File Offset: 0x00067738
		private string GetStatusText()
		{
			string result = string.Empty;
			List<ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>> clanMembersAndAvailabilityDetailsForLeadingAnAlley = this._alleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(this.Alley);
			Hero assignedClanMemberOfAlley = this._alleyBehavior.GetAssignedClanMemberOfAlley(this.Alley);
			if (this._alleyBehavior.GetIsPlayerAlleyUnderAttack(this.Alley))
			{
				TextObject textObject = new TextObject("{=q1DVNQS7}Under Attack! ({RESPONSE_TIME} {?RESPONSE_TIME>1}days{?}day{\\?} left.)", null);
				textObject.SetTextVariable("RESPONSE_TIME", this._alleyBehavior.GetResponseTimeLeftForAttackInDays(this.Alley));
				result = textObject.ToString();
			}
			else if (assignedClanMemberOfAlley.IsDead)
			{
				result = new TextObject("{=KjuxDQfn}Alley leader is dead.", null).ToString();
			}
			else if (assignedClanMemberOfAlley.IsTraveling)
			{
				TextObject textObject2 = new TextObject("{=SFB2uYHa}Alley leader is traveling to the alley. ({LEFT_TIME} {?LEFT_TIME>1}hours{?}hour{\\?} left.)", null);
				textObject2.SetTextVariable("LEFT_TIME", MathF.Ceiling(TeleportationHelper.GetHoursLeftForTeleportingHeroToReachItsDestination(assignedClanMemberOfAlley)));
				result = textObject2.ToString();
			}
			else
			{
				for (int i = 0; i < clanMembersAndAvailabilityDetailsForLeadingAnAlley.Count; i++)
				{
					if (clanMembersAndAvailabilityDetailsForLeadingAnAlley[i].Item1 == Hero.MainHero && clanMembersAndAvailabilityDetailsForLeadingAnAlley[i].Item2 != DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available)
					{
						result = new TextObject("{=NHZ1jNIF}Below Requirements", null).ToString();
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x06001C83 RID: 7299 RVA: 0x00069648 File Offset: 0x00067848
		private ClanCardSelectionItemPropertyInfo GetSkillProperty(Hero hero, SkillObject skill)
		{
			TextObject value = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(skill.Name, new TextObject("{=!}" + hero.GetSkillValue(skill), null));
			return new ClanCardSelectionItemPropertyInfo(TextObject.GetEmpty(), value);
		}

		// Token: 0x06001C84 RID: 7300 RVA: 0x00069688 File Offset: 0x00067888
		private IEnumerable<ClanCardSelectionItemPropertyInfo> GetHeroProperties(Hero hero, Alley alley, DefaultAlleyModel.AlleyMemberAvailabilityDetail detail)
		{
			if (detail == DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay)
			{
				string partyDistanceByTimeText = CampaignUIHelper.GetPartyDistanceByTimeText(Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, alley.Settlement.Party).ResultNumber, Campaign.Current.Models.DelayedTeleportationModel.DefaultTeleportationSpeed);
				yield return new ClanCardSelectionItemPropertyInfo(new TextObject("{=!}" + partyDistanceByTimeText, null));
			}
			yield return new ClanCardSelectionItemPropertyInfo(new TextObject("{=bz7Glmsm}Skills", null), TextObject.GetEmpty());
			yield return this.GetSkillProperty(hero, DefaultSkills.Tactics);
			yield return this.GetSkillProperty(hero, DefaultSkills.Leadership);
			yield return this.GetSkillProperty(hero, DefaultSkills.Steward);
			yield return this.GetSkillProperty(hero, DefaultSkills.Roguery);
			yield break;
		}

		// Token: 0x06001C85 RID: 7301 RVA: 0x000696AD File Offset: 0x000678AD
		private IEnumerable<ClanCardSelectionItemInfo> GetAvailableMembers()
		{
			yield return new ClanCardSelectionItemInfo(new TextObject("{=W3hmFcfv}Abandon Alley", null), false, TextObject.GetEmpty(), TextObject.GetEmpty());
			List<ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail>> availabilityDetails = this._alleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(this.Alley);
			using (List<Hero>.Enumerator enumerator = Clan.PlayerClan.Heroes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Hero member = enumerator.Current;
					ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail> valueTuple = availabilityDetails.FirstOrDefault((ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail> x) => x.Item1 == member);
					if (valueTuple.Item1 != null)
					{
						CharacterCode characterCode = CharacterCode.CreateFrom(member.CharacterObject);
						bool isDisabled = valueTuple.Item2 != DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available && valueTuple.Item2 != DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay;
						yield return new ClanCardSelectionItemInfo(member, member.Name, new CharacterImageIdentifier(characterCode), CardSelectionItemSpriteType.None, null, null, this.GetHeroProperties(member, this.Alley, valueTuple.Item2), isDisabled, this._alleyModel.GetDisabledReasonTextForHero(member, this.Alley, valueTuple.Item2), null);
					}
				}
			}
			List<Hero>.Enumerator enumerator = default(List<Hero>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06001C86 RID: 7302 RVA: 0x000696C0 File Offset: 0x000678C0
		private void OnMemberSelection(List<object> members, Action closePopup)
		{
			if (members.Count > 0)
			{
				Hero hero = members[0] as Hero;
				if (hero != null)
				{
					this._alleyBehavior.ChangeAlleyMember(this.Alley, hero);
					Action onRefresh = this._onRefresh;
					if (onRefresh != null)
					{
						onRefresh();
					}
					Action closePopup2 = closePopup;
					if (closePopup2 == null)
					{
						return;
					}
					closePopup2();
					return;
				}
				else
				{
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=W3hmFcfv}Abandon Alley", null).ToString(), new TextObject("{=pBVbKYwo}You will lose the ownership of the alley and the troops in it. Are you sure?", null).ToString(), true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=8OkPHu4f}No", null).ToString(), delegate()
					{
						this._alleyBehavior.AbandonAlleyFromClanMenu(this.Alley);
						Action onRefresh2 = this._onRefresh;
						if (onRefresh2 != null)
						{
							onRefresh2();
						}
						Action closePopup3 = closePopup;
						if (closePopup3 == null)
						{
							return;
						}
						closePopup3();
					}, null, "", 0f, null, null, null), false, false);
				}
			}
		}

		// Token: 0x06001C87 RID: 7303 RVA: 0x0006979C File Offset: 0x0006799C
		public void ExecuteManageAlley()
		{
			ClanCardSelectionInfo obj = new ClanCardSelectionInfo(new TextObject("{=dQBArrqh}Manage Alley", null), this.GetAvailableMembers(), new Action<List<object>, Action>(this.OnMemberSelection), false, 1, 0);
			this._openCardSelectionPopup(obj);
		}

		// Token: 0x06001C88 RID: 7304 RVA: 0x000697DC File Offset: 0x000679DC
		public void ExecuteBeginHeroHint()
		{
			InformationManager.ShowTooltip(typeof(Hero), new object[] { this._alleyOwner, true });
		}

		// Token: 0x06001C89 RID: 7305 RVA: 0x00069805 File Offset: 0x00067A05
		public void ExecuteEndHeroHint()
		{
			InformationManager.HideTooltip();
		}

		// Token: 0x170009B3 RID: 2483
		// (get) Token: 0x06001C8A RID: 7306 RVA: 0x0006980C File Offset: 0x00067A0C
		// (set) Token: 0x06001C8B RID: 7307 RVA: 0x00069814 File Offset: 0x00067A14
		[DataSourceProperty]
		public HintViewModel ManageAlleyHint
		{
			get
			{
				return this._manageAlleyHint;
			}
			set
			{
				if (value != this._manageAlleyHint)
				{
					this._manageAlleyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ManageAlleyHint");
				}
			}
		}

		// Token: 0x170009B4 RID: 2484
		// (get) Token: 0x06001C8C RID: 7308 RVA: 0x00069832 File Offset: 0x00067A32
		// (set) Token: 0x06001C8D RID: 7309 RVA: 0x0006983A File Offset: 0x00067A3A
		[DataSourceProperty]
		public CharacterImageIdentifierVM OwnerVisual
		{
			get
			{
				return this._ownerVisual;
			}
			set
			{
				if (value != this._ownerVisual)
				{
					this._ownerVisual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "OwnerVisual");
				}
			}
		}

		// Token: 0x170009B5 RID: 2485
		// (get) Token: 0x06001C8E RID: 7310 RVA: 0x00069858 File Offset: 0x00067A58
		// (set) Token: 0x06001C8F RID: 7311 RVA: 0x00069860 File Offset: 0x00067A60
		[DataSourceProperty]
		public string IncomeText
		{
			get
			{
				return this._incomeText;
			}
			set
			{
				if (value != this._incomeText)
				{
					this._incomeText = value;
					base.OnPropertyChangedWithValue<string>(value, "IncomeText");
				}
			}
		}

		// Token: 0x170009B6 RID: 2486
		// (get) Token: 0x06001C90 RID: 7312 RVA: 0x00069883 File Offset: 0x00067A83
		// (set) Token: 0x06001C91 RID: 7313 RVA: 0x0006988B File Offset: 0x00067A8B
		[DataSourceProperty]
		public string IncomeTextWithVisual
		{
			get
			{
				return this._incomeTextWithVisual;
			}
			set
			{
				if (value != this._incomeTextWithVisual)
				{
					this._incomeTextWithVisual = value;
					base.OnPropertyChangedWithValue<string>(value, "IncomeTextWithVisual");
				}
			}
		}

		// Token: 0x04000D4B RID: 3403
		public readonly Alley Alley;

		// Token: 0x04000D4C RID: 3404
		private readonly Hero _alleyOwner;

		// Token: 0x04000D4D RID: 3405
		private readonly IAlleyCampaignBehavior _alleyBehavior;

		// Token: 0x04000D4E RID: 3406
		private readonly AlleyModel _alleyModel;

		// Token: 0x04000D4F RID: 3407
		private readonly Action<ClanFinanceAlleyItemVM> _onSelectionT;

		// Token: 0x04000D50 RID: 3408
		private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

		// Token: 0x04000D51 RID: 3409
		private HintViewModel _manageAlleyHint;

		// Token: 0x04000D52 RID: 3410
		private CharacterImageIdentifierVM _ownerVisual;

		// Token: 0x04000D53 RID: 3411
		private string _incomeText;

		// Token: 0x04000D54 RID: 3412
		private string _incomeTextWithVisual;
	}
}
