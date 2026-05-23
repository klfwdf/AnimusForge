using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x02000029 RID: 41
	public class StringItemWithHintVM : ViewModel
	{
		// Token: 0x060001C2 RID: 450 RVA: 0x00005D97 File Offset: 0x00003F97
		public StringItemWithHintVM(string text, TextObject hint)
		{
			this.Text = text;
			this.Hint = new HintViewModel(hint, null);
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x00005DB3 File Offset: 0x00003FB3
		// (set) Token: 0x060001C4 RID: 452 RVA: 0x00005DBB File Offset: 0x00003FBB
		[DataSourceProperty]
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				if (value != this._text)
				{
					this._text = value;
					base.OnPropertyChangedWithValue<string>(value, "Text");
				}
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060001C5 RID: 453 RVA: 0x00005DDE File Offset: 0x00003FDE
		// (set) Token: 0x060001C6 RID: 454 RVA: 0x00005DE6 File Offset: 0x00003FE6
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

		// Token: 0x040000B8 RID: 184
		private string _text;

		// Token: 0x040000B9 RID: 185
		private HintViewModel _hint;
	}
}
