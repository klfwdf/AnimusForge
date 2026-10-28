namespace MCM.Abstractions.Base.PerCampaign;

public sealed class EmptyPerCampaignSettings : PerCampaignSettings<EmptyPerCampaignSettings>
{
	public override string Id => "empty_percampaign_v1";

	public override string DisplayName => "Empty PerCampaign Settings";
}
