using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MCM.UI.Dropdown
{
	// Token: 0x0200002F RID: 47
	[NullableContext(1)]
	[Nullable(0)]
	internal class MCMSelectorVM<[Nullable(0)] TSelectorItemVM> : ViewModel where TSelectorItemVM : ViewModel
	{
		// Token: 0x17000086 RID: 134
		// (get) Token: 0x0600019E RID: 414 RVA: 0x0000746E File Offset: 0x0000566E
		public static MCMSelectorVM<TSelectorItemVM> Empty
		{
			get
			{
				return new MCMSelectorVM<TSelectorItemVM>();
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x0600019F RID: 415 RVA: 0x00007475 File Offset: 0x00005675
		[DataSourceProperty]
		public MBBindingList<TSelectorItemVM> ItemList { get; } = new MBBindingList<TSelectorItemVM>();

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060001A0 RID: 416 RVA: 0x0000747D File Offset: 0x0000567D
		// (set) Token: 0x060001A1 RID: 417 RVA: 0x00007488 File Offset: 0x00005688
		[DataSourceProperty]
		public int SelectedIndex
		{
			get
			{
				return this._selectedIndex;
			}
			set
			{
				if (base.SetField<int>(ref this._selectedIndex, value, "SelectedIndex"))
				{
					if (this.SelectedItem != null)
					{
						MCMSelectorVM<TSelectorItemVM>._setIsSelectedDelegate(this.SelectedItem, false);
					}
					this.SelectedItem = this.GetCurrentItem();
					if (this.SelectedItem != null)
					{
						MCMSelectorVM<TSelectorItemVM>._setIsSelectedDelegate(this.SelectedItem, true);
					}
				}
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x000074FB File Offset: 0x000056FB
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x00007503 File Offset: 0x00005703
		[Nullable(2)]
		[DataSourceProperty]
		public TSelectorItemVM SelectedItem
		{
			[NullableContext(2)]
			get
			{
				return this._selectedItem;
			}
			[NullableContext(2)]
			set
			{
				base.SetField<TSelectorItemVM>(ref this._selectedItem, value, "SelectedItem");
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060001A4 RID: 420 RVA: 0x00007518 File Offset: 0x00005718
		// (set) Token: 0x060001A5 RID: 421 RVA: 0x00007520 File Offset: 0x00005720
		[DataSourceProperty]
		public bool HasSingleItem
		{
			get
			{
				return this._hasSingleItem;
			}
			set
			{
				base.SetField<bool>(ref this._hasSingleItem, value, "HasSingleItem");
			}
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x00007535 File Offset: 0x00005735
		public MCMSelectorVM()
		{
			this.HasSingleItem = true;
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00007556 File Offset: 0x00005756
		public MCMSelectorVM(IEnumerable<object> list, int selectedIndex)
		{
			this.Refresh(list, selectedIndex);
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x00007578 File Offset: 0x00005778
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ItemList.ApplyActionOnAllItems(delegate(TSelectorItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x000075AC File Offset: 0x000057AC
		public void Refresh(IEnumerable<object> list, int selectedIndex)
		{
			this.ItemList.Clear();
			this._selectedIndex = -1;
			foreach (object obj in list)
			{
				TSelectorItemVM val = Activator.CreateInstance(typeof(TSelectorItemVM), new object[] { obj }) as TSelectorItemVM;
				if (val != null)
				{
					this.ItemList.Add(val);
				}
			}
			this.HasSingleItem = this.ItemList.Count <= 1;
			this.SelectedIndex = selectedIndex;
		}

		// Token: 0x060001AA RID: 426 RVA: 0x00007658 File Offset: 0x00005858
		[NullableContext(2)]
		public TSelectorItemVM GetCurrentItem()
		{
			if (this.ItemList.Count > 0 && this.SelectedIndex >= 0 && this.SelectedIndex < this.ItemList.Count)
			{
				return this.ItemList[this.SelectedIndex];
			}
			return default(TSelectorItemVM);
		}

		// Token: 0x060001AB RID: 427 RVA: 0x000076AA File Offset: 0x000058AA
		public void AddItem(TSelectorItemVM item)
		{
			this.ItemList.Add(item);
			this.HasSingleItem = this.ItemList.Count <= 1;
		}

		// Token: 0x060001AC RID: 428 RVA: 0x000076D0 File Offset: 0x000058D0
		public void ExecuteRandomize()
		{
			TSelectorItemVM element = this.ItemList.GetRandomElementWithPredicate((TSelectorItemVM i) => MCMSelectorVM<TSelectorItemVM>._canBeSelectedDelegate(i));
			if (element != null)
			{
				this.SelectedIndex = this.ItemList.IndexOf(element);
			}
		}

		// Token: 0x060001AD RID: 429 RVA: 0x00007724 File Offset: 0x00005924
		public void ExecuteSelectNextItem()
		{
			if (this.ItemList.Count > 0)
			{
				for (int num = (this.SelectedIndex + 1) % this.ItemList.Count; num != this.SelectedIndex; num = (num + 1) % this.ItemList.Count)
				{
					if (MCMSelectorVM<TSelectorItemVM>._canBeSelectedDelegate(this.ItemList[num]))
					{
						this.SelectedIndex = num;
						return;
					}
				}
			}
		}

		// Token: 0x060001AE RID: 430 RVA: 0x00007790 File Offset: 0x00005990
		public void ExecuteSelectPreviousItem()
		{
			if (this.ItemList.Count > 0)
			{
				for (int num = ((this.SelectedIndex - 1 >= 0) ? (this.SelectedIndex - 1) : (this.ItemList.Count - 1)); num != this.SelectedIndex; num = ((num - 1 >= 0) ? (num - 1) : (this.ItemList.Count - 1)))
				{
					if (MCMSelectorVM<TSelectorItemVM>._canBeSelectedDelegate(this.ItemList[num]))
					{
						this.SelectedIndex = num;
						return;
					}
				}
			}
		}

		// Token: 0x04000069 RID: 105
		[Nullable(new byte[] { 1, 0 })]
		private static readonly MCMSelectorVM<TSelectorItemVM>.CanBeSelectedDelegate _canBeSelectedDelegate = AccessTools2.GetPropertyGetterDelegate<MCMSelectorVM<TSelectorItemVM>.CanBeSelectedDelegate>(typeof(TSelectorItemVM), "CanBeSelected", true) ?? ((TSelectorItemVM _) => false);

		// Token: 0x0400006A RID: 106
		[Nullable(new byte[] { 1, 0 })]
		private static readonly MCMSelectorVM<TSelectorItemVM>.SetIsSelectedDelegate _setIsSelectedDelegate = AccessTools2.GetPropertySetterDelegate<MCMSelectorVM<TSelectorItemVM>.SetIsSelectedDelegate>(typeof(TSelectorItemVM), "IsSelected", true) ?? delegate(object _, bool _)
		{
		};

		// Token: 0x0400006B RID: 107
		protected int _selectedIndex = -1;

		// Token: 0x0400006C RID: 108
		[Nullable(2)]
		private TSelectorItemVM _selectedItem;

		// Token: 0x0400006D RID: 109
		private bool _hasSingleItem;

		// Token: 0x02000099 RID: 153
		// (Invoke) Token: 0x06000523 RID: 1315
		[NullableContext(0)]
		private delegate bool CanBeSelectedDelegate(TSelectorItemVM instance);

		// Token: 0x0200009A RID: 154
		// (Invoke) Token: 0x06000527 RID: 1319
		[NullableContext(0)]
		private delegate void SetIsSelectedDelegate(object instance, bool value);
	}
}
