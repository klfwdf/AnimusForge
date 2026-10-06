using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptComponentParams("ship_visual_only", "ShipColorAssigner")]
public class ColorAssigner : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "Factor Color")]
	private Color _color = Color.White;

	[EditableScriptComponentVariable(true, "Ram Debris Color")]
	private Color _ramDebrisColor = Color.White;

	[EditableScriptComponentVariable(true, "Set Colors")]
	private SimpleButton _refreshButton = new SimpleButton();

	public Color ShipColor => _color;

	public Color RamDebrisColor => _ramDebrisColor;

	protected internal override void OnInit()
	{
		base.OnInit();
		SetColor(base.GameEntity);
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		SetColor(base.GameEntity);
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		if (variableName == "Set Colors" || variableName == "Factor Color")
		{
			SetColor(base.GameEntity);
		}
	}

	public void SetColor(WeakGameEntity entity)
	{
		entity.SetColorToAllMeshesWithTagRecursive(_color.ToUnsignedInteger(), "auto_factor_color");
	}
}
