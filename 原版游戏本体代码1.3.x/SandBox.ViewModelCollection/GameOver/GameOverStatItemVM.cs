using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.GameOver
{
	// Token: 0x02000059 RID: 89
	public class GameOverStatItemVM : ViewModel
	{
		// Token: 0x06000582 RID: 1410 RVA: 0x00014A2B File Offset: 0x00012C2B
		public GameOverStatItemVM(StatItem item)
		{
			this._item = item;
			this.RefreshValues();
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x00014A40 File Offset: 0x00012C40
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DefinitionText = GameTexts.FindText("str_game_over_stat_item", this._item.ID).ToString();
			this.ValueText = this._item.Value;
			this.StatTypeAsString = Enum.GetName(typeof(StatItem.StatType), this._item.Type);
		}

		// Token: 0x170001A3 RID: 419
		// (get) Token: 0x06000584 RID: 1412 RVA: 0x00014AA9 File Offset: 0x00012CA9
		// (set) Token: 0x06000585 RID: 1413 RVA: 0x00014AB1 File Offset: 0x00012CB1
		[DataSourceProperty]
		public string DefinitionText
		{
			get
			{
				return this._definitionText;
			}
			set
			{
				if (value != this._definitionText)
				{
					this._definitionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DefinitionText");
				}
			}
		}

		// Token: 0x170001A4 RID: 420
		// (get) Token: 0x06000586 RID: 1414 RVA: 0x00014AD4 File Offset: 0x00012CD4
		// (set) Token: 0x06000587 RID: 1415 RVA: 0x00014ADC File Offset: 0x00012CDC
		[DataSourceProperty]
		public string ValueText
		{
			get
			{
				return this._valueText;
			}
			set
			{
				if (value != this._valueText)
				{
					this._valueText = value;
					base.OnPropertyChangedWithValue<string>(value, "ValueText");
				}
			}
		}

		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x06000588 RID: 1416 RVA: 0x00014AFF File Offset: 0x00012CFF
		// (set) Token: 0x06000589 RID: 1417 RVA: 0x00014B07 File Offset: 0x00012D07
		[DataSourceProperty]
		public string StatTypeAsString
		{
			get
			{
				return this._statTypeAsString;
			}
			set
			{
				if (value != this._statTypeAsString)
				{
					this._statTypeAsString = value;
					base.OnPropertyChangedWithValue<string>(value, "StatTypeAsString");
				}
			}
		}

		// Token: 0x040002B7 RID: 695
		private readonly StatItem _item;

		// Token: 0x040002B8 RID: 696
		private string _definitionText;

		// Token: 0x040002B9 RID: 697
		private string _valueText;

		// Token: 0x040002BA RID: 698
		private string _statTypeAsString;
	}
}
