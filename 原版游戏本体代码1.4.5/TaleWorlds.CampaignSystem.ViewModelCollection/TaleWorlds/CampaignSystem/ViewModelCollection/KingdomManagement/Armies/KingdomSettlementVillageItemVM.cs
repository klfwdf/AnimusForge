using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;

public class KingdomSettlementVillageItemVM : ViewModel
{
	private Village _village;

	private ImageIdentifierVM _visual;

	private string _name;

	private string _visualPath;

	[DataSourceProperty]
	public ImageIdentifierVM Visual
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
	public string VisualPath
	{
		get
		{
			return _visualPath;
		}
		set
		{
			if (value != _visualPath)
			{
				_visualPath = value;
				OnPropertyChangedWithValue(value, "VisualPath");
			}
		}
	}

	public KingdomSettlementVillageItemVM(Village village)
	{
		_village = village;
		VisualPath = village.BackgroundMeshName + "_t";
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _village.Name.ToString();
	}

	private void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Settlement), _village.Settlement, true);
	}

	private void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteLink()
	{
		if (_village != null && _village.Settlement != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_village.Settlement.EncyclopediaLink);
		}
	}
}
