using System;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000154 RID: 340
	public class CharacterCreationOptionVM : ViewModel
	{
		// Token: 0x06001FED RID: 8173 RVA: 0x00074C02 File Offset: 0x00072E02
		public CharacterCreationOptionVM(Action<CharacterCreationOptionVM> onSelect, NarrativeMenuOption option)
		{
			this._onSelect = onSelect;
			this.Option = option;
			this.RefreshValues();
		}

		// Token: 0x06001FEE RID: 8174 RVA: 0x00074C20 File Offset: 0x00072E20
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ActionText = this.Option.Text.ToString();
			this.PositiveEffectText = this.Option.PositiveEffectText.ToString();
			this.DescriptionText = this.Option.DescriptionText.ToString();
		}

		// Token: 0x06001FEF RID: 8175 RVA: 0x00074C75 File Offset: 0x00072E75
		public void ExecuteSelect()
		{
			Action<CharacterCreationOptionVM> onSelect = this._onSelect;
			if (onSelect == null)
			{
				return;
			}
			onSelect(this);
		}

		// Token: 0x17000ADE RID: 2782
		// (get) Token: 0x06001FF0 RID: 8176 RVA: 0x00074C88 File Offset: 0x00072E88
		// (set) Token: 0x06001FF1 RID: 8177 RVA: 0x00074C90 File Offset: 0x00072E90
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000ADF RID: 2783
		// (get) Token: 0x06001FF2 RID: 8178 RVA: 0x00074CAE File Offset: 0x00072EAE
		// (set) Token: 0x06001FF3 RID: 8179 RVA: 0x00074CB6 File Offset: 0x00072EB6
		[DataSourceProperty]
		public string ActionText
		{
			get
			{
				return this._actionText;
			}
			set
			{
				if (value != this._actionText)
				{
					this._actionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActionText");
				}
			}
		}

		// Token: 0x17000AE0 RID: 2784
		// (get) Token: 0x06001FF4 RID: 8180 RVA: 0x00074CD9 File Offset: 0x00072ED9
		// (set) Token: 0x06001FF5 RID: 8181 RVA: 0x00074CE1 File Offset: 0x00072EE1
		[DataSourceProperty]
		public string PositiveEffectText
		{
			get
			{
				return this._positiveEffectText;
			}
			set
			{
				if (value != this._positiveEffectText)
				{
					this._positiveEffectText = value;
					base.OnPropertyChangedWithValue<string>(value, "PositiveEffectText");
				}
			}
		}

		// Token: 0x17000AE1 RID: 2785
		// (get) Token: 0x06001FF6 RID: 8182 RVA: 0x00074D04 File Offset: 0x00072F04
		// (set) Token: 0x06001FF7 RID: 8183 RVA: 0x00074D0C File Offset: 0x00072F0C
		[DataSourceProperty]
		public string NegativeEffectText
		{
			get
			{
				return this._negativeEffectText;
			}
			set
			{
				if (value != this._negativeEffectText)
				{
					this._negativeEffectText = value;
					base.OnPropertyChangedWithValue<string>(value, "NegativeEffectText");
				}
			}
		}

		// Token: 0x17000AE2 RID: 2786
		// (get) Token: 0x06001FF8 RID: 8184 RVA: 0x00074D2F File Offset: 0x00072F2F
		// (set) Token: 0x06001FF9 RID: 8185 RVA: 0x00074D37 File Offset: 0x00072F37
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x04000EDD RID: 3805
		public readonly NarrativeMenuOption Option;

		// Token: 0x04000EDE RID: 3806
		private readonly Action<CharacterCreationOptionVM> _onSelect;

		// Token: 0x04000EDF RID: 3807
		private bool _isSelected;

		// Token: 0x04000EE0 RID: 3808
		private string _actionText;

		// Token: 0x04000EE1 RID: 3809
		private string _positiveEffectText;

		// Token: 0x04000EE2 RID: 3810
		private string _negativeEffectText;

		// Token: 0x04000EE3 RID: 3811
		private string _descriptionText;
	}
}
