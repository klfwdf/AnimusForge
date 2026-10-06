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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;

public class ClanFinanceAlleyItemVM : ClanFinanceIncomeItemBaseVM
{
	public readonly Alley Alley;

	private readonly Hero _alleyOwner;

	private readonly IAlleyCampaignBehavior _alleyBehavior;

	private readonly AlleyModel _alleyModel;

	private readonly Action<ClanFinanceAlleyItemVM> _onSelectionT;

	private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

	private HintViewModel _manageAlleyHint;

	private CharacterImageIdentifierVM _ownerVisual;

	private string _incomeText;

	private string _incomeTextWithVisual;

	[DataSourceProperty]
	public HintViewModel ManageAlleyHint
	{
		get
		{
			return _manageAlleyHint;
		}
		set
		{
			if (value != _manageAlleyHint)
			{
				_manageAlleyHint = value;
				OnPropertyChangedWithValue(value, "ManageAlleyHint");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM OwnerVisual
	{
		get
		{
			return _ownerVisual;
		}
		set
		{
			if (value != _ownerVisual)
			{
				_ownerVisual = value;
				OnPropertyChangedWithValue(value, "OwnerVisual");
			}
		}
	}

	[DataSourceProperty]
	public string IncomeText
	{
		get
		{
			return _incomeText;
		}
		set
		{
			if (value != _incomeText)
			{
				_incomeText = value;
				OnPropertyChangedWithValue(value, "IncomeText");
			}
		}
	}

	[DataSourceProperty]
	public string IncomeTextWithVisual
	{
		get
		{
			return _incomeTextWithVisual;
		}
		set
		{
			if (value != _incomeTextWithVisual)
			{
				_incomeTextWithVisual = value;
				OnPropertyChangedWithValue(value, "IncomeTextWithVisual");
			}
		}
	}

	public ClanFinanceAlleyItemVM(Alley alley, Action<ClanCardSelectionInfo> openCardSelectionPopup, Action<ClanFinanceAlleyItemVM> onSelection, Action onRefresh)
		: base(null, onRefresh)
	{
		Alley = alley;
		_alleyModel = Campaign.Current.Models.AlleyModel;
		_alleyBehavior = Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>();
		_onSelection = tempOnSelection;
		_onSelectionT = onSelection;
		_openCardSelectionPopup = openCardSelectionPopup;
		ManageAlleyHint = new HintViewModel();
		_alleyOwner = _alleyBehavior.GetAssignedClanMemberOfAlley(Alley);
		if (_alleyOwner == null)
		{
			_alleyOwner = Alley.Owner;
		}
		OwnerVisual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(_alleyOwner.CharacterObject));
		base.ImageName = ((Alley.Settlement?.SettlementComponent != null) ? Alley.Settlement.SettlementComponent.WaitMeshName : "");
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Alley.Name.ToString();
		base.Location = Alley.Settlement.Name.ToString();
		base.Income = _alleyModel.GetDailyIncomeOfAlley(Alley);
		IncomeText = GameTexts.FindText("str_plus_with_number").SetTextVariable("NUMBER", base.Income).ToString();
		ManageAlleyHint.HintText = new TextObject("{=dQBArrqh}Manage Alley");
		PopulateStatsList();
	}

	private void tempOnSelection(ClanFinanceIncomeItemBaseVM item)
	{
		_onSelectionT(this);
	}

	protected override void PopulateStatsList()
	{
		base.PopulateStatsList();
		base.ItemProperties.Clear();
		string variable = GameTexts.FindText("str_plus_with_number").SetTextVariable("NUMBER", _alleyModel.GetDailyCrimeRatingOfAlley).ToString();
		string value = new TextObject("{=LuC5ZZMu}{CRIMINAL_RATING} ({INCREASE}){CRIME_ICON}").SetTextVariable("CRIMINAL_RATING", Alley.Settlement.MapFaction.MainHeroCrimeRating).SetTextVariable("INCREASE", variable).SetTextVariable("CRIME_ICON", "{=!}<img src=\"SPGeneral\\MapOverlay\\Settlement\\icon_crime\" extend=\"12\">")
			.ToString();
		IncomeTextWithVisual = new TextObject("{=ePmSvu1s}{AMOUNT}{GOLD_ICON}").SetTextVariable("AMOUNT", base.Income).ToString();
		base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=FkhJz0po}Location").ToString(), Alley.Settlement.Name.ToString()));
		base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=5k4dxUEJ}Troops").ToString(), _alleyBehavior.GetPlayerOwnedAlleyTroopCount(Alley).ToString()));
		base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=QPoA6vvx}Income").ToString(), IncomeTextWithVisual));
		base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=r0WIRUHo}Criminal Rating").ToString(), value));
		string statusText = GetStatusText();
		if (!string.IsNullOrEmpty(statusText))
		{
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=DXczLzml}Status").ToString(), statusText));
		}
	}

	private string GetStatusText()
	{
		string result = string.Empty;
		List<(Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail)> clanMembersAndAvailabilityDetailsForLeadingAnAlley = _alleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(Alley);
		Hero assignedClanMemberOfAlley = _alleyBehavior.GetAssignedClanMemberOfAlley(Alley);
		if (_alleyBehavior.GetIsPlayerAlleyUnderAttack(Alley))
		{
			TextObject textObject = new TextObject("{=q1DVNQS7}Under Attack! ({RESPONSE_TIME} {?RESPONSE_TIME>1}days{?}day{\\?} left.)");
			textObject.SetTextVariable("RESPONSE_TIME", _alleyBehavior.GetResponseTimeLeftForAttackInDays(Alley));
			result = textObject.ToString();
		}
		else if (assignedClanMemberOfAlley.IsDead)
		{
			result = new TextObject("{=KjuxDQfn}Alley leader is dead.").ToString();
		}
		else if (assignedClanMemberOfAlley.IsTraveling)
		{
			TextObject textObject2 = new TextObject("{=SFB2uYHa}Alley leader is traveling to the alley. ({LEFT_TIME} {?LEFT_TIME>1}hours{?}hour{\\?} left.)");
			textObject2.SetTextVariable("LEFT_TIME", TaleWorlds.Library.MathF.Ceiling(TeleportationHelper.GetHoursLeftForTeleportingHeroToReachItsDestination(assignedClanMemberOfAlley)));
			result = textObject2.ToString();
		}
		else
		{
			for (int i = 0; i < clanMembersAndAvailabilityDetailsForLeadingAnAlley.Count; i++)
			{
				if (clanMembersAndAvailabilityDetailsForLeadingAnAlley[i].Item1 == Hero.MainHero && clanMembersAndAvailabilityDetailsForLeadingAnAlley[i].Item2 != DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available)
				{
					result = new TextObject("{=NHZ1jNIF}Below Requirements").ToString();
					break;
				}
			}
		}
		return result;
	}

	private ClanCardSelectionItemPropertyInfo GetSkillProperty(Hero hero, SkillObject skill)
	{
		TextObject value = ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(skill.Name, new TextObject("{=!}" + hero.GetSkillValue(skill)));
		return new ClanCardSelectionItemPropertyInfo(TextObject.GetEmpty(), value);
	}

	private IEnumerable<ClanCardSelectionItemPropertyInfo> GetHeroProperties(Hero hero, Alley alley, DefaultAlleyModel.AlleyMemberAvailabilityDetail detail)
	{
		if (detail == DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay)
		{
			string partyDistanceByTimeText = CampaignUIHelper.GetPartyDistanceByTimeText(Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, alley.Settlement.Party).ResultNumber, Campaign.Current.Models.DelayedTeleportationModel.DefaultTeleportationSpeed);
			yield return new ClanCardSelectionItemPropertyInfo(new TextObject("{=!}" + partyDistanceByTimeText));
		}
		yield return new ClanCardSelectionItemPropertyInfo(new TextObject("{=bz7Glmsm}Skills"), TextObject.GetEmpty());
		yield return GetSkillProperty(hero, DefaultSkills.Tactics);
		yield return GetSkillProperty(hero, DefaultSkills.Leadership);
		yield return GetSkillProperty(hero, DefaultSkills.Steward);
		yield return GetSkillProperty(hero, DefaultSkills.Roguery);
	}

	private IEnumerable<ClanCardSelectionItemInfo> GetAvailableMembers()
	{
		yield return new ClanCardSelectionItemInfo(new TextObject("{=W3hmFcfv}Abandon Alley"), isDisabled: false, TextObject.GetEmpty(), TextObject.GetEmpty());
		List<(Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail)> availabilityDetails = _alleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(Alley);
		foreach (Hero member in Clan.PlayerClan.Heroes)
		{
			(Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail) tuple = availabilityDetails.FirstOrDefault(((Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail) x) => x.Item1 == member);
			if (tuple.Item1 != null)
			{
				CharacterCode characterCode = CharacterCode.CreateFrom(member.CharacterObject);
				bool isDisabled = tuple.Item2 != DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available && tuple.Item2 != DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay;
				yield return new ClanCardSelectionItemInfo(member, member.Name, new CharacterImageIdentifier(characterCode), CardSelectionItemSpriteType.None, null, null, GetHeroProperties(member, Alley, tuple.Item2), isDisabled, _alleyModel.GetDisabledReasonTextForHero(member, Alley, tuple.Item2), null);
			}
		}
	}

	private void OnMemberSelection(List<object> members, Action closePopup)
	{
		Hero hero = null;
		if (members.Count <= 0)
		{
			return;
		}
		if (members[0] is Hero newAlleyLead)
		{
			_alleyBehavior.ChangeAlleyMember(Alley, newAlleyLead);
			_onRefresh?.Invoke();
			closePopup?.Invoke();
			return;
		}
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=W3hmFcfv}Abandon Alley").ToString(), new TextObject("{=pBVbKYwo}You will lose the ownership of the alley and the troops in it. Are you sure?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), delegate
		{
			_alleyBehavior.AbandonAlleyFromClanMenu(Alley);
			_onRefresh?.Invoke();
			closePopup?.Invoke();
		}, null));
	}

	public void ExecuteManageAlley()
	{
		ClanCardSelectionInfo obj = new ClanCardSelectionInfo(new TextObject("{=dQBArrqh}Manage Alley"), GetAvailableMembers(), OnMemberSelection, isMultiSelection: false);
		_openCardSelectionPopup(obj);
	}

	public void ExecuteBeginHeroHint()
	{
		InformationManager.ShowTooltip(typeof(Hero), _alleyOwner, true);
	}

	public void ExecuteEndHeroHint()
	{
		InformationManager.HideTooltip();
	}
}
