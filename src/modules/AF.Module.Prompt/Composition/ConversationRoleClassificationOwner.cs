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

    // Request/edit boundary only. No game-object access or persisted-text mutation.
    // Memory validates commit markers on load; the existing typed part survives identity/language changes.
    internal static ConversationMessage ProjectDailyMemorySpeech(DailyMemoryLine line, string memoryName,
        int targetAgentIndex, string playerName)
    {
        string npcName = string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName.Trim();
        string speaker = (line.Speaker ?? "").Trim();
        string text = (line.Text ?? "").Replace("\r", "").Trim();
        var message = new ConversationMessage {
            Role = "assistant", Content = text,
            SpeakerName = speaker.Length > 0 ? speaker : PromptComposerTexts.Get("residual_record"),
            SpeakerAgentIndex = -1, TargetAgentIndex = -1, TargetName = "", PlayerDistanceMeters = -1f
        };
        if (line.IsAfef)
        {
            message.Role = "system";
            message.SpeakerName = "AFEF";
            return message;
        }
        string part = (line.MemoryCommitPart ?? "").Trim();
        bool player = part == "user";
        if (!player)
        {
            if (TryParseDailyMemorySceneShoutLine(text, out string heardSpeaker, out string heardContent))
            {
                message.SpeakerName = string.IsNullOrWhiteSpace(heardSpeaker) ? PromptComposerTexts.Get("residual_other_npc") : heardSpeaker;
                message.Content = string.IsNullOrWhiteSpace(heardContent) ? text : heardContent;
                return message;
            }
            if (IsDailyMemoryLineNpcSpeech(line, npcName))
            {
                message.SpeakerName = npcName;
                message.SpeakerAgentIndex = targetAgentIndex;
                return message;
            }
            player = part != "assistant" && IsDailyMemoryLinePlayerSpeech(line, npcName, playerName);
        }
        if (player)
        {
            message.Role = "user";
            message.TargetAgentIndex = line.TargetAgentIndex >= 0 ? line.TargetAgentIndex : targetAgentIndex;
            message.TargetName = string.IsNullOrWhiteSpace(line.TargetName) ? npcName : line.TargetName.Trim();
        }
        return message;
    }

    internal static bool IsDailyMemoryLineNpcSpeech(DailyMemoryLine line, string npcName)
    {
        if (line == null || string.IsNullOrWhiteSpace(npcName)) return false;
        string speaker = (line.Speaker ?? "").Trim();
        string name = npcName.Trim();
        string text = (line.Text ?? "").Trim();
        return string.Equals(speaker, name, StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(name + ":", StringComparison.Ordinal)
            || text.StartsWith(name + "：", StringComparison.Ordinal);
    }

    private static readonly string[] PlayerHeaders = PromptComposerTexts.Raw("History_PlayerHeaders").Split(',');
    private static readonly string[] AddressInfixes = PromptComposerTexts.Raw("History_AddressInfixes").Split('|');

    internal static bool IsDailyMemoryLinePlayerSpeech(DailyMemoryLine line, string npcName, string playerName = "")
    {
        if (line == null) return false;
        string speaker = (line.Speaker ?? "").Trim();
        if (IsLikelyPlayerHistorySpeaker(speaker)
            || (!string.IsNullOrWhiteSpace(playerName) && string.Equals(speaker, playerName.Trim(), StringComparison.Ordinal))) return true;
        string text = (line.Text ?? "").Trim();
        int delimiter = FindDialogueHistorySpeakerDelimiter(text);
        string header = delimiter < 0 ? "" : text.Substring(0, delimiter).Trim();
        string target = string.IsNullOrWhiteSpace(line.TargetName) ? npcName : line.TargetName.Trim();
        if (!string.IsNullOrWhiteSpace(playerName) && TryStripAddressedSpeech(text, playerName.Trim(), target, out _)) return true;
        foreach (string player in PlayerHeaders)
            if ((delimiter >= 0 && string.Equals(header, player, StringComparison.OrdinalIgnoreCase))
                || TryStripAddressedSpeech(text, player, target, out _)) return true;
        return false;
    }

    // Parse only a whole leading storage header with an exact captured speaker and recipient.
    // Both editions remain readable after a package switch; quoted/narrative mentions are not headers.
    internal static bool TryStripAddressedSpeech(string text, string speaker, string target, out string content)
    {
        content = text;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(target)) return false;
        if (!text.StartsWith(speaker, StringComparison.OrdinalIgnoreCase)) return false;
        foreach (string infix in AddressInfixes)
        {
            int targetStart = speaker.Length + infix.Length;
            int suffixStart = targetStart + target.Length;
            if (!MatchesAt(text, speaker.Length, infix) || !MatchesAt(text, targetStart, target)) continue;
            if (infix == PromptComposerTexts.Raw("History_SpeechAddressedInfix")
                || infix == PromptComposerTexts.Raw("History_SpeechDirectedInfix"))
            {
                if (suffixStart >= text.Length || (text[suffixStart] != PromptComposerTexts.Raw("History_SpeechAddressedSuffix")[0]
                    && text[suffixStart] != PromptComposerTexts.Raw("History_SpeechShoutedSuffix")[0])) continue;
                suffixStart++;
            }
            if (suffixStart >= text.Length || (text[suffixStart] != ':' && text[suffixStart] != '：')) continue;
            content = text.Substring(suffixStart + 1).Trim();
            return true;
        }
        return false;
    }

    private static bool MatchesAt(string text, int offset, string value)
        => offset <= text.Length - value.Length
            && string.Compare(text, offset, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;

    internal static bool TryParseDailyMemorySceneShoutLine(string rawText, out string speaker, out string content)
    {
        speaker = "";
        content = "";
        string text = (rawText ?? "").Trim();
        string prefix = PromptComposerTexts.Raw("Scene_OverheardShoutPrefix");
        int prefixLength;
        if (text.StartsWith(prefix, StringComparison.Ordinal)) prefixLength = prefix.Length;
        else if (text.StartsWith("[Scene shout]", StringComparison.OrdinalIgnoreCase)) prefixLength = "[Scene shout]".Length;
        else return false;
        string body = text.Substring(prefixLength).Trim();
        int delimiter = FindDialogueHistorySpeakerDelimiter(body);
        if (delimiter > 0)
        {
            speaker = body.Substring(0, delimiter).Trim();
            content = body.Substring(delimiter + 1).Trim();
        }
        else content = body;
        return true;
    }
}
