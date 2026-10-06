using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;

public class KingdomClanFiefItemVM : ViewModel
{
	private readonly Settlement Settlement;

	private string _visualPath;

	private string _fiefName;

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
				OnPropertyChanged("FileName");
			}
		}
	}

	[DataSourceProperty]
	public string FiefName
	{
		get
		{
			return _fiefName;
		}
		set
		{
			if (value != _fiefName)
			{
				_fiefName = value;
				OnPropertyChangedWithValue(value, "FiefName");
			}
		}
	}

	public KingdomClanFiefItemVM(Settlement settlement)
	{
		Settlement = settlement;
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		VisualPath = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		FiefName = Settlement.Name.ToString();
	}

	private void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Settlement), Settlement, true);
	}

	private void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteLink()
	{
		if (Settlement != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Settlement.EncyclopediaLink);
		}
	}
}
