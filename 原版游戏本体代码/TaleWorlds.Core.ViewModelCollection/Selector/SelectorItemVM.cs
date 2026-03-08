using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Selector
{
	// Token: 0x02000010 RID: 16
	public class SelectorItemVM : ViewModel
	{
		// Token: 0x060000C1 RID: 193 RVA: 0x00003493 File Offset: 0x00001693
		public SelectorItemVM(TextObject s)
		{
			this._s = s;
			this.RefreshValues();
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x000034AF File Offset: 0x000016AF
		public SelectorItemVM(string s)
		{
			this._stringItem = s;
			this.RefreshValues();
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x000034CB File Offset: 0x000016CB
		public SelectorItemVM(TextObject s, TextObject hint)
		{
			this._s = s;
			this._hintObj = hint;
			this.RefreshValues();
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x000034EE File Offset: 0x000016EE
		public SelectorItemVM(string s, TextObject hint)
		{
			this._stringItem = s;
			this._hintObj = hint;
			this.RefreshValues();
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00003514 File Offset: 0x00001714
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this._s != null)
			{
				this._stringItem = this._s.ToString();
			}
			if (this._hintObj != null)
			{
				if (this._hint == null)
				{
					this._hint = new HintViewModel(this._hintObj, null);
					return;
				}
				this._hint.HintText = this._hintObj;
			}
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x00003580 File Offset: 0x00001780
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x00003588 File Offset: 0x00001788
		[DataSourceProperty]
		public string StringItem
		{
			get
			{
				return this._stringItem;
			}
			set
			{
				if (value != this._stringItem)
				{
					this._stringItem = value;
					base.OnPropertyChangedWithValue<string>(value, "StringItem");
				}
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x000035AB File Offset: 0x000017AB
		// (set) Token: 0x060000C9 RID: 201 RVA: 0x000035B3 File Offset: 0x000017B3
		[DataSourceProperty]
		public bool CanBeSelected
		{
			get
			{
				return this._canBeSelected;
			}
			set
			{
				if (value != this._canBeSelected)
				{
					this._canBeSelected = value;
					base.OnPropertyChangedWithValue(value, "CanBeSelected");
				}
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060000CA RID: 202 RVA: 0x000035D1 File Offset: 0x000017D1
		// (set) Token: 0x060000CB RID: 203 RVA: 0x000035D9 File Offset: 0x000017D9
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

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060000CC RID: 204 RVA: 0x000035F7 File Offset: 0x000017F7
		// (set) Token: 0x060000CD RID: 205 RVA: 0x000035FF File Offset: 0x000017FF
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x04000054 RID: 84
		private TextObject _s;

		// Token: 0x04000055 RID: 85
		private TextObject _hintObj;

		// Token: 0x04000056 RID: 86
		private string _stringItem;

		// Token: 0x04000057 RID: 87
		private HintViewModel _hint;

		// Token: 0x04000058 RID: 88
		private bool _canBeSelected = true;

		// Token: 0x04000059 RID: 89
		private bool _isSelected;
	}
}
