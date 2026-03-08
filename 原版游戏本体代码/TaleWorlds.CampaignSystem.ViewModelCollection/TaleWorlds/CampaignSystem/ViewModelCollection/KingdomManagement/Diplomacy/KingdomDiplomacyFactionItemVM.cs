using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x0200006D RID: 109
	public class KingdomDiplomacyFactionItemVM : ViewModel
	{
		// Token: 0x060008DA RID: 2266 RVA: 0x0002790A File Offset: 0x00025B0A
		public KingdomDiplomacyFactionItemVM(IFaction faction)
		{
			this.Hint = new HintViewModel(faction.Name, null);
			this.Visual = new BannerImageIdentifierVM(faction.Banner, true);
		}

		// Token: 0x17000299 RID: 665
		// (get) Token: 0x060008DB RID: 2267 RVA: 0x00027936 File Offset: 0x00025B36
		// (set) Token: 0x060008DC RID: 2268 RVA: 0x0002793E File Offset: 0x00025B3E
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x1700029A RID: 666
		// (get) Token: 0x060008DD RID: 2269 RVA: 0x0002795C File Offset: 0x00025B5C
		// (set) Token: 0x060008DE RID: 2270 RVA: 0x00027964 File Offset: 0x00025B64
		[DataSourceProperty]
		public BannerImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x040003E2 RID: 994
		private HintViewModel _hint;

		// Token: 0x040003E3 RID: 995
		private BannerImageIdentifierVM _visual;
	}
}
