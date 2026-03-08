using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200001E RID: 30
	public class SelectableItemPropertyVM : ViewModel
	{
		// Token: 0x060001C4 RID: 452 RVA: 0x0000C62E File Offset: 0x0000A82E
		public SelectableItemPropertyVM(string name, string value, bool isWarning = false, BasicTooltipViewModel hint = null)
		{
			this.Name = name;
			this.Value = value;
			this.Hint = hint;
			this.Type = 0;
			this.IsWarning = isWarning;
			this.RefreshValues();
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0000C660 File Offset: 0x0000A860
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ColonText = GameTexts.FindText("str_colon", null).ToString();
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0000C67E File Offset: 0x0000A87E
		private void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x060001C7 RID: 455 RVA: 0x0000C690 File Offset: 0x0000A890
		// (set) Token: 0x060001C8 RID: 456 RVA: 0x0000C698 File Offset: 0x0000A898
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
					base.OnPropertyChangedWithValue(value, "Type");
				}
			}
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x0000C6B6 File Offset: 0x0000A8B6
		// (set) Token: 0x060001CA RID: 458 RVA: 0x0000C6BE File Offset: 0x0000A8BE
		[DataSourceProperty]
		public bool IsWarning
		{
			get
			{
				return this._isWarning;
			}
			set
			{
				if (value != this._isWarning)
				{
					this._isWarning = value;
					base.OnPropertyChangedWithValue(value, "IsWarning");
				}
			}
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x060001CB RID: 459 RVA: 0x0000C6DC File Offset: 0x0000A8DC
		// (set) Token: 0x060001CC RID: 460 RVA: 0x0000C6E4 File Offset: 0x0000A8E4
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

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x060001CD RID: 461 RVA: 0x0000C707 File Offset: 0x0000A907
		// (set) Token: 0x060001CE RID: 462 RVA: 0x0000C70F File Offset: 0x0000A90F
		[DataSourceProperty]
		public string Value
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
					base.OnPropertyChangedWithValue<string>(value, "Value");
				}
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060001CF RID: 463 RVA: 0x0000C732 File Offset: 0x0000A932
		// (set) Token: 0x060001D0 RID: 464 RVA: 0x0000C73A File Offset: 0x0000A93A
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

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x0000C758 File Offset: 0x0000A958
		// (set) Token: 0x060001D2 RID: 466 RVA: 0x0000C760 File Offset: 0x0000A960
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

		// Token: 0x040000D3 RID: 211
		private int _type;

		// Token: 0x040000D4 RID: 212
		private bool _isWarning;

		// Token: 0x040000D5 RID: 213
		private string _name;

		// Token: 0x040000D6 RID: 214
		private string _value;

		// Token: 0x040000D7 RID: 215
		private BasicTooltipViewModel _hint;

		// Token: 0x040000D8 RID: 216
		private string _colonText;

		// Token: 0x0200017A RID: 378
		public enum PropertyType
		{
			// Token: 0x04001018 RID: 4120
			None,
			// Token: 0x04001019 RID: 4121
			Wall,
			// Token: 0x0400101A RID: 4122
			Garrison,
			// Token: 0x0400101B RID: 4123
			Militia,
			// Token: 0x0400101C RID: 4124
			Prosperity,
			// Token: 0x0400101D RID: 4125
			Food,
			// Token: 0x0400101E RID: 4126
			Loyalty,
			// Token: 0x0400101F RID: 4127
			Security,
			// Token: 0x04001020 RID: 4128
			Shipyard,
			// Token: 0x04001021 RID: 4129
			Patrol,
			// Token: 0x04001022 RID: 4130
			CoastalPatrol
		}
	}
}
