using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x02000028 RID: 40
	public class StringItemWithEnabledAndHintVM : ViewModel
	{
		// Token: 0x060001BA RID: 442 RVA: 0x00005CC9 File Offset: 0x00003EC9
		public StringItemWithEnabledAndHintVM(Action<object> onExecute, string item, bool enabled, object identifier, TextObject hintText = null)
		{
			this._onExecute = onExecute;
			this.Identifier = identifier;
			this.ActionText = item;
			this.IsEnabled = enabled;
			this.Hint = new HintViewModel(hintText ?? TextObject.GetEmpty(), null);
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00005D05 File Offset: 0x00003F05
		public void ExecuteAction()
		{
			if (this.IsEnabled)
			{
				this._onExecute(this.Identifier);
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060001BC RID: 444 RVA: 0x00005D20 File Offset: 0x00003F20
		// (set) Token: 0x060001BD RID: 445 RVA: 0x00005D28 File Offset: 0x00003F28
		[DataSourceProperty]
		public string ActionText
		{
			get
			{
				return this._actionText;
			}
			set
			{
				if (value != this._actionText)
				{
					this._actionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActionText");
				}
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00005D4B File Offset: 0x00003F4B
		// (set) Token: 0x060001BF RID: 447 RVA: 0x00005D53 File Offset: 0x00003F53
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

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00005D71 File Offset: 0x00003F71
		// (set) Token: 0x060001C1 RID: 449 RVA: 0x00005D79 File Offset: 0x00003F79
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

		// Token: 0x040000B3 RID: 179
		public object Identifier;

		// Token: 0x040000B4 RID: 180
		protected Action<object> _onExecute;

		// Token: 0x040000B5 RID: 181
		private HintViewModel _hint;

		// Token: 0x040000B6 RID: 182
		private string _actionText;

		// Token: 0x040000B7 RID: 183
		private bool _isEnabled;
	}
}
