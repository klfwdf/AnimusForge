using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000018 RID: 24
	public class PropertyBasedTooltipVM : TooltipBaseVM
	{
		// Token: 0x0600012C RID: 300 RVA: 0x00004793 File Offset: 0x00002993
		public PropertyBasedTooltipVM(Type invokedType, object[] invokedArgs)
			: base(invokedType, invokedArgs)
		{
			this.TooltipPropertyList = new MBBindingList<TooltipProperty>();
			this._isPeriodicRefreshEnabled = true;
			this._periodicRefreshDelay = 2f;
			this.Refresh();
		}

		// Token: 0x0600012D RID: 301 RVA: 0x000047C0 File Offset: 0x000029C0
		protected override void OnFinalizeInternal()
		{
			base.IsActive = false;
			this.TooltipPropertyList.Clear();
		}

		// Token: 0x0600012E RID: 302 RVA: 0x000047D4 File Offset: 0x000029D4
		public static void AddKeyType(string keyID, Func<string> getKeyText)
		{
			PropertyBasedTooltipVM._keyTextGetters.Add(keyID, getKeyText);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x000047E2 File Offset: 0x000029E2
		public string GetKeyText(string keyID)
		{
			if (PropertyBasedTooltipVM._keyTextGetters.ContainsKey(keyID))
			{
				return PropertyBasedTooltipVM._keyTextGetters[keyID]();
			}
			return "";
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00004808 File Offset: 0x00002A08
		protected override void OnPeriodicRefresh()
		{
			base.OnPeriodicRefresh();
			foreach (TooltipProperty tooltipProperty in this.TooltipPropertyList)
			{
				tooltipProperty.RefreshDefinition();
				tooltipProperty.RefreshValue();
			}
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00004860 File Offset: 0x00002A60
		protected override void OnIsExtendedChanged()
		{
			if (base.IsActive)
			{
				base.IsActive = false;
				this.TooltipPropertyList.Clear();
				this.Refresh();
			}
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00004882 File Offset: 0x00002A82
		private void Refresh()
		{
			base.InvokeRefreshData<PropertyBasedTooltipVM>(this);
			if (this.TooltipPropertyList.Count > 0)
			{
				base.IsActive = true;
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000048A0 File Offset: 0x00002AA0
		public static void RefreshGenericPropertyBasedTooltip(PropertyBasedTooltipVM propertyBasedTooltip, object[] args)
		{
			IEnumerable<TooltipProperty> source = args[0] as List<TooltipProperty>;
			propertyBasedTooltip.Mode = 0;
			Func<TooltipProperty, bool> <>9__0;
			Func<TooltipProperty, bool> predicate;
			if ((predicate = <>9__0) == null)
			{
				predicate = (<>9__0 = (TooltipProperty p) => (p.OnlyShowWhenExtended && propertyBasedTooltip.IsExtended) || (!p.OnlyShowWhenExtended && p.OnlyShowWhenNotExtended && !propertyBasedTooltip.IsExtended) || (!p.OnlyShowWhenExtended && !p.OnlyShowWhenNotExtended));
			}
			foreach (TooltipProperty property in source.Where(predicate))
			{
				propertyBasedTooltip.AddPropertyDuplicate(property);
			}
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00004934 File Offset: 0x00002B34
		public void AddProperty(string definition, string value, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000495C File Offset: 0x00002B5C
		public void AddModifierProperty(string definition, int modifierValue, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			string value = ((modifierValue > 0) ? ("+" + modifierValue.ToString()) : modifierValue.ToString());
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x06000136 RID: 310 RVA: 0x000049A0 File Offset: 0x00002BA0
		public void AddProperty(string definition, Func<string> value, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x000049C8 File Offset: 0x00002BC8
		public void AddProperty(Func<string> definition, Func<string> value, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x06000138 RID: 312 RVA: 0x000049F0 File Offset: 0x00002BF0
		public void AddColoredProperty(string definition, string value, Color color, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			if (color == Colors.Black)
			{
				this.AddProperty(definition, value, textHeight, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, color, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00004A30 File Offset: 0x00002C30
		public void AddColoredProperty(string definition, Func<string> value, Color color, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			if (color == Colors.Black)
			{
				this.AddProperty(definition, value, textHeight, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, color, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00004A70 File Offset: 0x00002C70
		public void AddColoredProperty(Func<string> definition, Func<string> value, Color color, int textHeight = 0, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			if (color == Colors.Black)
			{
				this.AddProperty(definition, value, textHeight, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			TooltipProperty item = new TooltipProperty(definition, value, textHeight, color, false, propertyFlags);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00004AB0 File Offset: 0x00002CB0
		private void AddPropertyDuplicate(TooltipProperty property)
		{
			TooltipProperty item = new TooltipProperty(property);
			this.TooltipPropertyList.Add(item);
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x0600013C RID: 316 RVA: 0x00004AD0 File Offset: 0x00002CD0
		// (set) Token: 0x0600013D RID: 317 RVA: 0x00004AD8 File Offset: 0x00002CD8
		[DataSourceProperty]
		public MBBindingList<TooltipProperty> TooltipPropertyList
		{
			get
			{
				return this._tooltipPropertyList;
			}
			set
			{
				if (value != this._tooltipPropertyList)
				{
					this._tooltipPropertyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<TooltipProperty>>(value, "TooltipPropertyList");
				}
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x0600013E RID: 318 RVA: 0x00004AF6 File Offset: 0x00002CF6
		// (set) Token: 0x0600013F RID: 319 RVA: 0x00004AFE File Offset: 0x00002CFE
		[DataSourceProperty]
		public int Mode
		{
			get
			{
				return this._mode;
			}
			set
			{
				if (value != this._mode)
				{
					this._mode = value;
					base.OnPropertyChangedWithValue(value, "Mode");
				}
			}
		}

		// Token: 0x04000081 RID: 129
		private static Dictionary<string, Func<string>> _keyTextGetters = new Dictionary<string, Func<string>>();

		// Token: 0x04000082 RID: 130
		private MBBindingList<TooltipProperty> _tooltipPropertyList;

		// Token: 0x04000083 RID: 131
		private int _mode;

		// Token: 0x02000036 RID: 54
		public enum TooltipMode
		{
			// Token: 0x040000DF RID: 223
			DefaultGame,
			// Token: 0x040000E0 RID: 224
			DefaultCampaign,
			// Token: 0x040000E1 RID: 225
			Ally,
			// Token: 0x040000E2 RID: 226
			Enemy,
			// Token: 0x040000E3 RID: 227
			War
		}
	}
}
