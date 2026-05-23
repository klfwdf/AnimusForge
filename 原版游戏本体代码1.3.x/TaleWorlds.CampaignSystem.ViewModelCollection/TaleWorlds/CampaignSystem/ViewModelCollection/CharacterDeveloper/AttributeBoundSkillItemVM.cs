using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000141 RID: 321
	public class AttributeBoundSkillItemVM : ViewModel
	{
		// Token: 0x06001E32 RID: 7730 RVA: 0x0006F674 File Offset: 0x0006D874
		public AttributeBoundSkillItemVM(SkillObject skill)
		{
			this.Name = skill.Name.ToString();
			this.SkillId = skill.StringId;
		}

		// Token: 0x17000A45 RID: 2629
		// (get) Token: 0x06001E33 RID: 7731 RVA: 0x0006F699 File Offset: 0x0006D899
		// (set) Token: 0x06001E34 RID: 7732 RVA: 0x0006F6A1 File Offset: 0x0006D8A1
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

		// Token: 0x17000A46 RID: 2630
		// (get) Token: 0x06001E35 RID: 7733 RVA: 0x0006F6C4 File Offset: 0x0006D8C4
		// (set) Token: 0x06001E36 RID: 7734 RVA: 0x0006F6CC File Offset: 0x0006D8CC
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

		// Token: 0x04000E20 RID: 3616
		private string _name;

		// Token: 0x04000E21 RID: 3617
		private string _skillId;
	}
}
