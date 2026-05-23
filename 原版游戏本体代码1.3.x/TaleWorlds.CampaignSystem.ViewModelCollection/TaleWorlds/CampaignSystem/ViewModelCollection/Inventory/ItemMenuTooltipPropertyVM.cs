using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000090 RID: 144
	public class ItemMenuTooltipPropertyVM : TooltipProperty
	{
		// Token: 0x06000C6A RID: 3178 RVA: 0x0003291E File Offset: 0x00030B1E
		public ItemMenuTooltipPropertyVM()
		{
		}

		// Token: 0x06000C6B RID: 3179 RVA: 0x00032926 File Offset: 0x00030B26
		public ItemMenuTooltipPropertyVM(string definition, string value, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
			: base(definition, value, textHeight, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C6C RID: 3180 RVA: 0x0003293C File Offset: 0x00030B3C
		public ItemMenuTooltipPropertyVM(string definition, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
			: base(definition, _valueFunc, textHeight, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C6D RID: 3181 RVA: 0x00032952 File Offset: 0x00030B52
		public ItemMenuTooltipPropertyVM(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
			: base(_definitionFunc, _valueFunc, textHeight, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C6E RID: 3182 RVA: 0x00032968 File Offset: 0x00030B68
		public ItemMenuTooltipPropertyVM(Func<string> _definitionFunc, Func<string> _valueFunc, object[] valueArgs, int textHeight, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
			: base(_definitionFunc, _valueFunc, valueArgs, textHeight, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C6F RID: 3183 RVA: 0x00032980 File Offset: 0x00030B80
		public ItemMenuTooltipPropertyVM(string definition, string value, int textHeight, Color color, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
			: base(definition, value, textHeight, color, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C70 RID: 3184 RVA: 0x00032998 File Offset: 0x00030B98
		public ItemMenuTooltipPropertyVM(string definition, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
			: base(definition, _valueFunc, textHeight, color, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C71 RID: 3185 RVA: 0x000329B0 File Offset: 0x00030BB0
		public ItemMenuTooltipPropertyVM(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, HintViewModel propertyHint = null)
			: base(_definitionFunc, _valueFunc, textHeight, color, onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags.None)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x06000C72 RID: 3186 RVA: 0x000329C8 File Offset: 0x00030BC8
		public ItemMenuTooltipPropertyVM(TooltipProperty property, HintViewModel propertyHint = null)
			: base(property)
		{
			this.PropertyHint = propertyHint;
		}

		// Token: 0x170003FE RID: 1022
		// (get) Token: 0x06000C73 RID: 3187 RVA: 0x000329D8 File Offset: 0x00030BD8
		// (set) Token: 0x06000C74 RID: 3188 RVA: 0x000329E0 File Offset: 0x00030BE0
		[DataSourceProperty]
		public HintViewModel PropertyHint
		{
			get
			{
				return this._propertyHint;
			}
			set
			{
				if (value != this._propertyHint)
				{
					this._propertyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PropertyHint");
				}
			}
		}

		// Token: 0x0400058A RID: 1418
		private HintViewModel _propertyHint;
	}
}
