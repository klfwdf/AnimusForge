using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000150 RID: 336
	public class CharacterCreationGainGroupItemVM : ViewModel
	{
		// Token: 0x17000ACE RID: 2766
		// (get) Token: 0x06001FBC RID: 8124 RVA: 0x00074452 File Offset: 0x00072652
		// (set) Token: 0x06001FBD RID: 8125 RVA: 0x0007445A File Offset: 0x0007265A
		public CharacterAttribute AttributeObj { get; private set; }

		// Token: 0x06001FBE RID: 8126 RVA: 0x00074464 File Offset: 0x00072664
		public CharacterCreationGainGroupItemVM(CharacterAttribute attributeObj)
		{
			this.AttributeObj = attributeObj;
			this.Skills = new MBBindingList<CharacterCreationGainedSkillItemVM>();
			this.Attribute = new CharacterCreationGainedAttributeItemVM(this.AttributeObj);
			List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList<SkillObject>();
			list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			using (List<SkillObject>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					SkillObject skill = enumerator.Current;
					if (!CampaignUIHelper.GetIsNavalSkill(skill) && skill.Attributes.FirstOrDefault<CharacterAttribute>() == attributeObj && !this.Skills.Any((CharacterCreationGainedSkillItemVM s) => s.SkillObj == skill))
					{
						this.Skills.Add(new CharacterCreationGainedSkillItemVM(skill));
					}
				}
			}
		}

		// Token: 0x06001FBF RID: 8127 RVA: 0x00074544 File Offset: 0x00072744
		public void ResetValues()
		{
			this.Attribute.ResetValues();
			this.Skills.ApplyActionOnAllItems(delegate(CharacterCreationGainedSkillItemVM s)
			{
				s.ResetValues();
			});
		}

		// Token: 0x17000ACF RID: 2767
		// (get) Token: 0x06001FC0 RID: 8128 RVA: 0x0007457B File Offset: 0x0007277B
		// (set) Token: 0x06001FC1 RID: 8129 RVA: 0x00074583 File Offset: 0x00072783
		[DataSourceProperty]
		public MBBindingList<CharacterCreationGainedSkillItemVM> Skills
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
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationGainedSkillItemVM>>(value, "Skills");
				}
			}
		}

		// Token: 0x17000AD0 RID: 2768
		// (get) Token: 0x06001FC2 RID: 8130 RVA: 0x000745A1 File Offset: 0x000727A1
		// (set) Token: 0x06001FC3 RID: 8131 RVA: 0x000745A9 File Offset: 0x000727A9
		[DataSourceProperty]
		public CharacterCreationGainedAttributeItemVM Attribute
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
					base.OnPropertyChangedWithValue<CharacterCreationGainedAttributeItemVM>(value, "Attribute");
				}
			}
		}

		// Token: 0x04000ECB RID: 3787
		private MBBindingList<CharacterCreationGainedSkillItemVM> _skills;

		// Token: 0x04000ECC RID: 3788
		private CharacterCreationGainedAttributeItemVM _attribute;
	}
}
