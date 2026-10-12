using System.Linq;
using AnimusForge;
using System;

namespace AnimusForge;

internal static class ConversationRoleClassificationOwner
{
	internal static bool IsViewerAssistant(ConversationMessage message, string viewerName,
		string viewerHeroId, int viewerAgentIndex, bool useStableIdentity)
	{
		if (message == null || !string.Equals((message.Role ?? "").Trim(), "assistant", StringComparison.OrdinalIgnoreCase))
			return false;
		if (useStableIdentity)
		{
			if (message.SpeakerAgentIndex == viewerAgentIndex) return true;
			string speakerId = (message.SpeakerHeroId ?? "").Trim();
			string recipientId = (viewerHeroId ?? "").Trim();
			return speakerId.Length > 0 && recipientId.Length > 0
				&& string.Equals(speakerId, recipientId, StringComparison.OrdinalIgnoreCase);
		}
		string speaker = (message.SpeakerName ?? "").Trim();
		string recipient = (viewerName ?? "").Trim();
		return speaker.Length > 0 && recipient.Length > 0
			&& string.Equals(speaker, recipient, StringComparison.OrdinalIgnoreCase);
	}
internal static int FindDialogueHistorySpeakerDelimiter(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return -1;
		}
		int colon = line.IndexOf(':');
		int cnColon = line.IndexOf('：');
		if (colon < 0)
		{
			return cnColon;
		}
		if (cnColon < 0)
		{
			return colon;
		}
		return Math.Min(colon, cnColon);
	}
internal static bool IsLikelyPlayerHistorySpeaker(string speaker)
	{
		string text = (speaker ?? "").Trim();
		return PromptComposerTexts.Raw("History_PlayerNames_Cn").Split(',').Any(name => text.IndexOf(name, StringComparison.Ordinal) >= 0) || text.IndexOf("MainHero", StringComparison.OrdinalIgnoreCase) >= 0
			|| string.Equals(text, "Player", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "The player", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "You", StringComparison.OrdinalIgnoreCase);
	}
}
