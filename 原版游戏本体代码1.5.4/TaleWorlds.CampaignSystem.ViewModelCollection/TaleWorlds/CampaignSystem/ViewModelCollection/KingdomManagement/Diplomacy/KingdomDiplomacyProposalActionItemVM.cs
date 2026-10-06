using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;

public class KingdomDiplomacyProposalActionItemVM : ViewModel
{
	private readonly TextObject _nameText;

	private readonly TextObject _explanationText;

	private readonly Action _action;

	private string _name;

	private string _explanation;

	private bool _isEnabled;

	private int _influenceCost;

	private HintViewModel _hint;

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
	public string Explanation
	{
		get
		{
			return _explanation;
		}
		set
		{
			if (value != _explanation)
			{
				_explanation = value;
				OnPropertyChangedWithValue(value, "Explanation");
			}
		}
	}

	[DataSourceProperty]
	public int InfluenceCost
	{
		get
		{
			return _influenceCost;
		}
		set
		{
			if (value != _influenceCost)
			{
				_influenceCost = value;
				OnPropertyChangedWithValue(value, "InfluenceCost");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	public KingdomDiplomacyProposalActionItemVM(TextObject nameText, TextObject explanationText, int influenceCost, bool isEnabled, TextObject hintText, Action action)
	{
		_nameText = nameText;
		_explanationText = explanationText;
		_action = action;
		InfluenceCost = influenceCost;
		IsEnabled = isEnabled;
		Hint = new HintViewModel(hintText);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameText.ToString();
		Explanation = _explanationText.ToString();
	}

	public void ExecuteAction()
	{
		_action?.Invoke();
	}
}
