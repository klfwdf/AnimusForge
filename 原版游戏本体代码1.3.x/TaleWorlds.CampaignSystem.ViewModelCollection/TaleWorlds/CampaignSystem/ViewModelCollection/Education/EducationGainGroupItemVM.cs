using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F1 RID: 241
	public class EducationGainGroupItemVM : ViewModel
	{
		// Token: 0x1700073A RID: 1850
		// (get) Token: 0x060015D8 RID: 5592 RVA: 0x000558C0 File Offset: 0x00053AC0
		// (set) Token: 0x060015D9 RID: 5593 RVA: 0x000558C8 File Offset: 0x00053AC8
		public CharacterAttribute AttributeObj { get; private set; }

		// Token: 0x060015DA RID: 5594 RVA: 0x000558D4 File Offset: 0x00053AD4
		public EducationGainGroupItemVM(CharacterAttribute attributeObj)
		{
			this.AttributeObj = attributeObj;
			this.Skills = new MBBindingList<EducationGainedSkillItemVM>();
			this.Attribute = new EducationGainedAttributeItemVM(this.AttributeObj);
			List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList<SkillObject>();
			list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			using (List<SkillObject>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					SkillObject skill = enumerator.Current;
					if (!CampaignUIHelper.GetIsNavalSkill(skill) && skill.Attributes.FirstOrDefault<CharacterAttribute>() == this.AttributeObj && !this.Skills.Any((EducationGainedSkillItemVM s) => s.SkillObj == skill))
					{
						this.Skills.Add(new EducationGainedSkillItemVM(skill));
					}
				}
			}
		}

		// Token: 0x060015DB RID: 5595 RVA: 0x000559B8 File Offset: 0x00053BB8
		public void ResetValues()
		{
			this.Attribute.ResetValues();
			this.Skills.ApplyActionOnAllItems(delegate(EducationGainedSkillItemVM s)
			{
				s.ResetValues();
			});
		}

		// Token: 0x1700073B RID: 1851
		// (get) Token: 0x060015DC RID: 5596 RVA: 0x000559EF File Offset: 0x00053BEF
		// (set) Token: 0x060015DD RID: 5597 RVA: 0x000559F7 File Offset: 0x00053BF7
		[DataSourceProperty]
		public MBBindingList<EducationGainedSkillItemVM> Skills
		{
			get
			{
				return this._skills;
			}
			set
			{
				if (value != this._skills)
				{
					this._skills = value;
					base.OnPropertyChangedWithValue<MBBindingList<EducationGainedSkillItemVM>>(value, "Skills");
				}
			}
		}

		// Token: 0x1700073C RID: 1852
		// (get) Token: 0x060015DE RID: 5598 RVA: 0x00055A15 File Offset: 0x00053C15
		// (set) Token: 0x060015DF RID: 5599 RVA: 0x00055A1D File Offset: 0x00053C1D
		[DataSourceProperty]
		public EducationGainedAttributeItemVM Attribute
		{
			get
			{
				return this._attribute;
			}
			set
			{
				if (value != this._attribute)
				{
					this._attribute = value;
					base.OnPropertyChangedWithValue<EducationGainedAttributeItemVM>(value, "Attribute");
				}
			}
		}

		// Token: 0x040009F4 RID: 2548
		private MBBindingList<EducationGainedSkillItemVM> _skills;

		// Token: 0x040009F5 RID: 2549
		private EducationGainedAttributeItemVM _attribute;
	}
}
