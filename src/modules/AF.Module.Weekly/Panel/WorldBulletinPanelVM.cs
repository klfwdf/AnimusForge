using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace AnimusForge;

// Everything the bulletin sheet shows, resolved once when the panel opens.
internal sealed class WorldBulletinPanelData
{
	public string EventId = "";

	public string MastheadText = "";

	public string TitleText = "列 国 邸 报";

	public string KindText = "";

	public string HeadlineText = "";

	public string MetaText = "";

	public string BodyText = "";

	public List<KeyValuePair<string, string>> Minors = new List<KeyValuePair<string, string>>();

	// Plain text handed to the illustrator; never shown.
	public string IllustrationSubtitle = "";

	public string IllustrationBody = "";

	public WorldBulletinIllustrationPlan IllustrationPlan;
}

// Shared by the floating weekly-report overlay and the inline bulletin slot so one generation pipeline feeds both.
public interface IWeeklyIllustrationSink
{
	string SpriteName { get; set; }

	bool HasIllustration { get; set; }

	bool IsLoading { get; set; }

	string StatusText { get; set; }

	string TitleText { get; set; }

	string PromptText { get; set; }
}

// The integrated Illustrator registers here; the host never references Illustrator types directly.
internal static class WorldBulletinPanelIllustrationBridge
{
	// (slot, eventId, title, subtitle, body) -> true when the slot was attached.
	public static Func<WorldBulletinIllustrationVM, string, string, string, string, WorldBulletinIllustrationPlan, bool> AttachSlot;

	// Fired once after an issue's final text has been published, before its map notice.
	public static Action<string, string, string, string> PrepareIssue;
	public static Action<WorldBulletinIllustrationPlan> PrepareSelection;
	public static Action<WorldBulletinIllustrationPlan> CancelSelection;
	// Main-thread query: true only when the configured fast path should start an image
	// while the bulletin is being prepared rather than when its panel opens.
	public static Func<bool> ShouldPreloadSelection;
	// (plan, release) -> true when release will be invoked once, on the main thread, after the selected
	// illustration settles or times out; false means nothing is pending and the caller proceeds now.
	public static Func<WorldBulletinIllustrationPlan, Action, bool> AwaitSelection;
}

public sealed class WorldBulletinMinorItemVM : ViewModel
{
	private readonly Action<string> _onLink;

	[DataSourceProperty]
	public string TagText { get; }

	[DataSourceProperty]
	public string Text { get; }

	public WorldBulletinMinorItemVM(string tag, string text, Action<string> onLink)
	{
		TagText = tag ?? "";
		Text = text ?? "";
		_onLink = onLink;
	}

	public void ExecuteOpenEncyclopediaLink(string link)
	{
		_onLink?.Invoke(link);
	}
}

public sealed class WorldBulletinIllustrationVM : ViewModel, IWeeklyIllustrationSink
{
	private bool _isAvailable;

	private string _spriteName = "";

	private bool _hasIllustration;

	private bool _isLoading;

	private string _statusText = "";

	private string _titleText = "";

	private string _promptText = "";

	private bool _showPrompt;

	public Action OnRegenerate;

	public Action OnRegenerateWithPrompt;

	internal Action<bool> SetPromptEditing;

	public Action OnDelete;

	public Action OnOpenGallery;

	[DataSourceProperty]
	public bool IsAvailable
	{
		get => _isAvailable;
		set { if (value != _isAvailable) { _isAvailable = value; OnPropertyChangedWithValue(value, nameof(IsAvailable)); } }
	}

	[DataSourceProperty]
	public string SpriteName
	{
		get => _spriteName;
		set { if (value != _spriteName) { _spriteName = value; OnPropertyChangedWithValue(value, nameof(SpriteName)); } }
	}

	[DataSourceProperty]
	public bool HasIllustration
	{
		get => _hasIllustration;
		set { if (value != _hasIllustration) { _hasIllustration = value; OnPropertyChangedWithValue(value, nameof(HasIllustration)); RefreshDerived(); } }
	}

