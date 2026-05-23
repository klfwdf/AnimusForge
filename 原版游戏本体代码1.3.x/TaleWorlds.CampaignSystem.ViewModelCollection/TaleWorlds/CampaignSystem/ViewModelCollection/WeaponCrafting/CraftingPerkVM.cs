using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000FB RID: 251
	public class CraftingPerkVM : ViewModel
	{
		// Token: 0x0600168A RID: 5770 RVA: 0x0005724B File Offset: 0x0005544B
		public CraftingPerkVM(PerkObject perk)
		{
			this.Perk = perk;
			this.Name = this.Perk.Name.ToString();
		}

		// Token: 0x1700077E RID: 1918
		// (get) Token: 0x0600168B RID: 5771 RVA: 0x00057270 File Offset: 0x00055470
		// (set) Token: 0x0600168C RID: 5772 RVA: 0x00057278 File Offset: 0x00055478
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

		// Token: 0x04000A51 RID: 2641
		public readonly PerkObject Perk;

		// Token: 0x04000A52 RID: 2642
		private string _name;
	}
}
