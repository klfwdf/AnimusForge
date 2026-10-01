using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class NpcInitiatedOpeningRouter
{
	public static bool HasPendingNativeOpeningForCurrentConversation()
	{
		return CompanionProactiveChatBehavior.HasPendingNativeOpeningForCurrentConversation()
			|| ProactiveNpcRequestBehavior.HasPendingNativeOpeningForCurrentConversation();
	}

	public static bool TryConsumePendingNativeOpening(Hero hero, out string extraFact, out string promptText, out string source)
	{
		extraFact = "";
		promptText = "";
		source = "";
		if (CompanionProactiveChatBehavior.TryConsumePendingNativeOpening(hero, out extraFact, out promptText))
		{
			source = "CompanionProactiveChat";
			return true;
		}
		if (ProactiveNpcRequestBehavior.TryConsumePendingNativeOpening(hero, out extraFact, out promptText))
		{
			source = "ProactiveNpcRequest";
			return true;
		}
		return false;
	}
}
