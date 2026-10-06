using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class ActionCampaignOptionData : CampaignOptionData
{
	private Action _action;

	public ActionCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Action action, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null)
		: base(identifier, priorityIndex, enableState, null, null, getIsDisabledWithReason)
	{
		_action = action;
	}

	public override CampaignOptionDataType GetDataType()
	{
		return CampaignOptionDataType.Action;
	}

	public void ExecuteAction()
	{
		_action?.Invoke();
	}
}
