using System;

namespace TaleWorlds.DotNet;

public class EditableScriptComponentVariable : Attribute
{
	public bool Visible { get; set; }

	public string OverrideFieldName { get; set; }

	public EditableScriptComponentVariable(bool visible, string overrideFieldName = "")
	{
		Visible = visible;
		OverrideFieldName = overrideFieldName;
	}
}
