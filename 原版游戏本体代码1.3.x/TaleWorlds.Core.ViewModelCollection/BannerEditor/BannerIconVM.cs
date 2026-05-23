using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.BannerEditor
{
	// Token: 0x0200002D RID: 45
	public class BannerIconVM : ViewModel
	{
		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060001E1 RID: 481 RVA: 0x00006052 File Offset: 0x00004252
		public int IconID { get; }

		// Token: 0x060001E2 RID: 482 RVA: 0x0000605A File Offset: 0x0000425A
		public BannerIconVM(int iconID, Action<BannerIconVM> onSelection)
		{
			this.IconPath = iconID.ToString();
			this.IconID = iconID;
			this._onSelection = onSelection;
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0000607D File Offset: 0x0000427D
		public void ExecuteSelectIcon()
		{
			this._onSelection(this);
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060001E4 RID: 484 RVA: 0x0000608B File Offset: 0x0000428B
		// (set) Token: 0x060001E5 RID: 485 RVA: 0x00006093 File Offset: 0x00004293
		[DataSourceProperty]
		public string IconPath
		{
			get
			{
				return this._iconPath;
			}
			set
			{
				if (value != this._iconPath)
				{
					this._iconPath = value;
					base.OnPropertyChangedWithValue<string>(value, "IconPath");
				}
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060001E6 RID: 486 RVA: 0x000060B6 File Offset: 0x000042B6
		// (set) Token: 0x060001E7 RID: 487 RVA: 0x000060BE File Offset: 0x000042BE
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

		// Token: 0x040000C9 RID: 201
		private readonly Action<BannerIconVM> _onSelection;

		// Token: 0x040000CA RID: 202
		private string _iconPath;

		// Token: 0x040000CB RID: 203
		private bool _isSelected;
	}
}
