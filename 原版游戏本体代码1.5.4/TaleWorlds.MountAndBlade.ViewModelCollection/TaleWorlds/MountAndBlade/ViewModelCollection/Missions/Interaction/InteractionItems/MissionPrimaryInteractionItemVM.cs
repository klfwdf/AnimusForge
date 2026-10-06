using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;

public class MissionPrimaryInteractionItemVM : MissionGenericInteractionItemVM
{
	private string _focusTypeString;

	[DataSourceProperty]
	public string FocusTypeString
	{
		get
		{
			return _focusTypeString;
		}
		set
		{
			if (value != _focusTypeString)
			{
				_focusTypeString = value;
				OnPropertyChangedWithValue(value, "FocusTypeString");
			}
		}
	}

	protected override void OnResetData()
	{
		base.OnResetData();
		FocusTypeString = string.Empty;
	}
}
