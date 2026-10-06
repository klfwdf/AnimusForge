using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaSettlementVM : ViewModel
{
	private Settlement _settlement;

	private string _fileName;

	private string _nameText;

	[DataSourceProperty]
	public string FileName
	{
		get
		{
			return _fileName;
		}
		set
		{
			if (value != _fileName)
			{
				_fileName = value;
				OnPropertyChangedWithValue(value, "FileName");
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

	public EncyclopediaSettlementVM(Settlement settlement)
	{
		if (!settlement.IsHideout)
		{
			_settlement = settlement;
		}
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		FileName = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = _settlement?.Name.ToString() ?? "";
	}

	public void ExecuteLink()
	{
		if (_settlement != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_settlement.EncyclopediaLink);
		}
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Settlement), _settlement);
	}
}
