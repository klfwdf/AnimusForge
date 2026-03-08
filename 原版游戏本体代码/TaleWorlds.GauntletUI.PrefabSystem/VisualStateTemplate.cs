using System;
using System.Collections.Generic;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200000C RID: 12
	public class VisualStateTemplate
	{
		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00002BB8 File Offset: 0x00000DB8
		// (set) Token: 0x06000053 RID: 83 RVA: 0x00002BC0 File Offset: 0x00000DC0
		public string State { get; set; }

		// Token: 0x06000054 RID: 84 RVA: 0x00002BC9 File Offset: 0x00000DC9
		public VisualStateTemplate()
		{
			this._attributes = new Dictionary<string, string>();
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00002BDC File Offset: 0x00000DDC
		public void SetAttribute(string name, string value)
		{
			if (this._attributes.ContainsKey(name))
			{
				this._attributes[name] = value;
				return;
			}
			this._attributes.Add(name, value);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002C07 File Offset: 0x00000E07
		public Dictionary<string, string> GetAttributes()
		{
			return this._attributes;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00002C0F File Offset: 0x00000E0F
		public void ClearAttribute(string name)
		{
			if (this._attributes.ContainsKey(name))
			{
				this._attributes.Remove(name);
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002C2C File Offset: 0x00000E2C
		public VisualState CreateVisualState(BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
		{
			VisualState visualState = new VisualState(this.State);
			foreach (KeyValuePair<string, string> keyValuePair in this._attributes)
			{
				string key = keyValuePair.Key;
				string actualValueOf = ConstantDefinition.GetActualValueOf(keyValuePair.Value, brushFactory, spriteData, constants, parameters, defaultParameters);
				WidgetExtensions.SetWidgetAttributeFromString(visualState, key, actualValueOf, brushFactory, spriteData, visualDefinitionTemplates, constants, parameters, null, defaultParameters);
			}
			return visualState;
		}

		// Token: 0x04000026 RID: 38
		private Dictionary<string, string> _attributes;
	}
}
