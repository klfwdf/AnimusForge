using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x02000026 RID: 38
	public class BoolItemWithActionVM : ViewModel
	{
		// Token: 0x1700008E RID: 142
		// (get) Token: 0x060001B2 RID: 434 RVA: 0x00005C18 File Offset: 0x00003E18
		// (set) Token: 0x060001B3 RID: 435 RVA: 0x00005C20 File Offset: 0x00003E20
		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value != this._isActive)
				{
					this._isActive = value;
					base.OnPropertyChangedWithValue(value, "IsActive");
				}
			}
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x00005C3E File Offset: 0x00003E3E
		public void ExecuteAction()
		{
			this._onExecute(this.Identifier);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x00005C51 File Offset: 0x00003E51
		public BoolItemWithActionVM(Action<object> onExecute, bool isActive, object identifier)
		{
			this._onExecute = onExecute;
			this.Identifier = identifier;
			this.IsActive = isActive;
		}

		// Token: 0x040000AD RID: 173
		public object Identifier;

		// Token: 0x040000AE RID: 174
		protected Action<object> _onExecute;

		// Token: 0x040000AF RID: 175
		private bool _isActive;
	}
}
