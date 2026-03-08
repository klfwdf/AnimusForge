using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement
{
	// Token: 0x0200015B RID: 347
	public class ArmyManagementBoostEventVM : ViewModel
	{
		// Token: 0x17000B33 RID: 2867
		// (get) Token: 0x060020E8 RID: 8424 RVA: 0x0007794F File Offset: 0x00075B4F
		public ArmyManagementBoostEventVM.BoostCurrency CurrencyToPayForCohesion { get; }

		// Token: 0x060020E9 RID: 8425 RVA: 0x00077957 File Offset: 0x00075B57
		public ArmyManagementBoostEventVM(ArmyManagementBoostEventVM.BoostCurrency currencyToPayForCohesion, int amountToPay, int amountOfCohesionToGain, Action<ArmyManagementBoostEventVM> onExecuteEvent)
		{
			this.IsEnabled = true;
			this._onExecuteEvent = onExecuteEvent;
			this.AmountToPay = amountToPay;
			this.AmountOfCohesionToGain = amountOfCohesionToGain;
			this.CurrencyToPayForCohesion = currencyToPayForCohesion;
			this.CurrencyType = (int)currencyToPayForCohesion;
			this.RefreshValues();
		}

		// Token: 0x060020EA RID: 8426 RVA: 0x00077990 File Offset: 0x00075B90
		public override void RefreshValues()
		{
			base.RefreshValues();
			GameTexts.SetVariable("AMOUNT", this.AmountToPay);
			this.SpendText = GameTexts.FindText("str_cohesion_boost_spend", null).ToString();
			GameTexts.SetVariable("GAIN_AMOUNT", this.AmountOfCohesionToGain);
			this.GainText = GameTexts.FindText("str_cohesion_boost_gain", null).ToString();
		}

		// Token: 0x060020EB RID: 8427 RVA: 0x000779EF File Offset: 0x00075BEF
		private void ExecuteEvent()
		{
			this._onExecuteEvent(this);
		}

		// Token: 0x17000B34 RID: 2868
		// (get) Token: 0x060020EC RID: 8428 RVA: 0x000779FD File Offset: 0x00075BFD
		// (set) Token: 0x060020ED RID: 8429 RVA: 0x00077A05 File Offset: 0x00075C05
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000B35 RID: 2869
		// (get) Token: 0x060020EE RID: 8430 RVA: 0x00077A23 File Offset: 0x00075C23
		// (set) Token: 0x060020EF RID: 8431 RVA: 0x00077A2B File Offset: 0x00075C2B
		[DataSourceProperty]
		public int AmountToPay
		{
			get
			{
				return this._amountToPay;
			}
			set
			{
				if (value != this._amountToPay)
				{
					this._amountToPay = value;
					base.OnPropertyChangedWithValue(value, "AmountToPay");
				}
			}
		}

		// Token: 0x17000B36 RID: 2870
		// (get) Token: 0x060020F0 RID: 8432 RVA: 0x00077A49 File Offset: 0x00075C49
		// (set) Token: 0x060020F1 RID: 8433 RVA: 0x00077A51 File Offset: 0x00075C51
		[DataSourceProperty]
		public int CurrencyType
		{
			get
			{
				return this._currencyType;
			}
			set
			{
				if (value != this._currencyType)
				{
					this._currencyType = value;
					base.OnPropertyChangedWithValue(value, "CurrencyType");
				}
			}
		}

		// Token: 0x17000B37 RID: 2871
		// (get) Token: 0x060020F2 RID: 8434 RVA: 0x00077A6F File Offset: 0x00075C6F
		// (set) Token: 0x060020F3 RID: 8435 RVA: 0x00077A77 File Offset: 0x00075C77
		[DataSourceProperty]
		public int AmountOfCohesionToGain
		{
			get
			{
				return this._amountOfCohesionToGain;
			}
			set
			{
				if (value != this._amountOfCohesionToGain)
				{
					this._amountOfCohesionToGain = value;
					base.OnPropertyChangedWithValue(value, "AmountOfCohesionToGain");
				}
			}
		}

		// Token: 0x17000B38 RID: 2872
		// (get) Token: 0x060020F4 RID: 8436 RVA: 0x00077A95 File Offset: 0x00075C95
		// (set) Token: 0x060020F5 RID: 8437 RVA: 0x00077A9D File Offset: 0x00075C9D
		[DataSourceProperty]
		public string SpendText
		{
			get
			{
				return this._spendText;
			}
			set
			{
				if (value != this._spendText)
				{
					this._spendText = value;
					base.OnPropertyChangedWithValue<string>(value, "SpendText");
				}
			}
		}

		// Token: 0x17000B39 RID: 2873
		// (get) Token: 0x060020F6 RID: 8438 RVA: 0x00077AC0 File Offset: 0x00075CC0
		// (set) Token: 0x060020F7 RID: 8439 RVA: 0x00077AC8 File Offset: 0x00075CC8
		[DataSourceProperty]
		public string GainText
		{
			get
			{
				return this._gainText;
			}
			set
			{
				if (value != this._gainText)
				{
					this._gainText = value;
					base.OnPropertyChangedWithValue<string>(value, "GainText");
				}
			}
		}

		// Token: 0x04000F4A RID: 3914
		private readonly Action<ArmyManagementBoostEventVM> _onExecuteEvent;

		// Token: 0x04000F4B RID: 3915
		private int _amountToPay;

		// Token: 0x04000F4C RID: 3916
		private int _amountOfCohesionToGain;

		// Token: 0x04000F4D RID: 3917
		private int _currencyType;

		// Token: 0x04000F4E RID: 3918
		private string _spendText;

		// Token: 0x04000F4F RID: 3919
		private string _gainText;

		// Token: 0x04000F50 RID: 3920
		private bool _isEnabled;

		// Token: 0x020002E6 RID: 742
		public enum BoostCurrency
		{
			// Token: 0x040013D7 RID: 5079
			Gold,
			// Token: 0x040013D8 RID: 5080
			Influence
		}
	}
}
