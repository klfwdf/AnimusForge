using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class ServerStatusItemBrushWidget : BrushWidget
{
	private float _currentAlphaTarget;

	private bool _initialized;

	private int _status = -1;

	public int Status
	{
		get
		{
			return _status;
		}
		set
		{
			if (value != _status)
			{
				_status = value;
				OnPropertyChanged(value, "Status");
				OnStatusChange(value);
			}
		}
	}

	public ServerStatusItemBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			this.RegisterBrushStatesOfWidget();
			_initialized = true;
			OnStatusChange(Status);
		}
		if (Math.Abs(base.ReadOnlyBrush.GlobalAlphaFactor - _currentAlphaTarget) > 0.001f)
		{
			this.SetGlobalAlphaRecursively(TaleWorlds.Library.MathF.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, _currentAlphaTarget, dt * 5f));
		}
	}

	private void OnStatusChange(int value)
	{
		SetState(value.ToString());
		switch (value)
		{
		case 0:
			_currentAlphaTarget = 0f;
			break;
		case 1:
		case 2:
			_currentAlphaTarget = 1f;
			break;
		}
	}
}
