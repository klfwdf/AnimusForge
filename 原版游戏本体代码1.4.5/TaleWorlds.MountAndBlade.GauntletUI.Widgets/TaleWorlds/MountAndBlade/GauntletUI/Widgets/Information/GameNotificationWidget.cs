using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;

public class GameNotificationWidget : BrushWidget
{
	private bool _textWidgetAlignmentDirty = true;

	private float _notificationElapsedTimeInSeconds;

	private int _notificationId;

	private RichTextWidget _textWidget;

	private ImageIdentifierWidget _announcerImageIdentifier;

	private float _notificationDurationInSeconds;

	private bool _isPaused;

	private bool _mustFadeOutCurrentNotification;

	private float _notificationFadeOutDelayInSeconds;

	public float RampUpInSeconds { get; set; } = 0.2f;

	public float RampDownInSeconds { get; set; } = 0.2f;

	public ImageIdentifierWidget AnnouncerImageIdentifier
	{
		get
		{
			return _announcerImageIdentifier;
		}
		set
		{
			if (_announcerImageIdentifier != value)
			{
				_announcerImageIdentifier = value;
				OnPropertyChanged(value, "AnnouncerImageIdentifier");
			}
		}
	}

	public int NotificationId
	{
		get
		{
			return _notificationId;
		}
		set
		{
			if (_notificationId != value)
			{
				_notificationId = value;
				OnPropertyChanged(value, "NotificationId");
				_textWidgetAlignmentDirty = true;
				_notificationElapsedTimeInSeconds = 0f;
			}
		}
	}

	public float NotificationDurationInSeconds
	{
		get
		{
			return _notificationDurationInSeconds;
		}
		set
		{
			if (_notificationDurationInSeconds != value)
			{
				_notificationDurationInSeconds = value;
			}
		}
	}

	public RichTextWidget TextWidget
	{
		get
		{
			return _textWidget;
		}
		set
		{
			if (_textWidget != value)
			{
				_textWidget = value;
				OnPropertyChanged(value, "TextWidget");
			}
		}
	}

	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			if (_isPaused != value)
			{
				_isPaused = value;
				OnPropertyChanged(value, "IsPaused");
			}
		}
	}

	public bool MustFadeOutCurrentNotification
	{
		get
		{
			return _mustFadeOutCurrentNotification;
		}
		set
		{
			if (_mustFadeOutCurrentNotification != value)
			{
				_mustFadeOutCurrentNotification = value;
				OnPropertyChanged(value, "MustFadeOutCurrentNotification");
			}
		}
	}

	public float NotificationFadeOutDelayInSeconds
	{
		get
		{
			return _notificationFadeOutDelayInSeconds;
		}
		set
		{
			if (_notificationFadeOutDelayInSeconds != value)
			{
				_notificationFadeOutDelayInSeconds = value;
				OnPropertyChanged(value, "NotificationFadeOutDelayInSeconds");
			}
		}
	}

	public GameNotificationWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsVisible && _textWidgetAlignmentDirty)
		{
			ImageIdentifierWidget announcerImageIdentifier = AnnouncerImageIdentifier;
			if (announcerImageIdentifier != null && announcerImageIdentifier.IsVisible)
			{
				TextWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
			}
			else
			{
				TextWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
			}
			_textWidgetAlignmentDirty = false;
		}
		if (base.IsVisible && !IsPaused)
		{
			_notificationElapsedTimeInSeconds += dt;
			if (MustFadeOutCurrentNotification)
			{
				_notificationElapsedTimeInSeconds = RampUpInSeconds + NotificationDurationInSeconds - NotificationFadeOutDelayInSeconds;
				MustFadeOutCurrentNotification = false;
				NotificationFadeOutDelayInSeconds = 0f;
			}
			if (_notificationElapsedTimeInSeconds <= RampUpInSeconds)
			{
				float alphaFactor = Mathf.Lerp(0f, 1f, _notificationElapsedTimeInSeconds / RampUpInSeconds);
				this.SetGlobalAlphaRecursively(alphaFactor);
			}
			else if (_notificationElapsedTimeInSeconds <= RampUpInSeconds + NotificationDurationInSeconds)
			{
				this.SetGlobalAlphaRecursively(1f);
			}
			else if (_notificationElapsedTimeInSeconds < RampUpInSeconds + NotificationDurationInSeconds + RampDownInSeconds)
			{
				float alphaFactor2 = Mathf.Lerp(1f, 0f, (_notificationElapsedTimeInSeconds - RampUpInSeconds - NotificationDurationInSeconds) / RampDownInSeconds);
				this.SetGlobalAlphaRecursively(alphaFactor2);
			}
			else
			{
				this.SetGlobalAlphaRecursively(0f);
				EventFired("NotificationFinished");
			}
		}
	}
}
