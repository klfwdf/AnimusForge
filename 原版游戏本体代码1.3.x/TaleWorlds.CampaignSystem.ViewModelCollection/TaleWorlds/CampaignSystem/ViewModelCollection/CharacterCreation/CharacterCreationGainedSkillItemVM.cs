using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000151 RID: 337
	public class CharacterCreationGainedSkillItemVM : ViewModel
	{
		// Token: 0x17000AD1 RID: 2769
		// (get) Token: 0x06001FC4 RID: 8132 RVA: 0x000745C7 File Offset: 0x000727C7
		// (set) Token: 0x06001FC5 RID: 8133 RVA: 0x000745CF File Offset: 0x000727CF
		public SkillObject SkillObj { get; private set; }

		// Token: 0x06001FC6 RID: 8134 RVA: 0x000745D8 File Offset: 0x000727D8
		public CharacterCreationGainedSkillItemVM(SkillObject skill)
		{
			this.FocusPointGainList = new MBBindingList<BoolItemWithActionVM>();
			this.SkillObj = skill;
			this.SkillId = this.SkillObj.StringId;
			this.Skill = new EncyclopediaSkillVM(skill, 0);
		}

		// Token: 0x06001FC7 RID: 8135 RVA: 0x00074610 File Offset: 0x00072810
		public void SetValue(int gainedFromOtherStages, int gainedFromCurrentStage)
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
			this.HasIncreasedInCurrentStage = gainedFromCurrentStage > 0;
		}

		// Token: 0x06001FC8 RID: 8136 RVA: 0x00074670 File Offset: 0x00072870
		internal void ResetValues()
		{
			this.SetValue(0, 0);
		}

		// Token: 0x17000AD2 RID: 2770
		// (get) Token: 0x06001FC9 RID: 8137 RVA: 0x0007467A File Offset: 0x0007287A
		// (set) Token: 0x06001FCA RID: 8138 RVA: 0x00074682 File Offset: 0x00072882
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

		// Token: 0x17000AD3 RID: 2771
		// (get) Token: 0x06001FCB RID: 8139 RVA: 0x000746A5 File Offset: 0x000728A5
		// (set) Token: 0x06001FCC RID: 8140 RVA: 0x000746AD File Offset: 0x000728AD
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

		// Token: 0x17000AD4 RID: 2772
		// (get) Token: 0x06001FCD RID: 8141 RVA: 0x000746CB File Offset: 0x000728CB
		// (set) Token: 0x06001FCE RID: 8142 RVA: 0x000746D3 File Offset: 0x000728D3
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

		// Token: 0x17000AD5 RID: 2773
		// (get) Token: 0x06001FCF RID: 8143 RVA: 0x000746F1 File Offset: 0x000728F1
		// (set) Token: 0x06001FD0 RID: 8144 RVA: 0x000746F9 File Offset: 0x000728F9
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

		// Token: 0x04000ECE RID: 3790
		private string _skillId;

		// Token: 0x04000ECF RID: 3791
		private EncyclopediaSkillVM _skill;

		// Token: 0x04000ED0 RID: 3792
		private bool _hasIncreasedInCurrentStage;

		// Token: 0x04000ED1 RID: 3793
		private MBBindingList<BoolItemWithActionVM> _focusPointGainList;
	}
}
