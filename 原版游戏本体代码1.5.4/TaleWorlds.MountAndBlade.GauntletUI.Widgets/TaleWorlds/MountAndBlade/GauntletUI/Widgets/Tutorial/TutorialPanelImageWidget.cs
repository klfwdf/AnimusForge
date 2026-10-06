using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialPanelImageWidget : ImageWidget
{
	public enum AnimState
	{
		Idle,
		Start,
		Starting,
		Playing
	}

	private AnimState _animState;

	private int _tickCount;

	private BrushListPanel _tutorialPanel;

	[Editor(false)]
	public BrushListPanel TutorialPanel
	{
		get
		{
			return _tutorialPanel;
		}
		set
		{
			if (_tutorialPanel != value)
			{
				_tutorialPanel = value;
				OnPropertyChanged(value, "TutorialPanel");
				if (_tutorialPanel != null)
				{
					_tutorialPanel.UseGlobalTimeForAnimation = true;
				}
			}
		}
	}

	public TutorialPanelImageWidget(UIContext context)
		: base(context)
	{
		base.UseGlobalTimeForAnimation = true;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_animState == AnimState.Start)
		{
			_tickCount++;
			if (_tickCount > 20)
			{
				_animState = AnimState.Starting;
			}
		}
		else if (_animState == AnimState.Starting)
		{
			TutorialPanel?.BrushRenderer.RestartAnimation();
			_animState = AnimState.Playing;
		}
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		Initialize();
	}

	private void Initialize()
	{
		if (base.IsDisabled)
		{
			SetState("Disabled");
			_animState = AnimState.Idle;
			_tickCount = 0;
		}
		else if (_animState != AnimState.Start)
		{
			SetState("Default");
			_animState = AnimState.Start;
			base.Context.TwoDimensionContext.PlaySound("panels/tutorial");
		}
		base.IsVisible = base.IsEnabled;
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		Initialize();
	}
}
