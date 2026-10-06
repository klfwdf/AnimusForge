using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class EncyclopediaTroopScrollablePanel : ScrollablePanel
{
	private bool _isDragging;

	public bool PanWithMouseEnabled { get; set; }

	public EncyclopediaTroopScrollablePanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (PanWithMouseEnabled)
		{
			bool flag = IsMouseOverWidget(this);
			if (flag)
			{
				List<Widget> allChildrenAndThisRecursive = GetAllChildrenAndThisRecursive();
				for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
				{
					if (IsMouseOverWidget(allChildrenAndThisRecursive[i]) && allChildrenAndThisRecursive[i] is ButtonWidget)
					{
						flag = false;
					}
				}
			}
			if (flag && base.HorizontalScrollbar != null && _canScrollHorizontal)
			{
				SetActiveCursor(UIContext.MouseCursors.Move);
				if (Input.IsKeyPressed(InputKey.LeftMouseButton))
				{
					_isDragging = true;
				}
			}
		}
		if (Input.IsKeyReleased(InputKey.LeftMouseButton))
		{
			_isDragging = false;
		}
		if (_isDragging)
		{
			base.HorizontalScrollbar.ValueFloat -= Input.MouseMoveX;
			base.VerticalScrollbar.ValueFloat -= Input.MouseMoveY;
		}
	}

	private bool IsMouseOverWidget(Widget widget)
	{
		if (widget.GlobalPosition.X <= Input.MousePositionPixel.X && Input.MousePositionPixel.X <= widget.GlobalPosition.X + widget.Size.X && widget.GlobalPosition.Y <= Input.MousePositionPixel.Y)
		{
			return Input.MousePositionPixel.Y <= widget.GlobalPosition.Y + widget.Size.Y;
		}
		return false;
	}
}
