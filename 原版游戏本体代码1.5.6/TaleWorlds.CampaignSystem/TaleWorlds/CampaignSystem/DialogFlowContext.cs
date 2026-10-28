namespace TaleWorlds.CampaignSystem;

internal class DialogFlowContext
{
	internal readonly string Token;

	internal readonly bool ByPlayer;

	internal readonly DialogFlowContext Parent;

	internal readonly bool OptionsUsedOnlyOnce;

	public DialogFlowContext(string token, bool byPlayer, DialogFlowContext parent, bool optionsUsedOnlyOnce)
	{
		Token = token;
		ByPlayer = byPlayer;
		Parent = parent;
		OptionsUsedOnlyOnce = optionsUsedOnlyOnce;
	}
}
