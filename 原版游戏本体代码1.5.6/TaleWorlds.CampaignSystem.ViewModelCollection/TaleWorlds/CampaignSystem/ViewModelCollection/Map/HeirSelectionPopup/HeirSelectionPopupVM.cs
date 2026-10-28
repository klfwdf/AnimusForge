using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.HeirSelectionPopup;

public class HeirSelectionPopupVM : ViewModel
{
	private string _titleText;

	private string _buttonOkLabel;

	private string _nameLabel;

	private string _ageLabel;

	private string _cultureLabel;

	private string _occupationLabel;

	private BannerImageIdentifierVM _clanBanner;

	private MBBindingList<HeirSelectionPopupHeroVM> _heirApparents;

	private HeirSelectionPopupHeroVM _currentSelectedHero;

	private bool _areHotkeysVisible;

	private InputKeyItemVM _doneInputKey;

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ButtonOkLabel
	{
		get
		{
			return _buttonOkLabel;
		}
		set
		{
			if (value != _buttonOkLabel)
			{
				_buttonOkLabel = value;
				OnPropertyChangedWithValue(value, "ButtonOkLabel");
			}
		}
	}

	[DataSourceProperty]
	public string NameLabel
	{
		get
		{
			return _nameLabel;
		}
		set
		{
			if (value != _nameLabel)
			{
				_nameLabel = value;
				OnPropertyChangedWithValue(value, "NameLabel");
			}
		}
	}

	[DataSourceProperty]
	public string AgeLabel
	{
		get
		{
			return _ageLabel;
		}
		set
		{
			if (value != _ageLabel)
			{
				_ageLabel = value;
				OnPropertyChangedWithValue(value, "AgeLabel");
			}
		}
	}

	[DataSourceProperty]
	public string CultureLabel
	{
		get
		{
			return _cultureLabel;
		}
		set
		{
			if (value != _cultureLabel)
			{
				_cultureLabel = value;
				OnPropertyChangedWithValue(value, "CultureLabel");
			}
		}
	}

	[DataSourceProperty]
	public string OccupationLabel
	{
		get
		{
			return _occupationLabel;
		}
		set
		{
			if (value != _occupationLabel)
			{
				_occupationLabel = value;
				OnPropertyChangedWithValue(value, "OccupationLabel");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeirSelectionPopupHeroVM> HeirApparents
	{
		get
		{
			return _heirApparents;
		}
		set
		{
			if (value != _heirApparents)
			{
				_heirApparents = value;
				OnPropertyChangedWithValue(value, "HeirApparents");
			}
		}
	}

	[DataSourceProperty]
	public HeirSelectionPopupHeroVM CurrentSelectedHero
	{
		get
		{
			return _currentSelectedHero;
		}
		set
		{
			if (value != _currentSelectedHero)
			{
				_currentSelectedHero = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedHero");
			}
		}
	}

	[DataSourceProperty]
	public bool AreHotkeysVisible
	{
		get
		{
			return _areHotkeysVisible;
		}
		set
		{
			if (value != _areHotkeysVisible)
			{
				_areHotkeysVisible = value;
				OnPropertyChangedWithValue(value, "AreHotkeysVisible");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public HeirSelectionPopupVM(Dictionary<Hero, int> heirApparents)
	{
		HeirApparents = new MBBindingList<HeirSelectionPopupHeroVM>();
		foreach (KeyValuePair<Hero, int> item in heirApparents.OrderByDescending((KeyValuePair<Hero, int> x) => x.Value))
		{
			HeirApparents.Add(new HeirSelectionPopupHeroVM(item.Key));
		}
		CurrentSelectedHero = HeirApparents[0];
		CurrentSelectedHero.IsSelected = true;
		ClanBanner = new BannerImageIdentifierVM(Clan.PlayerClan.Banner, nineGrid: true);
		RefreshValues();
	}

	public void Update()
	{
		for (int i = 0; i < HeirApparents.Count; i++)
		{
			if (HeirApparents[i].IsSelected && HeirApparents[i] != CurrentSelectedHero)
			{
				CurrentSelectedHero.IsSelected = false;
				CurrentSelectedHero = HeirApparents[i];
			}
		}
		AreHotkeysVisible = !InformationManager.IsAnyInquiryActive();
	}

	public void ExecuteSelectHeir()
	{
		TextObject textObject = GameTexts.FindText("str_STR1_space_STR2");
		TextObject textObject2 = new TextObject("{=GEvP9i5f}You will play on as {HEIR.NAME}.");
		textObject2.SetCharacterProperties("HEIR", CurrentSelectedHero.Hero.CharacterObject);
		textObject.SetTextVariable("STR1", textObject2);
		textObject.SetTextVariable("STR2", new TextObject("{=awjomtnJ}Are you sure?"));
		InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			ExecuteFinalizeHeirSelection(CurrentSelectedHero.Hero);
		}, null));
	}

	private void ExecuteFinalizeHeirSelection(Hero selectedHeir)
	{
		CampaignEventDispatcher.Instance.OnHeirSelectionOver(selectedHeir);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=2maftPJP}Assign As Clan & Faction Leader").ToString();
		ButtonOkLabel = new TextObject("{=KXQ7Mvec}Select As Main Character").ToString();
		NameLabel = GameTexts.FindText("str_LEFT_colon_wSpace").SetTextVariable("LEFT", GameTexts.FindText("str_name")).ToString();
		AgeLabel = GameTexts.FindText("str_LEFT_colon_wSpace").SetTextVariable("LEFT", GameTexts.FindText("str_age")).ToString();
		CultureLabel = GameTexts.FindText("str_LEFT_colon_wSpace").SetTextVariable("LEFT", GameTexts.FindText("str_culture")).ToString();
		OccupationLabel = GameTexts.FindText("str_LEFT_colon_wSpace").SetTextVariable("LEFT", GameTexts.FindText("str_occupation")).ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		for (int i = 0; i < HeirApparents.Count; i++)
		{
			HeirApparents[i].OnFinalize();
		}
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
