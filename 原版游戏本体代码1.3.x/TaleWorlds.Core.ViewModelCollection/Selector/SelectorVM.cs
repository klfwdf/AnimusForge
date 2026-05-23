using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Selector
{
	// Token: 0x02000011 RID: 17
	public class SelectorVM<T> : ViewModel where T : SelectorItemVM
	{
		// Token: 0x060000CE RID: 206 RVA: 0x0000361D File Offset: 0x0000181D
		public SelectorVM(int selectedIndex, Action<SelectorVM<T>> onChange)
		{
			this.ItemList = new MBBindingList<T>();
			this.HasSingleItem = true;
			this._onChange = onChange;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00003645 File Offset: 0x00001845
		public SelectorVM(IEnumerable<string> list, int selectedIndex, Action<SelectorVM<T>> onChange)
		{
			this.ItemList = new MBBindingList<T>();
			this.Refresh(list, selectedIndex, onChange);
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00003668 File Offset: 0x00001868
		public SelectorVM(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<T>> onChange)
		{
			this.ItemList = new MBBindingList<T>();
			this.Refresh(list, selectedIndex, onChange);
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000368C File Offset: 0x0000188C
		public void Refresh(IEnumerable<string> list, int selectedIndex, Action<SelectorVM<T>> onChange)
		{
			this.ItemList.Clear();
			this._selectedIndex = -1;
			foreach (string text in list)
			{
				T item = (T)((object)Activator.CreateInstance(typeof(T), new object[] { text }));
				this.ItemList.Add(item);
			}
			this.HasSingleItem = this.ItemList.Count <= 1;
			this._onChange = onChange;
			this.SelectedIndex = selectedIndex;
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00003730 File Offset: 0x00001930
		public void Refresh(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<T>> onChange)
		{
			this.ItemList.Clear();
			this._selectedIndex = -1;
			foreach (TextObject textObject in list)
			{
				T item = (T)((object)Activator.CreateInstance(typeof(T), new object[] { textObject }));
				this.ItemList.Add(item);
			}
			this.HasSingleItem = this.ItemList.Count <= 1;
			this._onChange = onChange;
			this.SelectedIndex = selectedIndex;
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x000037D4 File Offset: 0x000019D4
		public void Refresh(IEnumerable<T> list, int selectedIndex, Action<SelectorVM<T>> onChange)
		{
			this.ItemList.Clear();
			this._selectedIndex = -1;
			foreach (T item in list)
			{
				this.ItemList.Add(item);
			}
			this.HasSingleItem = this.ItemList.Count <= 1;
			this._onChange = onChange;
			this.SelectedIndex = selectedIndex;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00003858 File Offset: 0x00001A58
		public void SetOnChangeAction(Action<SelectorVM<T>> onChange)
		{
			this._onChange = onChange;
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00003861 File Offset: 0x00001A61
		public void AddItem(T item)
		{
			this.ItemList.Add(item);
			this.HasSingleItem = this.ItemList.Count <= 1;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00003888 File Offset: 0x00001A88
		public void ExecuteRandomize()
		{
			MBBindingList<T> itemList = this.ItemList;
			T t;
			if (itemList == null)
			{
				t = default(T);
			}
			else
			{
				t = itemList.GetRandomElementWithPredicate((T i) => i.CanBeSelected);
			}
			T t2 = t;
			if (t2 != null)
			{
				this.SelectedIndex = this.ItemList.IndexOf(t2);
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x000038EC File Offset: 0x00001AEC
		public void ExecuteSelectNextItem()
		{
			MBBindingList<T> itemList = this.ItemList;
			if (itemList != null && itemList.Count > 0)
			{
				for (int num = (this.SelectedIndex + 1) % this.ItemList.Count; num != this.SelectedIndex; num = (num + 1) % this.ItemList.Count)
				{
					if (this.ItemList[num].CanBeSelected)
					{
						this.SelectedIndex = num;
						return;
					}
				}
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00003960 File Offset: 0x00001B60
		public void ExecuteSelectPreviousItem()
		{
			MBBindingList<T> itemList = this.ItemList;
			if (itemList != null && itemList.Count > 0)
			{
				for (int num = ((this.SelectedIndex - 1 >= 0) ? (this.SelectedIndex - 1) : (this.ItemList.Count - 1)); num != this.SelectedIndex; num = ((num - 1 >= 0) ? (num - 1) : (this.ItemList.Count - 1)))
				{
					if (this.ItemList[num].CanBeSelected)
					{
						this.SelectedIndex = num;
						return;
					}
				}
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x000039EC File Offset: 0x00001BEC
		public T GetCurrentItem()
		{
			MBBindingList<T> itemList = this._itemList;
			if (itemList != null && itemList.Count > 0 && this.SelectedIndex >= 0 && this.SelectedIndex < this._itemList.Count)
			{
				return this._itemList[this.SelectedIndex];
			}
			return default(T);
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00003A47 File Offset: 0x00001C47
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._itemList.ApplyActionOnAllItems(delegate(T x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060000DB RID: 219 RVA: 0x00003A79 File Offset: 0x00001C79
		// (set) Token: 0x060000DC RID: 220 RVA: 0x00003A81 File Offset: 0x00001C81
		[DataSourceProperty]
		public MBBindingList<T> ItemList
		{
			get
			{
				return this._itemList;
			}
			set
			{
				if (value != this._itemList)
				{
					this._itemList = value;
					base.OnPropertyChangedWithValue<MBBindingList<T>>(value, "ItemList");
				}
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060000DD RID: 221 RVA: 0x00003A9F File Offset: 0x00001C9F
		// (set) Token: 0x060000DE RID: 222 RVA: 0x00003AA8 File Offset: 0x00001CA8
		[DataSourceProperty]
		public int SelectedIndex
		{
			get
			{
				return this._selectedIndex;
			}
			set
			{
				if (value != this._selectedIndex)
				{
					this._selectedIndex = value;
					base.OnPropertyChangedWithValue(value, "SelectedIndex");
					if (this.SelectedItem != null)
					{
						this.SelectedItem.IsSelected = false;
					}
					this.SelectedItem = this.GetCurrentItem();
					if (this.SelectedItem != null)
					{
						this.SelectedItem.IsSelected = true;
					}
					Action<SelectorVM<T>> onChange = this._onChange;
					if (onChange == null)
					{
						return;
					}
					onChange(this);
				}
			}
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x060000DF RID: 223 RVA: 0x00003B2A File Offset: 0x00001D2A
		// (set) Token: 0x060000E0 RID: 224 RVA: 0x00003B32 File Offset: 0x00001D32
		[DataSourceProperty]
		public T SelectedItem
		{
			get
			{
				return this._selectedItem;
			}
			set
			{
				if (value != this._selectedItem)
				{
					this._selectedItem = value;
					base.OnPropertyChangedWithValue<T>(value, "SelectedItem");
				}
			}
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x060000E1 RID: 225 RVA: 0x00003B5A File Offset: 0x00001D5A
		// (set) Token: 0x060000E2 RID: 226 RVA: 0x00003B62 File Offset: 0x00001D62
		[DataSourceProperty]
		public bool HasSingleItem
		{
			get
			{
				return this._hasSingleItem;
			}
			set
			{
				if (value != this._hasSingleItem)
				{
					this._hasSingleItem = value;
					base.OnPropertyChangedWithValue(value, "HasSingleItem");
				}
			}
		}

		// Token: 0x0400005A RID: 90
		private Action<SelectorVM<T>> _onChange;

		// Token: 0x0400005B RID: 91
		private MBBindingList<T> _itemList;

		// Token: 0x0400005C RID: 92
		private int _selectedIndex = -1;

		// Token: 0x0400005D RID: 93
		private T _selectedItem;

		// Token: 0x0400005E RID: 94
		private bool _hasSingleItem;
	}
}
