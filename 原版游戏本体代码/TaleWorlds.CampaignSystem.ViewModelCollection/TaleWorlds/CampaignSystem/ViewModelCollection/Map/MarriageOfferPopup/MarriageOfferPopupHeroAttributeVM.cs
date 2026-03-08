using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup
{
	// Token: 0x02000038 RID: 56
	public class MarriageOfferPopupHeroAttributeVM : ViewModel
	{
		// Token: 0x0600057A RID: 1402 RVA: 0x0001D974 File Offset: 0x0001BB74
		public MarriageOfferPopupHeroAttributeVM(Hero hero, CharacterAttribute attribute)
		{
			this._hero = hero;
			this._attribute = attribute;
			this.FillSkillsList();
			this.RefreshValues();
		}

		// Token: 0x0600057B RID: 1403 RVA: 0x0001D998 File Offset: 0x0001BB98
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject textObject = GameTexts.FindText("str_STR1_space_STR2", null);
			textObject.SetTextVariable("STR1", this._attribute.Name);
			TextObject textObject2 = GameTexts.FindText("str_STR_in_parentheses", null);
			textObject2.SetTextVariable("STR", this._hero.GetAttributeValue(this._attribute));
			textObject.SetTextVariable("STR2", textObject2);
			this._attributeText = textObject.ToString();
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x0001DA10 File Offset: 0x0001BC10
		private void FillSkillsList()
		{
			this._attributeSkills = new MBBindingList<EncyclopediaSkillVM>();
			using (List<SkillObject>.Enumerator enumerator = Skills.All.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					SkillObject skill = enumerator.Current;
					if (!CampaignUIHelper.GetIsNavalSkill(skill) && skill.Attributes.FirstOrDefault<CharacterAttribute>() == this._attribute && !this._attributeSkills.Any((EncyclopediaSkillVM s) => s.SkillId == skill.StringId))
					{
						this._attributeSkills.Add(new EncyclopediaSkillVM(skill, this._hero.GetSkillValue(skill)));
					}
				}
			}
		}

		// Token: 0x1700018A RID: 394
		// (get) Token: 0x0600057D RID: 1405 RVA: 0x0001DADC File Offset: 0x0001BCDC
		// (set) Token: 0x0600057E RID: 1406 RVA: 0x0001DAE4 File Offset: 0x0001BCE4
		[DataSourceProperty]
		public string AttributeText
		{
			get
			{
				return this._attributeText;
			}
			set
			{
				if (value != this._attributeText)
				{
					this._attributeText = value;
					base.OnPropertyChangedWithValue<string>(value, "AttributeText");
				}
			}
		}

		// Token: 0x1700018B RID: 395
		// (get) Token: 0x0600057F RID: 1407 RVA: 0x0001DB07 File Offset: 0x0001BD07
		// (set) Token: 0x06000580 RID: 1408 RVA: 0x0001DB0F File Offset: 0x0001BD0F
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSkillVM> AttributeSkills
		{
			get
			{
				return this._attributeSkills;
			}
			set
			{
				if (value != this._attributeSkills)
				{
					this._attributeSkills = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSkillVM>>(value, "AttributeSkills");
				}
			}
		}

		// Token: 0x04000255 RID: 597
		private readonly Hero _hero;

		// Token: 0x04000256 RID: 598
		private readonly CharacterAttribute _attribute;

		// Token: 0x04000257 RID: 599
		private string _attributeText;

		// Token: 0x04000258 RID: 600
		private MBBindingList<EncyclopediaSkillVM> _attributeSkills;
	}
}
