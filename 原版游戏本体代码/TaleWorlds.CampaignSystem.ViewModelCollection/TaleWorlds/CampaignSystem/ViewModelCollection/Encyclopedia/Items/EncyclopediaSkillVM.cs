using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000EB RID: 235
	public class EncyclopediaSkillVM : ViewModel
	{
		// Token: 0x0600159B RID: 5531 RVA: 0x00054864 File Offset: 0x00052A64
		public EncyclopediaSkillVM(SkillObject skill, int skillValue)
		{
			this._skill = skill;
			this.SkillValue = skillValue;
			this.SkillId = skill.StringId;
			this.RefreshValues();
		}

		// Token: 0x0600159C RID: 5532 RVA: 0x0005488C File Offset: 0x00052A8C
		public override void RefreshValues()
		{
			base.RefreshValues();
			string name = this._skill.Name.ToString();
			string desc = this._skill.Description.ToString();
			this.Hint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("STR1", name);
				GameTexts.SetVariable("STR2", desc);
				return GameTexts.FindText("str_string_newline_string", null).ToString();
			});
		}

		// Token: 0x17000725 RID: 1829
		// (get) Token: 0x0600159D RID: 5533 RVA: 0x000548E8 File Offset: 0x00052AE8
		// (set) Token: 0x0600159E RID: 5534 RVA: 0x000548F0 File Offset: 0x00052AF0
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

		// Token: 0x17000726 RID: 1830
		// (get) Token: 0x0600159F RID: 5535 RVA: 0x0005490E File Offset: 0x00052B0E
		// (set) Token: 0x060015A0 RID: 5536 RVA: 0x00054916 File Offset: 0x00052B16
		[DataSourceProperty]
		public int SkillValue
		{
			get
			{
				return this._skillValue;
			}
			set
			{
				if (value != this._skillValue)
				{
					this._skillValue = value;
					base.OnPropertyChangedWithValue(value, "SkillValue");
				}
			}
		}

		// Token: 0x17000727 RID: 1831
		// (get) Token: 0x060015A1 RID: 5537 RVA: 0x00054934 File Offset: 0x00052B34
		// (set) Token: 0x060015A2 RID: 5538 RVA: 0x0005493C File Offset: 0x00052B3C
		[DataSourceProperty]
		public string SkillId
		{
			get
			{
				return this._skillId;
			}
			set
			{
				if (value != this._skillId)
				{
					this._skillId = value;
					base.OnPropertyChangedWithValue<string>(value, "SkillId");
				}
			}
		}

		// Token: 0x040009D5 RID: 2517
		private readonly SkillObject _skill;

		// Token: 0x040009D6 RID: 2518
		private string _skillId;

		// Token: 0x040009D7 RID: 2519
		private int _skillValue;

		// Token: 0x040009D8 RID: 2520
		private BasicTooltipViewModel _hint;
	}
}
