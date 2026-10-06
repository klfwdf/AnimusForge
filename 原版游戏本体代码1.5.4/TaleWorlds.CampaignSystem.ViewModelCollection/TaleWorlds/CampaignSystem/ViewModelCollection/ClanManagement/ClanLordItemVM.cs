using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanLordItemVM : ViewModel
{
	private readonly Action<ClanLordItemVM> _onCharacterSelect;

	private readonly Action _onRecall;

	private readonly Action _onTalk;

	private readonly Hero _hero;

	private readonly Action<Hero> _showHeroOnMap;

	private readonly ITeleportationCampaignBehavior _teleportationBehavior;

	private readonly TextObject _prisonerOfText = new TextObject("{=a8nRxITn}Prisoner of {PARTY_NAME}");

	private readonly TextObject _showLocationOfHeroOnMap = new TextObject("{=aGJYQOef}Show hero's location on map.");

	private readonly TextObject _recallHeroToMainPartyHintText = new TextObject("{=ANV8UV5f}Recall this member to your party.");

	private readonly TextObject _talkToHeroHintText = new TextObject("{=j4BdjLYp}Start a conversation with this clan member.");

	private CharacterImageIdentifierVM _visual;

	private BannerImageIdentifierVM _banner_9;

	private bool _isSelected;

	private bool _isChild;

	private bool _isMainHero;

	private bool _isFamilyMember;

	private bool _isPregnant;

	private bool _isTeleporting;

	private bool _isRecallVisible;

	private bool _isRecallEnabled;

	private bool _isTalkVisible;

	private bool _isTalkEnabled;

	private bool _canShowLocationOfHero;

	private string _name;

	private string _locationText;

	private string _relationToMainHeroText;

	private string _governorOfText;

	private string _currentActionText;

	private HeroViewModel _heroModel;

	private MBBindingList<EncyclopediaSkillVM> _skills;

	private MBBindingList<EncyclopediaTraitItemVM> _traits;

	private HintViewModel _pregnantHint;

	private HintViewModel _showOnMapHint;

	private HintViewModel _recallHint;

	private HintViewModel _talkHint;

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSkillVM> Skills
	{
		get
		{
			return _skills;
		}
		set
		{
			if (value != _skills)
			{
				_skills = value;
				OnPropertyChangedWithValue(value, "Skills");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaTraitItemVM> Traits
	{
		get
		{
			return _traits;
		}
		set
		{
			if (value != _traits)
			{
				_traits = value;
				OnPropertyChangedWithValue(value, "Traits");
			}
		}
	}

	[DataSourceProperty]
	public HeroViewModel HeroModel
	{
		get
		{
			return _heroModel;
		}
		set
		{
			if (value != _heroModel)
			{
				_heroModel = value;
				OnPropertyChangedWithValue(value, "HeroModel");
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
	public bool IsChild
	{
		get
		{
			return _isChild;
		}
		set
		{
			if (value != _isChild)
			{
				_isChild = value;
				OnPropertyChangedWithValue(value, "IsChild");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTeleporting
	{
		get
		{
			return _isTeleporting;
		}
		set
		{
			if (value != _isTeleporting)
			{
				_isTeleporting = value;
				OnPropertyChangedWithValue(value, "IsTeleporting");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRecallVisible
	{
		get
		{
			return _isRecallVisible;
		}
		set
		{
			if (value != _isRecallVisible)
			{
				_isRecallVisible = value;
				OnPropertyChangedWithValue(value, "IsRecallVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRecallEnabled
	{
		get
		{
			return _isRecallEnabled;
		}
		set
		{
			if (value != _isRecallEnabled)
			{
				_isRecallEnabled = value;
				OnPropertyChangedWithValue(value, "IsRecallEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTalkVisible
	{
		get
		{
			return _isTalkVisible;
		}
		set
		{
			if (value != _isTalkVisible)
			{
				_isTalkVisible = value;
				OnPropertyChangedWithValue(value, "IsTalkVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTalkEnabled
	{
		get
		{
			return _isTalkEnabled;
		}
		set
		{
			if (value != _isTalkEnabled)
			{
				_isTalkEnabled = value;
				OnPropertyChangedWithValue(value, "IsTalkEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool CanShowLocationOfHero
	{
		get
		{
			return _canShowLocationOfHero;
		}
		set
		{
			if (value != _canShowLocationOfHero)
			{
				_canShowLocationOfHero = value;
				OnPropertyChangedWithValue(value, "CanShowLocationOfHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChangedWithValue(value, "IsMainHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFamilyMember
	{
		get
		{
			return _isFamilyMember;
		}
		set
		{
			if (value != _isFamilyMember)
			{
				_isFamilyMember = value;
				OnPropertyChangedWithValue(value, "IsFamilyMember");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPregnant
	{
		get
		{
			return _isPregnant;
		}
		set
		{
			if (value != _isPregnant)
			{
				_isPregnant = value;
				OnPropertyChangedWithValue(value, "IsPregnant");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner_9
	{
		get
		{
			return _banner_9;
		}
		set
		{
			if (value != _banner_9)
			{
				_banner_9 = value;
				OnPropertyChangedWithValue(value, "Banner_9");
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
	public string CurrentActionText
	{
		get
		{
			return _currentActionText;
		}
		set
		{
			if (value != _currentActionText)
			{
				_currentActionText = value;
				OnPropertyChangedWithValue(value, "CurrentActionText");
			}
		}
	}

	[DataSourceProperty]
	public string RelationToMainHeroText
	{
		get
		{
			return _relationToMainHeroText;
		}
		set
		{
			if (value != _relationToMainHeroText)
			{
				_relationToMainHeroText = value;
				OnPropertyChangedWithValue(value, "RelationToMainHeroText");
			}
		}
	}

	[DataSourceProperty]
	public string GovernorOfText
	{
		get
		{
			return _governorOfText;
		}
		set
		{
			if (value != _governorOfText)
			{
				_governorOfText = value;
				OnPropertyChangedWithValue(value, "GovernorOfText");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PregnantHint
	{
		get
		{
			return _pregnantHint;
		}
		set
		{
			if (value != _pregnantHint)
			{
				_pregnantHint = value;
				OnPropertyChangedWithValue(value, "PregnantHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ShowOnMapHint
	{
		get
		{
			return _showOnMapHint;
		}
		set
		{
			if (value != _showOnMapHint)
			{
				_showOnMapHint = value;
				OnPropertyChangedWithValue(value, "ShowOnMapHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RecallHint
	{
		get
		{
			return _recallHint;
		}
		set
		{
			if (value != _recallHint)
			{
				_recallHint = value;
				OnPropertyChangedWithValue(value, "RecallHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TalkHint
	{
		get
		{
			return _talkHint;
		}
		set
		{
			if (value != _talkHint)
			{
				_talkHint = value;
				OnPropertyChangedWithValue(value, "TalkHint");
			}
		}
	}

	public ClanLordItemVM(Hero hero, ITeleportationCampaignBehavior teleportationBehavior, Action<Hero> showHeroOnMap, Action<ClanLordItemVM> onCharacterSelect, Action onRecall, Action onTalk)
	{
		_hero = hero;
		_onCharacterSelect = onCharacterSelect;
		_onRecall = onRecall;
		_onTalk = onTalk;
		_showHeroOnMap = showHeroOnMap;
		_teleportationBehavior = teleportationBehavior;
		CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject);
		Visual = new CharacterImageIdentifierVM(characterCode);
		Skills = new MBBindingList<EncyclopediaSkillVM>();
		Traits = new MBBindingList<EncyclopediaTraitItemVM>();
		IsFamilyMember = Hero.MainHero.Clan.AliveLords.Contains(_hero);
		Banner_9 = new BannerImageIdentifierVM(hero.ClanBanner, nineGrid: true);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _hero.Name.ToString();
		StringHelpers.SetCharacterProperties("NPC", _hero.CharacterObject);
		CurrentActionText = ((_hero != Hero.MainHero) ? CampaignUIHelper.GetHeroBehaviorText(_hero, _teleportationBehavior) : "");
		LocationText = CurrentActionText;
		PregnantHint = new HintViewModel(GameTexts.FindText("str_pregnant"));
		UpdateProperties();
	}

	public void ExecuteLocationLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public void UpdateProperties()
	{
		RelationToMainHeroText = "";
		GovernorOfText = "";
		Skills.Clear();
		Traits.Clear();
		IsMainHero = _hero == Hero.MainHero;
		IsPregnant = _hero.IsPregnant;
		List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList();
		list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject item in list)
		{
			Skills.Add(new EncyclopediaSkillVM(item, _hero.GetSkillValue(item)));
		}
		foreach (TraitObject heroTrait in CampaignUIHelper.GetHeroTraits())
		{
			if (_hero.GetTraitLevel(heroTrait) != 0)
			{
				Traits.Add(new EncyclopediaTraitItemVM(heroTrait, _hero));
			}
		}
		IsChild = FaceGen.GetMaturityTypeWithAge(_hero.Age) <= BodyMeshMaturityType.Child;
		if (_hero != Hero.MainHero)
		{
			RelationToMainHeroText = CampaignUIHelper.GetHeroRelationToHeroText(_hero, Hero.MainHero, uppercaseFirst: true).ToString();
		}
		if (_hero.GovernorOf != null)
		{
			GameTexts.SetVariable("SETTLEMENT_NAME", _hero.GovernorOf.Owner.Settlement.EncyclopediaLinkWithName);
			GovernorOfText = GameTexts.FindText("str_governor_of_label").ToString();
		}
		HeroModel = new HeroViewModel();
		HeroModel.FillFrom(_hero);
		Banner_9 = new BannerImageIdentifierVM(_hero.ClanBanner, nineGrid: true);
		bool flag = MobileParty.MainParty.CurrentSettlement == null || MobileParty.MainParty.CurrentSettlement == _hero.CurrentSettlement;
		CanShowLocationOfHero = _hero.GetCampaignPosition().IsValid() && _hero.PartyBelongedTo != MobileParty.MainParty && flag;
		ShowOnMapHint = new HintViewModel(CanShowLocationOfHero ? _showLocationOfHeroOnMap : TextObject.GetEmpty());
		TextObject disabledReason = TextObject.GetEmpty();
		bool flag2 = _hero.PartyBelongedTo == MobileParty.MainParty;
		IsTalkVisible = flag2 && !IsMainHero;
		IsTalkEnabled = IsTalkVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason);
		IsTeleporting = _teleportationBehavior.GetTargetOfTeleportingHero(_hero, out var _, out var _, out var _);
		TextObject disabledReason2 = TextObject.GetEmpty();
		IsRecallVisible = !IsMainHero && !flag2 && !IsTeleporting;
		IsRecallEnabled = IsRecallVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason2) && FactionHelper.IsMainClanMemberAvailableForRecall(_hero, MobileParty.MainParty, out disabledReason2);
		RecallHint = new HintViewModel(IsRecallEnabled ? _recallHeroToMainPartyHintText : disabledReason2);
		TalkHint = new HintViewModel(IsTalkEnabled ? _talkToHeroHintText : disabledReason);
	}

	public void ExecuteLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(_hero.EncyclopediaLink);
	}

	public void OnCharacterSelect()
	{
		_onCharacterSelect(this);
	}

	public virtual void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Hero), _hero, true);
	}

	public virtual void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public Hero GetHero()
	{
		return _hero;
	}

	public void ExecuteRename()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=2lFwF07j}Change Name").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), OnNamingHeroOver, null, shouldInputBeObfuscated: false, CampaignUIHelper.IsStringApplicableForHeroName));
	}

	private void OnNamingHeroOver(string suggestedName)
	{
		if (CampaignUIHelper.IsStringApplicableForHeroName(suggestedName).Item1)
		{
			TextObject textObject = GameTexts.FindText("str_generic_character_firstname");
			textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(suggestedName));
			TextObject textObject2 = GameTexts.FindText("str_generic_character_name");
			textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(suggestedName));
			textObject2.SetTextVariable("CHARACTER_GENDER", _hero.IsFemale ? 1 : 0);
			textObject.SetTextVariable("CHARACTER_GENDER", _hero.IsFemale ? 1 : 0);
			_hero.SetName(textObject2, textObject);
			Name = suggestedName;
			if (_hero.PartyBelongedTo?.Army != null && _hero.PartyBelongedTo.Army.LeaderParty.Owner == _hero)
			{
				_hero.PartyBelongedTo.Army.UpdateName();
			}
		}
		else
		{
			Debug.FailedAssert("Suggested name is not acceptable. This shouldn't happen", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanLordItemVM.cs", "OnNamingHeroOver", 203);
		}
	}

	public void ExecuteShowOnMap()
	{
		if (_hero != null && CanShowLocationOfHero)
		{
			_showHeroOnMap(_hero);
		}
	}

	public void ExecuteRecall()
	{
		_onRecall?.Invoke();
	}

	public void ExecuteTalk()
	{
		_onTalk?.Invoke();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		HeroModel.OnFinalize();
	}
}
