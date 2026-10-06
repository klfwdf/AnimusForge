namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public struct CampaignOptionDisableStatus
{
	public bool IsDisabled { get; private set; }

	public string DisabledReason { get; private set; }

	public float ValueIfDisabled { get; private set; }

	public CampaignOptionDisableStatus(bool isDisabled, string disabledReason, float valueIfDisabled = -1f)
	{
		IsDisabled = isDisabled;
		DisabledReason = disabledReason;
		ValueIfDisabled = valueIfDisabled;
	}
}
