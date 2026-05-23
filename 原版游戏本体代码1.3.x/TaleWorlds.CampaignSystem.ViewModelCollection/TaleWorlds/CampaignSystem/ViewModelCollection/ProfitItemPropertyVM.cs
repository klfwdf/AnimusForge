using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200001C RID: 28
	public class ProfitItemPropertyVM : ViewModel
	{
		// Token: 0x060001AF RID: 431 RVA: 0x0000C436 File Offset: 0x0000A636
		public ProfitItemPropertyVM(string name, int value, ProfitItemPropertyVM.PropertyType type = ProfitItemPropertyVM.PropertyType.None, CharacterImageIdentifierVM governorVisual = null, BasicTooltipViewModel hint = null)
		{
			this.Name = name;
			this.Value = value;
			this.Type = (int)type;
			this.GovernorVisual = governorVisual;
			this.Hint = hint;
			this.RefreshValues();
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x0000C469 File Offset: 0x0000A669
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ColonText = GameTexts.FindText("str_colon", null).ToString();
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060001B1 RID: 433 RVA: 0x0000C487 File Offset: 0x0000A687
		// (set) Token: 0x060001B2 RID: 434 RVA: 0x0000C48F File Offset: 0x0000A68F
		[DataSourceProperty]
		public int Type
		{
			get
			{
				return this._type;
			}
			set
			{
				if (value != this._type)
				{
					this._type = value;
					this.ShowGovernorPortrait = this._type == 5;
					base.OnPropertyChangedWithValue(value, "Type");
				}
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x0000C4BC File Offset: 0x0000A6BC
		// (set) Token: 0x060001B4 RID: 436 RVA: 0x0000C4C4 File Offset: 0x0000A6C4
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

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060001B5 RID: 437 RVA: 0x0000C4E7 File Offset: 0x0000A6E7
		// (set) Token: 0x060001B6 RID: 438 RVA: 0x0000C4EF File Offset: 0x0000A6EF
		[DataSourceProperty]
		public int Value
		{
			get
			{
				return this._value;
			}
			set
			{
				if (value != this._value)
				{
					this._value = value;
					this.ValueString = this._value.ToString("+0;-#");
					base.OnPropertyChangedWithValue(value, "Value");
				}
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060001B7 RID: 439 RVA: 0x0000C523 File Offset: 0x0000A723
		// (set) Token: 0x060001B8 RID: 440 RVA: 0x0000C52B File Offset: 0x0000A72B
		[DataSourceProperty]
		public string ValueString
		{
			get
			{
				return this._valueString;
			}
			private set
			{
				if (value != this._valueString)
				{
					this._valueString = value;
					base.OnPropertyChangedWithValue<string>(value, "ValueString");
				}
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060001B9 RID: 441 RVA: 0x0000C54E File Offset: 0x0000A74E
		// (set) Token: 0x060001BA RID: 442 RVA: 0x0000C556 File Offset: 0x0000A756
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
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
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060001BB RID: 443 RVA: 0x0000C574 File Offset: 0x0000A774
		// (set) Token: 0x060001BC RID: 444 RVA: 0x0000C57C File Offset: 0x0000A77C
		[DataSourceProperty]
		public string ColonText
		{
			get
			{
				return this._colonText;
			}
			set
			{
				if (value != this._colonText)
				{
					this._colonText = value;
					base.OnPropertyChangedWithValue<string>(value, "ColonText");
				}
			}
		}

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060001BD RID: 445 RVA: 0x0000C59F File Offset: 0x0000A79F
		// (set) Token: 0x060001BE RID: 446 RVA: 0x0000C5A7 File Offset: 0x0000A7A7
		[DataSourceProperty]
		public CharacterImageIdentifierVM GovernorVisual
		{
			get
			{
				return this._governorVisual;
			}
			set
			{
				if (value != this._governorVisual)
				{
					this._governorVisual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "GovernorVisual");
				}
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060001BF RID: 447 RVA: 0x0000C5C5 File Offset: 0x0000A7C5
		// (set) Token: 0x060001C0 RID: 448 RVA: 0x0000C5CD File Offset: 0x0000A7CD
		[DataSourceProperty]
		public bool ShowGovernorPortrait
		{
			get
			{
				return this._showGovernorPortrait;
			}
			private set
			{
				if (value != this._showGovernorPortrait)
				{
					this._showGovernorPortrait = value;
					base.OnPropertyChangedWithValue(value, "ShowGovernorPortrait");
				}
			}
		}

		// Token: 0x040000CA RID: 202
		private int _type;

		// Token: 0x040000CB RID: 203
		private string _name;

		// Token: 0x040000CC RID: 204
		private int _value;

		// Token: 0x040000CD RID: 205
		private string _valueString;

		// Token: 0x040000CE RID: 206
		private BasicTooltipViewModel _hint;

		// Token: 0x040000CF RID: 207
		private string _colonText;

		// Token: 0x040000D0 RID: 208
		private CharacterImageIdentifierVM _governorVisual;

		// Token: 0x040000D1 RID: 209
		private bool _showGovernorPortrait;

		// Token: 0x02000179 RID: 377
		public enum PropertyType
		{
			// Token: 0x04001011 RID: 4113
			None,
			// Token: 0x04001012 RID: 4114
			Tax,
			// Token: 0x04001013 RID: 4115
			Tariff,
			// Token: 0x04001014 RID: 4116
			Garrison,
			// Token: 0x04001015 RID: 4117
			Village,
			// Token: 0x04001016 RID: 4118
			Governor
		}
	}
}
