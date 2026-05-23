using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F3 RID: 243
	public class EducationGainedAttributeItemVM : ViewModel
	{
		// Token: 0x060015F2 RID: 5618 RVA: 0x00055BF4 File Offset: 0x00053DF4
		public EducationGainedAttributeItemVM(CharacterAttribute attributeObj)
		{
			this._attributeObj = attributeObj;
			TextObject nameExtended = this._attributeObj.Name;
			TextObject desc = this._attributeObj.Description;
			this.Hint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("STR1", nameExtended);
				GameTexts.SetVariable("STR2", desc);
				return GameTexts.FindText("str_string_newline_string", null).ToString();
			});
			this.SetValue(0, 0);
		}

		// Token: 0x060015F3 RID: 5619 RVA: 0x00055C55 File Offset: 0x00053E55
		internal void ResetValues()
		{
			this.SetValue(0, 0);
		}

		// Token: 0x060015F4 RID: 5620 RVA: 0x00055C60 File Offset: 0x00053E60
		public void SetValue(int gainedFromOtherStages, int gainedFromCurrentStage)
		{
			this.HasIncreasedInCurrentStage = gainedFromCurrentStage > 0;
			GameTexts.SetVariable("LEFT", this._attributeObj.Name);
			GameTexts.SetVariable("RIGHT", gainedFromOtherStages + gainedFromCurrentStage);
			this.NameText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
		}

		// Token: 0x17000744 RID: 1860
		// (get) Token: 0x060015F5 RID: 5621 RVA: 0x00055CAF File Offset: 0x00053EAF
		// (set) Token: 0x060015F6 RID: 5622 RVA: 0x00055CB7 File Offset: 0x00053EB7
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

		// Token: 0x17000745 RID: 1861
		// (get) Token: 0x060015F7 RID: 5623 RVA: 0x00055CD5 File Offset: 0x00053ED5
		// (set) Token: 0x060015F8 RID: 5624 RVA: 0x00055CDD File Offset: 0x00053EDD
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x17000746 RID: 1862
		// (get) Token: 0x060015F9 RID: 5625 RVA: 0x00055D00 File Offset: 0x00053F00
		// (set) Token: 0x060015FA RID: 5626 RVA: 0x00055D08 File Offset: 0x00053F08
		[DataSourceProperty]
		public bool HasIncreasedInCurrentStage
		{
			get
			{
				return this._hasIncreasedInCurrentStage;
			}
			set
			{
				if (value != this._hasIncreasedInCurrentStage)
				{
					this._hasIncreasedInCurrentStage = value;
					base.OnPropertyChangedWithValue(value, "HasIncreasedInCurrentStage");
				}
			}
		}

		// Token: 0x040009FD RID: 2557
		private readonly CharacterAttribute _attributeObj;

		// Token: 0x040009FE RID: 2558
		private string _nameText;

		// Token: 0x040009FF RID: 2559
		private bool _hasIncreasedInCurrentStage;

		// Token: 0x04000A00 RID: 2560
		private BasicTooltipViewModel _hint;
	}
}
