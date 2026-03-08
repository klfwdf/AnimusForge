using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x0200002B RID: 43
	public class StringPairItemWithActionVM : ViewModel
	{
		// Token: 0x060001CE RID: 462 RVA: 0x00005E9D File Offset: 0x0000409D
		public StringPairItemWithActionVM(Action<object> onExecute, string definition, string value, object identifier)
		{
			this._onExecute = onExecute;
			this.Identifier = identifier;
			this.Definition = definition;
			this.Value = value;
			this.Hint = new HintViewModel();
			this.IsEnabled = true;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00005ED4 File Offset: 0x000040D4
		public void ExecuteAction()
		{
			this._onExecute(this.Identifier);
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060001D0 RID: 464 RVA: 0x00005EE7 File Offset: 0x000040E7
		// (set) Token: 0x060001D1 RID: 465 RVA: 0x00005EEF File Offset: 0x000040EF
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

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060001D2 RID: 466 RVA: 0x00005F12 File Offset: 0x00004112
		// (set) Token: 0x060001D3 RID: 467 RVA: 0x00005F1A File Offset: 0x0000411A
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

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060001D4 RID: 468 RVA: 0x00005F3D File Offset: 0x0000413D
		// (set) Token: 0x060001D5 RID: 469 RVA: 0x00005F45 File Offset: 0x00004145
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

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060001D6 RID: 470 RVA: 0x00005F63 File Offset: 0x00004163
		// (set) Token: 0x060001D7 RID: 471 RVA: 0x00005F6B File Offset: 0x0000416B
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x040000BD RID: 189
		public object Identifier;

		// Token: 0x040000BE RID: 190
		protected Action<object> _onExecute;

		// Token: 0x040000BF RID: 191
		private string _definition;

		// Token: 0x040000C0 RID: 192
		private string _value;

		// Token: 0x040000C1 RID: 193
		private HintViewModel _hint;

		// Token: 0x040000C2 RID: 194
		private bool _isEnabled;
	}
}
