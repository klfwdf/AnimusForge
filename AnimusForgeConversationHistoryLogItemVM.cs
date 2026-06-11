using TaleWorlds.Library;

namespace AnimusForge;

public sealed class AnimusForgeConversationHistoryLogItemVM : ViewModel
{
	private string _chatItemTime;

	private string _chatSpeaker;

	private string _chatText;

	private string _fontColor;

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

	public AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string text, string kind)
	{
		ChatItemTime = time ?? "";
		ChatSpeaker = string.IsNullOrWhiteSpace(speaker) ? "\u8bb0\u5f55" : speaker.Trim();
		ChatText = string.IsNullOrWhiteSpace(ChatSpeaker) ? (text ?? "") : "(" + ChatSpeaker + ")" + (text ?? "");
		FontColor = ResolveColor(kind);
	}

	private static string ResolveColor(string kind)
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
