using System;

namespace TaleWorlds.Library.Graph
{
	// Token: 0x020000B6 RID: 182
	public class GraphLineVM : ViewModel
	{
		// Token: 0x060006C2 RID: 1730 RVA: 0x00016FBB File Offset: 0x000151BB
		public GraphLineVM(string ID, string name)
		{
			this.Points = new MBBindingList<GraphLinePointVM>();
			this.Name = name;
			this.ID = ID;
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x060006C3 RID: 1731 RVA: 0x00016FDC File Offset: 0x000151DC
		// (set) Token: 0x060006C4 RID: 1732 RVA: 0x00016FE4 File Offset: 0x000151E4
		[DataSourceProperty]
		public MBBindingList<GraphLinePointVM> Points
		{
			get
			{
				return this._points;
			}
			set
			{
				if (value != this._points)
				{
					this._points = value;
					base.OnPropertyChangedWithValue<MBBindingList<GraphLinePointVM>>(value, "Points");
				}
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x060006C5 RID: 1733 RVA: 0x00017002 File Offset: 0x00015202
		// (set) Token: 0x060006C6 RID: 1734 RVA: 0x0001700A File Offset: 0x0001520A
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x060006C7 RID: 1735 RVA: 0x0001702D File Offset: 0x0001522D
		// (set) Token: 0x060006C8 RID: 1736 RVA: 0x00017035 File Offset: 0x00015235
		[DataSourceProperty]
		public string ID
		{
			get
			{
				return this._ID;
			}
			set
			{
				if (value != this._ID)
				{
					this._ID = value;
					base.OnPropertyChangedWithValue<string>(value, "ID");
				}
			}
		}

		// Token: 0x04000210 RID: 528
		private MBBindingList<GraphLinePointVM> _points;

		// Token: 0x04000211 RID: 529
		private string _name;

		// Token: 0x04000212 RID: 530
		private string _ID;
	}
}
