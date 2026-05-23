using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000E9 RID: 233
	public class EncyclopediaShipSlotVM : ViewModel
	{
		// Token: 0x06001588 RID: 5512 RVA: 0x0005464E File Offset: 0x0005284E
		public EncyclopediaShipSlotVM(string slotId, bool isAvailable)
		{
			this.SlotTypeId = slotId;
			this.IsAvailable = isAvailable;
			this.RefreshValues();
		}

		// Token: 0x06001589 RID: 5513 RVA: 0x0005466A File Offset: 0x0005286A
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = GameTexts.FindText("str_ship_slot_type", this.SlotTypeId).ToString();
		}

		// Token: 0x1700071E RID: 1822
		// (get) Token: 0x0600158A RID: 5514 RVA: 0x0005468D File Offset: 0x0005288D
		// (set) Token: 0x0600158B RID: 5515 RVA: 0x00054695 File Offset: 0x00052895
		[DataSourceProperty]
		public string SlotTypeId
		{
			get
			{
				return this._slotTypeId;
			}
			set
			{
				if (value != this._slotTypeId)
				{
					this._slotTypeId = value;
					base.OnPropertyChangedWithValue<string>(value, "SlotTypeId");
				}
			}
		}

		// Token: 0x1700071F RID: 1823
		// (get) Token: 0x0600158C RID: 5516 RVA: 0x000546B8 File Offset: 0x000528B8
		// (set) Token: 0x0600158D RID: 5517 RVA: 0x000546C0 File Offset: 0x000528C0
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

		// Token: 0x17000720 RID: 1824
		// (get) Token: 0x0600158E RID: 5518 RVA: 0x000546E3 File Offset: 0x000528E3
		// (set) Token: 0x0600158F RID: 5519 RVA: 0x000546EB File Offset: 0x000528EB
		[DataSourceProperty]
		public bool IsAvailable
		{
			get
			{
				return this._isAvailable;
			}
			set
			{
				if (value != this._isAvailable)
				{
					this._isAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsAvailable");
				}
			}
		}

		// Token: 0x040009CD RID: 2509
		private string _slotTypeId;

		// Token: 0x040009CE RID: 2510
		private string _name;

		// Token: 0x040009CF RID: 2511
		private bool _isAvailable;
	}
}
