using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x0200002A RID: 42
	public class StringPairItemVM : ViewModel
	{
		// Token: 0x060001C7 RID: 455 RVA: 0x00005E04 File Offset: 0x00004004
		public StringPairItemVM(string definition, string value, BasicTooltipViewModel hint = null)
		{
			this.Definition = definition;
			this.Value = value;
			this.Hint = hint;
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060001C8 RID: 456 RVA: 0x00005E21 File Offset: 0x00004021
		// (set) Token: 0x060001C9 RID: 457 RVA: 0x00005E29 File Offset: 0x00004029
		[DataSourceProperty]
		public string Definition
		{
			get
			{
				return this._definition;
			}
			set
			{
				if (value != this._definition)
				{
					this._definition = value;
					base.OnPropertyChangedWithValue<string>(value, "Definition");
				}
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060001CA RID: 458 RVA: 0x00005E4C File Offset: 0x0000404C
		// (set) Token: 0x060001CB RID: 459 RVA: 0x00005E54 File Offset: 0x00004054
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

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060001CC RID: 460 RVA: 0x00005E77 File Offset: 0x00004077
		// (set) Token: 0x060001CD RID: 461 RVA: 0x00005E7F File Offset: 0x0000407F
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
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
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x040000BA RID: 186
		private string _definition;

		// Token: 0x040000BB RID: 187
		private string _value;

		// Token: 0x040000BC RID: 188
		private BasicTooltipViewModel _hint;
	}
}
