using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000140 RID: 320
	public class CharacterAttributeItemVM : ViewModel
	{
		// Token: 0x17000A39 RID: 2617
		// (get) Token: 0x06001E13 RID: 7699 RVA: 0x0006F12F File Offset: 0x0006D32F
		// (set) Token: 0x06001E14 RID: 7700 RVA: 0x0006F137 File Offset: 0x0006D337
		public CharacterAttribute AttributeType { get; private set; }

		// Token: 0x06001E15 RID: 7701 RVA: 0x0006F140 File Offset: 0x0006D340
		public CharacterAttributeItemVM(Hero hero, CharacterAttribute currAtt, CharacterDeveloperHeroItemVM developerVM, Action<CharacterAttributeItemVM> onInpectAttribute, Action<CharacterAttributeItemVM> onAddAttributePoint)
		{
			this._hero = hero;
			this._developer = this._hero.HeroDeveloper;
			this._characterVM = developerVM;
			this.AttributeType = currAtt;
			this._onInpectAttribute = onInpectAttribute;
			this._onAddAttributePoint = onAddAttributePoint;
			this._initialAttValue = this._characterVM.CharacterAttributes.GetPropertyValue(currAtt);
			this.AttributeValue = this._initialAttValue;
			this.BoundSkills = new MBBindingList<AttributeBoundSkillItemVM>();
			this.RefreshWithCurrentValues();
			this.RefreshValues();
			this.UnspentAttributePoints = this._characterVM.UnspentAttributePoints;
		}

		// Token: 0x06001E16 RID: 7702 RVA: 0x0006F1D4 File Offset: 0x0006D3D4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.AttributeType.Abbreviation.ToString();
			string content = this.AttributeType.Description.ToString();
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("ATTRIBUTE_NAME", this.AttributeType.Name);
			TextObject textObject = GameTexts.FindText("str_skill_attribute_bound_skills", null);
			textObject.SetTextVariable("IS_SOCIAL", (this.AttributeType == DefaultCharacterAttributes.Social) ? 1 : 0);
			GameTexts.SetVariable("STR2", textObject);
			this.Description = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			TextObject textObject2 = GameTexts.FindText("str_skill_attribute_increase_description", null);
			textObject2.SetTextVariable("IS_SOCIAL", (this.AttributeType == DefaultCharacterAttributes.Social) ? 1 : 0);
			GameTexts.SetVariable("NUMBER", this.UnspentAttributePoints);
			this.UnspentAttributePointsText = GameTexts.FindText("str_free_attribute_points", null).ToString();
			this.IncreaseHelpText = textObject2.ToString();
			this.BoundSkills.Clear();
			List<SkillObject> list = Skills.All.ToList<SkillObject>();
			list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			using (List<SkillObject>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					SkillObject skill = enumerator.Current;
					if (skill.Attributes.Contains(this.AttributeType) && !this.BoundSkills.Any((AttributeBoundSkillItemVM s) => s.SkillId == skill.StringId))
					{
						this.BoundSkills.Add(new AttributeBoundSkillItemVM(skill));
					}
				}
			}
		}

		// Token: 0x06001E17 RID: 7703 RVA: 0x0006F384 File Offset: 0x0006D584
		public void ExecuteInspectAttribute()
		{
			Action<CharacterAttributeItemVM> onInpectAttribute = this._onInpectAttribute;
			if (onInpectAttribute == null)
			{
				return;
			}
			onInpectAttribute(this);
		}

		// Token: 0x06001E18 RID: 7704 RVA: 0x0006F397 File Offset: 0x0006D597
		public void ExecuteAddAttributePoint()
		{
			Action<CharacterAttributeItemVM> onAddAttributePoint = this._onAddAttributePoint;
			if (onAddAttributePoint != null)
			{
				onAddAttributePoint(this);
			}
			this.UnspentAttributePoints = this._characterVM.UnspentAttributePoints;
			this.RefreshWithCurrentValues();
		}

		// Token: 0x06001E19 RID: 7705 RVA: 0x0006F3C2 File Offset: 0x0006D5C2
		public void Reset()
		{
			this.RefreshWithCurrentValues();
		}

		// Token: 0x06001E1A RID: 7706 RVA: 0x0006F3CC File Offset: 0x0006D5CC
		public void RefreshWithCurrentValues()
		{
			this.UnspentAttributePoints = this._characterVM.UnspentAttributePoints;
			this.AttributeValue = this._characterVM.CharacterAttributes.GetPropertyValue(this.AttributeType);
			this.CanAddPoint = this.AttributeValue < Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute && this._characterVM.UnspentAttributePoints > 0;
			this.IsAttributeAtMax = this.AttributeValue >= Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute;
		}

		// Token: 0x06001E1B RID: 7707 RVA: 0x0006F460 File Offset: 0x0006D660
		public void Commit()
		{
			for (int i = 0; i < this.AttributeValue - this._initialAttValue; i++)
			{
				this._developer.AddAttribute(this.AttributeType, 1, true);
			}
		}

		// Token: 0x17000A3A RID: 2618
		// (get) Token: 0x06001E1C RID: 7708 RVA: 0x0006F498 File Offset: 0x0006D698
		// (set) Token: 0x06001E1D RID: 7709 RVA: 0x0006F4A0 File Offset: 0x0006D6A0
		[DataSourceProperty]
		public MBBindingList<AttributeBoundSkillItemVM> BoundSkills
		{
			get
			{
				return this._boundSkills;
			}
			set
			{
				if (value != this._boundSkills)
				{
					this._boundSkills = value;
					base.OnPropertyChangedWithValue<MBBindingList<AttributeBoundSkillItemVM>>(value, "BoundSkills");
				}
			}
		}

		// Token: 0x17000A3B RID: 2619
		// (get) Token: 0x06001E1E RID: 7710 RVA: 0x0006F4BE File Offset: 0x0006D6BE
		// (set) Token: 0x06001E1F RID: 7711 RVA: 0x0006F4C6 File Offset: 0x0006D6C6
		[DataSourceProperty]
		public int AttributeValue
		{
			get
			{
				return this._atttributeValue;
			}
			set
			{
				if (value != this._atttributeValue)
				{
					this._atttributeValue = value;
					base.OnPropertyChangedWithValue(value, "AttributeValue");
				}
			}
		}

		// Token: 0x17000A3C RID: 2620
		// (get) Token: 0x06001E20 RID: 7712 RVA: 0x0006F4E4 File Offset: 0x0006D6E4
		// (set) Token: 0x06001E21 RID: 7713 RVA: 0x0006F4EC File Offset: 0x0006D6EC
		[DataSourceProperty]
		public int UnspentAttributePoints
		{
			get
			{
				return this._unspentAttributePoints;
			}
			set
			{
				if (value != this._unspentAttributePoints)
				{
					this._unspentAttributePoints = value;
					base.OnPropertyChangedWithValue(value, "UnspentAttributePoints");
					GameTexts.SetVariable("NUMBER", value);
					this.UnspentAttributePointsText = GameTexts.FindText("str_free_attribute_points", null).ToString();
				}
			}
		}

		// Token: 0x17000A3D RID: 2621
		// (get) Token: 0x06001E22 RID: 7714 RVA: 0x0006F52B File Offset: 0x0006D72B
		// (set) Token: 0x06001E23 RID: 7715 RVA: 0x0006F533 File Offset: 0x0006D733
		[DataSourceProperty]
		public string UnspentAttributePointsText
		{
			get
			{
				return this._unspentAttributePointsText;
			}
			set
			{
				if (value != this._unspentAttributePointsText)
				{
					this._unspentAttributePointsText = value;
					base.OnPropertyChangedWithValue<string>(value, "UnspentAttributePointsText");
				}
			}
		}

		// Token: 0x17000A3E RID: 2622
		// (get) Token: 0x06001E24 RID: 7716 RVA: 0x0006F556 File Offset: 0x0006D756
		// (set) Token: 0x06001E25 RID: 7717 RVA: 0x0006F55E File Offset: 0x0006D75E
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

		// Token: 0x17000A3F RID: 2623
		// (get) Token: 0x06001E26 RID: 7718 RVA: 0x0006F581 File Offset: 0x0006D781
		// (set) Token: 0x06001E27 RID: 7719 RVA: 0x0006F589 File Offset: 0x0006D789
		[DataSourceProperty]
		public string NameExtended
		{
			get
			{
				return this._nameExtended;
			}
			set
			{
				if (value != this._nameExtended)
				{
					this._nameExtended = value;
					base.OnPropertyChangedWithValue<string>(value, "NameExtended");
				}
			}
		}

		// Token: 0x17000A40 RID: 2624
		// (get) Token: 0x06001E28 RID: 7720 RVA: 0x0006F5AC File Offset: 0x0006D7AC
		// (set) Token: 0x06001E29 RID: 7721 RVA: 0x0006F5B4 File Offset: 0x0006D7B4
		[DataSourceProperty]
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (value != this._description)
				{
					this._description = value;
					base.OnPropertyChangedWithValue<string>(value, "Description");
				}
			}
		}

		// Token: 0x17000A41 RID: 2625
		// (get) Token: 0x06001E2A RID: 7722 RVA: 0x0006F5D7 File Offset: 0x0006D7D7
		// (set) Token: 0x06001E2B RID: 7723 RVA: 0x0006F5DF File Offset: 0x0006D7DF
		[DataSourceProperty]
		public string IncreaseHelpText
		{
			get
			{
				return this._increaseHelpText;
			}
			set
			{
				if (value != this._increaseHelpText)
				{
					this._increaseHelpText = value;
					base.OnPropertyChangedWithValue<string>(value, "IncreaseHelpText");
				}
			}
		}

		// Token: 0x17000A42 RID: 2626
		// (get) Token: 0x06001E2C RID: 7724 RVA: 0x0006F602 File Offset: 0x0006D802
		// (set) Token: 0x06001E2D RID: 7725 RVA: 0x0006F60A File Offset: 0x0006D80A
		[DataSourceProperty]
		public bool IsInspecting
		{
			get
			{
				return this._isInspecting;
			}
			set
			{
				if (value != this._isInspecting)
				{
					this._isInspecting = value;
					base.OnPropertyChangedWithValue(value, "IsInspecting");
				}
			}
		}

		// Token: 0x17000A43 RID: 2627
		// (get) Token: 0x06001E2E RID: 7726 RVA: 0x0006F628 File Offset: 0x0006D828
		// (set) Token: 0x06001E2F RID: 7727 RVA: 0x0006F630 File Offset: 0x0006D830
		[DataSourceProperty]
		public bool IsAttributeAtMax
		{
			get
			{
				return this._isAttributeAtMax;
			}
			set
			{
				if (value != this._isAttributeAtMax)
				{
					this._isAttributeAtMax = value;
					base.OnPropertyChangedWithValue(value, "IsAttributeAtMax");
				}
			}
		}

		// Token: 0x17000A44 RID: 2628
		// (get) Token: 0x06001E30 RID: 7728 RVA: 0x0006F64E File Offset: 0x0006D84E
		// (set) Token: 0x06001E31 RID: 7729 RVA: 0x0006F656 File Offset: 0x0006D856
		[DataSourceProperty]
		public bool CanAddPoint
		{
			get
			{
				return this._canAddPoint;
			}
			set
			{
				if (value != this._canAddPoint)
				{
					this._canAddPoint = value;
					base.OnPropertyChangedWithValue(value, "CanAddPoint");
				}
			}
		}

		// Token: 0x04000E0E RID: 3598
		private readonly Hero _hero;

		// Token: 0x04000E10 RID: 3600
		private readonly HeroDeveloper _developer;

		// Token: 0x04000E11 RID: 3601
		private readonly int _initialAttValue;

		// Token: 0x04000E12 RID: 3602
		private readonly Action<CharacterAttributeItemVM> _onInpectAttribute;

		// Token: 0x04000E13 RID: 3603
		private readonly Action<CharacterAttributeItemVM> _onAddAttributePoint;

		// Token: 0x04000E14 RID: 3604
		private readonly CharacterDeveloperHeroItemVM _characterVM;

		// Token: 0x04000E15 RID: 3605
		private int _atttributeValue;

		// Token: 0x04000E16 RID: 3606
		private int _unspentAttributePoints;

		// Token: 0x04000E17 RID: 3607
		private string _unspentAttributePointsText;

		// Token: 0x04000E18 RID: 3608
		private bool _canAddPoint;

		// Token: 0x04000E19 RID: 3609
		private bool _isInspecting;

		// Token: 0x04000E1A RID: 3610
		private bool _isAttributeAtMax;

		// Token: 0x04000E1B RID: 3611
		private string _name;

		// Token: 0x04000E1C RID: 3612
		private string _nameExtended;

		// Token: 0x04000E1D RID: 3613
		private string _description;

		// Token: 0x04000E1E RID: 3614
		private string _increaseHelpText;

		// Token: 0x04000E1F RID: 3615
		private MBBindingList<AttributeBoundSkillItemVM> _boundSkills;
	}
}
