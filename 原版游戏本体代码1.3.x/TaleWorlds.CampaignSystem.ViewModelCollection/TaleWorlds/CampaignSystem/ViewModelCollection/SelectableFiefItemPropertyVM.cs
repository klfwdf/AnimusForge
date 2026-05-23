using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200001D RID: 29
	public class SelectableFiefItemPropertyVM : SelectableItemPropertyVM
	{
		// Token: 0x060001C1 RID: 449 RVA: 0x0000C5EB File Offset: 0x0000A7EB
		public SelectableFiefItemPropertyVM(string name, string value, int changeAmount, SelectableItemPropertyVM.PropertyType type, BasicTooltipViewModel hint = null, bool isWarning = false)
			: base(name, value, isWarning, hint)
		{
			this.ChangeAmount = changeAmount;
			base.Type = (int)type;
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x060001C2 RID: 450 RVA: 0x0000C608 File Offset: 0x0000A808
		// (set) Token: 0x060001C3 RID: 451 RVA: 0x0000C610 File Offset: 0x0000A810
		[DataSourceProperty]
		public int ChangeAmount
		{
			get
			{
				return this._changeAmount;
			}
			set
			{
				if (value != this._changeAmount)
				{
					this._changeAmount = value;
					base.OnPropertyChangedWithValue(value, "ChangeAmount");
				}
			}
		}

		// Token: 0x040000D2 RID: 210
		private int _changeAmount;
	}
}
