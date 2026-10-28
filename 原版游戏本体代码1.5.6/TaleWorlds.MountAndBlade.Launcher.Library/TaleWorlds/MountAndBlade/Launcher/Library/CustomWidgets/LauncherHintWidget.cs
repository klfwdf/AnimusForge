using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;

public class LauncherHintWidget : Widget
{
	private int _frame;

	private bool _prevIsActive;

	private Vector2 _tooltipPosition;

	private bool _isActive;

	private float TooltipOffset => 30f;

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
			}
		}
	}

	public LauncherHintWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateAlpha(dt);
		if (IsActive)
		{
			if (!_prevIsActive)
			{
				_frame = 0;
			}
			UpdatePosition();
		}
		_prevIsActive = IsActive;
	}

	private void UpdatePosition()
	{
		Vector2 vector;
		if (_frame < 3)
		{
			_tooltipPosition = base.EventManager.MousePosition;
			vector = new Vector2(-2000f, -2000f);
		}
		else
		{
			vector = _tooltipPosition;
		}
		_frame++;
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
		VerticalAlignment verticalAlignment = VerticalAlignment.Center;
		float num = 0f;
		float num2 = 0f;
		if ((double)vector.X < (double)base.EventManager.PageSize.X * 0.5)
		{
			horizontalAlignment = HorizontalAlignment.Left;
			num = TooltipOffset;
		}
		else
		{
			horizontalAlignment = HorizontalAlignment.Right;
			num -= 0f;
			vector = new Vector2(0f - (base.EventManager.PageSize.X - vector.X), vector.Y);
		}
		if ((double)vector.Y < (double)base.EventManager.PageSize.Y * 0.5)
		{
			verticalAlignment = VerticalAlignment.Top;
			num2 = TooltipOffset;
		}
		else
		{
			verticalAlignment = VerticalAlignment.Bottom;
			num2 = 0f;
			vector = new Vector2(vector.X, 0f - (base.EventManager.PageSize.Y - vector.Y));
		}
		vector += new Vector2(num, num2);
		if (_frame > 3)
		{
			if (base.Size.Y > base.EventManager.PageSize.Y)
			{
				verticalAlignment = VerticalAlignment.Center;
				vector = new Vector2(vector.X, 0f);
			}
			else
			{
				if (verticalAlignment == VerticalAlignment.Top && vector.Y + base.Size.Y > base.EventManager.PageSize.Y)
				{
					vector += new Vector2(0f, 0f - (vector.Y + base.Size.Y - base.EventManager.PageSize.Y));
				}
				if (verticalAlignment == VerticalAlignment.Bottom && vector.Y - base.Size.Y + base.EventManager.PageSize.Y < 0f)
				{
					vector += new Vector2(0f, 0f - (vector.Y - base.Size.Y + base.EventManager.PageSize.Y));
				}
			}
		}
		base.HorizontalAlignment = horizontalAlignment;
		base.VerticalAlignment = verticalAlignment;
		base.ScaledPositionXOffset = vector.X;
		base.ScaledPositionYOffset = vector.Y;
	}

	private void UpdateAlpha(float dt)
	{
		float alphaFactor = MathF.Lerp(valueTo: (!IsActive) ? 0f : 1f, valueFrom: base.AlphaFactor, amount: dt * 20f);
		this.SetGlobalAlphaRecursively(alphaFactor);
	}
}
