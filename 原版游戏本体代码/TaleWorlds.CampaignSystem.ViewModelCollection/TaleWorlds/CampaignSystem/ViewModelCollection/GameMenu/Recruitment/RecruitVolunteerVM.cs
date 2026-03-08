using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment
{
	// Token: 0x020000B3 RID: 179
	public class RecruitVolunteerVM : ViewModel
	{
		// Token: 0x170005B6 RID: 1462
		// (get) Token: 0x0600117D RID: 4477 RVA: 0x00045AF7 File Offset: 0x00043CF7
		// (set) Token: 0x0600117E RID: 4478 RVA: 0x00045AFF File Offset: 0x00043CFF
		public Hero OwnerHero { get; private set; }

		// Token: 0x170005B7 RID: 1463
		// (get) Token: 0x0600117F RID: 4479 RVA: 0x00045B08 File Offset: 0x00043D08
		// (set) Token: 0x06001180 RID: 4480 RVA: 0x00045B10 File Offset: 0x00043D10
		public List<CharacterObject> VolunteerTroops { get; private set; }

		// Token: 0x170005B8 RID: 1464
		// (get) Token: 0x06001181 RID: 4481 RVA: 0x00045B19 File Offset: 0x00043D19
		public int GoldCost { get; }

		// Token: 0x06001182 RID: 4482 RVA: 0x00045B24 File Offset: 0x00043D24
		public RecruitVolunteerVM(Hero owner, List<CharacterObject> troops, Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> onRecruit, Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> onRemoveFromCart)
		{
			this.OwnerHero = owner;
			this.VolunteerTroops = troops;
			this._onRecruit = onRecruit;
			this._onRemoveFromCart = onRemoveFromCart;
			this.Owner = new RecruitVolunteerOwnerVM(owner, (int)owner.GetRelationWithPlayer());
			this.Troops = new MBBindingList<RecruitVolunteerTroopVM>();
			int num = 0;
			foreach (CharacterObject characterObject in troops)
			{
				RecruitVolunteerTroopVM recruitVolunteerTroopVM = new RecruitVolunteerTroopVM(this, characterObject, num, new Action<RecruitVolunteerTroopVM>(this.ExecuteRecruit), new Action<RecruitVolunteerTroopVM>(this.ExecuteRemoveFromCart));
				recruitVolunteerTroopVM.CanBeRecruited = false;
				recruitVolunteerTroopVM.PlayerHasEnoughRelation = false;
				if (HeroHelper.HeroCanRecruitFromHero(Hero.MainHero, this.OwnerHero, num))
				{
					recruitVolunteerTroopVM.PlayerHasEnoughRelation = true;
					if (characterObject != null)
					{
						recruitVolunteerTroopVM.CanBeRecruited = true;
					}
				}
				num++;
				this.Troops.Add(recruitVolunteerTroopVM);
			}
			this.RecruitHint = new HintViewModel();
			this.RefreshProperties();
		}

		// Token: 0x06001183 RID: 4483 RVA: 0x00045C24 File Offset: 0x00043E24
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.RefreshProperties();
			RecruitVolunteerOwnerVM owner = this.Owner;
			if (owner != null)
			{
				owner.RefreshValues();
			}
			this.Troops.ApplyActionOnAllItems(delegate(RecruitVolunteerTroopVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06001184 RID: 4484 RVA: 0x00045C78 File Offset: 0x00043E78
		public void ExecuteRecruit(RecruitVolunteerTroopVM troop)
		{
			this._onRecruit(this, troop);
			this.RefreshProperties();
		}

		// Token: 0x06001185 RID: 4485 RVA: 0x00045C8D File Offset: 0x00043E8D
		public void ExecuteRemoveFromCart(RecruitVolunteerTroopVM troop)
		{
			this._onRemoveFromCart(this, troop);
			this.RefreshProperties();
		}

		// Token: 0x06001186 RID: 4486 RVA: 0x00045CA4 File Offset: 0x00043EA4
		private void RefreshProperties()
		{
			this.RecruitText = this.GoldCost.ToString();
			if (this.RecruitableNumber == 0)
			{
				this.QuantityText = GameTexts.FindText("str_none", null).ToString();
				return;
			}
			GameTexts.SetVariable("QUANTITY", this.RecruitableNumber.ToString());
			this.QuantityText = GameTexts.FindText("str_x_quantity", null).ToString();
		}

		// Token: 0x06001187 RID: 4487 RVA: 0x00045D10 File Offset: 0x00043F10
		public void OnRecruitMoveToCart(RecruitVolunteerTroopVM troop)
		{
			MBInformationManager.HideInformations();
			this.Troops.RemoveAt(troop.Index);
			RecruitVolunteerTroopVM recruitVolunteerTroopVM = new RecruitVolunteerTroopVM(this, null, troop.Index, new Action<RecruitVolunteerTroopVM>(this.ExecuteRecruit), new Action<RecruitVolunteerTroopVM>(this.ExecuteRemoveFromCart));
			recruitVolunteerTroopVM.IsTroopEmpty = true;
			recruitVolunteerTroopVM.PlayerHasEnoughRelation = true;
			this.Troops.Insert(troop.Index, recruitVolunteerTroopVM);
		}

		// Token: 0x06001188 RID: 4488 RVA: 0x00045D79 File Offset: 0x00043F79
		public void OnRecruitRemovedFromCart(RecruitVolunteerTroopVM troop)
		{
			this.Troops.RemoveAt(troop.Index);
			this.Troops.Insert(troop.Index, troop);
		}

		// Token: 0x170005B9 RID: 1465
		// (get) Token: 0x06001189 RID: 4489 RVA: 0x00045D9E File Offset: 0x00043F9E
		// (set) Token: 0x0600118A RID: 4490 RVA: 0x00045DA6 File Offset: 0x00043FA6
		[DataSourceProperty]
		public MBBindingList<RecruitVolunteerTroopVM> Troops
		{
			get
			{
				return this._troops;
			}
			set
			{
				if (value != this._troops)
				{
					this._troops = value;
					base.OnPropertyChangedWithValue<MBBindingList<RecruitVolunteerTroopVM>>(value, "Troops");
				}
			}
		}

		// Token: 0x170005BA RID: 1466
		// (get) Token: 0x0600118B RID: 4491 RVA: 0x00045DC4 File Offset: 0x00043FC4
		// (set) Token: 0x0600118C RID: 4492 RVA: 0x00045DCC File Offset: 0x00043FCC
		[DataSourceProperty]
		public RecruitVolunteerOwnerVM Owner
		{
			get
			{
				return this._owner;
			}
			set
			{
				if (value != this._owner)
				{
					this._owner = value;
					base.OnPropertyChangedWithValue<RecruitVolunteerOwnerVM>(value, "Owner");
				}
			}
		}

		// Token: 0x170005BB RID: 1467
		// (get) Token: 0x0600118D RID: 4493 RVA: 0x00045DEA File Offset: 0x00043FEA
		// (set) Token: 0x0600118E RID: 4494 RVA: 0x00045DF2 File Offset: 0x00043FF2
		[DataSourceProperty]
		public bool CanRecruit
		{
			get
			{
				return this._canRecruit;
			}
			set
			{
				if (value != this._canRecruit)
				{
					this._canRecruit = value;
					base.OnPropertyChangedWithValue(value, "CanRecruit");
				}
			}
		}

		// Token: 0x170005BC RID: 1468
		// (get) Token: 0x0600118F RID: 4495 RVA: 0x00045E10 File Offset: 0x00044010
		// (set) Token: 0x06001190 RID: 4496 RVA: 0x00045E18 File Offset: 0x00044018
		[DataSourceProperty]
		public bool ButtonIsVisible
		{
			get
			{
				return this._buttonIsVisible;
			}
			set
			{
				if (value != this._buttonIsVisible)
				{
					this._buttonIsVisible = value;
					base.OnPropertyChangedWithValue(value, "ButtonIsVisible");
				}
			}
		}

		// Token: 0x170005BD RID: 1469
		// (get) Token: 0x06001191 RID: 4497 RVA: 0x00045E36 File Offset: 0x00044036
		// (set) Token: 0x06001192 RID: 4498 RVA: 0x00045E3E File Offset: 0x0004403E
		[DataSourceProperty]
		public string QuantityText
		{
			get
			{
				return this._quantityText;
			}
			set
			{
				if (value != this._quantityText)
				{
					this._quantityText = value;
					base.OnPropertyChangedWithValue<string>(value, "QuantityText");
				}
			}
		}

		// Token: 0x170005BE RID: 1470
		// (get) Token: 0x06001193 RID: 4499 RVA: 0x00045E61 File Offset: 0x00044061
		// (set) Token: 0x06001194 RID: 4500 RVA: 0x00045E69 File Offset: 0x00044069
		[DataSourceProperty]
		public string RecruitText
		{
			get
			{
				return this._recruitText;
			}
			set
			{
				if (value != this._recruitText)
				{
					this._recruitText = value;
					base.OnPropertyChangedWithValue<string>(value, "RecruitText");
				}
			}
		}

		// Token: 0x170005BF RID: 1471
		// (get) Token: 0x06001195 RID: 4501 RVA: 0x00045E8C File Offset: 0x0004408C
		// (set) Token: 0x06001196 RID: 4502 RVA: 0x00045E94 File Offset: 0x00044094
		[DataSourceProperty]
		public HintViewModel RecruitHint
		{
			get
			{
				return this._recruitHint;
			}
			set
			{
				if (value != this._recruitHint)
				{
					this._recruitHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RecruitHint");
				}
			}
		}

		// Token: 0x04000800 RID: 2048
		public int RecruitableNumber;

		// Token: 0x04000801 RID: 2049
		private readonly Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> _onRecruit;

		// Token: 0x04000802 RID: 2050
		private readonly Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> _onRemoveFromCart;

		// Token: 0x04000803 RID: 2051
		private string _quantityText;

		// Token: 0x04000804 RID: 2052
		private string _recruitText;

		// Token: 0x04000805 RID: 2053
		private bool _canRecruit;

		// Token: 0x04000806 RID: 2054
		private bool _buttonIsVisible;

		// Token: 0x04000807 RID: 2055
		private HintViewModel _recruitHint;

		// Token: 0x04000808 RID: 2056
		private RecruitVolunteerOwnerVM _owner;

		// Token: 0x04000809 RID: 2057
		private MBBindingList<RecruitVolunteerTroopVM> _troops;
	}
}
