using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanLordStatusItemVM : ViewModel
{
	public enum LordStatus
	{
		Dead,
		Married,
		Pregnant,
		InBattle,
		InSiege,
		Child,
		Prisoner,
		Sick
	}

	private int _type = -1;

	private HintViewModel _hint;

	[DataSourceProperty]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
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

	public ClanLordStatusItemVM(LordStatus status, TextObject hintText)
	{
		Type = (int)status;
		Hint = new HintViewModel(hintText);
	}
}
