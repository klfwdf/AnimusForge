using System;
using System.Runtime.CompilerServices;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace MCM.UI.Dropdown
{
	// Token: 0x0200002E RID: 46
	[NullableContext(2)]
	[Nullable(0)]
	internal abstract class MCMSelectorItemVMBase : ViewModel
	{
		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000195 RID: 405 RVA: 0x000073EB File Offset: 0x000055EB
		// (set) Token: 0x06000196 RID: 406 RVA: 0x000073F3 File Offset: 0x000055F3
		[DataSourceProperty]
		public bool CanBeSelected
		{
			get
			{
				return this._canBeSelected;
			}
			set
			{
				base.SetField<bool>(ref this._canBeSelected, value, "CanBeSelected");
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000197 RID: 407 RVA: 0x00007408 File Offset: 0x00005608
		// (set) Token: 0x06000198 RID: 408 RVA: 0x00007410 File Offset: 0x00005610
		[DataSourceProperty]
		public string StringItem
		{
			get
			{
				return this._stringItem;
			}
			set
			{
				base.SetField<string>(ref this._stringItem, value, "StringItem");
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000199 RID: 409 RVA: 0x00007425 File Offset: 0x00005625
		// (set) Token: 0x0600019A RID: 410 RVA: 0x0000742D File Offset: 0x0000562D
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				base.SetField<HintViewModel>(ref this._hint, value, "Hint");
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x0600019B RID: 411 RVA: 0x00007442 File Offset: 0x00005642
		// (set) Token: 0x0600019C RID: 412 RVA: 0x0000744A File Offset: 0x0000564A
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				base.SetField<bool>(ref this._isSelected, value, "IsSelected");
			}
		}

		// Token: 0x04000065 RID: 101
		protected string _stringItem;

		// Token: 0x04000066 RID: 102
		protected bool _canBeSelected = true;

		// Token: 0x04000067 RID: 103
		protected HintViewModel _hint;

		// Token: 0x04000068 RID: 104
		protected bool _isSelected;
	}
}
