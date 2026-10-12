using System; namespace AnimusForge; internal static class CourierProjection {internal static object CreateCourierChatMessage(string role, string content)
	{
		return new
		{
			role = role ?? "",
			content = content ?? ""
		};
	}
internal static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage)
	{
		chatMessage = null;
		if (message == null)
		{
			return false;
		}
		string content = (message.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		string role = (message.Role ?? "").Trim();
		string speaker = (message.SpeakerName ?? "").Trim();
		string metadata = BuildCourierMemoryMetadataPrefix(message, string.IsNullOrWhiteSpace(speaker) ? PromptComposerTexts.Get("residual_record") : speaker);
		if (ConversationRoleClassificationOwner.IsViewerAssistant(message, npcName, null, -1, useStableIdentity: false))
		{
			chatMessage = CreateCourierChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName));
			return true;
		}
		if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			chatMessage = CreateCourierChatMessage("user", metadata + PromptComposerTexts.Raw("main_history_raw_019") + StripCourierPromptScopeLabel(content));
			return true;
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string otherSpeaker = string.IsNullOrWhiteSpace(speaker) ? PromptComposerTexts.Get("residual_other_npc") : speaker;
			chatMessage = CreateCourierChatMessage("user", metadata + PromptComposerTexts.Get("residual_past_heard") + otherSpeaker + PromptComposerTexts.Get("residual_says") + StripCourierSpeakerPrefix(content, otherSpeaker));
			return true;
		}
		string player = string.IsNullOrWhiteSpace(playerName) ? PromptComposerTexts.Raw("player_fallback") : playerName.Trim();
		chatMessage = CreateCourierChatMessage("user", metadata + StripCourierSpeakerPrefix(content, player));
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
			string[] prefixes = new string[4] { PromptComposerTexts.Raw("history_section_scope_current"), PromptComposerTexts.Raw("main_history_raw_019"), PromptComposerTexts.Raw("main_history_raw_031"), PromptComposerTexts.Raw("main_history_raw_032") };
			foreach (string prefix in prefixes)
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