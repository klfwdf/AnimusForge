using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IScriptComponent
{
	[EngineMethod("get_script_component_behavior", false, null, false)]
	ScriptComponentBehavior GetScriptComponentBehavior(UIntPtr pointer);

	[EngineMethod("set_variable_editor_widget_status", false, null, false)]
	void SetVariableEditorWidgetStatus(UIntPtr pointer, string field, bool enabled);

	[EngineMethod("set_variable_editor_widget_value", false, null, false)]
	void SetVariableEditorWidgetValue(UIntPtr pointer, string field, RglScriptFieldType fieldType, double value);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr pointer);
}