	[DataSourceProperty]
	public bool IsLoading
	{
		get => _isLoading;
		set { if (value != _isLoading) { _isLoading = value; OnPropertyChangedWithValue(value, nameof(IsLoading)); RefreshDerived(); } }
	}

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText;
		set { if (value != _statusText) { _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); } }
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set { if (value != _titleText) { _titleText = value; OnPropertyChangedWithValue(value, nameof(TitleText)); OnPropertyChangedWithValue(CaptionText, nameof(CaptionText)); } }
	}

	[DataSourceProperty]
	public string CaptionText => string.IsNullOrWhiteSpace(_titleText) ? "" : "图：" + _titleText.Trim();

	[DataSourceProperty]
	public string PromptText
	{
		get => _promptText;
		set { if (value != _promptText) { _promptText = value; OnPropertyChangedWithValue(value, nameof(PromptText)); } }
	}

	[DataSourceProperty]
	public bool ShowPrompt
	{
		get => _showPrompt;
		set { if (value != _showPrompt) { _showPrompt = value; OnPropertyChangedWithValue(value, nameof(ShowPrompt)); } }
	}

	// The status line covers the empty frame; once an image is up the status stays out of the way.
	[DataSourceProperty]
	public bool ShowPlaceholder => !_hasIllustration || _isLoading;

	[DataSourceProperty]
	public bool CanRegenerate => !_isLoading && OnRegenerate != null;

	[DataSourceProperty]
	public bool CanRegenerateWithPrompt => !_isLoading && OnRegenerateWithPrompt != null;

	private void RefreshDerived()
	{
		OnPropertyChangedWithValue(ShowPlaceholder, nameof(ShowPlaceholder));
		OnPropertyChangedWithValue(CanRegenerate, nameof(CanRegenerate));
		OnPropertyChangedWithValue(CanRegenerateWithPrompt, nameof(CanRegenerateWithPrompt));
	}

	public void NotifyHandlersChanged()
	{
		RefreshDerived();
	}

	public void ExecuteRegenerate()
	{
		if (!_isLoading)
		{
			ShowPrompt = false;
			OnRegenerate?.Invoke();
		}
	}

	public void ExecuteTogglePrompt()
	{
		if (string.IsNullOrWhiteSpace(_promptText))
		{
			StatusText = "还没有可查看的提示词。";
			ShowPrompt = false;
			return;
		}
		ShowPrompt = !ShowPrompt;
	}

	public void ExecuteRegenerateWithPrompt()
	{
		if (CanRegenerateWithPrompt) OnRegenerateWithPrompt.Invoke();
	}

	public void ExecuteCopyPrompt()
	{
		if (string.IsNullOrWhiteSpace(_promptText))
		{
			return;
		}
		try
		{
			Input.SetClipboardText(_promptText);
			InformationManager.DisplayMessage(new InformationMessage("提示词已复制到剪贴板。"));
		}
		catch (Exception ex)
		{
			Logger.Log("WorldBulletinPanel", "[WARN] copy prompt failed: " + ex.Message);
		}
	}

	public void ExecuteOpenGallery()
	{
		OnOpenGallery?.Invoke();
	}

	public void ExecuteDelete()
	{
		if (_hasIllustration && !_isLoading)
		{
			OnDelete?.Invoke();
		}
	}
}

