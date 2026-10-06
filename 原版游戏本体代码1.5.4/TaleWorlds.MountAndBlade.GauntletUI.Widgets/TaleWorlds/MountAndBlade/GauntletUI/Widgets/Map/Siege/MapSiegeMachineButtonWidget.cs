using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;

public class MapSiegeMachineButtonWidget : ButtonWidget
{
	private Vec2 _orgClipSize = new Vec2(-1f, -1f);

	private bool _machineSpritesUpdated;

	private Widget _coloredImageWidget;

	private bool _isDeploymentTarget;

	private string _machineID;

	[Editor(false)]
	public Widget ColoredImageWidget
	{
		get
		{
			return _coloredImageWidget;
		}
		set
		{
			if (value != _coloredImageWidget)
			{
				_coloredImageWidget = value;
				OnPropertyChanged(value, "ColoredImageWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsDeploymentTarget
	{
		get
		{
			return _isDeploymentTarget;
		}
		set
		{
			if (value != _isDeploymentTarget)
			{
				_isDeploymentTarget = value;
				OnPropertyChanged(value, "IsDeploymentTarget");
			}
		}
	}

	[Editor(false)]
	public string MachineID
	{
		get
		{
			return _machineID;
		}
		set
		{
			if (value != _machineID)
			{
				_machineID = value;
				OnPropertyChanged(value, "MachineID");
				_machineSpritesUpdated = false;
			}
		}
	}

	public MapSiegeMachineButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (ColoredImageWidget != null && !_machineSpritesUpdated)
		{
			SetStylesSprite(ColoredImageWidget, "SPGeneral\\Siege\\" + MachineID);
			_machineSpritesUpdated = true;
		}
	}

	private void SetStylesSprite(Widget widget, string spriteName)
	{
		widget.Sprite = base.Context.SpriteData.GetSprite(spriteName);
	}
}
