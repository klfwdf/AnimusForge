using System; namespace AnimusForge; internal static class CourierProjection {internal static object CreateCourierChatMessage(string role, string content)
	{
        return new ConversationPromptMessage(role ?? "", content ?? "");
	}
internal static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage, Action<int> lengthSink = null)
	{
		chatMessage = null;
		if (message == null)
		{
			return false;
		}
		string content = (message.Role ?? "").Trim().Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? DiplomacyReplyTextRules.CleanNpcText(message.Content ?? "", message.SpeakerName)
            : message.Content ?? "";
		content = content.Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		string role = (message.Role ?? "").Trim();
		string speaker = (message.SpeakerName ?? "").Trim();
		string metadata = BuildCourierMemoryMetadataPrefix(message, string.IsNullOrWhiteSpace(speaker) ? PromptComposerTexts.Get("residual_record") : speaker);
		if (ConversationRoleClassificationOwner.IsViewerAssistant(message, npcName, null, -1, useStableIdentity: false))
		{
			chatMessage = CreateMemoryChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName), lengthSink);
			return true;
		}
		if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			chatMessage = CreateMemoryChatMessage("user", metadata + PromptComposerTexts.Get("history_section_scope_historical") + MemorySystemFactProjection.ProjectNpcFact(StripCourierPromptScopeLabel(content)), lengthSink);
			return true;
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string otherSpeaker = string.IsNullOrWhiteSpace(speaker) ? PromptComposerTexts.Get("residual_other_npc") : speaker;
			chatMessage = CreateMemoryChatMessage("user", metadata + PromptComposerTexts.Get("residual_past_heard") + otherSpeaker + PromptComposerTexts.Get("residual_says") + StripCourierSpeakerPrefix(content, otherSpeaker), lengthSink);
			return true;
		}
		string body;
		if (string.IsNullOrWhiteSpace(playerName))
		{
			// Stored records may carry the fallback label of either language.
			body = (content ?? "").Trim();
			foreach (string fallback in PromptComposerTexts.RawVariants("player_fallback"))
			{
				string stripped = StripCourierSpeakerPrefix(body, fallback);
				if (!string.Equals(stripped, body, StringComparison.Ordinal)) { body = stripped; break; }
			}
		}
		else
		{
			body = StripCourierSpeakerPrefix(content, playerName.Trim());
		}
		chatMessage = CreateMemoryChatMessage("user", metadata + body, lengthSink);
		return true;
	}
internal static string BuildCourierMemoryMetadataPrefix(ConversationMessage message, string fallbackSpeaker)
	{
		string date = string.IsNullOrWhiteSpace(message?.GameDate) ? (PromptComposerTexts.Get("residual_day_prefix") + Math.Max(0, message?.GameDayIndex ?? 0) + PromptComposerTexts.Get("residual_day_suffix")) : message.GameDate.Trim();
		int hour = Math.Max(0, Math.Min(23, message?.GameHour ?? 0));
		string scene = (message?.Scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string speaker = string.IsNullOrWhiteSpace(fallbackSpeaker) ? PromptComposerTexts.Get("residual_record") : fallbackSpeaker.Trim();
		if (string.IsNullOrWhiteSpace(scene))
		{
			return "[" + date + " " + hour + PromptComposerTexts.Get("residual_hour_separator") + speaker + "] ";
		}
		return "[" + date + " " + hour + PromptComposerTexts.Get("residual_hour_separator") + scene + "｜" + speaker + "] ";
	}
internal static string StripCourierPromptScopeLabel(string text)
	{
		string value = (text ?? "").Trim();
		bool changed;
		do
		{
			changed = false;
			foreach (string prefix in CourierScopePrefixes)
			{
				if (value.StartsWith(prefix, StringComparison.Ordinal))
				{
					value = value.Substring(prefix.Length).Trim();
					changed = true;
				}
			}
		}
		while (changed);
		return value;
	}
internal static string StripCourierSpeakerPrefix(string content, string speaker)
	{
		string text = (content ?? "").Trim();
		string name = (speaker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(name))
		{
			return text;
		}
		string[] prefixes = new string[2] { name + ": ", name + "：" };
		foreach (string prefix in prefixes)
		{
			if (text.StartsWith(prefix, StringComparison.Ordinal))
			{
				return text.Substring(prefix.Length).Trim();
			}
		}
		return text;
	}}