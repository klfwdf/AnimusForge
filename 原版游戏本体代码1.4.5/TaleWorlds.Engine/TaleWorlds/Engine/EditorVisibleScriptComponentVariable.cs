using System;

namespace TaleWorlds.Engine;

public class EditorVisibleScriptComponentVariable : Attribute
{
	public bool Visible { get; set; }

	public EditorVisibleScriptComponentVariable(bool visible)
	{
		Visible = visible;
	}
}
