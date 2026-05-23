using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x0200002B RID: 43
	public class PartyTradeVM : ViewModel
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600034B RID: 843 RVA: 0x0001641C File Offset: 0x0001461C
		// (remove) Token: 0x0600034C RID: 844 RVA: 0x00016450 File Offset: 0x00014650
		public static event Action RemoveZeroCounts;

		// Token: 0x0600034D RID: 845 RVA: 0x00016484 File Offset: 0x00014684
		public PartyTradeVM(PartyScreenLogic partyScreenLogic, TroopRosterElement troopRoster, PartyScreenLogic.PartyRosterSide side, bool isTransfarable, bool isPrisoner, Action<int, bool> onApplyTransaction)
		{
			this._partyScreenLogic = partyScreenLogic;
			this._referenceTroopRoster = troopRoster;
			this._side = side;
			this._onApplyTransaction = onApplyTransaction;
			this._otherSide = ((side == PartyScreenLogic.PartyRosterSide.Right) ? PartyScreenLogic.PartyRosterSide.Left : PartyScreenLogic.PartyRosterSide.Right);
			this.IsTransfarable = isTransfarable;
			this._isPrisoner = isPrisoner;
			this.UpdateTroopData(troopRoster, side, true);
			this.RefreshValues();
		}

		// Token: 0x0600034E RID: 846 RVA: 0x000164E8 File Offset: 0x000146E8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ThisStockLbl = GameTexts.FindText("str_party_your_party", null).ToString();
			this.TotalStockLbl = GameTexts.FindText("str_party_total_units", null).ToString();
		}

		// Token: 0x0600034F RID: 847 RVA: 0x0001651C File Offset: 0x0001471C
		public void UpdateTroopData(TroopRosterElement troopRoster, PartyScreenLogic.PartyRosterSide side, bool forceUpdate = true)
		{
			if (side != PartyScreenLogic.PartyRosterSide.Left && side != PartyScreenLogic.PartyRosterSide.Right)
			{
				Debug.FailedAssert("Troop has to be either from left or right side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyTradeVM.cs", "UpdateTroopData", 50);
				return;
			}
			TroopRosterElement? troopRosterElement = null;
			TroopRosterElement? troopRosterElement2 = null;
			troopRosterElement = new TroopRosterElement?(troopRoster);
			troopRosterElement2 = this.FindTroopFromSide(troopRoster.Character, this._otherSide, this._isPrisoner);
			this.InitialThisStock = ((troopRosterElement != null) ? troopRosterElement.GetValueOrDefault().Number : 0);
			this.InitialOtherStock = ((troopRosterElement2 != null) ? troopRosterElement2.GetValueOrDefault().Number : 0);
			this.TotalStock = this.InitialThisStock + this.InitialOtherStock;
			this.ThisStock = this.InitialThisStock;
			this.OtherStock = this.InitialOtherStock;
			if (forceUpdate)
			{
				this.ThisStockUpdated();
			}
		}

		// Token: 0x06000350 RID: 848 RVA: 0x000165F0 File Offset: 0x000147F0
		public TroopRosterElement? FindTroopFromSide(CharacterObject character, PartyScreenLogic.PartyRosterSide side, bool isPrisoner)
		{
			TroopRosterElement? result = null;
			TroopRoster[] array = (isPrisoner ? this._partyScreenLogic.PrisonerRosters : this._partyScreenLogic.MemberRosters);
			int num = array[(int)side].FindIndexOfTroop(character);
			if (num >= 0)
			{
				result = new TroopRosterElement?(array[(int)side].GetElementCopyAtIndex(num));
			}
			return result;
		}

		// Token: 0x06000351 RID: 849 RVA: 0x00016640 File Offset: 0x00014840
		private void ThisStockUpdated()
		{
			this.ExecuteApplyTransaction();
			this.OtherStock = this.TotalStock - this.ThisStock;
			this.IsThisStockIncreasable = this.OtherStock > 0;
			this.IsOtherStockIncreasable = this.OtherStock < this.TotalStock && this.IsTransfarable;
		}

		// Token: 0x06000352 RID: 850 RVA: 0x00016692 File Offset: 0x00014892
		public void ExecuteIncreasePlayerStock()
		{
			if (this.OtherStock > 0)
			{
				this.ThisStock++;
			}
		}

		// Token: 0x06000353 RID: 851 RVA: 0x000166AB File Offset: 0x000148AB
		public void ExecuteIncreaseOtherStock()
		{
			if (this.OtherStock < this.TotalStock)
			{
				this.ThisStock--;
			}
		}

		// Token: 0x06000354 RID: 852 RVA: 0x000166C9 File Offset: 0x000148C9
		public void ExecuteReset()
		{
			this.OtherStock = this.InitialOtherStock;
		}

		// Token: 0x06000355 RID: 853 RVA: 0x000166D8 File Offset: 0x000148D8
		public void ExecuteApplyTransaction()
		{
			int num = this.ThisStock - this.InitialThisStock;
			bool arg = (num >= 0 && this._side == PartyScreenLogic.PartyRosterSide.Right) || (num <= 0 && this._side == PartyScreenLogic.PartyRosterSide.Left);
			if (num == 0 || this._onApplyTransaction == null)
			{
				return;
			}
			if (num < 0)
			{
				PartyScreenLogic.PartyRosterSide otherSide = this._otherSide;
			}
			else
			{
				PartyScreenLogic.PartyRosterSide side = this._side;
			}
			int arg2 = MathF.Abs(num);
			this._onApplyTransaction(arg2, arg);
		}

		// Token: 0x06000356 RID: 854 RVA: 0x00016748 File Offset: 0x00014948
		public void ExecuteRemoveZeroCounts()
		{
			Action removeZeroCounts = PartyTradeVM.RemoveZeroCounts;
			if (removeZeroCounts == null)
			{
				return;
			}
			removeZeroCounts();
		}

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000357 RID: 855 RVA: 0x00016759 File Offset: 0x00014959
		// (set) Token: 0x06000358 RID: 856 RVA: 0x00016761 File Offset: 0x00014961
		[DataSourceProperty]
		public bool IsTransfarable
		{
			get
			{
				return this._isTransfarable;
			}
			set
			{
				if (value != this._isTransfarable)
				{
					this._isTransfarable = value;
					base.OnPropertyChangedWithValue(value, "IsTransfarable");
				}
			}
		}

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000359 RID: 857 RVA: 0x0001677F File Offset: 0x0001497F
		// (set) Token: 0x0600035A RID: 858 RVA: 0x00016787 File Offset: 0x00014987
		[DataSourceProperty]
		public string ThisStockLbl
		{
			get
			{
				return this._thisStockLbl;
			}
			set
			{
				if (value != this._thisStockLbl)
				{
					this._thisStockLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ThisStockLbl");
				}
			}
		}

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x0600035B RID: 859 RVA: 0x000167AA File Offset: 0x000149AA
		// (set) Token: 0x0600035C RID: 860 RVA: 0x000167B2 File Offset: 0x000149B2
		[DataSourceProperty]
		public string TotalStockLbl
		{
			get
			{
				return this._totalStockLbl;
			}
			set
			{
				if (value != this._totalStockLbl)
				{
					this._totalStockLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalStockLbl");
				}
			}
		}

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x0600035D RID: 861 RVA: 0x000167D5 File Offset: 0x000149D5
		// (set) Token: 0x0600035E RID: 862 RVA: 0x000167DD File Offset: 0x000149DD
		[DataSourceProperty]
		public int ThisStock
		{
			get
			{
				return this._thisStock;
			}
			set
			{
				if (value != this._thisStock)
				{
					this._thisStock = value;
					base.OnPropertyChangedWithValue(value, "ThisStock");
					this.ThisStockUpdated();
				}
			}
		}

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x0600035F RID: 863 RVA: 0x00016801 File Offset: 0x00014A01
		// (set) Token: 0x06000360 RID: 864 RVA: 0x00016809 File Offset: 0x00014A09
		[DataSourceProperty]
		public int InitialThisStock
		{
			get
			{
				return this._initialThisStock;
			}
			set
			{
				if (value != this._initialThisStock)
				{
					this._initialThisStock = value;
					base.OnPropertyChangedWithValue(value, "InitialThisStock");
				}
			}
		}

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x06000361 RID: 865 RVA: 0x00016827 File Offset: 0x00014A27
		// (set) Token: 0x06000362 RID: 866 RVA: 0x0001682F File Offset: 0x00014A2F
		[DataSourceProperty]
		public int OtherStock
		{
			get
			{
				return this._otherStock;
			}
			set
			{
				if (value != this._otherStock)
				{
					this._otherStock = value;
					base.OnPropertyChangedWithValue(value, "OtherStock");
				}
			}
		}

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x06000363 RID: 867 RVA: 0x0001684D File Offset: 0x00014A4D
		// (set) Token: 0x06000364 RID: 868 RVA: 0x00016855 File Offset: 0x00014A55
		[DataSourceProperty]
		public int InitialOtherStock
		{
			get
			{
				return this._initialOtherStock;
			}
			set
			{
				if (value != this._initialOtherStock)
				{
					this._initialOtherStock = value;
					base.OnPropertyChangedWithValue(value, "InitialOtherStock");
				}
			}
		}

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x06000365 RID: 869 RVA: 0x00016873 File Offset: 0x00014A73
		// (set) Token: 0x06000366 RID: 870 RVA: 0x0001687B File Offset: 0x00014A7B
		[DataSourceProperty]
		public int TotalStock
		{
			get
			{
				return this._totalStock;
			}
			set
			{
				if (value != this._totalStock)
				{
					this._totalStock = value;
					base.OnPropertyChangedWithValue(value, "TotalStock");
				}
			}
		}

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x06000367 RID: 871 RVA: 0x00016899 File Offset: 0x00014A99
		// (set) Token: 0x06000368 RID: 872 RVA: 0x000168A1 File Offset: 0x00014AA1
		[DataSourceProperty]
		public bool IsThisStockIncreasable
		{
			get
			{
				return this._isThisStockIncreasable;
			}
			set
			{
				if (value != this._isThisStockIncreasable)
				{
					this._isThisStockIncreasable = value;
					base.OnPropertyChangedWithValue(value, "IsThisStockIncreasable");
				}
			}
		}

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000369 RID: 873 RVA: 0x000168BF File Offset: 0x00014ABF
		// (set) Token: 0x0600036A RID: 874 RVA: 0x000168C7 File Offset: 0x00014AC7
		[DataSourceProperty]
		public bool IsOtherStockIncreasable
		{
			get
			{
				return this._isOtherStockIncreasable;
			}
			set
			{
				if (value != this._isOtherStockIncreasable)
				{
					this._isOtherStockIncreasable = value;
					base.OnPropertyChangedWithValue(value, "IsOtherStockIncreasable");
				}
			}
		}

		// Token: 0x0400017A RID: 378
		private readonly PartyScreenLogic _partyScreenLogic;

		// Token: 0x0400017B RID: 379
		private readonly Action<int, bool> _onApplyTransaction;

		// Token: 0x0400017C RID: 380
		private readonly bool _isPrisoner;

		// Token: 0x0400017D RID: 381
		private TroopRosterElement _referenceTroopRoster;

		// Token: 0x0400017E RID: 382
		private readonly PartyScreenLogic.PartyRosterSide _side;

		// Token: 0x0400017F RID: 383
		private PartyScreenLogic.PartyRosterSide _otherSide;

		// Token: 0x04000180 RID: 384
		private bool _isTransfarable;

		// Token: 0x04000181 RID: 385
		private string _thisStockLbl;

		// Token: 0x04000182 RID: 386
		private string _totalStockLbl;

		// Token: 0x04000183 RID: 387
		private int _thisStock = -1;

		// Token: 0x04000184 RID: 388
		private int _initialThisStock;

		// Token: 0x04000185 RID: 389
		private int _otherStock;

		// Token: 0x04000186 RID: 390
		private int _initialOtherStock;

		// Token: 0x04000187 RID: 391
		private int _totalStock;

		// Token: 0x04000188 RID: 392
		private bool _isThisStockIncreasable;

		// Token: 0x04000189 RID: 393
		private bool _isOtherStockIncreasable;
	}
}
