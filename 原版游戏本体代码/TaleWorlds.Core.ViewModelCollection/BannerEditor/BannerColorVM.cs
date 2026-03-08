using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.BannerEditor
{
	// Token: 0x0200002C RID: 44
	public class BannerColorVM : ViewModel
	{
		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060001D8 RID: 472 RVA: 0x00005F89 File Offset: 0x00004189
		public int ColorID { get; }

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060001D9 RID: 473 RVA: 0x00005F91 File Offset: 0x00004191
		public uint Color { get; }

		// Token: 0x060001DA RID: 474 RVA: 0x00005F9C File Offset: 0x0000419C
		public BannerColorVM(int colorID, uint color, Action<BannerColorVM> onSelection)
		{
			this.Color = color;
			this.ColorAsStr = TaleWorlds.Library.Color.FromUint(this.Color).ToString();
			this.ColorID = colorID;
			this._onSelection = onSelection;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x00005FE3 File Offset: 0x000041E3
		public void ExecuteSelectIcon()
		{
			this._onSelection(this);
		}

		// Token: 0x060001DC RID: 476 RVA: 0x00005FF1 File Offset: 0x000041F1
		public void SetOnSelectionAction(Action<BannerColorVM> onSelection)
		{
			this._onSelection = onSelection;
			this.IsSelected = false;
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060001DD RID: 477 RVA: 0x00006001 File Offset: 0x00004201
		// (set) Token: 0x060001DE RID: 478 RVA: 0x00006009 File Offset: 0x00004209
		[DataSourceProperty]
		public string ColorAsStr
		{
			get
			{
				return this._colorAsStr;
			}
			set
			{
				if (value != this._colorAsStr)
				{
					this._colorAsStr = value;
					base.OnPropertyChangedWithValue<string>(value, "ColorAsStr");
				}
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060001DF RID: 479 RVA: 0x0000602C File Offset: 0x0000422C
		// (set) Token: 0x060001E0 RID: 480 RVA: 0x00006034 File Offset: 0x00004234
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

		// Token: 0x040000C5 RID: 197
		private Action<BannerColorVM> _onSelection;

		// Token: 0x040000C6 RID: 198
		private string _colorAsStr;

		// Token: 0x040000C7 RID: 199
		private bool _isSelected;
	}
}
