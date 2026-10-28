using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tournament;

public class TournamentParticipantBrushWidget : BrushWidget
{
	private bool _stateChanged;

	private bool _brushApplied;

	private int _matchState;

	private bool _isDead;

	private bool _onMission;

	private bool _isMainHero;

	private Brush _mainHeroTextBrush;

	private Brush _normalTextBrush;

	private TextWidget _nameTextWidget;

	public TextWidget NameTextWidget
	{
		get
		{
			return _nameTextWidget;
		}
		set
		{
			if (_nameTextWidget != value)
			{
				_nameTextWidget = value;
			}
		}
	}

	public int MatchState
	{
		get
		{
			return _matchState;
		}
		set
		{
			if (_matchState != value)
			{
				_stateChanged = true;
				_matchState = value;
			}
		}
	}

	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (_isDead != value)
			{
				_stateChanged = true;
				_isDead = value;
			}
		}
	}

	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (_isMainHero != value)
			{
				_isMainHero = value;
				_brushApplied = false;
			}
		}
	}

	public Brush MainHeroTextBrush
	{
		get
		{
			return _mainHeroTextBrush;
		}
		set
		{
			if (_mainHeroTextBrush != value)
			{
				_mainHeroTextBrush = value;
			}
		}
	}

	public Brush NormalTextBrush
	{
		get
		{
			return _normalTextBrush;
		}
		set
		{
			if (_normalTextBrush != value)
			{
				_normalTextBrush = value;
			}
		}
	}

	public bool OnMission
	{
		get
		{
			return _onMission;
		}
		set
		{
			if (_onMission != value)
			{
				_stateChanged = true;
				_onMission = value;
			}
		}
	}

	public TournamentParticipantBrushWidget(UIContext context)
		: base(context)
	{
		AddState("Current");
		AddState("Over");
		AddState("Dead");
	}

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		EventFired("ClickEvent");
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.AddState("Current");
		child.AddState("Over");
		child.AddState("Dead");
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		base.ParentWidget.AddState("Current");
		base.ParentWidget.AddState("Over");
		base.ParentWidget.AddState("Dead");
	}

	private void SetWidgetState(string state)
	{
		base.ParentWidget.SetState(state);
		SetState(state);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_brushApplied)
		{
			NameTextWidget.Brush = (IsMainHero ? MainHeroTextBrush : NormalTextBrush);
			_brushApplied = true;
		}
		if (!_stateChanged || base.ReadOnlyBrush == null || base.BrushRenderer.Brush == null)
		{
			return;
		}
		_stateChanged = false;
		SetWidgetState("Default");
		foreach (BrushLayer layer in base.Brush.Layers)
		{
			layer.Color = base.Brush.Color;
		}
		if (OnMission)
		{
			base.Brush.GlobalAlphaFactor = 0.75f;
		}
		else
		{
			base.Brush.GlobalAlphaFactor = 1f;
		}
		if (MatchState == 0)
		{
			SetWidgetState("Default");
		}
		else if (MatchState == 1)
		{
			SetWidgetState("Current");
		}
		else if (MatchState == 2)
		{
			SetWidgetState("Over");
		}
		else if (MatchState == 3)
		{
			if (_isDead && OnMission)
			{
				SetWidgetState("Dead");
			}
			else
			{
				SetWidgetState("Default");
			}
		}
	}
}
