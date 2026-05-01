using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

public class AutoHeightSyncWidget : Widget
{
	private bool _isLayoutDirty;

	private int _paddingAmount;

	public AutoHeightSyncWidget(UIContext context)
		: base(context)
	{
		base.EventManager.AddLateUpdateAction(this, new Action<float>(UpdateDimensions), 5);
	}

	public int PaddingAmount
	{
		get
		{
			return _paddingAmount;
		}
		set
		{
			if (_paddingAmount != value)
			{
				_paddingAmount = value;
			}
		}
	}

	protected override void OnLayoutUpdated()
	{
		base.OnLayoutUpdated();
		_isLayoutDirty = true;
		base.ScaledSuggestedHeight = 0f;
	}

	private void UpdateDimensions(float dt)
	{
		try
		{
			Widget widget = GetContentChild();
			if (widget != null)
			{
				if (_isLayoutDirty)
				{
					if (base.IsRecursivelyVisible())
					{
						_isLayoutDirty = false;
					}
				}
				else
				{
					base.ScaledSuggestedHeight = widget.Size.Y + (float)_paddingAmount * base._scaleToUse;
				}
			}
		}
		finally
		{
			base.EventManager.AddLateUpdateAction(this, new Action<float>(UpdateDimensions), 5);
		}
	}

	private Widget GetContentChild()
	{
		for (int num = base.ChildCount - 1; num >= 0; num--)
		{
			Widget child = base.GetChild(num);
			if (child != null && child.IsVisible)
			{
				return child;
			}
		}
		return null;
	}
}
