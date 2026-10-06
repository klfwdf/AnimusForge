using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingMaterialVisualBrushWidget : BrushWidget
{
	private bool _visualDirty = true;

	private string _materialType;

	private bool _isBig;

	public string MaterialType
	{
		get
		{
			return _materialType;
		}
		set
		{
			if (_materialType != value)
			{
				_materialType = value;
				_visualDirty = true;
			}
		}
	}

	public bool IsBig
	{
		get
		{
			return _isBig;
		}
		set
		{
			if (_isBig != value)
			{
				_isBig = value;
				_visualDirty = true;
			}
		}
	}

	public CraftingMaterialVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_visualDirty)
		{
			UpdateVisual();
			_visualDirty = false;
		}
	}

	private void UpdateVisual()
	{
		this.RegisterBrushStatesOfWidget();
		string text = MaterialType;
		if (IsBig)
		{
			text += "Big";
		}
		SetState(text);
	}
}
