using System;
using System.Collections.Generic;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200000D RID: 13
	public class WidgetAttributeContext
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00002CB8 File Offset: 0x00000EB8
		public IEnumerable<WidgetAttributeKeyType> RegisteredKeyTypes
		{
			get
			{
				return this._registeredKeyTypes;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00002CC0 File Offset: 0x00000EC0
		public IEnumerable<WidgetAttributeValueType> RegisteredValueTypes
		{
			get
			{
				return this._registeredValueTypes;
			}
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00002CC8 File Offset: 0x00000EC8
		public WidgetAttributeContext()
		{
			this._registeredKeyTypes = new List<WidgetAttributeKeyType>();
			this._registeredValueTypes = new List<WidgetAttributeValueType>();
			WidgetAttributeKeyTypeId keyType = new WidgetAttributeKeyTypeId();
			WidgetAttributeKeyTypeParameter keyType2 = new WidgetAttributeKeyTypeParameter();
			this._widgetAttributeKeyTypeAttribute = new WidgetAttributeKeyTypeAttribute();
			this.RegisterKeyType(keyType);
			this.RegisterKeyType(keyType2);
			this.RegisterKeyType(this._widgetAttributeKeyTypeAttribute);
			WidgetAttributeValueTypeConstant valueType = new WidgetAttributeValueTypeConstant();
			WidgetAttributeValueTypeParameter valueType2 = new WidgetAttributeValueTypeParameter();
			this._widgetAttributeValueTypeDefault = new WidgetAttributeValueTypeDefault();
			this.RegisterValueType(valueType);
			this.RegisterValueType(valueType2);
			this.RegisterValueType(this._widgetAttributeValueTypeDefault);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002D53 File Offset: 0x00000F53
		public void RegisterKeyType(WidgetAttributeKeyType keyType)
		{
			this._registeredKeyTypes.Add(keyType);
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002D61 File Offset: 0x00000F61
		public void RegisterValueType(WidgetAttributeValueType valueType)
		{
			this._registeredValueTypes.Add(valueType);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002D70 File Offset: 0x00000F70
		public WidgetAttributeKeyType GetKeyType(string key)
		{
			WidgetAttributeKeyType widgetAttributeKeyType = null;
			foreach (WidgetAttributeKeyType widgetAttributeKeyType2 in this._registeredKeyTypes)
			{
				if (!(widgetAttributeKeyType2 is WidgetAttributeKeyTypeAttribute) && widgetAttributeKeyType2.CheckKeyType(key))
				{
					widgetAttributeKeyType = widgetAttributeKeyType2;
				}
			}
			if (widgetAttributeKeyType == null)
			{
				widgetAttributeKeyType = this._widgetAttributeKeyTypeAttribute;
			}
			return widgetAttributeKeyType;
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002DDC File Offset: 0x00000FDC
		public WidgetAttributeValueType GetValueType(string value)
		{
			WidgetAttributeValueType widgetAttributeValueType = null;
			foreach (WidgetAttributeValueType widgetAttributeValueType2 in this._registeredValueTypes)
			{
				if (!(widgetAttributeValueType2 is WidgetAttributeValueTypeDefault) && widgetAttributeValueType2.CheckValueType(value))
				{
					widgetAttributeValueType = widgetAttributeValueType2;
				}
			}
			if (widgetAttributeValueType == null)
			{
				widgetAttributeValueType = this._widgetAttributeValueTypeDefault;
			}
			return widgetAttributeValueType;
		}

		// Token: 0x04000027 RID: 39
		private List<WidgetAttributeKeyType> _registeredKeyTypes;

		// Token: 0x04000028 RID: 40
		private List<WidgetAttributeValueType> _registeredValueTypes;

		// Token: 0x04000029 RID: 41
		private WidgetAttributeKeyTypeAttribute _widgetAttributeKeyTypeAttribute;

		// Token: 0x0400002A RID: 42
		private WidgetAttributeValueTypeDefault _widgetAttributeValueTypeDefault;
	}
}
