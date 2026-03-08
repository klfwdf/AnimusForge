using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x0200014F RID: 335
	public class CharacterCreationGainedPropertiesVM : ViewModel
	{
		// Token: 0x06001FAF RID: 8111 RVA: 0x00073BE4 File Offset: 0x00071DE4
		public CharacterCreationGainedPropertiesVM(CharacterCreationManager characterCreationManager)
		{
			this._characterCreationManager = characterCreationManager;
			this._affectedAttributesMap = new Dictionary<CharacterAttribute, Tuple<int, int>>();
			this._affectedSkillMap = new Dictionary<SkillObject, Tuple<int, int>>();
			this.GainGroups = new MBBindingList<CharacterCreationGainGroupItemVM>();
			this.OtherSkills = new MBBindingList<CharacterCreationGainedSkillItemVM>();
			List<CharacterAttribute> list = Attributes.All.ToList<CharacterAttribute>();
			list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
			foreach (CharacterAttribute attributeObj in list)
			{
				this.GainGroups.Add(new CharacterCreationGainGroupItemVM(attributeObj));
			}
			List<SkillObject> list2 = Skills.All.ToList<SkillObject>();
			list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			using (List<SkillObject>.Enumerator enumerator2 = list2.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					SkillObject skill = enumerator2.Current;
					Func<CharacterCreationGainedSkillItemVM, bool> <>9__1;
					if (!this.GainGroups.Any(delegate(CharacterCreationGainGroupItemVM attribute)
					{
						IEnumerable<CharacterCreationGainedSkillItemVM> skills = attribute.Skills;
						Func<CharacterCreationGainedSkillItemVM, bool> predicate;
						if ((predicate = <>9__1) == null)
						{
							predicate = (<>9__1 = (CharacterCreationGainedSkillItemVM attributeSkill) => attributeSkill.SkillId == skill.StringId);
						}
						return skills.Any(predicate);
					}))
					{
						this.OtherSkills.Add(new CharacterCreationGainedSkillItemVM(skill));
					}
				}
			}
			this.GainedTraits = new MBBindingList<EncyclopediaTraitItemVM>();
			this.UpdateValues();
		}

		// Token: 0x06001FB0 RID: 8112 RVA: 0x00073D20 File Offset: 0x00071F20
		public void UpdateValues()
		{
			this._affectedAttributesMap.Clear();
			this._affectedSkillMap.Clear();
			this.GainGroups.ApplyActionOnAllItems(delegate(CharacterCreationGainGroupItemVM g)
			{
				g.ResetValues();
			});
			this.OtherSkills.ApplyActionOnAllItems(delegate(CharacterCreationGainedSkillItemVM s)
			{
				s.ResetValues();
			});
			this.PopulateInitialValues();
			this.PopulateGainedAttributeValues();
			this.PopulateGainedTraitValues();
			foreach (KeyValuePair<CharacterAttribute, Tuple<int, int>> keyValuePair in this._affectedAttributesMap)
			{
				this.GetItemFromAttribute(keyValuePair.Key).SetValue(keyValuePair.Value.Item1, keyValuePair.Value.Item2);
			}
			foreach (KeyValuePair<SkillObject, Tuple<int, int>> keyValuePair2 in this._affectedSkillMap)
			{
				this.GetItemFromSkill(keyValuePair2.Key).SetValue(keyValuePair2.Value.Item1, keyValuePair2.Value.Item2);
			}
		}

		// Token: 0x06001FB1 RID: 8113 RVA: 0x00073E78 File Offset: 0x00072078
		private void PopulateInitialValues()
		{
			foreach (SkillObject skillObject in Skills.All)
			{
				int focus = Hero.MainHero.HeroDeveloper.GetFocus(skillObject);
				if (this._affectedSkillMap.ContainsKey(skillObject))
				{
					Tuple<int, int> tuple = this._affectedSkillMap[skillObject];
					this._affectedSkillMap[skillObject] = new Tuple<int, int>(tuple.Item1 + focus, 0);
				}
				else
				{
					this._affectedSkillMap.Add(skillObject, new Tuple<int, int>(focus, 0));
				}
			}
			foreach (CharacterAttribute characterAttribute in Attributes.All)
			{
				int attributeValue = Hero.MainHero.GetAttributeValue(characterAttribute);
				if (this._affectedAttributesMap.ContainsKey(characterAttribute))
				{
					Tuple<int, int> tuple2 = this._affectedAttributesMap[characterAttribute];
					this._affectedAttributesMap[characterAttribute] = new Tuple<int, int>(tuple2.Item1 + attributeValue, 0);
				}
				else
				{
					this._affectedAttributesMap.Add(characterAttribute, new Tuple<int, int>(attributeValue, 0));
				}
			}
		}

		// Token: 0x06001FB2 RID: 8114 RVA: 0x00073FBC File Offset: 0x000721BC
		private void PopulateGainedAttributeValues()
		{
			foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> keyValuePair in this._characterCreationManager.SelectedOptions)
			{
				NarrativeMenu key = keyValuePair.Key;
				NarrativeMenuOption value = keyValuePair.Value;
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				if (key == this._characterCreationManager.CurrentMenu)
				{
					num = value.Args.AttributeLevelToAdd;
				}
				else
				{
					num2 += value.Args.AttributeLevelToAdd;
				}
				if (value.Args.EffectedAttribute != null)
				{
					if (this._affectedAttributesMap.ContainsKey(value.Args.EffectedAttribute))
					{
						Tuple<int, int> tuple = this._affectedAttributesMap[value.Args.EffectedAttribute];
						this._affectedAttributesMap[value.Args.EffectedAttribute] = new Tuple<int, int>(tuple.Item1 + num2, tuple.Item2 + num);
					}
					else
					{
						this._affectedAttributesMap.Add(value.Args.EffectedAttribute, new Tuple<int, int>(num2, num));
					}
				}
				if (key == this._characterCreationManager.CurrentMenu)
				{
					num3 = value.Args.FocusToAdd;
				}
				else
				{
					num4 += value.Args.FocusToAdd;
				}
				foreach (SkillObject key2 in value.Args.AffectedSkills)
				{
					if (this._affectedSkillMap.ContainsKey(key2))
					{
						Tuple<int, int> tuple2 = this._affectedSkillMap[key2];
						this._affectedSkillMap[key2] = new Tuple<int, int>(tuple2.Item1 + num4, tuple2.Item2 + num3);
					}
					else
					{
						this._affectedSkillMap.Add(key2, new Tuple<int, int>(num4, num3));
					}
				}
			}
		}

		// Token: 0x06001FB3 RID: 8115 RVA: 0x000741C8 File Offset: 0x000723C8
		private void PopulateGainedTraitValues()
		{
			this.GainedTraits.Clear();
			foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> keyValuePair in this._characterCreationManager.SelectedOptions)
			{
				NarrativeMenuOption value = keyValuePair.Value;
				if (value.Args.AffectedTraits != null && value.Args.AffectedTraits.Count > 0)
				{
					using (List<TraitObject>.Enumerator enumerator2 = value.Args.AffectedTraits.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							TraitObject effectedTrait = enumerator2.Current;
							if (this.GainedTraits.FirstOrDefault((EncyclopediaTraitItemVM t) => t.TraitId == effectedTrait.StringId) == null)
							{
								this.GainedTraits.Add(new EncyclopediaTraitItemVM(effectedTrait, 1));
							}
						}
					}
				}
			}
		}

		// Token: 0x06001FB4 RID: 8116 RVA: 0x000742D4 File Offset: 0x000724D4
		private CharacterCreationGainedAttributeItemVM GetItemFromAttribute(CharacterAttribute attribute)
		{
			CharacterCreationGainGroupItemVM characterCreationGainGroupItemVM = this.GainGroups.SingleOrDefault((CharacterCreationGainGroupItemVM g) => g.AttributeObj == attribute);
			if (characterCreationGainGroupItemVM == null)
			{
				return null;
			}
			return characterCreationGainGroupItemVM.Attribute;
		}

		// Token: 0x06001FB5 RID: 8117 RVA: 0x00074310 File Offset: 0x00072510
		private CharacterCreationGainedSkillItemVM GetItemFromSkill(SkillObject skill)
		{
			foreach (CharacterCreationGainGroupItemVM characterCreationGainGroupItemVM in this.GainGroups)
			{
				foreach (CharacterCreationGainedSkillItemVM characterCreationGainedSkillItemVM in characterCreationGainGroupItemVM.Skills)
				{
					if (characterCreationGainedSkillItemVM.SkillObj == skill)
					{
						return characterCreationGainedSkillItemVM;
					}
				}
			}
			foreach (CharacterCreationGainedSkillItemVM characterCreationGainedSkillItemVM2 in this.OtherSkills)
			{
				if (characterCreationGainedSkillItemVM2.SkillObj == skill)
				{
					return characterCreationGainedSkillItemVM2;
				}
			}
			return null;
		}

		// Token: 0x17000ACB RID: 2763
		// (get) Token: 0x06001FB6 RID: 8118 RVA: 0x000743E0 File Offset: 0x000725E0
		// (set) Token: 0x06001FB7 RID: 8119 RVA: 0x000743E8 File Offset: 0x000725E8
		[DataSourceProperty]
		public MBBindingList<CharacterCreationGainGroupItemVM> GainGroups
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
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationGainGroupItemVM>>(value, "GainGroups");
				}
			}
		}

		// Token: 0x17000ACC RID: 2764
		// (get) Token: 0x06001FB8 RID: 8120 RVA: 0x00074406 File Offset: 0x00072606
		// (set) Token: 0x06001FB9 RID: 8121 RVA: 0x0007440E File Offset: 0x0007260E
		[DataSourceProperty]
		public MBBindingList<EncyclopediaTraitItemVM> GainedTraits
		{
			get
			{
				return this._gainedTraits;
			}
			set
			{
				if (value != this._gainedTraits)
				{
					this._gainedTraits = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaTraitItemVM>>(value, "GainedTraits");
				}
			}
		}

		// Token: 0x17000ACD RID: 2765
		// (get) Token: 0x06001FBA RID: 8122 RVA: 0x0007442C File Offset: 0x0007262C
		// (set) Token: 0x06001FBB RID: 8123 RVA: 0x00074434 File Offset: 0x00072634
		[DataSourceProperty]
		public MBBindingList<CharacterCreationGainedSkillItemVM> OtherSkills
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
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationGainedSkillItemVM>>(value, "OtherSkills");
				}
			}
		}

		// Token: 0x04000EC4 RID: 3780
		private readonly CharacterCreationManager _characterCreationManager;

		// Token: 0x04000EC5 RID: 3781
		private readonly Dictionary<CharacterAttribute, Tuple<int, int>> _affectedAttributesMap;

		// Token: 0x04000EC6 RID: 3782
		private readonly Dictionary<SkillObject, Tuple<int, int>> _affectedSkillMap;

		// Token: 0x04000EC7 RID: 3783
		private MBBindingList<CharacterCreationGainGroupItemVM> _gainGroups;

		// Token: 0x04000EC8 RID: 3784
		private MBBindingList<EncyclopediaTraitItemVM> _gainedTraits;

		// Token: 0x04000EC9 RID: 3785
		private MBBindingList<CharacterCreationGainedSkillItemVM> _otherSkills;
	}
}
