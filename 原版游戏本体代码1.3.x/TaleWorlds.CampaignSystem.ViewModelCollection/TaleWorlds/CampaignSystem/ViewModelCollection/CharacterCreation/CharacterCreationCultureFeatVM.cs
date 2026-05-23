using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x0200014C RID: 332
	public class CharacterCreationCultureFeatVM : ViewModel
	{
		// Token: 0x06001F84 RID: 8068 RVA: 0x00073317 File Offset: 0x00071517
		public CharacterCreationCultureFeatVM(bool isPositive, string description)
		{
			this.IsPositive = isPositive;
			this.Description = description;
		}

		// Token: 0x17000ABC RID: 2748
		// (get) Token: 0x06001F85 RID: 8069 RVA: 0x0007332D File Offset: 0x0007152D
		// (set) Token: 0x06001F86 RID: 8070 RVA: 0x00073335 File Offset: 0x00071535
		[DataSourceProperty]
		public bool IsPositive
		{
			get
			{
				return this._isPositive;
			}
			set
			{
				if (value != this._isPositive)
				{
					this._isPositive = value;
					base.OnPropertyChangedWithValue(value, "IsPositive");
				}
			}
		}

		// Token: 0x17000ABD RID: 2749
		// (get) Token: 0x06001F87 RID: 8071 RVA: 0x00073353 File Offset: 0x00071553
		// (set) Token: 0x06001F88 RID: 8072 RVA: 0x0007335B File Offset: 0x0007155B
		[DataSourceProperty]
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (value != this._description)
				{
					this._description = value;
					base.OnPropertyChangedWithValue<string>(value, "Description");
				}
			}
		}

		// Token: 0x04000EB3 RID: 3763
		private bool _isPositive;

		// Token: 0x04000EB4 RID: 3764
		private string _description;
	}
}
