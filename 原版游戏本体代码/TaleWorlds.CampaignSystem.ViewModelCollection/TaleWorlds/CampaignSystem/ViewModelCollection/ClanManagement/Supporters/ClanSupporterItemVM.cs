using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters
{
	// Token: 0x02000131 RID: 305
	public class ClanSupporterItemVM : ViewModel
	{
		// Token: 0x06001C79 RID: 7289 RVA: 0x00069177 File Offset: 0x00067377
		public ClanSupporterItemVM(Hero hero)
		{
			this.Hero = new HeroVM(hero, false);
		}

		// Token: 0x06001C7A RID: 7290 RVA: 0x0006918C File Offset: 0x0006738C
		public void ExecuteOpenTooltip()
		{
			InformationManager.ShowTooltip(typeof(Hero), new object[]
			{
				this.Hero.Hero,
				false
			});
		}

		// Token: 0x06001C7B RID: 7291 RVA: 0x000691BA File Offset: 0x000673BA
		public void ExecuteCloseTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x170009B2 RID: 2482
		// (get) Token: 0x06001C7C RID: 7292 RVA: 0x000691C1 File Offset: 0x000673C1
		// (set) Token: 0x06001C7D RID: 7293 RVA: 0x000691C9 File Offset: 0x000673C9
		[DataSourceProperty]
		public HeroVM Hero
		{
			get
			{
				return this._hero;
			}
			set
			{
				if (value != this._hero)
				{
					this._hero = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Hero");
				}
			}
		}

		// Token: 0x04000D4A RID: 3402
		private HeroVM _hero;
	}
}
