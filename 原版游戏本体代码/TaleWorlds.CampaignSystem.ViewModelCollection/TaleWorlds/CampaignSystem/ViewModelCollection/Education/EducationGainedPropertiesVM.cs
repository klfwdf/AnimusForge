using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F0 RID: 240
	public class EducationGainedPropertiesVM : ViewModel
	{
		// Token: 0x060015CE RID: 5582 RVA: 0x0005502C File Offset: 0x0005322C
		public EducationGainedPropertiesVM(Hero child, int pageCount)
		{
			this._child = child;
			this._pageCount = pageCount;
			this._educationBehavior = Campaign.Current.GetCampaignBehavior<IEducationLogic>();
			this._affectedSkillFocusMap = new Dictionary<SkillObject, Tuple<int, int>>();
			this._affectedSkillValueMap = new Dictionary<SkillObject, Tuple<int, int>>();
			this._affectedAttributesMap = new Dictionary<CharacterAttribute, Tuple<int, int>>();
			this.GainGroups = new MBBindingList<EducationGainGroupItemVM>();
			this.OtherSkills = new MBBindingList<EducationGainedSkillItemVM>();
			List<CharacterAttribute> list = Attributes.All.ToList<CharacterAttribute>();
			list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
			foreach (CharacterAttribute attributeObj in list)
			{
				this.GainGroups.Add(new EducationGainGroupItemVM(attributeObj));
			}
			List<SkillObject> list2 = Skills.All.ToList<SkillObject>();
			list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			using (List<SkillObject>.Enumerator enumerator2 = list2.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					SkillObject skill = enumerator2.Current;
					Func<EducationGainedSkillItemVM, bool> <>9__1;
					if (!this.GainGroups.Any(delegate(EducationGainGroupItemVM attribute)
					{
						IEnumerable<EducationGainedSkillItemVM> skills = attribute.Skills;
						Func<EducationGainedSkillItemVM, bool> predicate;
						if ((predicate = <>9__1) == null)
						{
							predicate = (<>9__1 = (EducationGainedSkillItemVM attributeSkill) => attributeSkill.SkillId == skill.StringId);
						}
						return skills.Any(predicate);
					}))
					{
						this.OtherSkills.Add(new EducationGainedSkillItemVM(skill));
					}
				}
			}
			this.UpdateWithSelections(new List<string>(), -1);
		}

		// Token: 0x060015CF RID: 5583 RVA: 0x00055184 File Offset: 0x00053384
		internal void UpdateWithSelections(List<string> selectedOptions, int currentPageIndex)
		{
			this._affectedAttributesMap.Clear();
			this._affectedSkillFocusMap.Clear();
			this._affectedSkillValueMap.Clear();
			this.GainGroups.ApplyActionOnAllItems(delegate(EducationGainGroupItemVM g)
			{
				g.ResetValues();
			});
			this.OtherSkills.ApplyActionOnAllItems(delegate(EducationGainedSkillItemVM s)
			{
				s.ResetValues();
			});
			this.PopulateInitialValues();
			this.PopulateGainedAttributeValues(selectedOptions, currentPageIndex);
			foreach (KeyValuePair<CharacterAttribute, Tuple<int, int>> keyValuePair in this._affectedAttributesMap)
			{
				this.GetItemFromAttribute(keyValuePair.Key).SetValue(keyValuePair.Value.Item1, keyValuePair.Value.Item2);
			}
			foreach (KeyValuePair<SkillObject, Tuple<int, int>> keyValuePair2 in this._affectedSkillFocusMap)
			{
				this.GetItemFromSkill(keyValuePair2.Key).SetFocusValue(keyValuePair2.Value.Item1, keyValuePair2.Value.Item2);
			}
			foreach (KeyValuePair<SkillObject, Tuple<int, int>> keyValuePair3 in this._affectedSkillValueMap)
			{
				this.GetItemFromSkill(keyValuePair3.Key).SetSkillValue(keyValuePair3.Value.Item1, keyValuePair3.Value.Item2);
			}
		}

		// Token: 0x060015D0 RID: 5584 RVA: 0x00055348 File Offset: 0x00053548
		private void PopulateInitialValues()
		{
			foreach (SkillObject skillObject in Skills.All)
			{
				int focus = this._child.HeroDeveloper.GetFocus(skillObject);
				if (this._affectedSkillFocusMap.ContainsKey(skillObject))
				{
					Tuple<int, int> tuple = this._affectedSkillFocusMap[skillObject];
					this._affectedSkillFocusMap[skillObject] = new Tuple<int, int>(tuple.Item1 + focus, 0);
				}
				else
				{
					this._affectedSkillFocusMap.Add(skillObject, new Tuple<int, int>(focus, 0));
				}
				int skillValue = this._child.GetSkillValue(skillObject);
				if (this._affectedSkillValueMap.ContainsKey(skillObject))
				{
					Tuple<int, int> tuple2 = this._affectedSkillValueMap[skillObject];
					this._affectedSkillValueMap[skillObject] = new Tuple<int, int>(tuple2.Item1 + skillValue, 0);
				}
				else
				{
					this._affectedSkillValueMap.Add(skillObject, new Tuple<int, int>(skillValue, 0));
				}
			}
			foreach (CharacterAttribute characterAttribute in Attributes.All)
			{
				int attributeValue = this._child.GetAttributeValue(characterAttribute);
				if (this._affectedAttributesMap.ContainsKey(characterAttribute))
				{
					Tuple<int, int> tuple3 = this._affectedAttributesMap[characterAttribute];
					this._affectedAttributesMap[characterAttribute] = new Tuple<int, int>(tuple3.Item1 + attributeValue, 0);
				}
				else
				{
					this._affectedAttributesMap.Add(characterAttribute, new Tuple<int, int>(attributeValue, 0));
				}
			}
		}

		// Token: 0x060015D1 RID: 5585 RVA: 0x000554F0 File Offset: 0x000536F0
		private void PopulateGainedAttributeValues(List<string> selectedOptions, int currentPageIndex)
		{
			bool flag = currentPageIndex == this._pageCount - 1;
			for (int i = 0; i < selectedOptions.Count; i++)
			{
				string optionKey = selectedOptions[i];
				TextObject textObject;
				TextObject textObject2;
				TextObject textObject3;
				ValueTuple<CharacterAttribute, int>[] array;
				ValueTuple<SkillObject, int>[] array2;
				ValueTuple<SkillObject, int>[] array3;
				EducationCampaignBehavior.EducationCharacterProperties[] array4;
				this._educationBehavior.GetOptionProperties(this._child, optionKey, selectedOptions, out textObject, out textObject2, out textObject3, out array, out array2, out array3, out array4);
				bool flag2 = i == currentPageIndex;
				if (array != null)
				{
					foreach (ValueTuple<CharacterAttribute, int> valueTuple in array)
					{
						Tuple<int, int> tuple = this._affectedAttributesMap[valueTuple.Item1];
						int item = (flag2 ? valueTuple.Item2 : (flag ? (tuple.Item2 + valueTuple.Item2) : 0));
						int item2 = (flag2 ? tuple.Item1 : (flag ? tuple.Item1 : (tuple.Item1 + valueTuple.Item2)));
						this._affectedAttributesMap[valueTuple.Item1] = new Tuple<int, int>(item2, item);
					}
				}
				if (array2 != null)
				{
					foreach (ValueTuple<SkillObject, int> valueTuple2 in array2)
					{
						Tuple<int, int> tuple2 = this._affectedSkillValueMap[valueTuple2.Item1];
						int item3 = (flag2 ? valueTuple2.Item2 : (flag ? (tuple2.Item2 + valueTuple2.Item2) : 0));
						int item4 = (flag2 ? tuple2.Item1 : (flag ? tuple2.Item1 : (tuple2.Item1 + valueTuple2.Item2)));
						this._affectedSkillValueMap[valueTuple2.Item1] = new Tuple<int, int>(item4, item3);
					}
				}
				if (array3 != null)
				{
					foreach (ValueTuple<SkillObject, int> valueTuple3 in array3)
					{
						Tuple<int, int> tuple3 = this._affectedSkillFocusMap[valueTuple3.Item1];
						int num = (flag2 ? valueTuple3.Item2 : (flag ? (tuple3.Item2 + valueTuple3.Item2) : 0));
						int num2 = (flag2 ? tuple3.Item1 : (flag ? tuple3.Item1 : (tuple3.Item1 + valueTuple3.Item2)));
						num2 = Math.Min(num2, 5);
						num = Math.Min(num, 5 - num2);
						this._affectedSkillFocusMap[valueTuple3.Item1] = new Tuple<int, int>(num2, num);
					}
				}
			}
		}

		// Token: 0x060015D2 RID: 5586 RVA: 0x00055768 File Offset: 0x00053968
		private EducationGainedAttributeItemVM GetItemFromAttribute(CharacterAttribute attribute)
		{
			EducationGainGroupItemVM educationGainGroupItemVM = this.GainGroups.SingleOrDefault((EducationGainGroupItemVM g) => g.AttributeObj == attribute);
			if (educationGainGroupItemVM == null)
			{
				return null;
			}
			return educationGainGroupItemVM.Attribute;
		}

		// Token: 0x060015D3 RID: 5587 RVA: 0x000557A4 File Offset: 0x000539A4
		private EducationGainedSkillItemVM GetItemFromSkill(SkillObject skill)
		{
			foreach (EducationGainGroupItemVM educationGainGroupItemVM in this.GainGroups)
			{
				foreach (EducationGainedSkillItemVM educationGainedSkillItemVM in educationGainGroupItemVM.Skills)
				{
					if (educationGainedSkillItemVM.SkillObj == skill)
					{
						return educationGainedSkillItemVM;
					}
				}
			}
			foreach (EducationGainedSkillItemVM educationGainedSkillItemVM2 in this.OtherSkills)
			{
				if (educationGainedSkillItemVM2.SkillObj == skill)
				{
					return educationGainedSkillItemVM2;
				}
			}
			return null;
		}

		// Token: 0x17000738 RID: 1848
		// (get) Token: 0x060015D4 RID: 5588 RVA: 0x00055874 File Offset: 0x00053A74
		// (set) Token: 0x060015D5 RID: 5589 RVA: 0x0005587C File Offset: 0x00053A7C
		[DataSourceProperty]
		public MBBindingList<EducationGainGroupItemVM> GainGroups
		{
			get
			{
				return this._gainGroups;
			}
			set
			{
				if (value != this._gainGroups)
				{
					this._gainGroups = value;
					base.OnPropertyChangedWithValue<MBBindingList<EducationGainGroupItemVM>>(value, "GainGroups");
				}
			}
		}

		// Token: 0x17000739 RID: 1849
		// (get) Token: 0x060015D6 RID: 5590 RVA: 0x0005589A File Offset: 0x00053A9A
		// (set) Token: 0x060015D7 RID: 5591 RVA: 0x000558A2 File Offset: 0x00053AA2
		[DataSourceProperty]
		public MBBindingList<EducationGainedSkillItemVM> OtherSkills
		{
			get
			{
				return this._otherSkills;
			}
			set
			{
				if (value != this._otherSkills)
				{
					this._otherSkills = value;
					base.OnPropertyChangedWithValue<MBBindingList<EducationGainedSkillItemVM>>(value, "OtherSkills");
				}
			}
		}

		// Token: 0x040009EB RID: 2539
		private readonly Hero _child;

		// Token: 0x040009EC RID: 2540
		private readonly int _pageCount;

		// Token: 0x040009ED RID: 2541
		private readonly IEducationLogic _educationBehavior;

		// Token: 0x040009EE RID: 2542
		private readonly Dictionary<CharacterAttribute, Tuple<int, int>> _affectedAttributesMap;

		// Token: 0x040009EF RID: 2543
		private readonly Dictionary<SkillObject, Tuple<int, int>> _affectedSkillFocusMap;

		// Token: 0x040009F0 RID: 2544
		private readonly Dictionary<SkillObject, Tuple<int, int>> _affectedSkillValueMap;

		// Token: 0x040009F1 RID: 2545
		private MBBindingList<EducationGainGroupItemVM> _gainGroups;

		// Token: 0x040009F2 RID: 2546
		private MBBindingList<EducationGainedSkillItemVM> _otherSkills;
	}
}
