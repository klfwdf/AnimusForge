using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;

public class DecisionSupporterVM : ViewModel
{
	private Supporter.SupportWeights _weight;

	private Clan _clan;

	private TextObject _nameObj;

	private Hero _hero;

	private CharacterImageIdentifierVM _visual;

	private string _name;

	private int _supportStrength;

	private string _supportWeightImagePath;

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
	public int SupportStrength
	{
		get
		{
			return _supportStrength;
		}
		set
		{
			if (value != _supportStrength)
			{
				_supportStrength = value;
				OnPropertyChangedWithValue(value, "SupportStrength");
			}
		}
	}

	[DataSourceProperty]
	public string SupportWeightImagePath
	{
		get
		{
			return _supportWeightImagePath;
		}
		set
		{
			if (value != _supportWeightImagePath)
			{
				_supportWeightImagePath = value;
				OnPropertyChangedWithValue(value, "SupportWeightImagePath");
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
				OnPropertyChanged("string");
			}
		}
	}

	public DecisionSupporterVM(TextObject name, string imagePath, Clan clan, Supporter.SupportWeights weight)
	{
		_nameObj = name;
		_clan = clan;
		_weight = weight;
		SupportWeightImagePath = GetSupporterWeightImagePath(weight);
		RefreshValues();
		_hero = Hero.FindFirst((Hero H) => H.Name == name);
		if (_hero != null)
		{
			Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(_hero.CharacterObject));
		}
		else
		{
			Visual = new CharacterImageIdentifierVM(null);
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameObj.ToString();
	}

	private void ExecuteBeginHint()
	{
		if (_hero != null)
		{
			InformationManager.ShowTooltip(typeof(Hero), _hero, false);
		}
	}

	private void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	internal static string GetSupporterWeightImagePath(Supporter.SupportWeights weight)
	{
		return weight switch
		{
			Supporter.SupportWeights.SlightlyFavor => "SPKingdom\\voter_strength1", 
			Supporter.SupportWeights.StronglyFavor => "SPKingdom\\voter_strength2", 
			Supporter.SupportWeights.FullyPush => "SPKingdom\\voter_strength3", 
			_ => string.Empty, 
		};
	}
}
