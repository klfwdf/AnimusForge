using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class MultiplayerDeathCardWidget : Widget
{
	private float _targetAlpha;

	private float _initAlpha;

	private float _activeTimeStart;

	private bool _initialized;

	private bool _isActive;

	private bool _isSelfInflicted;

	private bool _killCountsEnabled;

	public TextWidget WeaponTextWidget { get; set; }

	public TextWidget TitleTextWidget { get; set; }

	public ScrollingRichTextWidget KillerNameTextWidget { get; set; }

	public Widget KillCountContainer { get; set; }

	public Brush SelfInflictedTitleBrush { get; set; }

	public Brush NormalBrushTitleBrush { get; set; }

	public float FadeInModifier { get; set; } = 2f;

	public float FadeOutModifier { get; set; } = 10f;

	public float StayTime { get; set; } = 7f;

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
				HandleIsActiveToggle(value);
			}
		}
	}

	public bool IsSelfInflicted
	{
		get
		{
			return _isSelfInflicted;
		}
		set
		{
			if (value != _isSelfInflicted)
			{
				_isSelfInflicted = value;
				OnPropertyChanged(value, "IsSelfInflicted");
				HandleSelfInflictedToggle(value);
			}
		}
	}

	public bool KillCountsEnabled
	{
		get
		{
			return _killCountsEnabled;
		}
		set
		{
			if (value != _killCountsEnabled)
			{
				_killCountsEnabled = value;
				OnPropertyChanged(value, "KillCountsEnabled");
				HandleKillCountsEnabledSwitch(value);
			}
		}
	}

	public MultiplayerDeathCardWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			_initialized = true;
			base.IsEnabled = false;
			_initAlpha = base.AlphaFactor;
			this.SetGlobalAlphaRecursively(_targetAlpha);
		}
		if (Math.Abs(base.AlphaFactor - _targetAlpha) > float.Epsilon)
		{
			float num = ((base.AlphaFactor > _targetAlpha) ? FadeOutModifier : FadeInModifier);
			float alphaFactor = Mathf.Lerp(base.AlphaFactor, _targetAlpha, dt * num);
			this.SetGlobalAlphaRecursively(alphaFactor);
		}
		if ((IsActive && base.AlphaFactor < float.Epsilon) || base.Context.EventManager.Time - _activeTimeStart > StayTime)
		{
			IsActive = false;
		}
	}

	private void HandleIsActiveToggle(bool isActive)
	{
		_targetAlpha = (isActive ? 1f : 0f);
		if (isActive)
		{
			_activeTimeStart = base.Context.EventManager.Time;
		}
		KillCountContainer.IsVisible = !IsSelfInflicted && KillCountsEnabled;
	}

	private void HandleSelfInflictedToggle(bool isSelfInflicted)
	{
		TitleTextWidget.IsVisible = true;
		TitleTextWidget.Brush = (isSelfInflicted ? SelfInflictedTitleBrush : NormalBrushTitleBrush);
		KillerNameTextWidget.IsVisible = !isSelfInflicted;
		WeaponTextWidget.IsVisible = !isSelfInflicted;
		KillCountContainer.IsVisible = !IsSelfInflicted && KillCountsEnabled;
	}

	private void HandleKillCountsEnabledSwitch(bool killCountsEnabled)
	{
		KillCountContainer.IsVisible = killCountsEnabled;
	}
}
