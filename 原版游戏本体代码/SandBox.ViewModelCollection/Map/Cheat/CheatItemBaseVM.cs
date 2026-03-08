using System;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Cheat
{
	// Token: 0x02000050 RID: 80
	public abstract class CheatItemBaseVM : ViewModel
	{
		// Token: 0x060004DF RID: 1247 RVA: 0x00012A63 File Offset: 0x00010C63
		public CheatItemBaseVM()
		{
		}

		// Token: 0x060004E0 RID: 1248
		public abstract void ExecuteAction();

		// Token: 0x1700016C RID: 364
		// (get) Token: 0x060004E1 RID: 1249 RVA: 0x00012A6B File Offset: 0x00010C6B
		// (set) Token: 0x060004E2 RID: 1250 RVA: 0x00012A73 File Offset: 0x00010C73
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

		// Token: 0x04000268 RID: 616
		private string _name;
	}
}
