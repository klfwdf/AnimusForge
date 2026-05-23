using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.KillFeed;

public class MultiplayerDuelKillFeedItemWidget : MultiplayerGeneralKillFeedItemWidget
{
	private const string EndOfDuelState = "EndOfDuel";

	private bool _isEndOfDuel;

	private BrushWidget _background;

	private BrushWidget _victimCompassBackground;

	private BrushWidget _murdererCompassBackground;

	private ScrollingRichTextWidget _victimNameText;

	private ScrollingRichTextWidget _murdererNameText;

	private TextWidget _victimScoreText;

	private TextWidget _murdererScoreText;

	[Editor(false)]
	public bool IsEndOfDuel
	{
		get
		{
			return _isEndOfDuel;
		}
		set
		{
			if (value != _isEndOfDuel)
			{
				_isEndOfDuel = value;
				OnPropertyChanged(value, "IsEndOfDuel");
				if (value)
				{
					Background?.SetState("EndOfDuel");
					VictimCompassBackground?.SetState("EndOfDuel");
					MurdererCompassBackground?.SetState("EndOfDuel");
					VictimNameText?.SetState("EndOfDuel");
					MurdererNameText?.SetState("EndOfDuel");
					VictimScoreText?.SetState("EndOfDuel");
					MurdererScoreText?.SetState("EndOfDuel");
				}
			}
		}
	}

	[Editor(false)]
	public BrushWidget Background
	{
		get
		{
			return _background;
		}
		set
		{
			if (value != _background)
			{
				_background = value;
				OnPropertyChanged(value, "Background");
			}
		}
	}

	[Editor(false)]
	public BrushWidget VictimCompassBackground
	{
		get
		{
			return _victimCompassBackground;
		}
		set
		{
			if (value != _victimCompassBackground)
			{
				_victimCompassBackground = value;
				OnPropertyChanged(value, "VictimCompassBackground");
			}
		}
	}

	[Editor(false)]
	public BrushWidget MurdererCompassBackground
	{
		get
		{
			return _murdererCompassBackground;
		}
		set
		{
			if (value != _murdererCompassBackground)
			{
				_murdererCompassBackground = value;
				OnPropertyChanged(value, "MurdererCompassBackground");
			}
		}
	}

	[Editor(false)]
	public ScrollingRichTextWidget VictimNameText
	{
		get
		{
			return _victimNameText;
		}
		set
		{
			if (value != _victimNameText)
			{
				_victimNameText = value;
				OnPropertyChanged(value, "VictimNameText");
			}
		}
	}

	[Editor(false)]
	public ScrollingRichTextWidget MurdererNameText
	{
		get
		{
			return _murdererNameText;
		}
		set
		{
			if (value != _murdererNameText)
			{
				_murdererNameText = value;
				OnPropertyChanged(value, "MurdererNameText");
			}
		}
	}

	[Editor(false)]
	public TextWidget VictimScoreText
	{
		get
		{
			return _victimScoreText;
		}
		set
		{
			if (value != _victimScoreText)
			{
				_victimScoreText = value;
				OnPropertyChanged(value, "VictimScoreText");
			}
		}
	}

	[Editor(false)]
	public TextWidget MurdererScoreText
	{
		get
		{
			return _murdererScoreText;
		}
		set
		{
			if (value != _murdererScoreText)
			{
				_murdererScoreText = value;
				OnPropertyChanged(value, "MurdererScoreText");
			}
		}
	}

	public MultiplayerDuelKillFeedItemWidget(UIContext context)
		: base(context)
	{
	}
}
