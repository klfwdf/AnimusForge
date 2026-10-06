using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class EquipmentTypeVisualBrushWidget : BrushWidget
{
	private bool _hasVisualDetermined;

	private string _type = "";

	[Editor(false)]
	public string Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
			}
		}
	}

	public EquipmentTypeVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_hasVisualDetermined)
		{
			this.RegisterBrushStatesOfWidget();
			UpdateVisual(Type);
			_hasVisualDetermined = true;
		}
	}

	private void UpdateVisual(string type)
	{
		if (ContainsState(type))
		{
			SetState(type);
		}
		else
		{
			SetState("Invalid");
		}
	}
}
