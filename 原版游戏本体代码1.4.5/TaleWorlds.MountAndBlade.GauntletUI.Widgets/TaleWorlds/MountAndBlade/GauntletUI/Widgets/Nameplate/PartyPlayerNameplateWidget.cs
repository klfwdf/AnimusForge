using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

public class PartyPlayerNameplateWidget : PartyNameplateWidget
{
	private bool _isPrisoner;

	private Widget _mainPartyArrowWidget;

	public bool IsPrisoner
	{
		get
		{
			return _isPrisoner;
		}
		set
		{
			if (_isPrisoner != value)
			{
				_isPrisoner = value;
				OnPropertyChanged(value, "IsPrisoner");
			}
		}
	}

	public Widget MainPartyArrowWidget
	{
		get
		{
			return _mainPartyArrowWidget;
		}
		set
		{
			if (_mainPartyArrowWidget != value)
			{
				_mainPartyArrowWidget = value;
				OnPropertyChanged(value, "MainPartyArrowWidget");
			}
		}
	}

	public PartyPlayerNameplateWidget(UIContext context)
		: base(context)
	{
	}

	protected override void UpdateNameplatesVisibility(float dt)
	{
		float num = 0f;
		float num2 = 0f;
		bool flag = IsPositionOutsideScreen() || base.IsBehind || base.IsHigh;
		bool flag2 = flag || base.IsInArmy || IsPrisoner || base.IsInSettlement;
		base.NameplateTextWidget.IsVisible = !flag2;
		base.NameplateFullNameTextWidget.IsVisible = !flag2;
		base.SpeedTextWidget.IsVisible = !flag2;
		base.SpeedIconWidget.IsVisible = !flag2;
		base.PartyBannerWidget.IsVisible = !flag2;
		base.NameplateExtraInfoTextWidget.IsVisible = !flag2;
		base.DisorganizedWidget.IsVisible = !flag2 && base.IsDisorganized;
		num = ((!flag2) ? 1 : 0);
		MainPartyArrowWidget.IsVisible = flag;
		base.TrackerFrame.IsVisible = flag;
		base.IsEnabled = flag;
		if (_initialDelayAmount <= 0f)
		{
			num2 = (base.ShouldShowFullName ? 1 : 0);
		}
		else
		{
			_initialDelayAmount -= dt;
			num2 = 1f;
		}
		base.NameplateTextWidget.Brush.GlobalAlphaFactor = TaleWorlds.Library.MathF.Lerp(base.NameplateTextWidget.ReadOnlyBrush.GlobalAlphaFactor, num, dt * base._animSpeedModifier);
		base.NameplateFullNameTextWidget.Brush.GlobalAlphaFactor = TaleWorlds.Library.MathF.Lerp(base.NameplateFullNameTextWidget.ReadOnlyBrush.GlobalAlphaFactor, num2, dt * base._animSpeedModifier);
		base.SpeedTextWidget.Brush.GlobalAlphaFactor = TaleWorlds.Library.MathF.Lerp(base.SpeedTextWidget.ReadOnlyBrush.GlobalAlphaFactor, num2, dt * base._animSpeedModifier);
		float alphaFactor = TaleWorlds.Library.MathF.Lerp(base.SpeedIconWidget.AlphaFactor, num2, dt * base._animSpeedModifier);
		base.SpeedIconWidget.SetGlobalAlphaRecursively(alphaFactor);
		base.NameplateExtraInfoTextWidget.Brush.GlobalAlphaFactor = TaleWorlds.Library.MathF.Lerp(base.NameplateExtraInfoTextWidget.ReadOnlyBrush.GlobalAlphaFactor, base.ShouldShowFullName ? 1 : 0, dt * base._animSpeedModifier);
		base.PartyBannerWidget.Brush.GlobalAlphaFactor = TaleWorlds.Library.MathF.Lerp(base.PartyBannerWidget.ReadOnlyBrush.GlobalAlphaFactor, num, dt * base._animSpeedModifier);
		base.ParleyIconWidget.AlphaFactor = TaleWorlds.Library.MathF.Lerp(base.ParleyIconWidget.AlphaFactor, base.CanParley ? 1 : 0, dt * base._animSpeedModifier);
	}

	protected override void UpdateNameplatesScreenPosition()
	{
		_screenWidth = base.Context.EventManager.PageSize.X;
		_screenHeight = base.Context.EventManager.PageSize.Y;
		bool num = base.IsBehind || IsPositionOutsideScreen();
		bool flag = base.IsHigh || base.IsBehind || IsPositionOutsideScreen();
		if (num)
		{
			Vec2 vec = new Vec2(_screenWidth / 2f, _screenHeight / 2f);
			Vec2 headPosition = base.HeadPosition;
			headPosition -= vec;
			if (base.IsBehind)
			{
				headPosition *= -1f;
			}
			float radian = Mathf.Atan2(headPosition.y, headPosition.x) - System.MathF.PI / 2f;
			float num2 = Mathf.Cos(radian);
			float num3 = Mathf.Sin(radian);
			float num4 = num2 / num3;
			Vec2 vec2 = vec * 1f;
			headPosition = ((num2 > 0f) ? new Vec2((0f - vec2.y) / num4, vec.y) : new Vec2(vec2.y / num4, 0f - vec.y));
			if (headPosition.x > vec2.x)
			{
				headPosition = new Vec2(vec2.x, (0f - vec2.x) * num4);
			}
			else if (headPosition.x < 0f - vec2.x)
			{
				headPosition = new Vec2(0f - vec2.x, vec2.x * num4);
			}
			headPosition += vec;
			base.ScaledPositionXOffset = headPosition.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = headPosition.y - base.Size.Y / 2f;
		}
		else
		{
			float num5 = base.HeadGroupWidget?.Size.Y ?? 0f;
			base.NameplateLayoutListPanel.ScaledPositionXOffset = base.Size.X / 2f - base.PartyBannerWidget.Size.X;
			base.NameplateLayoutListPanel.ScaledPositionYOffset = base.Position.y - base.HeadPosition.y + num5;
			base.ScaledPositionXOffset = base.HeadPosition.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = base.HeadPosition.y - num5;
		}
		if (flag)
		{
			base.ScaledPositionXOffset = TaleWorlds.Library.MathF.Clamp(base.ScaledPositionXOffset, 0f, _screenWidth - base.Size.X);
			base.ScaledPositionYOffset = TaleWorlds.Library.MathF.Clamp(base.ScaledPositionYOffset, 0f, _screenHeight - base.Size.Y);
		}
	}
}
