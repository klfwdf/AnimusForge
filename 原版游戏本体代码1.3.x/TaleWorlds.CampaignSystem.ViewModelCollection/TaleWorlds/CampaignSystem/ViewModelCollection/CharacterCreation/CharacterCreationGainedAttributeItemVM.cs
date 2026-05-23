using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000152 RID: 338
	public class CharacterCreationGainedAttributeItemVM : ViewModel
	{
		// Token: 0x06001FD1 RID: 8145 RVA: 0x00074718 File Offset: 0x00072918
		public CharacterCreationGainedAttributeItemVM(CharacterAttribute attributeObj)
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

		// Token: 0x06001FD2 RID: 8146 RVA: 0x00074779 File Offset: 0x00072979
		internal void ResetValues()
		{
			this.SetValue(0, 0);
		}

		// Token: 0x06001FD3 RID: 8147 RVA: 0x00074784 File Offset: 0x00072984
		public void SetValue(int gainedFromOtherStages, int gainedFromCurrentStage)
		{
			this.HasIncreasedInCurrentStage = gainedFromCurrentStage > 0;
			GameTexts.SetVariable("LEFT", this._attributeObj.Name);
			GameTexts.SetVariable("RIGHT", gainedFromOtherStages + gainedFromCurrentStage);
			this.NameText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
		}

		// Token: 0x17000AD6 RID: 2774
		// (get) Token: 0x06001FD4 RID: 8148 RVA: 0x000747D3 File Offset: 0x000729D3
		// (set) Token: 0x06001FD5 RID: 8149 RVA: 0x000747DB File Offset: 0x000729DB
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

		// Token: 0x17000AD7 RID: 2775
		// (get) Token: 0x06001FD6 RID: 8150 RVA: 0x000747F9 File Offset: 0x000729F9
		// (set) Token: 0x06001FD7 RID: 8151 RVA: 0x00074801 File Offset: 0x00072A01
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

		// Token: 0x17000AD8 RID: 2776
		// (get) Token: 0x06001FD8 RID: 8152 RVA: 0x00074824 File Offset: 0x00072A24
		// (set) Token: 0x06001FD9 RID: 8153 RVA: 0x0007482C File Offset: 0x00072A2C
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

		// Token: 0x04000ED2 RID: 3794
		private readonly CharacterAttribute _attributeObj;

		// Token: 0x04000ED3 RID: 3795
		private string _nameText;

		// Token: 0x04000ED4 RID: 3796
		private bool _hasIncreasedInCurrentStage;

		// Token: 0x04000ED5 RID: 3797
		private BasicTooltipViewModel _hint;
	}
}
