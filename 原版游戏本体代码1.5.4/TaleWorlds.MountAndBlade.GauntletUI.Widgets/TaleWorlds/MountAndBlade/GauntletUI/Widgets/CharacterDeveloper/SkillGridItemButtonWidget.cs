using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class SkillGridItemButtonWidget : ButtonWidget
{
	private bool _isVisualsDirty = true;

	private Widget _focusLevelWidget;

	private int _currentFocusLevel;

	private bool _canLearnSkill;

	public Brush CannotLearnBrush { get; set; }

	public Brush CanLearnBrush { get; set; }

	public Widget FocusLevelWidget
	{
		get
		{
			return _focusLevelWidget;
		}
		set
		{
			if (_focusLevelWidget != value)
			{
				_focusLevelWidget = value;
				OnPropertyChanged(value, "FocusLevelWidget");
			}
		}
	}

	public bool CanLearnSkill
	{
		get
		{
			return _canLearnSkill;
		}
		set
		{
			if (_canLearnSkill != value)
			{
				_canLearnSkill = value;
				OnPropertyChanged(value, "CanLearnSkill");
				_isVisualsDirty = true;
			}
		}
	}

	public int CurrentFocusLevel
	{
		get
		{
			return _currentFocusLevel;
		}
		set
		{
			if (_currentFocusLevel != value)
			{
				_currentFocusLevel = value;
				OnPropertyChanged(value, "CurrentFocusLevel");
			}
		}
	}

	public SkillGridItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		FocusLevelWidget?.SetState(CurrentFocusLevel.ToString());
		if (_isVisualsDirty)
		{
			base.Brush = (CanLearnSkill ? CanLearnBrush : CannotLearnBrush);
			_isVisualsDirty = false;
		}
	}
}
