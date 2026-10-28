using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;

public class MarriageOfferPopupVM : ViewModel
{
	private readonly IMarriageOfferCampaignBehavior _marriageBehavior;

	private Action _onClose;

	private string _titleText;

	private string _clanText;

	private string _ageText;

	private string _occupationText;

	private string _relationText;

	private string _consequencesText;

	private MBBindingList<BindingListStringItem> _consequencesList;

	private string _buttonOkLabel;

	private string _buttonCancelLabel;

	private bool _isEncyclopediaOpen;

	private MarriageOfferPopupHeroVM _offereeClanMember;

	private MarriageOfferPopupHeroVM _offererClanMember;

	private InputKeyItemVM _cancelInputKey;

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
	public string ClanText
	{
		get
		{
			return _clanText;
		}
		set
		{
			if (value != _clanText)
			{
				_clanText = value;
				OnPropertyChangedWithValue(value, "ClanText");
			}
		}
	}

	[DataSourceProperty]
	public string AgeText
	{
		get
		{
			return _ageText;
		}
		set
		{
			if (value != _ageText)
			{
				_ageText = value;
				OnPropertyChangedWithValue(value, "AgeText");
			}
		}
	}

	[DataSourceProperty]
	public string OccupationText
	{
		get
		{
			return _occupationText;
		}
		set
		{
			if (value != _occupationText)
			{
				_occupationText = value;
				OnPropertyChangedWithValue(value, "OccupationText");
			}
		}
	}

	[DataSourceProperty]
	public string RelationText
	{
		get
		{
			return _relationText;
		}
		set
		{
			if (value != _relationText)
			{
				_relationText = value;
				OnPropertyChangedWithValue(value, "RelationText");
			}
		}
	}

	[DataSourceProperty]
	public string ConsequencesText
	{
		get
		{
			return _consequencesText;
		}
		set
		{
			if (value != _consequencesText)
			{
				_consequencesText = value;
				OnPropertyChangedWithValue(value, "ConsequencesText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BindingListStringItem> ConsequencesList
	{
		get
		{
			return _consequencesList;
		}
		set
		{
			if (value != _consequencesList)
			{
				_consequencesList = value;
				OnPropertyChangedWithValue(value, "ConsequencesList");
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
	public string ButtonCancelLabel
	{
		get
		{
			return _buttonCancelLabel;
		}
		set
		{
			if (value != _buttonCancelLabel)
			{
				_buttonCancelLabel = value;
				OnPropertyChangedWithValue(value, "ButtonCancelLabel");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEncyclopediaOpen
	{
		get
		{
			return _isEncyclopediaOpen;
		}
		set
		{
			if (value != _isEncyclopediaOpen)
			{
				_isEncyclopediaOpen = value;
				OnPropertyChangedWithValue(value, "IsEncyclopediaOpen");
			}
		}
	}

	[DataSourceProperty]
	public MarriageOfferPopupHeroVM OffereeClanMember
	{
		get
		{
			return _offereeClanMember;
		}
		set
		{
			if (value != _offereeClanMember)
			{
				_offereeClanMember = value;
				OnPropertyChangedWithValue(value, "OffereeClanMember");
			}
		}
	}

	[DataSourceProperty]
	public MarriageOfferPopupHeroVM OffererClanMember
	{
		get
		{
			return _offererClanMember;
		}
		set
		{
			if (value != _offererClanMember)
			{
				_offererClanMember = value;
				OnPropertyChangedWithValue(value, "OffererClanMember");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
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

	public MarriageOfferPopupVM(Hero suitor, Hero maiden, Action onClose)
	{
		_marriageBehavior = Campaign.Current.GetCampaignBehavior<IMarriageOfferCampaignBehavior>();
		_onClose = onClose;
		if (suitor.Clan == Clan.PlayerClan)
		{
			OffereeClanMember = new MarriageOfferPopupHeroVM(suitor);
			OffererClanMember = new MarriageOfferPopupHeroVM(maiden);
		}
		else
		{
			OffereeClanMember = new MarriageOfferPopupHeroVM(maiden);
			OffererClanMember = new MarriageOfferPopupHeroVM(suitor);
		}
		ConsequencesList = new MBBindingList<BindingListStringItem>();
		RefreshValues();
	}

	public void Update()
	{
		OffereeClanMember?.Update();
		OffererClanMember?.Update();
	}

	public void ExecuteAcceptOffer()
	{
		_marriageBehavior?.OnMarriageOfferAcceptedOnPopUp();
		_onClose?.Invoke();
	}

	public void ExecuteDeclineOffer()
	{
		_marriageBehavior?.OnMarriageOfferDeclinedOnPopUp();
		_onClose?.Invoke();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TextObject textObject = GameTexts.FindText("str_marriage_offer_from_clan");
		textObject.SetTextVariable("CLAN_NAME", OffererClanMember.Hero.Clan.Name);
		TitleText = textObject.ToString();
		ClanText = GameTexts.FindText("str_clan").ToString();
		AgeText = new TextObject("{=jaaQijQs}Age").ToString();
		OccupationText = new TextObject("{=GZxFIeiJ}Occupation").ToString();
		RelationText = new TextObject("{=BlidMNGT}Relation").ToString();
		ConsequencesText = new TextObject("{=Lm6Mkhru}Consequences").ToString();
		ButtonOkLabel = new TextObject("{=Y94H6XnK}Accept").ToString();
		ButtonCancelLabel = new TextObject("{=cOgmdp9e}Decline").ToString();
		ConsequencesList.Clear();
		foreach (TextObject item in _marriageBehavior?.GetMarriageAcceptedConsequences() ?? new MBBindingList<TextObject>())
		{
			ConsequencesList.Add(new BindingListStringItem("- " + item.ToString()));
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
		OffereeClanMember?.OnFinalize();
		OffererClanMember?.OnFinalize();
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
