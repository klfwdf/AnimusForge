using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.KillFeed.General;

public class SingleplayerGeneralKillFeedItemWidget : Widget
{
	private float _speedModifier = 1f;

	private bool _initialized;

	private string _murdererName;

	private string _murdererType;

	private string _victimName;

	private string _victimType;

	private bool _isUnconscious;

	private bool _isHeadshot;

	private bool _isSuicide;

	private bool _isDrowning;

	private bool _isPaused;

	public Brush TroopTypeIconBrush { get; set; }

	public Widget MurdererTypeWidget { get; set; }

	public Widget VictimTypeWidget { get; set; }

	public Widget ActionIconWidget { get; set; }

	public TextWidget VictimNameWidget { get; set; }

	public TextWidget MurdererNameWidget { get; set; }

	public float FadeInTime { get; set; } = 0.7f;

	public float StayTime { get; set; } = 3f;

	public float FadeOutTime { get; set; } = 0.7f;

	public float TimeSinceCreation { get; private set; }

	[Editor(false)]
	public string MurdererName
	{
		get
		{
			return _murdererName;
		}
		set
		{
			if (value != _murdererName)
			{
				_murdererName = value;
				OnPropertyChanged(value, "MurdererName");
			}
		}
	}

	[Editor(false)]
	public string MurdererType
	{
		get
		{
			return _murdererType;
		}
		set
		{
			if (value != _murdererType)
			{
				_murdererType = value;
				OnPropertyChanged(value, "MurdererType");
			}
		}
	}

	[Editor(false)]
	public string VictimName
	{
		get
		{
			return _victimName;
		}
		set
		{
			if (value != _victimName)
			{
				_victimName = value;
				OnPropertyChanged(value, "VictimName");
			}
		}
	}

	[Editor(false)]
	public string VictimType
	{
		get
		{
			return _victimType;
		}
		set
		{
			if (value != _victimType)
			{
				_victimType = value;
				OnPropertyChanged(value, "VictimType");
			}
		}
	}

	[Editor(false)]
	public bool IsUnconscious
	{
		get
		{
			return _isUnconscious;
		}
		set
		{
			if (value != _isUnconscious)
			{
				_isUnconscious = value;
				OnPropertyChanged(value, "IsUnconscious");
			}
		}
	}

	[Editor(false)]
	public bool IsHeadshot
	{
		get
		{
			return _isHeadshot;
		}
		set
		{
			if (value != _isHeadshot)
			{
				_isHeadshot = value;
				OnPropertyChanged(value, "IsHeadshot");
			}
		}
	}

	[Editor(false)]
	public bool IsSuicide
	{
		get
		{
			return _isSuicide;
		}
		set
		{
			if (value != _isSuicide)
			{
				_isSuicide = value;
				OnPropertyChanged(value, "IsSuicide");
			}
		}
	}

	[Editor(false)]
	public bool IsDrowning
	{
		get
		{
			return _isDrowning;
		}
		set
		{
			if (value != _isDrowning)
			{
				_isDrowning = value;
				OnPropertyChanged(value, "IsDrowning");
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
			if (value != _isPaused)
			{
				_isPaused = value;
				OnPropertyChanged(value, "IsPaused");
			}
		}
	}

	public SingleplayerGeneralKillFeedItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			Initialize();
			_initialized = true;
		}
		if (!IsPaused)
		{
			TimeSinceCreation += dt * _speedModifier;
		}
		if (TimeSinceCreation <= FadeInTime)
		{
			this.SetGlobalAlphaRecursively(Mathf.Lerp(base.AlphaFactor, 0.5f, TimeSinceCreation / FadeInTime));
		}
		else if (TimeSinceCreation - FadeInTime <= StayTime)
		{
			this.SetGlobalAlphaRecursively(0.5f);
		}
		else if (TimeSinceCreation - (FadeInTime + StayTime) <= FadeOutTime)
		{
			this.SetGlobalAlphaRecursively(Mathf.Lerp(base.AlphaFactor, 0f, (TimeSinceCreation - (FadeInTime + StayTime)) / FadeOutTime));
			if (base.AlphaFactor <= 0.1f)
			{
				EventFired("OnRemove");
			}
		}
		else
		{
			EventFired("OnRemove");
		}
	}

	private void Initialize()
	{
		this.SetGlobalAlphaRecursively(0f);
		MurdererTypeWidget.Sprite = TroopTypeIconBrush?.GetLayer(MurdererType)?.Sprite;
		VictimTypeWidget.Sprite = TroopTypeIconBrush?.GetLayer(VictimType)?.Sprite;
		ActionIconWidget.Sprite = ActionIconWidget.Context.SpriteData.GetSprite("General\\Mission\\PersonalKillfeed\\" + GetSpriteName());
		ActionIconWidget.Color = (IsUnconscious ? new Color(1f, 1f, 1f) : new Color(1f, 0f, 0f));
		if (string.IsNullOrEmpty(VictimName))
		{
			VictimNameWidget.IsVisible = false;
			ActionIconWidget.MarginRight = 0f;
			VictimTypeWidget.MarginLeft = 5f;
		}
		if (string.IsNullOrEmpty(MurdererName))
		{
			MurdererNameWidget.IsVisible = false;
			ActionIconWidget.MarginLeft = 0f;
			MurdererTypeWidget.MarginRight = 5f;
		}
		if (IsSuicide)
		{
			MurdererNameWidget.IsVisible = false;
			MurdererTypeWidget.IsVisible = false;
		}
	}

	private string GetSpriteName()
	{
		if (IsDrowning)
		{
			return "drowning_kill_icon";
		}
		if (IsHeadshot)
		{
			return "headshot_kill_icon";
		}
		return "kill_feed_skull";
	}

	public void SetSpeedModifier(float newSpeed)
	{
		if (newSpeed > _speedModifier)
		{
			_speedModifier = newSpeed;
		}
	}
}
