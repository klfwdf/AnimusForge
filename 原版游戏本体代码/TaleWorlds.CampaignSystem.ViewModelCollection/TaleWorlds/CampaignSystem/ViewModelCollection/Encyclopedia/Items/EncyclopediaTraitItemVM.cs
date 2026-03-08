using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000EC RID: 236
	public class EncyclopediaTraitItemVM : ViewModel
	{
		// Token: 0x060015A3 RID: 5539 RVA: 0x00054960 File Offset: 0x00052B60
		public EncyclopediaTraitItemVM(TraitObject traitObj, int value)
		{
			this._traitObj = traitObj;
			this.TraitId = traitObj.StringId;
			this.Value = value;
			string traitTooltipText = CampaignUIHelper.GetTraitTooltipText(traitObj, this.Value);
			this.Hint = new HintViewModel(new TextObject("{=!}" + traitTooltipText, null), null);
		}

		// Token: 0x060015A4 RID: 5540 RVA: 0x000549B7 File Offset: 0x00052BB7
		public EncyclopediaTraitItemVM(TraitObject traitObj, Hero hero)
			: this(traitObj, hero.GetTraitLevel(traitObj))
		{
		}

		// Token: 0x17000728 RID: 1832
		// (get) Token: 0x060015A5 RID: 5541 RVA: 0x000549C7 File Offset: 0x00052BC7
		// (set) Token: 0x060015A6 RID: 5542 RVA: 0x000549CF File Offset: 0x00052BCF
		[DataSourceProperty]
		public string TraitId
		{
			get
			{
				return this._traitId;
			}
			set
			{
				if (value != this._traitId)
				{
					this._traitId = value;
					base.OnPropertyChangedWithValue<string>(value, "TraitId");
				}
			}
		}

		// Token: 0x17000729 RID: 1833
		// (get) Token: 0x060015A7 RID: 5543 RVA: 0x000549F2 File Offset: 0x00052BF2
		// (set) Token: 0x060015A8 RID: 5544 RVA: 0x000549FA File Offset: 0x00052BFA
		[DataSourceProperty]
		public HintViewModel Hint
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
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x1700072A RID: 1834
		// (get) Token: 0x060015A9 RID: 5545 RVA: 0x00054A18 File Offset: 0x00052C18
		// (set) Token: 0x060015AA RID: 5546 RVA: 0x00054A20 File Offset: 0x00052C20
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
					base.OnPropertyChangedWithValue(value, "Value");
				}
			}
		}

		// Token: 0x040009D9 RID: 2521
		private readonly TraitObject _traitObj;

		// Token: 0x040009DA RID: 2522
		private string _traitId;

		// Token: 0x040009DB RID: 2523
		private int _value;

		// Token: 0x040009DC RID: 2524
		private HintViewModel _hint;
	}
}
