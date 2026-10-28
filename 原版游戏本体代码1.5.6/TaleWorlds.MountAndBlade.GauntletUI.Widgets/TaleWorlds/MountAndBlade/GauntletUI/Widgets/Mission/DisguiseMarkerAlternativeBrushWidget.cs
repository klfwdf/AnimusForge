using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class DisguiseMarkerAlternativeBrushWidget : BrushWidget
{
	private int _alarmProgress;

	private string _alarmState;

	private string _offenseTypeIdentifier;

	public Widget BackgroundGlowWidget { get; set; }

	public Widget FrameWidget { get; set; }

	public Widget FillBarWidget { get; set; }

	public float AlarmedHeight { get; set; } = 40f;

	public float DefaultHeight { get; set; } = 20f;

	public Vec2 Position { get; set; }

	public int AlarmProgress
	{
		get
		{
			return _alarmProgress;
		}
		set
		{
			if (value != _alarmProgress)
			{
				_alarmProgress = value;
				OnPropertyChanged(value, "AlarmProgress");
			}
		}
	}

	public string AlarmState
	{
		get
		{
			return _alarmState;
		}
		set
		{
			if (value != _alarmState)
			{
				_alarmState = value;
				OnPropertyChanged(value, "AlarmState");
				UpdateAlarmState();
			}
		}
	}

	public string OffenseTypeIdentifier
	{
		get
		{
			return _offenseTypeIdentifier;
		}
		set
		{
			if (value != _offenseTypeIdentifier)
			{
				_offenseTypeIdentifier = value;
				OnPropertyChanged(value, "OffenseTypeIdentifier");
				UpdateState();
			}
		}
	}

	public DisguiseMarkerAlternativeBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.ScaledPositionYOffset = Position.y - base.Size.Y / 2f;
		base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
		bool flag = !string.IsNullOrEmpty(AlarmState) && (float)AlarmProgress > 0f;
		base.SuggestedHeight = MathF.Lerp(base.SuggestedHeight, flag ? AlarmedHeight : DefaultHeight, dt * 5f);
		base.SuggestedWidth = MathF.Lerp(base.SuggestedWidth, flag ? 38 : 32, dt * 5f);
		if (!string.IsNullOrEmpty(OffenseTypeIdentifier))
		{
			BackgroundGlowWidget?.SetState(OffenseTypeIdentifier);
		}
		if (!string.IsNullOrEmpty(AlarmState))
		{
			FillBarWidget?.SetState(AlarmState);
		}
	}

	private void UpdateState()
	{
	}

	private void UpdateAlarmState()
	{
	}
}
