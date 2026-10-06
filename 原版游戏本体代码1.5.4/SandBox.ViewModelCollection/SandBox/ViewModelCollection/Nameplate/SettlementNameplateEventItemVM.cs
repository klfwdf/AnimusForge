using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate;

public class SettlementNameplateEventItemVM : ViewModel
{
	public enum SettlementEventType
	{
		Tournament,
		AvailableIssue,
		ActiveQuest,
		ActiveStoryQuest,
		TrackedIssue,
		TrackedStoryQuest,
		Production
	}

	public readonly SettlementEventType EventType;

	private int _type;

	private string _additionalParameters;

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
	public string AdditionalParameters
	{
		get
		{
			return _additionalParameters;
		}
		set
		{
			if (value != _additionalParameters)
			{
				_additionalParameters = value;
				OnPropertyChangedWithValue(value, "AdditionalParameters");
			}
		}
	}

	public SettlementNameplateEventItemVM(SettlementEventType eventType)
	{
		EventType = eventType;
		Type = (int)eventType;
		AdditionalParameters = "";
	}

	public SettlementNameplateEventItemVM(string productionIconId = "")
	{
		EventType = SettlementEventType.Production;
		Type = (int)EventType;
		AdditionalParameters = productionIconId;
	}
}
