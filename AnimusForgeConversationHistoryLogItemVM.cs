using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class AnimusForgeConversationHistoryLogItemVM : ViewModel
{
	private string _chatItemTime;

	private string _chatSpeaker;

	private string _chatText;

	private string _fontColor;

	private readonly Action<string> _onOpenEncyclopediaLink;

	[DataSourceProperty]
	public string ChatItemTime
	{
		get => _chatItemTime;
		set
		{
			if (value != _chatItemTime)
			{
				_chatItemTime = value;
				OnPropertyChangedWithValue(value, nameof(ChatItemTime));
			}
		}
	}

	[DataSourceProperty]
	public string ChatSpeaker
	{
		get => _chatSpeaker;
		set
		{
			if (value != _chatSpeaker)
			{
				_chatSpeaker = value;
				OnPropertyChangedWithValue(value, nameof(ChatSpeaker));
			}
		}
	}

	[DataSourceProperty]
	public string ChatText
	{
		get => _chatText;
		set
		{
			if (value != _chatText)
			{
				_chatText = value;
				OnPropertyChangedWithValue(value, nameof(ChatText));
			}
		}
	}

	[DataSourceProperty]
	public string FontColor
	{
		get => _fontColor;
		set
		{
			if (value != _fontColor)
			{
				_fontColor = value;
				OnPropertyChangedWithValue(value, nameof(FontColor));
			}
		}
	}

	// This constructor receives an internal UI snapshot and is intentionally not part of the public VM surface.
	internal AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string text, string kind, EncyclopediaEntityLinkFormatter.DisplaySession linkDisplaySession, Hero conversationTargetHero, CharacterObject conversationTargetCharacter, Action<string> onOpenEncyclopediaLink)
	{
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		ChatItemTime = time ?? "";
		ChatSpeaker = string.IsNullOrWhiteSpace(speaker) ? "\u8bb0\u5f55" : speaker.Trim();
		// Build only a disposable RichText copy; the persisted dialogue entry stays plain for memory and LLM reuse.
		string rawDisplayText = string.IsNullOrWhiteSpace(ChatSpeaker) ? (text ?? "") : "(" + ChatSpeaker + ")" + (text ?? "");
		ChatText = linkDisplaySession?.Format(rawDisplayText, conversationTargetHero, conversationTargetCharacter) ?? EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawDisplayText);
		FontColor = ResolveFontColor(kind);
	}

	// Paged history has already prepared trusted RichText, so revisiting a page must not repeat entity matching or string replacement.
	internal AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string formattedText, string fontColor, Action<string> onOpenEncyclopediaLink)
	{
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		ChatItemTime = time ?? "";
		ChatSpeaker = string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker.Trim();
		ChatText = formattedText ?? "";
		FontColor = string.IsNullOrWhiteSpace(fontColor) ? "#D6D6D6FF" : fontColor;
	}

	public void ExecuteOpenEncyclopediaLink(string link)
	{
		_onOpenEncyclopediaLink?.Invoke(link);
	}

	// Shared by the page-level cache so row colors remain identical whether the record is first formatted or restored from cache.
	internal static string ResolveFontColor(string kind)
	{
		switch ((kind ?? "").Trim())
		{
			case "player":
				return "#E2AF54FF";
			case "afef_player":
			case "afef_npc":
				return "#7DDCFFFF";
			case "scene":
				return "#A5FF9AFF";
			case "npc":
				return "#FFFFFFFF";
			default:
				return "#D6D6D6FF";
		}
	}
}
