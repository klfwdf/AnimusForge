using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class SkillPointsContainerListPanel : ListPanel
{
	private bool _initialized;

	private int _currentFocusLevel;

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

	public SkillPointsContainerListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		for (int i = 0; i < base.ChildCount; i++)
		{
			if (!_initialized)
			{
				GetChild(i).RegisterBrushStatesOfWidget();
			}
			bool flag = CurrentFocusLevel >= i + 1;
			GetChild(i).SetState(flag ? "Full" : "Empty");
		}
		_initialized = true;
	}
}
