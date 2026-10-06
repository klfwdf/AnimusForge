using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanMembersVM : ViewModel
{
	private readonly Clan _faction;

	private readonly Action _onRefresh;

	private readonly Action<Hero> _showHeroOnMap;

	private readonly ITeleportationCampaignBehavior _teleportationBehavior;

	private bool _isSelected;

	private MBBindingList<ClanLordItemVM> _companions;

	private MBBindingList<ClanLordItemVM> _family;

	private ClanLordItemVM _currentSelectedMember;

	private string _familyText;

	private string _traitsText;

	private string _companionsText;

	private string _skillsText;

	private string _nameText;

	private string _locationText;

	private bool _isAnyValidMemberSelected;

	private ClanMembersSortControllerVM _sortController;

	[DataSourceProperty]
	public bool IsAnyValidMemberSelected
	{
		get
		{
			return _isAnyValidMemberSelected;
		}
		set
		{
			if (value != _isAnyValidMemberSelected)
			{
				_isAnyValidMemberSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyValidMemberSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public string FamilyText
	{
		get
		{
			return _familyText;
		}
		set
		{
			if (value != _familyText)
			{
				_familyText = value;
				OnPropertyChangedWithValue(value, "FamilyText");
			}
		}
	}

	[DataSourceProperty]
	public string TraitsText
	{
		get
		{
			return _traitsText;
		}
		set
		{
			if (value != _traitsText)
			{
				_traitsText = value;
				OnPropertyChangedWithValue(value, "TraitsText");
			}
		}
	}

	[DataSourceProperty]
	public string SkillsText
	{
		get
		{
			return _skillsText;
		}
		set
		{
			if (value != _skillsText)
			{
				_skillsText = value;
				OnPropertyChangedWithValue(value, "SkillsText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string LocationText
	{
		get
		{
			return _locationText;
		}
		set
		{
			if (value != _locationText)
			{
				_locationText = value;
				OnPropertyChangedWithValue(value, "LocationText");
			}
		}
	}

	[DataSourceProperty]
	public string CompanionsText
	{
		get
		{
			return _companionsText;
		}
		set
		{
			if (value != _companionsText)
			{
				_companionsText = value;
				OnPropertyChangedWithValue(value, "CompanionsText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanLordItemVM> Companions
	{
		get
		{
			return _companions;
		}
		set
		{
			if (value != _companions)
			{
				_companions = value;
				OnPropertyChangedWithValue(value, "Companions");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanLordItemVM> Family
	{
		get
		{
			return _family;
		}
		set
		{
			if (value != _family)
			{
				_family = value;
				OnPropertyChangedWithValue(value, "Family");
			}
		}
	}

	[DataSourceProperty]
	public ClanLordItemVM CurrentSelectedMember
	{
		get
		{
			return _currentSelectedMember;
		}
		set
		{
			if (value != _currentSelectedMember)
			{
				_currentSelectedMember = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedMember");
				IsAnyValidMemberSelected = value != null;
			}
		}
	}

	[DataSourceProperty]
	public ClanMembersSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	public ClanMembersVM(Action onRefresh, Action<Hero> showHeroOnMap)
	{
		_onRefresh = onRefresh;
		_faction = Hero.MainHero.Clan;
		_showHeroOnMap = showHeroOnMap;
		_teleportationBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
		Family = new MBBindingList<ClanLordItemVM>();
		Companions = new MBBindingList<ClanLordItemVM>();
		MBBindingList<MBBindingList<ClanLordItemVM>> listsToControl = new MBBindingList<MBBindingList<ClanLordItemVM>> { Family, Companions };
		SortController = new ClanMembersSortControllerVM(listsToControl);
		RefreshMembersList();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TraitsText = GameTexts.FindText("str_traits_group").ToString();
		SkillsText = GameTexts.FindText("str_skills").ToString();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		LocationText = GameTexts.FindText("str_tooltip_label_location").ToString();
		Family.ApplyActionOnAllItems(delegate(ClanLordItemVM x)
		{
			x.RefreshValues();
		});
		Companions.ApplyActionOnAllItems(delegate(ClanLordItemVM x)
		{
			x.RefreshValues();
		});
		SortController.RefreshValues();
	}

	public void RefreshMembersList()
	{
		Family.Clear();
		Companions.Clear();
		SortController.ResetAllStates();
		List<Hero> list = new List<Hero>();
		foreach (Hero aliveLord in _faction.AliveLords)
		{
			if (!aliveLord.IsDisabled)
			{
				if (aliveLord == Hero.MainHero)
				{
					list.Insert(0, aliveLord);
				}
				else
				{
					list.Add(aliveLord);
				}
			}
		}
		IEnumerable<Hero> enumerable = _faction.Companions.Where((Hero m) => m.IsPlayerCompanion);
		foreach (Hero item in list)
		{
			Family.Add(new ClanLordItemVM(item, _teleportationBehavior, _showHeroOnMap, OnMemberSelection, OnRequestRecall, OnTalkWithMember));
		}
		foreach (Hero item2 in enumerable)
		{
			Companions.Add(new ClanLordItemVM(item2, _teleportationBehavior, _showHeroOnMap, OnMemberSelection, OnRequestRecall, OnTalkWithMember));
		}
		GameTexts.SetVariable("RANK", GameTexts.FindText("str_family_group"));
		GameTexts.SetVariable("NUMBER", Family.Count);
		FamilyText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_companions_group"));
		GameTexts.SetVariable("LEFT", _faction.Companions.Count);
		GameTexts.SetVariable("RIGHT", _faction.CompanionLimit);
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis"));
		CompanionsText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		OnMemberSelection(GetDefaultMember());
	}

	private ClanLordItemVM GetDefaultMember()
	{
		if (Family.Count > 0)
		{
			return Family[0];
		}
		if (Companions.Count <= 0)
		{
			return null;
		}
		return Companions[0];
	}

	public void SelectMember(Hero hero)
	{
		bool flag = false;
		foreach (ClanLordItemVM item in Family)
		{
			if (item.GetHero() == hero)
			{
				OnMemberSelection(item);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			foreach (ClanLordItemVM companion in Companions)
			{
				if (companion.GetHero() == hero)
				{
					OnMemberSelection(companion);
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			return;
		}
		foreach (ClanLordItemVM item2 in Family)
		{
			if (item2.GetHero() == Hero.MainHero)
			{
				OnMemberSelection(item2);
				flag = true;
				break;
			}
		}
	}

	private void OnMemberSelection(ClanLordItemVM member)
	{
		if (CurrentSelectedMember != null)
		{
			CurrentSelectedMember.IsSelected = false;
		}
		CurrentSelectedMember = member;
		if (member != null)
		{
			member.IsSelected = true;
		}
	}

	private void OnRequestRecall()
	{
		Hero hero = CurrentSelectedMember?.GetHero();
		if (hero != null)
		{
			int hours = (int)Math.Ceiling(Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, PartyBase.MainParty).ResultNumber);
			MBTextManager.SetTextVariable("TRAVEL_DURATION", CampaignUIHelper.GetHoursAndDaysTextFromHourValue(hours).ToString());
			MBTextManager.SetTextVariable("HERO_NAME", hero.Name.ToString());
			TextObject textObject = GameTexts.FindText("str_recall_member");
			InformationManager.ShowInquiry(new InquiryData(text: GameTexts.FindText("str_recall_clan_member_inquiry").ToString(), titleText: textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, affirmativeText: GameTexts.FindText("str_yes").ToString(), negativeText: GameTexts.FindText("str_no").ToString(), affirmativeAction: OnConfirmRecall, negativeAction: null));
		}
	}

	private void OnConfirmRecall()
	{
		TeleportHeroAction.ApplyDelayedTeleportToParty(CurrentSelectedMember.GetHero(), MobileParty.MainParty);
		_onRefresh?.Invoke();
	}

	private void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	private void OnTalkWithMember()
	{
		if (CurrentSelectedMember?.GetHero()?.CharacterObject != null)
		{
			CharacterObject characterObject = CurrentSelectedMember.GetHero().CharacterObject;
			if (LocationComplex.Current?.GetLocationOfCharacter(LocationComplex.Current.GetFirstLocationCharacterOfCharacter(characterObject)) == null)
			{
				CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(characterObject, PartyBase.MainParty));
				return;
			}
			Game.Current.GameStateManager.PopState();
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(characterObject, PartyBase.MainParty));
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Family.ApplyActionOnAllItems(delegate(ClanLordItemVM f)
		{
			f.OnFinalize();
		});
		Companions.ApplyActionOnAllItems(delegate(ClanLordItemVM f)
		{
			f.OnFinalize();
		});
	}
}
