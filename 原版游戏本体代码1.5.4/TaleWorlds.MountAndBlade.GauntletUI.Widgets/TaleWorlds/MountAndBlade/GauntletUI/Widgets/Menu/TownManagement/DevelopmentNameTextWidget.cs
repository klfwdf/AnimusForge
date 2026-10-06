using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class DevelopmentNameTextWidget : TextWidget
{
	public enum AnimState
	{
		Start,
		DownName,
		UpMax,
		StayMax,
		DownMax,
		UpName,
		Idle
	}

	private float _currentAlphaTarget;

	private float _stayMaxTotalTime;

	private AnimState _currentState = AnimState.Idle;

	private float _maxTextStayTime = 1f;

	private bool _isInQueue;

	private string _maxText;

	private string _nameText;

	[Editor(false)]
	public string MaxText
	{
		get
		{
			return _maxText;
		}
		set
		{
			if (_maxText != value)
			{
				_maxText = value;
				OnPropertyChanged(value, "MaxText");
			}
		}
	}

	[Editor(false)]
	public float MaxTextStayTime
	{
		get
		{
			return _maxTextStayTime;
		}
		set
		{
			if (_maxTextStayTime != value)
			{
				_maxTextStayTime = value;
				OnPropertyChanged(value, "MaxTextStayTime");
			}
		}
	}

	[Editor(false)]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (_nameText != value)
			{
				_nameText = value;
				OnPropertyChanged(value, "NameText");
				base.Text = NameText;
			}
		}
	}

	[Editor(false)]
	public bool IsInQueue
	{
		get
		{
			return _isInQueue;
		}
		set
		{
			if (_isInQueue != value)
			{
				_isInQueue = value;
				OnPropertyChanged(value, "IsInQueue");
			}
		}
	}

	public DevelopmentNameTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!IsInQueue)
		{
			SetState(base.ParentWidget.CurrentState);
		}
		else
		{
			SetState("Selected");
		}
		HandleAnim(dt);
	}

	private void HandleAnim(float dt)
	{
		switch (_currentState)
		{
		case AnimState.Start:
			_currentAlphaTarget = 0f;
			_currentState = AnimState.DownName;
			break;
		case AnimState.DownName:
			if ((double)base.ReadOnlyBrush.TextAlphaFactor < 0.01)
			{
				_currentAlphaTarget = 1f;
				base.Text = MaxText;
				_currentState = AnimState.UpMax;
			}
			break;
		case AnimState.UpMax:
			if ((double)base.ReadOnlyBrush.TextAlphaFactor > 0.99)
			{
				_currentAlphaTarget = 0f;
				_currentState = AnimState.StayMax;
				_stayMaxTotalTime = 0f;
			}
			break;
		case AnimState.StayMax:
			_stayMaxTotalTime += dt;
			if (_stayMaxTotalTime >= MaxTextStayTime)
			{
				_currentAlphaTarget = 0f;
				_currentState = AnimState.DownMax;
			}
			break;
		case AnimState.DownMax:
			if ((double)base.ReadOnlyBrush.TextAlphaFactor < 0.01)
			{
				_currentAlphaTarget = 1f;
				_currentState = AnimState.UpName;
				base.Text = NameText;
			}
			break;
		case AnimState.UpName:
			if ((double)base.ReadOnlyBrush.TextAlphaFactor > 0.99)
			{
				_currentState = AnimState.Idle;
				base.Text = NameText;
			}
			break;
		}
		if (_currentState != AnimState.Idle && _currentState != AnimState.StayMax)
		{
			base.Brush.TextAlphaFactor = Mathf.Lerp(base.ReadOnlyBrush.TextAlphaFactor, _currentAlphaTarget, dt * 15f);
		}
	}

	public void StartMaxTextAnimation()
	{
		AnimState currentState = _currentState;
		if ((uint)currentState > 3u)
		{
			_ = currentState - 4;
			_ = 2;
			_currentState = AnimState.Start;
		}
	}
}
