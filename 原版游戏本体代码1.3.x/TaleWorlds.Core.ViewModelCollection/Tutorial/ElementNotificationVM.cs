using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Tutorial
{
	// Token: 0x0200000E RID: 14
	public class ElementNotificationVM : ViewModel
	{
		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060000BB RID: 187 RVA: 0x0000342C File Offset: 0x0000162C
		// (set) Token: 0x060000BC RID: 188 RVA: 0x00003434 File Offset: 0x00001634
		[DataSourceProperty]
		public string ElementID
		{
			get
			{
				return this._elementID;
			}
			set
			{
				if (value != this._elementID)
				{
					this._elementID = value;
					base.OnPropertyChangedWithValue<string>(value, "ElementID");
				}
			}
		}

		// Token: 0x04000052 RID: 82
		private string _elementID = string.Empty;
	}
}