public sealed class WorldBulletinPanelVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action<string> _onOpenEncyclopediaLink;
	private readonly List<KeyValuePair<string, string>> _minors;
	private readonly EncyclopediaEntityLinkFormatter.DisplaySession _links;
	private float _minorScrollPosition;
	[DataSourceProperty] public float MinorScrollPosition
	{
		get => _minorScrollPosition;
		set { _minorScrollPosition = value; OnPropertyChangedWithValue(value, nameof(MinorScrollPosition)); }
	}

	[DataSourceProperty]
	public string MastheadText { get; }

	[DataSourceProperty]
	public string TitleText { get; }

	[DataSourceProperty]
	public string KindText { get; }

	[DataSourceProperty]
	public string HeadlineText { get; }

	[DataSourceProperty]
	public string MetaText { get; }

	[DataSourceProperty]
	public bool HasDropCap { get; }

	[DataSourceProperty]
	public string DropCapText { get; }

	[DataSourceProperty]
	public string BodyText { get; }

	[DataSourceProperty]
	public int BodyFontSize { get; }

	[DataSourceProperty]
	public MBBindingList<WorldBulletinMinorItemVM> LeftMinors { get; } = new MBBindingList<WorldBulletinMinorItemVM>();

	[DataSourceProperty]
	public MBBindingList<WorldBulletinMinorItemVM> RightMinors { get; } = new MBBindingList<WorldBulletinMinorItemVM>();

	[DataSourceProperty]
	public bool HasMinors { get; }

	[DataSourceProperty]
	public string FooterHintText { get; } = "点击人名与地名，查阅百科";

	[DataSourceProperty]
	public string CloseText { get; } = "封 存 快 报";

	[DataSourceProperty]
	public bool ShowCloseButton { get; } = true;

	[DataSourceProperty]
	public WorldBulletinIllustrationVM Illustration { get; } = new WorldBulletinIllustrationVM();

	internal WorldBulletinPanelVM(WorldBulletinPanelData data, int bodyFontSize, Action onClose, Action<string> onOpenEncyclopediaLink)
	{
		_onClose = onClose;
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		data ??= new WorldBulletinPanelData();
		MastheadText = Sanitize(data.MastheadText);
		TitleText = Sanitize(string.IsNullOrWhiteSpace(data.TitleText) ? "列 国 邸 报" : data.TitleText);
		KindText = Sanitize(data.KindText);
		HeadlineText = Sanitize(data.HeadlineText);
		MetaText = Sanitize(data.MetaText);
		BodyFontSize = Math.Max(14, Math.Min(18, bodyFontSize));
		_links = EncyclopediaEntityLinkFormatter.CreateDisplaySession();
		string body = (data.BodyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (body.Length == 0)
		{
			body = "本期快报正文为空。";
		}
		string formatted = _links.Format(body);
		// The drop cap takes the first CJK character only when link markup has not claimed it,
		// so a leading hero/settlement name keeps its encyclopedia link intact.
		char first = body[0];
		if (IsCjk(first) && formatted.Length > 1 && formatted[0] == first)
		{
			HasDropCap = true;
			DropCapText = first.ToString();
			BodyText = formatted.Substring(1);
		}
		else
		{
			DropCapText = "";
			BodyText = formatted;
		}
		_minors = new List<KeyValuePair<string, string>>(data.Minors ?? new List<KeyValuePair<string, string>>());
		HasMinors = _minors.Count > 0;
		RefreshMinors();
	}

	private void RefreshMinors()
	{
		LeftMinors.Clear();
		RightMinors.Clear();
		int count = _minors.Count;
		int leftCount = (count + 1) / 2;
		for (int i = 0; i < count; i++)
		{
			var minor = _minors[i];
			WorldBulletinMinorItemVM item = new WorldBulletinMinorItemVM("¶ " + Sanitize(minor.Key), _links.Format((minor.Value ?? "").Trim()), HandleLink);
			(i < leftCount ? LeftMinors : RightMinors).Add(item);
		}
		MinorScrollPosition = 0;
	}

	private static bool IsCjk(char c)
	{
		return c >= '一' && c <= '鿿';
	}

	private static string Sanitize(string text)
	{
		return EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(text ?? "");
	}

	private void HandleLink(string link)
	{
		_onOpenEncyclopediaLink?.Invoke(link);
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	public void ExecuteOpenEncyclopediaLink(string link)
	{
		HandleLink(link);
	}
}
