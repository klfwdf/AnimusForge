using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000EA RID: 234
	public class EncyclopediaShipStatVM : ViewModel
	{
		// Token: 0x06001590 RID: 5520 RVA: 0x0005470C File Offset: 0x0005290C
		public EncyclopediaShipStatVM(string statId, TextObject name, string value, Func<List<TooltipProperty>> getTooltipProperties = null)
		{
			this._nameTextObj = name;
			this.ValueText = value;
			this.StatId = statId;
			if (getTooltipProperties != null)
			{
				this.Tooltip = new BasicTooltipViewModel(getTooltipProperties);
			}
			else
			{
				this.Tooltip = new BasicTooltipViewModel(() => GameTexts.FindText("str_ship_stat_explanation", this.StatId).ToString());
			}
			this.RefreshValues();
		}

		// Token: 0x06001591 RID: 5521 RVA: 0x00054764 File Offset: 0x00052964
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject textObject = GameTexts.FindText("str_LEFT_colon", null);
			textObject.SetTextVariable("LEFT", this._nameTextObj.ToString());
			this.Name = textObject.ToString();
		}

		// Token: 0x17000721 RID: 1825
		// (get) Token: 0x06001592 RID: 5522 RVA: 0x000547A6 File Offset: 0x000529A6
		// (set) Token: 0x06001593 RID: 5523 RVA: 0x000547AE File Offset: 0x000529AE
		[DataSourceProperty]
		public string StatId
		{
			get
			{
				return this._statId;
			}
			set
			{
				if (value != this._statId)
				{
					this._statId = value;
					base.OnPropertyChangedWithValue<string>(value, "StatId");
				}
			}
		}

		// Token: 0x17000722 RID: 1826
		// (get) Token: 0x06001594 RID: 5524 RVA: 0x000547D1 File Offset: 0x000529D1
		// (set) Token: 0x06001595 RID: 5525 RVA: 0x000547D9 File Offset: 0x000529D9
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

		// Token: 0x17000723 RID: 1827
		// (get) Token: 0x06001596 RID: 5526 RVA: 0x000547FC File Offset: 0x000529FC
		// (set) Token: 0x06001597 RID: 5527 RVA: 0x00054804 File Offset: 0x00052A04
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

		// Token: 0x17000724 RID: 1828
		// (get) Token: 0x06001598 RID: 5528 RVA: 0x00054827 File Offset: 0x00052A27
		// (set) Token: 0x06001599 RID: 5529 RVA: 0x0005482F File Offset: 0x00052A2F
		[DataSourceProperty]
		public BasicTooltipViewModel Tooltip
		{
			get
			{
				return this._tooltip;
			}
			set
			{
				if (value != this._tooltip)
				{
					this._tooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Tooltip");
				}
			}
		}

		// Token: 0x040009D0 RID: 2512
		private readonly TextObject _nameTextObj;

		// Token: 0x040009D1 RID: 2513
		private string _statId;

		// Token: 0x040009D2 RID: 2514
		private string _name;

		// Token: 0x040009D3 RID: 2515
		private string _valueText;

		// Token: 0x040009D4 RID: 2516
		private BasicTooltipViewModel _tooltip;
	}
}
