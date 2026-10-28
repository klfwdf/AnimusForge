using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;

public class MissionGenericInteractionItemVM : MissionInteractionItemBaseVM
{
	private TextObject _messageTextObj;

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Message = _messageTextObj?.ToString();
	}

	public void SetData(TextObject message, bool isDisabled = false)
	{
		_messageTextObj = message;
		base.IsDisabled = isDisabled;
		RefreshValues();
		OnSetData(message, isDisabled);
	}

	public void ResetData()
	{
		_messageTextObj = null;
		base.IsDisabled = false;
		base.Message = string.Empty;
		base.IsDisplayed = false;
		OnResetData();
	}

	protected virtual void OnSetData(TextObject message, bool isDisabled)
	{
	}

	protected virtual void OnResetData()
	{
	}
}
