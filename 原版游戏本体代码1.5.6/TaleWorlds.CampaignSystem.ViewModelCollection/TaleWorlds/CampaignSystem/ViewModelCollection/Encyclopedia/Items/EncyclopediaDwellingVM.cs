using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaDwellingVM : ViewModel
{
	private readonly WorkshopType _workshop;

	private readonly VillageType _villageType;

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

	public EncyclopediaDwellingVM(WorkshopType workshop)
	{
		_workshop = workshop;
		FileName = workshop.StringId;
		RefreshValues();
	}

	public EncyclopediaDwellingVM(VillageType villageType)
	{
		_villageType = villageType;
		FileName = villageType.StringId;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_workshop != null)
		{
			NameText = _workshop.Name.ToString();
		}
		else if (_villageType != null)
		{
			NameText = _villageType.ShortName.ToString();
		}
	}
}
