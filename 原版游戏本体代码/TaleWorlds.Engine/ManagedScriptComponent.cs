using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200005B RID: 91
	[EngineClass("rglManaged_script_component")]
	public sealed class ManagedScriptComponent : ScriptComponent
	{
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000908 RID: 2312 RVA: 0x0000808D File Offset: 0x0000628D
		public ScriptComponentBehavior ScriptComponentBehavior
		{
			get
			{
				return EngineApplicationInterface.IScriptComponent.GetScriptComponentBehavior(base.Pointer);
			}
		}

		// Token: 0x06000909 RID: 2313 RVA: 0x0000809F File Offset: 0x0000629F
		public void SetVariableEditorWidgetStatus(string field, bool enabled)
		{
			EngineApplicationInterface.IScriptComponent.SetVariableEditorWidgetStatus(base.Pointer, field, enabled);
		}

		// Token: 0x0600090A RID: 2314 RVA: 0x000080B3 File Offset: 0x000062B3
		public void SetVariableEditorWidgetValue(string field, RglScriptFieldType fieldType, double value)
		{
			EngineApplicationInterface.IScriptComponent.SetVariableEditorWidgetValue(base.Pointer, field, fieldType, value);
		}

		// Token: 0x0600090B RID: 2315 RVA: 0x000080C8 File Offset: 0x000062C8
		private ManagedScriptComponent()
		{
		}

		// Token: 0x0600090C RID: 2316 RVA: 0x000080D0 File Offset: 0x000062D0
		internal ManagedScriptComponent(UIntPtr pointer)
			: base(pointer)
		{
		}
	}
}
