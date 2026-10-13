using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class NpcInitiatedOpeningRouter
{
	public static bool HasPendingNativeOpeningForCurrentConversation()
	{
		if (PlayerCaptivityConversation.HasSession)
			return PlayerCaptivityConversation.HasPendingNativeOpeningForCurrentConversation();
		return CompanionProactiveChatBehavior.HasPendingNativeOpeningForCurrentConversation()
			|| ProactiveNpcRequestBehavior.HasPendingNativeOpeningForCurrentConversation();
	}

	public static bool TryConsumePendingNativeOpening(Hero hero, out string extraFact, out string promptText, out string source)
	{
		extraFact = "";
		promptText = "";
		source = "";
		if (PlayerCaptivityConversation.HasSession)
		{
			if (!PlayerCaptivityConversation.TryConsumePendingNativeOpening(hero, out extraFact, out promptText)) return false;
			source = "PlayerCaptivity";
			return true;
		}
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
