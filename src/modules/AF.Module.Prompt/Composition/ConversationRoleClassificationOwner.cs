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
}
