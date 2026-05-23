using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.SaveLoad
{
	// Token: 0x02000015 RID: 21
	public class SavedGamePropertyVM : ViewModel
	{
		// Token: 0x060001AB RID: 427 RVA: 0x000082FC File Offset: 0x000064FC
		public SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty type, TextObject value, TextObject hint)
		{
			this.PropertyType = type.ToString();
			this._valueText = value;
			this.Hint = new HintViewModel(hint, null);
			this.RefreshValues();
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00008331 File Offset: 0x00006531
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Value = this._valueText.ToString();
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060001AD RID: 429 RVA: 0x0000834A File Offset: 0x0000654A
		// (set) Token: 0x060001AE RID: 430 RVA: 0x00008352 File Offset: 0x00006552
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x060001AF RID: 431 RVA: 0x00008370 File Offset: 0x00006570
		// (set) Token: 0x060001B0 RID: 432 RVA: 0x00008378 File Offset: 0x00006578
		[DataSourceProperty]
		public string PropertyType
		{
			get
			{
				return this._propertyType;
			}
			set
			{
				if (value != this._propertyType)
				{
					this._propertyType = value;
					base.OnPropertyChangedWithValue<string>(value, "PropertyType");
				}
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060001B1 RID: 433 RVA: 0x0000839B File Offset: 0x0000659B
		// (set) Token: 0x060001B2 RID: 434 RVA: 0x000083A3 File Offset: 0x000065A3
		[DataSourceProperty]
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				if (value != this._value)
				{
					this._value = value;
					base.OnPropertyChangedWithValue<string>(value, "Value");
				}
			}
		}

		// Token: 0x040000B9 RID: 185
		private TextObject _valueText;

		// Token: 0x040000BA RID: 186
		private HintViewModel _hint;

		// Token: 0x040000BB RID: 187
		private string _propertyType;

		// Token: 0x040000BC RID: 188
		private string _value;

		// Token: 0x02000078 RID: 120
		public enum SavedGameProperty
		{
			// Token: 0x04000348 RID: 840
			None = -1,
			// Token: 0x04000349 RID: 841
			Health,
			// Token: 0x0400034A RID: 842
			Gold,
			// Token: 0x0400034B RID: 843
			Influence,
			// Token: 0x0400034C RID: 844
			PartySize,
			// Token: 0x0400034D RID: 845
			Food,
			// Token: 0x0400034E RID: 846
			Fiefs
		}
	}
}
