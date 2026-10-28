using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Quest;

public class QuestStageItemWidget : Widget
{
	private bool _firstFrame;

	private bool _previousHoverBegan;

	private bool _hoverBegan;

	private bool _isNew;

	[Editor(false)]
	public bool IsNew
	{
		get
		{
			return _isNew;
		}
		set
		{
			if (_isNew != value)
			{
				_isNew = value;
				OnPropertyChanged(value, "IsNew");
			}
		}
	}

	public QuestStageItemWidget(UIContext context)
		: base(context)
	{
		_firstFrame = true;
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		_previousHoverBegan = _hoverBegan;
		if (!_firstFrame && IsNew)
		{
			bool flag = IsMouseOverWidget();
			if (flag && !_hoverBegan)
			{
				_hoverBegan = true;
			}
			else if (!flag && _hoverBegan)
			{
				_hoverBegan = false;
			}
		}
		_firstFrame = false;
		if (_previousHoverBegan && !_hoverBegan)
		{
			EventFired("ResetGlow");
		}
	}

	private bool IsMouseOverWidget()
	{
		Vector2 globalPosition = base.GlobalPosition;
		if (IsBetween(base.EventManager.MousePosition.X, globalPosition.X, globalPosition.X + base.Size.X))
		{
			return IsBetween(base.EventManager.MousePosition.Y, globalPosition.Y, globalPosition.Y + base.Size.Y);
		}
		return false;
	}

	private bool IsBetween(float number, float min, float max)
	{
		if (number >= min)
		{
			return number <= max;
		}
		return false;
	}
}
