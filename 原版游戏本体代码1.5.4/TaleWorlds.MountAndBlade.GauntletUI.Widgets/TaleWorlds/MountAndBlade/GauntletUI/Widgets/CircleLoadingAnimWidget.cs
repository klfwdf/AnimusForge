using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class CircleLoadingAnimWidget : Widget
{
	public enum VisualState
	{
		FadeIn,
		Animating,
		FadeOut
	}

	private VisualState _visualState;

	private float _stayStartTime;

	private bool _initialized;

	private float _totalTime;

	private bool _isChildPositionsDirty;

	public float NumOfCirclesInASecond { get; set; } = 0.5f;

	public float FullAlpha { get; set; } = 1f;

	public float CircleRadius { get; set; } = 50f;

	public float StaySeconds { get; set; } = 2f;

	public float FadeInSeconds { get; set; } = 0.2f;

	public float FadeOutSeconds { get; set; } = 0.2f;

	public CircleLoadingAnimWidget(UIContext context)
		: base(context)
	{
		_isChildPositionsDirty = true;
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		_isChildPositionsDirty = true;
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		base.OnAfterChildRemoved(child, previousIndexOfChild);
		_isChildPositionsDirty = true;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			_visualState = VisualState.FadeIn;
			this.SetGlobalAlphaRecursively(0f);
			_initialized = true;
		}
		_totalTime += dt;
		if (_isChildPositionsDirty)
		{
			float num = 360f / (float)base.Children.Count;
			float num2 = 0f;
			for (int i = 0; i < base.Children.Count; i++)
			{
				float positionXOffset = TaleWorlds.Library.MathF.Cos(num2 * (System.MathF.PI / 180f)) * CircleRadius;
				float positionYOffset = TaleWorlds.Library.MathF.Sin(num2 * (System.MathF.PI / 180f)) * CircleRadius;
				base.Children[i].PositionXOffset = positionXOffset;
				base.Children[i].PositionYOffset = positionYOffset;
				num2 += num;
				num2 %= 360f;
			}
			_isChildPositionsDirty = false;
		}
		if (IsRecursivelyVisible())
		{
			base.Rotation += dt * 360f * NumOfCirclesInASecond;
			UpdateAlphaValues(dt);
		}
	}

	private void UpdateAlphaValues(float dt)
	{
		float alphaFactor = 1f;
		if (_visualState == VisualState.FadeIn)
		{
			alphaFactor = Mathf.Lerp(base.AlphaFactor, 1f, dt / FadeInSeconds);
			if (base.AlphaFactor >= 0.9f)
			{
				_visualState = VisualState.Animating;
				_stayStartTime = _totalTime;
			}
		}
		else if (_visualState == VisualState.Animating)
		{
			alphaFactor = 1f;
			if (StaySeconds != -1f && _totalTime - _stayStartTime > StaySeconds)
			{
				_visualState = VisualState.FadeOut;
			}
		}
		else if (_visualState == VisualState.FadeOut)
		{
			alphaFactor = Mathf.Lerp(base.AlphaFactor, 0f, dt / FadeOutSeconds);
			if (base.AlphaFactor <= 0.01f && _totalTime - (_stayStartTime + StaySeconds + FadeOutSeconds) > 3f)
			{
				_visualState = VisualState.FadeIn;
			}
		}
		else
		{
			Debug.FailedAssert("This visual state is not enabled", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\CircleLoadingAnimWidget.cs", "UpdateAlphaValues", 122);
		}
		this.SetGlobalAlphaRecursively(alphaFactor);
	}
}
