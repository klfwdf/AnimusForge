using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters;

public class ClanSupporterGroupVM : ViewModel
{
	private TextObject _groupNameText;

	private float _influenceBonus;

	private Action<ClanSupporterGroupVM> _onSelection;

	private string _titleText;

	private string _influenceBonusDescription;

	private string _name;

	private string _totalInfluence;

	private bool _isSelected;

	private MBBindingList<ClanSupporterItemVM> _supporters;

	private float _totalInfluenceBonus;

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
	public float TotalInfluenceBonus
	{
		get
		{
			return _totalInfluenceBonus;
		}
		private set
		{
			if (value != _totalInfluenceBonus)
			{
				_totalInfluenceBonus = value;
				OnPropertyChangedWithValue(value, "TotalInfluenceBonus");
			}
		}
	}

	[DataSourceProperty]
	public string InfluenceBonusDescription
	{
		get
		{
			return _influenceBonusDescription;
		}
		set
		{
			if (value != _influenceBonusDescription)
			{
				_influenceBonusDescription = value;
				OnPropertyChangedWithValue(value, "InfluenceBonusDescription");
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
	public string TotalInfluence
	{
		get
		{
			return _totalInfluence;
		}
		set
		{
			if (value != _totalInfluence)
			{
				_totalInfluence = value;
				OnPropertyChangedWithValue(value, "TotalInfluence");
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
	public MBBindingList<ClanSupporterItemVM> Supporters
	{
		get
		{
			return _supporters;
		}
		set
		{
			if (value != _supporters)
			{
				_supporters = value;
				OnPropertyChangedWithValue(value, "Supporters");
			}
		}
	}

	public ClanSupporterGroupVM(TextObject groupName, float influenceBonus, Action<ClanSupporterGroupVM> onSelection)
	{
		_groupNameText = groupName;
		_influenceBonus = influenceBonus;
		_onSelection = onSelection;
		Supporters = new MBBindingList<ClanSupporterItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Refresh();
	}

	public void AddSupporter(Hero hero)
	{
		if (!Supporters.Any((ClanSupporterItemVM x) => x.Hero.Hero == hero))
		{
			Supporters.Add(new ClanSupporterItemVM(hero));
		}
	}

	public void Refresh()
	{
		TextObject textObject = GameTexts.FindText("str_amount_with_influence_icon");
		TotalInfluenceBonus = (float)Supporters.Count * _influenceBonus;
		TextObject textObject2 = GameTexts.FindText("str_plus_with_number");
		textObject2.SetTextVariable("NUMBER", TotalInfluenceBonus.ToString("F2"));
		textObject.SetTextVariable("AMOUNT", textObject2.ToString());
		textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
		TotalInfluence = textObject.ToString();
		TextObject textObject3 = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis");
		textObject3.SetTextVariable("RANK", _groupNameText.ToString());
		textObject3.SetTextVariable("NUMBER", Supporters.Count);
		Name = textObject3.ToString();
		TextObject textObject4 = new TextObject("{=cZCOa00c}{SUPPORTER_RANK} Supporters ({NUM})");
		textObject4.SetTextVariable("SUPPORTER_RANK", _groupNameText.ToString());
		textObject4.SetTextVariable("NUM", Supporters.Count);
		TitleText = textObject4.ToString();
		TextObject textObject5 = new TextObject("{=jdbT6nc9}Each {SUPPORTER_RANK} supporter provides {INFLUENCE_BONUS} per day.");
		textObject5.SetTextVariable("SUPPORTER_RANK", _groupNameText.ToString());
		textObject5.SetTextVariable("INFLUENCE_BONUS", _influenceBonus.ToString("F2") + "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
		InfluenceBonusDescription = textObject5.ToString();
	}

	public void ExecuteSelect()
	{
		_onSelection?.Invoke(this);
	}
}
