using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment
{
	// Token: 0x020000B1 RID: 177
	public class RecruitVolunteerOwnerVM : HeroVM
	{
		// Token: 0x06001153 RID: 4435 RVA: 0x00045393 File Offset: 0x00043593
		public RecruitVolunteerOwnerVM(Hero hero, int relation)
			: base(hero, hero != null && hero.IsNotable)
		{
			this._hero = hero;
			this.RelationToPlayer = relation;
			this.RefreshValues();
		}

		// Token: 0x06001154 RID: 4436 RVA: 0x000453BC File Offset: 0x000435BC
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this._hero != null)
			{
				if (this._hero.IsPreacher)
				{
					this.TitleText = GameTexts.FindText("str_preacher", null).ToString();
					return;
				}
				if (this._hero.IsGangLeader)
				{
					this.TitleText = GameTexts.FindText("str_gang_leader", null).ToString();
					return;
				}
				if (this._hero.IsMerchant)
				{
					this.TitleText = GameTexts.FindText("str_merchant", null).ToString();
					return;
				}
				if (this._hero.IsRuralNotable)
				{
					this.TitleText = GameTexts.FindText("str_rural_notable", null).ToString();
				}
			}
		}

		// Token: 0x06001155 RID: 4437 RVA: 0x00045469 File Offset: 0x00043669
		public void ExecuteOpenEncyclopedia()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this._hero.EncyclopediaLink);
		}

		// Token: 0x06001156 RID: 4438 RVA: 0x00045485 File Offset: 0x00043685
		public void ExecuteFocus()
		{
			Action<RecruitVolunteerOwnerVM> onFocused = RecruitVolunteerOwnerVM.OnFocused;
			if (onFocused == null)
			{
				return;
			}
			onFocused(this);
		}

		// Token: 0x06001157 RID: 4439 RVA: 0x00045497 File Offset: 0x00043697
		public void ExecuteUnfocus()
		{
			Action<RecruitVolunteerOwnerVM> onFocused = RecruitVolunteerOwnerVM.OnFocused;
			if (onFocused == null)
			{
				return;
			}
			onFocused(null);
		}

		// Token: 0x170005A8 RID: 1448
		// (get) Token: 0x06001158 RID: 4440 RVA: 0x000454A9 File Offset: 0x000436A9
		// (set) Token: 0x06001159 RID: 4441 RVA: 0x000454B1 File Offset: 0x000436B1
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x170005A9 RID: 1449
		// (get) Token: 0x0600115A RID: 4442 RVA: 0x000454D4 File Offset: 0x000436D4
		// (set) Token: 0x0600115B RID: 4443 RVA: 0x000454DC File Offset: 0x000436DC
		[DataSourceProperty]
		public int RelationToPlayer
		{
			get
			{
				return this._relationToPlayer;
			}
			set
			{
				if (value != this._relationToPlayer)
				{
					this._relationToPlayer = value;
					base.OnPropertyChangedWithValue(value, "RelationToPlayer");
				}
			}
		}

		// Token: 0x040007E3 RID: 2019
		public static Action<RecruitVolunteerOwnerVM> OnFocused;

		// Token: 0x040007E4 RID: 2020
		private Hero _hero;

		// Token: 0x040007E5 RID: 2021
		private string _titleText;

		// Token: 0x040007E6 RID: 2022
		private int _relationToPlayer;
	}
}
