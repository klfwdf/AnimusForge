using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x02000027 RID: 39
	public class StringItemWithActionVM : ViewModel
	{
		// Token: 0x060001B6 RID: 438 RVA: 0x00005C6E File Offset: 0x00003E6E
		public StringItemWithActionVM(Action<object> onExecute, string item, object identifier)
		{
			this._onExecute = onExecute;
			this.Identifier = identifier;
			this.ActionText = item;
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x00005C8B File Offset: 0x00003E8B
		public void ExecuteAction()
		{
			this._onExecute(this.Identifier);
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060001B8 RID: 440 RVA: 0x00005C9E File Offset: 0x00003E9E
		// (set) Token: 0x060001B9 RID: 441 RVA: 0x00005CA6 File Offset: 0x00003EA6
		[DataSourceProperty]
		public string ActionText
		{
			get
			{
				return this._actionText;
			}
			set
			{
				if (value != this._actionText)
				{
					this._actionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActionText");
				}
			}
		}

		// Token: 0x040000B0 RID: 176
		public object Identifier;

		// Token: 0x040000B1 RID: 177
		protected Action<object> _onExecute;

		// Token: 0x040000B2 RID: 178
		private string _actionText;
	}
}
