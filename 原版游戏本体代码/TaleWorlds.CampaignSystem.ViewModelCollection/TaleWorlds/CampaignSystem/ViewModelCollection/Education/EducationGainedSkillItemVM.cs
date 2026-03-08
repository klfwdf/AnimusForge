using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F2 RID: 242
	public class EducationGainedSkillItemVM : ViewModel
	{
		// Token: 0x1700073D RID: 1853
		// (get) Token: 0x060015E0 RID: 5600 RVA: 0x00055A3B File Offset: 0x00053C3B
		// (set) Token: 0x060015E1 RID: 5601 RVA: 0x00055A43 File Offset: 0x00053C43
		public SkillObject SkillObj { get; private set; }

		// Token: 0x060015E2 RID: 5602 RVA: 0x00055A4C File Offset: 0x00053C4C
		public EducationGainedSkillItemVM(SkillObject skill)
		{
			this.FocusPointGainList = new MBBindingList<BoolItemWithActionVM>();
			this.SkillObj = skill;
			this.SkillId = this.SkillObj.StringId;
			this.Skill = new EncyclopediaSkillVM(skill, 0);
		}

		// Token: 0x060015E3 RID: 5603 RVA: 0x00055A84 File Offset: 0x00053C84
		public void SetFocusValue(int gainedFromOtherStages, int gainedFromCurrentStage)
		{
			this.FocusPointGainList.Clear();
			for (int i = 0; i < gainedFromOtherStages; i++)
			{
				this.FocusPointGainList.Add(new BoolItemWithActionVM(null, false, null));
			}
			for (int j = 0; j < gainedFromCurrentStage; j++)
			{
				this.FocusPointGainList.Add(new BoolItemWithActionVM(null, true, null));
			}
			this.HasFocusIncreasedInCurrentStage = gainedFromCurrentStage > 0;
		}

		// Token: 0x060015E4 RID: 5604 RVA: 0x00055AE4 File Offset: 0x00053CE4
		public void SetSkillValue(int gaintedFromOtherStages, int gainedFromCurrentStage)
		{
			this.SkillValueInt = gaintedFromOtherStages + gainedFromCurrentStage;
			this.HasSkillValueIncreasedInCurrentStage = gainedFromCurrentStage > 0;
		}

		// Token: 0x060015E5 RID: 5605 RVA: 0x00055AF9 File Offset: 0x00053CF9
		internal void ResetValues()
		{
			this.SetFocusValue(0, 0);
			this.SetSkillValue(0, 0);
		}

		// Token: 0x1700073E RID: 1854
		// (get) Token: 0x060015E6 RID: 5606 RVA: 0x00055B0B File Offset: 0x00053D0B
		// (set) Token: 0x060015E7 RID: 5607 RVA: 0x00055B13 File Offset: 0x00053D13
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

		// Token: 0x1700073F RID: 1855
		// (get) Token: 0x060015E8 RID: 5608 RVA: 0x00055B36 File Offset: 0x00053D36
		// (set) Token: 0x060015E9 RID: 5609 RVA: 0x00055B3E File Offset: 0x00053D3E
		[DataSourceProperty]
		public int SkillValueInt
		{
			get
			{
				return this._skillValueInt;
			}
			set
			{
				if (value != this._skillValueInt)
				{
					this._skillValueInt = value;
					base.OnPropertyChangedWithValue(value, "SkillValueInt");
				}
			}
		}

		// Token: 0x17000740 RID: 1856
		// (get) Token: 0x060015EA RID: 5610 RVA: 0x00055B5C File Offset: 0x00053D5C
		// (set) Token: 0x060015EB RID: 5611 RVA: 0x00055B64 File Offset: 0x00053D64
		[DataSourceProperty]
		public EncyclopediaSkillVM Skill
		{
			get
			{
				return this._skill;
			}
			set
			{
				if (value != this._skill)
				{
					this._skill = value;
					base.OnPropertyChangedWithValue<EncyclopediaSkillVM>(value, "Skill");
				}
			}
		}

		// Token: 0x17000741 RID: 1857
		// (get) Token: 0x060015EC RID: 5612 RVA: 0x00055B82 File Offset: 0x00053D82
		// (set) Token: 0x060015ED RID: 5613 RVA: 0x00055B8A File Offset: 0x00053D8A
		[DataSourceProperty]
		public bool HasFocusIncreasedInCurrentStage
		{
			get
			{
				return this._hasFocusIncreasedInCurrentStage;
			}
			set
			{
				if (value != this._hasFocusIncreasedInCurrentStage)
				{
					this._hasFocusIncreasedInCurrentStage = value;
					base.OnPropertyChangedWithValue(value, "HasFocusIncreasedInCurrentStage");
				}
			}
		}

		// Token: 0x17000742 RID: 1858
		// (get) Token: 0x060015EE RID: 5614 RVA: 0x00055BA8 File Offset: 0x00053DA8
		// (set) Token: 0x060015EF RID: 5615 RVA: 0x00055BB0 File Offset: 0x00053DB0
		[DataSourceProperty]
		public bool HasSkillValueIncreasedInCurrentStage
		{
			get
			{
				return this._hasSkillValueIncreasedInCurrentStage;
			}
			set
			{
				if (value != this._hasSkillValueIncreasedInCurrentStage)
				{
					this._hasSkillValueIncreasedInCurrentStage = value;
					base.OnPropertyChangedWithValue(value, "HasSkillValueIncreasedInCurrentStage");
				}
			}
		}

		// Token: 0x17000743 RID: 1859
		// (get) Token: 0x060015F0 RID: 5616 RVA: 0x00055BCE File Offset: 0x00053DCE
		// (set) Token: 0x060015F1 RID: 5617 RVA: 0x00055BD6 File Offset: 0x00053DD6
		[DataSourceProperty]
		public MBBindingList<BoolItemWithActionVM> FocusPointGainList
		{
			get
			{
				return this._focusPointGainList;
			}
			set
			{
				if (value != this._focusPointGainList)
				{
					this._focusPointGainList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BoolItemWithActionVM>>(value, "FocusPointGainList");
				}
			}
		}

		// Token: 0x040009F7 RID: 2551
		private string _skillId;

		// Token: 0x040009F8 RID: 2552
		private EncyclopediaSkillVM _skill;

		// Token: 0x040009F9 RID: 2553
		private bool _hasFocusIncreasedInCurrentStage;

		// Token: 0x040009FA RID: 2554
		private bool _hasSkillValueIncreasedInCurrentStage;

		// Token: 0x040009FB RID: 2555
		private int _skillValueInt;

		// Token: 0x040009FC RID: 2556
		private MBBindingList<BoolItemWithActionVM> _focusPointGainList;
	}
}
