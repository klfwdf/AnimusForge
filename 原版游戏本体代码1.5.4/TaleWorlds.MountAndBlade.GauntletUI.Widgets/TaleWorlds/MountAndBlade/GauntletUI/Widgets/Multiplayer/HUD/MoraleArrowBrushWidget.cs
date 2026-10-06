using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class MoraleArrowBrushWidget : BrushWidget
{
	private enum AnimStates
	{
		FadeIn,
		Move,
		FadeOut,
		GoToInitPos
	}

	private float _timeSinceCreation;

	private bool _initialized;

	private int _currentFlow;

	private AnimStates _currentAnimState;

	public bool LeftSideArrow { get; set; }

	public float BaseHorizontalExtendRange => 3.3f;

	private float BaseSpeedModifier => 13f;

	public bool AreMoralesIndependent { get; set; }

	public MoraleArrowBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			base.Brush.GlobalAlphaFactor = 0f;
			_initialized = true;
		}
		base.IsVisible = _currentFlow > 0 && !AreMoralesIndependent;
		if (base.IsVisible)
		{
			float num = BaseSpeedModifier * (float)Math.Sqrt(_currentFlow);
			float num2 = BaseHorizontalExtendRange * (float)_currentFlow;
			if (_currentAnimState == AnimStates.FadeIn)
			{
				if (base.ReadOnlyBrush.GlobalAlphaFactor < 1f)
				{
					this.SetGlobalAlphaRecursively(Mathf.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, 1f, dt * num));
				}
				if ((double)base.ReadOnlyBrush.GlobalAlphaFactor >= 0.99)
				{
					_currentAnimState = AnimStates.Move;
				}
			}
			else if (_currentAnimState == AnimStates.Move)
			{
				if (Math.Abs(base.PositionXOffset) < num2)
				{
					int num3 = ((!LeftSideArrow) ? 1 : (-1));
					base.PositionXOffset = Mathf.Lerp(base.PositionXOffset, num2 * (float)num3, dt * num);
				}
				if ((double)Math.Abs(base.PositionXOffset) >= (double)num2 - 0.01)
				{
					_currentAnimState = AnimStates.FadeOut;
				}
			}
			else if (_currentAnimState == AnimStates.FadeOut)
			{
				if (base.ReadOnlyBrush.GlobalAlphaFactor > 0f)
				{
					this.SetGlobalAlphaRecursively(Mathf.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, 0f, dt * num));
				}
				if ((double)base.ReadOnlyBrush.GlobalAlphaFactor <= 0.01)
				{
					_currentAnimState = AnimStates.GoToInitPos;
				}
			}
			else
			{
				base.PositionXOffset = 0f;
				_currentAnimState = AnimStates.FadeIn;
			}
		}
		else
		{
			base.PositionXOffset = 0f;
			_currentAnimState = AnimStates.FadeIn;
		}
		_timeSinceCreation += dt;
	}

	public void SetFlowLevel(int flow)
	{
		_currentFlow = flow;
		base.IsVisible = _currentFlow > 0 && !AreMoralesIndependent;
	}
}
